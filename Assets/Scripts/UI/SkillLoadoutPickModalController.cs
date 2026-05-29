using System.Collections.Generic;
using TapBrawl.Core.Skills;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>
    /// Модальное окно выбора скилла для слота лоадаута. Иерархия UI собирается в редакторе и назначается в инспекторе.
    /// Родитель плиток — обычно <see cref="GridLayoutGroup"/> на два столбца.
    /// </summary>
    public sealed class SkillLoadoutPickModalController : MonoBehaviour
    {
        [Header("Корень модалки")]
        [SerializeField] private GameObject modalRoot = null!;
        [SerializeField] private Transform skillRowsParent = null!;
        [SerializeField] private Button skillOptionTilePrefab = null!;
        [SerializeField] private SkillCatalog? skillCatalog;

        [Header("Закрытие")]
        [SerializeField] private Button? backdropCloseButton;
        [SerializeField] private Button? cancelButton;

        [Header("Оформление плиток")]
        [SerializeField] private Color tileEnabledBackgroundColor = new(0.22f, 0.38f, 0.62f, 1f);
        [SerializeField] private Color tileDisabledBackgroundColor = new(0.18f, 0.18f, 0.18f, 0.85f);
        [SerializeField] private Color tileEnabledTitleColor = Color.white;
        [SerializeField] private Color tileDisabledTitleColor = new(1f, 1f, 1f, 0.45f);

        private readonly List<GameObject> _spawnedTiles = new();

        private SkillLoadoutSlotView[]? _slots;
        private int _editingSlotIndex;

        private void Awake()
        {
            if (backdropCloseButton != null)
                backdropCloseButton.onClick.AddListener(Close);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(Close);

            if (modalRoot != null && !modalRoot.activeSelf)
                modalRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (backdropCloseButton != null)
                backdropCloseButton.onClick.RemoveListener(Close);
            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(Close);
        }

        /// <summary>Открыть выбор скилла для слота <paramref name="editingSlotIndex"/>.</summary>
        public void Open(int editingSlotIndex, SkillLoadoutSlotView[] loadoutSlots)
        {
            if (modalRoot == null || skillRowsParent == null || skillOptionTilePrefab == null)
            {
                Debug.LogError("[SkillLoadoutPickModal] Назначьте modalRoot, skillRowsParent и skillOptionTilePrefab.", this);
                return;
            }

            _slots = loadoutSlots;
            _editingSlotIndex = Mathf.Clamp(editingSlotIndex, 0, loadoutSlots.Length - 1);
            RebuildTiles();
            modalRoot.SetActive(true);
        }

        private void Close()
        {
            ClearSpawnedTiles();
            if (modalRoot != null)
                modalRoot.SetActive(false);
        }

        private void RebuildTiles()
        {
            ClearSpawnedTiles();
            if (skillRowsParent == null || _slots == null || _slots.Length == 0)
                return;

            var slot = _slots[_editingSlotIndex];
            if (slot == null)
                return;

            var currentId = slot.SelectedSkillId;
            var takenElsewhere = new HashSet<int>();
            for (var i = 0; i < _slots.Length; i++)
            {
                if (i == _editingSlotIndex || _slots[i] == null)
                    continue;
                takenElsewhere.Add(_slots[i].SelectedSkillId);
            }

            foreach (var skillId in SkillBalance.KnownSkillIds)
            {
                var blocked = takenElsewhere.Contains(skillId) && skillId != currentId;
                var title = ResolveDisplayName(skillId);
                if (blocked)
                    title += "\n(занят в другом слоте)";

                var icon = skillCatalog != null ? skillCatalog.GetIcon(skillId) : null;
                var capturedId = skillId;
                CreateSkillTile(
                    icon,
                    title,
                    !blocked,
                    () =>
                    {
                        slot.SelectSkillById(capturedId);
                        Close();
                    });
            }
        }

        private string ResolveDisplayName(int skillId) =>
            skillCatalog != null ? skillCatalog.GetDisplayName(skillId) : SkillDefinitions.GetDisplayName(skillId);

        private void ClearSpawnedTiles()
        {
            foreach (var go in _spawnedTiles)
            {
                if (go != null)
                    Destroy(go);
            }

            _spawnedTiles.Clear();
        }

        private void CreateSkillTile(Sprite? icon, string title, bool interactable, UnityAction onChosen)
        {
            var btn = Instantiate(skillOptionTilePrefab, skillRowsParent);
            btn.gameObject.SetActive(true);
            btn.interactable = interactable;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(onChosen);

            var bgColor = interactable ? tileEnabledBackgroundColor : tileDisabledBackgroundColor;
            var txtColor = interactable ? tileEnabledTitleColor : tileDisabledTitleColor;

            var tileView = btn.GetComponent<SkillLoadoutPickTileView>();
            if (tileView != null)
                tileView.Apply(icon, title, interactable, bgColor, txtColor);
            else
            {
                var img = btn.GetComponent<Image>();
                if (img != null)
                    img.color = bgColor;
            }

            _spawnedTiles.Add(btn.gameObject);
        }
    }
}
