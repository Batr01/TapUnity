using TapBrawl.Models;

namespace TapBrawl.Network
{
    /// <summary>
    /// Результат, пришедший через SignalR (MatchResultReady) во время пост-игровой фазы.
    /// Хранится статически между сценами Match → Result.
    /// </summary>
    public static class PendingMatchFinalResult
    {
        private static MatchResultResponseDto? _result;

        public static void Set(MatchResultResponseDto result) => _result = result;

        /// <summary>Вернуть и очистить (consume-once).</summary>
        public static MatchResultResponseDto? TryConsume()
        {
            var r = _result;
            _result = null;
            return r;
        }

        public static bool HasResult => _result != null;

        public static void Clear() => _result = null;
    }
}
