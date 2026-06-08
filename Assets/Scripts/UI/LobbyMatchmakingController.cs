using System;
using System.Threading;
using System.Threading.Tasks;
using TapBrawl.Models;
using TapBrawl.Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SynchronizationContext = System.Threading.SynchronizationContext;

namespace TapBrawl.UI
{
    /// <summary>
    /// Кнопка «Искать матч 1v1»: SignalR через <see cref="LobbyHubHost"/>, очередь, затем Match.
    /// </summary>
    public sealed class LobbyMatchmakingController : MonoBehaviour
    {
        [SerializeField] private BackendConfig backendConfig = null!;
        [SerializeField] private string authSceneName = "Auth";
        [SerializeField] private Button? findMatchButton;
        [SerializeField] private Text? statusText;

        private LobbyHubHost? _hubHost;
        private SynchronizationContext? _mainContext;

        private void Awake()
        {
            _mainContext = SynchronizationContext.Current;
            if (findMatchButton != null)
                findMatchButton.onClick.AddListener(FindMatchClickedAsync);
        }

        private void Start()
        {
            _hubHost = LobbyHubHost.Instance ?? GetComponent<LobbyHubHost>() ?? gameObject.AddComponent<LobbyHubHost>();
            if (_hubHost != null)
                _hubHost.QueueJoined += OnQueueJoined;

            if (LobbyAutoSearch.RequestAutoSearch)
            {
                LobbyAutoSearch.RequestAutoSearch = false;
                FindMatchClickedAsync();
            }
        }

        private void OnDestroy()
        {
            if (_hubHost != null)
                _hubHost.QueueJoined -= OnQueueJoined;
        }

        private async void FindMatchClickedAsync()
        {
            if (backendConfig == null)
            {
                SetStatus("BackendConfig не назначен.");
                return;
            }

            try
            {
                SetStatus("Проверка сессии…");
                var api = new ApiClient(backendConfig);
                var auth = new AuthManager(api);
                if (!await auth.TryRestoreSessionAsync(CancellationToken.None).ConfigureAwait(true))
                {
                    SetStatus("Нужен вход.");
                    if (!string.IsNullOrEmpty(authSceneName))
                        SceneManager.LoadScene(authSceneName, LoadSceneMode.Single);
                    return;
                }

                for (var attempt = 0; attempt < 2; attempt++)
                {
                    var session = AuthContext.Current;
                    if (session == null || string.IsNullOrWhiteSpace(session.AccessToken))
                    {
                        SetStatus("Нет сессии. Запустите Boot или проверьте API.");
                        return;
                    }

                    var me = await api.PlayersMeAsync(session.AccessToken, CancellationToken.None);
                    if (me.Success && me.Data != null)
                        break;

                    if (me.StatusCode == 401 && attempt == 0)
                    {
                        AuthStorage.Clear();
                        AuthContext.Current = null;
                        if (!await auth.TryRestoreSessionAsync(CancellationToken.None).ConfigureAwait(true))
                        {
                            if (!string.IsNullOrEmpty(authSceneName))
                                SceneManager.LoadScene(authSceneName, LoadSceneMode.Single);
                            return;
                        }

                        continue;
                    }

                    SetStatus($"Сессия недействительна (HTTP {me.StatusCode}). Перезапустите игру или очистите данные приложения.");
                    return;
                }

                _hubHost = LobbyHubHost.Instance ?? GetComponent<LobbyHubHost>() ?? gameObject.AddComponent<LobbyHubHost>();
                SetStatus("Подключение к матчмейкингу...");
                await _hubHost.EnsureConnectedAsync(CancellationToken.None);
                SetStatus("В очереди…");
                await _hubHost.JoinQueue1v1Async(CancellationToken.None);
            }
            catch (Exception ex)
            {
                var detail = ex.GetType().FullName + ": " + ex.Message;
                for (var inner = ex.InnerException; inner != null; inner = inner.InnerException)
                    detail += " → " + inner.GetType().Name + ": " + inner.Message;
                SetStatus("Ошибка: " + detail);
                Debug.LogError("[Lobby MM] " + ex);
                Debug.LogException(ex);
            }
        }

        private void OnQueueJoined() => SetStatus("В очереди (ожидание соперника)…");

        private void SetStatus(string msg)
        {
            if (statusText != null)
                statusText.text = msg;
            Debug.Log("[Lobby MM] " + msg);
        }
    }
}
