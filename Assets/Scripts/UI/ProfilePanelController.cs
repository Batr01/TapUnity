using System;
using System.Threading;
using System.Threading.Tasks;
using TapBrawl.Models;
using TapBrawl.Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>
    /// Панель в лобби: смена ника и URL аватара через <c>PUT /api/v1/players/me</c>.
    /// </summary>
    public sealed class ProfilePanelController : MonoBehaviour
    {
        [SerializeField] private BackendConfig backendConfig = null!;
        [SerializeField] private string authSceneName = "Auth";
        [SerializeField] private InputField? usernameInput;
        [SerializeField] private InputField? avatarUrlInput;
        [SerializeField] private Button? saveButton;
        [Header("Профиль: ID")]
        [SerializeField] private Text? playerIdText;
        [SerializeField] private Button? copyIdButton;
        [Header("Профиль: уровень / XP")]
        [SerializeField] private Text? levelText;
        [SerializeField] private Text? xpText;
        [SerializeField] private Image? xpFillImage;
        [Tooltip("Если Fill не назначен, можно использовать Slider.")]
        [SerializeField] private Slider? xpSlider;
        [Header("Профиль: статистика")]
        [SerializeField] private Text? winrateText;
        [SerializeField] private Text? matchesText;
        [SerializeField] private Text? recordText;
        [SerializeField] private Text? accuracyText;
        [Header("Профиль: мини-карточка достижений")]
        [SerializeField] private Text? achievementsMiniText;
        [SerializeField] private Text? statusText;

        private void Awake()
        {
            if (saveButton != null)
                saveButton.onClick.AddListener(OnSaveClicked);
            if (copyIdButton != null)
                copyIdButton.onClick.AddListener(OnCopyIdClicked);
        }

        private void OnEnable()
        {
            var s = AuthContext.Current?.Player;
            if (s != null)
            {
                if (usernameInput != null)
                    usernameInput.text = s.Username;
                RenderIdentityBlock(s);
                RenderProgressBlock(s.RankPoints);
            }
            RenderFallbackStats();
            SetStatus(string.Empty);
            _ = RefreshExtendedProfileAsync();
        }

        private async void OnSaveClicked()
        {
            if (backendConfig == null)
            {
                SetStatus("Backend Config не назначен.");
                return;
            }

            try
            {
                var api = new ApiClient(backendConfig);
                var auth = new AuthManager(api);
                if (!await auth.TryRestoreSessionAsync(CancellationToken.None).ConfigureAwait(true))
                {
                    SetStatus("Нужен вход.");
                    if (!string.IsNullOrEmpty(authSceneName))
                        SceneManager.LoadScene(authSceneName, LoadSceneMode.Single);
                    return;
                }

                var session = AuthContext.Current;
                if (session == null)
                {
                    SetStatus("Нет сессии.");
                    return;
                }

                var username = usernameInput != null ? usernameInput.text.Trim() : string.Empty;
                var req = new UpdatePlayerProfileRequest
                {
                    Username = string.IsNullOrEmpty(username) ? null : username,
                    AvatarUrl = avatarUrlInput == null || string.IsNullOrWhiteSpace(avatarUrlInput.text)
                        ? null
                        : avatarUrlInput.text.Trim(),
                };

                SetStatus("Сохранение…");
                var r = await api.PlayersPutMeAsync(session.AccessToken, req, CancellationToken.None);
                if (!r.Success || r.Data == null)
                {
                    SetStatus($"Ошибка: HTTP {r.StatusCode} {r.ErrorBody}");
                    return;
                }

                session.Player = r.Data;
                AuthContext.Current = session;
                AuthStorage.Save(session);
                RenderIdentityBlock(session.Player);
                RenderProgressBlock(session.Player.RankPoints);
                SetStatus("Сохранено.");
                _ = RefreshExtendedProfileAsync();
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message);
                Debug.LogException(ex);
            }
        }

        private async Task RefreshExtendedProfileAsync()
        {
            try
            {
                var api = new ApiClient(backendConfig);
                var auth = new AuthManager(api);
                if (!await auth.TryRestoreSessionAsync(CancellationToken.None).ConfigureAwait(true))
                    return;

                var session = AuthContext.Current;
                if (session == null)
                    return;

                var me = await api.PlayersMeAsync(session.AccessToken, CancellationToken.None).ConfigureAwait(true);
                if (me.Success && me.Data != null)
                {
                    session.Player = me.Data;
                    AuthContext.Current = session;
                    AuthStorage.Save(session);
                    RenderIdentityBlock(session.Player);
                    RenderProgressBlock(session.Player.RankPoints);
                }

                var stats = await api.PlayersStatsAsync(session.Player.Id, CancellationToken.None).ConfigureAwait(true);
                if (!stats.Success || stats.Data == null)
                    return;
                RenderStatsBlock(stats.Data);
                RenderAchievementsMini(stats.Data);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Profile] RefreshExtendedProfile: " + ex.Message);
            }
        }

        private void OnCopyIdClicked()
        {
            var id = AuthContext.Current?.Player.Id ?? Guid.Empty;
            if (id == Guid.Empty)
            {
                SetStatus("ID игрока пока недоступен.");
                return;
            }

            GUIUtility.systemCopyBuffer = id.ToString("D");
            SetStatus("ID скопирован.");
        }

        private void RenderIdentityBlock(PlayerProfileDto player)
        {
            if (playerIdText != null)
                playerIdText.text = $"ID: {player.Id:D}";
        }

        private void RenderProgressBlock(int rankPoints)
        {
            var clamped = Mathf.Max(0, rankPoints);
            const int pointsPerLevel = 1000;
            var level = clamped / pointsPerLevel + 1;
            var xpInLevel = clamped % pointsPerLevel;
            var xpNeed = pointsPerLevel;
            var normalized = xpNeed <= 0 ? 0f : (float)xpInLevel / xpNeed;

            if (levelText != null)
                levelText.text = $"Уровень: {level}";
            if (xpText != null)
                xpText.text = $"XP: {xpInLevel}/{xpNeed}";
            if (xpFillImage != null)
                xpFillImage.fillAmount = Mathf.Clamp01(normalized);
            if (xpSlider != null)
                xpSlider.value = Mathf.Clamp01(normalized);
        }

        private void RenderStatsBlock(PlayerPublicStatsDto stats)
        {
            var wins = Mathf.Max(0, stats.Wins);
            var losses = Mathf.Max(0, stats.Losses);
            var matches = wins + losses;
            var winrate = matches <= 0 ? 0f : (100f * wins) / matches;

            if (winrateText != null)
                winrateText.text = $"Winrate: {winrate:0.#}%";
            if (matchesText != null)
                matchesText.text = $"Матчи: {matches} (W {wins} / L {losses})";
            if (recordText != null)
                recordText.text = $"Рекорд (стрик): {Mathf.Max(0, stats.BestStreak)}";

            // Бэкенд пока не отдаёт misses/accuracy для публичного профиля; показываем безопасный fallback.
            if (accuracyText != null)
                accuracyText.text = stats.TotalTaps > 0
                    ? $"Точность тапов: н/д (тапов: {stats.TotalTaps})"
                    : "Точность тапов: н/д";
        }

        private void RenderAchievementsMini(PlayerPublicStatsDto stats)
        {
            var wins = Mathf.Max(0, stats.Wins);
            var losses = Mathf.Max(0, stats.Losses);
            var matches = wins + losses;
            var unlocked = 0;
            if (matches >= 1) unlocked++;
            if (wins >= 10) unlocked++;
            if (wins >= 50) unlocked++;
            if (stats.BestStreak >= 10) unlocked++;
            if (stats.TotalTaps >= 1000) unlocked++;

            if (achievementsMiniText != null)
                achievementsMiniText.text = $"Достижения: {unlocked}/5\nСледующее: Победить 10 матчей";
        }

        private void RenderFallbackStats()
        {
            if (winrateText != null)
                winrateText.text = "Winrate: --";
            if (matchesText != null)
                matchesText.text = "Матчи: --";
            if (recordText != null)
                recordText.text = "Рекорд (стрик): --";
            if (accuracyText != null)
                accuracyText.text = "Точность тапов: --";
            if (achievementsMiniText != null)
                achievementsMiniText.text = "Достижения: --";
        }

        private void SetStatus(string msg)
        {
            if (statusText != null)
                statusText.text = msg;
        }
    }
}
