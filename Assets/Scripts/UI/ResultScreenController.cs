using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using TapBrawl.Models;
using TapBrawl.Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>
    /// Сцена Result: отправка <c>submit-my-stats</c>, при необходимости ожидание соперника, вывод итога.
    /// Звук: в инспекторе можно назначить фоновую музыку для победы/поражения/ничьей и короткие эффекты для «ты» / соперника.
    /// </summary>
    public sealed class ResultScreenController : MonoBehaviour
    {
        [SerializeField] private BackendConfig backendConfig = null!;
        [SerializeField] private string authSceneName = "Auth";
        [SerializeField] private string lobbySceneName = "Lobby";
        [SerializeField] private Text? statusText;
        [SerializeField] private Text? headlineText;
        [SerializeField] private Text? detailsText;
        [SerializeField] private Button? backToLobbyButton;

        [Header("Звук результата")]
        [Tooltip("Источник для фоновой музыки (loop). Если пусто — будет создан на этом объекте.")]
        [SerializeField] private AudioSource? resultMusicSource;
        [Tooltip("Источник для коротких эффектов при показе результата. Если пусто — второй AudioSource на этом объекте.")]
        [SerializeField] private AudioSource? resultSfxSource;
        [Tooltip("Фон при победе локального игрока (зацикливается).")]
        [SerializeField] private AudioClip? musicIfLocalWon;
        [Tooltip("Фон при поражении локального игрока (зацикливается).")]
        [SerializeField] private AudioClip? musicIfLocalLost;
        [Tooltip("Фон при одинаковом счёте (если не задан — берётся победа/поражение по серверному победителю).")]
        [SerializeField] private AudioClip? musicIfDrawScore;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.75f;

        [Tooltip("Короткий эффект для строки «ты» при появлении итога.")]
        [SerializeField] private AudioClip? sfxLocalPlayerResult;
        [Tooltip("Короткий эффект для блока соперника при появлении итога.")]
        [SerializeField] private AudioClip? sfxOpponentPlayerResult;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
        [Tooltip("Задержка перед звуком соперника (сек), чтобы не накладывались.")]
        [SerializeField] private float opponentSfxDelaySec = 0.12f;
        [Tooltip("Дополнительные one-shot эффекты по порядку после основных (интервал между ними, сек).")]
        [SerializeField] private AudioClip[] extraResultSfx = new AudioClip[0];
        [SerializeField] private float extraSfxIntervalSec = 0.08f;

        private void Awake()
        {
            if (backToLobbyButton != null)
                backToLobbyButton.onClick.AddListener(GoLobby);
            EnsureAudioSources();
        }

        private void OnDestroy() => StopResultMusic();

        private void Start() => _ = RunAsync();

        private async Task RunAsync()
        {
            SetStatus("Загрузка…");

            if (backendConfig == null)
            {
                SetStatus("Backend Config не назначен.");
                return;
            }

            if (!PendingMatchResult.TryConsume(out var pending))
            {
                SetStatus("Нет данных матча. Запустите онлайн-матч из лобби.");
                return;
            }

            try
            {
                var api = new ApiClient(backendConfig);
                var auth = new AuthManager(api);
                if (!await auth.TryRestoreSessionAsync(CancellationToken.None).ConfigureAwait(true))
                {
                    SetStatus("Нужен вход.");
                    if (!string.IsNullOrEmpty(authSceneName))
                        SceneManager.LoadScene(authSceneName, LoadSceneMode.Single);
                    return;
                }

                var session = AuthContext.Current;
                if (session == null || pending.MyPlayerId == Guid.Empty)
                {
                    SetStatus("Нет сессии. Пройдите Boot снова.");
                    return;
                }

                SetStatus("Отправка результата…");
                var body = new SubmitMyMatchStatsBody
                {
                    Score = pending.MyScore,
                    Taps = pending.MyTaps,
                    Misses = pending.MyMisses,
                };

                var sub = await api.MatchesSubmitMyStatsAsync(session.AccessToken, pending.MatchId, body, CancellationToken.None);
                if (!sub.Success || sub.Data == null)
                {
                    SetStatus($"Ошибка отправки: HTTP {sub.StatusCode} {sub.ErrorBody}");
                    return;
                }

                if (sub.Data.Complete && sub.Data.Result != null)
                {
                    ShowResult(sub.Data.Result, pending.MyPlayerId);
                    StartResultAudio(sub.Data.Result, pending.MyPlayerId);
                    return;
                }

                SetStatus("Ждём соперника…");
                for (var i = 0; i < 120; i++)
                {
                    await Task.Delay(500, CancellationToken.None);
                    var get = await api.MatchesResultAsync(pending.MatchId, CancellationToken.None);
                    if (get.Success && get.Data != null)
                    {
                        ShowResult(get.Data, pending.MyPlayerId);
                        StartResultAudio(get.Data, pending.MyPlayerId);
                        return;
                    }
                }

                SetStatus("Таймаут: соперник не отправил результат.");
            }
            catch (Exception ex)
            {
                SetStatus("Ошибка: " + ex.Message);
                Debug.LogException(ex);
            }
        }

        private void ShowResult(MatchResultResponseDto r, Guid myPlayerId)
        {
            SetStatus(string.Empty);

            var me = r.Player1.PlayerId == myPlayerId ? r.Player1 : r.Player2;
            var opp = r.Player1.PlayerId == myPlayerId ? r.Player2 : r.Player1;
            var iWon = r.WinnerPlayerId == myPlayerId;
            var sameScore = r.Player1.Score == r.Player2.Score;

            if (headlineText != null)
            {
                headlineText.text = sameScore
                    ? "Одинаковый счёт"
                    : (iWon ? "Победа!" : "Поражение");
            }

            if (detailsText != null)
            {
                var winnerLine = sameScore
                    ? $"\nПри равном счёте зачтена победа: {(r.Player1.IsWinner ? r.Player1.Username : r.Player2.Username)}\n"
                    : string.Empty;

                detailsText.text =
                    $"Счёт: {me.Username} {me.Score}  —  {opp.Score} {opp.Username}\n" +
                    $"Всего очков (оба): {r.TotalScore}    Разница: {r.ScoreDifference}\n" +
                    $"Длительность: {r.DurationSec} с{winnerLine}\n" +
                    $"── Ты ({me.Username}) ──\n" +
                    $"Тапы: {me.Taps}    Промахи: {me.Misses}\n" +
                    $"Точность: {me.AccuracyPercent:0.##}%    Тапов/сек: {me.TapsPerSecond:0.##}\n\n" +
                    $"── Соперник ({opp.Username}) ──\n" +
                    $"Тапы: {opp.Taps}    Промахи: {opp.Misses}\n" +
                    $"Точность: {opp.AccuracyPercent:0.##}%    Тапов/сек: {opp.TapsPerSecond:0.##}";
            }
        }

        private void StartResultAudio(MatchResultResponseDto r, Guid myPlayerId)
        {
            var iWon = r.WinnerPlayerId == myPlayerId;
            var sameScore = r.Player1.Score == r.Player2.Score;
            StartCoroutine(PlayResultAudioRoutine(iWon, sameScore));
        }

        private IEnumerator PlayResultAudioRoutine(bool localWon, bool sameScore)
        {
            EnsureAudioSources();
            PlayResultMusic(localWon, sameScore);

            var sfx = resultSfxSource;
            if (sfx != null)
            {
                if (sfxLocalPlayerResult != null)
                    sfx.PlayOneShot(sfxLocalPlayerResult, sfxVolume);
                if (sfxOpponentPlayerResult != null)
                {
                    if (opponentSfxDelaySec > 0f)
                        yield return new WaitForSeconds(opponentSfxDelaySec);
                    sfx.PlayOneShot(sfxOpponentPlayerResult, sfxVolume);
                }

                if (extraResultSfx is { Length: > 0 })
                {
                    if (extraSfxIntervalSec > 0f)
                        yield return new WaitForSeconds(extraSfxIntervalSec);
                    foreach (var clip in extraResultSfx)
                    {
                        if (clip != null)
                            sfx.PlayOneShot(clip, sfxVolume);
                        if (extraSfxIntervalSec > 0f)
                            yield return new WaitForSeconds(extraSfxIntervalSec);
                    }
                }
            }
        }

        private void EnsureAudioSources()
        {
            if (resultMusicSource == null)
            {
                resultMusicSource = GetComponent<AudioSource>();
                if (resultMusicSource == null)
                    resultMusicSource = gameObject.AddComponent<AudioSource>();
                resultMusicSource.playOnAwake = false;
                resultMusicSource.loop = true;
            }

            if (resultSfxSource == null)
            {
                var sources = GetComponents<AudioSource>();
                if (sources.Length >= 2)
                    resultSfxSource = sources[1];
                else
                {
                    resultSfxSource = gameObject.AddComponent<AudioSource>();
                    resultSfxSource.playOnAwake = false;
                    resultSfxSource.loop = false;
                }
            }
        }

        private void PlayResultMusic(bool localWon, bool sameScore)
        {
            var src = resultMusicSource;
            if (src == null)
                return;

            AudioClip? clip = null;
            if (sameScore && musicIfDrawScore != null)
                clip = musicIfDrawScore;
            else
                clip = localWon ? musicIfLocalWon : musicIfLocalLost;

            if (clip == null)
                return;

            src.Stop();
            src.clip = clip;
            src.volume = musicVolume;
            src.loop = true;
            src.Play();
        }

        private void StopResultMusic()
        {
            if (resultMusicSource != null)
            {
                resultMusicSource.Stop();
                resultMusicSource.clip = null;
            }
        }

        private void SetStatus(string msg)
        {
            if (statusText != null)
                statusText.text = msg;
            if (!string.IsNullOrEmpty(msg))
                Debug.Log("[Result] " + msg);
        }

        private void GoLobby()
        {
            StopResultMusic();
            if (!string.IsNullOrEmpty(lobbySceneName))
                SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
        }
    }
}
