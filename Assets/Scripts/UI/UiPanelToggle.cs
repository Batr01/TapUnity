using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>
    /// Показ/скрытие UI-панели (модалки) через SetActive.
    /// Backdrop-кнопка опционально закрывает панель.
    /// </summary>
    public sealed class UiPanelToggle : MonoBehaviour
    {
        [Tooltip("Корень панели (если пусто — этот GameObject).")]
        [SerializeField]
        private GameObject? panelRoot;

        [Tooltip("Кнопка на затемнённом фоне для закрытия.")]
        [SerializeField]
        private Button? backdropCloseButton;

        [Tooltip("Длительность анимации выезда снизу, сек.")]
        [SerializeField]
        private float openAnimDuration = 0.22f;

        private GameObject Panel => panelRoot != null ? panelRoot : gameObject;

        private void Awake()
        {
            UiModalStyle.ApplyLobbyModalChrome(transform);
            ApplyBackdropStyle();
            WireBackButtons();

            if (backdropCloseButton != null)
                backdropCloseButton.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            if (backdropCloseButton != null)
                backdropCloseButton.onClick.RemoveListener(Hide);
        }

        private void ApplyBackdropStyle()
        {
            if (backdropCloseButton == null)
                return;

            UiModalStyle.ApplyBackdrop(backdropCloseButton.GetComponent<Image>());
            backdropCloseButton.transition = Selectable.Transition.None;
        }

        private void WireBackButtons()
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                if (button == backdropCloseButton || button.gameObject.name != "Back Button")
                    continue;

                UiModalStyle.PrepareBackButton(button);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(Hide);
            }
        }

        public void Show() => SetVisible(true);

        public void Hide() => SetVisible(false);

        public void Toggle()
        {
            if (Panel == null)
                return;
            SetVisible(!Panel.activeSelf);
        }

        private void SetVisible(bool visible)
        {
            if (Panel == null)
            {
                Debug.LogWarning("[UiPanelToggle] panelRoot не назначен.", this);
                return;
            }

            if (visible)
            {
                Panel.transform.SetAsLastSibling();
                Panel.SetActive(true);
                PlayOpenAnimation();
            }
            else
            {
                Panel.SetActive(false);
            }
        }

        private void PlayOpenAnimation()
        {
            UiModalSlideAnimator.Play(this, Panel.transform, openAnimDuration);
        }
    }
}
