using TapBrawl.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>Синхронизация layout и стилей HUD на сцене Match.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-200)]
    public sealed class MatchHudLayoutApplier : MonoBehaviour
    {
        public const string TopHudBarName = "TopHudBar";
        public const string NoticeBarName = "OpponentNoticeBar";
        public const string PinBarName = "PinBar";
        public const string SkillBarName = "SkillBar";
        public const string EnergyTextName = "SkillEnergyText";

        private void Awake()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null && canvas.isRootCanvas)
                Apply(canvas);
        }

        public static void Apply(Canvas rootCanvas)
        {
            if (rootCanvas == null || !rootCanvas.isRootCanvas)
                return;

            ApplyCanvasScaler(rootCanvas);
            ApplyCameraBackground();
            ApplyBackground(rootCanvas.transform);
            var topBar = EnsureTopHudBar(rootCanvas.transform);
            var noticeBar = EnsureNoticeBar(rootCanvas.transform);
            ReparentHudLabels(rootCanvas.transform, topBar, noticeBar);
            ApplyPlayArea(rootCanvas.transform);
            HideSpawnCenterZone(rootCanvas.transform);
            var skillBar = FindDeep(rootCanvas.transform, SkillBarName);
            if (skillBar != null)
                StyleSkillBar(skillBar);
            var pinBar = FindDeep(rootCanvas.transform, PinBarName);
            if (pinBar != null)
                ReparentPinBar(rootCanvas.transform, pinBar);
            StylePinPanels(rootCanvas.transform);
            WireControllers(rootCanvas);
        }

        public static void ResetSessionFlag() { }

        private static void ApplyCanvasScaler(Canvas canvas)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = MatchHudStyle.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = MatchHudStyle.MatchWidthOrHeight;
        }

        private static void ApplyCameraBackground()
        {
            var cam = Camera.main;
            if (cam != null)
                cam.backgroundColor = MatchHudStyle.CameraBackground;
        }

        private static void ApplyBackground(Transform canvas)
        {
            var bg = FindDeep(canvas, "Background");
            if (bg == null)
                return;
            var img = bg.GetComponent<Image>();
            if (img != null)
            {
                img.color = MatchHudStyle.ScreenBackground;
                img.raycastTarget = false;
            }
            StretchFull(bg);
        }

        private static RectTransform EnsureTopHudBar(Transform canvas)
        {
            var existing = FindDeep(canvas, TopHudBarName);
            if (existing != null)
            {
                ConfigureTopBarShell(existing);
                return existing;
            }

            var go = new GameObject(TopHudBarName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas, false);
            go.transform.SetSiblingIndex(1);
            ConfigureTopBarShell(go.GetComponent<RectTransform>());
            var img = go.GetComponent<Image>();
            img.color = MatchHudStyle.SkillBarPanel;
            img.raycastTarget = false;
            var slice = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
            if (slice != null)
            {
                img.sprite = slice;
                img.type = Image.Type.Sliced;
            }
            return go.GetComponent<RectTransform>();
        }

        private static void ConfigureTopBarShell(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, MatchHudStyle.TopBarHeight);
        }

        private static RectTransform EnsureNoticeBar(Transform canvas)
        {
            var existing = FindDeep(canvas, NoticeBarName);
            if (existing != null)
            {
                ConfigureNoticeBarShell(existing);
                return existing;
            }

            var go = new GameObject(NoticeBarName, typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            go.transform.SetSiblingIndex(2);
            ConfigureNoticeBarShell(go.GetComponent<RectTransform>());
            return go.GetComponent<RectTransform>();
        }

        private static void ConfigureNoticeBarShell(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -MatchHudStyle.TopBarHeight);
            rt.sizeDelta = new Vector2(0f, MatchHudStyle.NoticeBarHeight);
        }

        private static void ReparentHudLabels(Transform canvas, RectTransform topBar, RectTransform noticeBar)
        {
            PlaceInTopBar(FindDeep(canvas, "ScoreText"), topBar, 0f, 0.08f, 0.32f, 0.92f,
                MatchHudStyle.ScoreFontSize, MatchHudStyle.PrimaryText, TextAlignmentOptions.MidlineLeft);
            PlaceInTopBar(FindDeep(canvas, "TimerText"), topBar, 0.32f, 0.08f, 0.68f, 0.92f,
                MatchHudStyle.TimerFontSize, MatchHudStyle.PrimaryText, TextAlignmentOptions.Center);
            PlaceInTopBar(FindDeep(canvas, "ModeLabel"), topBar, 0.68f, 0.52f, 1f, 0.92f,
                MatchHudStyle.SecondaryFontSize, MatchHudStyle.AccentText, TextAlignmentOptions.MidlineRight);
            PlaceInTopBar(FindDeep(canvas, "Opponent Label"), topBar, 0.68f, 0.08f, 1f, 0.48f,
                MatchHudStyle.SecondaryFontSize, MatchHudStyle.AccentText, TextAlignmentOptions.MidlineRight);

            var notice = FindDeep(canvas, "OpponentSkillNoticeText");
            if (notice != null)
            {
                notice.SetParent(noticeBar, false);
                StretchFull(notice);
                EnsureTmp(notice.gameObject, MatchHudStyle.NoticeFontSize, MatchHudStyle.NoticeText,
                    TextAlignmentOptions.Center);
            }
        }

        private static void PlaceInTopBar(
            RectTransform? label,
            RectTransform topBar,
            float anchorMinX,
            float anchorMinY,
            float anchorMaxX,
            float anchorMaxY,
            int fontSize,
            Color color,
            TextAlignmentOptions alignment)
        {
            if (label == null)
                return;
            label.SetParent(topBar, false);
            label.anchorMin = new Vector2(anchorMinX, anchorMinY);
            label.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
            label.offsetMin = Vector2.zero;
            label.offsetMax = Vector2.zero;
            label.pivot = new Vector2(0.5f, 0.5f);
            EnsureTmp(label.gameObject, fontSize, color, alignment);
        }

        private static void ApplyPlayArea(Transform canvas)
        {
            var play = FindDeep(canvas, "PlayArea");
            if (play == null)
                return;
            play.anchorMin = Vector2.zero;
            play.anchorMax = Vector2.one;
            play.pivot = new Vector2(0.5f, 0.5f);
            play.offsetMin = new Vector2(0f, MatchHudStyle.PlayAreaBottomInset);
            play.offsetMax = new Vector2(0f, -MatchHudStyle.PlayAreaTopInset);
        }

        private static void HideSpawnCenterZone(Transform canvas)
        {
            var zone = FindDeep(canvas, "SpawnCenterZone");
            if (zone == null)
                return;
            zone.gameObject.SetActive(false);
            var img = zone.GetComponent<Image>();
            if (img != null)
                img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);
        }

        private static void StyleSkillBar(RectTransform skillBar)
        {
            skillBar.anchorMin = new Vector2(0f, 0f);
            skillBar.anchorMax = new Vector2(1f, 0f);
            skillBar.pivot = new Vector2(0.5f, 0f);
            skillBar.anchoredPosition = Vector2.zero;
            skillBar.sizeDelta = new Vector2(0f, MatchHudStyle.SkillBarHeight);

            var bg = skillBar.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = MatchHudStyle.SkillBarPanel;
                bg.raycastTarget = false;
                var slice = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
                if (slice != null)
                {
                    bg.sprite = slice;
                    bg.type = Image.Type.Sliced;
                }
            }

            var energyBg = FindDeep(skillBar, "EnergyBar_BG");
            if (energyBg != null)
            {
                energyBg.anchorMin = new Vector2(0.04f, 1f);
                energyBg.anchorMax = new Vector2(0.96f, 1f);
                energyBg.pivot = new Vector2(0.5f, 1f);
                energyBg.anchoredPosition = new Vector2(0f, -8f);
                energyBg.sizeDelta = new Vector2(0f, MatchHudStyle.EnergyBarHeight);
                var eImg = energyBg.GetComponent<Image>();
                if (eImg != null)
                    eImg.color = new Color(0.16f, 0.16f, 0.19f, 1f);
            }

            EnsureSkillEnergyText(skillBar);

            PlaceOpenPinButtonInSkillBar(skillBar);

            var slotNames = new[] { "SkillGiantCircles", "SkillRedDeception", "SkillSmokeVeil" };
            var slotX = new[] { -112f, 0f, 112f };
            for (var i = 0; i < slotNames.Length; i++)
            {
                var slot = FindDeep(skillBar, slotNames[i]);
                if (slot == null)
                    continue;
                slot.anchorMin = new Vector2(0.5f, 0.5f);
                slot.anchorMax = new Vector2(0.5f, 0.5f);
                slot.pivot = new Vector2(0.5f, 0.5f);
                slot.anchoredPosition = new Vector2(slotX[i], -36f);
                slot.sizeDelta = new Vector2(MatchHudStyle.SkillSlotSize, MatchHudStyle.SkillSlotSize);
                var status = slot.Find("Text");
                if (status != null)
                    EnsureTmp(status.gameObject, MatchHudStyle.SkillStatusFontSize, MatchHudStyle.PrimaryText,
                        TextAlignmentOptions.Center);
            }

            var pinBarChild = FindDeep(skillBar, PinBarName);
            if (pinBarChild != null && pinBarChild.parent == skillBar)
                ReparentPinBar(skillBar.parent, pinBarChild);
        }

        private static void EnsureSkillEnergyText(RectTransform skillBar)
        {
            var energyBg = FindDeep(skillBar, "EnergyBar_BG");
            if (energyBg == null)
                return;
            var existing = FindDeep(energyBg, EnergyTextName);
            if (existing != null)
            {
                EnsureTmp(existing.gameObject, MatchHudStyle.EnergyFontSize, MatchHudStyle.AccentText,
                    TextAlignmentOptions.Center);
                return;
            }

            var go = new GameObject(EnergyTextName, typeof(RectTransform));
            go.transform.SetParent(energyBg, false);
            StretchFull(go.GetComponent<RectTransform>());
            EnsureTmp(go, MatchHudStyle.EnergyFontSize, MatchHudStyle.AccentText, TextAlignmentOptions.Center);
        }

        private static void PlaceOpenPinButtonInSkillBar(RectTransform skillBar)
        {
            var openBtn = FindDeep(skillBar.parent, "OpenPinPanelButton");
            if (openBtn == null)
                return;

            openBtn.SetParent(skillBar, false);
            openBtn.SetAsFirstSibling();
            openBtn.anchorMin = new Vector2(0f, 0.5f);
            openBtn.anchorMax = new Vector2(0f, 0.5f);
            openBtn.pivot = new Vector2(0f, 0.5f);
            openBtn.anchoredPosition = new Vector2(16f, -36f);
            openBtn.sizeDelta = new Vector2(MatchHudStyle.SkillSlotSize, MatchHudStyle.SkillSlotSize);
            openBtn.localScale = Vector3.one;

            var img = openBtn.GetComponent<Image>();
            if (img != null)
                img.color = Color.white;
        }

        private static void ReparentPinBar(Transform canvas, RectTransform pinBar)
        {
            if (pinBar.parent != canvas)
            {
                pinBar.SetParent(canvas, false);
                pinBar.SetAsLastSibling();
            }

            pinBar.anchorMin = new Vector2(0f, 0f);
            pinBar.anchorMax = new Vector2(0f, 0f);
            pinBar.pivot = new Vector2(0f, 0f);
            pinBar.anchoredPosition = Vector2.zero;
            pinBar.sizeDelta = Vector2.zero;
        }

        private static void StylePinPanels(Transform canvas)
        {
            var picker = FindDeep(canvas, "PinPanel");
            if (picker != null)
            {
                picker.anchorMin = new Vector2(0f, 0f);
                picker.anchorMax = new Vector2(0f, 0f);
                picker.pivot = new Vector2(0f, 0f);
                picker.anchoredPosition = new Vector2(16f, MatchHudStyle.SkillBarHeight + 40f);
                picker.sizeDelta = new Vector2(120f, 360f);
                var childImg = picker.Find("Image")?.GetComponent<Image>();
                if (childImg != null)
                {
                    childImg.color = MatchHudStyle.PinPickerPanel;
                    var slice = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
                    if (slice != null)
                    {
                        childImg.sprite = slice;
                        childImg.type = Image.Type.Sliced;
                    }
                }
            }

            var incoming = FindDeep(canvas, "IncomingPinPanel");
            if (incoming != null)
            {
                incoming.anchorMin = new Vector2(1f, 1f);
                incoming.anchorMax = new Vector2(1f, 1f);
                incoming.pivot = new Vector2(1f, 1f);
                incoming.anchoredPosition = new Vector2(-24f, -MatchHudStyle.TopBarHeight - MatchHudStyle.NoticeBarHeight - 24f);
                incoming.sizeDelta = new Vector2(120f, 120f);
            }

            var outgoing = FindDeep(canvas, "OutgoingPanelPanel");
            if (outgoing != null)
            {
                outgoing.anchorMin = new Vector2(0f, 1f);
                outgoing.anchorMax = new Vector2(0f, 1f);
                outgoing.pivot = new Vector2(0f, 1f);
                outgoing.anchoredPosition = new Vector2(24f, -MatchHudStyle.TopBarHeight - MatchHudStyle.NoticeBarHeight - 24f);
                outgoing.sizeDelta = new Vector2(120f, 120f);
            }

            var openBtn = FindDeep(canvas, "OpenPinPanelButton");
            if (openBtn != null)
            {
                var img = openBtn.GetComponent<Image>();
                if (img != null)
                    img.color = Color.white;
            }
        }

        private static void WireControllers(Canvas canvas)
        {
            var match = Object.FindFirstObjectByType<MatchController>();
            var skillBar = FindDeep(canvas.transform, SkillBarName);
            var skillCtrl = skillBar != null ? skillBar.GetComponent<GameplaySkillBarController>() : null;
            if (skillCtrl == null && skillBar != null)
                skillCtrl = skillBar.gameObject.AddComponent<GameplaySkillBarController>();

            if (match != null)
            {
                var duplicate = match.GetComponent<GameplaySkillBarController>();
                if (duplicate != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        Object.DestroyImmediate(duplicate);
                    else
#endif
                        Object.Destroy(duplicate);
                }
            }

            if (skillCtrl == null || match == null)
                return;

            skillCtrl.BindFromScene(match, canvas.transform);
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

        private static TMP_Text EnsureTmp(GameObject go, int fontSize, Color color, TextAlignmentOptions alignment)
        {
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                var legacy = go.GetComponent<Text>();
                var prevText = legacy != null ? legacy.text : string.Empty;
                var prevRaycast = legacy != null && legacy.raycastTarget;
                if (legacy != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        Object.DestroyImmediate(legacy);
                    else
#endif
                        Object.Destroy(legacy);
                }

                tmp = go.GetComponent<TextMeshProUGUI>();
                if (tmp == null)
                    tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text = prevText;
                tmp.raycastTarget = prevRaycast;
            }

            var font = TMP_Settings.defaultFontAsset;
            if (font == null)
                font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font != null)
                tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.outlineWidth = 0.12f;
            tmp.outlineColor = new Color32(0, 0, 0, 140);
            return tmp;
        }
    }
}
