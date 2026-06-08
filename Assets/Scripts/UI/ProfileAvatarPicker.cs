using System;
using System.Collections.Generic;
using TapBrawl.Avatars;
using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>Аватар в профиле: превью + модалка выбора из выданных id.</summary>
    public sealed class ProfileAvatarPicker : MonoBehaviour
    {
        [SerializeField] private AvatarCatalogAsset? catalog;
        [SerializeField] private Image? previewImage;
        [SerializeField] private Button? previewButton;
        [SerializeField] private ProfileAvatarPickModalController? pickModal;

        private string _selectedId = AvatarIds.Default;
        private IReadOnlyList<string> _unlocked = Array.Empty<string>();

        public string SelectedAvatarId => _selectedId;

        private void Awake()
        {
            if (catalog == null)
                catalog = AvatarCatalogAsset.LoadDefault();

            if (pickModal == null)
                pickModal = GetComponent<ProfileAvatarPickModalController>();
            if (pickModal == null)
                pickModal = gameObject.AddComponent<ProfileAvatarPickModalController>();

            pickModal.EnsureBuilt(transform);
            EnsureUi();
        }

        public void Bind(string currentId, IReadOnlyList<string> unlocked)
        {
            if (catalog == null)
                catalog = AvatarCatalogAsset.LoadDefault();

            EnsureUi();

            _selectedId = string.IsNullOrEmpty(currentId) ? AvatarIds.Default : currentId;
            _unlocked = unlocked is { Count: > 0 }
                ? unlocked
                : new[] { AvatarIds.Default, AvatarIds.Blue, AvatarIds.Purple };

            RenderPreview();
        }

        private void EnsureUi()
        {
            var block = FindBlockProfil();
            if (block == null)
                return;

            var avatarUrl = block.Find("avatarUrlInput");
            if (avatarUrl != null)
                avatarUrl.gameObject.SetActive(false);

            var oldRow = block.Find("AvatarRow");
            if (oldRow != null)
            {
                var oldPicker = oldRow.Find("AvatarPickerRow");
                if (oldPicker != null)
                    oldPicker.gameObject.SetActive(false);
            }

            var preview = block.Find("AvatarPreview");
            if (preview == null)
            {
                var previewGo = new GameObject(
                    "AvatarPreview",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button),
                    typeof(LayoutElement));
                previewGo.transform.SetParent(block, false);
                previewGo.transform.SetAsFirstSibling();
                preview = previewGo.transform;

                var size = UiModalStyle.AvatarProfileSize;
                var previewLe = previewGo.GetComponent<LayoutElement>();
                previewLe.preferredWidth = size;
                previewLe.preferredHeight = size;
                previewLe.minWidth = size;
                previewLe.minHeight = size;

                var previewRt = previewGo.GetComponent<RectTransform>();
                previewRt.sizeDelta = new Vector2(size, size);

                var img = previewGo.GetComponent<Image>();
                img.preserveAspect = true;
                img.color = Color.white;

                var btn = previewGo.GetComponent<Button>();
                btn.targetGraphic = img;
                btn.onClick.AddListener(OpenPickModal);
            }

            previewImage = preview.GetComponent<Image>();
            previewButton = preview.GetComponent<Button>();
            if (previewButton != null)
            {
                previewButton.onClick.RemoveListener(OpenPickModal);
                previewButton.onClick.AddListener(OpenPickModal);
            }

            var sizeLe = preview.GetComponent<LayoutElement>();
            if (sizeLe != null)
            {
                var size = UiModalStyle.AvatarProfileSize;
                sizeLe.preferredWidth = size;
                sizeLe.preferredHeight = size;
                sizeLe.minWidth = size;
                sizeLe.minHeight = size;
            }
        }

        private void OpenPickModal()
        {
            if (pickModal == null)
                return;

            pickModal.Open(_selectedId, _unlocked, id =>
            {
                _selectedId = id;
                RenderPreview();
            });
        }

        private Transform? FindBlockProfil()
        {
            foreach (var t in GetComponentsInParent<Transform>(true))
            {
                if (t.name == "BlockProfil")
                    return t;
            }

            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "BlockProfil")
                    return t;
            }

            return null;
        }

        private void RenderPreview()
        {
            if (previewImage == null || catalog == null)
                return;

            var sprite = catalog.GetSprite(_selectedId) ?? catalog.GetSprite(AvatarIds.Default);
            previewImage.sprite = sprite;
            previewImage.enabled = sprite != null;
            previewImage.preserveAspect = true;
        }
    }
}
