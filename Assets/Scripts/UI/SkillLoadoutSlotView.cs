using TapBrawl.Core.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>Один вручную собранный слот выбора активного скилла.</summary>
    public sealed class SkillLoadoutSlotView : MonoBehaviour
    {
        [SerializeField] private Text? slotLabelText;
        [SerializeField] private Text? selectedSkillText;
        [SerializeField] private Image? selectedSkillIconImage;
        [SerializeField] private Button? cycleButton;
        [SerializeField] private SkillCatalog? skillCatalog;

        private int _choiceIndex;

        public Button? CycleButton => cycleButton;
        public int SelectedSkillId => SkillBalance.KnownSkillIds[Mathf.Clamp(_choiceIndex, 0, SkillBalance.KnownSkillIds.Length - 1)];

        public void BindSlot(int slotIndex, int skillId)
        {
            if (slotLabelText != null)
                slotLabelText.text = $"Слот {slotIndex + 1}";

            var idx = System.Array.IndexOf(SkillBalance.KnownSkillIds, skillId);
            _choiceIndex = idx >= 0 ? idx : Mathf.Clamp(slotIndex, 0, SkillBalance.KnownSkillIds.Length - 1);
            RefreshText();
        }

        public void CycleNext()
        {
            _choiceIndex = (_choiceIndex + 1) % SkillBalance.KnownSkillIds.Length;
            RefreshText();
        }

        /// <summary>Установить выбранный скилл по известному SkillBalance id.</summary>
        public void SelectSkillById(int skillId)
        {
            if (!SkillBalance.IsKnownSkillId(skillId))
                return;
            var idx = System.Array.IndexOf(SkillBalance.KnownSkillIds, skillId);
            if (idx < 0)
                return;
            _choiceIndex = idx;
            RefreshText();
        }

        private void RefreshText()
        {
            if (selectedSkillText != null)
                selectedSkillText.text = ResolveDisplayName(SelectedSkillId);

            if (selectedSkillIconImage == null)
                return;

            var icon = skillCatalog != null ? skillCatalog.GetIcon(SelectedSkillId) : null;
            if (icon != null)
            {
                selectedSkillIconImage.enabled = true;
                selectedSkillIconImage.sprite = icon;
                return;
            }

            selectedSkillIconImage.sprite = null;
            selectedSkillIconImage.enabled = false;
        }

        private string ResolveDisplayName(int skillId) =>
            skillCatalog != null ? skillCatalog.GetDisplayName(skillId) : SkillDefinitions.GetDisplayName(skillId);
    }
}
