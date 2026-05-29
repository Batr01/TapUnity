using TapBrawl.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TapBrawl.DebugTools
{
    /// <summary>
    /// Проверка префаба дыма отдельно от скилла: в Play Mode нажмите клавишу — инстанс с той же настройкой Canvas, что в <see cref="TapBrawl.Core.MatchController"/>.
    /// Повесьте на объект с <see cref="RectTransform"/> под игровым Canvas (например на <c>PlayArea</c>) или укажите <see cref="spawnParent"/>.
    /// </summary>
    public sealed class SmokeVeilLocalPreview : MonoBehaviour
    {
        [SerializeField] private GameObject smokeVeilPrefab = null!;
        [SerializeField] private Key previewKey = Key.P;
        [SerializeField] [Min(1f)] private float destroyAfterSeconds = 6f;
        [Tooltip("Если пусто — берётся RectTransform на этом же GameObject.")]
        [SerializeField] private RectTransform? spawnParent;

        private void Update()
        {
            if (!Application.isPlaying || smokeVeilPrefab == null)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard[previewKey].wasPressedThisFrame)
                return;

            var parent = spawnParent != null ? spawnParent : GetComponent<RectTransform>();
            if (parent == null)
            {
                Debug.LogWarning(
                    "[SmokePreview] Укажите Spawn Parent или повесьте скрипт на объект с RectTransform (например PlayArea).");
                return;
            }

            var inst = Instantiate(smokeVeilPrefab, parent, false);
            inst.name = "SmokeVeil_Preview";
            SmokeVeilOverlayRuntime.FlattenNestedSmokeVeilRootIfPresent(inst);

            if (!inst.TryGetComponent<RectTransform>(out var rt))
            {
                Debug.LogError("[SmokePreview] У префаба на корне должен быть RectTransform.");
                Destroy(inst);
                return;
            }

            rt.anchorMin = new Vector2(0.15f, 0.15f);
            rt.anchorMax = new Vector2(0.85f, 0.85f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition3D = Vector3.zero;
            rt.localScale = Vector3.one;

            SmokeVeilOverlayRuntime.EnsureSmokeVeilBackdrop(inst);
            SmokeVeilOverlayRuntime.CompensateSmokeVeilScaleIfCanvasMakesParticlesTiny(rt);

            SmokeVeilOverlayRuntime.TryConfigureParticleCanvasForOverlay(inst);
            SmokeVeilOverlayRuntime.EnsureSmokeVeilBackdrop(inst);
            foreach (var ps in inst.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.useUnscaledTime = true;
                ps.Clear(true);
                ps.Play(true);
            }

            SmokeVeilOverlayRuntime.NormalizeParticleRenderersForUi(inst);

            inst.transform.SetAsLastSibling();
            Destroy(inst, destroyAfterSeconds);
            Debug.Log($"[SmokePreview] Создан «{inst.name}» на «{parent.name}». Клавиша: {previewKey}. Удаление через {destroyAfterSeconds:0} с.");
        }
    }
}
