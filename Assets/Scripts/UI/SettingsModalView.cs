using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>
    /// Заглушка настроек. Тексты задаются в инспекторе или на компонентах Text в иерархии.
    /// </summary>
    public sealed class SettingsModalView : MonoBehaviour
    {
        [Header("Тексты")]
        [SerializeField]
        private string title = "Настройки";

        [SerializeField]
        [TextArea(2, 6)]
        private string message = "Настройки всё ещё в разработке.";

        [Header("UI (опционально — найдутся по имени)")]
        [SerializeField]
        private Text? titleText;

        [SerializeField]
        private Text? messageText;

        private void Awake()
        {
            TryAutoWire();
            ApplyTexts();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                TryAutoWire();
                ApplyTexts();
            }
        }

        public void TryAutoWire()
        {
            if (titleText == null)
                titleText = transform.Find("Panel/Header/Title Text")?.GetComponent<Text>();
            if (messageText == null)
                messageText = transform.Find("Panel/Message Text")?.GetComponent<Text>();
        }

        private void ApplyTexts()
        {
            if (titleText != null)
                titleText.text = title;
            if (messageText != null)
                messageText.text = message;
        }
    }
}
