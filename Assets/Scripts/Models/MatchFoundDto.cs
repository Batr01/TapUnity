using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace TapBrawl.Models
{
    /// <summary>Событие MatchFound с сервера (SignalR). Клиент десериализует через System.Text.Json.</summary>
    [Serializable]
    public sealed class MatchFoundDto
    {
        [JsonProperty("matchId")]
        [JsonPropertyName("matchId")]
        public Guid MatchId { get; set; }

        [JsonProperty("seed")]
        [JsonPropertyName("seed")]
        public uint Seed { get; set; }

        [JsonProperty("durationSec")]
        [JsonPropertyName("durationSec")]
        public int DurationSec { get; set; }

        [JsonProperty("opponentPlayerId")]
        [JsonPropertyName("opponentPlayerId")]
        public Guid OpponentPlayerId { get; set; }

        [JsonProperty("opponentUsername")]
        [JsonPropertyName("opponentUsername")]
        public string OpponentUsername { get; set; } = string.Empty;

        /// <summary>1 = первый в <c>match_players</c> (ждал в очереди), 2 = второй (подключился к паре).</summary>
        [JsonProperty("yourSlot")]
        [JsonPropertyName("yourSlot")]
        public int YourSlot { get; set; } = 1;
    }
}
