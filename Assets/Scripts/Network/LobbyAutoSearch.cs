namespace TapBrawl.Network
{
    /// <summary>
    /// Флаг: при входе в Lobby автоматически начать поиск матча (кнопка «Найти игру» в Result).
    /// </summary>
    public static class LobbyAutoSearch
    {
        public static bool RequestAutoSearch { get; set; }
    }
}
