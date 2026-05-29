using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TapBrawl.Models
{
    [Serializable]
    public sealed class PlayerSkillStateItemDto
    {
        [JsonProperty("skillId")]
        public int SkillId { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("nextUpgradeCostCoins")]
        public int NextUpgradeCostCoins { get; set; }
    }

    [Serializable]
    public sealed class PlayerSkillsStateDto
    {
        [JsonProperty("coins")]
        public int Coins { get; set; }

        [JsonProperty("skills")]
        public List<PlayerSkillStateItemDto> Skills { get; set; } = new();

        [JsonProperty("loadoutSlotSkillIds")]
        public List<int> LoadoutSlotSkillIds { get; set; } = new();
    }

    [Serializable]
    public sealed class UpgradePlayerSkillRequestDto
    {
        [JsonProperty("skillId")]
        public int SkillId { get; set; }
    }

    [Serializable]
    public sealed class SetPlayerSkillLoadoutRequestDto
    {
        [JsonProperty("skillIds")]
        public List<int> SkillIds { get; set; } = new();
    }
}
