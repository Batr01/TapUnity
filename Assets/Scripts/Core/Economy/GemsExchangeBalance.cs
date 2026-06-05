namespace TapBrawl.Core.Economy
{
    /// <summary>Дублирует TapBrawl.Core.Economy.GemsExchangeBalance на сервере (должны совпадать).</summary>
    public static class GemsExchangeBalance
    {
        public const int BaseCoinsPerGem = 100;

        public static int ToBaseCoins(int gems) => gems * BaseCoinsPerGem;

        public sealed class ExchangePack
        {
            public ExchangePack(string packId, string displayName, int gemsCost, int coinsReward, int bonusPercent)
            {
                PackId = packId;
                DisplayName = displayName;
                GemsCost = gemsCost;
                CoinsReward = coinsReward;
                BonusPercent = bonusPercent;
            }

            public string PackId { get; }
            public string DisplayName { get; }
            public int GemsCost { get; }
            public int CoinsReward { get; }
            public int BonusPercent { get; }
        }

        public static readonly ExchangePack[] Packs =
        {
            new("small", "Малый кошель", 10, 1_000, 0),
            new("medium", "Средняя стопка", 50, 5_500, 10),
            new("large", "Большой сундук", 100, 12_000, 20),
            new("dragon", "Казна Дракона", 500, 65_000, 30),
        };

        public static bool TryGetPack(string packId, out ExchangePack pack)
        {
            packId = packId.Trim();
            foreach (var p in Packs)
            {
                if (string.Equals(p.PackId, packId, System.StringComparison.OrdinalIgnoreCase))
                {
                    pack = p;
                    return true;
                }
            }

            pack = null!;
            return false;
        }
    }
}
