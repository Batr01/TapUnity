using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace TapBrawl.UI
{
    /// <summary>Экран компании после Unity Splash: надпись + блик по тексту.</summary>
    public sealed class CompanySplashView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup? canvasGroup;
        [SerializeField] private TextMeshProUGUI? titleText;
        [SerializeField] private TextShineSweep? shineSweep;
        [SerializeField] private string companyName = "Adiponya";
        [SerializeField, Min(0f)] private float fadeInDuration = 0.35f;
        [SerializeField, Min(0f)] private float shineDuration = 1.1f;
        [SerializeField, Min(0f)] private float holdAfterShine = 0.45f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.3f;

        public async Task PlayAsync(CancellationToken cancellationToken = default)
        {
            if (titleText != null)
                titleText.text = companyName;

            shineSweep?.RefreshBounds();

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            gameObject.SetActive(true);
            await FadeAsync(0f, 1f, fadeInDuration, cancellationToken).ConfigureAwait(true);
            await AnimateShineAsync(cancellationToken).ConfigureAwait(true);

            if (holdAfterShine > 0f)
                await Task.Delay(Mathf.RoundToInt(holdAfterShine * 1000f), cancellationToken).ConfigureAwait(true);

            await FadeAsync(1f, 0f, fadeOutDuration, cancellationToken).ConfigureAwait(true);
            gameObject.SetActive(false);
        }

        private async Task AnimateShineAsync(CancellationToken cancellationToken)
        {
            if (shineSweep == null || shineDuration <= 0f)
                return;

            var elapsed = 0f;
            while (elapsed < shineDuration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                elapsed += Time.unscaledDeltaTime;
                shineSweep.Progress = Mathf.Clamp01(elapsed / shineDuration);
                await Task.Yield();
            }

            shineSweep.Progress = 1f;
        }

        private async Task FadeAsync(float from, float to, float duration, CancellationToken cancellationToken)
        {
            if (canvasGroup == null || duration <= 0f)
            {
                if (canvasGroup != null)
                    canvasGroup.alpha = to;
                return;
            }

            var elapsed = 0f;
            canvasGroup.alpha = from;
            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                await Task.Yield();
            }

            canvasGroup.alpha = to;
        }
    }
}
