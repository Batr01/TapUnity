using System;
using Newtonsoft.Json;

namespace TapBrawl.Models
{
    [Serializable]
    public sealed class SubmitMyMatchStatsBody
    {
        [JsonProperty("score")] public int Score { get; set; }
        [JsonProperty("taps")] public int Taps { get; set; }
        [JsonProperty("misses")] public int Misses { get; set; }
    }

    public sealed class SubmitMatchStatsResponseDto
    {
        [JsonProperty("complete")] public bool Complete { get; set; }
        [JsonProperty("result")] public MatchResultResponseDto? Result { get; set; }
    }

    public sealed class MatchResultResponseDto
    {
        [JsonProperty("matchId")] public Guid MatchId { get; set; }
        [JsonProperty("winnerPlayerId")] public Guid WinnerPlayerId { get; set; }
        [JsonProperty("totalScore")] public int TotalScore { get; set; }
        [JsonProperty("player1")] public MatchPlayerResultDto Player1 { get; set; } = new();
        [JsonProperty("player2")] public MatchPlayerResultDto Player2 { get; set; } = new();
        [JsonProperty("scoreDifference")] public int ScoreDifference { get; set; }
        [JsonProperty("durationSec")] public int DurationSec { get; set; }
    }

    public sealed class MatchPlayerResultDto
    {
        [JsonProperty("playerId")] public Guid PlayerId { get; set; }
        [JsonProperty("username")] public string Username { get; set; } = string.Empty;
        [JsonProperty("score")] public int Score { get; set; }
        [JsonProperty("taps")] public int Taps { get; set; }
        [JsonProperty("misses")] public int Misses { get; set; }
        [JsonProperty("accuracyPercent")] public double AccuracyPercent { get; set; }
        [JsonProperty("tapsPerSecond")] public double TapsPerSecond { get; set; }
        [JsonProperty("isWinner")] public bool IsWinner { get; set; }
    }
}
