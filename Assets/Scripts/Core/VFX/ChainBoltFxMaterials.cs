using UnityEngine;

namespace TapBrawl.Core.VFX
{
    internal static class ChainBoltFxMaterials
    {
        private static Material? _runtimeFallback;

        /// <summary>
        /// Если материал не задан в префабе — пробуем URP Particles/Unlit, иначе Sprites/Default.
        /// </summary>
        public static Material? TryRuntimeFallback()
        {
            if (_runtimeFallback != null)
                return _runtimeFallback;

            Shader? sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null)
                sh = Shader.Find("Particles/Standard Unlit");
            if (sh == null)
                sh = Shader.Find("Sprites/Default");

            if (sh == null)
                return null;

            _runtimeFallback = new Material(sh)
            {
                name = "ChainBolt_RuntimeFallback",
                hideFlags = HideFlags.HideAndDontSave,
            };

            if (_runtimeFallback.HasProperty("_BaseColor"))
                _runtimeFallback.SetColor("_BaseColor", Color.white);
            if (_runtimeFallback.HasProperty("_Color"))
                _runtimeFallback.SetColor("_Color", Color.white);

            return _runtimeFallback;
        }
    }
}
