using System;
using System.Collections.Generic;
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
    /// Панель в лобби: смена ника и аватара через <c>PUT /api/v1/players/me</c>.
    /// </summary>
    public sealed class ProfilePanelController : MonoBehaviour
    {
        private const int RecentMatchDotCount = 5;
        private const float RecentMatchDotSize = 44f;
        private static readonly Color RecentMatchWinColor = new(0.28f, 0.78f, 0.38f, 1f);
        private static readonly Color RecentMatchLossColor = new(0.9f, 0.28f, 0.28f, 1f);
        private static readonly Color RecentMatchEmptyColor = new(0.35f, 0.35f, 0.4f, 0.35f);
        private static Sprite? _recentMatchDotSprite;

        [SerializeField] private BackendConfig backendConfig = null!;
        [SerializeField] private string authSceneName = "Auth";
        [SerializeField] private InputField? usernameInput;
        [SerializeField] private ProfileAvatarPicker? avatarPicker;
        [SerializeField] private Button? saveButton;
        [Header("Профиль: ID")]
        [SerializeField] private Text? playerIdText;
        [SerializeField] private Button? copyIdButton;
        [Header("Профиль: валюта")]
        [SerializeField] private Text? coinsText;
        [SerializeField] private Text? gemsText;
        [SerializeField] private Text? equivalentText;
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
        [SerializeField] private Text? avgReactionText;
        [SerializeField] private Text? totalTapsText;
        [Header("История матчей")]
        [SerializeField] private Transform? matchHistoryContainer;
        [Header("Профиль: мини-карточка достижений")]
        [SerializeField] private Text? achievementsMiniText;
        [SerializeField] private Text? statusText;

        private void Awake()
        {
            if (avatarPicker == null)
                avatarPicker = GetComponent<ProfileAvatarPicker>();
            if (avatarPicker == null)
                avatarPicker = gameObject.AddComponent<ProfileAvatarPicker>();

            TryAutoWireCurrencyTexts();
            TryAutoWireStatsTexts();
            TryAutoWireMatchHistoryContainer();
            if (saveButton != null)
                saveButton.onClick.AddListener(OnSaveClicked);
            if (copyIdButton != null)
                copyIdButton.onClick.AddListener(OnCopyIdClicked);
        }

        private void OnEnable()
        {
            CurrencyState.BalancesUpdated -= OnBalancesUpdated;
            CurrencyState.BalancesUpdated += OnBalancesUpdated;

            var s = AuthContext.Current?.Player;
            if (s != null)
            {
                if (usernameInput != null)
                    usernameInput.text = s.Username;
                BindAvatarPicker(s);
                RenderIdentityBlock(s);
                RenderCurrencyBlock(s);
                RenderProgressBlock(s);
            }
            RenderFallbackStats();
            RenderMatchHistory(null);
            SetStatus(string.Empty);
            _ = RefreshExtendedProfileAsync();
        }

        private void OnDisable()
        {
            CurrencyState.BalancesUpdated -= OnBalancesUpdated;
        }

        private void OnBalancesUpdated(int coins, int gems)
        {
            var player = AuthContext.Current?.Player;
            if (player == null)
                return;
            player.Coins = coins;
            player.Gems = gems;
            RenderCurrencyBlock(player);
        }

        private void TryAutoWireCurrencyTexts()
        {
            var block = FindBlockCoint();
            if (block == null)
                return;

            if (coinsText == null)
                coinsText = block.Find("CoinsText")?.GetComponent<Text>();
            if (gemsText == null)
                gemsText = block.Find("GemsText")?.GetComponent<Text>();
            if (equivalentText == null)
                equivalentText = block.Find("EquivalentText")?.GetComponent<Text>();

            EnsureCurrencyText(block, "GemsText", ref gemsText, "Adipoint: 0");
            EnsureCurrencyText(block, "EquivalentText", ref equivalentText, "≈ 0 монет");
        }

        private Transform? FindBlockCoint()
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "BlockCoint")
                    return t;
            }

            return null;
        }

        private static void EnsureCurrencyText(Transform block, string name, ref Text? field, string placeholder)
        {
            if (field != null)
                return;

            var existing = block.Find(name);
            if (existing != null)
            {
                field = existing.GetComponent<Text>();
                return;
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(block, false);
            var text = go.GetComponent<Text>();
            text.text = placeholder;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.color = UiModalStyle.ProfileAccentTextColor;
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = 40f;
            le.minHeight = 40f;
            field = text;
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
                    AvatarId = avatarPicker != null ? avatarPicker.SelectedAvatarId : null,
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
                BindAvatarPicker(session.Player);
                RenderIdentityBlock(session.Player);
                RenderCurrencyBlock(session.Player);
                RenderProgressBlock(session.Player);
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
                    BindAvatarPicker(session.Player);
                    RenderIdentityBlock(session.Player);
                    RenderCurrencyBlock(session.Player);
                    RenderProgressBlock(session.Player);
                }

                var stats = await api.PlayersStatsAsync(session.Player.Id, CancellationToken.None).ConfigureAwait(true);
                List<RecentMatchDto>? recentMatches = null;
                var recent = await api.PlayersRecentMatchesAsync(session.AccessToken, 5, CancellationToken.None)
                    .ConfigureAwait(true);
                if (recent.Success && recent.Data != null)
                    recentMatches = recent.Data;

                if (stats.Success && stats.Data != null)
                {
                    RenderStatsBlock(stats.Data, recentMatches);
                    RenderAchievementsMini(stats.Data);
                }

                RenderMatchHistory(recentMatches);
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

        private void BindAvatarPicker(PlayerProfileDto player)
        {
            if (avatarPicker == null)
                return;

            avatarPicker.Bind(player.AvatarId, player.UnlockedAvatarIds);
        }

        private void RenderIdentityBlock(PlayerProfileDto player)
        {
            if (playerIdText != null)
                playerIdText.text = $"ID: {player.Id:D}";
        }

        private void RenderCurrencyBlock(PlayerProfileDto player)
        {
            if (coinsText != null)
                coinsText.text = CurrencyDisplay.FormatCoins(player.Coins);
            if (gemsText != null)
                gemsText.text = CurrencyDisplay.FormatGems(player.Gems);
            if (equivalentText != null)
                equivalentText.text = CurrencyDisplay.FormatEquivalent(player.Gems);
        }

        private void RenderProgressBlock(PlayerProfileDto player)
        {
            var rankPoints = Mathf.Max(0, player.RankPoints);
            var pointsInDivision = Mathf.Max(0, player.PointsInDivision);
            var pointsToNext = Mathf.Max(1, player.PointsToNextDivision);
            var divisionSize = pointsInDivision + pointsToNext;
            var normalized = divisionSize <= 0 ? 0f : (float)pointsInDivision / divisionSize;

            if (levelText != null)
                levelText.text = player.RankLabel;
            if (xpText != null)
                xpText.text = pointsToNext <= 0
                    ? $"RP: {rankPoints}"
                    : $"RP: {pointsInDivision}/{divisionSize}";
            if (xpFillImage != null)
                xpFillImage.fillAmount = Mathf.Clamp01(normalized);
            if (xpSlider != null)
                xpSlider.value = Mathf.Clamp01(normalized);
        }

        private void TryAutoWireStatsTexts()
        {
            var block = FindBlock("BlockStatistic");
            if (block == null)
                return;

            EnsureStatsText(block, "AvgReactionText", ref avgReactionText, "Реакция: --");
            EnsureStatsText(block, "TotalTapsText", ref totalTapsText, "Всего тапов: --");
        }

        private void TryAutoWireMatchHistoryContainer()
        {
            if (matchHistoryContainer != null)
                return;

            var block = FindBlock("BlockMatchHistory");
            matchHistoryContainer = block?.Find("MatchHistoryContainer");
        }

        private Transform? FindBlock(string blockName)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == blockName)
                    return t;
            }

            return null;
        }

        private static void EnsureStatsText(Transform block, string name, ref Text? field, string placeholder)
        {
            if (field != null)
                return;

            var existing = block.Find(name);
            if (existing != null)
            {
                field = existing.GetComponent<Text>();
                return;
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(block, false);
            var text = go.GetComponent<Text>();
            text.text = placeholder;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.color = UiModalStyle.ProfileAccentTextColor;
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = 40f;
            le.minHeight = 40f;
            field = text;
        }

        private void RenderStatsBlock(PlayerPublicStatsDto stats, IReadOnlyList<RecentMatchDto>? recentMatches)
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
                recordText.text = $"Лучший стрик: {Mathf.Max(0, stats.BestStreak)}";

            if (accuracyText != null)
            {
                var avgAccuracy = ComputeAverageAccuracy(recentMatches);
                accuracyText.text = avgAccuracy.HasValue
                    ? $"Средняя точность: {avgAccuracy.Value:0.#}%"
                    : "Средняя точность: н/д";
            }

            if (avgReactionText != null)
            {
                avgReactionText.text = stats.AvgReactionMs.HasValue
                    ? $"Средняя реакция: {stats.AvgReactionMs.Value:0.#} мс"
                    : "Средняя реакция: н/д";
            }

            if (totalTapsText != null)
                totalTapsText.text = $"Всего тапов: {Mathf.Max(0, stats.TotalTaps)}";
        }

        private static double? ComputeAverageAccuracy(IReadOnlyList<RecentMatchDto>? recentMatches)
        {
            if (recentMatches == null || recentMatches.Count == 0)
                return null;

            double sum = 0;
            var count = 0;
            foreach (var match in recentMatches)
            {
                if (match.MyTaps <= 0 && match.MyMisses <= 0)
                    continue;
                sum += match.AccuracyPercent;
                count++;
            }

            return count <= 0 ? null : sum / count;
        }

        private void RenderMatchHistory(IReadOnlyList<RecentMatchDto>? matches)
        {
            TryAutoWireMatchHistoryContainer();
            if (matchHistoryContainer == null)
                return;

            for (var i = matchHistoryContainer.childCount - 1; i >= 0; i--)
                Destroy(matchHistoryContainer.GetChild(i).gameObject);

            var count = matches?.Count ?? 0;
            var visibleStart = Mathf.Max(0, RecentMatchDotCount - count);
            for (var slot = 0; slot < RecentMatchDotCount; slot++)
            {
                RecentMatchDto? match = null;
                if (slot >= visibleStart && count > 0)
                {
                    var matchIndex = count - 1 - (slot - visibleStart);
                    if (matchIndex >= 0 && matchIndex < count)
                        match = matches![matchIndex];
                }

                CreateMatchHistoryDot(slot, match);
            }
        }

        private void CreateMatchHistoryDot(int slot, RecentMatchDto? match)
        {
            if (matchHistoryContainer == null)
                return;

            var go = new GameObject($"MatchDot_{slot}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(matchHistoryContainer, false);

            var image = go.GetComponent<Image>();
            image.sprite = GetRecentMatchDotSprite();
            image.color = match == null
                ? RecentMatchEmptyColor
                : match.IsWinner
                    ? RecentMatchWinColor
                    : RecentMatchLossColor;
            image.raycastTarget = false;

            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = RecentMatchDotSize;
            le.preferredHeight = RecentMatchDotSize;
            le.minWidth = RecentMatchDotSize;
            le.minHeight = RecentMatchDotSize;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;
        }

        private static Sprite GetRecentMatchDotSprite()
        {
            if (_recentMatchDotSprite != null)
                return _recentMatchDotSprite;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.hideFlags = HideFlags.DontSave;

            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                var ny = (y + 0.5f) / size * 2f - 1f;
                for (var x = 0; x < size; x++)
                {
                    var nx = (x + 0.5f) / size * 2f - 1f;
                    var r = Mathf.Sqrt(nx * nx + ny * ny);
                    var idx = y * size + x;
                    if (r > 0.48f)
                    {
                        pixels[idx] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    var edge = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.43f, 0.48f, r));
                    pixels[idx] = new Color32(255, 255, 255, (byte)(edge * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            _recentMatchDotSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
            _recentMatchDotSprite.hideFlags = HideFlags.HideAndDontSave;
            return _recentMatchDotSprite;
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
                recordText.text = "Лучший стрик: --";
            if (accuracyText != null)
                accuracyText.text = "Средняя точность: --";
            if (avgReactionText != null)
                avgReactionText.text = "Средняя реакция: --";
            if (totalTapsText != null)
                totalTapsText.text = "Всего тапов: --";
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
