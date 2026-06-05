using System;
using Newtonsoft.Json;

namespace TapBrawl.Models
{
    [Serializable]
    public sealed class VerifyPurchaseRequestDto
    {
        [JsonProperty("productId")] public string ProductId { get; set; } = string.Empty;
        [JsonProperty("purchaseToken")] public string PurchaseToken { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class VerifyPurchaseResponseDto
    {
        [JsonProperty("gems")] public int Gems { get; set; }
        [JsonProperty("granted")] public int Granted { get; set; }
    }

    [Serializable]
    public sealed class ShopProductDto
    {
        [JsonProperty("productId")] public string ProductId { get; set; } = string.Empty;
        [JsonProperty("gemsAmount")] public int GemsAmount { get; set; }
    }
}
