using System.Text;
using TapBrawl.Core.Skills;
using TapBrawl.Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>
    /// Универсальный экран подробной информации о скилле.
    /// UI собирается вручную в Unity Inspector, код только подставляет данные.
    /// </summary>
    public sealed class SkillDetailsScreenController : MonoBehaviour
    {
        [SerializeField] private SkillCatalog? skillCatalog;

        [Header("Навигация")]
        [SerializeField] private LobbyModalsHost? lobbyModals;
        [SerializeField] private UiPanelToggle? panelToggle;
        [SerializeField] private string skillsSceneName = "Skills";
        [SerializeField] private Button? backButton;

        [Header("Тексты")]
        [SerializeField] private Text? titleText;
        [SerializeField] private Text? levelText;
        [SerializeField] private Text? shortDescriptionText;
        [SerializeField] private Text? statsText;
        [SerializeField] private Text? statusText;

        [Header("Иконка")]
        [SerializeField] private Image? skillIconImage;

        private void Awake()
        {
            if (backButton != null)
            {
                UiModalStyle.PrepareBackButton(backButton);
                backButton.onClick.AddListener(ClosePanel);
            }
        }

        private void OnDestroy()
        {
            if (backButton != null)
                backButton.onClick.RemoveListener(ClosePanel);
        }

        private void OnEnable()
        {
            var skillId = ResolveSkillIdFromPending();
            RenderSkill(skillId);
        }

        private int ResolveSkillIdFromPending()
        {
            if (PendingSkillDetails.TryConsume(out var pendingSkillId) && SkillBalance.IsKnownSkillId(pendingSkillId))
                return pendingSkillId;
            return SkillBalance.KnownSkillIds[0];
        }

        private void RenderSkill(int skillId)
        {
            var level = PlayerSkillsRuntimeState.GetLevel(skillId);

            if (titleText != null)
                titleText.text = skillCatalog != null ? skillCatalog.GetDisplayName(skillId) : SkillDefinitions.GetDisplayName(skillId);
            if (levelText != null)
                levelText.text = $"Текущий уровень: {level}/{SkillBalance.MaxLevel}";
            if (shortDescriptionText != null)
                shortDescriptionText.text = skillCatalog != null ? skillCatalog.GetShortHint(skillId) : SkillDefinitions.GetShortHint(skillId);
            if (statsText != null)
                statsText.text = BuildStats(skillId, level);
            if (statusText != null)
                statusText.text = $"SkillId: {skillId}";

            ApplyIcon(skillId);
        }

        private string BuildStats(int skillId, int level)
        {
            var clamped = SkillBalance.ClampLevel(level);
            var sb = new StringBuilder();
            switch (skillId)
            {
                case SkillBalance.GiantCirclesSkillId:
                    sb.AppendLine("Эффект: бафф своих целей");
                    sb.AppendLine($"Длительность: {SkillBalance.GiantCirclesDurationSec(clamped):0.##} с");
                    sb.AppendLine($"Множитель размера: x{SkillBalance.GiantCirclesSizeMultiplier(clamped):0.##}");
                    break;
                case SkillBalance.RedDeceptionSkillId:
                    sb.AppendLine("Эффект: визуальный дебафф соперника");
                    sb.AppendLine($"Длительность: {SkillBalance.RedDeceptionDurationSec(clamped):0.##} с");
                    break;
                case SkillBalance.SmokeVeilSkillId:
                    sb.AppendLine("Эффект: дым на поле соперника");
                    sb.AppendLine($"Длительность: {SkillBalance.SmokeVeilDurationSec(clamped):0.##} с");
                    break;
                case SkillBalance.OverheatSkillId:
                    sb.AppendLine("Эффект: ускорение темпа появления целей");
                    sb.AppendLine($"Длительность: {SkillBalance.OverheatDurationSec(clamped):0.##} с");
                    sb.AppendLine($"Каденс-коэфф: x{SkillBalance.OverheatSpawnCadenceMultiplier(clamped):0.##}");
                    break;
                case SkillBalance.ChainDischargeSkillId:
                    sb.AppendLine("Эффект: следующий идеальный тап разряжает цепь");
                    sb.AppendLine($"Окно разряда: {SkillBalance.ChainDischargeArmedDurationSec(clamped):0.##} с");
                    sb.AppendLine($"Доп. авто-тапов: +{SkillBalance.ChainDischargeAdditionalTaps(clamped)}");
                    break;
                default:
                    sb.Append("Нет данных для выбранного скилла.");
                    break;
            }

            var upgradeCost = SkillBalance.UpgradeCostCoins(skillId, clamped);
            sb.AppendLine(upgradeCost > 0
                ? $"Следующий апгрейд: {upgradeCost} монет"
                : "Скилл прокачан до максимума");
            return sb.ToString().TrimEnd();
        }

        private void ApplyIcon(int skillId)
        {
            if (skillIconImage == null)
                return;

            var icon = skillCatalog != null ? skillCatalog.GetIcon(skillId) : null;
            if (icon != null)
            {
                skillIconImage.enabled = true;
                skillIconImage.sprite = icon;
                return;
            }

            skillIconImage.enabled = false;
            skillIconImage.sprite = null;
        }

        private void ClosePanel()
        {
            if (panelToggle != null)
            {
                panelToggle.Hide();
                return;
            }

            var modals = lobbyModals != null ? lobbyModals : LobbyModalsHost.Instance;
            if (modals != null)
            {
                modals.CloseSkillDetails();
                return;
            }

            OpenSkillsSceneLegacy();
        }

        private void OpenSkillsSceneLegacy()
        {
            if (!string.IsNullOrEmpty(skillsSceneName))
                SceneManager.LoadScene(skillsSceneName, LoadSceneMode.Single);
        }
    }
}
