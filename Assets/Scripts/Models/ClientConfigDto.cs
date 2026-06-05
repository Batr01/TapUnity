using System;
using Newtonsoft.Json;

namespace TapBrawl.Models
{
    [Serializable]
    public sealed class ClientConfigDto
    {
        [JsonProperty("minSupportedVersion")]
        public string MinSupportedVersion { get; set; } = string.Empty;

        [JsonProperty("latestVersion")]
        public string LatestVersion { get; set; } = string.Empty;

        [JsonProperty("androidStoreUrl")]
        public string AndroidStoreUrl { get; set; } = string.Empty;

        [JsonProperty("iosStoreUrl")]
        public string IosStoreUrl { get; set; } = string.Empty;
    }
}
