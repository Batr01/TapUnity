using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.Core
{
    /// <summary>
    /// Общая настройка префаба дыма с частицами под родительским <see cref="Canvas"/> в режиме Overlay.
    /// </summary>
    public static class SmokeVeilOverlayRuntime
    {
        public const string ParticleCanvasHostName = "SmokeParticleCanvas";

        /// <summary>
        /// Множитель к <see cref="ParticleSystem.MainModule.startSizeMultiplier"/> под ваш Canvas
        /// (базовые размеры задаются в <see cref="ConfigureSmokeVeilFogPresentation"/>).
        /// </summary>
        public const float SmokeParticleStartSizeUiBoost = 200f;

        /// <summary>
        /// Убирает ошибочную вложенность «корень SmokeVeilRoot → дочерний SmokeVeilRoot → SmokeParticles»:
        /// детей среднего объекта поднимает на <paramref name="smokeRoot"/>, средний удаляет.
        /// Иначе якоря и масштаб часто «двоятся», эффект не виден в Game.
        /// </summary>
        public static void FlattenNestedSmokeVeilRootIfPresent(GameObject smokeRoot)
        {
            for (var guard = 0; guard < 4 && smokeRoot.transform.childCount == 1; guard++)
            {
                var mid = smokeRoot.transform.GetChild(0);
                if (mid.name != "SmokeVeilRoot")
                    return;
                for (var i = mid.childCount - 1; i >= 0; i--)
                    mid.GetChild(i).SetParent(smokeRoot.transform, false);
                Object.Destroy(mid.gameObject);
            }
        }

        /// <summary>
        /// Если над <paramref name="smokeRoot"/> есть Overlay Canvas, добавляет дочерний хост с
        /// <see cref="RenderMode.ScreenSpaceCamera"/> и переносит под него существующих детей корня.
        /// </summary>
        public static void TryConfigureParticleCanvasForOverlay(GameObject smokeRoot)
        {
            var rootCanvas = smokeRoot.GetComponentInParent<Canvas>();
            if (rootCanvas == null || rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                return;

            if (smokeRoot.transform.Find(ParticleCanvasHostName) != null)
                return;

            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning(
                    "[SmokeVeil] Родительский Canvas — Overlay, а Camera.main не найден. Частицы могут быть невидимы.");
                return;
            }

            var existingChildren = new List<Transform>(smokeRoot.transform.childCount);
            for (var i = 0; i < smokeRoot.transform.childCount; i++)
                existingChildren.Add(smokeRoot.transform.GetChild(i));

            var hostGo = new GameObject(ParticleCanvasHostName, typeof(RectTransform), typeof(Canvas));
            var hostRt = hostGo.GetComponent<RectTransform>();
            hostRt.SetParent(smokeRoot.transform, false);
            hostRt.anchorMin = Vector2.zero;
            hostRt.anchorMax = Vector2.one;
            hostRt.offsetMin = Vector2.zero;
            hostRt.offsetMax = Vector2.zero;
            hostRt.localScale = Vector3.one;

            foreach (var t in existingChildren)
                t.SetParent(hostRt, false);

            var cv = hostGo.GetComponent<Canvas>();
            cv.overrideSorting = true;
            cv.sortingOrder = rootCanvas.sortingOrder + 200;
            cv.renderMode = RenderMode.ScreenSpaceCamera;
            cv.worldCamera = cam;
            cv.planeDistance = Mathf.Clamp(10f, cam.nearClipPlane + 0.05f, cam.farClipPlane - 1f);
        }

        /// <summary>
        /// Выравнивание к камере и высокий <see cref="ParticleSystemRenderer.sortingOrder"/> — иначе частицы под UI
        /// Screen Space — Camera иногда оказываются «под» батчем Canvas.
        /// </summary>
        public static void NormalizeParticleRenderersForUi(GameObject smokeRoot)
        {
            var canvas = smokeRoot.GetComponentInParent<Canvas>();
            var sortBase = canvas != null ? canvas.sortingOrder : 0;
            foreach (var r in smokeRoot.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                r.alignment = ParticleSystemRenderSpace.View;
                r.sortingOrder = sortBase + 5000;
                // Unity 6: maxParticleSize — доля вьюпорта (типично до 1). В YAML «без лимита» часто
                // превращается в 0 — билборды с нулевым потолком не рисуются.
                if (r.maxParticleSize <= 0f)
                    r.maxParticleSize = 1f;
                else if (r.maxParticleSize > 1f)
                    r.maxParticleSize = 1f;
            }
        }

        /// <summary>
        /// При Canvas в режиме Screen Space — Camera у дочерних Rect часто очень маленький <see cref="Transform.lossyScale"/>:
        /// частицы с <see cref="ParticleSystemScalingMode.Hierarchy"/> на экране почти исчезают. Увеличиваем <c>localScale</c> корня дыма
        /// (стремимся к <c>lossyScale</c> ≈ 1 по осям x/y, пока он заметно меньше единицы — типичный UI Canvas).
        /// </summary>
        public static void CompensateSmokeVeilScaleIfCanvasMakesParticlesTiny(RectTransform smokeRt)
        {
            Canvas.ForceUpdateCanvases();
            var s = Mathf.Max(smokeRt.lossyScale.x, smokeRt.lossyScale.y, 1e-6f);
            const float targetLossy = 1f;
            if (s >= 0.985f)
                return;
            var mult = Mathf.Clamp(targetLossy / s, 1f, 4000f);
            smokeRt.localScale = Vector3.one * mult;
        }

        /// <summary>
        /// Туман/облако: плотность, шум, мягкие альфы, долгая жизнь. Базовый <c>startSize</c> маленький —
        /// итоговый размер даёт <see cref="BoostSmokeParticleStartSizeForUi"/> (множитель Canvas).
        /// </summary>
        public static void ConfigureSmokeVeilFogPresentation(GameObject smokeRoot)
        {
            var systems = smokeRoot.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            foreach (var ps in systems)
            {
                var main = ps.main;
                main.playOnAwake = false;
                main.prewarm = true;
                main.loop = true;
                main.duration = 6f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(4.2f, 7.5f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.012f, 0.06f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.48f);
                main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI * 0.35f, Mathf.PI * 0.35f);
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.gravityModifier = 0f;
                main.maxParticles = Mathf.Max(main.maxParticles, 220);
                main.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(0.62f, 0.66f, 0.74f, 0.22f),
                    new Color(0.48f, 0.52f, 0.62f, 0.38f));

                var emission = ps.emission;
                emission.rateOverTime = 52f;

                var sh = ps.shape;
                sh.enabled = true;
                sh.shapeType = ParticleSystemShapeType.Sphere;
                sh.radius = 2.62f;
                sh.radiusThickness = 0.92f;

                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.Local;
                vel.x = new ParticleSystem.MinMaxCurve(-0.045f, 0.045f);
                vel.y = new ParticleSystem.MinMaxCurve(-0.018f, 0.028f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);

                var noise = ps.noise;
                noise.enabled = true;
                noise.separateAxes = false;
                noise.strength = new ParticleSystem.MinMaxCurve(0.22f, 0.38f);
                noise.frequency = 0.42f;
                noise.scrollSpeed = 0.07f;
                noise.damping = true;
                noise.octaveCount = 2;
                noise.quality = ParticleSystemNoiseQuality.Medium;

                var col = ps.colorOverLifetime;
                col.enabled = true;
                var g = new Gradient();
                g.SetKeys(
                    new[]
                    {
                        new GradientColorKey(new Color(0.78f, 0.82f, 0.9f), 0f),
                        new GradientColorKey(new Color(0.58f, 0.62f, 0.72f), 0.45f),
                        new GradientColorKey(new Color(0.48f, 0.52f, 0.62f), 1f)
                    },
                    new[]
                    {
                        new GradientAlphaKey(0f, 0f),
                        new GradientAlphaKey(0.08f, 0.06f),
                        new GradientAlphaKey(0.32f, 0.28f),
                        new GradientAlphaKey(0.26f, 0.55f),
                        new GradientAlphaKey(0.14f, 0.78f),
                        new GradientAlphaKey(0f, 1f)
                    });
                col.color = new ParticleSystem.MinMaxGradient(g);

                var size = ps.sizeOverLifetime;
                size.enabled = true;
                var sizeCurve = new AnimationCurve(
                    new Keyframe(0f, 0.12f, 0f, 2.4f),
                    new Keyframe(0.22f, 0.95f, 0.2f, 0.05f),
                    new Keyframe(0.52f, 1f, 0f, 0f),
                    new Keyframe(0.82f, 0.78f, -0.35f, -0.35f),
                    new Keyframe(1f, 0.2f, -0.6f, 0f));
                size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

                var rot = ps.rotationOverLifetime;
                rot.enabled = true;
                rot.z = new ParticleSystem.MinMaxCurve(-5f, 5f);
            }
        }

        /// <summary>
        /// Множитель размера частиц под Canvas (радиус shape задаётся в <see cref="ConfigureSmokeVeilFogPresentation"/>,
        /// здесь не масштабируем — иначе при ~100x получится гигантский шар эмиссии).
        /// </summary>
        public static void BoostSmokeParticleStartSizeForUi(GameObject smokeRoot, float multiplier = SmokeParticleStartSizeUiBoost)
        {
            if (multiplier <= 1.0001f)
                return;
            foreach (var ps in smokeRoot.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.startSizeMultiplier *= multiplier;
            }
        }

        /// <summary>
        /// Полупрозрачный <see cref="Image"/> на весь прямоугольник дыма — рисуется в том же UI-проходе, что и остальной Canvas.
        /// Если подложка видна, а частицы нет — проблема в рендере Particle System / материале.
        /// </summary>
        public static void EnsureSmokeVeilBackdrop(GameObject smokeRoot)
        {
            const string backdropName = "SmokeVeilBackdrop";
            if (FindNamedDescendant(smokeRoot.transform, backdropName) == null)
            {
                var go = new GameObject(backdropName, typeof(RectTransform), typeof(Image));
                var rt = go.GetComponent<RectTransform>();
                rt.SetParent(smokeRoot.transform, false);
                rt.SetAsFirstSibling();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
                rt.localScale = Vector3.one;

                var img = go.GetComponent<Image>();
                img.color = new Color(0.1f, 0.12f, 0.16f, 0.58f);
                img.raycastTarget = false;
            }

            // Подложка должна быть ниже по отрисовке, чем Particle System на том же Canvas,
            // иначе дочерний Image перекрывает билборды (частицы «есть», но не видны).
            ReorderParticleSystemsAfterBackdrop(smokeRoot.transform);
        }

        private static Transform FindNamedDescendant(Transform root, string name)
        {
            if (root.name == name)
                return root;
            for (var i = 0; i < root.childCount; i++)
            {
                var hit = FindNamedDescendant(root.GetChild(i), name);
                if (hit != null)
                    return hit;
            }

            return null;
        }

        private static void ReorderParticleSystemsAfterBackdrop(Transform smokeRoot)
        {
            Transform backdrop = null;
            foreach (var t in smokeRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != "SmokeVeilBackdrop")
                    continue;
                backdrop = t;
                break;
            }

            if (backdrop == null)
                return;
            var container = backdrop.parent;
            if (container == null)
                return;

            foreach (var ps in smokeRoot.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (ps.transform == backdrop || ps.transform.IsChildOf(backdrop))
                    continue;
                var cur = ps.transform;
                while (cur.parent != null && cur.parent != container)
                    cur = cur.parent;
                if (cur.parent == container)
                    cur.SetAsLastSibling();
            }
        }
    }
}
