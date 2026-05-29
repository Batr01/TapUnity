using System.Collections;
using UnityEngine;

namespace TapBrawl.Core.VFX
{
    /// <summary>
    /// Краткая «молния» между двумя точками в мировых координатах (например между UI-кругами при разряде цепи).
    /// Корень обычно под <c>PlayArea</c> с <see cref="RectTransform"/>; линия в world space.
    /// </summary>
    public sealed class ChainBoltFx : MonoBehaviour
    {
        [SerializeField] private Material? boltMaterial;

        [SerializeField] [Min(3)] private int segmentCount = 14;

        [SerializeField] private float zigzagAmplitudeWorld = 18f;

        [SerializeField] private float lifetimeSec = 0.28f;

        [SerializeField] private float widthMultiplier = 6f;

        [SerializeField] private string sortingLayerName = "Default";

        [SerializeField] private int sortingOrder = 32000;

        [SerializeField] private bool enableSparks = true;

        private LineRenderer? _line;
        private ParticleSystem? _sparks;
        private Coroutine? _routine;

        private void Awake()
        {
            EnsureLineRenderer();
            if (enableSparks)
                EnsureSparks();
            ApplySorting();
        }

        /// <summary>Запускает анимацию болта и уничтожает объект по окончании (unscaled time).</summary>
        public void Play(Vector3 worldFrom, Vector3 worldTo)
        {
            if (_routine != null)
                StopCoroutine(_routine);
            _routine = StartCoroutine(BoltRoutine(worldFrom, worldTo));
        }

        private void EnsureLineRenderer()
        {
            var boltGo = transform.Find("BoltLine");
            if (boltGo == null)
            {
                boltGo = new GameObject("BoltLine").transform;
                boltGo.SetParent(transform, false);
                boltGo.localPosition = Vector3.zero;
                boltGo.localRotation = Quaternion.identity;
                boltGo.localScale = Vector3.one;
            }

            _line = boltGo.GetComponent<LineRenderer>();
            if (_line == null)
                _line = boltGo.gameObject.AddComponent<LineRenderer>();

            _line.useWorldSpace = true;
            _line.textureMode = LineTextureMode.Stretch;
            _line.numCornerVertices = 3;
            _line.numCapVertices = 3;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            _line.sortingLayerName = sortingLayerName;
            _line.sortingOrder = sortingOrder;
            _line.positionCount = segmentCount;

            var mat = boltMaterial != null ? boltMaterial : ChainBoltFxMaterials.TryRuntimeFallback();
            if (mat != null)
                _line.material = mat;

            var widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
            _line.widthCurve = widthCurve;
            _line.widthMultiplier = widthMultiplier;

            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.85f, 0.95f, 1f), 0f),
                    new GradientColorKey(new Color(0.35f, 0.75f, 1f), 0.45f),
                    new GradientColorKey(new Color(0.55f, 0.9f, 1f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(0f, 1f),
                });
            _line.colorGradient = gradient;
        }

        private void EnsureSparks()
        {
            var sparksTf = transform.Find("Sparks");
            GameObject sparksGo;
            if (sparksTf != null)
                sparksGo = sparksTf.gameObject;
            else
            {
                sparksGo = new GameObject("Sparks");
                sparksGo.transform.SetParent(transform, false);
                sparksGo.transform.localPosition = Vector3.zero;
                sparksGo.transform.localRotation = Quaternion.identity;
                sparksGo.transform.localScale = Vector3.one;
            }

            _sparks = sparksGo.GetComponent<ParticleSystem>();
            if (_sparks == null)
                _sparks = sparksGo.AddComponent<ParticleSystem>();

            var main = _sparks.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.35f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(80f, 220f);
            main.startSize = new ParticleSystem.MinMaxCurve(4f, 14f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.useUnscaledTime = true;
            main.gravityModifier = 0f;
            main.startColor = new Color(0.75f, 0.92f, 1f, 1f);

            var emission = _sparks.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 22, 28) });

            var shape = _sparks.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 38f;
            shape.radius = 2f;

            var renderer = _sparks.GetComponent<ParticleSystemRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder + 1;

            var sparkMat = boltMaterial != null ? boltMaterial : ChainBoltFxMaterials.TryRuntimeFallback();
            if (sparkMat != null)
                renderer.sharedMaterial = sparkMat;
        }

        private void ApplySorting()
        {
            if (_line != null)
            {
                _line.sortingLayerName = sortingLayerName;
                _line.sortingOrder = sortingOrder;
            }

            if (_sparks != null)
            {
                var r = _sparks.GetComponent<ParticleSystemRenderer>();
                r.sortingLayerName = sortingLayerName;
                r.sortingOrder = sortingOrder + 1;
            }
        }

        private IEnumerator BoltRoutine(Vector3 worldFrom, Vector3 worldTo)
        {
            if (_line == null)
                yield break;

            Random.InitState(GetInstanceID() ^ (int)(Time.unscaledTime * 10000f));

            TriggerSparks(worldFrom, worldTo);

            var elapsed = 0f;
            while (elapsed < lifetimeSec)
            {
                elapsed += Time.unscaledDeltaTime;
                FillZigZag(worldFrom, worldTo);
                var fade = 1f - Mathf.SmoothStep(0f, 1f, elapsed / lifetimeSec);
                _line.widthMultiplier = widthMultiplier * Mathf.Lerp(0.15f, 1f, fade);
                yield return null;
            }

            if (_sparks != null)
                _sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            Destroy(gameObject);
        }

        private void TriggerSparks(Vector3 worldFrom, Vector3 worldTo)
        {
            if (_sparks == null)
                return;

            var mid = (worldFrom + worldTo) * 0.5f;
            var dir = worldTo - worldFrom;
            _sparks.transform.position = mid;
            if (dir.sqrMagnitude > 0.0001f)
                _sparks.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

            var main = _sparks.main;
            main.useUnscaledTime = true;
            _sparks.Clear(true);
            _sparks.Play(true);
        }

        private void FillZigZag(Vector3 a, Vector3 b)
        {
            if (_line == null)
                return;

            var dir = b - a;
            var len = dir.magnitude;
            if (len < 1e-5f)
                dir = Vector3.right * 0.01f;
            else
                dir /= len;

            var ortho = Vector3.Cross(dir, Vector3.forward);
            if (ortho.sqrMagnitude < 1e-4f)
                ortho = Vector3.Cross(dir, Vector3.up);
            ortho.Normalize();

            _line.positionCount = segmentCount;
            for (var i = 0; i < segmentCount; i++)
            {
                var t = i / (float)(segmentCount - 1);
                var basePos = Vector3.Lerp(a, b, t);
                var envelope = Mathf.Sin(t * Mathf.PI);
                var jitter = (Random.value - 0.5f) * 2f * zigzagAmplitudeWorld * envelope;
                _line.SetPosition(i, basePos + ortho * jitter);
            }
        }
    }
}
