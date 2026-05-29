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

        /// <summary>Открыть панель.</summary>
        public void Show()
        {
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

            profilePanelRoot.SetActive(visible);
        }
    }
}
