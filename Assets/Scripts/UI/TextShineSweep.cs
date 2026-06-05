using TMPro;
using UnityEngine;

namespace TapBrawl.UI
{
    /// <summary>Анимация полосы света слева направо только по глифам TMP.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class TextShineSweep : MonoBehaviour
    {
        private static readonly int ShineProgressId = Shader.PropertyToID("_ShineProgress");
        private static readonly int ShineBoundsId = Shader.PropertyToID("_ShineBounds");

        [SerializeField] private Shader? shineShader;
        [SerializeField, Range(0.01f, 0.5f)] private float shineWidth = 0.12f;
        [SerializeField, Range(0f, 4f)] private float shineIntensity = 2.2f;
        [SerializeField] private Color shineColor = new(1f, 1f, 1f, 1f);

        private TMP_Text _text = null!;
        private Material? _material;
        private float _progress;

        public float Progress
        {
            get => _progress;
            set
            {
                _progress = Mathf.Clamp01(value);
                ApplyProgress();
            }
        }

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            EnsureMaterial();
        }

        private void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
        }

        public void RefreshBounds()
        {
            if (_material == null)
                return;

            _text.ForceMeshUpdate();
            var bounds = _text.textBounds;
            _material.SetVector(ShineBoundsId, new Vector4(bounds.min.x, bounds.max.x, 0f, 0f));
        }

        private void EnsureMaterial()
        {
            if (_material != null)
                return;

            var shader = shineShader != null
                ? shineShader
                : Shader.Find("Tap/TMP Shine Sweep");

            if (shader == null)
            {
                Debug.LogError("TextShineSweep: шейдер Tap/TMP Shine Sweep не найден.");
                return;
            }

            var source = _text.fontSharedMaterial;
            _material = new Material(source) { shader = shader };
            _material.SetFloat("_ShineWidth", shineWidth);
            _material.SetFloat("_ShineIntensity", shineIntensity);
            _material.SetColor("_ShineColor", shineColor);
            _text.fontMaterial = _material;
            RefreshBounds();
            ApplyProgress();
        }

        private void ApplyProgress()
        {
            if (_material == null)
                return;

            _material.SetFloat(ShineProgressId, _progress);
        }
    }
}
