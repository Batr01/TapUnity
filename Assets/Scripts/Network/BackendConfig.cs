using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace TapBrawl.Network
{
    /// <summary>
    /// Базовый URL API. Создайте ассет: Create → Tap Brawl → Backend Config.
    /// </summary>
    [CreateAssetMenu(fileName = "BackendConfig", menuName = "Tap Brawl/Backend Config")]
    public sealed class BackendConfig : ScriptableObject
    {
        [Tooltip("HTTP для localhost/LAN. Используется в Editor и Development Build.")]
        [FormerlySerializedAs("baseUrl")]
        [SerializeField]
        private string developmentBaseUrl = "http://localhost:5088";

        [Tooltip("HTTPS endpoint для Release-сборок.")]
        [SerializeField]
        private string productionBaseUrl = "https://api.tapbrawl.com";

        [Tooltip("Web OAuth Client ID из Google Console (обязателен для id_token на Android).")]
        [SerializeField]
        private string googleWebClientId = string.Empty;

        [Tooltip("iOS OAuth Client ID из Google Console (опционально, для нативного iOS Google Sign-In).")]
        [SerializeField]
        private string googleIosClientId = string.Empty;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // TODO: убрать после теста LoadingOverlay — задержка перед каждым HTTP-запросом (0 = выкл).
        [Header("TODO: убрать после теста загрузки")]
        [Tooltip("Искусственная задержка перед каждым HTTP-запросом, мс. Поставьте 2000–5000 для проверки оверлея.")]
        [SerializeField]
        private int devSimulateRequestDelayMs = 3000;

        public int DevSimulateRequestDelayMs => Mathf.Max(0, devSimulateRequestDelayMs);
#endif

        public string BaseUrl
        {
            get
            {
                var url = ResolveBaseUrl();
                if (!IsUrlAllowed(url, out var reason))
                {
                    Debug.LogError($"BackendConfig: {reason} URL={url}");
                    return GetFallbackUrl();
                }

                return url;
            }
        }

        public string GoogleWebClientId => googleWebClientId?.Trim() ?? string.Empty;

        public string GoogleIosClientId => googleIosClientId?.Trim() ?? string.Empty;

        private string ResolveBaseUrl()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (string.IsNullOrWhiteSpace(developmentBaseUrl))
                return "http://localhost:5088";
            return developmentBaseUrl.Trim().TrimEnd('/');
#else
            if (string.IsNullOrWhiteSpace(productionBaseUrl))
                return string.Empty;
            return productionBaseUrl.Trim().TrimEnd('/');
#endif
        }

        private static string GetFallbackUrl()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return "http://localhost:5088";
#else
            return "https://api.tapbrawl.com";
#endif
        }

        private static bool IsUrlAllowed(string url, out string reason)
        {
            reason = string.Empty;
            if (string.IsNullOrWhiteSpace(url))
            {
                reason = "Base URL is empty.";
                return false;
            }

            if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                return true;

            reason = "Development URL must start with http:// or https://.";
            return false;
#else
            reason = "Release builds require HTTPS base URL.";
            return false;
#endif
        }
    }
}
