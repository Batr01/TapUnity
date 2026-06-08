using TapBrawl.Core.Enums;
using UnityEngine;

namespace TapBrawl.Core.Skills
{
    /// <summary>Дублирует кривые TapBrawl.Core.Skills.SkillBalance на сервере (должны совпадать).</summary>
    public static class SkillBalance
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 10;

        public const int GiantCirclesSkillId = 1;
        public const int RedDeceptionSkillId = 2;
        public const int SmokeVeilSkillId = 3;
        public const int OverheatSkillId = 4;
        public const int ChainDischargeSkillId = 5;

        public static readonly int[] KnownSkillIds =
            { GiantCirclesSkillId, RedDeceptionSkillId, SmokeVeilSkillId, OverheatSkillId, ChainDischargeSkillId };

        public static bool IsKnownSkillId(int skillId) =>
            skillId is GiantCirclesSkillId or RedDeceptionSkillId or SmokeVeilSkillId or OverheatSkillId
                or ChainDischargeSkillId;

        public static SkillRarity RarityForSkillId(int skillId) =>
            skillId switch
            {
                GiantCirclesSkillId => SkillRarity.Common,
                RedDeceptionSkillId => SkillRarity.Epic,
                SmokeVeilSkillId => SkillRarity.Legendary,
                OverheatSkillId => SkillRarity.Legendary,
                ChainDischargeSkillId => SkillRarity.Epic,
                _ => SkillRarity.Common,
            };

        public static string RarityDisplayName(SkillRarity rarity) =>
            rarity switch
            {
                SkillRarity.Common => "Обычный",
                SkillRarity.Rare => "Редкий",
                SkillRarity.Epic => "Эпический",
                SkillRarity.Legendary => "Легендарный",
                _ => string.Empty,
            };

        public static int ClampLevel(int level) => Mathf.Clamp(level, MinLevel, MaxLevel);

        public static int UpgradeCostCoins(int skillId, int currentLevel)
        {
            _ = skillId;
            var l = ClampLevel(currentLevel);
            if (l >= MaxLevel)
                return 0;
            return 25 + l * 35;
        }

        public static float GiantCirclesDurationSec(int level)
        {
            var t = (ClampLevel(level) - MinLevel) / (float)(MaxLevel - MinLevel);
            return Mathf.Lerp(2f, 5f, t);
        }

        public static float GiantCirclesSizeMultiplier(int level)
        {
            var t = (ClampLevel(level) - MinLevel) / (float)(MaxLevel - MinLevel);
            return Mathf.Lerp(1.2f, 2.25f, t);
        }

        public static float RedDeceptionDurationSec(int level)
        {
            var t = (ClampLevel(level) - MinLevel) / (float)(MaxLevel - MinLevel);
            return Mathf.Lerp(1.2f, 3f, t);
        }

        public static float SmokeVeilDurationSec(int level)
        {
            var t = (ClampLevel(level) - MinLevel) / (float)(MaxLevel - MinLevel);
            return Mathf.Lerp(2f, 5f, t);
        }

        public static float OverheatDurationSec(int level)
        {
            var t = (ClampLevel(level) - MinLevel) / (float)(MaxLevel - MinLevel);
            return Mathf.Lerp(4.5f, 5.5f, t);
        }

        public static float OverheatSpawnCadenceMultiplier(int level)
        {
            var t = (ClampLevel(level) - MinLevel) / (float)(MaxLevel - MinLevel);
            return Mathf.Lerp(0.65f, 0.45f, t);
        }

        public static float ChainDischargeArmedDurationSec(int level)
        {
            var t = (ClampLevel(level) - MinLevel) / (float)(MaxLevel - MinLevel);
            return Mathf.Lerp(4f, 7f, t);
        }

        public static int ChainDischargeAdditionalTaps(int level)
        {
            var l = ClampLevel(level);
            if (l >= 10)
                return 3;
            if (l >= 5)
                return 2;
            return 1;
        }

        public static float EffectDurationForSkill(int skillId, int effectLevel) =>
            skillId switch
            {
                RedDeceptionSkillId => RedDeceptionDurationSec(effectLevel),
                SmokeVeilSkillId => SmokeVeilDurationSec(effectLevel),
                _ => RedDeceptionDurationSec(effectLevel),
            };

        // --- Энергия скиллов в матче (должно совпадать с TapBackend/TapBrawl.Core/Skills/SkillBalance.cs) ---

        public const float SkillEnergyMaxPercentDefault = 100f;

        public const float SkillEnergyPassiveGainPerSecondDefault = 4f;

        public const float GiantCirclesSkillEnergyCostPercent = 30f;

        public const float RedDeceptionSkillEnergyCostPercent = 50f;

        public const float SmokeVeilSkillEnergyCostPercent = 80f;

        public const float OverheatSkillEnergyCostPercent = 90f;

        public const float ChainDischargeSkillEnergyCostPercent = 30f;

        /// <summary>Сколько % шкалы энергии списывает активация скилла (0 для неизвестного id).</summary>
        public static float SkillEnergyCostPercentForSkillId(int skillId) =>
            skillId switch
            {
                GiantCirclesSkillId => GiantCirclesSkillEnergyCostPercent,
                RedDeceptionSkillId => RedDeceptionSkillEnergyCostPercent,
                SmokeVeilSkillId => SmokeVeilSkillEnergyCostPercent,
                OverheatSkillId => OverheatSkillEnergyCostPercent,
                ChainDischargeSkillId => ChainDischargeSkillEnergyCostPercent,
                _ => 0f,
            };
    }
}
