using TapBrawl.Core;
using TapBrawl.Core.VFX;
using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>Полноэкранный оверлей загрузки: блокирует ввод и показывает статус.</summary>
    public sealed class LoadingOverlay : MonoBehaviour
    {
        private static Sprite? _cachedSpinnerSprite;
        private static Sprite? _cachedBallSprite;

        private enum LoadingIndicatorStyle
        {
            ExplodingBalls = 0,
            Spinner = 1,
        }

        [SerializeField] private CanvasGroup? canvasGroup;
        [SerializeField] private Text? statusText;
        [SerializeField] private RectTransform? spinner;
        [SerializeField] private float spinnerSpeed = 220f;
        [SerializeField] private LoadingIndicatorStyle indicatorStyle = LoadingIndicatorStyle.ExplodingBalls;
        [SerializeField] private CircleSpawnConfig? circleFxConfig;
        [SerializeField] private RectTransform? explodingBallsRoot;
        [SerializeField, Min(0.1f)] private float firstExplosionDelay = 0.45f;
        [SerializeField, Min(0.1f)] private float explosionInterval = 0.36f;
        [SerializeField, Min(0.1f)] private float explosionResetDelay = 0.62f;

        private bool _visible;
        private readonly Image?[] _ballImages = new Image?[3];
        private readonly RectTransform?[] _ballRects = new RectTransform?[3];
        private float _ballsElapsed;
        private int _ballsCycle = -1;
        private int _explodedMask;

        public static LoadingOverlay EnsureOnCanvas(Transform canvasTransform)
        {
            var existing = canvasTransform.GetComponentInChildren<LoadingOverlay>(true);
            if (existing != null)
                return existing;

            var root = new GameObject(
                "LoadingOverlay",
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(LoadingOverlay));
            root.transform.SetParent(canvasTransform, false);
            StretchFull(root.GetComponent<RectTransform>());

            var overlay = root.GetComponent<LoadingOverlay>();
            overlay.BuildUi();
            root.SetActive(false);
            return overlay;
        }

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            // Prefab/динамически созданный оверлей может иметь пустой sprite у Image,
            // поэтому вращение не видно. Подставляем процедурный спиннер один раз.
            EnsureSpinnerSprite();
            EnsureExplodingBallsUi();
            ApplyIndicatorStyle();
            SetVisible(false);
        }

        private void EnsureSpinnerSprite()
        {
            if (spinner == null)
                return;

            var spinnerImg = spinner.GetComponent<Image>();
            if (spinnerImg == null)
                return;

            if (spinnerImg.sprite != null)
                return;

            if (_cachedSpinnerSprite == null)
                _cachedSpinnerSprite = CreateSpinnerSprite();

            spinnerImg.sprite = _cachedSpinnerSprite;
            spinnerImg.type = Image.Type.Simple;
        }

        private static Sprite CreateSpinnerSprite()
        {
            const int size = 64;
            const float pixelsPerUnit = size; // 1 sprite unit ~ 1x1 texture in UI scaling

            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.hideFlags = HideFlags.DontSave;

            var pixels = new Color32[size * size];

            // Рисуем сегмент-дугу (полный круг вращать бессмысленно: он симметричен).
            const float innerR = 0.36f;
            const float outerR = 0.48f;
            const float tailStartDeg = 270f; // "вверх"
            const float tailSpanDeg = 70f;

            for (var y = 0; y < size; y++)
            {
                var ny = (y + 0.5f) / size * 2f - 1f; // -1..1
                for (var x = 0; x < size; x++)
                {
                    var nx = (x + 0.5f) / size * 2f - 1f; // -1..1
                    var r = Mathf.Sqrt(nx * nx + ny * ny);

                    var idx = y * size + x;
                    if (r < innerR || r > outerR)
                    {
                        pixels[idx] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    var angleRad = Mathf.Atan2(ny, nx); // -pi..pi
                    var angleDeg = angleRad * Mathf.Rad2Deg;
                    if (angleDeg < 0f)
                        angleDeg += 360f; // 0..360

                    // delta = насколько "вперед" от tailStartDeg мы ушли по окружности (0..360).
                    var delta = Mathf.Repeat(angleDeg - tailStartDeg, 360f);
                    if (delta > tailSpanDeg)
                    {
                        pixels[idx] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    // Плавное затухание дуги.
                    var t = 1f - (delta / tailSpanDeg); // 1..0

                    // Доп. затухание по радиусу (чтобы дуга выглядела "объемнее").
                    var rt = Mathf.InverseLerp(innerR, outerR, r); // 0..1
                    var alpha01 = t * Mathf.Lerp(0.6f, 1f, rt);
                    alpha01 = Mathf.Clamp01(alpha01);

                    var a = (byte)(alpha01 * 255f);
                    pixels[idx] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        private static Sprite CreateBallSprite()
        {
            const int size = 64;
            const float pixelsPerUnit = size;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.hideFlags = HideFlags.DontSave;

            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                var ny = (y + 0.5f) / size * 2f - 1f;
                for (var x = 0; x < size; x++)
                {
                    var nx = (x + 0.5f) / size * 2f - 1f;
                    var r = Mathf.Sqrt(nx * nx + ny * ny);
                    var idx = y * size + x;
                    if (r > 0.48f)
                    {
                        pixels[idx] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    var highlight = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(nx, ny), new Vector2(-0.18f, 0.22f)) / 0.72f);
                    var edge = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.43f, 0.48f, r));
                    var alpha = (byte)(edge * 255f);
                    var shade = (byte)Mathf.Lerp(205f, 255f, highlight * 0.5f);
                    pixels[idx] = new Color32(shade, shade, shade, alpha);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        public void Show(string message = "Загрузка…")
        {
            SetStatusVisible(true);
            SetMessage(message);
            SetVisible(true);
        }

        public void ShowAnimationOnly() => Show(string.Empty);

        public void Hide() => SetVisible(false);

        public void SetFxConfig(CircleSpawnConfig config) => circleFxConfig = config;

        public void SetMessage(string message)
        {
            if (statusText == null)
                return;

            statusText.text = message;
            SetStatusVisible(!string.IsNullOrEmpty(message));
        }

        private void SetStatusVisible(bool visible)
        {
            if (statusText != null)
                statusText.gameObject.SetActive(visible);
        }

        private void Update()
        {
            if (!_visible)
                return;

            if (indicatorStyle == LoadingIndicatorStyle.Spinner)
            {
                if (spinner != null)
                    spinner.Rotate(0f, 0f, -spinnerSpeed * Time.unscaledDeltaTime);
                return;
            }

            UpdateExplodingBalls();
        }

        private void SetVisible(bool visible)
        {
            _visible = visible;
            gameObject.SetActive(visible);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.blocksRaycasts = visible;
                canvasGroup.interactable = visible;
            }

            if (visible)
                ResetExplodingBalls();
        }

        public void BuildUi()
        {
            var backdrop = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.62f);
            backdrop.raycastTarget = true;

            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(transform, false);
            StretchFull(content.GetComponent<RectTransform>());
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 28f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var spinnerGo = new GameObject("Spinner", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            spinnerGo.transform.SetParent(content.transform, false);
            spinner = spinnerGo.GetComponent<RectTransform>();
            spinner.sizeDelta = new Vector2(72f, 72f);
            var spinnerImg = spinnerGo.GetComponent<Image>();
            spinnerImg.color = new Color(1f, 1f, 1f, 0.92f);
            spinnerImg.raycastTarget = false;
            var spinnerLe = spinnerGo.GetComponent<LayoutElement>();
            spinnerLe.minWidth = 72f;
            spinnerLe.minHeight = 72f;
            spinnerLe.preferredWidth = 72f;
            spinnerLe.preferredHeight = 72f;

            var statusGo = new GameObject("StatusText", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            statusGo.transform.SetParent(content.transform, false);
            statusText = statusGo.GetComponent<Text>();
            statusText.font = UiFont();
            statusText.text = "Загрузка…";
            statusText.fontSize = 30;
            statusText.color = Color.white;
            statusText.alignment = TextAnchor.MiddleCenter;
            var statusLe = statusGo.GetComponent<LayoutElement>();
            statusLe.minHeight = 48f;
            statusLe.preferredHeight = 48f;

            EnsureSpinnerSprite();
            EnsureExplodingBallsUi();
            ApplyIndicatorStyle();
        }

        private void EnsureExplodingBallsUi()
        {
            if (explodingBallsRoot == null)
                explodingBallsRoot = transform.Find("Content/ExplodingBalls") as RectTransform;

            if (explodingBallsRoot == null && spinner == null && transform.Find("Content") == null)
                return;

            if (_cachedBallSprite == null)
                _cachedBallSprite = CreateBallSprite();

            if (explodingBallsRoot == null)
            {
                var parent = spinner != null && spinner.parent != null ? spinner.parent : transform;
                var go = new GameObject("ExplodingBalls", typeof(RectTransform), typeof(LayoutElement));
                go.transform.SetParent(parent, false);
                if (spinner != null)
                    go.transform.SetSiblingIndex(spinner.GetSiblingIndex());

                explodingBallsRoot = go.GetComponent<RectTransform>();
                explodingBallsRoot.sizeDelta = new Vector2(180f, 82f);
                var le = go.GetComponent<LayoutElement>();
                le.minWidth = 180f;
                le.minHeight = 82f;
                le.preferredWidth = 180f;
                le.preferredHeight = 82f;
            }

            var colors = new[]
            {
                new Color(0.28f, 0.62f, 1f, 1f),
                new Color(1f, 0.82f, 0.16f, 1f),
                new Color(0.72f, 0.43f, 1f, 1f),
            };
            var xPositions = new[] { -54f, 0f, 54f };
            var sizes = new[] { 48f, 62f, 48f };

            for (var i = 0; i < _ballImages.Length; i++)
            {
                var child = explodingBallsRoot.Find($"Ball{i + 1}") as RectTransform;
                if (child == null)
                {
                    var go = new GameObject($"Ball{i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    go.transform.SetParent(explodingBallsRoot, false);
                    child = go.GetComponent<RectTransform>();
                }

                child.anchorMin = child.anchorMax = new Vector2(0.5f, 0.5f);
                child.pivot = new Vector2(0.5f, 0.5f);
                child.anchoredPosition = new Vector2(xPositions[i], 0f);
                child.sizeDelta = Vector2.one * sizes[i];

                var image = child.GetComponent<Image>();
                image.sprite = _cachedBallSprite;
                image.color = colors[i];
                image.preserveAspect = true;
                image.raycastTarget = false;

                _ballRects[i] = child;
                _ballImages[i] = image;
            }
        }

        private void ApplyIndicatorStyle()
        {
            if (spinner != null)
                spinner.gameObject.SetActive(indicatorStyle == LoadingIndicatorStyle.Spinner);
            if (explodingBallsRoot != null)
                explodingBallsRoot.gameObject.SetActive(indicatorStyle == LoadingIndicatorStyle.ExplodingBalls);
        }

        private void ResetExplodingBalls()
        {
            _ballsElapsed = 0f;
            _ballsCycle = -1;
            _explodedMask = 0;
            ResetBallVisuals();
        }

        private void ResetBallVisuals()
        {
            for (var i = 0; i < _ballImages.Length; i++)
            {
                if (_ballImages[i] == null || _ballRects[i] == null)
                    continue;

                var color = _ballImages[i]!.color;
                color.a = 1f;
                _ballImages[i]!.color = color;
                _ballRects[i]!.localScale = Vector3.one;
            }
        }

        private void UpdateExplodingBalls()
        {
            if (explodingBallsRoot == null)
                EnsureExplodingBallsUi();

            var cycleDuration = firstExplosionDelay + explosionInterval * _ballImages.Length + explosionResetDelay;
            _ballsElapsed += Time.unscaledDeltaTime;
            var cycle = Mathf.FloorToInt(_ballsElapsed / cycleDuration);
            if (cycle != _ballsCycle)
            {
                _ballsCycle = cycle;
                _explodedMask = 0;
                ResetBallVisuals();
            }

            var cycleTime = _ballsElapsed - cycle * cycleDuration;
            var appear = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(cycleTime / firstExplosionDelay));
            for (var i = 0; i < _ballImages.Length; i++)
            {
                var exploded = (_explodedMask & (1 << i)) != 0;
                if (exploded || _ballImages[i] == null || _ballRects[i] == null)
                    continue;

                var color = _ballImages[i]!.color;
                color.a = appear;
                _ballImages[i]!.color = color;
                var pulse = 1f + Mathf.Sin((_ballsElapsed + i * 0.13f) * 7.5f) * 0.035f;
                _ballRects[i]!.localScale = Vector3.one * appear * pulse;
            }

            for (var i = 0; i < _ballImages.Length; i++)
            {
                var explodeAt = firstExplosionDelay + explosionInterval * i;
                if (cycleTime < explodeAt || (_explodedMask & (1 << i)) != 0)
                    continue;

                SpawnBallExplosion(i);
                _explodedMask |= 1 << i;
            }
        }

        private void SpawnBallExplosion(int index)
        {
            var source = _ballRects[index];
            var image = _ballImages[index];
            if (source == null || image == null || explodingBallsRoot == null)
                return;

            var color = image.color;
            color.a = 0f;
            image.color = color;

            var anim = circleFxConfig != null ? circleFxConfig.normalHitAnim : default;
            if (SpriteSheetTapFx.TrySpawn(explodingBallsRoot, source, anim, BallColor(index, 1f)))
                return;

            CircleTapJuiceFx.Spawn(
                explodingBallsRoot,
                source,
                image.sprite,
                KindForBall(index),
                circleFxConfig != null ? circleFxConfig.tapPopLifetimeSec : 0.34f,
                circleFxConfig != null ? circleFxConfig.tapPopScale : 1.75f,
                circleFxConfig != null ? circleFxConfig.bombExplosionScale : 4.8f,
                chainLightningStrike: false);
        }

        private static Color BallColor(int index, float alpha)
        {
            var color = index switch
            {
                1 => new Color(1f, 0.82f, 0.16f, alpha),
                2 => new Color(0.72f, 0.43f, 1f, alpha),
                _ => new Color(0.28f, 0.62f, 1f, alpha),
            };
            return color;
        }

        private static CircleKind KindForBall(int index) =>
            index switch
            {
                1 => CircleKind.Gold,
                2 => CircleKind.Phantom,
                _ => CircleKind.Normal,
            };

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static Font UiFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f != null)
                return f;
            f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f != null ? f : Font.CreateDynamicFontFromOSFont("Arial", 16);
        }
    }
}
