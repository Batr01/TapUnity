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
    /// Кнопка «Искать матч 1v1»: SignalR, очередь, затем загрузка сцены Match с параметрами матча.
    /// Нужен пакет Microsoft.AspNetCore.SignalR.Client (см. Docs/Unity/06-SIGNALR-CLIENT-UNITY.md).
    /// </summary>
    public sealed class LobbyMatchmakingController : MonoBehaviour
    {
        [SerializeField] private BackendConfig backendConfig = null!;
        [SerializeField] private string authSceneName = "Auth";
        [SerializeField] private string matchSceneName = "Match";
        [SerializeField] private Button? findMatchButton;
        [SerializeField] private Text? statusText;

        private MatchHubClient? _hub;
        private bool _leavingToMatchScene;
        private SynchronizationContext? _mainContext;

        private void Awake()
        {
            _mainContext = SynchronizationContext.Current;
            if (findMatchButton != null)
                findMatchButton.onClick.AddListener(FindMatchClickedAsync);
        }

        private void OnDestroy()
        {
            if (_leavingToMatchScene)
                return;
            _ = DisconnectSafeAsync();
        }

        private async void FindMatchClickedAsync()
        {
            if (backendConfig == null)
            {
                SetStatus("BackendConfig не назначен.");
                return;
            }

            // Снимок контекста с UI-потока: MatchFound с SignalR может прийти на другом потоке — без Post() AttachHub/LoadScene недопустимы.
            var uiSyncContext = SynchronizationContext.Current ?? _mainContext;

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

                // JWT может быть ещё валидным, а строка Player в БД уже удалена (сброс БД) → на сервере FirstAsync и «Sequence contains no matching element».
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

                var sessionFinal = AuthContext.Current;
                if (sessionFinal == null || string.IsNullOrWhiteSpace(sessionFinal.AccessToken))
                {
                    SetStatus("Нет сессии после входа.");
                    return;
                }

                SetStatus("Подключение к матчмейкингу...");
                _hub?.Dispose();
                _hub = new MatchHubClient();
                _hub.QueueJoined += OnQueueJoined;
                _hub.MatchFound += dto => OnMatchFound(dto, uiSyncContext);

                await _hub.ConnectAsync(backendConfig.BaseUrl, sessionFinal.AccessToken, CancellationToken.None);
                SetStatus("В очереди…");
                await _hub.JoinQueue1v1Async(CancellationToken.None);
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

        private void OnMatchFound(MatchFoundDto dto, SynchronizationContext? uiSyncContext)
        {
            void Load()
            {
                if (_hub == null)
                {
                    SetStatus("Ошибка: матч найден, но соединение с хабом потеряно. Начните поиск заново.");
                    Debug.LogError("[Lobby MM] MatchFound при _hub == null — сцена Match не загружена, иначе скиллы по сети не работали бы.");
                    return;
                }

                SetStatus($"Матч! {dto.OpponentUsername}, загрузка…");
                Debug.Log($"[Match] id={dto.MatchId} seed={dto.Seed} duration={dto.DurationSec}s");

                PendingOnlineMatch.SetFromDto(dto);

                var holder = MatchConnectionHolder.Ensure();
                holder.AttachHub(_hub);
                _hub = null;

                _leavingToMatchScene = true;

                if (!string.IsNullOrEmpty(matchSceneName))
                    SceneManager.LoadScene(matchSceneName, LoadSceneMode.Single);
            }

            var ctx = uiSyncContext ?? _mainContext;
            if (ctx != null)
                ctx.Post(_ => Load(), null);
            else
                Load();
        }

        private async Task DisconnectSafeAsync()
        {
            if (_hub == null)
                return;
            try
            {
                await _hub.LeaveQueueAsync();
                await _hub.DisconnectAsync();
            }
            catch
            {
                // ignored
            }
            _hub = null;
        }

        private void SetStatus(string msg)
        {
            if (statusText != null)
                statusText.text = msg;
            Debug.Log("[Lobby MM] " + msg);
        }
    }
}
