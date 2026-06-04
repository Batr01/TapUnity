using System.Collections;
using UnityEngine;

namespace TapBrawl.UI
{
    /// <summary>Анимация выезда модалки снизу вверх.</summary>
    public static class UiModalSlideAnimator
    {
        public const float DefaultDuration = 0.22f;

        private static readonly string[] ContentNames = { "Panel", "SkillsScreen", "Content" };

        public static void Play(MonoBehaviour host, Transform modalRoot, float duration = DefaultDuration)
        {
            if (duration <= 0f)
                return;

            var target = ResolveAnimTarget(modalRoot);
            if (target == null)
                return;

            host.StartCoroutine(SlideUp(modalRoot, target, duration));
        }

        public static RectTransform? ResolveAnimTarget(Transform modalRoot)
        {
            foreach (var name in ContentNames)
            {
                var t = modalRoot.Find(name);
                if (t != null)
                    return t as RectTransform;
            }

            foreach (Transform c in modalRoot)
            {
                if (c.name == "Backdrop")
                    continue;
                return c as RectTransform;
            }

            return null;
        }

        private static IEnumerator SlideUp(Transform modalRoot, RectTransform target, float duration)
        {
            Canvas.ForceUpdateCanvases();

            var restPos = target.anchoredPosition;
            var backdrop = GetOrCreateBackdropGroup(modalRoot);
            float dist = target.rect.height > 1f ? target.rect.height : Screen.height;
            var startPos = restPos - new Vector2(0f, dist);

            target.anchoredPosition = startPos;
            if (backdrop != null)
                backdrop.alpha = 0f;

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                float e = 1f - Mathf.Pow(1f - k, 3f);
                target.anchoredPosition = Vector2.LerpUnclamped(startPos, restPos, e);
                if (backdrop != null)
                    backdrop.alpha = e;
                yield return null;
            }

            target.anchoredPosition = restPos;
            if (backdrop != null)
                backdrop.alpha = 1f;
        }

        private static CanvasGroup? GetOrCreateBackdropGroup(Transform modalRoot)
        {
            var b = modalRoot.Find("Backdrop");
            if (b == null)
                return null;

            var cg = b.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = b.gameObject.AddComponent<CanvasGroup>();
            return cg;
        }
    }
}
