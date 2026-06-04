using System;
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
        [SerializeField] private Text? coinsText;
        [SerializeField] private Text? rankText;
        [SerializeField] private Text? tierText;

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
            if (logoutButton != null)
                logoutButton.onClick.AddListener(OnLogoutClicked);
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
            if (backendConfig != null)
            {
                _ = RefreshProfileFromServerAsync();
                _ = PrefetchSkillsAsync(s);
            }
        }

        private async Task RefreshProfileFromServerAsync()
        {
            if (backendConfig == null)
                return;

            try
            {
                var api = new ApiClient(backendConfig);
                var auth = new AuthManager(api);
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

                await PrefetchSkillsCoreAsync(api, session, CancellationToken.None).ConfigureAwait(true);

                var me = await api.PlayersMeAsync(session.AccessToken, CancellationToken.None);
                if (!me.Success || me.Data == null)
                {
                    Debug.LogWarning($"[Lobby] GET players/me: HTTP {me.StatusCode} {me.ErrorBody}");
                    return;
                }

                session.Player = me.Data;
                AuthContext.Current = session;
                AuthStorage.Save(session);
                ApplyProfileUi(session.Player);
                await PrefetchSkillsCoreAsync(api, session, CancellationToken.None).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Lobby] Профиль с сервера: " + ex.Message);
            }
        }

        private async Task PrefetchSkillsAsync(AuthSession session)
        {
            if (backendConfig == null)
                return;
            try
            {
                var api = new ApiClient(backendConfig);
                await PrefetchSkillsCoreAsync(api, session, CancellationToken.None).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Lobby] Скиллы: " + ex.Message);
                PlayerSkillsRuntimeState.ApplyOfflineMaxDefaults();
            }
        }

        private static async Task PrefetchSkillsCoreAsync(ApiClient api, AuthSession session, CancellationToken ct)
        {
            var skills = await api.PlayersMeSkillsAsync(session.AccessToken, ct).ConfigureAwait(true);
            if (skills.Success && skills.Data != null)
            {
                PlayerSkillsRuntimeState.ApplyFromServer(skills.Data);
                session.Player.Coins = skills.Data.Coins;
                AuthContext.Current = session;
                AuthStorage.Save(session);
            }
            else
                PlayerSkillsRuntimeState.ApplyOfflineMaxDefaults();
        }

        private void ApplyProfileUi(PlayerProfileDto p)
        {
            if (usernameText != null)
                usernameText.text = p.Username;
            if (coinsText != null)
                coinsText.text = $"Монеты: {p.Coins}";
            if (rankText != null)
                rankText.text = $"Рейтинг: {p.RankPoints}";
            if (tierText != null)
                tierText.text = $"Тир: {p.Tier}";
        }

        private void OnDestroy() => StopLobbyMusic();

        private void OnLogoutClicked()
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
