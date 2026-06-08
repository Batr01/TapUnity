using System;
using System.Threading;
using System.Threading.Tasks;
using TapBrawl.Models;
using TapBrawl.UI;
using UnityEngine;
using SynchronizationContext = System.Threading.SynchronizationContext;

namespace TapBrawl.Network
{
    /// <summary>Фоновое SignalR-подключение в Lobby: presence, вызовы друзей, матчмейкинг.</summary>
    public sealed class LobbyHubHost : MonoBehaviour
    {
        public static LobbyHubHost? Instance { get; private set; }

        [SerializeField] private BackendConfig backendConfig = null!;
        [SerializeField] private string authSceneName = "Auth";
        [SerializeField] private string matchSceneName = "Match";

        private MatchHubClient? _hub;
        private SynchronizationContext? _mainContext;
        private bool _leavingToMatchScene;
        private bool _connecting;

        public MatchHubClient? Hub => _hub;
        public bool IsConnected => _hub?.IsConnected ?? false;

        public event Action<FriendChallengeReceivedDto>? FriendChallengeReceived;
        public event Action<FriendChallengeDeclinedDto>? FriendChallengeDeclined;
        public event Action? QueueJoined;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _mainContext = SynchronizationContext.Current;
            backendConfig = BackendConfigLocator.Resolve(backendConfig);
        }

        private void Start() => _ = ConnectAsync();

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (_leavingToMatchScene)
                return;

            _ = DisconnectSafeAsync();
        }

        public async Task EnsureConnectedAsync(CancellationToken ct = default)
        {
            if (_hub?.IsConnected == true)
                return;

            await ConnectAsync(ct);
        }

        public async Task JoinQueue1v1Async(CancellationToken ct = default)
        {
            await EnsureConnectedAsync(ct);
            if (_hub == null)
                throw new InvalidOperationException("Hub не подключён.");
            await _hub.JoinQueue1v1Async(ct);
        }

        public async Task LeaveQueueAsync(CancellationToken ct = default)
        {
            if (_hub == null)
                return;
            await _hub.LeaveQueueAsync(ct);
        }

        public async Task SendFriendChallengeAsync(Guid friendPlayerId, CancellationToken ct = default)
        {
            await EnsureConnectedAsync(ct);
            if (_hub == null)
                throw new InvalidOperationException("Hub не подключён.");
            await _hub.SendFriendChallengeAsync(friendPlayerId, ct);
        }

        public async Task AcceptFriendChallengeAsync(Guid challengeId, CancellationToken ct = default)
        {
            await EnsureConnectedAsync(ct);
            if (_hub == null)
                throw new InvalidOperationException("Hub не подключён.");
            await _hub.AcceptFriendChallengeAsync(challengeId, ct);
        }

        public async Task DeclineFriendChallengeAsync(Guid challengeId, CancellationToken ct = default)
        {
            await EnsureConnectedAsync(ct);
            if (_hub == null)
                throw new InvalidOperationException("Hub не подключён.");
            await _hub.DeclineFriendChallengeAsync(challengeId, ct);
        }

        private async Task ConnectAsync(CancellationToken ct = default)
        {
            if (_connecting || _leavingToMatchScene)
                return;

            backendConfig = BackendConfigLocator.Resolve(backendConfig);
            if (backendConfig == null)
            {
                Debug.LogError("[LobbyHubHost] BackendConfig не назначен.");
                return;
            }

            _connecting = true;
            try
            {
                var api = new ApiClient(backendConfig);
                var auth = new AuthManager(api);
                if (!await auth.TryRestoreSessionAsync(ct).ConfigureAwait(true))
                    return;

                var session = AuthContext.Current;
                if (session == null || string.IsNullOrWhiteSpace(session.AccessToken))
                    return;

                _hub?.Dispose();
                _hub = new MatchHubClient();
                _hub.QueueJoined += () => QueueJoined?.Invoke();
                _hub.MatchFound += dto => OnMatchFound(dto);
                _hub.FriendChallengeReceived += dto => FriendChallengeReceived?.Invoke(dto);
                _hub.FriendChallengeDeclined += dto => FriendChallengeDeclined?.Invoke(dto);

                await _hub.ConnectAsync(backendConfig.BaseUrl, session.AccessToken, ct);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[LobbyHubHost] Connect failed: " + ex.Message);
            }
            finally
            {
                _connecting = false;
            }
        }

        private void OnMatchFound(MatchFoundDto dto)
        {
            LobbyMatchSceneLoader.LoadFromMatchFound(
                dto,
                _hub,
                matchSceneName,
                _mainContext,
                onLeavingToMatch: () =>
                {
                    _hub = null;
                    _leavingToMatchScene = true;
                });
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
    }
}
