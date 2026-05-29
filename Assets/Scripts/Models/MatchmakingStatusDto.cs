using System;
using Newtonsoft.Json;

namespace TapBrawl.Models
{
    [Serializable]
    public sealed class MatchmakingStatusDto
    {
        [JsonProperty("inQueue")] public bool InQueue { get; set; }
    }
}
