using Newtonsoft.Json.Linq;

namespace TapBrawl.Network
{
    public static class ApiErrorHelper
    {
        public const string SuspiciousStatsCode = "SUSPICIOUS_STATS";

        public static bool TryGetErrorCode(string? errorBody, out string code)
        {
            code = string.Empty;
            if (string.IsNullOrWhiteSpace(errorBody))
                return false;

            try
            {
                var obj = JObject.Parse(errorBody);
                code = obj["code"]?.ToString() ?? string.Empty;
                return !string.IsNullOrEmpty(code);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsSuspiciousStats(string? errorBody) =>
            TryGetErrorCode(errorBody, out var code) && code == SuspiciousStatsCode;
    }
}
