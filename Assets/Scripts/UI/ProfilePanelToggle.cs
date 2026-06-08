using UnityEngine;

namespace TapBrawl.UI
{
    /// <summary>
    /// Открытие и закрытие панели профиля в лобби.
    /// Назначьте корень панели (объект с <see cref="ProfilePanelController"/>), затем в Button → On Click () перетащите этот компонент и выберите <c>Show</c> / <c>Hide</c>.
    /// </summary>
    public sealed class ProfilePanelToggle : MonoBehaviour
    {
        [Tooltip("Корень панели профиля (обычно объект с ProfilePanelController).")]
        [SerializeField]
        private GameObject profilePanelRoot = null!;

        [SerializeField]
        private float openAnimDuration = UiModalSlideAnimator.DefaultDuration;

        private void Awake()
        {
            if (profilePanelRoot == null)
                return;

            var sync = profilePanelRoot.GetComponent<ProfileModalLayoutSync>();
            if (sync == null)
                sync = profilePanelRoot.AddComponent<ProfileModalLayoutSync>();
            sync.Apply();
        }

        /// <summary>Открыть панель (повторный вызов закрывает).</summary>
        public void Show()
        {
            if (profilePanelRoot != null && profilePanelRoot.activeSelf)
            {
                Hide();
                return;
            }

            var host = LobbyModalsHost.Instance;
            host?.CloseShop();
            host?.CloseSettings();
            host?.CloseFriends();
            host?.CloseAllSkillModals();
            SetVisible(true);
        }

        /// <summary>Закрыть панель.</summary>
        public void Hide()
        {
            SetVisible(false);
        }

        /// <summary>Переключить видимость.</summary>
        public void Toggle()
        {
            if (profilePanelRoot == null)
                return;
            SetVisible(!profilePanelRoot.activeSelf);
        }

        private void SetVisible(bool visible)
        {
            if (profilePanelRoot == null)
            {
                Debug.LogWarning("[ProfilePanelToggle] Не назначен profilePanelRoot.", this);
                return;
            }

            if (visible)
            {
                profilePanelRoot.transform.SetAsLastSibling();
                profilePanelRoot.SetActive(true);
                UiModalSlideAnimator.Play(this, profilePanelRoot.transform, openAnimDuration);
            }
            else
            {
                profilePanelRoot.SetActive(false);
            }
        }
    }
}
