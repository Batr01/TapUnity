using System;

namespace TapBrawl.Network
{
    /// <summary>
    /// Передача локальной статистики из сцены Match в сцену Result (до вызова API).
    /// </summary>
    public static class PendingMatchResult
    {
        private static bool _has;
        private static PendingMatchResultPayload _payload;

        public static void Set(in PendingMatchResultPayload payload)
        {
            _has = true;
            _payload = payload;
        }

        public static bool TryConsume(out PendingMatchResultPayload payload)
        {
            payload = default;
            if (!_has)
                return false;
            payload = _payload;
            _has = false;
            return true;
        }

        public static void Clear() => _has = false;
    }

    /// <summary>Локальные итоги онлайн-матча для отправки на сервер (submit-my-stats).</summary>
    public readonly struct PendingMatchResultPayload
    {
        public Guid MatchId { get; }
        public Guid MyPlayerId { get; }
        public string OpponentUsername { get; }
        public int MyScore { get; }
        public int MyTaps { get; }
        public int MyMisses { get; }
        public int DurationSec { get; }

        public PendingMatchResultPayload(
            Guid matchId,
            Guid myPlayerId,
            string opponentUsername,
            int myScore,
            int myTaps,
            int myMisses,
            int durationSec)
        {
            MatchId = matchId;
            MyPlayerId = myPlayerId;
            OpponentUsername = string.IsNullOrEmpty(opponentUsername) ? "?" : opponentUsername;
            MyScore = myScore;
            MyTaps = myTaps;
            MyMisses = myMisses;
            DurationSec = durationSec;
        }
    }
}
