using System;
using System.Collections.Generic;
using TapBrawl.Avatars;
using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>Модалка выбора аватара из выданных игрой.</summary>
    public sealed class ProfileAvatarPickModalController : MonoBehaviour
    {
        [SerializeField] private GameObject? modalRoot;
        [SerializeField] private Transform? gridParent;
        [SerializeField] private AvatarCatalogAsset? catalog;

        private readonly List<GameObject> _tiles = new();
        private string _selectedId = AvatarIds.Default;
        private IReadOnlyList<string> _unlocked = Array.Empty<string>();
        private Action<string>? _onSelected;

        public void EnsureBuilt(Transform host)
        {
            if (modalRoot != null)
                return;

            if (catalog == null)
                catalog = AvatarCatalogAsset.LoadDefault();

            modalRoot = new GameObject("AvatarPickModal", typeof(RectTransform));
            modalRoot.transform.SetParent(host, false);
            var rootRt = modalRoot.GetComponent<RectTransform>();
            StretchFull(rootRt);

            var backdropGo = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            backdropGo.transform.SetParent(modalRoot.transform, false);
            StretchFull(backdropGo.GetComponent<RectTransform>());
            var backdropImg = backdropGo.GetComponent<Image>();
            UiModalStyle.ApplyBackdrop(backdropImg);
            var backdropBtn = backdropGo.GetComponent<Button>();
            backdropBtn.transition = Selectable.Transition.None;
            backdropBtn.targetGraphic = backdropImg;
            backdropBtn.onClick.AddListener(Close);

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panelGo.transform.SetParent(modalRoot.transform, false);
            var panelRt = panelGo.GetComponent<RectTransform>();
            UiModalStyle.ApplyPanelRect(panelRt);
            UiModalStyle.ApplyPanel(panelGo.GetComponent<Image>());
            var panelVlg = panelGo.GetComponent<VerticalLayoutGroup>();
            panelVlg.padding = new RectOffset(16, 16, 12, 16);
            panelVlg.spacing = 12;
            panelVlg.childAlignment = TextAnchor.UpperCenter;
            panelVlg.childControlWidth = true;
            panelVlg.childControlHeight = false;
            panelVlg.childForceExpandWidth = true;
            panelVlg.childForceExpandHeight = false;

            var headerGo = new GameObject("Header", typeof(RectTransform), typeof(LayoutElement));
            headerGo.transform.SetParent(panelGo.transform, false);
            var headerLe = headerGo.GetComponent<LayoutElement>();
            headerLe.preferredHeight = 72f;
            headerLe.minHeight = 72f;

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(headerGo.transform, false);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = Vector2.zero;
            titleRt.anchorMax = Vector2.one;
            titleRt.offsetMin = new Vector2(72f, 0f);
            titleRt.offsetMax = new Vector2(-16f, 0f);
            var title = titleGo.GetComponent<Text>();
            title.text = "Выберите аватар";
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.fontSize = 36;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Color.white;
            title.raycastTarget = false;

            var backGo = new GameObject("Back Button", typeof(RectTransform), typeof(Image), typeof(Button));
            backGo.transform.SetParent(headerGo.transform, false);
            UiModalStyle.ApplyBackButtonRect(backGo.GetComponent<RectTransform>());
            backGo.GetComponent<RectTransform>().sizeDelta = new Vector2(72f, 72f);
            var backBtn = backGo.GetComponent<Button>();
            UiModalStyle.PrepareBackButton(backBtn);
            backBtn.onClick.AddListener(Close);

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(LayoutElement));
            scrollGo.transform.SetParent(panelGo.transform, false);
            var scrollLe = scrollGo.GetComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1f;
            scrollLe.preferredHeight = 280f;
            scrollLe.minHeight = 200f;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            StretchFull(viewportRt);
            viewportGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);

            var grid = contentGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(UiModalStyle.AvatarPickerTileSize, UiModalStyle.AvatarPickerTileSize);
            grid.spacing = new Vector2(12f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewportRt;
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            gridParent = contentGo.transform;
            modalRoot.SetActive(false);
        }

        public void Open(string currentId, IReadOnlyList<string> unlocked, Action<string> onSelected)
        {
            if (modalRoot == null)
                return;

            _selectedId = currentId;
            _unlocked = unlocked;
            _onSelected = onSelected;
            RebuildGrid();
            modalRoot.SetActive(true);
        }

        public void Close()
        {
            ClearTiles();
            if (modalRoot != null)
                modalRoot.SetActive(false);
            _onSelected = null;
        }

        private void RebuildGrid()
        {
            ClearTiles();
            if (gridParent == null || catalog == null)
                return;

            foreach (var entry in catalog.Entries)
            {
                if (string.IsNullOrEmpty(entry.id))
                    continue;

                var unlocked = IsUnlocked(entry.id);
                var tile = CreateTile(entry, unlocked);
                tile.transform.SetParent(gridParent, false);
                _tiles.Add(tile);
            }
        }

        private bool IsUnlocked(string id)
        {
            for (var i = 0; i < _unlocked.Count; i++)
            {
                if (_unlocked[i] == id)
                    return true;
            }

            return false;
        }

        private GameObject CreateTile(AvatarCatalogAsset.Entry entry, bool unlocked)
        {
            var go = new GameObject($"Avatar_{entry.id}", typeof(RectTransform), typeof(Image), typeof(Button));
            var img = go.GetComponent<Image>();
            img.sprite = entry.sprite;
            img.preserveAspect = true;
            img.color = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.35f);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = unlocked;

            if (entry.id == _selectedId)
            {
                var ring = new GameObject("Selection", typeof(RectTransform), typeof(Image));
                ring.transform.SetParent(go.transform, false);
                ring.transform.SetAsFirstSibling();
                var ringRt = ring.GetComponent<RectTransform>();
                ringRt.anchorMin = Vector2.zero;
                ringRt.anchorMax = Vector2.one;
                ringRt.offsetMin = new Vector2(-4f, -4f);
                ringRt.offsetMax = new Vector2(4f, 4f);
                ring.GetComponent<Image>().color = UiModalStyle.ProfileAccentTextColor;
            }

            if (unlocked)
            {
                var capturedId = entry.id;
                btn.onClick.AddListener(() =>
                {
                    _selectedId = capturedId;
                    _onSelected?.Invoke(capturedId);
                    Close();
                });
            }

            return go;
        }

        private void ClearTiles()
        {
            for (var i = 0; i < _tiles.Count; i++)
            {
                if (_tiles[i] != null)
                    Destroy(_tiles[i]);
            }

            _tiles.Clear();
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
