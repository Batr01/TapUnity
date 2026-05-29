namespace TapBrawl.Core
{
    /// <summary>Идентификаторы скиллов для UI и SignalR (типы 1 и 4 только локально).</summary>
    public static class MatchSkillIds
    {
        public const int GiantCirclesSelfBuff = 1;
        /// <summary>Все круги красные (обман), логика очков без изменений.</summary>
        public const int OpponentRedDeceptionVisual = 2;
        /// <summary>Затемнённая «дымовая» завеса на части поля.</summary>
        public const int OpponentSmokeVeil = 3;
        /// <summary>Перегрев: ускорение спавна и комбо (только локально).</summary>
        public const int OverheatSelfBuff = 4;
        /// <summary>Разряд цепи: следующий идеальный тап автоматически засчитывает 1–3 ближайшие цели.</summary>
        public const int ChainDischargeSelfBuff = 5;
    }
}
