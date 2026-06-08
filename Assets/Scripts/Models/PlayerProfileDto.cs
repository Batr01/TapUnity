using System;
using Newtonsoft.Json;

namespace TapBrawl.Models
{
    [Serializable]
    public sealed class PlayerProfileDto
    {
        [JsonProperty("id")] public Guid Id { get; set; }
        [JsonProperty("username")] public string Username { get; set; } = string.Empty;
        [JsonProperty("coins")] public int Coins { get; set; }
        [JsonProperty("gems")] public int Gems { get; set; }
        [JsonProperty("rankPoints")] public int RankPoints { get; set; }
        [JsonProperty("tier")] public string Tier { get; set; } = string.Empty;
        [JsonProperty("avatarId")] public string AvatarId { get; set; } = "default";
        [JsonProperty("unlockedAvatarIds")] public string[] UnlockedAvatarIds { get; set; } = System.Array.Empty<string>();
    }
}
