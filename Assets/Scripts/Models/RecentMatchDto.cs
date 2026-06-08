using System;
using Newtonsoft.Json;

namespace TapBrawl.Models
{
    public sealed class RecentMatchDto
    {
        [JsonProperty("matchId")] public Guid MatchId { get; set; }
        [JsonProperty("finishedAt")] public DateTimeOffset FinishedAt { get; set; }
        [JsonProperty("isWinner")] public bool IsWinner { get; set; }
        [JsonProperty("myScore")] public int MyScore { get; set; }
        [JsonProperty("opponentScore")] public int OpponentScore { get; set; }
        [JsonProperty("opponentUsername")] public string OpponentUsername { get; set; } = string.Empty;
        [JsonProperty("myTaps")] public int MyTaps { get; set; }
        [JsonProperty("myMisses")] public int MyMisses { get; set; }
        [JsonProperty("accuracyPercent")] public double AccuracyPercent { get; set; }
        [JsonProperty("tapsPerSecond")] public double TapsPerSecond { get; set; }
        [JsonProperty("rpDelta")] public int RpDelta { get; set; }
        [JsonProperty("isFriendly")] public bool IsFriendly { get; set; }
    }
}
