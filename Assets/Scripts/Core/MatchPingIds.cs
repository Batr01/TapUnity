namespace TapBrawl.Core
{
    /// <summary>Типы пингов для SignalR (сервер принимает только эти id).</summary>
    public static class MatchPingIds
    {
        public const int Like = 1;
        public const int Dislike = 2;
        public const int Sticker67 = 3;

        public static bool IsValid(int pingType) => pingType is Like or Dislike or Sticker67;
    }
}
