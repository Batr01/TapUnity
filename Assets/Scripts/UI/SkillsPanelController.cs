using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TapBrawl.Core.Skills;
using TapBrawl.Models;
using TapBrawl.Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>
    /// Панель лобби: прокачка скиллов и сохранение 3 активных слотов.
    /// Все визуальные элементы создаются руками в Unity и назначаются в инспекторе.
    /// </summary>
    public sealed class SkillsPanelController : MonoBehaviour
    {
        [SerializeField] private BackendConfig? backendConfig;
        [Header("Строки прокачки")]
        [SerializeField] private SkillUpgradeRowView[] skillRows = new SkillUpgradeRowView[0];
        [Header("Лоадаут")]
        [SerializeField] private SkillLoadoutSlotView[] loadoutSlots = new SkillLoadoutSlotView[0];
        [SerializeField] private Button? saveLoadoutButton;
        [Header("Навигация")]
        [SerializeField] private LobbyModalsHost? lobbyModals;
        [SerializeField] private SkillsSceneNavigationController? navigationController;
        [SerializeField] private string skillDetailsSceneName = "Details";
        [Header("Статус")]
        [SerializeField] private Text? statusText;

        private PlayerSkillsStateDto? _lastState;
        private SkillLoadoutPickModalController? _loadoutPickModal;

        private void Awake()
        {
            _loadoutPickModal = GetComponent<SkillLoadoutPickModalController>();

            if (saveLoadoutButton != null)
                saveLoadoutButton.onClick.AddListener(OnSaveLoadoutClicked);

            foreach (var row in skillRows)
            {
                if (row == null || row.UpgradeButton == null)
                    continue;
                var skillId = row.SkillId;
                row.UpgradeButton.onClick.AddListener(() => _ = UpgradeClickedAsync(skillId));
                if (row.DetailsButton != null && row.DetailsButton != row.UpgradeButton)
                    row.DetailsButton.onClick.AddListener(() => OpenSkillDetails(skillId));
            }

            for (var i = 0; i < loadoutSlots.Length; i++)
            {
                var slot = loadoutSlots[i];
                if (slot == null || slot.CycleButton == null)
                    continue;
                var slotIndex = i;
                var slotRef = slot;
                slot.CycleButton.onClick.AddListener(() =>
                {
                    if (_loadoutPickModal != null)
                        _loadoutPickModal.Open(slotIndex, loadoutSlots);
                    else
                        slotRef.CycleNext();
                });
            }
        }

        private void OnEnable() => _ = RefreshAsync();

        private void OnDestroy()
        {
            if (saveLoadoutButton != null)
                saveLoadoutButton.onClick.RemoveListener(OnSaveLoadoutClicked);

            foreach (var row in skillRows)
            {
                if (row != null && row.UpgradeButton != null)
                    row.UpgradeButton.onClick.RemoveAllListeners();
                if (row != null && row.DetailsButton != null)
                    row.DetailsButton.onClick.RemoveAllListeners();
            }

            foreach (var slot in loadoutSlots)
            {
                if (slot != null && slot.CycleButton != null)
                    slot.CycleButton.onClick.RemoveAllListeners();
            }
        }

        public Task RefreshAsync(CancellationToken ct = default) => RefreshInternalAsync(ct);

        private async Task RefreshInternalAsync(CancellationToken ct)
        {
            var session = AuthContext.Current;
            if (session == null || backendConfig == null)
            {
                SetStatus("Нет сессии или BackendConfig.");
                return;
            }

            var api = new ApiClient(backendConfig);
            var res = await api.PlayersMeSkillsAsync(session.AccessToken, ct).ConfigureAwait(true);
            if (!res.Success || res.Data == null)
            {
                SetStatus($"Скиллы: HTTP {res.StatusCode} {res.ErrorBody}");
                return;
            }

            ApplyState(res.Data, session);
            SetStatus($"Монеты: {res.Data.Coins}. Лоадаут: {string.Join(", ", res.Data.LoadoutSlotSkillIds)}");
        }

        private void ApplyState(PlayerSkillsStateDto data, AuthSession session)
        {
            _lastState = data;
            PlayerSkillsRuntimeState.ApplyFromServer(data);
            session.Player.Coins = data.Coins;
            AuthContext.Current = session;
            AuthStorage.Save(session);

            var byId = data.Skills.ToDictionary(x => x.SkillId, x => x);
            foreach (var row in skillRows)
            {
                if (row == null || !byId.TryGetValue(row.SkillId, out var skill))
                    continue;
                row.Bind(skill.Level, skill.NextUpgradeCostCoins, data.Coins >= skill.NextUpgradeCostCoins);
            }

            for (var i = 0; i < loadoutSlots.Length && i < 3; i++)
            {
                if (loadoutSlots[i] == null)
                    continue;
                var skillId = i < data.LoadoutSlotSkillIds.Count
                    ? data.LoadoutSlotSkillIds[i]
                    : SkillBalance.KnownSkillIds[Mathf.Clamp(i, 0, SkillBalance.KnownSkillIds.Length - 1)];
                loadoutSlots[i].BindSlot(i, skillId);
            }
        }

        private async Task UpgradeClickedAsync(int skillId)
        {
            var session = AuthContext.Current;
            if (session == null || backendConfig == null)
                return;

            var current = _lastState?.Skills.FirstOrDefault(x => x.SkillId == skillId);
            if (current != null && current.NextUpgradeCostCoins > session.Player.Coins)
            {
                SetStatus("Недостаточно монет.");
                return;
            }

            var api = new ApiClient(backendConfig);
            var res = await api.PlayersMeSkillsUpgradeAsync(session.AccessToken, skillId, CancellationToken.None)
                .ConfigureAwait(true);
            if (!res.Success || res.Data == null)
            {
                SetStatus($"Апгрейд: HTTP {res.StatusCode} {res.ErrorBody}");
                return;
            }

            ApplyState(res.Data, session);
            SetStatus($"Прокачено. Монеты: {res.Data.Coins}");
        }

        private void OnSaveLoadoutClicked() => _ = SaveLoadoutClickedAsync();

        private async Task SaveLoadoutClickedAsync()
        {
            var session = AuthContext.Current;
            if (session == null || backendConfig == null)
                return;
            if (loadoutSlots.Length < 3)
            {
                SetStatus("Назначьте 3 слота лоадаута в инспекторе.");
                return;
            }

            var ids = new List<int>(3);
            for (var i = 0; i < 3; i++)
            {
                if (loadoutSlots[i] == null)
                {
                    SetStatus($"Слот {i + 1} не назначен.");
                    return;
                }

                ids.Add(loadoutSlots[i].SelectedSkillId);
            }

            if (ids.Distinct().Count() != 3)
            {
                SetStatus("В лоадауте не должно быть одинаковых скиллов.");
                return;
            }

            var api = new ApiClient(backendConfig);
            var res = await api.PlayersMeSkillsLoadoutAsync(session.AccessToken, ids, CancellationToken.None)
                .ConfigureAwait(true);
            if (!res.Success || res.Data == null)
            {
                SetStatus($"Лоадаут: HTTP {res.StatusCode} {res.ErrorBody}");
                return;
            }

            ApplyState(res.Data, session);
            SetStatus("Лоадаут сохранён.");
        }

        private void SetStatus(string msg)
        {
            if (statusText != null)
                statusText.text = msg;
        }

        private void OpenSkillDetails(int skillId)
        {
            if (!SkillBalance.IsKnownSkillId(skillId))
            {
                SetStatus($"Неизвестный SkillId: {skillId}");
                return;
            }

            var modals = lobbyModals != null ? lobbyModals : LobbyModalsHost.Instance;
            if (modals != null)
            {
                modals.OpenSkillDetails(skillId);
                return;
            }

            if (navigationController != null)
            {
                navigationController.OpenSkillDetailsSceneForSkill(skillId);
                return;
            }

            if (string.IsNullOrEmpty(skillDetailsSceneName))
            {
                SetStatus("Не задана сцена деталей скилла.");
                return;
            }

            PendingSkillDetails.Set(skillId);
            SceneManager.LoadScene(skillDetailsSceneName, LoadSceneMode.Single);
        }
    }
}
