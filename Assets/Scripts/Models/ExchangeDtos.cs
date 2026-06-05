using Newtonsoft.Json;

namespace TapBrawl.Models
{
    public sealed class ExchangePackDto
    {
        [JsonProperty("packId")] public string PackId { get; set; } = string.Empty;
        [JsonProperty("displayName")] public string DisplayName { get; set; } = string.Empty;
        [JsonProperty("gemsCost")] public int GemsCost { get; set; }
        [JsonProperty("coinsReward")] public int CoinsReward { get; set; }
        [JsonProperty("bonusPercent")] public int BonusPercent { get; set; }
    }

    public sealed class ExchangeGemsRequestDto
    {
        [JsonProperty("packId")] public string PackId { get; set; } = string.Empty;
    }

    public sealed class ExchangeGemsResponseDto
    {
        [JsonProperty("coins")] public int Coins { get; set; }
        [JsonProperty("gems")] public int Gems { get; set; }
        [JsonProperty("coinsGranted")] public int CoinsGranted { get; set; }
    }
}
