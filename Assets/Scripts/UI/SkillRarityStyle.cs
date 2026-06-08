using TapBrawl.Core.Enums;
using TapBrawl.Core.Skills;
using UnityEngine;

namespace TapBrawl.UI
{
    public static class SkillRarityStyle
    {
        public static Color GetTextColor(SkillRarity rarity) =>
            rarity switch
            {
                SkillRarity.Common => new Color(0.75f, 0.75f, 0.78f, 1f),
                SkillRarity.Rare => new Color(0.45f, 0.72f, 1f, 1f),
                SkillRarity.Epic => new Color(0.78f, 0.52f, 1f, 1f),
                SkillRarity.Legendary => new Color(1f, 0.72f, 0.28f, 1f),
                _ => UiModalStyle.ProfilePrimaryTextColor,
            };

        public static Color GetTileBackgroundColor(SkillRarity rarity, bool interactable)
        {
            if (!interactable)
                return new Color(0.18f, 0.18f, 0.18f, 0.85f);

            return rarity switch
            {
                SkillRarity.Common => new Color(0.24f, 0.28f, 0.34f, 1f),
                SkillRarity.Rare => new Color(0.18f, 0.32f, 0.52f, 1f),
                SkillRarity.Epic => new Color(0.34f, 0.22f, 0.48f, 1f),
                SkillRarity.Legendary => new Color(0.48f, 0.32f, 0.12f, 1f),
                _ => new Color(0.22f, 0.38f, 0.62f, 1f),
            };
        }

        public static string GetDisplayName(int skillId) => SkillDefinitions.GetRarityDisplayName(skillId);
    }
}
