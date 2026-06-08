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
    /// Сцена Result: отправка <c>submit-my-stats</c> (или использование SignalR-результата),
    /// ожидание соперника при необходимости, вывод итогов.
    /// Кнопки: «Найти игру» → Lobby, «В меню» → Lobby без автопоиска.
    /// </summary>
    public sealed class ResultScreenController : MonoBehaviour
    {
        [SerializeField] private BackendConfig backendConfig = null!;
        [SerializeField] private string authSceneName  = "Auth";
        [SerializeField] private string lobbySceneName = "Lobby";
        [SerializeField] private Text? statusText;
        [SerializeField] private Text? headlineText;
        [SerializeField] private Text? detailsText;

        [Header("Кнопки после матча")]
        [Tooltip("«Найти новую игру» — переход в лобби с автоматическим запуском поиска.")]
        [SerializeField] private Button? findNewGameButton;
        [Tooltip("«В меню» — переход в лобби без поиска.")]
        [SerializeField] private Button? backToMenuButton;
        [Tooltip("Устаревшая кнопка «Назад в лобби» (если используется). Работает как «В меню».")]
        [SerializeField] private Button? backToLobbyButton;

        [Header("Звук результата")]
        [SerializeField] private AudioSource? resultMusicSource;
        [SerializeField] private AudioSource? resultSfxSource;
        [SerializeField] private AudioClip? musicIfLocalWon;
        [SerializeField] private AudioClip? musicIfLocalLost;
        [SerializeField] private AudioClip? musicIfDrawScore;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.75f;

        [SerializeField] private AudioClip? sfxLocalPlayerResult;
        [SerializeField] private AudioClip? sfxOpponentPlayerResult;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
        [SerializeField] private float opponentSfxDelaySec = 0.12f;
        [SerializeField] private AudioClip[] extraResultSfx = new AudioClip[0];
        [SerializeField] private float extraSfxIntervalSec = 0.08f;

        private void Awake()
        {
            if (findNewGameButton != null)
                findNewGameButton.onClick.AddListener(GoFindGame);
            if (backToMenuButton != null)
                backToMenuButton.onClick.AddListener(GoMenu);
            if (backToLobbyButton != null)
                backToLobbyButton.onClick.AddListener(GoMenu);

            SetButtonsVisible(false);
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
                // Быстрый путь: сервер уже прислал результат по SignalR во время пост-игровой фазы
                var pushed = PendingMatchFinalResult.TryConsume();
                if (pushed != null)
                {
                    SetStatus(string.Empty);
                    ShowResult(pushed, pending.MyPlayerId);
                    StartResultAudio(pushed, pending.MyPlayerId);
                    SetButtonsVisible(true);

                    // Отправляем статы в фоне для надёжности (Mongo-запись), не ждём ответа
                    _ = SubmitStatsInBackgroundAsync(pending);
                    return;
                }

                // Стандартный путь: авторизация + submit + poll
                var api  = new ApiClient(backendConfig);
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
                var body = BuildSubmitBody(pending);

                var sub = await api.MatchesSubmitMyStatsAsync(session.AccessToken, pending.MatchId, body, CancellationToken.None);
                if (!sub.Success || sub.Data == null)
                {
                    SetStatus($"Ошибка отправки: HTTP {sub.StatusCode} {sub.ErrorBody}");
                    SetButtonsVisible(true);
                    return;
                }

                if (sub.Data.Complete && sub.Data.Result != null)
                {
                    ShowResult(sub.Data.Result, pending.MyPlayerId);
                    StartResultAudio(sub.Data.Result, pending.MyPlayerId);
                    SetButtonsVisible(true);
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
                        SetButtonsVisible(true);
                        return;
                    }
                }

                SetStatus("Таймаут: соперник не отправил результат.");
                SetButtonsVisible(true);
            }
            catch (Exception ex)
            {
                SetStatus("Ошибка: " + ex.Message);
                Debug.LogException(ex);
                SetButtonsVisible(true);
            }
        }

        private async Task SubmitStatsInBackgroundAsync(PendingMatchResultPayload pending)
        {
            try
            {
                var api  = new ApiClient(backendConfig);
                var auth = new AuthManager(api);
                if (!await auth.TryRestoreSessionAsync(CancellationToken.None).ConfigureAwait(false))
                    return;
                var session = AuthContext.Current;
                if (session == null)
                    return;
                var body = BuildSubmitBody(pending);
                await api.MatchesSubmitMyStatsAsync(session.AccessToken, pending.MatchId, body, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Result] Фоновая отправка статов: " + ex.Message);
            }
        }

        private static SubmitMyMatchStatsBody BuildSubmitBody(PendingMatchResultPayload pending)
        {
            var body = new SubmitMyMatchStatsBody
            {
                Score  = pending.MyScore,
                Taps   = pending.MyTaps,
                Misses = pending.MyMisses,
            };
            if (pending.DebuffsApplied.Count > 0)
            {
                body.DebuffsApplied = new System.Collections.Generic.List<PlayerDebuffSegment>();
                foreach (var d in pending.DebuffsApplied)
                    body.DebuffsApplied.Add(d);
            }

            return body;
        }

        private void ShowResult(MatchResultResponseDto r, Guid myPlayerId)
        {
            SetStatus(string.Empty);

            var me  = r.Player1.PlayerId == myPlayerId ? r.Player1 : r.Player2;
            var opp = r.Player1.PlayerId == myPlayerId ? r.Player2 : r.Player1;
            var iWon      = r.WinnerPlayerId == myPlayerId;
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
                var myRanking = r.Player1.PlayerId == myPlayerId ? r.Player1Ranking : r.Player2Ranking;
                var rankingLine = FormatRankingLine(myRanking);

                detailsText.text =
                    $"Счёт: {me.Username} {me.Score}  —  {opp.Score} {opp.Username}\n" +
                    $"Всего очков (оба): {r.TotalScore}    Разница: {r.ScoreDifference}\n" +
                    $"Длительность: {r.DurationSec} с{winnerLine}" +
                    rankingLine +
                    $"── Ты ({me.Username}) ──\n" +
                    $"Тапы: {me.Taps}    Промахи: {me.Misses}\n" +
                    $"Точность: {me.AccuracyPercent:0.##}%    Тапов/сек: {me.TapsPerSecond:0.##}\n\n" +
                    $"── Соперник ({opp.Username}) ──\n" +
                    $"Тапы: {opp.Taps}    Промахи: {opp.Misses}\n" +
                    $"Точность: {opp.AccuracyPercent:0.##}%    Тапов/сек: {opp.TapsPerSecond:0.##}";
            }
        }

        private static string FormatRankingLine(MatchPlayerRankingDeltaDto? ranking)
        {
            if (ranking == null || ranking.Delta == 0 && ranking.NewRankPoints == 0)
                return string.Empty;

            var sign = ranking.Delta > 0 ? "+" : string.Empty;
            var streak = ranking.WinStreakBonus > 0
                ? $" (бонус серии +{ranking.WinStreakBonus})"
                : string.Empty;

            return $"\nРейтинг: {sign}{ranking.Delta} RP → {ranking.NewRankPoints} ({ranking.RankLabel}){streak}\n";
        }

        private void SetButtonsVisible(bool visible)
        {
            if (findNewGameButton != null) findNewGameButton.gameObject.SetActive(visible);
            if (backToMenuButton  != null) backToMenuButton.gameObject.SetActive(visible);
            if (backToLobbyButton != null) backToLobbyButton.gameObject.SetActive(visible);
        }

        private void StartResultAudio(MatchResultResponseDto r, Guid myPlayerId)
        {
            var iWon      = r.WinnerPlayerId == myPlayerId;
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

        /// <summary>Переход в Lobby с флагом автопоиска игры.</summary>
        private void GoFindGame()
        {
            StopResultMusic();
            LobbyAutoSearch.RequestAutoSearch = true;
            if (!string.IsNullOrEmpty(lobbySceneName))
                SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
        }

        /// <summary>Переход в Lobby без поиска.</summary>
        private void GoMenu()
        {
            StopResultMusic();
            LobbyAutoSearch.RequestAutoSearch = false;
            if (!string.IsNullOrEmpty(lobbySceneName))
                SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
        }
    }
}
