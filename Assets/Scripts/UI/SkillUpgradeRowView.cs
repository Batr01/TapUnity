using TapBrawl.Core.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>Одна вручную собранная строка скилла в панели прокачки.</summary>
    public sealed class SkillUpgradeRowView : MonoBehaviour
    {
        [Tooltip("SkillId: 1 — крупные круги, 2 — красная пелена, 3 — дымовая завеса, 4 — перегрев.")]
        [SerializeField] private int skillId = 1;
        [SerializeField] private SkillCatalog? skillCatalog;
        [SerializeField] private Text? nameText;
        [SerializeField] private Text? levelText;
        [SerializeField] private Text? descriptionText;
        [SerializeField] private Text? upgradeCostText;
        [Tooltip("Рядом с «Подробнее»: стоимость активации в % шкалы энергии матча (подключите в инспекторе).")]
        [SerializeField] private Text? matchEnergyCostText;
        [SerializeField] private Image? skillIconImage;
        [SerializeField] private Button? upgradeButton;
        [SerializeField] private Button? detailsButton;

        public int SkillId => skillId;
        public Button? UpgradeButton => upgradeButton;
        public Button? DetailsButton => detailsButton;

        public void Bind(int level, int nextUpgradeCostCoins, bool canAfford)
        {
            if (nameText != null)
                nameText.text = skillCatalog != null ? skillCatalog.GetDisplayName(skillId) : SkillDefinitions.GetDisplayName(skillId);
            if (levelText != null)
                levelText.text = $"Уровень: {level}/10";
            if (descriptionText != null)
                descriptionText.text = skillCatalog != null ? skillCatalog.GetShortHint(skillId) : SkillDefinitions.GetShortHint(skillId);

            var maxed = level >= SkillBalance.MaxLevel || nextUpgradeCostCoins <= 0;
            if (upgradeCostText != null)
                upgradeCostText.text = maxed ? "Максимум" : $"Апгрейд: {nextUpgradeCostCoins} монет";
            if (upgradeButton != null)
                upgradeButton.interactable = !maxed && canAfford;

            if (matchEnergyCostText != null)
            {
                var energyPct = SkillBalance.SkillEnergyCostPercentForSkillId(skillId);
                if (energyPct > 0.001f)
                {
                    matchEnergyCostText.gameObject.SetActive(true);
                    matchEnergyCostText.text = $"{energyPct:0}%";
                }
                else
                {
                    matchEnergyCostText.gameObject.SetActive(false);
                }
            }

            if (skillIconImage == null)
                return;

            var icon = skillCatalog != null ? skillCatalog.GetIcon(skillId) : null;
            if (icon != null)
            {
                skillIconImage.enabled = true;
                skillIconImage.sprite = icon;
                return;
            }

            skillIconImage.sprite = null;
            skillIconImage.enabled = false;
        }
    }
}
