using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TapBrawl.Models;
using TapBrawl.Network;
using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    public sealed class FriendsModalView : MonoBehaviour
    {
        private enum Tab
        {
            Friends = 0,
            Search = 1,
            Requests = 2,
        }

        [SerializeField] private BackendConfig? backendConfig;
        [SerializeField] private Text? titleText;
        [SerializeField] private Text? statusText;
        [SerializeField] private Text? countText;
        [SerializeField] private Toggle[]? tabToggles;
        [SerializeField] private GameObject? friendsSection;
        [SerializeField] private GameObject? searchSection;
        [SerializeField] private GameObject? requestsSection;
        [SerializeField] private Transform? friendsContainer;
        [SerializeField] private Transform? searchContainer;
        [SerializeField] private Transform? requestsContainer;
        [SerializeField] private InputField? searchInput;
        [SerializeField] private Button? searchButton;
        [SerializeField] private Button? friendRowPrefab;
        [SerializeField] private Button? searchRowPrefab;
        [SerializeField] private Button? requestRowPrefab;

        private readonly List<GameObject> _spawnedFriends = new();
        private readonly List<GameObject> _spawnedSearch = new();
        private readonly List<GameObject> _spawnedRequests = new();
        private Tab _currentTab = Tab.Friends;
        private bool _loading;

        private void Awake()
        {
            backendConfig = BackendConfigLocator.Resolve(backendConfig);
            TryAutoWire();
            WireTabs();
            if (searchButton != null)
                searchButton.onClick.AddListener(() => _ = SearchAsync());
        }

        private void OnEnable() => _ = RefreshCurrentTabAsync();

        private void WireTabs()
        {
            if (tabToggles == null || tabToggles.Length == 0)
                return;

            for (var i = 0; i < tabToggles.Length; i++)
            {
                var index = i;
                var toggle = tabToggles[i];
                if (toggle == null)
                    continue;
                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener(on =>
                {
                    if (on)
                        SwitchTab((Tab)index);
                });
            }
        }

        public void TryAutoWire()
        {
            if (titleText == null)
                titleText = transform.Find("Panel/Header/Title Text")?.GetComponent<Text>();
            if (statusText == null)
                statusText = transform.Find("Panel/Status Text")?.GetComponent<Text>();
            if (countText == null)
                countText = transform.Find("Panel/Count Text")?.GetComponent<Text>();
            if (friendsSection == null)
                friendsSection = transform.Find("Panel/Friends Section")?.gameObject;
            if (searchSection == null)
                searchSection = transform.Find("Panel/Search Section")?.gameObject;
            if (requestsSection == null)
                requestsSection = transform.Find("Panel/Requests Section")?.gameObject;
            if (friendsContainer == null)
                friendsContainer = transform.Find("Panel/Friends Section/Scroll/Viewport/Content");
            if (searchContainer == null)
                searchContainer = transform.Find("Panel/Search Section/Scroll/Viewport/Content");
            if (requestsContainer == null)
                requestsContainer = transform.Find("Panel/Requests Section/Scroll/Viewport/Content");
            if (searchInput == null)
                searchInput = transform.Find("Panel/Search Section/Search Input")?.GetComponent<InputField>();
            if (searchButton == null)
                searchButton = transform.Find("Panel/Search Section/Search Button")?.GetComponent<Button>();
        }

        private void SwitchTab(Tab tab)
        {
            _currentTab = tab;
            if (friendsSection != null)
                friendsSection.SetActive(tab == Tab.Friends);
            if (searchSection != null)
                searchSection.SetActive(tab == Tab.Search);
            if (requestsSection != null)
                requestsSection.SetActive(tab == Tab.Requests);
            _ = RefreshCurrentTabAsync();
        }

        private async Task RefreshCurrentTabAsync()
        {
            switch (_currentTab)
            {
                case Tab.Friends:
                    await LoadFriendsAsync();
                    break;
                case Tab.Search:
                    ClearSearch();
                    SetStatus(string.Empty);
                    break;
                case Tab.Requests:
                    await LoadRequestsAsync();
                    break;
            }
        }

        private async Task LoadFriendsAsync()
        {
            if (_loading)
                return;

            backendConfig = BackendConfigLocator.Resolve(backendConfig);
            var session = AuthContext.Current;
            if (backendConfig == null || session == null)
            {
                SetStatus("Нет сессии.");
                return;
            }

            _loading = true;
            SetStatus("Загрузка…");
            try
            {
                var api = new ApiClient(backendConfig);
                var result = await api.FriendsListAsync(session.AccessToken, CancellationToken.None);
                if (!result.Success || result.Data == null)
                {
                    SetStatus(ParseError(result.ErrorBody) ?? "Не удалось загрузить друзей.");
                    return;
                }

                if (countText != null)
                    countText.text = $"{result.Data.Count}/{result.Data.Max}";

                RebuildFriends(result.Data.Friends);
                SetStatus(result.Data.Friends.Count == 0 ? "Список друзей пуст." : string.Empty);
            }
            finally
            {
                _loading = false;
            }
        }

        private void RebuildFriends(List<FriendEntryDto> friends)
        {
            ClearSpawned(_spawnedFriends);
            if (friendsContainer == null || friendRowPrefab == null)
                return;

            foreach (var friend in friends)
            {
                var row = Instantiate(friendRowPrefab, friendsContainer);
                row.gameObject.SetActive(true);
                _spawnedFriends.Add(row.gameObject);

                var label = row.GetComponentInChildren<Text>();
                if (label != null)
                {
                    var online = friend.IsOnline ? "●" : "○";
                    label.text = $"{online} {friend.Username}  {friend.MyWins}W–{friend.MyLosses}L";
                }

                var challengeBtn = row.transform.Find("Challenge Button")?.GetComponent<Button>()
                                   ?? row;
                var removeBtn = row.transform.Find("Remove Button")?.GetComponent<Button>();
                var playerId = friend.PlayerId;
                var canChallenge = friend.IsOnline;

                if (challengeBtn != null)
                {
                    challengeBtn.interactable = canChallenge;
                    challengeBtn.onClick.RemoveAllListeners();
                    challengeBtn.onClick.AddListener(() => _ = ChallengeFriendAsync(playerId));
                }

                if (removeBtn != null)
                {
                    removeBtn.onClick.RemoveAllListeners();
                    removeBtn.onClick.AddListener(() => _ = RemoveFriendAsync(playerId));
                }
            }
        }

        private async Task ChallengeFriendAsync(Guid friendPlayerId)
        {
            var host = LobbyHubHost.Instance;
            if (host == null)
            {
                SetStatus("Нет подключения к серверу.");
                return;
            }

            SetStatus("Отправляем вызов…");
            try
            {
                await host.EnsureConnectedAsync(CancellationToken.None);
                await host.SendFriendChallengeAsync(friendPlayerId, CancellationToken.None);
                SetStatus("Вызов отправлен.");
            }
            catch (Exception ex)
            {
                SetStatus("Ошибка: " + ex.Message);
            }
        }

        private async Task RemoveFriendAsync(Guid friendPlayerId)
        {
            var session = AuthContext.Current;
            backendConfig = BackendConfigLocator.Resolve(backendConfig);
            if (session == null || backendConfig == null)
                return;

            var api = new ApiClient(backendConfig);
            var result = await api.FriendsRemoveAsync(session.AccessToken, friendPlayerId, CancellationToken.None);
            if (!result.Success)
            {
                SetStatus(ParseError(result.ErrorBody) ?? "Не удалось удалить друга.");
                return;
            }

            await LoadFriendsAsync();
        }

        private async Task SearchAsync()
        {
            if (_loading)
                return;

            var query = searchInput != null ? searchInput.text.Trim() : string.Empty;
            if (query.Length < 3)
            {
                SetStatus("Минимум 3 символа.");
                return;
            }

            backendConfig = BackendConfigLocator.Resolve(backendConfig);
            var session = AuthContext.Current;
            if (backendConfig == null || session == null)
            {
                SetStatus("Нет сессии.");
                return;
            }

            _loading = true;
            SetStatus("Поиск…");
            try
            {
                var api = new ApiClient(backendConfig);
                var result = await api.PlayersSearchAsync(session.AccessToken, query, CancellationToken.None);
                if (!result.Success || result.Data == null)
                {
                    SetStatus(ParseError(result.ErrorBody) ?? "Поиск не удался.");
                    return;
                }

                RebuildSearch(result.Data);
                SetStatus(result.Data.Count == 0 ? "Никого не найдено." : string.Empty);
            }
            finally
            {
                _loading = false;
            }
        }

        private void RebuildSearch(List<PlayerSearchResultDto> results)
        {
            ClearSpawned(_spawnedSearch);
            if (searchContainer == null || searchRowPrefab == null)
                return;

            foreach (var player in results)
            {
                var row = Instantiate(searchRowPrefab, searchContainer);
                row.gameObject.SetActive(true);
                _spawnedSearch.Add(row.gameObject);

                var label = row.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = player.Username;

                var addBtn = row.transform.Find("Add Button")?.GetComponent<Button>() ?? row;
                var targetId = player.PlayerId;
                addBtn.onClick.RemoveAllListeners();
                addBtn.onClick.AddListener(() => _ = SendRequestAsync(targetId));
            }
        }

        private async Task SendRequestAsync(Guid targetPlayerId)
        {
            var session = AuthContext.Current;
            backendConfig = BackendConfigLocator.Resolve(backendConfig);
            if (session == null || backendConfig == null)
                return;

            var api = new ApiClient(backendConfig);
            var result = await api.FriendsSendRequestAsync(session.AccessToken, targetPlayerId, CancellationToken.None);
            if (!result.Success)
            {
                SetStatus(ParseError(result.ErrorBody) ?? "Не удалось отправить заявку.");
                return;
            }

            SetStatus("Заявка отправлена.");
        }

        private async Task LoadRequestsAsync()
        {
            if (_loading)
                return;

            backendConfig = BackendConfigLocator.Resolve(backendConfig);
            var session = AuthContext.Current;
            if (backendConfig == null || session == null)
            {
                SetStatus("Нет сессии.");
                return;
            }

            _loading = true;
            SetStatus("Загрузка…");
            try
            {
                var api = new ApiClient(backendConfig);
                var result = await api.FriendsIncomingRequestsAsync(session.AccessToken, CancellationToken.None);
                if (!result.Success || result.Data == null)
                {
                    SetStatus(ParseError(result.ErrorBody) ?? "Не удалось загрузить заявки.");
                    return;
                }

                RebuildRequests(result.Data);
                SetStatus(result.Data.Count == 0 ? "Нет входящих заявок." : string.Empty);
            }
            finally
            {
                _loading = false;
            }
        }

        private void RebuildRequests(List<FriendRequestDto> requests)
        {
            ClearSpawned(_spawnedRequests);
            if (requestsContainer == null || requestRowPrefab == null)
                return;

            foreach (var request in requests)
            {
                var row = Instantiate(requestRowPrefab, requestsContainer);
                row.gameObject.SetActive(true);
                _spawnedRequests.Add(row.gameObject);

                var label = row.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = request.FromUsername;

                var acceptBtn = row.transform.Find("Accept Button")?.GetComponent<Button>();
                var declineBtn = row.transform.Find("Decline Button")?.GetComponent<Button>();
                var requestId = request.RequestId;

                if (acceptBtn != null)
                {
                    acceptBtn.onClick.RemoveAllListeners();
                    acceptBtn.onClick.AddListener(() => _ = AcceptRequestAsync(requestId));
                }

                if (declineBtn != null)
                {
                    declineBtn.onClick.RemoveAllListeners();
                    declineBtn.onClick.AddListener(() => _ = DeclineRequestAsync(requestId));
                }
            }
        }

        private async Task AcceptRequestAsync(Guid requestId)
        {
            var session = AuthContext.Current;
            backendConfig = BackendConfigLocator.Resolve(backendConfig);
            if (session == null || backendConfig == null)
                return;

            var api = new ApiClient(backendConfig);
            var result = await api.FriendsAcceptRequestAsync(session.AccessToken, requestId, CancellationToken.None);
            if (!result.Success)
            {
                SetStatus(ParseError(result.ErrorBody) ?? "Не удалось принять заявку.");
                return;
            }

            await LoadRequestsAsync();
        }

        private async Task DeclineRequestAsync(Guid requestId)
        {
            var session = AuthContext.Current;
            backendConfig = BackendConfigLocator.Resolve(backendConfig);
            if (session == null || backendConfig == null)
                return;

            var api = new ApiClient(backendConfig);
            var result = await api.FriendsDeclineRequestAsync(session.AccessToken, requestId, CancellationToken.None);
            if (!result.Success)
            {
                SetStatus(ParseError(result.ErrorBody) ?? "Не удалось отклонить заявку.");
                return;
            }

            await LoadRequestsAsync();
        }

        private void ClearSearch() => ClearSpawned(_spawnedSearch);

        private static void ClearSpawned(List<GameObject> list)
        {
            foreach (var go in list)
            {
                if (go != null)
                    Destroy(go);
            }
            list.Clear();
        }

        private void SetStatus(string msg)
        {
            if (statusText != null)
                statusText.text = msg;
        }

        private static string? ParseError(string? body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return null;
            try
            {
                var err = JsonUtility.FromJson<ApiErrorBody>(body);
                return string.IsNullOrWhiteSpace(err.error) ? body : err.error;
            }
            catch
            {
                return body;
            }
        }

        [Serializable]
        private sealed class ApiErrorBody
        {
            public string error = string.Empty;
        }
    }
}
