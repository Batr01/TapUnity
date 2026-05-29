using TapBrawl.Core.VFX;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TapBrawl.Core
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public sealed class PooledCircle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private enum CircleRuntimeState : byte
        {
            Inactive = 0,
            Idle = 1,
        }

        private Button _button;
        private Image _image;
        private SpriteSheetUiAnimator? _idleAnimator;
        private MatchController _match;
        private CircleSpawnConfig _config;

        public int CircleId { get; private set; }
        public CircleKind Kind { get; private set; }
        public float DespawnAt { get; private set; }
        /// <summary>Unscaled время появления (для «идеального» тапа).</summary>
        public float SpawnAtUnscaled { get; private set; }
        /// <summary>Заданная длительность жизни круга (сек).</summary>
        public float LifetimeSec { get; private set; }
        public bool PhantomVisiblePhase { get; private set; }
        public Sprite? CurrentVisualSprite => _image != null ? _image.sprite : null;
        public float TapPopLifetimeSec => _config != null ? _config.tapPopLifetimeSec : 0.34f;
        public float TapPopScale => _config != null ? _config.tapPopScale : 1.75f;
        public float BombExplosionScale => _config != null ? _config.bombExplosionScale : 4.8f;

        private const float PerfectTapElapsedFractionMin = 2f / 3f;

        private RectTransform? _rt;
        private Vector2 _centerAnchored;
        private float _anchorSpan;
        private float _nextBlink;
        private CircleRuntimeState _runtimeState = CircleRuntimeState.Inactive;
        private bool _isPressed;
        private static readonly Color Normal = new(0.25f, 0.55f, 1f, 1f);
        private static readonly Color Gold = new(1f, 0.85f, 0.2f, 1f);
        private static readonly Color Bomb = new(0.9f, 0.2f, 0.2f, 1f);
        private static readonly Color Phantom = new(0.6f, 0.4f, 1f, 1f);

        /// <summary>Середина диапазона спавна <see cref="CircleSpawner"/> (<c>0.07…0.13</c> по якорям) — один размер при обмане «все бомбы».</summary>
        private const float BombDeceptionUniformAnchorSpan = 0.1f;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _image = GetComponent<Image>();
            _idleAnimator = GetComponent<SpriteSheetUiAnimator>();
            _button.onClick.AddListener(OnClicked);
        }

        public void Init(
            MatchController match,
            CircleSpawnConfig config,
            int circleId,
            CircleKind kind,
            float lifetimeSec,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            _match = match;
            _config = config;
            CircleId = circleId;
            Kind = kind;
            SpawnAtUnscaled = Time.unscaledTime;
            LifetimeSec = Mathf.Max(0.0001f, lifetimeSec);
            DespawnAt = SpawnAtUnscaled + LifetimeSec;
            _nextBlink = Time.unscaledTime;
            PhantomVisiblePhase = true;
            _runtimeState = CircleRuntimeState.Idle;
            _isPressed = false;

            _rt = (RectTransform)transform;
            _centerAnchored = (anchorMin + anchorMax) * 0.5f;
            _anchorSpan = Mathf.Max(0.0001f, anchorMax.x - anchorMin.x);
            ApplyAnchorVisualMultiplier(match.ActiveCircleVisualSizeMultiplier);
            ResetIdleScale();
            RefreshVisualState();
            ApplyIdleSpriteSheet();
        }

        /// <summary>Спрайт/цвет по типу или «пелена» соперника: все цели выглядят как boom.png (логика тапа не меняется).</summary>
        public void RefreshVisualState()
        {
            if (_image == null || _match == null)
                return;

            if (!_isPressed && TryApplyIdleSpriteSheetVisual())
                return;

            var sp = CurrentSpriteForKind(Kind);
            var redDeception = _match.IsOpponentRedDeceptionVisualActive;
            var useBombMask = UseBombDeceptionMask();

            if (useBombMask)
            {
                _image.sprite = _config.bombTargetSprite;
                if (Kind == CircleKind.Phantom)
                {
                    _image.color = Color.white;
                    ApplyPhantomAlpha();
                }
                else
                {
                    _image.color = Color.white;
                    _image.canvasRenderer.SetAlpha(1f);
                }

                return;
            }

            if (sp != null)
                _image.sprite = sp;

            if (redDeception)
            {
                var c = new Color(0.92f, 0.14f, 0.11f, 1f);
                if (Kind == CircleKind.Phantom)
                    c.a = PhantomVisiblePhase ? 0.95f : 0.18f;
                _image.color = c;
                return;
            }

            if (sp != null)
            {
                if (Kind == CircleKind.Phantom)
                {
                    _image.color = Color.white;
                    ApplyPhantomAlpha();
                }
                else
                {
                    _image.color = Color.white;
                    _image.canvasRenderer.SetAlpha(1f);
                }

                return;
            }

            _image.color = Kind switch
            {
                CircleKind.Gold => Gold,
                CircleKind.Bomb => Bomb,
                CircleKind.Phantom => Phantom,
                _ => Normal,
            };

            if (Kind == CircleKind.Phantom)
                ApplyPhantomAlpha();
            else
                _image.canvasRenderer.SetAlpha(1f);
        }

        /// <summary>Базовый span якорей задаётся при спавне; <paramref name="multiplier"/> — множитель от скилла (1 = как в конфиге).</summary>
        public void ApplyAnchorVisualMultiplier(float multiplier)
        {
            if (_rt == null || _match == null)
                return;
            var m = Mathf.Max(0.05f, multiplier);
            var span = UseBombDeceptionMask() ? BombDeceptionUniformAnchorSpan : _anchorSpan;
            var half = span * 0.5f * m;
            var h = new Vector2(half, half);
            _rt.anchorMin = _centerAnchored - h;
            _rt.anchorMax = _centerAnchored + h;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }

        private void Update()
        {
            if (_runtimeState != CircleRuntimeState.Idle || _config == null)
            {
                ResetIdleScale();
                return;
            }

            ApplyIdlePulseScale();

            if (Kind != CircleKind.Phantom)
                return;
            if (Time.unscaledTime < _nextBlink)
                return;
            var cadence = _match != null ? _match.SpawnCadenceGapMultiplier : 1f;
            var blinkInterval = Mathf.Max(0.02f, _config.phantomBlinkInterval * cadence);
            _nextBlink = Time.unscaledTime + blinkInterval;
            PhantomVisiblePhase = !PhantomVisiblePhase;
            RefreshVisualState();
        }

        private Sprite? SpriteForKind(CircleKind k) =>
            _config == null
                ? null
                : k switch
                {
                    CircleKind.Gold => _config.goldTargetSprite,
                    CircleKind.Bomb => _config.bombTargetSprite,
                    CircleKind.Phantom => _config.phantomTargetSprite,
                    _ => _config.normalTargetSprite,
                };

        private Sprite? CurrentSpriteForKind(CircleKind k)
        {
            if (_config == null)
                return null;
            var idle = SpriteForKind(k);
            if (!_isPressed)
                return idle;
            return k switch
            {
                CircleKind.Gold => _config.goldPressedSprite != null ? _config.goldPressedSprite : idle,
                CircleKind.Bomb => _config.bombPressedSprite != null ? _config.bombPressedSprite : idle,
                CircleKind.Phantom => _config.phantomPressedSprite != null ? _config.phantomPressedSprite : idle,
                _ => _config.normalPressedSprite != null ? _config.normalPressedSprite : idle,
            };
        }

        private void ApplyPhantomAlpha()
        {
            if (_match != null && _match.IsOpponentRedDeceptionVisualActive)
            {
                if (UseBombDeceptionMask())
                {
                    var c = Color.white;
                    c.a = PhantomVisiblePhase ? 0.95f : 0.18f;
                    _image.color = c;
                    return;
                }

                var cTint = new Color(0.92f, 0.14f, 0.11f, PhantomVisiblePhase ? 0.95f : 0.18f);
                _image.color = cTint;
                return;
            }

            var col = _image.color;
            col.a = PhantomVisiblePhase ? 0.95f : 0.15f;
            _image.color = col;
        }

        private bool TryApplyIdleSpriteSheetVisual()
        {
            if (_config == null || _image == null || _match == null)
                return false;
            if (UseBombDeceptionMask())
                return false;

            var idle = _config.GetIdleAnim(Kind);
            if (!idle.HasFrames)
            {
                if (_idleAnimator != null)
                    _idleAnimator.enabled = false;
                return false;
            }

            if (_idleAnimator != null)
            {
                _idleAnimator.enabled = true;
                _idleAnimator.Configure(idle.frames, idle.fps, shouldLoop: true, shouldDestroyWhenComplete: false);
            }

            _image.color = Color.white;
            if (Kind == CircleKind.Phantom)
                ApplyPhantomAlpha();
            else
                _image.canvasRenderer.SetAlpha(1f);
            return true;
        }

        private void ApplyIdleSpriteSheet()
        {
            if (_isPressed)
                return;
            TryApplyIdleSpriteSheetVisual();
        }

        private void ApplyIdlePulseScale()
        {
            if (_rt == null || _config == null)
            {
                ResetIdleScale();
                return;
            }

            var press = _isPressed ? Mathf.Clamp(_config.pressedScaleMultiplier, 0.65f, 1f) : 1f;
            if (_config.HasIdleAnim(Kind))
            {
                _rt.localScale = new Vector3(press, press, 1f);
                return;
            }

            if (!_config.enableIdlePulse)
            {
                _rt.localScale = new Vector3(press, press, 1f);
                return;
            }

            var amplitude = Mathf.Clamp(_config.idlePulseAmplitude, 0f, 0.1f);
            if (amplitude <= 0f)
            {
                _rt.localScale = new Vector3(press, press, 1f);
                return;
            }

            var frequency = Mathf.Max(0.01f, _config.idlePulseFrequency);
            var phaseOffset = (CircleId % 11) * 0.37f;
            var t = Time.unscaledTime * (Mathf.PI * 2f * frequency) + phaseOffset;
            var s = (1f + Mathf.Sin(t) * amplitude) * press;
            _rt.localScale = new Vector3(s, s, 1f);
        }

        private void ResetIdleScale()
        {
            if (_rt == null)
                return;
            _rt.localScale = Vector3.one;
        }

        private bool UseBombDeceptionMask() =>
            _match != null &&
            _match.IsOpponentRedDeceptionVisualActive &&
            _config != null &&
            _config.bombTargetSprite != null;

        /// <summary>Идеально: успешный положительный тап в последней трети жизни цели.</summary>
        public bool IsPerfectTimingPositiveTap(float nowUnscaled)
        {
            var elapsed = nowUnscaled - SpawnAtUnscaled;
            var frac = elapsed / LifetimeSec;
            return frac >= PerfectTapElapsedFractionMin;
        }

        private void OnClicked()
        {
            if (_match == null || !_match.IsRunning)
                return;
            _match.RegisterTap(this);
        }

        public void ClearHandlers()
        {
            _runtimeState = CircleRuntimeState.Inactive;
            _isPressed = false;
            if (_idleAnimator != null)
                _idleAnimator.enabled = false;
            ResetIdleScale();
            _match = null;
            _config = null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_runtimeState != CircleRuntimeState.Idle || _match == null || !_match.IsRunning)
                return;
            _isPressed = true;
            if (_idleAnimator != null)
                _idleAnimator.enabled = false;
            RefreshVisualState();
            ApplyIdlePulseScale();
        }

        public void OnPointerUp(PointerEventData eventData) => ReleasePressedVisual();

        public void OnPointerExit(PointerEventData eventData) => ReleasePressedVisual();

        private void ReleasePressedVisual()
        {
            if (!_isPressed)
                return;
            _isPressed = false;
            RefreshVisualState();
            ApplyIdleSpriteSheet();
            ApplyIdlePulseScale();
        }
    }
}
