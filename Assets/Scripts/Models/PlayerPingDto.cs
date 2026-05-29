using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace TapBrawl.Models
{
    /// <summary>Входящий пинг от соперника (1 — лайк, 2 — дизлайк, 3 — стикер 67).</summary>
    [Serializable]
    public sealed class PlayerPingDto
    {
        [JsonProperty("pingType")]
        [JsonPropertyName("pingType")]
        public int PingType { get; set; }
    }
}
