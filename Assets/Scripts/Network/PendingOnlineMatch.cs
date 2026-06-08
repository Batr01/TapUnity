using System;
using TapBrawl.Models;

namespace TapBrawl.Network
{
    /// <summary>
    /// Одноразовая передача параметров онлайн-матча из Lobby в сцену Match.
    /// </summary>
    public static class PendingOnlineMatch
    {
        private static bool _hasPending;
        private static Guid _matchId;
        private static uint _seed;
        private static int _durationSec;
        private static string _opponentUsername = string.Empty;
        private static Guid _opponentPlayerId;
        private static int _yourSlot = 1;
        private static bool _hasBotOpponent;
        private static string? _botDifficulty;

        public static bool HasPending => _hasPending;

        public static void SetFromDto(MatchFoundDto dto)
        {
            _hasPending = true;
            _matchId = dto.MatchId;
            _seed = NormalizeSeed(dto.Seed);
            _durationSec = dto.DurationSec > 0 ? dto.DurationSec : 60;
            _opponentUsername = string.IsNullOrEmpty(dto.OpponentUsername) ? "?" : dto.OpponentUsername;
            _opponentPlayerId = dto.OpponentPlayerId;
            _yourSlot = dto.YourSlot is 1 or 2 ? dto.YourSlot : 1;
            _hasBotOpponent = dto.HasBotOpponent;
            _botDifficulty = dto.BotDifficulty;
        }

        public static bool TryConsume(out OnlineMatchParams p)
        {
            p = default;
            if (!_hasPending)
                return false;

            p = new OnlineMatchParams(
                _matchId, _seed, _durationSec, _opponentUsername, _opponentPlayerId, _yourSlot,
                _hasBotOpponent, _botDifficulty);
            _hasPending = false;
            return true;
        }

        public static void Clear()
        {
            _hasPending = false;
        }

        private static uint NormalizeSeed(uint seed) => seed == 0 ? 1u : seed;
    }

    public readonly struct OnlineMatchParams
    {
        public Guid MatchId { get; }
        public uint Seed { get; }
        public int DurationSec { get; }
        public string OpponentUsername { get; }
        public Guid OpponentPlayerId { get; }
        public int YourSlot { get; }
        public bool HasBotOpponent { get; }
        public string? BotDifficulty { get; }

        public OnlineMatchParams(
            Guid matchId,
            uint seed,
            int durationSec,
            string opponentUsername,
            Guid opponentPlayerId,
            int yourSlot,
            bool hasBotOpponent = false,
            string? botDifficulty = null)
        {
            MatchId = matchId;
            Seed = seed;
            DurationSec = durationSec;
            OpponentUsername = opponentUsername;
            OpponentPlayerId = opponentPlayerId;
            YourSlot = yourSlot is 1 or 2 ? yourSlot : 1;
            HasBotOpponent = hasBotOpponent;
            BotDifficulty = botDifficulty;
        }
    }
}
