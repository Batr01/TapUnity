using System.Collections.Generic;

namespace TapBrawl.Core.Skills
{
    public static class SkillDefinitions
    {
        public static string GetDisplayName(int skillId) =>
            skillId switch
            {
                SkillBalance.GiantCirclesSkillId => "Крупные круги",
                SkillBalance.RedDeceptionSkillId => "Красная пелена",
                SkillBalance.SmokeVeilSkillId => "Дымовая завеса",
                SkillBalance.OverheatSkillId => "Перегрев",
                SkillBalance.ChainDischargeSkillId => "Цепная реакция",
                _ => $"Скилл #{skillId}",
            };

        public static string GetShortHint(int skillId) =>
            skillId switch
            {
                SkillBalance.GiantCirclesSkillId => "Бафф размера кругов на себя.",
                SkillBalance.RedDeceptionSkillId => "Визуальный дебафф сопернику.",
                SkillBalance.SmokeVeilSkillId => "Дым на поле соперника.",
                SkillBalance.OverheatSkillId =>
                    "~5 с: цели чаще, фантомы мигают быстрее; идеальный тап в конце жизни цели даёт +1 к серии комбо.",
                SkillBalance.ChainDischargeSkillId =>
                    "После активации следующий идеальный тап цепляет 1–3 ближайших Normal/Gold круга.",
                _ => string.Empty,
            };

        public static IReadOnlyList<int> AllKnownSkillIds => SkillBalance.KnownSkillIds;
    }
}
