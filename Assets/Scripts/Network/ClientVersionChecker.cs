using System.Threading;
using System.Threading.Tasks;
using TapBrawl.Models;
using TapBrawl.Utils;
using UnityEngine;

namespace TapBrawl.Network
{
    public readonly struct ClientVersionCheckResult
    {
        public bool UpdateRequired { get; }
        public string CurrentVersion { get; }
        public string MinSupportedVersion { get; }
        public string StoreUrl { get; }

        public ClientVersionCheckResult(
            bool updateRequired,
            string currentVersion,
            string minSupportedVersion,
            string storeUrl)
        {
            UpdateRequired = updateRequired;
            CurrentVersion = currentVersion;
            MinSupportedVersion = minSupportedVersion;
            StoreUrl = storeUrl;
        }
    }

    public static class ClientVersionChecker
    {
        public static async Task<ClientVersionCheckResult?> TryCheckAsync(ApiClient api, CancellationToken ct = default)
        {
            var result = await api.GetConfigAsync(ct).ConfigureAwait(true);
            if (!result.Success || result.Data == null)
            {
                Debug.LogWarning("[ClientVersionChecker] Config недоступен, пропускаем проверку версии.");
                return null;
            }

            return Evaluate(result.Data);
        }

        public static ClientVersionCheckResult Evaluate(ClientConfigDto config)
        {
            var currentVersion = Application.version;
            var minVersion = config.MinSupportedVersion ?? string.Empty;
            var updateRequired = IsUpdateRequired(currentVersion, minVersion);
            var storeUrl = ResolveStoreUrl(config);

            return new ClientVersionCheckResult(updateRequired, currentVersion, minVersion, storeUrl);
        }

        public static bool IsUpdateRequired(string currentVersion, string minSupportedVersion)
        {
            if (!SemVer.TryParse(currentVersion, out var current))
            {
                Debug.LogWarning($"[ClientVersionChecker] Некорректная текущая версия: {currentVersion}");
                return false;
            }

            if (!SemVer.TryParse(minSupportedVersion, out var min))
            {
                Debug.LogWarning($"[ClientVersionChecker] Некорректная minSupportedVersion: {minSupportedVersion}");
                return false;
            }

            return current < min;
        }

        private static string ResolveStoreUrl(ClientConfigDto config)
        {
#if UNITY_IOS
            return config.IosStoreUrl ?? string.Empty;
#elif UNITY_ANDROID
            return config.AndroidStoreUrl ?? string.Empty;
#else
            return !string.IsNullOrWhiteSpace(config.AndroidStoreUrl)
                ? config.AndroidStoreUrl
                : config.IosStoreUrl ?? string.Empty;
#endif
        }
    }
}
