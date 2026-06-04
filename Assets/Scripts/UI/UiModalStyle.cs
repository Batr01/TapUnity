using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>Общие параметры оформления модальных окон.</summary>
    public static class UiModalStyle
    {
        public static readonly Color BackdropColor = new(0f, 0f, 0f, 0.55f);
        public static readonly Color PanelColor = new(0.12f, 0.12f, 0.14f, 0.98f);

        public static readonly Vector2 PanelAnchorMin = new(0.08f, 0.12f);
        public static readonly Vector2 PanelAnchorMax = new(0.92f, 0.88f);
        public const float HeaderHeight = 100f;
        public static readonly Vector2 BackButtonSize = new(100f, 100f);
        public static readonly Vector2 BackButtonPosition = new(16f, 0f);
        public static readonly Vector2 HeaderSaveButtonSize = new(80f, 80f);
        public static readonly Vector2 HeaderSaveButtonPosition = new(-16f, 0f);

        public static readonly Color ProfileBlockColor = new(0.2f, 0.2f, 0.24f, 1f);
        public static readonly Color ProfilePrimaryTextColor = Color.white;
        public static readonly Color ProfileAccentTextColor = new(0.55f, 0.82f, 1f, 1f);
        public const int ProfileBodyFontSize = 28;
        public const float ProfileScrollPadding = 16f;
        public const float ProfileBlockSpacing = 12f;

        public static readonly Color SkillsSectionColor = new(0.2f, 0.2f, 0.24f, 1f);
        public static readonly Color SkillsHeaderColor = new(0.16f, 0.16f, 0.19f, 1f);
        public const float SkillsModalPadding = 16f;
        public const float SkillsSectionSpacing = 12f;
        public const float SkillsLoadoutHeight = 200f;
        public const float SkillsRowHeight = 96f;
        public const int SkillsTitleFontSize = 40;

        public static void ApplyPanelRect(RectTransform rt)
        {
            rt.anchorMin = PanelAnchorMin;
            rt.anchorMax = PanelAnchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void ApplyHeaderRect(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, HeaderHeight);
            rt.anchoredPosition = Vector2.zero;
        }

        public static void ApplyBackButtonRect(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = BackButtonSize;
            rt.anchoredPosition = BackButtonPosition;
        }

        public static void ApplyHeaderSaveButtonRect(RectTransform rt)
        {
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = HeaderSaveButtonSize;
            rt.anchoredPosition = HeaderSaveButtonPosition;
        }

        public static void ApplyBackdrop(Image? image)
        {
            if (image == null)
                return;

            image.color = BackdropColor;
            image.raycastTarget = true;
        }

        public static void ApplyPanel(Image? image)
        {
            if (image == null)
                return;

            image.color = PanelColor;
            image.raycastTarget = true;
        }

        public static void PrepareBackButton(Button button)
        {
            foreach (var graphic in button.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.gameObject == button.gameObject)
                    continue;

                graphic.raycastTarget = false;
            }
        }

        /// <summary>Единый chrome для модалок лобби (backdrop, панель, кнопки назад).</summary>
        public static void ApplyLobbyModalChrome(Transform modalRoot)
        {
            var backdrop = modalRoot.Find("Backdrop");
            if (backdrop != null)
                ApplyBackdrop(backdrop.GetComponent<Image>());

            foreach (var panelName in new[] { "Panel", "SkillsScreen", "Content" })
            {
                var panel = modalRoot.Find(panelName);
                if (panel == null)
                    continue;

                ApplyPanelRect((RectTransform)panel);
                var panelImg = panel.GetComponent<Image>();
                if (panelImg != null)
                    ApplyPanel(panelImg);
            }

            foreach (var t in modalRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Header")
                    ApplyHeaderRect((RectTransform)t);
            }

            foreach (var btn in modalRoot.GetComponentsInChildren<Button>(true))
            {
                if (btn.gameObject.name != "Back Button")
                    continue;
                ApplyBackButtonRect((RectTransform)btn.transform);
                PrepareBackButton(btn);
            }
        }
    }
}
