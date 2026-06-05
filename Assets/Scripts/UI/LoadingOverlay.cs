using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>Полноэкранный оверлей загрузки: блокирует ввод и показывает статус.</summary>
    public sealed class LoadingOverlay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup? canvasGroup;
        [SerializeField] private Text? statusText;
        [SerializeField] private RectTransform? spinner;
        [SerializeField] private float spinnerSpeed = 220f;

        private bool _visible;

        public static LoadingOverlay EnsureOnCanvas(Transform canvasTransform)
        {
            var existing = canvasTransform.GetComponentInChildren<LoadingOverlay>(true);
            if (existing != null)
                return existing;

            var root = new GameObject(
                "LoadingOverlay",
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(LoadingOverlay));
            root.transform.SetParent(canvasTransform, false);
            StretchFull(root.GetComponent<RectTransform>());

            var overlay = root.GetComponent<LoadingOverlay>();
            overlay.BuildUi();
            root.SetActive(false);
            return overlay;
        }

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            SetVisible(false);
        }

        public void Show(string message = "Загрузка…")
        {
            SetMessage(message);
            SetVisible(true);
        }

        public void Hide() => SetVisible(false);

        public void SetMessage(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        private void Update()
        {
            if (!_visible || spinner == null)
                return;

            spinner.Rotate(0f, 0f, -spinnerSpeed * Time.unscaledDeltaTime);
        }

        private void SetVisible(bool visible)
        {
            _visible = visible;
            gameObject.SetActive(visible);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.blocksRaycasts = visible;
                canvasGroup.interactable = visible;
            }
        }

        public void BuildUi()
        {
            var backdrop = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.62f);
            backdrop.raycastTarget = true;

            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(transform, false);
            StretchFull(content.GetComponent<RectTransform>());
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 28f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var spinnerGo = new GameObject("Spinner", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            spinnerGo.transform.SetParent(content.transform, false);
            spinner = spinnerGo.GetComponent<RectTransform>();
            spinner.sizeDelta = new Vector2(72f, 72f);
            var spinnerImg = spinnerGo.GetComponent<Image>();
            spinnerImg.color = new Color(1f, 1f, 1f, 0.92f);
            spinnerImg.raycastTarget = false;
            var spinnerLe = spinnerGo.GetComponent<LayoutElement>();
            spinnerLe.minWidth = 72f;
            spinnerLe.minHeight = 72f;
            spinnerLe.preferredWidth = 72f;
            spinnerLe.preferredHeight = 72f;

            var statusGo = new GameObject("StatusText", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            statusGo.transform.SetParent(content.transform, false);
            statusText = statusGo.GetComponent<Text>();
            statusText.font = UiFont();
            statusText.text = "Загрузка…";
            statusText.fontSize = 30;
            statusText.color = Color.white;
            statusText.alignment = TextAnchor.MiddleCenter;
            var statusLe = statusGo.GetComponent<LayoutElement>();
            statusLe.minHeight = 48f;
            statusLe.preferredHeight = 48f;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static Font UiFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f != null)
                return f;
            f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f != null ? f : Font.CreateDynamicFontFromOSFont("Arial", 16);
        }
    }
}
