using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace TapBrawl.Models
{
    [Serializable]
    public sealed class PlayerDebuffSegment
    {
        [JsonProperty("skillType")] [JsonPropertyName("skillType")] public int SkillType { get; set; }
        [JsonProperty("durationSec")] [JsonPropertyName("durationSec")] public float DurationSec { get; set; }
        [JsonProperty("startOffsetSec")] [JsonPropertyName("startOffsetSec")] public float StartOffsetSec { get; set; }
    }

    [Serializable]
    public sealed class SubmitMyMatchStatsBody
    {
        [JsonProperty("score")] public int Score { get; set; }
        [JsonProperty("taps")]  public int Taps  { get; set; }
        [JsonProperty("misses")] public int Misses { get; set; }
        [JsonProperty("debuffsApplied")] [JsonPropertyName("debuffsApplied")] public List<PlayerDebuffSegment>? DebuffsApplied { get; set; }
    }

    public sealed class SubmitMatchStatsResponseDto
    {
        [JsonProperty("complete")] public bool Complete { get; set; }
        [JsonProperty("result")]   public MatchResultResponseDto? Result { get; set; }
    }

    /// <summary>Итог матча — приходит через REST и SignalR (MatchResultReady).</summary>
    [Serializable]
    public sealed class MatchResultResponseDto
    {
        [JsonProperty("matchId")]         [JsonPropertyName("matchId")]         public Guid MatchId { get; set; }
        [JsonProperty("winnerPlayerId")]  [JsonPropertyName("winnerPlayerId")]  public Guid WinnerPlayerId { get; set; }
        [JsonProperty("totalScore")]      [JsonPropertyName("totalScore")]      public int TotalScore { get; set; }
        [JsonProperty("player1")]         [JsonPropertyName("player1")]         public MatchPlayerResultDto Player1 { get; set; } = new();
        [JsonProperty("player2")]         [JsonPropertyName("player2")]         public MatchPlayerResultDto Player2 { get; set; } = new();
        [JsonProperty("scoreDifference")] [JsonPropertyName("scoreDifference")] public int ScoreDifference { get; set; }
        [JsonProperty("durationSec")]     [JsonPropertyName("durationSec")]     public int DurationSec { get; set; }
        [JsonProperty("player1Ranking")]  [JsonPropertyName("player1Ranking")]  public MatchPlayerRankingDeltaDto? Player1Ranking { get; set; }
        [JsonProperty("player2Ranking")]  [JsonPropertyName("player2Ranking")]  public MatchPlayerRankingDeltaDto? Player2Ranking { get; set; }
    }

    [Serializable]
    public sealed class MatchPlayerRankingDeltaDto
    {
        [JsonProperty("oldRankPoints")]   [JsonPropertyName("oldRankPoints")]   public int OldRankPoints { get; set; }
        [JsonProperty("newRankPoints")]   [JsonPropertyName("newRankPoints")]   public int NewRankPoints { get; set; }
        [JsonProperty("delta")]         [JsonPropertyName("delta")]         public int Delta { get; set; }
        [JsonProperty("tier")]          [JsonPropertyName("tier")]          public string Tier { get; set; } = string.Empty;
        [JsonProperty("division")]      [JsonPropertyName("division")]      public string? Division { get; set; }
        [JsonProperty("winStreakBonus")] [JsonPropertyName("winStreakBonus")] public int WinStreakBonus { get; set; }

        public string RankLabel => string.IsNullOrEmpty(Division) ? Tier : $"{Tier} {Division}";
    }

    [Serializable]
    public sealed class MatchPlayerResultDto
    {
        [JsonProperty("playerId")]        [JsonPropertyName("playerId")]        public Guid PlayerId { get; set; }
        [JsonProperty("username")]        [JsonPropertyName("username")]        public string Username { get; set; } = string.Empty;
        [JsonProperty("score")]           [JsonPropertyName("score")]           public int Score { get; set; }
        [JsonProperty("taps")]            [JsonPropertyName("taps")]            public int Taps { get; set; }
        [JsonProperty("misses")]          [JsonPropertyName("misses")]          public int Misses { get; set; }
        [JsonProperty("accuracyPercent")] [JsonPropertyName("accuracyPercent")] public double AccuracyPercent { get; set; }
        [JsonProperty("tapsPerSecond")]   [JsonPropertyName("tapsPerSecond")]   public double TapsPerSecond { get; set; }
        [JsonProperty("isWinner")]        [JsonPropertyName("isWinner")]        public bool IsWinner { get; set; }
    }
}
