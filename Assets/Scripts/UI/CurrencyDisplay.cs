using System.Globalization;
using TapBrawl.Core.Economy;

namespace TapBrawl.UI
{
    public static class CurrencyDisplay
    {
        private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

        public static string FormatCoins(int coins) => $"Монеты: {coins.ToString("N0", Ru)}";

        public static string FormatGems(int gems) => $"Adipoint: {gems.ToString("N0", Ru)}";

        public static string FormatEquivalent(int gems) =>
            $"≈ {GemsExchangeBalance.ToBaseCoins(gems).ToString("N0", Ru)} монет";

        public static string FormatGemsWithEquivalent(int gems) =>
            $"{FormatGems(gems)} ({FormatEquivalent(gems)})";

        public static string FormatLobbyCompact(int coins, int gems) =>
            $"{FormatCoins(coins)} · {FormatGems(gems)} ({FormatEquivalent(gems)})";

        public static string FormatShopBalance(int gems) =>
            $"{FormatGems(gems)} ({FormatEquivalent(gems)})";

        public static string FormatExchangePack(int gemsCost, int coinsReward, int bonusPercent)
        {
            var bonus = bonusPercent > 0 ? $" (+{bonusPercent}%)" : string.Empty;
            return $"{gemsCost.ToString("N0", Ru)} Adipoint → {coinsReward.ToString("N0", Ru)} монет{bonus}";
        }
    }
}
