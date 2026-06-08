using TapBrawl.Core.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>Одна вручную собранная строка скилла в панели прокачки.</summary>
    public sealed class SkillUpgradeRowView : MonoBehaviour
    {
        private const string InfoColumnName = "Info Column";
        private const string RarityTextName = "Rarity Text";
        private const string LevelTextName = "Level Text";
        private const string UpgradeCostTextName = "Upgrade Cost Text";

        [Tooltip("SkillId: 1 — крупные круги, 2 — красная пелена, 3 — дымовая завеса, 4 — перегрев.")]
        [SerializeField] private int skillId = 1;
        [SerializeField] private SkillCatalog? skillCatalog;
        [SerializeField] private Text? nameText;
        [SerializeField] private Text? rarityText;
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

        private void Awake() => EnsureInfoUi();

        public void EnsureInfoUi()
        {
            ResolveRefs();
            EnsureInfoColumn();
        }

        public void Bind(int level, int nextUpgradeCostCoins, bool canAfford)
        {
            EnsureInfoUi();

            if (nameText != null)
                nameText.text = skillCatalog != null ? skillCatalog.GetDisplayName(skillId) : SkillDefinitions.GetDisplayName(skillId);
            if (rarityText != null)
            {
                var rarity = SkillDefinitions.GetRarity(skillId);
                rarityText.text = SkillDefinitions.GetRarityDisplayName(skillId);
                rarityText.color = SkillRarityStyle.GetTextColor(rarity);
            }

            if (levelText != null)
                levelText.text = $"Уровень: {level}/{SkillBalance.MaxLevel}";
            if (descriptionText != null)
                descriptionText.text = skillCatalog != null ? skillCatalog.GetShortHint(skillId) : SkillDefinitions.GetShortHint(skillId);

            var maxed = level >= SkillBalance.MaxLevel || nextUpgradeCostCoins <= 0;
            if (upgradeCostText != null)
                upgradeCostText.text = maxed ? "Максимальный уровень" : $"Следующий уровень: {nextUpgradeCostCoins} монет";
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

        private void ResolveRefs()
        {
            if (nameText == null)
                nameText = FindText(InfoColumnName + "/Name Text", "Name Text");
            if (rarityText == null)
                rarityText = FindText(InfoColumnName + "/" + RarityTextName, RarityTextName);
            if (levelText == null)
                levelText = FindText(InfoColumnName + "/" + LevelTextName, LevelTextName);
            if (upgradeCostText == null)
                upgradeCostText = FindText(InfoColumnName + "/" + UpgradeCostTextName, UpgradeCostTextName);
            if (descriptionText == null)
                descriptionText = FindText("Description Text");
            if (skillIconImage == null)
                skillIconImage = transform.Find("Skill Icon Image")?.GetComponent<Image>();
            if (upgradeButton == null)
                upgradeButton = transform.Find("Button Group/Upgrade Button")?.GetComponent<Button>();
            if (detailsButton == null)
                detailsButton = transform.Find("Button Group/Details Button")?.GetComponent<Button>();
            if (matchEnergyCostText == null)
            {
                matchEnergyCostText = FindText("Button Group/Match Energy Cost Text", "Button Group/Energy Text");
                if (matchEnergyCostText == null)
                {
                    foreach (var t in GetComponentsInChildren<Text>(true))
                    {
                        if (t.gameObject.name.Contains("Energy"))
                            matchEnergyCostText = t;
                    }
                }
            }
        }

        private Text? FindText(params string[] paths)
        {
            foreach (var path in paths)
            {
                var tr = transform.Find(path);
                if (tr != null)
                    return tr.GetComponent<Text>();
            }

            return null;
        }

        private void EnsureInfoColumn()
        {
            if (levelText != null && upgradeCostText != null)
                return;

            var infoColumn = transform.Find(InfoColumnName);
            if (infoColumn == null)
            {
                var nameTr = transform.Find("Name Text");
                if (nameTr == null)
                    return;

                var go = new GameObject(InfoColumnName, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
                infoColumn = go.transform;
                infoColumn.SetParent(transform, false);
                infoColumn.SetSiblingIndex(nameTr.GetSiblingIndex());

                var vlg = go.GetComponent<VerticalLayoutGroup>();
                vlg.childAlignment = TextAnchor.MiddleLeft;
                vlg.spacing = 2f;
                vlg.padding = new RectOffset(0, 0, 0, 0);
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                var le = go.GetComponent<LayoutElement>();
                le.flexibleWidth = 1f;
                le.minWidth = 100f;
                le.preferredHeight = UiModalStyle.SkillsRowHeight - 16f;

                nameTr.SetParent(infoColumn, false);
                nameText = nameTr.GetComponent<Text>();
                StyleNameText(nameText);
            }

            if (rarityText == null)
            {
                rarityText = EnsureChildText(infoColumn, RarityTextName, 18, SkillRarityStyle.GetTextColor(SkillDefinitions.GetRarity(skillId)), 20f);
                rarityText.text = SkillDefinitions.GetRarityDisplayName(skillId);
            }

            if (levelText == null)
                levelText = EnsureChildText(infoColumn, LevelTextName, 20, UiModalStyle.ProfilePrimaryTextColor, 24f);
            if (upgradeCostText == null)
                upgradeCostText = EnsureChildText(infoColumn, UpgradeCostTextName, 18, UiModalStyle.ProfileAccentTextColor, 22f);
        }

        private static void StyleNameText(Text? text)
        {
            if (text == null)
                return;

            text.color = UiModalStyle.ProfilePrimaryTextColor;
            text.alignment = TextAnchor.MiddleLeft;
            text.fontSize = Mathf.Min(text.fontSize <= 0 ? 24 : text.fontSize, 24);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
        }

        private static Text EnsureChildText(Transform parent, string childName, int fontSize, Color color, float preferredHeight)
        {
            var existing = parent.Find(childName);
            if (existing != null)
            {
                var existingText = existing.GetComponent<Text>();
                if (existingText != null)
                    return existingText;
            }

            var go = new GameObject(childName, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;

            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            le.minHeight = preferredHeight;
            le.flexibleHeight = 0f;
            return text;
        }
    }
}
