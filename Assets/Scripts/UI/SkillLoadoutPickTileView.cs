using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>Ссылочная часть префаба плитки выбора скилла (иконка + подпись).</summary>
    public sealed class SkillLoadoutPickTileView : MonoBehaviour
    {
        [SerializeField] private Image? iconImage;
        [SerializeField] private Text? titleText;

        public void Apply(Sprite? icon, string title, bool interactable, Color backgroundColor, Color titleColor)
        {
            var bg = GetComponent<Image>();
            if (bg != null)
                bg.color = backgroundColor;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
                iconImage.color = interactable ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                iconImage.preserveAspect = true;
            }

            if (titleText != null)
            {
                titleText.text = title;
                titleText.color = titleColor;
            }
        }
    }
}
