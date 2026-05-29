using UnityEngine;

namespace TapBrawl.Network
{
    /// <summary>
    /// Сохраняет <see cref="MatchHubClient"/> между сценами (Lobby → Match) для тапов/синхронизации в фазе 3.
    /// </summary>
    public sealed class MatchConnectionHolder : MonoBehaviour
    {
        public static MatchConnectionHolder? Instance { get; private set; }

        public MatchHubClient? Hub { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static MatchConnectionHolder Ensure()
        {
            if (Instance != null)
                return Instance;

            var go = new GameObject(nameof(MatchConnectionHolder));
            return go.AddComponent<MatchConnectionHolder>();
        }

        public void AttachHub(MatchHubClient hub)
        {
            if (hub == null)
                return;
            if (Hub != null && !ReferenceEquals(Hub, hub))
            {
                try
                {
                    Hub.Dispose();
                }
                catch
                {
                    // ignored
                }
            }

            Hub = hub;
        }

        public void ReleaseHub()
        {
            if (Hub == null)
                return;
            try
            {
                Hub.Dispose();
            }
            catch
            {
                // ignored
            }

            Hub = null;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
