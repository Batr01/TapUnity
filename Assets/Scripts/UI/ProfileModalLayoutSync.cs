using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>Приводит ProfilePanel к единому стилю модалок лобби.</summary>
    [DisallowMultipleComponent]
    public sealed class ProfileModalLayoutSync : MonoBehaviour
    {
        private static readonly string[] ContentBlockOrder =
        {
            "BlockProfil",
            "BlockRanked",
            "BlockCoint",
            "BlockLevel",
            "BlockStatistic",
            "BlockMatchHistory",
            "BlockStatus",
        };

        private static readonly (string Name, float Height)[] BlockHeights =
        {
            ("BlockProfil", 280f),
            ("BlockRanked", 160f),
            ("BlockCoint", 132f),
            ("BlockLevel", 140f),
            ("BlockStatistic", 280f),
            ("BlockMatchHistory", 132f),
            ("BlockStatus", 64f),
        };

        [SerializeField] private Sprite? backArrowSprite;

        private void Awake() => Apply();

        public void Apply()
        {
            EnsureBackdrop();
            var panel = EnsurePanel();
            var header = EnsureHeader(panel);
            UiModalStyle.ApplyLobbyModalChrome(transform);
            ConfigureScrollArea(panel);
            ConfigureContentBlocks(panel, header);
        }

        private void EnsureBackdrop()
        {
            var backdrop = transform.Find("Backdrop");
            if (backdrop == null)
            {
                var go = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(transform, false);
                go.transform.SetAsFirstSibling();
                StretchFull(go.GetComponent<RectTransform>());
                backdrop = go.transform;
            }

            WireBackdropClose(backdrop);
        }

        private static void WireBackdropClose(Transform backdrop)
        {
            var img = backdrop.GetComponent<Image>();
            if (img == null)
                img = backdrop.gameObject.AddComponent<Image>();
            UiModalStyle.ApplyBackdrop(img);

            var btn = backdrop.GetComponent<Button>();
            if (btn == null)
                btn = backdrop.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = img;

            btn.onClick.RemoveAllListeners();
            var toggle = Object.FindFirstObjectByType<ProfilePanelToggle>();
            if (toggle != null)
                btn.onClick.AddListener(toggle.Hide);
        }

        private Transform EnsurePanel()
        {
            var panel = transform.Find("Panel");
            if (panel != null)
            {
                UiModalStyle.ApplyPanelRect((RectTransform)panel);
                var panelImg = panel.GetComponent<Image>();
                if (panelImg != null)
                    UiModalStyle.ApplyPanel(panelImg);
                return panel;
            }

            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            UiModalStyle.ApplyPanelRect(rt);
            UiModalStyle.ApplyPanel(go.GetComponent<Image>());

            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name is "Backdrop" or "Panel")
                    continue;
                child.SetParent(go.transform, false);
            }

            return go.transform;
        }

        private Transform EnsureHeader(Transform panel)
        {
            var header = panel.Find("Header");
            if (header == null)
            {
                var go = new GameObject("Header", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(panel, false);
                go.transform.SetAsFirstSibling();
                header = go.transform;
            }

            UiModalStyle.ApplyHeaderRect((RectTransform)header);
            var headerImg = header.GetComponent<Image>();
            if (headerImg == null)
                headerImg = header.gameObject.AddComponent<Image>();
            UiModalStyle.ApplyPanel(headerImg);

            if (header.Find("Title Text") == null)
            {
                var titleGo = new GameObject("Title Text", typeof(RectTransform), typeof(Text));
                titleGo.transform.SetParent(header, false);
                var rt = titleGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(120f, 0f);
                rt.offsetMax = new Vector2(-120f, 0f);
                var text = titleGo.GetComponent<Text>();
                text.text = "Профиль";
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 44;
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
                text.raycastTarget = false;
            }

            var back = header.Find("Back Button");
            if (back == null)
            {
                var backGo = new GameObject("Back Button", typeof(RectTransform), typeof(Image), typeof(Button));
                backGo.transform.SetParent(header, false);
                back = backGo.transform;
                var img = backGo.GetComponent<Image>();
                if (backArrowSprite != null)
                    img.sprite = backArrowSprite;
                img.color = Color.white;
                backGo.GetComponent<Button>().targetGraphic = img;
            }

            UiModalStyle.ApplyBackButtonRect((RectTransform)back);
            var backBtn = back.GetComponent<Button>();
            if (backBtn != null)
            {
                UiModalStyle.PrepareBackButton(backBtn);
                backBtn.onClick.RemoveAllListeners();
                var toggle = Object.FindFirstObjectByType<ProfilePanelToggle>();
                if (toggle != null)
                    backBtn.onClick.AddListener(toggle.Hide);
            }

            return header;
        }

        private static void ConfigureScrollArea(Transform panel)
        {
            var scroll = panel.Find("Scroll View");
            if (scroll == null)
                return;

            var rt = (RectTransform)scroll;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0f, -UiModalStyle.HeaderHeight);
            rt.anchoredPosition = Vector2.zero;

            var scrollImg = scroll.GetComponent<Image>();
            if (scrollImg != null)
            {
                scrollImg.color = new Color(0f, 0f, 0f, 0f);
                scrollImg.raycastTarget = false;
            }

            var viewport = scroll.Find("Viewport");
            if (viewport != null)
            {
                var vpRt = (RectTransform)viewport;
                vpRt.anchorMin = Vector2.zero;
                vpRt.anchorMax = Vector2.one;
                vpRt.offsetMin = Vector2.zero;
                vpRt.offsetMax = Vector2.zero;
                vpRt.pivot = new Vector2(0f, 1f);
            }
        }

        private static void ConfigureContentBlocks(Transform panel, Transform header)
        {
            var scroll = panel.Find("Scroll View");
            var content = scroll != null ? scroll.Find("Viewport/Content") : null;
            if (content == null)
                content = scroll?.Find("Content");
            if (content == null)
                return;

            var contentRt = (RectTransform)content;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
                vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(
                (int)UiModalStyle.ProfileScrollPadding,
                (int)UiModalStyle.ProfileScrollPadding,
                (int)UiModalStyle.ProfileScrollPadding,
                (int)UiModalStyle.ProfileScrollPadding);
            vlg.spacing = UiModalStyle.ProfileBlockSpacing;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            MoveSaveButtonToHeader(content, header);
            EnsureBlockMatchHistory(content);

            var blockTitle = content.Find("BlockTitle");
            if (blockTitle != null)
                blockTitle.gameObject.SetActive(false);

            for (var i = 0; i < ContentBlockOrder.Length; i++)
            {
                var block = content.Find(ContentBlockOrder[i]);
                if (block != null)
                    block.SetSiblingIndex(i);
            }

            foreach (var (name, height) in BlockHeights)
            {
                var block = content.Find(name);
                if (block == null)
                    continue;
                var blockRt = (RectTransform)block;
                ConfigureBlock(blockRt, height);
                ConfigureBlockInterior(blockRt);
                ApplyBlockTypography(blockRt);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
            if (scroll != null)
            {
                var scrollRect = scroll.GetComponent<ScrollRect>();
                if (scrollRect != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.viewport);
            }
        }

        private static void EnsureBlockMatchHistory(Transform content)
        {
            var block = content.Find("BlockMatchHistory");
            if (block == null)
            {
                var go = new GameObject(
                    "BlockMatchHistory",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(VerticalLayoutGroup));
                go.transform.SetParent(content, false);
                block = go.transform;
                var img = go.GetComponent<Image>();
                img.color = UiModalStyle.ProfileBlockColor;
                img.raycastTarget = true;
            }

            if (block.Find("MatchHistoryTitle") == null)
            {
                var titleGo = new GameObject("MatchHistoryTitle", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
                titleGo.transform.SetParent(block, false);
                var title = titleGo.GetComponent<Text>();
                title.text = "Последние игры";
                title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                title.fontSize = UiModalStyle.ProfileBodyFontSize;
                title.fontStyle = FontStyle.Bold;
                title.color = UiModalStyle.ProfilePrimaryTextColor;
                title.alignment = TextAnchor.MiddleLeft;
                title.raycastTarget = false;
                var titleLe = titleGo.GetComponent<LayoutElement>();
                titleLe.preferredHeight = 36f;
                titleLe.minHeight = 36f;
            }

            var container = block.Find("MatchHistoryContainer");
            if (container == null)
            {
                var containerGo = new GameObject(
                    "MatchHistoryContainer",
                    typeof(RectTransform),
                    typeof(HorizontalLayoutGroup),
                    typeof(LayoutElement));
                containerGo.transform.SetParent(block, false);
                container = containerGo.transform;
            }

            ConfigureMatchHistoryContainer(container);
        }

        private static void ConfigureMatchHistoryContainer(Transform container)
        {
            if (container.TryGetComponent<VerticalLayoutGroup>(out var oldVlg))
                Destroy(oldVlg);

            var hlg = container.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null)
                hlg = container.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var containerLe = container.GetComponent<LayoutElement>();
            if (containerLe == null)
                containerLe = container.gameObject.AddComponent<LayoutElement>();
            containerLe.flexibleHeight = 1f;
            containerLe.minHeight = 52f;
            containerLe.preferredHeight = 52f;
        }

        private static void MoveSaveButtonToHeader(Transform content, Transform header)
        {
            var save = content.Find("BlockTitle/BtnSave");
            if (save == null)
                save = content.parent?.Find("BlockTitle/BtnSave");
            if (save == null)
            {
                foreach (var t in content.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "BtnSave")
                    {
                        save = t;
                        break;
                    }
                }
            }

            if (save == null)
                return;

            save.SetParent(header, false);
            var rt = (RectTransform)save;
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(80f, 80f);
            rt.anchoredPosition = new Vector2(-16f, 0f);
        }

        private static void ConfigureBlock(RectTransform block, float height)
        {
            block.anchorMin = new Vector2(0f, 1f);
            block.anchorMax = new Vector2(1f, 1f);
            block.pivot = new Vector2(0.5f, 0.5f);
            block.anchoredPosition = Vector2.zero;
            block.sizeDelta = new Vector2(0f, height);

            var le = block.GetComponent<LayoutElement>();
            if (le == null)
                le = block.gameObject.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 1f;
            le.ignoreLayout = false;

            var img = block.GetComponent<Image>();
            if (img != null)
            {
                img.color = UiModalStyle.ProfileBlockColor;
                img.raycastTarget = true;
            }
        }

        private static void ConfigureBlockInterior(RectTransform block)
        {
            var hlg = block.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                ApplyHorizontalGroup(hlg);
                for (var i = 0; i < block.childCount; i++)
                    PrepareLayoutChild((RectTransform)block.GetChild(i), GuessPreferredHeight((RectTransform)block.GetChild(i)));
                LayoutRebuilder.ForceRebuildLayoutImmediate(block);
                return;
            }

            var vlg = block.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
                vlg = block.gameObject.AddComponent<VerticalLayoutGroup>();
            ApplyVerticalGroup(vlg);

            for (var i = 0; i < block.childCount; i++)
                PrepareLayoutChild((RectTransform)block.GetChild(i), GuessPreferredHeight((RectTransform)block.GetChild(i)));

            LayoutRebuilder.ForceRebuildLayoutImmediate(block);
        }

        private static void ApplyVerticalGroup(VerticalLayoutGroup vlg)
        {
            vlg.padding = new RectOffset(16, 16, 12, 12);
            vlg.spacing = 8;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
        }

        private static void ApplyHorizontalGroup(HorizontalLayoutGroup hlg)
        {
            hlg.padding = new RectOffset(8, 8, 8, 8);
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
        }

        private static void ApplyBlockTypography(RectTransform block)
        {
            foreach (var text in block.GetComponentsInChildren<Text>(true))
                ApplyProfileText(text);

            foreach (var field in block.GetComponentsInChildren<InputField>(true))
                ApplyProfileInputField(field);

            for (var i = 0; i < block.childCount; i++)
            {
                var child = (RectTransform)block.GetChild(i);
                PrepareLayoutChild(child, GuessPreferredHeight(child));
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(block);
        }

        private static void ApplyProfileText(Text text)
        {
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (text.fontSize > UiModalStyle.ProfileBodyFontSize)
                text.fontSize = UiModalStyle.ProfileBodyFontSize;

            text.color = ResolveProfileTextColor(text.color);
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
        }

        private static void ApplyProfileInputField(InputField field)
        {
            if (field.textComponent != null)
            {
                field.textComponent.color = new Color(0.12f, 0.12f, 0.14f, 1f);
                field.textComponent.fontSize = Mathf.Min(field.textComponent.fontSize, 24);
            }

            if (field.placeholder is Text placeholder)
            {
                placeholder.color = new Color(0.45f, 0.48f, 0.52f, 1f);
                placeholder.fontSize = Mathf.Min(placeholder.fontSize, 22);
            }
        }

        private static Color ResolveProfileTextColor(Color current)
        {
            if (IsDefaultUnityLabelColor(current))
                return UiModalStyle.ProfilePrimaryTextColor;

            var luminance = 0.299f * current.r + 0.587f * current.g + 0.114f * current.b;
            if (luminance < 0.35f)
                return UiModalStyle.ProfilePrimaryTextColor;

            if (current.b > 0.55f && current.g > 0.3f)
                return UiModalStyle.ProfileAccentTextColor;

            return current;
        }

        private static bool IsDefaultUnityLabelColor(Color color)
        {
            return Mathf.Abs(color.r - 0.19607843f) < 0.03f
                && Mathf.Abs(color.g - 0.19607843f) < 0.03f
                && Mathf.Abs(color.b - 0.19607843f) < 0.03f;
        }

        private static float GuessPreferredHeight(RectTransform child)
        {
            if (child.GetComponent<InputField>() != null)
                return 52f;
            if (child.GetComponent<Button>() != null)
                return 44f;
            if (child.TryGetComponent<Text>(out var text))
                return Mathf.Max(40f, text.fontSize + 12f);
            if (child.GetComponent<Slider>() != null)
                return 28f;
            if (child.GetComponent<HorizontalLayoutGroup>() != null
                || child.GetComponent<VerticalLayoutGroup>() != null)
                return 52f;
            return 40f;
        }

        private static void PrepareLayoutChild(RectTransform child, float preferredHeight)
        {
            child.anchorMin = new Vector2(0f, 1f);
            child.anchorMax = new Vector2(1f, 1f);
            child.pivot = new Vector2(0.5f, 1f);
            child.anchoredPosition = Vector2.zero;
            child.sizeDelta = new Vector2(0f, preferredHeight);

            var le = child.GetComponent<LayoutElement>();
            if (le == null)
                le = child.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            le.minHeight = preferredHeight;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 1f;
            le.ignoreLayout = false;

            if (child.GetComponent<HorizontalLayoutGroup>() is { } hlg)
            {
                ApplyHorizontalGroup(hlg);
                for (var i = 0; i < child.childCount; i++)
                {
                    var nested = (RectTransform)child.GetChild(i);
                    nested.anchorMin = new Vector2(0f, 0.5f);
                    nested.anchorMax = new Vector2(0f, 0.5f);
                    nested.pivot = new Vector2(0.5f, 0.5f);
                    var nestedLe = nested.GetComponent<LayoutElement>();
                    if (nestedLe == null)
                        nestedLe = nested.gameObject.AddComponent<LayoutElement>();
                    nestedLe.preferredWidth = nested.GetComponent<Button>() != null ? 120f : -1;
                    nestedLe.flexibleWidth = nested.GetComponent<Text>() != null ? 1f : 0f;
                    nestedLe.preferredHeight = 40f;
                }
            }
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
