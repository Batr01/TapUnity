using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.Core
{
    public sealed class CircleSpawner : MonoBehaviour
    {
        [SerializeField] private CircleSpawnConfig config = null!;
        [SerializeField] private PooledCircle prefab = null!;
        [SerializeField] private RectTransform playArea = null!;
        [Header("Debug")]
        [SerializeField] private bool showSpawnAreaDebugOverlay;
        [SerializeField] private Color spawnAreaDebugColor = new(0.18f, 0.95f, 0.3f, 0.2f);
        [SerializeField] private string spawnAreaDebugOverlayName = "SpawnAreaDebugOverlay";

        private SimpleObjectPool<PooledCircle>? _pool;
        private SeedRandom _rng;
        private MatchController? _match;
        private float _nextSpawnAt;
        private int _nextCircleId = 1;

        public RectTransform PlayAreaRect => playArea;
        public CircleSpawnConfig Config => config;

        public void Begin(MatchController match, uint seed)
        {
            _match = match;
            _rng = new SeedRandom(seed);
            _nextCircleId = 1;
            _nextSpawnAt = Time.unscaledTime;
            EnsureSpawnAreaDebugOverlay();

            if (_pool == null)
            {
                var parent = playArea != null ? playArea : (RectTransform)transform;
                _pool = new SimpleObjectPool<PooledCircle>(prefab, parent, 16);
            }
        }

        public void Stop()
        {
            _match = null;
        }

        private void OnValidate()
        {
            EnsureSpawnAreaDebugOverlay();
        }

        private void Update()
        {
            if (_match == null || !_match.IsRunning || config == null || _pool == null || playArea == null)
                return;

            if (Time.unscaledTime < _nextSpawnAt)
                return;

            SpawnOne();
            var gap = Mathf.Lerp(config.minSpawnInterval, config.maxSpawnInterval, _rng.NextFloat01());
            var cadence = _match.SpawnCadenceGapMultiplier;
            _nextSpawnAt = Time.unscaledTime + gap * cadence;
        }

        private void SpawnOne()
        {
            var kindIndex = _rng.PickWeightedIndex(config.weights);
            var kind = (CircleKind)Mathf.Clamp(kindIndex, 0, 3);

            var life = Mathf.Lerp(config.minLifetime, config.maxLifetime, _rng.NextFloat01());
            var m = config.margin;
            var ax = _rng.NextFloat01() * (1f - 2f * m) + m;
            var ay = _rng.NextFloat01() * (1f - 2f * m) + m;
            var size = _rng.NextFloat01() * 0.06f + 0.07f;

            var circle = _pool.Get();
            var rt = (RectTransform)circle.transform;
            rt.SetParent(playArea, false);

            var anchorMin = new Vector2(ax - size * 0.5f, ay - size * 0.5f);
            var anchorMax = new Vector2(ax + size * 0.5f, ay + size * 0.5f);

            circle.Init(_match!, config, _nextCircleId++, kind, life, anchorMin, anchorMax);
            _match!.TrackActiveCircle(circle);
        }

        public void Despawn(PooledCircle circle)
        {
            if (_pool == null)
                return;
            circle.ClearHandlers();
            _pool.Release(circle);
        }

        private void EnsureSpawnAreaDebugOverlay()
        {
            if (playArea == null)
                return;

            var existing = playArea.Find(spawnAreaDebugOverlayName);
            if (!showSpawnAreaDebugOverlay)
            {
                if (existing != null)
                    existing.gameObject.SetActive(false);
                return;
            }

            Image img;
            RectTransform rt;
            if (existing == null)
            {
                var go = new GameObject(spawnAreaDebugOverlayName, typeof(RectTransform), typeof(Image));
                rt = go.GetComponent<RectTransform>();
                rt.SetParent(playArea, false);
                img = go.GetComponent<Image>();
                img.raycastTarget = false;
            }
            else
            {
                rt = (RectTransform)existing;
                img = existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>();
            }

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetAsFirstSibling();
            img.color = spawnAreaDebugColor;
            img.raycastTarget = false;
            img.enabled = true;
        }
    }
}
