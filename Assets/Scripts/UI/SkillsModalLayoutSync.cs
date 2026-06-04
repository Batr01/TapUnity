using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>
    /// SkillsModal: inset-панель и горизонтальные строки скилла (иконка, имя, апгрейд, инфо).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkillsModalLayoutSync : MonoBehaviour
    {
        private static readonly string[] PanelOrder =
        {
            "Top Panel",
            "Skill Panel",
            "Loadout Panel",
            "SkillLoadoutPickModal",
        };

        private const float RowIconSize = 72f;
        private const float RowActionButtonWidth = 84f;
        private const float RowActionButtonHeight = 64f;

        private void Awake() => Apply();

        private void OnEnable()
        {
            if (isActiveAndEnabled)
                StartCoroutine(RefreshLayoutNextFrame());
        }

        public void Apply()
        {
            var content = transform.Find("Content");
            var screen = transform.Find("SkillsScreen");

            if (screen == null)
            {
                screen = CreateSkillsScreenRoot().transform;
                screen.SetParent(transform, false);
                screen.SetAsLastSibling();
            }

            MigrateContentToScreen(transform, content, screen);
            ReorderPanels(screen);
            ConfigureScreenRect(screen);
            RemoveSceneNavigation(screen);

            var backdrop = transform.Find("Backdrop");
            if (backdrop != null)
            {
                backdrop.SetAsFirstSibling();
                UiModalStyle.ApplyBackdrop(backdrop.GetComponent<Image>());
            }

            screen.SetAsLastSibling();

            EnsureSkillsPanelBackground(screen);
            ConfigureTopPanel(screen.Find("Top Panel"));
            ConfigureSkillPanel(screen.Find("Skill Panel"));
            ConfigureLoadoutPanel(screen.Find("Loadout Panel"));
            ApplyModalChromeWithoutTopPanelHeader(transform);

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)screen);
        }

        private static void MigrateContentToScreen(Transform modalRoot, Transform? content, Transform screen)
        {
            if (content == null)
            {
                foreach (var panelName in PanelOrder)
                {
                    var panel = modalRoot.Find(panelName);
                    if (panel != null && panel.parent != screen)
                        panel.SetParent(screen, false);
                }

                return;
            }

            foreach (var panelName in PanelOrder)
            {
                var panel = content.Find(panelName);
                if (panel != null)
                    panel.SetParent(screen, false);
            }

            for (var i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i);
                child.SetParent(screen, false);
            }

            if (Application.isPlaying)
                Object.Destroy(content.gameObject);
            else
                Object.DestroyImmediate(content.gameObject);
        }

        private System.Collections.IEnumerator RefreshLayoutNextFrame()
        {
            yield return null;
            var screen = transform.Find("SkillsScreen");
            if (screen == null)
                yield break;

            ConfigureSkillPanel(screen.Find("Skill Panel"));
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)screen);
        }

        private static void EnsureSkillsPanelBackground(Transform screen)
        {
            UiModalStyle.ApplyPanelRect((RectTransform)screen);
            var img = screen.GetComponent<Image>();
            if (img == null)
                img = screen.gameObject.AddComponent<Image>();
            UiModalStyle.ApplyPanel(img);
        }

        private static GameObject CreateSkillsScreenRoot()
        {
            var go = new GameObject("SkillsScreen", typeof(RectTransform), typeof(VerticalLayoutGroup));
            ConfigureScreenRect(go.transform);
            return go;
        }

        private static void ReorderPanels(Transform screen)
        {
            for (var i = 0; i < PanelOrder.Length; i++)
            {
                var panel = screen.Find(PanelOrder[i]);
                if (panel != null)
                    panel.SetSiblingIndex(i);
            }
        }

        private static void ConfigureScreenRect(Transform screen)
        {
            var rt = (RectTransform)screen;
            rt.localScale = Vector3.one;
            UiModalStyle.ApplyPanelRect(rt);

            var vlg = screen.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
                vlg = screen.gameObject.AddComponent<VerticalLayoutGroup>();

            var pad = (int)UiModalStyle.SkillsModalPadding;
            vlg.padding = new RectOffset(pad, pad, pad, pad);
            vlg.spacing = UiModalStyle.SkillsSectionSpacing;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
        }

        private static void RemoveSceneNavigation(Transform screen)
        {
            var nav = screen.GetComponentInChildren<SkillsSceneNavigationController>(true);
            if (nav != null)
                Object.Destroy(nav);
        }

        private static void ConfigureTopPanel(Transform? topPanel)
        {
            if (topPanel == null)
                return;

            var rt = (RectTransform)topPanel;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, UiModalStyle.HeaderHeight);

            var le = GetOrAddLayoutElement(topPanel.gameObject);
            le.ignoreLayout = false;
            le.minHeight = UiModalStyle.HeaderHeight;
            le.preferredHeight = UiModalStyle.HeaderHeight;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 1f;

            ApplySectionImage(topPanel, UiModalStyle.SkillsHeaderColor);

            var buttonGroup = topPanel.Find("Button Group");
            if (buttonGroup != null)
                buttonGroup.gameObject.SetActive(false);

            var hlg = topPanel.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.padding = new RectOffset(8, 8, 0, 0);
                hlg.spacing = 8f;
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
            }

            var back = topPanel.Find("Back Button");
            if (back != null)
            {
                back.SetAsFirstSibling();
                UiModalStyle.ApplyBackButtonRect((RectTransform)back);
                var backLe = GetOrAddLayoutElement(back.gameObject);
                backLe.preferredWidth = UiModalStyle.BackButtonSize.x;
                backLe.preferredHeight = UiModalStyle.BackButtonSize.y;
                backLe.flexibleWidth = 0f;
                backLe.flexibleHeight = 0f;

                var backBtn = back.GetComponent<Button>();
                if (backBtn != null)
                    UiModalStyle.PrepareBackButton(backBtn);
            }

            var save = topPanel.Find("Save Button");
            if (save != null)
            {
                save.SetAsLastSibling();
                UiModalStyle.ApplyHeaderSaveButtonRect((RectTransform)save);
                var saveLe = GetOrAddLayoutElement(save.gameObject);
                saveLe.preferredWidth = UiModalStyle.HeaderSaveButtonSize.x;
                saveLe.preferredHeight = UiModalStyle.HeaderSaveButtonSize.y;
                saveLe.flexibleWidth = 0f;
                saveLe.flexibleHeight = 0f;

                var saveImg = save.GetComponent<Image>();
                if (saveImg != null)
                    saveImg.preserveAspect = true;

                var saveBtn = save.GetComponent<Button>();
                if (saveBtn != null)
                    UiModalStyle.PrepareBackButton(saveBtn);
            }

            var titleRoot = topPanel.Find("Title");
            if (titleRoot != null)
            {
                var titleLe = GetOrAddLayoutElement(titleRoot.gameObject);
                titleLe.flexibleWidth = 1f;
                titleLe.minWidth = 120f;
            }

            var status = topPanel.Find("Status Text");
            if (status != null)
            {
                if (back != null)
                    status.SetSiblingIndex(back.GetSiblingIndex() + 1);

                var statusLe = GetOrAddLayoutElement(status.gameObject);
                statusLe.flexibleWidth = 1f;
                statusLe.minWidth = 80f;
                statusLe.flexibleHeight = 0f;
                statusLe.preferredHeight = 36f;
            }

            foreach (var text in topPanel.GetComponentsInChildren<Text>(true))
            {
                if (text.gameObject.name is "Title" or "Status Text")
                {
                    text.color = UiModalStyle.ProfilePrimaryTextColor;
                    if (text.gameObject.name == "Title")
                        text.fontSize = UiModalStyle.SkillsTitleFontSize;
                    text.alignment = TextAnchor.MiddleCenter;
                }
            }
        }

        private static void ConfigureSkillPanel(Transform? skillPanel)
        {
            if (skillPanel == null)
                return;

            var rt = (RectTransform)skillPanel;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 560f);

            var le = GetOrAddLayoutElement(skillPanel.gameObject);
            le.ignoreLayout = false;
            le.minHeight = 400f;
            le.preferredHeight = 560f;
            le.flexibleHeight = 1f;
            le.flexibleWidth = 1f;

            ApplySectionImage(skillPanel, UiModalStyle.SkillsSectionColor);

            var grid = skillPanel.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 1;
                grid.padding = new RectOffset(8, 8, 8, 8);
                grid.spacing = new Vector2(0f, 8f);
                grid.childAlignment = TextAnchor.UpperCenter;
                UpdateSkillGridCellSize(skillPanel, grid);
            }

            foreach (Transform child in skillPanel)
            {
                if (!child.name.StartsWith("Skill"))
                    continue;

                ConfigureSkillUpgradeRow(child);
            }
        }

        private static void ConfigureSkillUpgradeRow(Transform row)
        {
            var rowImg = row.GetComponent<Image>();
            if (rowImg != null)
                rowImg.color = new Color(0.26f, 0.28f, 0.32f, 1f);

            ReplaceVerticalWithHorizontal(row.gameObject);

            var icon = row.Find("Skill Icon Image");
            var name = row.Find("Name Text");
            var buttons = row.Find("Button Group");

            if (icon != null)
                icon.SetSiblingIndex(0);
            if (name != null)
                name.SetSiblingIndex(1);
            if (buttons != null)
                buttons.SetSiblingIndex(2);

            if (icon != null)
            {
                var iconLe = GetOrAddLayoutElement(icon.gameObject);
                iconLe.preferredWidth = RowIconSize;
                iconLe.preferredHeight = RowIconSize;
                iconLe.flexibleWidth = 0f;
                iconLe.flexibleHeight = 0f;

                var iconImg = icon.GetComponent<Image>();
                if (iconImg != null)
                    iconImg.preserveAspect = true;
            }

            if (name != null)
            {
                var nameLe = GetOrAddLayoutElement(name.gameObject);
                nameLe.flexibleWidth = 1f;
                nameLe.minWidth = 100f;
                nameLe.preferredHeight = RowIconSize;
                nameLe.flexibleHeight = 0f;

                var text = name.GetComponent<Text>();
                if (text != null)
                {
                    text.color = UiModalStyle.ProfilePrimaryTextColor;
                    text.alignment = TextAnchor.MiddleLeft;
                    text.fontSize = Mathf.Min(text.fontSize, 26);
                    text.horizontalOverflow = HorizontalWrapMode.Wrap;
                    text.verticalOverflow = VerticalWrapMode.Truncate;
                }
            }

            if (buttons != null)
            {
                var buttonsLe = GetOrAddLayoutElement(buttons.gameObject);
                buttonsLe.preferredWidth = RowActionButtonWidth * 2f + 16f;
                buttonsLe.flexibleWidth = 0f;
                buttonsLe.preferredHeight = RowActionButtonHeight;
                buttonsLe.flexibleHeight = 0f;

                var btnHlg = buttons.GetComponent<HorizontalLayoutGroup>();
                if (btnHlg != null)
                {
                    btnHlg.padding = new RectOffset(0, 0, 0, 0);
                    btnHlg.spacing = 8f;
                    btnHlg.childAlignment = TextAnchor.MiddleCenter;
                    btnHlg.childControlWidth = true;
                    btnHlg.childControlHeight = true;
                    btnHlg.childForceExpandWidth = false;
                    btnHlg.childForceExpandHeight = true;
                }

                foreach (Transform child in buttons)
                {
                    if (child.name is not ("Upgrade Button" or "Details Button"))
                        continue;

                    var btnLe = GetOrAddLayoutElement(child.gameObject);
                    btnLe.preferredWidth = RowActionButtonWidth;
                    btnLe.preferredHeight = RowActionButtonHeight;
                    btnLe.flexibleWidth = 0f;
                }

                var energy = buttons.Find("Match Energy Cost Text");
                if (energy == null)
                {
                    foreach (var t in buttons.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name.Contains("Energy") || t.name.Contains("100%"))
                            energy = t;
                    }
                }

                if (energy != null)
                {
                    var energyLe = GetOrAddLayoutElement(energy.gameObject);
                    energyLe.preferredWidth = 48f;
                    energyLe.flexibleWidth = 0f;
                }
            }

            var rowLe = GetOrAddLayoutElement(row.gameObject);
            rowLe.minHeight = UiModalStyle.SkillsRowHeight;
            rowLe.preferredHeight = UiModalStyle.SkillsRowHeight;
            rowLe.flexibleHeight = 0f;
            rowLe.flexibleWidth = 1f;
        }

        private static void ReplaceVerticalWithHorizontal(GameObject row)
        {
            var vlg = row.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(vlg);
                else
                    Object.DestroyImmediate(vlg);
            }

            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null)
                hlg = row.AddComponent<HorizontalLayoutGroup>();

            hlg.padding = new RectOffset(10, 10, 8, 8);
            hlg.spacing = 10f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
        }

        private static void UpdateSkillGridCellSize(Transform skillPanel, GridLayoutGroup grid)
        {
            Canvas.ForceUpdateCanvases();
            var panelRt = (RectTransform)skillPanel;
            var width = panelRt.rect.width;
            if (width < 32f && panelRt.parent is RectTransform parentRt)
                width = parentRt.rect.width - UiModalStyle.SkillsModalPadding * 2f;

            if (width < 32f)
                width = 480f;

            var inner = width - grid.padding.horizontal;
            grid.cellSize = new Vector2(Mathf.Max(200f, inner), UiModalStyle.SkillsRowHeight);
        }

        private static void ConfigureLoadoutPanel(Transform? loadoutPanel)
        {
            if (loadoutPanel == null)
                return;

            var rt = (RectTransform)loadoutPanel;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, UiModalStyle.SkillsLoadoutHeight);

            var le = GetOrAddLayoutElement(loadoutPanel.gameObject);
            le.ignoreLayout = false;
            le.minHeight = UiModalStyle.SkillsLoadoutHeight;
            le.preferredHeight = UiModalStyle.SkillsLoadoutHeight;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 1f;

            ApplySectionImage(loadoutPanel, UiModalStyle.SkillsSectionColor);

            var hlg = loadoutPanel.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.padding = new RectOffset(12, 12, 12, 12);
                hlg.spacing = 12f;
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = true;
                hlg.childForceExpandHeight = true;
            }

            foreach (Transform slot in loadoutPanel)
            {
                if (!slot.name.StartsWith("Slot"))
                    continue;

                var slotImg = slot.GetComponent<Image>();
                if (slotImg != null)
                    slotImg.color = new Color(0.26f, 0.28f, 0.32f, 1f);
            }
        }

        private static LayoutElement GetOrAddLayoutElement(GameObject go)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null)
                le = go.AddComponent<LayoutElement>();
            return le;
        }

        private static void ApplyModalChromeWithoutTopPanelHeader(Transform modalRoot)
        {
            var backdrop = modalRoot.Find("Backdrop");
            if (backdrop != null)
                UiModalStyle.ApplyBackdrop(backdrop.GetComponent<Image>());

            foreach (var panelName in new[] { "Panel", "SkillsScreen", "Content" })
            {
                var panel = modalRoot.Find(panelName);
                if (panel == null)
                    continue;

                UiModalStyle.ApplyPanelRect((RectTransform)panel);
                var panelImg = panel.GetComponent<Image>();
                if (panelImg != null)
                    UiModalStyle.ApplyPanel(panelImg);
            }

            foreach (var btn in modalRoot.GetComponentsInChildren<Button>(true))
            {
                if (btn.gameObject.name != "Back Button")
                    continue;
                UiModalStyle.ApplyBackButtonRect((RectTransform)btn.transform);
                UiModalStyle.PrepareBackButton(btn);
            }
        }

        private static void ApplySectionImage(Transform section, Color color)
        {
            var img = section.GetComponent<Image>();
            if (img == null)
                return;

            img.color = color;
            img.raycastTarget = true;
        }
    }
}
