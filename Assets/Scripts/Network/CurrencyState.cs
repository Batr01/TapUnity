using System;
using TapBrawl.Models;

namespace TapBrawl.Network
{
    public static class CurrencyState
    {
        public static event Action<int, int>? BalancesUpdated;

        public static void ApplyBalances(int coins, int gems)
        {
            var session = AuthContext.Current;
            if (session == null)
                return;

            session.Player.Coins = coins;
            session.Player.Gems = gems;
            AuthContext.Current = session;
            AuthStorage.Save(session);
            BalancesUpdated?.Invoke(coins, gems);
        }
    }
}
