using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TapBrawl.Core.Skills;
using TapBrawl.Core.VFX;
using TapBrawl.Models;
using TapBrawl.Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TapBrawl.Core
{
    [DefaultExecutionOrder(-100)]
    public sealed class MatchController : MonoBehaviour
    {
        [SerializeField] private CircleSpawner spawner = null!;
        [SerializeField] private int durationSec = 75;
        [SerializeField] private uint trainingSeed = 42;
        [SerializeField] private Text scoreLabel = null!;
        [SerializeField] private Text timerLabel = null!;
        [SerializeField] private Text? opponentLabel;
        [SerializeField] private Text? modeLabel;
        [SerializeField] private string resultSceneName = "Result";

        [Header("Скилл (тест): крупные круги")]
        [SerializeField] private float giantCirclesSkillDurationSec = 5f;
        [SerializeField] private float giantCirclesSkillSizeMultiplier = 2.25f;
        [Header("Визуальные дебаффы от соперника (длительность на вашем клиенте)")]
        [SerializeField] private float opponentRedDeceptionDurationSec = 3f;
        [SerializeField] private float opponentSmokeVeilDurationSec = 5f;
        [Header("Каталог скиллов (иконки, VFX, loop-звуки)")]
        [SerializeField] private SkillCatalog? skillCatalog;
        [Header("Отладка (онлайн)")]
        [Tooltip(
            "В онлайне скиллы 2–3 по сети приходят событием от сервера: без второго клиента эффект у вас не появится. " +
            "Если включено — после успешной отправки эффект сразу применится локально (для проверки VFX). Выключите, если сервер дублирует событие и эффект срабатывает дважды.")]
        [SerializeField] private bool debugApplyOpponentVisualSkillsLocallyAfterOnlineSend;

        [Header("Звук")]
        [Tooltip("Обычно Assets/Audio Effects/pick.mp3 — перетащите сюда в инспекторе.")]
        [SerializeField] private AudioClip? bubbleHitSuccessClip;
        [Tooltip("Обычно Assets/Audio Effects/boom.mp3 — звук при тапе по бомбе.")]
        [SerializeField] private AudioClip? bubbleBombClip;
        [SerializeField] private AudioSource? bubbleHitAudioSource;
        [SerializeField] [Range(0f, 1f)] private float bubbleHitVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float bubbleBombVolume = 1f;
        [Tooltip("Удар цепной молнии по кругу (авто-тапы в разряде). Если пусто — при цепи играет звук успешного попадания.")]
        [SerializeField] private AudioClip? chainLightningHitClip;
        [SerializeField] [Range(0f, 1f)] private float chainLightningHitVolume = 1f;

        [Header("Фоновая музыка матча")]
        [Tooltip("Источник для музыки во время матча. Если пусто — будет создан на этом объекте.")]
        [SerializeField] private AudioSource? gameplayMusicSource;
        [Tooltip("Зацикленный трек, который играет только пока матч активен.")]
        [SerializeField] private AudioClip? gameplayBackgroundMusic;
        [SerializeField] [Range(0f, 1f)] private float gameplayMusicVolume = 0.45f;

        [Header("Всплывающие очки при тапе (+1, +3, −1 по бомбе)")]
        [SerializeField] private bool showScoreDeltaPopups = true;
        [Tooltip("Если задан — текст появляется в этой панели (якоря в её локальных координатах). Иначе — в области спавна кругов.")]
        [SerializeField] private RectTransform? scorePopupParentOverride;
        [SerializeField] private float scorePopupLifetimeSec = 0.85f;
        [SerializeField] private float scorePopupRisePixels = 76f;
        [SerializeField] private int scorePopupFontSize = 36;
        [SerializeField] private Color scorePopupPositiveColor = new(0.35f, 1f, 0.55f, 1f);
        [SerializeField] private Color scorePopupNegativeColor = new(1f, 0.38f, 0.4f, 1f);

        private readonly List<PooledCircle> _active = new();
        private float _endTime;
        private int _score;

        private bool _onlineMode;
        private bool _trainingArenaLoadoutOverridden;
        private OnlineMatchParams _online;
        private int _tapCount;
        private int _missCount;
        private bool _resultSceneLoadAttempted;
        private float _giantCirclesSkillEndUnscaled;
        private float _overheatSkillEndUnscaled;
        private float _opponentRedDeceptionEndUnscaled;
        private float _opponentSmokeVeilEndUnscaled;
        private GameObject? _smokeVeilGo;
        private bool _hubMatchEventsSubscribed;
        private AudioSource? _bubbleHitAudioResolved;
        private float _skillEnergyPercent;
        private int _successfulTapStreak;
        private float _streakBestAwardPercent;
        private bool _opponentVisualSkillSendInFlight;

        // Очередь скиллов, пришедших с SignalR (фоновый поток) — обрабатывается в Update (главный поток).
        private readonly ConcurrentQueue<(int SkillType, int CasterSkillLevel)> _pendingOpponentSkills = new();

        /// <summary>Эффективные длительности/множитель с учётом прокачки (выставляет <see cref="MatchSkillRuntime"/>).</summary>
        private float _runtimeGiantCirclesDurationSec;
        private float _runtimeGiantCirclesSizeMultiplier;
        private float _runtimeRedDeceptionDurationSec;
        private float _runtimeSmokeVeilDurationSec;
        private float _runtimeOverheatDurationSec;
        private float _runtimeOverheatCadenceMultiplier;
        private float _runtimeChainDischargeArmedDurationSec;
        private int _runtimeChainDischargeAdditionalTaps;
        private float _chainDischargeArmedUntilUnscaled;
        private bool _chainDischargeChainInProgress;
        private Coroutine? _chainDischargeRoutine;
        private readonly ConcurrentQueue<int> _pendingIncomingPings = new();
        private readonly ConcurrentQueue<int> _pendingLocalPingsSent = new();
        private float _pingSendReadyUnscaled;
        private const float PingCooldownSec = 3f;
        private readonly Dictionary<int, AudioSource> _skillLoopAudioSources = new();

        /// <summary>Локальные координаты <see cref="scorePopupParentOverride"/> или play area — последний удачный тап (для всплывающих +/−).</summary>
        private bool _hasLastSuccessfulTapPopupAnchor;

        private Vector2 _lastSuccessfulTapPopupAnchored;

        public bool IsRunning { get; private set; }

        /// <summary>Множитель визуального размера кругов (якоря в play area). Используется спавнером и <see cref="PooledCircle"/>.</summary>
        public float ActiveCircleVisualSizeMultiplier =>
            !IsRunning ? 1f : (Time.unscaledTime < _giantCirclesSkillEndUnscaled ? _runtimeGiantCirclesSizeMultiplier : 1f);

        public bool IsGiantCirclesSkillActive => IsRunning && Time.unscaledTime < _giantCirclesSkillEndUnscaled;

        public float GiantCirclesSkillRemainingSeconds =>
            Mathf.Max(0f, _giantCirclesSkillEndUnscaled - Time.unscaledTime);

        /// <summary>Длительность баффа «крупные круги» (сек) — для UI кулдауна.</summary>
        public float GiantCirclesBuffDurationSeconds => Mathf.Max(0.01f, _runtimeGiantCirclesDurationSec);

        public bool IsOverheatSkillActive => IsRunning && Time.unscaledTime < _overheatSkillEndUnscaled;

        public float OverheatSkillRemainingSeconds =>
            Mathf.Max(0f, _overheatSkillEndUnscaled - Time.unscaledTime);

        /// <summary>Множитель паузы между спавнами и интервала мигания фантомов (&lt;1 во время перегрева).</summary>
        public float SpawnCadenceGapMultiplier =>
            !IsRunning || !IsOverheatSkillActive ? 1f : Mathf.Clamp(_runtimeOverheatCadenceMultiplier, 0.05f, 1f);

        /// <summary>Длительность дебаффа «красная пелена» на клиенте (сек) — для UI кулдауна кнопки отправки.</summary>
        public float OpponentRedDeceptionDurationSeconds => Mathf.Max(0.01f, _runtimeRedDeceptionDurationSec);

        /// <summary>Длительность «дымовой завесы» на клиенте (сек) — для UI кулдауна кнопки отправки.</summary>
        public float OpponentSmokeVeilDurationSeconds => Mathf.Max(0.01f, _runtimeSmokeVeilDurationSec);

        public bool IsOpponentRedDeceptionVisualActive =>
            IsRunning && Time.unscaledTime < _opponentRedDeceptionEndUnscaled;

        public bool IsOpponentSmokeVeilActive =>
            IsRunning && Time.unscaledTime < _opponentSmokeVeilEndUnscaled;

        public float OpponentRedDeceptionRemainingSeconds =>
            Mathf.Max(0f, _opponentRedDeceptionEndUnscaled - Time.unscaledTime);

        public float OpponentSmokeVeilRemainingSeconds =>
            Mathf.Max(0f, _opponentSmokeVeilEndUnscaled - Time.unscaledTime);
        public float SkillEnergyPercent => _skillEnergyPercent;
        public float SkillEnergyMaxPercent => Mathf.Max(1f, SkillBalance.SkillEnergyMaxPercentDefault);
        public float SkillEnergyNormalized => Mathf.Clamp01(_skillEnergyPercent / SkillEnergyMaxPercent);
        public float GiantCirclesSkillCostPercent =>
            Mathf.Clamp(SkillBalance.GiantCirclesSkillEnergyCostPercent, 0f, SkillEnergyMaxPercent);
        public float OpponentRedDeceptionCostPercent =>
            Mathf.Clamp(SkillBalance.RedDeceptionSkillEnergyCostPercent, 0f, SkillEnergyMaxPercent);
        public float OpponentSmokeVeilCostPercent =>
            Mathf.Clamp(SkillBalance.SmokeVeilSkillEnergyCostPercent, 0f, SkillEnergyMaxPercent);
        public float OverheatSkillCostPercent =>
            Mathf.Clamp(SkillBalance.OverheatSkillEnergyCostPercent, 0f, SkillEnergyMaxPercent);
        public float ChainDischargeSkillCostPercent =>
            Mathf.Clamp(SkillBalance.ChainDischargeSkillEnergyCostPercent, 0f, SkillEnergyMaxPercent);
        public bool IsChainDischargeArmed => IsRunning && Time.unscaledTime < _chainDischargeArmedUntilUnscaled;
        public float ChainDischargeArmedRemainingSeconds =>
            Mathf.Max(0f, _chainDischargeArmedUntilUnscaled - Time.unscaledTime);
        public float ChainDischargeArmedDurationSeconds => Mathf.Max(0.01f, _runtimeChainDischargeArmedDurationSec);

        /// <summary>Только входящее с сервера (у соперника нажали 2/3); не вызывается при локальной тренировке и не при отправке с вашей стороны.</summary>
        public event Action<int>? OpponentVisualSkillReceivedFromNetwork;

        /// <summary>Входящий пинг от соперника (<see cref="MatchPingIds"/>).</summary>
        public event Action<int>? PlayerPingReceivedFromNetwork;

        /// <summary>Ваш отправленный пинг (тип <see cref="MatchPingIds"/>); вызывается на главном потоке после успешной отправки.</summary>
        public event Action<int>? LocalPlayerPingSent;

        /// <summary>Зарегистрирован тап по игровому кружку — UI может закрыть выбор пинга без отправки.</summary>
        public event Action? GameplayCircleTapped;

        /// <summary>Оставшееся время до следующей отправки пинга (сек, unscaled).</summary>
        public float PingSendCooldownRemainingSeconds =>
            IsRunning ? Mathf.Max(0f, _pingSendReadyUnscaled - Time.unscaledTime) : 0f;

        /// <summary>Продлевает таймер скилла на полную длительность от текущего момента.</summary>
        public void TryActivateGiantCirclesSkill()
        {
            if (!TryConsumeSkillEnergy(GiantCirclesSkillCostPercent))
                return;
            _giantCirclesSkillEndUnscaled = Time.unscaledTime + Mathf.Max(0.1f, _runtimeGiantCirclesDurationSec);
            SyncActiveEffectSounds();
        }

        public void TryActivateOverheatSkill()
        {
            if (!TryConsumeSkillEnergy(OverheatSkillCostPercent))
                return;
            _overheatSkillEndUnscaled = Time.unscaledTime + Mathf.Max(0.1f, _runtimeOverheatDurationSec);
            SyncActiveEffectSounds();
        }

        public void TryActivateChainDischargeSkill()
        {
            if (!TryConsumeSkillEnergy(ChainDischargeSkillCostPercent))
                return;
            _chainDischargeArmedUntilUnscaled = Time.unscaledTime + Mathf.Max(0.1f, _runtimeChainDischargeArmedDurationSec);
        }

        /// <summary>
        /// Скиллы 2–3: в онлайне уходит сопернику через SignalR; в тренировке применяется к вам локально (проверка эффекта).
        /// </summary>
        public void RequestSendOpponentVisualSkill(int skillType)
        {
            if (!IsRunning)
                return;
            if (skillType != MatchSkillIds.OpponentRedDeceptionVisual && skillType != MatchSkillIds.OpponentSmokeVeil)
                return;
            var cost = skillType == MatchSkillIds.OpponentRedDeceptionVisual
                ? OpponentRedDeceptionCostPercent
                : OpponentSmokeVeilCostPercent;
            if (!HasEnoughSkillEnergy(cost))
                return;

            if (!_onlineMode)
            {
                if (!TryConsumeSkillEnergy(cost))
                    return;
                var localLvl = PlayerSkillsRuntimeState.GetLevel(skillType);
                ApplyIncomingOpponentVisualSkill(skillType, localLvl);
                return;
            }

            if (_opponentVisualSkillSendInFlight)
                return;
            _ = SendOpponentVisualSkillOnlineAsync(skillType, cost);
        }

        private async Task SendOpponentVisualSkillOnlineAsync(int skillType, float costPercent)
        {
            _opponentVisualSkillSendInFlight = true;
            try
            {
                var holder = MatchConnectionHolder.Instance;
                var hub = holder?.Hub;
                if (hub == null)
                {
                    Debug.LogWarning(
                        "[Match] SendOpponentVisualSkill: хаб недоступен (MatchConnectionHolder или Hub == null). " +
                        "Обычно лобби не передало соединение в MatchConnectionHolder — перезайдите в поиск 1v1.");
                    SubscribeMatchHubEventsIfNeeded();
                    return;
                }

                Debug.Log($"[Match] Отправляем скилл {skillType} для матча {_online.MatchId}…");
                await hub.SendOpponentVisualSkillAsync(_online.MatchId, skillType, CancellationToken.None);
                Debug.Log($"[Match] Скилл {skillType} отправлен.");
                TryConsumeSkillEnergy(costPercent);
                if (debugApplyOpponentVisualSkillsLocallyAfterOnlineSend)
                    ApplyIncomingOpponentVisualSkill(skillType, PlayerSkillsRuntimeState.GetLevel(skillType));
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[Match] SendOpponentVisualSkill: " + ex.Message);
            }
            finally
            {
                _opponentVisualSkillSendInFlight = false;
            }
        }

        private void ApplyIncomingOpponentVisualSkill(int skillType, int casterSkillLevel)
        {
            if (!IsRunning)
                return;
            var lvl = Mathf.Clamp(casterSkillLevel, SkillBalance.MinLevel, SkillBalance.MaxLevel);
            var dur = Mathf.Max(0.1f, SkillBalance.EffectDurationForSkill(skillType, lvl));
            if (skillType == MatchSkillIds.OpponentRedDeceptionVisual)
            {
                _opponentRedDeceptionEndUnscaled = Time.unscaledTime + dur;
                SyncActiveEffectSounds();
            }
            else if (skillType == MatchSkillIds.OpponentSmokeVeil)
            {
                _opponentSmokeVeilEndUnscaled = Time.unscaledTime + dur;
                EnsureSmokeVeilOverlay();
                SyncActiveEffectSounds();
            }
        }

        /// <summary>Выставляет параметры скиллов из прокачки (см. <see cref="MatchSkillRuntime"/>).</summary>
        public void ApplyRuntimeSkillScaling(
            float giantCirclesDurationSec,
            float giantCirclesSizeMultiplier,
            float redDeceptionDurationSec,
            float smokeVeilDurationSec,
            float overheatDurationSec,
            float overheatCadenceMultiplier,
            float chainDischargeArmedDurationSec,
            int chainDischargeAdditionalTaps)
        {
            _runtimeGiantCirclesDurationSec = Mathf.Max(0.1f, giantCirclesDurationSec);
            _runtimeGiantCirclesSizeMultiplier = Mathf.Max(1f, giantCirclesSizeMultiplier);
            _runtimeRedDeceptionDurationSec = Mathf.Max(0.1f, redDeceptionDurationSec);
            _runtimeSmokeVeilDurationSec = Mathf.Max(0.1f, smokeVeilDurationSec);
            _runtimeOverheatDurationSec = Mathf.Max(0.1f, overheatDurationSec);
            _runtimeOverheatCadenceMultiplier = Mathf.Clamp(overheatCadenceMultiplier, 0.05f, 1f);
            _runtimeChainDischargeArmedDurationSec = Mathf.Max(0.1f, chainDischargeArmedDurationSec);
            _runtimeChainDischargeAdditionalTaps = Mathf.Max(1, chainDischargeAdditionalTaps);
        }

        /// <summary>Пинг сопернику (онлайн — через хаб; тренировка — только локальный UI «отправлено»).</summary>
        public void RequestSendPing(int pingType)
        {
            if (!IsRunning || !MatchPingIds.IsValid(pingType))
                return;
            if (Time.unscaledTime < _pingSendReadyUnscaled)
                return;

            if (!_onlineMode)
            {
                _pendingLocalPingsSent.Enqueue(pingType);
                _pingSendReadyUnscaled = Time.unscaledTime + PingCooldownSec;
                return;
            }

            _ = SendPingOnlineAsync(pingType);
        }

        private async Task SendPingOnlineAsync(int pingType)
        {
            try
            {
                var holder = MatchConnectionHolder.Instance;
                var hub = holder?.Hub;
                if (hub == null)
                {
                    Debug.LogWarning("[Match] SendPing: хаб недоступен.");
                    SubscribeMatchHubEventsIfNeeded();
                    return;
                }

                await hub.SendPingAsync(_online.MatchId, pingType, CancellationToken.None);
                _pingSendReadyUnscaled = Time.unscaledTime + PingCooldownSec;
                _pendingLocalPingsSent.Enqueue(pingType);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[Match] SendPing: " + ex.Message);
            }
        }

        private void SubscribeMatchHubEventsIfNeeded()
        {
            if (_hubMatchEventsSubscribed)
                return;
            var hub = MatchConnectionHolder.Instance?.Hub;
            if (hub == null)
                return;
            hub.OpponentVisualSkill += OnOpponentVisualSkillReceived;
            hub.PlayerPing += OnPlayerPingReceived;
            _hubMatchEventsSubscribed = true;
            Debug.Log("[Match] Подписка на события матча (скиллы соперника + пинги).");
        }

        private void UnsubscribeMatchHubEvents()
        {
            if (!_hubMatchEventsSubscribed)
                return;
            var hub = MatchConnectionHolder.Instance?.Hub;
            if (hub != null)
            {
                hub.OpponentVisualSkill -= OnOpponentVisualSkillReceived;
                hub.PlayerPing -= OnPlayerPingReceived;
            }

            _hubMatchEventsSubscribed = false;
        }

        // Вызывается с фонового потока SignalR — только потокобезопасные операции (ConcurrentQueue.Enqueue).
        private void OnOpponentVisualSkillReceived(OpponentVisualSkillDto dto)
        {
            if (dto == null)
                return;
            if (dto.SkillType != MatchSkillIds.OpponentRedDeceptionVisual &&
                dto.SkillType != MatchSkillIds.OpponentSmokeVeil)
            {
                Debug.LogWarning(
                    $"[Match] OpponentVisualSkill: неизвестный тип {dto.SkillType} (ожидались 2 или 3). Проверьте JSON с сервера.");
                return;
            }

            var lvl = dto.CasterSkillLevel <= 0 ? SkillBalance.MaxLevel : dto.CasterSkillLevel;
            _pendingOpponentSkills.Enqueue((dto.SkillType, lvl));
        }

        private void OnPlayerPingReceived(PlayerPingDto dto)
        {
            if (dto == null)
                return;
            if (!MatchPingIds.IsValid(dto.PingType))
            {
                Debug.LogWarning($"[Match] PlayerPing: неизвестный тип {dto.PingType}.");
                return;
            }

            _pendingIncomingPings.Enqueue(dto.PingType);
        }

        private void EnsureSmokeVeilOverlay()
        {
            if (spawner == null)
                return;
            var play = spawner.PlayAreaRect;
            if (play == null)
                return;

            DestroySmokeVeil();

            var smokeVeilEffectPrefab = skillCatalog != null ? skillCatalog.GetSmokeVeilEffectPrefab() : null;
            if (smokeVeilEffectPrefab != null)
            {
                var inst = Instantiate(smokeVeilEffectPrefab, play, false);
                inst.name = "SmokeVeil";
                SmokeVeilOverlayRuntime.FlattenNestedSmokeVeilRootIfPresent(inst);
                var rt = inst.GetComponent<RectTransform>();
                if (rt == null)
                {
                    Debug.LogError("[Match] smokeVeilEffectPrefab: на корне нужен RectTransform.");
                    Destroy(inst);
                    return;
                }

                ApplyRandomSmokeVeilAnchors(rt);
                SmokeVeilOverlayRuntime.EnsureSmokeVeilBackdrop(inst);
                SmokeVeilOverlayRuntime.CompensateSmokeVeilScaleIfCanvasMakesParticlesTiny(rt);
                SmokeVeilOverlayRuntime.TryConfigureParticleCanvasForOverlay(inst);
                SmokeVeilOverlayRuntime.EnsureSmokeVeilBackdrop(inst);
                SmokeVeilOverlayRuntime.ConfigureSmokeVeilFogPresentation(inst);
                SmokeVeilOverlayRuntime.BoostSmokeParticleStartSizeForUi(inst);
                foreach (var ps in inst.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = ps.main;
                    main.useUnscaledTime = true;
                    ps.Clear(true);
                    ps.Play(true);
                }

                SmokeVeilOverlayRuntime.NormalizeParticleRenderersForUi(inst);

                _smokeVeilGo = inst;
                _smokeVeilGo.transform.SetAsLastSibling();
                return;
            }

            var go = new GameObject("SmokeVeil");
            go.transform.SetParent(play, false);
            var rtFallback = go.AddComponent<RectTransform>();
            ApplyRandomSmokeVeilAnchors(rtFallback);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.12f, 0.16f, 0.62f);
            img.raycastTarget = false;
            _smokeVeilGo = go;
            _smokeVeilGo.transform.SetAsLastSibling();
        }

        private static void ApplyRandomSmokeVeilAnchors(RectTransform rt)
        {
            var w = UnityEngine.Random.Range(0.62f, 0.98f);
            var h = UnityEngine.Random.Range(0.66f, 0.92f);
            var axMin = UnityEngine.Random.Range(0f, Mathf.Max(0.01f, 1f - w));
            var ayMin = UnityEngine.Random.Range(0f, Mathf.Max(0.01f, 1f - h));
            rt.anchorMin = new Vector2(axMin, ayMin);
            rt.anchorMax = new Vector2(axMin + w, ayMin + h);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition3D = Vector3.zero;
            rt.localScale = Vector3.one;
        }

        private void DestroySmokeVeil()
        {
            if (_smokeVeilGo == null)
                return;
            foreach (var ps in _smokeVeilGo.GetComponentsInChildren<ParticleSystem>(true))
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Destroy(_smokeVeilGo);
            _smokeVeilGo = null;
        }

        private void OnDestroy()
        {
            if (_trainingArenaLoadoutOverridden)
            {
                PlayerSkillsRuntimeState.PopTrainingArenaLoadoutOverride();
                _trainingArenaLoadoutOverridden = false;
            }

            UnsubscribeMatchHubEvents();
            DestroySmokeVeil();
            StopGameplayMusic();
            StopActiveEffectSounds();
            StopChainDischargeRoutineIfRunning();
        }

        private void Awake()
        {
            _runtimeGiantCirclesDurationSec = giantCirclesSkillDurationSec;
            _runtimeGiantCirclesSizeMultiplier = giantCirclesSkillSizeMultiplier;
            _runtimeRedDeceptionDurationSec = opponentRedDeceptionDurationSec;
            _runtimeSmokeVeilDurationSec = opponentSmokeVeilDurationSec;
            _runtimeOverheatDurationSec = SkillBalance.OverheatDurationSec(SkillBalance.MaxLevel);
            _runtimeOverheatCadenceMultiplier = SkillBalance.OverheatSpawnCadenceMultiplier(SkillBalance.MaxLevel);
            _runtimeChainDischargeArmedDurationSec = SkillBalance.ChainDischargeArmedDurationSec(SkillBalance.MaxLevel);
            _runtimeChainDischargeAdditionalTaps = SkillBalance.ChainDischargeAdditionalTaps(SkillBalance.MaxLevel);

            _bubbleHitAudioResolved = bubbleHitAudioSource != null
                ? bubbleHitAudioSource
                : GetComponent<AudioSource>();
            if (_bubbleHitAudioResolved == null &&
                (bubbleHitSuccessClip != null || bubbleBombClip != null))
            {
                _bubbleHitAudioResolved = gameObject.AddComponent<AudioSource>();
                _bubbleHitAudioResolved.playOnAwake = false;
            }
        }

        private void Start()
        {
            ApplySkillScalingFromRuntimeState();
            if (PendingOnlineMatch.TryConsume(out var online))
                StartOnline(online);
            else
            {
                TryApplyTrainingArenaLoadoutOverride();
                StartTraining();
            }
        }

        private void TryApplyTrainingArenaLoadoutOverride()
        {
            if (SceneManager.GetActiveScene().name != "TrainingScene")
                return;
            PlayerSkillsRuntimeState.PushTrainingArenaLoadoutOverride();
            _trainingArenaLoadoutOverridden = true;
        }

        private void ApplySkillScalingFromRuntimeState()
        {
            if (!PlayerSkillsRuntimeState.HasAnyData)
                PlayerSkillsRuntimeState.ApplyOfflineMaxDefaults();

            var g = PlayerSkillsRuntimeState.GetLevel(MatchSkillIds.GiantCirclesSelfBuff);
            var r = PlayerSkillsRuntimeState.GetLevel(MatchSkillIds.OpponentRedDeceptionVisual);
            var s = PlayerSkillsRuntimeState.GetLevel(MatchSkillIds.OpponentSmokeVeil);
            var o = PlayerSkillsRuntimeState.GetLevel(MatchSkillIds.OverheatSelfBuff);
            var c = PlayerSkillsRuntimeState.GetLevel(MatchSkillIds.ChainDischargeSelfBuff);
            ApplyRuntimeSkillScaling(
                SkillBalance.GiantCirclesDurationSec(g),
                SkillBalance.GiantCirclesSizeMultiplier(g),
                SkillBalance.RedDeceptionDurationSec(r),
                SkillBalance.SmokeVeilDurationSec(s),
                SkillBalance.OverheatDurationSec(o),
                SkillBalance.OverheatSpawnCadenceMultiplier(o),
                SkillBalance.ChainDischargeArmedDurationSec(c),
                SkillBalance.ChainDischargeAdditionalTaps(c));
        }

        [ContextMenu("Training / Restart")]
        public void StartTraining()
        {
            StopMatch();
            IsRunning = true;
            _score = 0;
            _hasLastSuccessfulTapPopupAnchor = false;
            _endTime = Time.unscaledTime + durationSec;
            spawner.Begin(this, trainingSeed);
            ApplyModeUi("Тренировка", null);
            RefreshUi();
            PlayGameplayMusicIfConfigured();
        }

        /// <summary>Старт по данным с сервера (после MatchFound).</summary>
        public void StartOnline(OnlineMatchParams p)
        {
            StopMatch();
            _onlineMode = true;
            _online = p;
            _tapCount = 0;
            _missCount = 0;
            _resultSceneLoadAttempted = false;
            IsRunning = true;
            _score = 0;
            _hasLastSuccessfulTapPopupAnchor = false;
            _endTime = Time.unscaledTime + p.DurationSec;
            spawner.Begin(this, p.Seed);
            ApplyModeUi("Онлайн 1v1", p.OpponentUsername);
            RefreshUi();
            PlayGameplayMusicIfConfigured();
            _pingSendReadyUnscaled = 0f;
            SubscribeMatchHubEventsIfNeeded();
        }

        public void StopMatch()
        {
            StopChainDischargeRoutineIfRunning();
            IsRunning = false;
            _giantCirclesSkillEndUnscaled = 0f;
            _overheatSkillEndUnscaled = 0f;
            _chainDischargeArmedUntilUnscaled = 0f;
            _opponentRedDeceptionEndUnscaled = 0f;
            _opponentSmokeVeilEndUnscaled = 0f;
            DestroySmokeVeil();
            StopGameplayMusic();
            StopActiveEffectSounds();
            UnsubscribeMatchHubEvents();
            _onlineMode = false;
            _pingSendReadyUnscaled = 0f;
            _skillEnergyPercent = 0f;
            ResetSuccessfulTapStreak();
            _hasLastSuccessfulTapPopupAnchor = false;
            _opponentVisualSkillSendInFlight = false;
            _chainDischargeChainInProgress = false;
            while (_pendingOpponentSkills.TryDequeue(out _)) { }
            while (_pendingIncomingPings.TryDequeue(out _)) { }
            while (_pendingLocalPingsSent.TryDequeue(out _)) { }
            spawner.Stop();
            for (var i = _active.Count - 1; i >= 0; i--)
                spawner.Despawn(_active[i]);
            _active.Clear();
        }

        public void TrackActiveCircle(PooledCircle circle) => _active.Add(circle);

        public void RegisterTap(PooledCircle circle)
        {
            ResolveTap(circle, true);
        }

        private void ResolveTap(PooledCircle circle, bool allowChainDischarge, bool chainLightningStrike = false)
        {
            if (!IsRunning)
                return;

            GameplayCircleTapped?.Invoke();

            if (_onlineMode)
                _tapCount++;

            var delta = circle.Kind switch
            {
                CircleKind.Normal => 1,
                CircleKind.Gold => 3,
                CircleKind.Bomb => -1,
                CircleKind.Phantom => circle.PhantomVisiblePhase ? 1 : 0,
                _ => 0,
            };

            if (delta != 0)
                _score = Mathf.Max(0, _score + delta);
            var isPerfectTap = false;
            if (delta > 0)
            {
                var tappedAt = Time.unscaledTime;
                isPerfectTap = circle.IsPerfectTimingPositiveTap(tappedAt);
                var streakSteps = 1;
                if (IsOverheatSkillActive && isPerfectTap)
                    streakSteps++;
                RegisterSuccessfulTapForEnergy(streakSteps);

                if (allowChainDischarge &&
                    !_chainDischargeChainInProgress &&
                    IsChainDischargeArmed &&
                    isPerfectTap)
                    TryTriggerChainDischarge(circle);

                if (chainLightningStrike)
                    PlayChainLightningHitSound();
                else
                    PlayBubbleHitSuccessSound();
            }
            else
            {
                ResetSuccessfulTapStreak();
                if (circle.Kind == CircleKind.Bomb)
                    PlayBubbleBombSound();
            }

            if (showScoreDeltaPopups && delta != 0)
            {
                var popupParent = scorePopupParentOverride != null
                    ? scorePopupParentOverride
                    : spawner != null
                        ? spawner.PlayAreaRect
                        : null;
                if (popupParent != null &&
                    TryResolveScorePopupAnchoredForTap(popupParent, circle, delta, out var anchored))
                    StartCoroutine(ScoreDeltaPopupRoutine(popupParent, anchored, delta));
            }

            if (!TrySpawnTapSpriteSheetFx(circle, delta, isPerfectTap, chainLightningStrike))
                SpawnCircleTapJuiceFx(circle, chainLightningStrike);
            _active.Remove(circle);
            spawner.Despawn(circle);
            RefreshUi();
        }

        private bool TrySpawnTapSpriteSheetFx(
            PooledCircle circle,
            int scoreDelta,
            bool isPerfectTap,
            bool chainLightningStrike)
        {
            if (spawner == null || spawner.Config == null || spawner.PlayAreaRect == null)
                return false;
            if (circle.transform is not RectTransform source)
                return false;

            var anim = spawner.Config.ResolveTapAnim(circle.Kind, scoreDelta, isPerfectTap, chainLightningStrike);
            if (!anim.HasPlayableTapFx)
                return false;

            return SpriteSheetTapFx.TrySpawn(
                spawner.PlayAreaRect,
                source,
                anim,
                TapFxTintFor(circle.Kind, scoreDelta, chainLightningStrike));
        }

        private static Color TapFxTintFor(CircleKind kind, int scoreDelta, bool chainLightningStrike)
        {
            if (chainLightningStrike)
                return new Color(0.55f, 0.92f, 1f, 1f);
            if (scoreDelta < 0)
                return new Color(1f, 0.35f, 0.2f, 1f);
            if (scoreDelta == 0)
                return new Color(0.65f, 0.5f, 0.95f, 0.85f);
            return kind switch
            {
                CircleKind.Gold => new Color(1f, 0.9f, 0.35f, 1f),
                CircleKind.Phantom => new Color(0.78f, 0.55f, 1f, 1f),
                _ => Color.white,
            };
        }

        private void SpawnCircleTapJuiceFx(PooledCircle circle, bool chainLightningStrike)
        {
            if (spawner == null || spawner.PlayAreaRect == null)
                return;
            if (circle.transform is not RectTransform source)
                return;

            CircleTapJuiceFx.Spawn(
                spawner.PlayAreaRect,
                source,
                circle.CurrentVisualSprite,
                circle.Kind,
                circle.TapPopLifetimeSec,
                circle.TapPopScale,
                circle.BombExplosionScale,
                chainLightningStrike);
        }

        private void TryTriggerChainDischarge(PooledCircle sourceCircle)
        {
            _chainDischargeArmedUntilUnscaled = 0f;
            var maxTargets = Mathf.Max(0, _runtimeChainDischargeAdditionalTaps);
            if (maxTargets <= 0)
                return;

            StopChainDischargeRoutineIfRunning();
            _chainDischargeChainInProgress = true;
            var chainTip = sourceCircle.transform.position;
            _chainDischargeRoutine = StartCoroutine(ChainDischargeRoutine(chainTip, sourceCircle, maxTargets));
        }

        private void StopChainDischargeRoutineIfRunning()
        {
            if (_chainDischargeRoutine == null)
                return;
            StopCoroutine(_chainDischargeRoutine);
            _chainDischargeRoutine = null;
            _chainDischargeChainInProgress = false;
        }

        private IEnumerator ChainDischargeRoutine(Vector3 chainTipStart, PooledCircle sourceCircle, int maxTargets)
        {
            try
            {
                var chainTip = chainTipStart;
                for (var step = 0; step < maxTargets; step++)
                {
                    if (!IsRunning)
                        break;

                    var delay = Mathf.Max(
                        0f,
                        skillCatalog != null ? skillCatalog.GetChainDischargeStepDelayUnscaledSec() : 0.12f);
                    if (delay > 0f)
                        yield return new WaitForSecondsRealtime(delay);

                    if (!IsRunning)
                        break;

                    PooledCircle? best = null;
                    var bestSqr = float.PositiveInfinity;
                    for (var i = 0; i < _active.Count; i++)
                    {
                        var c = _active[i];
                        if (c == null || c == sourceCircle || !IsChainDischargeCandidate(c))
                            continue;
                        var sqr = (c.transform.position - chainTip).sqrMagnitude;
                        if (sqr < bestSqr)
                        {
                            bestSqr = sqr;
                            best = c;
                        }
                    }

                    if (best == null)
                        break;

                    var nextPos = best.transform.position;
                    SpawnChainBoltVisual(chainTip, nextPos);
                    chainTip = nextPos;
                    ResolveTap(best, false, chainLightningStrike: true);
                }
            }
            finally
            {
                _chainDischargeChainInProgress = false;
                _chainDischargeRoutine = null;
            }
        }

        private static bool IsChainDischargeCandidate(PooledCircle circle) =>
            circle.Kind is CircleKind.Normal or CircleKind.Gold;

        private void SpawnChainBoltVisual(Vector3 worldFrom, Vector3 worldTo)
        {
            var chainBoltEffectPrefab = skillCatalog != null ? skillCatalog.GetChainBoltEffectPrefab() : null;
            if (chainBoltEffectPrefab == null || spawner == null)
                return;

            var parent = spawner.PlayAreaRect;
            if (parent == null)
                return;

            var inst = Instantiate(chainBoltEffectPrefab, parent, false);
            inst.transform.SetAsLastSibling();

            if (!inst.TryGetComponent<ChainBoltFx>(out var fx))
            {
                Debug.LogError("[Match] chainBoltEffectPrefab: на корне нужен компонент ChainBoltFx.");
                Destroy(inst);
                return;
            }

            fx.Play(worldFrom, worldTo);
        }

        public void RegisterMissedTap()
        {
            if (!IsRunning)
                return;
            ResetSuccessfulTapStreak();
        }

        private bool TryResolveScorePopupAnchoredForTap(
            RectTransform popupParent,
            PooledCircle circle,
            int delta,
            out Vector2 anchored)
        {
            if (delta > 0)
            {
                if (!TryGetPointerLocalInRectangle(popupParent, out anchored) &&
                    !TryResolveScorePopupAnchoredPosition(popupParent, circle, out anchored))
                    return false;
                _lastSuccessfulTapPopupAnchored = anchored;
                _hasLastSuccessfulTapPopupAnchor = true;
                return true;
            }

            if (_hasLastSuccessfulTapPopupAnchor)
            {
                anchored = _lastSuccessfulTapPopupAnchored;
                return true;
            }

            return TryResolveScorePopupAnchoredPosition(popupParent, circle, out anchored);
        }

        private static bool TryGetPointerLocalInRectangle(RectTransform parent, out Vector2 local)
        {
            var canvas = parent.GetComponentInParent<Canvas>();
            Camera? cam = null;
            if (canvas != null &&
                (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace))
                cam = canvas.worldCamera;

            if (!TryGetPointerScreenPoint(out var screen))
            {
                local = default;
                return false;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, cam, out local))
            {
                local = default;
                return false;
            }

            return true;
        }

        private static bool TryGetPointerScreenPoint(out Vector2 screen)
        {
            screen = default;

#if ENABLE_INPUT_SYSTEM
            if (TryReadPointerFromInputSystem(out screen))
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
            {
                screen = Input.GetTouch(0).position;
                return true;
            }

            screen = Input.mousePosition;
            return true;
#else
            return false;
#endif
        }

        /// <summary>Для UI (панель пингов и т.п.): текущая позиция указателя в экранных пикселях.</summary>
        public static bool TryGetCurrentPointerScreen(out Vector2 screen) => TryGetPointerScreenPoint(out screen);

        /// <summary>Кадр отпускания основной кнопки мыши / основного касания (закрытие оверлеев по «тапу»).</summary>
        public static bool WasPrimaryPointerReleasedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            if (m != null && m.leftButton.wasReleasedThisFrame)
                return true;
            var ts = Touchscreen.current;
            if (ts != null && ts.primaryTouch.press.wasReleasedThisFrame)
                return true;
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
                return Input.GetTouch(0).phase == TouchPhase.Ended;
            return Input.GetMouseButtonUp(0);
#else
            return false;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// Фазы, для которых ещё есть осмысленная позиция на экране. Для UI-кнопки кадр клика часто совпадает с
        /// <see cref="UnityEngine.InputSystem.TouchPhase.Ended"/>, когда <c>press.isPressed</c> уже false — иначе позицию не прочитать.
        /// </summary>
        private static bool TouchPhaseHasPosition(UnityEngine.InputSystem.TouchPhase phase) =>
            phase is UnityEngine.InputSystem.TouchPhase.Began or UnityEngine.InputSystem.TouchPhase.Moved
                or UnityEngine.InputSystem.TouchPhase.Stationary or UnityEngine.InputSystem.TouchPhase.Ended;

        private static bool TryReadPointerFromInputSystem(out Vector2 screen)
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var pt = touchscreen.primaryTouch;
                if (TouchPhaseHasPosition(pt.phase.ReadValue()))
                {
                    screen = pt.position.ReadValue();
                    return true;
                }

                for (var i = 0; i < touchscreen.touches.Count; i++)
                {
                    var t = touchscreen.touches[i];
                    if (!TouchPhaseHasPosition(t.phase.ReadValue()))
                        continue;
                    screen = t.position.ReadValue();
                    return true;
                }

                // На телефоне часто есть синтезированный Mouse с позицией (0,0) — не подставлять её вместо касания.
                screen = default;
                return false;
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                screen = mouse.position.ReadValue();
                return true;
            }

            screen = default;
            return false;
        }
#endif

        private static bool TryResolveScorePopupAnchoredPosition(
            RectTransform popupParent,
            PooledCircle circle,
            out Vector2 anchored)
        {
            var cr = (RectTransform)circle.transform;
            if (cr.parent == popupParent)
            {
                anchored = cr.anchoredPosition;
                return true;
            }

            var canvas = popupParent.GetComponentInParent<Canvas>() ?? cr.GetComponentInParent<Canvas>();
            Camera? cam = null;
            if (canvas != null)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)
                    cam = canvas.worldCamera;
            }

            var screen = RectTransformUtility.WorldToScreenPoint(cam, cr.position);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(popupParent, screen, cam, out anchored);
        }

        private IEnumerator ScoreDeltaPopupRoutine(RectTransform parent, Vector2 startAnchored, int delta)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject("ScoreDeltaPopup", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = startAnchored;
            rt.sizeDelta = new Vector2(200f, 80f);
            rt.SetAsLastSibling();

            var tx = go.GetComponent<Text>();
            tx.font = font;
            tx.fontSize = scorePopupFontSize;
            tx.fontStyle = FontStyle.Bold;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.text = delta > 0 ? $"+{delta}" : $"{delta}";
            tx.color = delta > 0 ? scorePopupPositiveColor : scorePopupNegativeColor;
            tx.raycastTarget = false;

            var rise = scorePopupRisePixels;
            var life = Mathf.Max(0.12f, scorePopupLifetimeSec);
            var elapsed = 0f;
            while (elapsed < life)
            {
                elapsed += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(elapsed / life);
                var ease = 1f - (1f - u) * (1f - u);
                rt.anchoredPosition = startAnchored + new Vector2(0f, rise * ease);
                var c = tx.color;
                c.a = (delta > 0 ? scorePopupPositiveColor : scorePopupNegativeColor).a * (1f - u);
                tx.color = c;
                yield return null;
            }

            Destroy(go);
        }

        private void Update()
        {
            if (_onlineMode && IsRunning && !_hubMatchEventsSubscribed)
                SubscribeMatchHubEventsIfNeeded();

            // Применяем скиллы, пришедшие с SignalR-потока, здесь — на главном потоке Unity.
            while (_pendingOpponentSkills.TryDequeue(out var pending))
            {
                if (IsRunning)
                {
                    OpponentVisualSkillReceivedFromNetwork?.Invoke(pending.SkillType);
                    ApplyIncomingOpponentVisualSkill(pending.SkillType, pending.CasterSkillLevel);
                }
            }

            while (_pendingIncomingPings.TryDequeue(out var pingType))
            {
                if (IsRunning)
                    PlayerPingReceivedFromNetwork?.Invoke(pingType);
            }

            while (_pendingLocalPingsSent.TryDequeue(out var sentPingType))
            {
                if (IsRunning)
                    LocalPlayerPingSent?.Invoke(sentPingType);
            }

            if (!IsRunning)
                return;
            AddSkillEnergy(SkillBalance.SkillEnergyPassiveGainPerSecondDefault * Time.unscaledDeltaTime);
            SyncActiveEffectSounds();

            var now = Time.unscaledTime;
            if (now >= _endTime)
            {
                if (_onlineMode)
                {
                    if (_resultSceneLoadAttempted)
                        return;
                    _resultSceneLoadAttempted = true;

                    var myId = AuthContext.Current?.Player.Id ?? System.Guid.Empty;
                    PendingMatchResult.Set(new PendingMatchResultPayload(
                        _online.MatchId,
                        myId,
                        _online.OpponentUsername,
                        _score,
                        _tapCount,
                        _missCount,
                        _online.DurationSec));
                    UnsubscribeMatchHubEvents();
                    MatchConnectionHolder.Instance?.ReleaseHub();
                    IsRunning = false;
                    if (!TryLoadResultScene())
                        StopMatch();
                    return;
                }

                StopMatch();
                RefreshUi();
                return;
            }

            for (var i = _active.Count - 1; i >= 0; i--)
            {
                var c = _active[i];
                if (now < c.DespawnAt)
                    continue;
                _active.RemoveAt(i);
                if (_onlineMode)
                    _missCount++;
                ResetSuccessfulTapStreak();
                spawner.Despawn(c);
            }

            if (_smokeVeilGo != null && !IsOpponentSmokeVeilActive)
                DestroySmokeVeil();

            // Таймер завязан на Time.unscaledTime — без перерисовки каждый кадр текст менялся только при тапе (RegisterTap вызывал RefreshUi).
            RefreshUi();
        }

        private void LateUpdate()
        {
            if (!IsRunning)
                return;

            if (_smokeVeilGo != null && IsOpponentSmokeVeilActive && spawner != null && spawner.PlayAreaRect != null)
                _smokeVeilGo.transform.SetAsLastSibling();

            if (_active.Count == 0)
                return;

            var m = ActiveCircleVisualSizeMultiplier;
            for (var i = 0; i < _active.Count; i++)
            {
                var c = _active[i];
                c.ApplyAnchorVisualMultiplier(m);
                c.RefreshVisualState();
            }
        }

        private bool TryLoadResultScene()
        {
            if (string.IsNullOrWhiteSpace(resultSceneName))
            {
                Debug.LogError("[Match] Result scene name is empty.");
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(resultSceneName))
            {
                Debug.LogError($"[Match] Scene '{resultSceneName}' is not available. Add it to File -> Build Profiles.");
                return false;
            }

            SceneManager.LoadScene(resultSceneName, LoadSceneMode.Single);
            return true;
        }

        private void PlayGameplayMusicIfConfigured()
        {
            if (gameplayBackgroundMusic == null)
                return;

            var src = gameplayMusicSource;
            if (src == null)
            {
                src = gameObject.AddComponent<AudioSource>();
                gameplayMusicSource = src;
            }

            ConfigureLooping2DAudioSource(src);
            src.volume = gameplayMusicVolume;
            if (src.clip == gameplayBackgroundMusic && src.isPlaying)
                return;

            src.clip = gameplayBackgroundMusic;
            src.Play();
        }

        private void StopGameplayMusic()
        {
            if (gameplayMusicSource == null)
                return;
            gameplayMusicSource.Stop();
            gameplayMusicSource.clip = null;
        }

        private void SyncActiveEffectSounds()
        {
            if (skillCatalog == null)
                return;
            foreach (var entry in skillCatalog.EnumerateEntriesWithLoopAudio())
            {
                var shouldPlay = IsSkillLoopSoundActive(entry.skillId);
                var src = EnsureSkillLoopAudioSource(entry.skillId);
                SyncLoopingEffectSoundOnSource(shouldPlay, entry.loopClip, src, entry.loopVolume);
            }
        }

        private bool IsSkillLoopSoundActive(int skillId) =>
            skillId switch
            {
                MatchSkillIds.GiantCirclesSelfBuff => IsGiantCirclesSkillActive,
                MatchSkillIds.OpponentRedDeceptionVisual => IsOpponentRedDeceptionVisualActive,
                MatchSkillIds.OpponentSmokeVeil => IsOpponentSmokeVeilActive,
                _ => false,
            };

        private AudioSource EnsureSkillLoopAudioSource(int skillId)
        {
            if (_skillLoopAudioSources.TryGetValue(skillId, out var src) && src != null)
                return src;
            var go = new GameObject($"SkillLoop_{skillId}");
            go.transform.SetParent(transform, false);
            src = go.AddComponent<AudioSource>();
            _skillLoopAudioSources[skillId] = src;
            return src;
        }

        private void SyncLoopingEffectSoundOnSource(bool shouldPlay, AudioClip? clip, AudioSource source, float volume)
        {
            if (!shouldPlay || clip == null)
            {
                StopLoopingSource(source);
                return;
            }

            ConfigureLooping2DAudioSource(source);
            source.volume = volume;
            if (source.clip != clip)
            {
                source.clip = clip;
                source.Play();
                return;
            }

            if (!source.isPlaying)
                source.Play();
        }

        private void StopActiveEffectSounds()
        {
            foreach (var src in _skillLoopAudioSources.Values)
                StopLoopingSource(src);
        }

        private static void StopLoopingSource(AudioSource? src)
        {
            if (src == null)
                return;
            src.Stop();
            src.clip = null;
        }

        private static void ConfigureLooping2DAudioSource(AudioSource src)
        {
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f;
        }

        private void ApplyModeUi(string modeTitle, string? opponent)
        {
            if (modeLabel != null)
                modeLabel.text = modeTitle;
            if (opponentLabel != null)
                opponentLabel.text = string.IsNullOrEmpty(opponent) ? string.Empty : $"Противник: {opponent}";
        }

        private AudioSource EnsureBubbleHitAudioSource()
        {
            var src = _bubbleHitAudioResolved;
            if (src != null)
                return src;
            src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            _bubbleHitAudioResolved = src;
            return src;
        }

        private void PlayBubbleHitSuccessSound()
        {
            if (bubbleHitSuccessClip == null)
                return;
            EnsureBubbleHitAudioSource().PlayOneShot(bubbleHitSuccessClip, bubbleHitVolume);
        }

        private void PlayChainLightningHitSound()
        {
            if (chainLightningHitClip != null)
                EnsureBubbleHitAudioSource().PlayOneShot(chainLightningHitClip, chainLightningHitVolume);
            else
                PlayBubbleHitSuccessSound();
        }

        private void PlayBubbleBombSound()
        {
            if (bubbleBombClip == null)
                return;
            EnsureBubbleHitAudioSource().PlayOneShot(bubbleBombClip, bubbleBombVolume);
        }

        private void RefreshUi()
        {
            if (scoreLabel != null)
                scoreLabel.text = $"Очки: {_score}";
            if (timerLabel != null)
            {
                if (!IsRunning)
                    timerLabel.text = "Стоп";
                else
                    timerLabel.text = $"Время: {Mathf.Max(0, _endTime - Time.unscaledTime):0}s";
            }
        }

        public bool HasEnoughSkillEnergy(float costPercent) =>
            IsRunning && _skillEnergyPercent + 0.0001f >= Mathf.Clamp(costPercent, 0f, SkillEnergyMaxPercent);

        private bool TryConsumeSkillEnergy(float costPercent)
        {
            var clamped = Mathf.Clamp(costPercent, 0f, SkillEnergyMaxPercent);
            if (!HasEnoughSkillEnergy(clamped))
                return false;
            _skillEnergyPercent = Mathf.Max(0f, _skillEnergyPercent - clamped);
            return true;
        }

        private void AddSkillEnergy(float amountPercent)
        {
            if (amountPercent <= 0f)
                return;
            _skillEnergyPercent = Mathf.Clamp(_skillEnergyPercent + amountPercent, 0f, SkillEnergyMaxPercent);
        }

        private void RegisterSuccessfulTapForEnergy(int streakSteps = 1)
        {
            var steps = Mathf.Max(1, streakSteps);
            for (var i = 0; i < steps; i++)
            {
                _successfulTapStreak++;
                var targetAwardPercent = GetComboAwardForStreak(_successfulTapStreak);
                if (targetAwardPercent <= _streakBestAwardPercent + 0.0001f)
                    continue;
                AddSkillEnergy(targetAwardPercent - _streakBestAwardPercent);
                _streakBestAwardPercent = targetAwardPercent;
            }
        }

        private void ResetSuccessfulTapStreak()
        {
            _successfulTapStreak = 0;
            _streakBestAwardPercent = 0f;
        }

        private static float GetComboAwardForStreak(int streak)
        {
            if (streak >= 10)
                return 100f;
            if (streak >= 7)
                return 50f;
            if (streak >= 5)
                return 30f;
            if (streak >= 3)
                return 20f;
            return 0f;
        }
    }
}
