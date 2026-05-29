using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace TapBrawl.Models
{
    /// <summary>Событие с сервера: соперник применил визуальный скилл (2 — красная пелена, 3 — дым).</summary>
    [Serializable]
    public sealed class OpponentVisualSkillDto
    {
        [JsonProperty("skillType")]
        [JsonPropertyName("skillType")]
        public int SkillType { get; set; }

        /// <summary>Уровень скилла у атакующего (1..10). Старые серверы могут не слать поле — тогда 10.</summary>
        [JsonProperty("casterSkillLevel")]
        [JsonPropertyName("casterSkillLevel")]
        public int CasterSkillLevel { get; set; } = 10;
    }
}
