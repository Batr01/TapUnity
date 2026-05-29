using TapBrawl.Core;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine;

namespace TapBrawl.DebugTools
{
    /// <summary>
    /// Локальный просмотр молнии: в Play Mode нажмите клавишу — болт между двумя первыми активными кругами под родителем.
    /// Повесьте на <c>PlayArea</c> (или задайте <see cref="spawnParent"/>).
    /// </summary>
    public sealed class ChainBoltLocalPreview : MonoBehaviour
    {
        [SerializeField] private GameObject chainBoltPrefab = null!;
#if ENABLE_INPUT_SYSTEM
        [SerializeField] private Key previewKey = Key.L;
#endif
        [Tooltip("Если пусто — RectTransform на этом же объекте.")]
        [SerializeField] private RectTransform? spawnParent;

#if ENABLE_INPUT_SYSTEM
        private void Update()
        {
            if (!Application.isPlaying || chainBoltPrefab == null)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard[previewKey].wasPressedThisFrame)
                return;

            var parent = spawnParent != null ? spawnParent : GetComponent<RectTransform>();
            if (parent == null)
            {
                Debug.LogWarning(
                    "[ChainBoltPreview] Укажите Spawn Parent или повесьте на объект с RectTransform (PlayArea).");
                return;
            }

            var circles = parent.GetComponentsInChildren<PooledCircle>(false);
            if (circles.Length < 2)
            {
                Debug.LogWarning("[ChainBoltPreview] Нужно минимум 2 активных круга под родителем.");
                return;
            }

            var inst = Instantiate(chainBoltPrefab, parent, false);
            inst.transform.SetAsLastSibling();
            if (!inst.TryGetComponent<TapBrawl.Core.VFX.ChainBoltFx>(out var fx))
            {
                Debug.LogError("[ChainBoltPreview] На префабе должен быть ChainBoltFx.");
                Destroy(inst);
                return;
            }

            fx.Play(circles[0].transform.position, circles[1].transform.position);
            Debug.Log($"[ChainBoltPreview] Болт {circles[0].name} → {circles[1].name}. Клавиша: {previewKey}.");
        }
#endif
    }
}
