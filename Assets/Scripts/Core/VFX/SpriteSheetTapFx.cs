using TapBrawl.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.Core.VFX
{
    /// <summary>One-shot UI VFX from a sprite sheet (hit, bomb, miss, etc.).</summary>
    public static class SpriteSheetTapFx
    {
        public static bool TrySpawn(
            RectTransform parent,
            RectTransform source,
            SpriteSheetAnimSet anim,
            Color tint)
        {
            anim = anim.WithValidFramesOnly();
            if (!anim.HasPlayableTapFx || parent == null || source == null)
                return false;

            var go = new GameObject("SpriteSheetTapFx", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.transform.SetAsLastSibling();

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.position = source.position;
            var sourceSize = Mathf.Max(source.rect.width, source.rect.height, 56f);
            rt.sizeDelta = Vector2.one * sourceSize * Mathf.Max(0.5f, anim.scale);

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.color = tint;

            var animator = go.AddComponent<SpriteSheetUiAnimator>();
            animator.Configure(anim.frames, anim.fps, shouldLoop: false, shouldDestroyWhenComplete: true);
            return true;
        }
    }
}
