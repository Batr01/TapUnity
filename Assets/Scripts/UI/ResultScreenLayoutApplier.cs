using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>Синхронизация layout и стилей экрана Result.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-200)]
    public sealed class ResultScreenLayoutApplier : MonoBehaviour
    {
        public const string ContentPanelName = "ResultContentPanel";
        public const string BadgeChipName = "BadgeChip";
        public const string DetailsScrollName = "DetailsScroll";

        private void Awake()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null && canvas.isRootCanvas)
                Apply(canvas);
        }

        public static void TryRebuildLayout(Transform? canvasRoot)
        {
            if (canvasRoot == null)
                return;

            var panel = FindDeep(canvasRoot, ContentPanelName);
            if (panel == null)
                return;

            var details = FindDeep(panel, "detailsText");
            if (details != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(details);

            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        }

        public static void Apply(Canvas rootCanvas)
        {
            if (rootCanvas == null || !rootCanvas.isRootCanvas)
                return;

            ApplyCanvasScaler(rootCanvas);
            ApplyBackground(rootCanvas.transform);
            StyleStatusText(rootCanvas.transform);

            var panel = EnsureContentPanel(rootCanvas.transform);
            var headline = FindDeep(rootCanvas.transform, "headlineText");
            var badge = FindDeep(rootCanvas.transform, "performanceBadgeText");
            var details = FindDeep(rootCanvas.transform, "detailsText");

            if (headline != null)
                headline.SetParent(panel, false);
            if (badge != null)
                WrapBadgeChip(panel, badge);
            if (details != null)
                WrapDetailsScroll(panel, details);

            ConfigureContentPanel(panel);
            StyleHeadline(headline);
            StyleBadge(badge);
            StyleDetails(details);
            StyleBottomButtons(rootCanvas.transform);

            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        }

        private static void ApplyCanvasScaler(Canvas canvas)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                return;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ResultScreenStyle.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = ResultScreenStyle.MatchWidthOrHeight;
        }

        private static void ApplyBackground(Transform canvas)
        {
            var bg = FindDeep(canvas, "Background");
            if (bg == null)
                return;

            var img = bg.GetComponent<Image>();
            if (img != null)
            {
                img.color = ResultScreenStyle.ScreenBackground;
                img.raycastTarget = false;
            }

            StretchFull(bg);
        }

        private static void StyleStatusText(Transform canvas)
        {
            var status = FindDeep(canvas, "statusText");
            if (status == null)
                return;

            var text = status.GetComponent<Text>();
            if (text == null)
                return;

            text.fontSize = ResultScreenStyle.StatusFontSize;
            text.color = ResultScreenStyle.PrimaryText;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var rt = status;
            rt.anchorMin = new Vector2(0.06f, 0.84f);
            rt.anchorMax = new Vector2(0.94f, 0.92f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static RectTransform EnsureContentPanel(Transform canvas)
        {
            var existing = FindDeep(canvas, ContentPanelName);
            if (existing != null)
                return existing;

            var go = new GameObject(ContentPanelName, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            go.transform.SetParent(canvas, false);
            go.transform.SetSiblingIndex(1);

            var img = go.GetComponent<Image>();
            img.color = ResultScreenStyle.PanelColor;
            img.raycastTarget = false;
            ApplySlicedSprite(img);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(ResultScreenStyle.PanelHorizontalInset, ResultScreenStyle.PanelBottomAnchor);
            rt.anchorMax = new Vector2(1f - ResultScreenStyle.PanelHorizontalInset, ResultScreenStyle.PanelTopAnchor);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);

            return rt;
        }

        private static void ConfigureContentPanel(RectTransform panel)
        {
            var vlg = panel.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(
                (int)ResultScreenStyle.PanelPadding,
                (int)ResultScreenStyle.PanelPadding,
                (int)ResultScreenStyle.PanelPadding,
                (int)ResultScreenStyle.PanelPadding);
            vlg.spacing = ResultScreenStyle.SectionSpacing;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
        }

        private static void WrapBadgeChip(RectTransform panel, RectTransform badge)
        {
            var chip = panel.Find(BadgeChipName) as RectTransform;
            if (chip == null)
            {
                var chipGo = new GameObject(BadgeChipName, typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                chip = chipGo.GetComponent<RectTransform>();
                chip.SetParent(panel, false);

                var chipImg = chipGo.GetComponent<Image>();
                chipImg.color = ResultScreenStyle.BadgeChipBackground;
                chipImg.raycastTarget = false;
                ApplySlicedSprite(chipImg);

                var hlg = chipGo.GetComponent<HorizontalLayoutGroup>();
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
                hlg.padding = new RectOffset(24, 24, 8, 8);

                var le = chipGo.GetComponent<LayoutElement>();
                le.preferredHeight = ResultScreenStyle.BadgeChipHeight;
            }

            badge.SetParent(chip, false);
            StretchLayoutChild(badge);

            var badgeLe = badge.GetComponent<LayoutElement>();
            if (badgeLe == null)
                badgeLe = badge.gameObject.AddComponent<LayoutElement>();
            badgeLe.preferredHeight = ResultScreenStyle.BadgeChipHeight - 16f;
        }

        private static void WrapDetailsScroll(RectTransform panel, RectTransform details)
        {
            var scrollRoot = panel.Find(DetailsScrollName) as RectTransform;
            if (scrollRoot == null)
            {
                var scrollGo = new GameObject(DetailsScrollName, typeof(RectTransform), typeof(ScrollRect), typeof(LayoutElement));
                scrollRoot = scrollGo.GetComponent<RectTransform>();
                scrollRoot.SetParent(panel, false);

                var scrollLe = scrollGo.GetComponent<LayoutElement>();
                scrollLe.flexibleHeight = 1f;
                scrollLe.preferredHeight = ResultScreenStyle.DetailsMaxHeight;
                scrollLe.minHeight = 200f;

                var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
                viewportGo.transform.SetParent(scrollRoot, false);
                var viewport = viewportGo.GetComponent<RectTransform>();
                StretchFull(viewport);
                var viewportImg = viewportGo.GetComponent<Image>();
                viewportImg.color = new Color(1f, 1f, 1f, 0.01f);
                viewportImg.raycastTarget = true;
                viewportGo.GetComponent<Mask>().showMaskGraphic = false;

                var contentGo = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
                contentGo.transform.SetParent(viewport, false);
                var content = contentGo.GetComponent<RectTransform>();
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(0.5f, 1f);
                content.anchoredPosition = Vector2.zero;
                content.sizeDelta = new Vector2(0f, 0f);

                var fitter = contentGo.GetComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var scroll = scrollGo.GetComponent<ScrollRect>();
                scroll.content = content;
                scroll.viewport = viewport;
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.scrollSensitivity = 24f;

                StretchLayoutChild(scrollRoot);
            }

            var contentRoot = scrollRoot.Find("Viewport/Content") as RectTransform;
            if (contentRoot != null)
                details.SetParent(contentRoot, false);

            StretchLayoutChild(details);

            var detailsFitter = details.GetComponent<ContentSizeFitter>();
            if (detailsFitter == null)
                detailsFitter = details.gameObject.AddComponent<ContentSizeFitter>();
            detailsFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            detailsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var detailsLe = details.GetComponent<LayoutElement>();
            if (detailsLe == null)
                detailsLe = details.gameObject.AddComponent<LayoutElement>();
            detailsLe.minHeight = 120f;
        }

        private static void StyleHeadline(RectTransform? headline)
        {
            if (headline == null)
                return;

            var text = headline.GetComponent<Text>();
            if (text == null)
                return;

            text.fontSize = ResultScreenStyle.HeadlineFontSize;
            text.fontStyle = FontStyle.Bold;
            text.color = ResultScreenStyle.PrimaryText;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = 1f;

            var le = headline.GetComponent<LayoutElement>();
            if (le == null)
                le = headline.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = ResultScreenStyle.HeadlineMinHeight;
        }

        private static void StyleBadge(RectTransform? badge)
        {
            if (badge == null)
                return;

            var text = badge.GetComponent<Text>();
            if (text == null)
                return;

            text.fontSize = ResultScreenStyle.BadgeFontSize;
            text.fontStyle = FontStyle.Bold;
            text.color = ResultScreenStyle.BadgeText;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = 1.15f;
        }

        private static void StyleDetails(RectTransform? details)
        {
            if (details == null)
                return;

            var text = details.GetComponent<Text>();
            if (text == null)
                return;

            text.fontSize = ResultScreenStyle.DetailsFontSize;
            text.fontStyle = FontStyle.Normal;
            text.color = ResultScreenStyle.PrimaryText;
            text.alignment = TextAnchor.UpperCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = 1.2f;
            text.supportRichText = true;
        }

        private static void StyleBottomButtons(Transform canvas)
        {
            foreach (var buttonName in new[] { "backToLobbyButton", "findNewGameButton", "backToMenuButton" })
            {
                var btn = FindDeep(canvas, buttonName);
                if (btn == null)
                    continue;

                btn.anchorMin = new Vector2(ResultScreenStyle.PanelHorizontalInset, 0.06f);
                btn.anchorMax = new Vector2(1f - ResultScreenStyle.PanelHorizontalInset, 0.06f);
                btn.pivot = new Vector2(0.5f, 0.5f);
                btn.sizeDelta = new Vector2(0f, ResultScreenStyle.BottomButtonHeight);
                btn.anchoredPosition = Vector2.zero;

                foreach (var label in btn.GetComponentsInChildren<Text>(true))
                {
                    label.fontSize = ResultScreenStyle.DetailsFontSize;
                    label.fontStyle = FontStyle.Bold;
                    label.alignment = TextAnchor.MiddleCenter;
                }
            }
        }

        private static void ApplySlicedSprite(Image img)
        {
            var slice = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
            if (slice == null)
                return;

            img.sprite = slice;
            img.type = Image.Type.Sliced;
        }

        private static void StretchLayoutChild(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        private static RectTransform? FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                    return (RectTransform)t;
            }

            return null;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
