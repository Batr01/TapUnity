using TapBrawl.Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>
    /// Заглушка настроек. Тексты задаются в инспекторе или на компонентах Text в иерархии.
    /// </summary>
    public sealed class SettingsModalView : MonoBehaviour
    {
        private const float ActionButtonHeight = 72f;

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

        [SerializeField]
        private Button? logoutButton;

        [SerializeField]
        private string authSceneName = "Auth";

        private void Awake()
        {
            TryAutoWire();
            EnsureLogoutButton();
            ApplyTexts();
            if (logoutButton != null)
                logoutButton.onClick.AddListener(OnLogoutClicked);
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
            if (logoutButton == null)
                logoutButton = transform.Find("Panel/Actions/Logout Button")?.GetComponent<Button>();
        }

        private void EnsureLogoutButton()
        {
            if (logoutButton != null)
                return;

            var panel = transform.Find("Panel");
            if (panel == null)
                return;

            var actions = panel.Find("Actions");
            if (actions == null)
            {
                var actionsGo = new GameObject("Actions", typeof(RectTransform), typeof(VerticalLayoutGroup));
                actionsGo.transform.SetParent(panel, false);
                actionsGo.transform.SetSiblingIndex(1);

                var actionsRt = actionsGo.GetComponent<RectTransform>();
                actionsRt.anchorMin = new Vector2(0.08f, 0.45f);
                actionsRt.anchorMax = new Vector2(0.92f, 0.82f);
                actionsRt.offsetMin = Vector2.zero;
                actionsRt.offsetMax = Vector2.zero;

                var layout = actionsGo.GetComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(0, 0, 8, 8);
                layout.spacing = 12f;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                actions = actionsGo.transform;
            }

            logoutButton = CreateActionButton(actions, "Logout Button", "Выйти из аккаунта", new Color(0.55f, 0.22f, 0.22f, 1f));

            var messageRt = panel.Find("Message Text")?.GetComponent<RectTransform>();
            if (messageRt != null)
            {
                messageRt.anchorMin = new Vector2(0.1f, 0.12f);
                messageRt.anchorMax = new Vector2(0.9f, 0.38f);
            }
        }

        private static Button CreateActionButton(Transform parent, string name, string label, Color bgColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var layoutElement = go.GetComponent<LayoutElement>();
            layoutElement.minHeight = ActionButtonHeight;
            layoutElement.preferredHeight = ActionButtonHeight;

            var image = go.GetComponent<Image>();
            image.color = bgColor;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            var text = labelGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = label;
            text.fontSize = 32;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;

            return button;
        }

        private void OnLogoutClicked()
        {
            var lobby = FindFirstObjectByType<LobbyScreenController>();
            if (lobby != null)
            {
                lobby.Logout();
                return;
            }

            AuthManager.Logout();
            if (!string.IsNullOrEmpty(authSceneName))
                SceneManager.LoadScene(authSceneName, LoadSceneMode.Single);
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
