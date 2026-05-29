using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.Core.VFX
{
    /// <summary>Runtime UI pop/explosion for tapped circles; works without a prepared prefab.</summary>
    public sealed class CircleTapJuiceFx : MonoBehaviour
    {
        private RectTransform _rt = null!;
        private Image? _flash;
        private Image? _ring;
        private ParticleSystem? _particles;
        private float _lifetimeSec;
        private float _targetScale;
        private Color _baseColor;

        public static void Spawn(
            RectTransform parent,
            RectTransform source,
            Sprite? sprite,
            CircleKind kind,
            float lifetimeSec,
            float tapPopScale,
            float bombExplosionScale,
            bool chainLightningStrike)
        {
            var go = new GameObject("CircleTapJuiceFx", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.SetAsLastSibling();

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.position = source.position;
            var sourceSize = Mathf.Max(source.rect.width, source.rect.height, 40f);
            var isBomb = kind == CircleKind.Bomb;
            rt.sizeDelta = Vector2.one * sourceSize;

            var fx = go.AddComponent<CircleTapJuiceFx>();
            fx.Configure(
                sprite,
                ColorForKind(kind, chainLightningStrike),
                Mathf.Max(0.08f, lifetimeSec) * (isBomb ? 1.45f : 1f),
                isBomb ? Mathf.Max(2f, bombExplosionScale) : Mathf.Max(1.1f, tapPopScale),
                isBomb,
                chainLightningStrike);
        }

        private void Configure(
            Sprite? sprite,
            Color color,
            float lifetimeSec,
            float targetScale,
            bool isBomb,
            bool chainLightningStrike)
        {
            _rt = (RectTransform)transform;
            _lifetimeSec = lifetimeSec;
            _targetScale = targetScale;
            _baseColor = color;
            _flash = CreateImage("PopFlash", sprite, color, isBomb ? 1.1f : 0.92f);
            _ring = CreateImage("PopRing", sprite, WithAlpha(color, isBomb ? 0.78f : 0.58f), 1f);
            CreateParticles(isBomb, chainLightningStrike);
            StartCoroutine(PlayRoutine());
        }

        private Image CreateImage(string name, Sprite? sprite, Color color, float scale)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one * scale;

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        private void CreateParticles(bool isBomb, bool chainLightningStrike)
        {
            var go = new GameObject("PopParticles");
            go.transform.SetParent(transform, false);
            go.transform.position = transform.position;

            _particles = go.AddComponent<ParticleSystem>();
            var main = _particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.28f;
            main.startLifetime = isBomb
                ? new ParticleSystem.MinMaxCurve(0.18f, 0.42f)
                : new ParticleSystem.MinMaxCurve(0.1f, 0.24f);
            main.startSpeed = isBomb
                ? new ParticleSystem.MinMaxCurve(180f, 420f)
                : new ParticleSystem.MinMaxCurve(90f, 210f);
            main.startSize = isBomb
                ? new ParticleSystem.MinMaxCurve(10f, 26f)
                : new ParticleSystem.MinMaxCurve(5f, 14f);
            main.startColor = chainLightningStrike ? new Color(0.55f, 0.9f, 1f, 1f) : _baseColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.useUnscaledTime = true;
            main.gravityModifier = 0f;

            var emission = _particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)(isBomb ? 46 : 18), (short)(isBomb ? 62 : 28)),
            });

            var shape = _particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = isBomb ? 14f : 6f;

            var renderer = _particles.GetComponent<ParticleSystemRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 32001;
            var mat = ChainBoltFxMaterials.TryRuntimeFallback();
            if (mat != null)
                renderer.sharedMaterial = mat;

            _particles.Play(true);
        }

        private IEnumerator PlayRoutine()
        {
            var elapsed = 0f;
            while (elapsed < _lifetimeSec)
            {
                elapsed += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(elapsed / _lifetimeSec);
                var easeOut = 1f - (1f - u) * (1f - u);

                _rt.localScale = Vector3.one * Mathf.Lerp(0.74f, _targetScale, easeOut);
                if (_flash != null)
                {
                    _flash.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.22f, easeOut);
                    _flash.color = WithAlpha(_baseColor, 1f - u);
                }

                if (_ring != null)
                {
                    _ring.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.35f, easeOut);
                    _ring.color = WithAlpha(_baseColor, (1f - u) * 0.62f);
                }

                yield return null;
            }

            if (_particles != null)
                _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Destroy(gameObject);
        }

        private static Color ColorForKind(CircleKind kind, bool chainLightningStrike)
        {
            if (chainLightningStrike)
                return new Color(0.55f, 0.92f, 1f, 1f);
            return kind switch
            {
                CircleKind.Gold => new Color(1f, 0.85f, 0.24f, 1f),
                CircleKind.Bomb => new Color(1f, 0.28f, 0.16f, 1f),
                CircleKind.Phantom => new Color(0.7f, 0.46f, 1f, 1f),
                _ => new Color(0.35f, 0.68f, 1f, 1f),
            };
        }

        private static Color WithAlpha(Color c, float a)
        {
            c.a = Mathf.Clamp01(a);
            return c;
        }
    }
}
