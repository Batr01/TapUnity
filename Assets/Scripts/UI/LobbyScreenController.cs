using System;
using System.Collections.Generic;
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
    /// Сцена Lobby: показ профиля из <see cref="AuthContext"/> (после Boot), затем актуализация через <c>GET /api/v1/players/me</c>.
    /// Фоновая музыка: назначьте <see cref="lobbyBackgroundMusic"/> в инспекторе (зацикливается на время сцены).
    /// </summary>
    public sealed class LobbyScreenController : MonoBehaviour
    {
        [SerializeField] private BackendConfig? backendConfig;
        [SerializeField] private string authSceneName = "Auth";
        [SerializeField] private string lobbySceneName = "Lobby";
        [SerializeField] private string skillsSceneName = "Skills";
        [SerializeField] private LobbyModalsHost? lobbyModals;
        [SerializeField] private Button? logoutButton;
        [SerializeField] private Text? usernameText;
        [SerializeField] private Text? lobbyCurrencyText;
        [SerializeField] private Text? coinsText;
        [SerializeField] private Text? gemsText;
        [SerializeField] private Text? rankText;
        [SerializeField] private Text? tierText;
        [SerializeField] private LoadingOverlay? loadingOverlay;

        [Header("Фоновая музыка")]
        [Tooltip("Источник для фона лобби. Если пусто — будет создан на этом объекте.")]
        [SerializeField]
        private AudioSource? lobbyMusicSource;
        [Tooltip("Зацикленный трек лобби (оставьте пустым, если музыки нет).")]
        [SerializeField]
        private AudioClip? lobbyBackgroundMusic;
        [Range(0f, 1f)]
        [SerializeField]
        private float lobbyMusicVolume = 0.45f;

        private void Start()
        {
            EnsureLobbyCurrencyBar();
            TryAutoWireGemsText();
            CurrencyState.BalancesUpdated -= OnBalancesUpdated;
            CurrencyState.BalancesUpdated += OnBalancesUpdated;
            if (logoutButton != null)
                logoutButton.onClick.AddListener(Logout);
            PlayLobbyMusicIfConfigured();
            var s = AuthContext.Current;
            if (s == null && AuthStorage.TryLoad(out var stored) && stored != null)
            {
                AuthContext.Current = stored;
                s = stored;
            }

            if (s == null)
            {
                Debug.LogWarning("[Lobby] Нет сессии. Запустите сцену Boot первой или проверьте Build Settings.");
                return;
            }

            ApplyProfileUi(s.Player);
            EnsureIapSubscription();
            if (backendConfig != null)
                _ = LoadLobbyDataAsync();
        }

        private void EnsureIapSubscription()
        {
            if (IapManager.Instance == null)
                return;
            IapManager.Instance.GemsUpdated -= OnGemsUpdated;
            IapManager.Instance.GemsUpdated += OnGemsUpdated;
        }

        private void OnGemsUpdated(int gems)
        {
            var session = AuthContext.Current;
            if (session == null)
                return;
            session.Player.Gems = gems;
            AuthContext.Current = session;
            AuthStorage.Save(session);
            ApplyProfileUi(session.Player);
        }

        private void OnBalancesUpdated(int coins, int gems)
        {
            var session = AuthContext.Current;
            if (session == null)
                return;
            session.Player.Coins = coins;
            session.Player.Gems = gems;
            ApplyProfileUi(session.Player);
        }

        private void EnsureLobbyCurrencyBar()
        {
            if (lobbyCurrencyText != null)
                return;

            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return;

            var bar = canvas.transform.Find("LobbyCurrencyBar");
            if (bar == null)
            {
                var go = new GameObject("LobbyCurrencyBar", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(canvas.transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -112f);
                rt.sizeDelta = new Vector2(0f, 48f);
                var img = go.GetComponent<Image>();
                img.color = new Color(0.12f, 0.14f, 0.2f, 0.85f);
                img.raycastTarget = false;

                var textGo = new GameObject("CurrencyText", typeof(RectTransform), typeof(Text));
                textGo.transform.SetParent(go.transform, false);
                var textRt = textGo.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = new Vector2(16f, 0f);
                textRt.offsetMax = new Vector2(-16f, 0f);
                var text = textGo.GetComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 24;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = UiModalStyle.ProfileAccentTextColor;
                text.raycastTarget = false;
                lobbyCurrencyText = text;
                bar = go.transform;
            }

            lobbyCurrencyText ??= bar.Find("CurrencyText")?.GetComponent<Text>();
        }

        private void TryAutoWireGemsText()
        {
            if (lobbyCurrencyText != null)
                return;

            if (gemsText != null)
                return;

            if (coinsText != null)
            {
                var sibling = coinsText.transform.parent?.Find("GemsText")?.GetComponent<Text>();
                if (sibling != null)
                {
                    gemsText = sibling;
                    return;
                }
            }

            foreach (var text in FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (text.gameObject.name == "GemsText")
                {
                    gemsText = text;
                    return;
                }
            }
        }

        private LoadingOverlay? ResolveLoadingOverlay()
        {
            if (loadingOverlay != null)
                return loadingOverlay;

            var existing = FindFirstObjectByType<LoadingOverlay>(FindObjectsInactive.Include);
            if (existing != null)
                return loadingOverlay = existing;

            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return null;

            return loadingOverlay = LoadingOverlay.EnsureOnCanvas(canvas.transform);
        }

        private async Task LoadLobbyDataAsync()
        {
            if (backendConfig == null)
                return;

            var overlay = ResolveLoadingOverlay();
            overlay?.Show("Загрузка данных…");

            try
            {
                var api = new ApiClient(backendConfig);
                var auth = new AuthManager(api);
                overlay?.SetMessage("Проверка сессии…");
                if (!await auth.TryRestoreSessionAsync(CancellationToken.None).ConfigureAwait(true))
                {
                    Debug.LogWarning("[Lobby] Сессия недействительна. Переход на экран входа.");
                    if (!string.IsNullOrEmpty(authSceneName))
                        SceneManager.LoadScene(authSceneName, LoadSceneMode.Single);
                    return;
                }

                var session = AuthContext.Current;
                if (session == null)
                    return;

                ApplyProfileUi(session.Player);

                overlay?.SetMessage("Загрузка профиля и скиллов…");
                var meTask = api.PlayersMeAsync(session.AccessToken, CancellationToken.None);
                var skillsTask = api.PlayersMeSkillsAsync(session.AccessToken, CancellationToken.None);
                var shopProductsTask = api.ShopProductsAsync(CancellationToken.None);
                var shopExchangeTask = api.ShopExchangePacksAsync(CancellationToken.None);
                await Task.WhenAll(meTask, skillsTask, shopProductsTask, shopExchangeTask).ConfigureAwait(true);

                var me = await meTask.ConfigureAwait(true);
                if (me.Success && me.Data != null)
                {
                    session.Player = me.Data;
                    AuthContext.Current = session;
                    AuthStorage.Save(session);
                    ApplyProfileUi(session.Player);
                }
                else
                    Debug.LogWarning($"[Lobby] GET players/me: HTTP {me.StatusCode} {me.ErrorBody}");

                var skills = await skillsTask.ConfigureAwait(true);
                ApplySkillsResult(session, skills);

                var shopProducts = await shopProductsTask.ConfigureAwait(true);
                if (shopProducts.Success && shopProducts.Data is { Count: > 0 })
                    ShopCatalogCache.SetProducts(shopProducts.Data);

                var shopExchange = await shopExchangeTask.ConfigureAwait(true);
                if (shopExchange.Success && shopExchange.Data is { Count: > 0 })
                    ShopCatalogCache.SetExchangePacks(shopExchange.Data);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Lobby] Загрузка данных: " + ex.Message);
                PlayerSkillsRuntimeState.ApplyOfflineMaxDefaults();
            }
            finally
            {
                overlay?.Hide();
            }
        }

        private static void ApplySkillsResult(AuthSession session, ApiResult<PlayerSkillsStateDto> skills)
        {
            if (skills.Success && skills.Data != null)
            {
                PlayerSkillsRuntimeState.ApplyFromServer(skills.Data);
                session.Player.Coins = skills.Data.Coins;
                AuthContext.Current = session;
                AuthStorage.Save(session);
            }
            else
            {
                Debug.LogWarning($"[Lobby] GET players/me/skills: HTTP {skills.StatusCode} {skills.ErrorBody}");
                PlayerSkillsRuntimeState.ApplyOfflineMaxDefaults();
            }
        }

        private void ApplyProfileUi(PlayerProfileDto p)
        {
            if (usernameText != null)
                usernameText.text = p.Username;
            if (lobbyCurrencyText != null)
                lobbyCurrencyText.text = CurrencyDisplay.FormatLobbyCompact(p.Coins, p.Gems);
            if (coinsText != null)
                coinsText.text = CurrencyDisplay.FormatCoins(p.Coins);
            if (gemsText != null)
                gemsText.text = CurrencyDisplay.FormatGems(p.Gems);
            if (rankText != null)
                rankText.text = $"Рейтинг: {p.RankPoints}";
            if (tierText != null)
                tierText.text = $"Тир: {p.Tier}";
        }

        private void OnDestroy()
        {
            CurrencyState.BalancesUpdated -= OnBalancesUpdated;
            if (IapManager.Instance != null)
                IapManager.Instance.GemsUpdated -= OnGemsUpdated;
            StopLobbyMusic();
        }

        public void Logout()
        {
            StopLobbyMusic();
            AuthManager.Logout();
            if (!string.IsNullOrEmpty(authSceneName))
                SceneManager.LoadScene(authSceneName, LoadSceneMode.Single);
        }

        public void OpenSkillsScene()
        {
            var modals = lobbyModals != null ? lobbyModals : LobbyModalsHost.Instance;
            if (modals != null)
            {
                modals.OpenSkills();
                return;
            }

            OpenSkillsSceneLegacy();
        }

        private void OpenSkillsSceneLegacy()
        {
            StopLobbyMusic();
            if (!string.IsNullOrEmpty(skillsSceneName))
                SceneManager.LoadScene(skillsSceneName, LoadSceneMode.Single);
        }

        public void OpenLobbyScene()
        {
            if (!string.IsNullOrEmpty(SceneManager.GetActiveScene().name) && SceneManager.GetActiveScene().name == lobbySceneName)
                return;
            if (!string.IsNullOrEmpty(lobbySceneName))
                SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
        }

        private void PlayLobbyMusicIfConfigured()
        {
            if (lobbyBackgroundMusic == null)
                return;

            var src = lobbyMusicSource;
            if (src == null)
            {
                src = GetComponent<AudioSource>();
                if (src == null)
                    src = gameObject.AddComponent<AudioSource>();
                lobbyMusicSource = src;
            }

            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f;
            src.clip = lobbyBackgroundMusic;
            src.volume = lobbyMusicVolume;
            src.Play();
        }

        private void StopLobbyMusic()
        {
            if (lobbyMusicSource == null)
                return;
            lobbyMusicSource.Stop();
            lobbyMusicSource.clip = null;
        }
    }
}
