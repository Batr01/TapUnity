using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>Блокирующая модалка force-update без кнопки закрытия.</summary>
    public sealed class UpdateRequiredModal : MonoBehaviour
    {
        [SerializeField] private UiPanelToggle? panelToggle;
        [SerializeField] private Text? titleText;
        [SerializeField] private Text? messageText;
        [SerializeField] private Button? updateButton;

        private string _storeUrl = string.Empty;

        public static UpdateRequiredModal EnsureOnCanvas(Transform canvasTransform)
        {
            var existing = canvasTransform.GetComponentInChildren<UpdateRequiredModal>(true);
            if (existing != null)
                return existing;

            var modalRoot = new GameObject(
                "UpdateRequiredModal",
                typeof(RectTransform),
                typeof(UiPanelToggle),
                typeof(UpdateRequiredModal));
            modalRoot.transform.SetParent(canvasTransform, false);
            StretchFull(modalRoot.GetComponent<RectTransform>());

            var modal = modalRoot.GetComponent<UpdateRequiredModal>();
            modal.BuildUi(modalRoot);
            modalRoot.SetActive(false);
            return modal;
        }

        private void Awake()
        {
            if (panelToggle == null)
                panelToggle = GetComponent<UiPanelToggle>();

            if (updateButton != null)
                updateButton.onClick.AddListener(OpenStore);
        }

        private void OnDestroy()
        {
            if (updateButton != null)
                updateButton.onClick.RemoveListener(OpenStore);
        }

        public void Show(string minSupportedVersion, string currentVersion, string storeUrl)
        {
            _storeUrl = storeUrl ?? string.Empty;

            if (titleText != null)
                titleText.text = "Требуется обновление";

            if (messageText != null)
            {
                messageText.text =
                    $"Ваша версия: {currentVersion}\n" +
                    $"Минимальная версия: {minSupportedVersion}\n\n" +
                    "Обновите приложение, чтобы продолжить.";
            }

            gameObject.SetActive(true);
            panelToggle?.Show();
        }

        private void OpenStore()
        {
            if (string.IsNullOrWhiteSpace(_storeUrl))
            {
                Debug.LogWarning("[UpdateRequiredModal] Store URL не задан.");
                return;
            }

            Application.OpenURL(_storeUrl);
        }

        private void BuildUi(GameObject modalRoot)
        {
            var backdrop = CreateBackdrop(modalRoot.transform);
            var panel = CreatePanel(modalRoot.transform);
            var header = CreateHeader(panel.transform);
            titleText = CreateTitleText(header.transform);
            messageText = CreateMessageText(panel.transform);
            updateButton = CreateUpdateButton(panel.transform);

            panelToggle = modalRoot.GetComponent<UiPanelToggle>();
            DisableBackdropClose(backdrop);
        }

        private static void DisableBackdropClose(GameObject backdrop)
        {
            var button = backdrop.GetComponent<Button>();
            if (button != null)
                button.interactable = false;
        }

        private static GameObject CreateBackdrop(Transform parent)
        {
            var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            backdrop.transform.SetParent(parent, false);
            backdrop.transform.SetAsFirstSibling();
            StretchFull(backdrop.GetComponent<RectTransform>());

            var image = backdrop.GetComponent<Image>();
            UiModalStyle.ApplyBackdrop(image);

            var button = backdrop.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;
            return backdrop;
        }

        private static GameObject CreatePanel(Transform parent)
        {
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            UiModalStyle.ApplyPanelRect(panel.GetComponent<RectTransform>());
            UiModalStyle.ApplyPanel(panel.GetComponent<Image>());
            return panel;
        }

        private static GameObject CreateHeader(Transform parent)
        {
            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(parent, false);
            UiModalStyle.ApplyHeaderRect(header.GetComponent<RectTransform>());
            return header;
        }

        private static Text CreateTitleText(Transform header)
        {
            var go = new GameObject("Title Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(header, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(32f, 0f);
            rt.offsetMax = new Vector2(-32f, 0f);

            var text = go.GetComponent<Text>();
            ConfigureText(text, "Требуется обновление", 44, FontStyle.Bold, TextAnchor.MiddleCenter);
            return text;
        }

        private static Text CreateMessageText(Transform panel)
        {
            var go = new GameObject("Message Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(panel, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.08f, 0.35f);
            rt.anchorMax = new Vector2(0.92f, 0.72f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var text = go.GetComponent<Text>();
            ConfigureText(text, string.Empty, 30, FontStyle.Normal, TextAnchor.MiddleCenter);
            return text;
        }

        private static Button CreateUpdateButton(Transform panel)
        {
            var go = new GameObject("Update Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(panel, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.2f, 0.12f);
            rt.anchorMax = new Vector2(0.8f, 0.22f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.2f, 0.55f, 0.95f, 1f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            StretchFull(labelGo.GetComponent<RectTransform>());
            var label = labelGo.GetComponent<Text>();
            ConfigureText(label, "Обновить", 34, FontStyle.Bold, TextAnchor.MiddleCenter);
            return button;
        }

        private static void ConfigureText(Text text, string content, int size, FontStyle style, TextAnchor anchor)
        {
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }
}
