using System;
using System.Threading;
using System.Threading.Tasks;
using TapBrawl.Models;
using TapBrawl.Network;
using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>Popup входящего вызова друга на дружеский 1v1.</summary>
    public sealed class FriendChallengeOverlay : MonoBehaviour
    {
        public static FriendChallengeOverlay? Instance { get; private set; }

        [SerializeField] private GameObject? root;
        [SerializeField] private Text? titleText;
        [SerializeField] private Text? statusText;
        [SerializeField] private Button? acceptButton;
        [SerializeField] private Button? declineButton;

        private FriendChallengeReceivedDto? _pending;
        private bool _busy;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            TryAutoWire();
            if (acceptButton != null)
                acceptButton.onClick.AddListener(() => _ = AcceptAsync());
            if (declineButton != null)
                declineButton.onClick.AddListener(() => _ = DeclineAsync());
            Hide();
        }

        private void OnEnable()
        {
            var host = LobbyHubHost.Instance;
            if (host != null)
            {
                host.FriendChallengeReceived -= OnChallengeReceived;
                host.FriendChallengeReceived += OnChallengeReceived;
                host.FriendChallengeDeclined -= OnChallengeDeclined;
                host.FriendChallengeDeclined += OnChallengeDeclined;
            }
        }

        private void OnDisable()
        {
            var host = LobbyHubHost.Instance;
            if (host != null)
            {
                host.FriendChallengeReceived -= OnChallengeReceived;
                host.FriendChallengeDeclined -= OnChallengeDeclined;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void TryAutoWire()
        {
            if (root == null)
                root = transform.Find("Panel")?.gameObject ?? gameObject;
            if (titleText == null)
                titleText = transform.Find("Panel/Title Text")?.GetComponent<Text>();
            if (statusText == null)
                statusText = transform.Find("Panel/Status Text")?.GetComponent<Text>();
            if (acceptButton == null)
                acceptButton = transform.Find("Panel/Accept Button")?.GetComponent<Button>();
            if (declineButton == null)
                declineButton = transform.Find("Panel/Decline Button")?.GetComponent<Button>();
        }

        private void OnChallengeReceived(FriendChallengeReceivedDto dto)
        {
            _pending = dto;
            if (titleText != null)
                titleText.text = $"{dto.FromUsername} вызывает на бой";
            SetStatus(string.Empty);
            Show();
        }

        private void OnChallengeDeclined(FriendChallengeDeclinedDto dto)
        {
            if (_pending != null && _pending.ChallengeId == dto.ChallengeId)
                Hide();
        }

        private async Task AcceptAsync()
        {
            if (_pending == null || _busy)
                return;

            var host = LobbyHubHost.Instance;
            if (host == null)
            {
                SetStatus("Нет подключения к серверу.");
                return;
            }

            _busy = true;
            SetStatus("Принимаем…");
            try
            {
                await host.AcceptFriendChallengeAsync(_pending.ChallengeId, CancellationToken.None);
                Hide();
            }
            catch (Exception ex)
            {
                SetStatus("Ошибка: " + ex.Message);
            }
            finally
            {
                _busy = false;
            }
        }

        private async Task DeclineAsync()
        {
            if (_pending == null || _busy)
                return;

            var host = LobbyHubHost.Instance;
            if (host == null)
            {
                Hide();
                return;
            }

            _busy = true;
            try
            {
                await host.DeclineFriendChallengeAsync(_pending.ChallengeId, CancellationToken.None);
            }
            catch
            {
                // ignored
            }
            finally
            {
                _busy = false;
                Hide();
            }
        }

        private void Show()
        {
            if (root != null)
                root.SetActive(true);
            gameObject.SetActive(true);
        }

        private void Hide()
        {
            _pending = null;
            if (root != null)
                root.SetActive(false);
            gameObject.SetActive(false);
        }

        private void SetStatus(string msg)
        {
            if (statusText != null)
                statusText.text = msg;
        }
    }
}
