using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.Core.VFX
{
    [RequireComponent(typeof(Image))]
    public sealed class SpriteSheetUiAnimator : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames = System.Array.Empty<Sprite>();
        [SerializeField, Min(1f)] private float framesPerSecond = 12f;
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private bool loop = true;
        [SerializeField] private bool destroyWhenComplete;

        private Image _image = null!;
        private float _elapsed;
        private bool _isPlaying;
        private bool _completed;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        private void OnEnable()
        {
            ResetPlayback();
            _isPlaying = playOnEnable;
        }

        public void Configure(
            Sprite[] newFrames,
            float fps,
            bool shouldLoop,
            bool shouldDestroyWhenComplete,
            bool shouldUseUnscaledTime = true)
        {
            frames = newFrames ?? System.Array.Empty<Sprite>();
            framesPerSecond = Mathf.Max(1f, fps);
            loop = shouldLoop;
            destroyWhenComplete = shouldDestroyWhenComplete;
            useUnscaledTime = shouldUseUnscaledTime;
            ResetPlayback();
            _isPlaying = true;
        }

        private void ResetPlayback()
        {
            _elapsed = 0f;
            _completed = false;
            ApplyFrame(0);
        }

        private void Update()
        {
            if (!_isPlaying || frames.Length == 0 || _completed)
                return;

            _elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            var frameIndex = Mathf.FloorToInt(_elapsed * framesPerSecond);
            if (loop)
            {
                ApplyFrame(frameIndex % frames.Length);
                return;
            }

            if (frameIndex >= frames.Length)
            {
                ApplyFrame(frames.Length - 1);
                _completed = true;
                if (destroyWhenComplete)
                    Destroy(gameObject);
                return;
            }

            ApplyFrame(frameIndex);
        }

        public void Play()
        {
            _isPlaying = true;
        }

        public void Stop()
        {
            _isPlaying = false;
        }

        private void ApplyFrame(int index)
        {
            if (frames.Length == 0 || _image == null)
                return;

            _image.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        }
    }
}
