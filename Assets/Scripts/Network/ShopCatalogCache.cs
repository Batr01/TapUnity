using System.Collections.Generic;
using TapBrawl.Models;

namespace TapBrawl.Network
{
    /// <summary>Сессионный кеш каталога магазина. Заполняется однократно при входе в лобби.</summary>
    public static class ShopCatalogCache
    {
        public static IReadOnlyList<ShopProductDto>? Products { get; private set; }
        public static IReadOnlyList<ExchangePackDto>? ExchangePacks { get; private set; }

        public static bool HasProducts => Products is { Count: > 0 };
        public static bool HasExchangePacks => ExchangePacks is { Count: > 0 };

        public static void SetProducts(IReadOnlyList<ShopProductDto> products) =>
            Products = products;

        public static void SetExchangePacks(IReadOnlyList<ExchangePackDto> packs) =>
            ExchangePacks = packs;

        public static void Clear()
        {
            Products = null;
            ExchangePacks = null;
        }
    }
}
