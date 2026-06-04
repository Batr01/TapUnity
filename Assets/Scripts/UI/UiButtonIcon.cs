using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>
    /// Иконка слева на кнопке. Назначьте Sprite в инспекторе.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class UiButtonIcon : MonoBehaviour
    {
        [SerializeField] private Image? iconImage;
        [SerializeField] private Sprite? icon;
        [SerializeField] private bool hideWhenEmpty = true;

        private void Reset() => iconImage = GetComponent<Image>();

        private void Awake() => ApplyIcon();

#if UNITY_EDITOR
        private void OnValidate() => ApplyIcon();
#endif

        public void SetIcon(Sprite? sprite)
        {
            icon = sprite;
            ApplyIcon();
        }

        private void ApplyIcon()
        {
            if (iconImage == null)
                iconImage = GetComponent<Image>();
            if (iconImage == null)
                return;

            iconImage.sprite = icon;
            if (hideWhenEmpty)
                iconImage.enabled = icon != null;
        }
    }
}
