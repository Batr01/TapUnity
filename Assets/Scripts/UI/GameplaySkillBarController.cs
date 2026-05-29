using System.Collections;
using System.Collections.Generic;
using TapBrawl.Core;
using TapBrawl.Core.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>
    /// Три слота скиллов (порядок = лоадаут с сервера). Кнопки в инспекторе: слот 0 / 1 / 2 слева направо.
    /// </summary>
    public sealed class GameplaySkillBarController : MonoBehaviour
    {
        [SerializeField] private MatchController? match;
        [SerializeField] private SkillCatalog? skillCatalog;

        [Header("Слоты 0..2 (лоадаут из лобби)")]
        [SerializeField] private Button? skillSlot0Button;
        [SerializeField] private Text? skillSlot0StatusText;
        [SerializeField] private Image? skillSlot0IconImage;
        [SerializeField] private Button? skillSlot1Button;
        [SerializeField] private Text? skillSlot1StatusText;
        [SerializeField] private Image? skillSlot1IconImage;
        [SerializeField] private Button? skillSlot2Button;
        [SerializeField] private Text? skillSlot2StatusText;
        [SerializeField] private Image? skillSlot2IconImage;

        [Header("Совместимость со старыми сценами (если слоты не заданы)")]
        [SerializeField] private Button? giantCirclesButton;
        [SerializeField] private Text? giantCirclesStatusText;
        [SerializeField] private Button? redDeceptionButton;
        [SerializeField] private Text? redDeceptionStatusText;
        [SerializeField] private Button? smokeVeilButton;
        [SerializeField] private Text? smokeVeilStatusText;

        [Header("Дебаффы от соперника (отдельно от кнопок)")]
        [SerializeField] private Text? incomingDebuffHintText;

        [Header("Энергия скиллов")]
        [SerializeField] private Image? skillEnergyFill;
        [SerializeField] private Text? skillEnergyText;
        [SerializeField] private Color skillOnCooldownDimColor = new(0.52f, 0.52f, 0.55f, 1f);
        [SerializeField] [Range(0f, 1f)] private float cooldownDimBlend = 0.42f;
        [SerializeField] private Color cooldownRadialTint = new(0.1f, 0.1f, 0.12f, 0.78f);

        [Header("Онлайн: уведомление о скилле соперника")]
        [SerializeField] private Text? opponentSkillNoticeText;

        private readonly Button[] _buttons = new Button[3];
        private readonly Text?[] _statusTexts = new Text?[3];
        private readonly Image?[] _iconImages = new Image?[3];
        private readonly Image?[] _overlays = new Image?[3];
        private readonly Color[] _graphicColors = new Color[3];
        private readonly bool[] _graphicCached = new bool[3];
        private readonly string?[] _defaultIdleCaptions = new string?[3];
        private string?[] _idleCaptions = new string?[3];
        private readonly int[] _lastSlotSkillIds = { int.MinValue, int.MinValue, int.MinValue };
        private bool _matchSkillSubscribed;
        private Coroutine? _noticeRoutine;
        private static Sprite? _whiteRadialSprite;

        private void Awake()
        {
            if (match == null)
                match = FindFirstObjectByType<MatchController>();

            _buttons[0] = skillSlot0Button != null ? skillSlot0Button : giantCirclesButton;
            _buttons[1] = skillSlot1Button != null ? skillSlot1Button : redDeceptionButton;
            _buttons[2] = skillSlot2Button != null ? skillSlot2Button : smokeVeilButton;
            _statusTexts[0] = skillSlot0StatusText != null ? skillSlot0StatusText : giantCirclesStatusText;
            _statusTexts[1] = skillSlot1StatusText != null ? skillSlot1StatusText : redDeceptionStatusText;
            _statusTexts[2] = skillSlot2StatusText != null ? skillSlot2StatusText : smokeVeilStatusText;
            _iconImages[0] = skillSlot0IconImage;
            _iconImages[1] = skillSlot1IconImage;
            _iconImages[2] = skillSlot2IconImage;

            if (_buttons[0] == null || _buttons[1] == null || _buttons[2] == null)
            {
                Debug.LogError("[SkillBar] Назначьте три кнопки (skillSlot0..2 или giant/red/smoke).");
                enabled = false;
                return;
            }

            EnsureOpponentSkillNoticeText();
            CacheGraphicColors();
            for (var i = 0; i < 3; i++)
            {
                var idx = i;
                EnsureCooldownOverlay(_buttons[i].transform, ref _overlays[i]);
                _buttons[i].onClick.RemoveAllListeners();
                _buttons[i].onClick.AddListener(() => OnSlotClicked(idx));
            }
        }

        private void Start()
        {
            // После MatchController.Start (подмена лоадаута на арене тренировки).
            CacheIdleCaptions();
        }

        private void OnEnable()
        {
            if (match == null)
                match = FindFirstObjectByType<MatchController>();
            if (match != null && !_matchSkillSubscribed)
            {
                match.OpponentVisualSkillReceivedFromNetwork += OnOpponentVisualSkillFromNetwork;
                _matchSkillSubscribed = true;
            }
        }

        private void OnDisable()
        {
            if (match != null && _matchSkillSubscribed)
            {
                match.OpponentVisualSkillReceivedFromNetwork -= OnOpponentVisualSkillFromNetwork;
                _matchSkillSubscribed = false;
            }

            if (_noticeRoutine != null)
            {
                StopCoroutine(_noticeRoutine);
                _noticeRoutine = null;
            }
        }

        private void EnsureOpponentSkillNoticeText()
        {
            if (opponentSkillNoticeText != null)
                return;

            var go = new GameObject("OpponentSkillNotice", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            go.transform.SetAsLastSibling();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.02f, 0.7f);
            rt.anchorMax = new Vector2(0.98f, 0.97f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var tx = go.AddComponent<Text>();
            var fontRef = skillSlot0StatusText != null
                ? skillSlot0StatusText.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tx.font = fontRef;
            tx.fontSize = skillSlot0StatusText != null ? Mathf.Clamp(skillSlot0StatusText.fontSize, 20, 30) : 24;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = new Color(1f, 0.88f, 0.25f, 1f);
            tx.raycastTarget = false;
            tx.text = string.Empty;
            opponentSkillNoticeText = tx;
        }

        private void OnOpponentVisualSkillFromNetwork(int skillType)
        {
            if (opponentSkillNoticeText == null)
                return;
            var msg = skillCatalog != null
                ? skillCatalog.GetOpponentAppliedNotice(skillType)
                : skillType == MatchSkillIds.OpponentRedDeceptionVisual
                    ? "Соперник применил: красная пелена"
                    : skillType == MatchSkillIds.OpponentSmokeVeil
                        ? "Соперник применил: дымовая завеса"
                        : "Соперник применил скилл";
            if (_noticeRoutine != null)
                StopCoroutine(_noticeRoutine);
            _noticeRoutine = StartCoroutine(NoticeRoutine(msg));
        }

        private IEnumerator NoticeRoutine(string message)
        {
            opponentSkillNoticeText!.text = message;
            yield return new WaitForSecondsRealtime(2.75f);
            opponentSkillNoticeText.text = string.Empty;
            _noticeRoutine = null;
        }

        private void CacheIdleCaptions()
        {
            for (var i = 0; i < 3; i++)
            {
                _defaultIdleCaptions[i] = _statusTexts[i] != null ? _statusTexts[i]!.text : null;
                var sid = PlayerSkillsRuntimeState.GetSkillIdInSlot(i);
                _idleCaptions[i] = ResolveIdleCaption(i, sid);
            }
        }

        private void OnDestroy()
        {
            for (var i = 0; i < 3; i++)
            {
                if (_buttons[i] != null)
                    _buttons[i].onClick.RemoveAllListeners();
            }
        }

        private void OnSlotClicked(int slotIndex)
        {
            if (match == null)
                match = FindFirstObjectByType<MatchController>();
            if (match == null || !match.IsRunning)
                return;

            var sid = PlayerSkillsRuntimeState.GetSkillIdInSlot(slotIndex);
            if (sid == MatchSkillIds.GiantCirclesSelfBuff)
                match.TryActivateGiantCirclesSkill();
            else if (sid == MatchSkillIds.OverheatSelfBuff)
                match.TryActivateOverheatSkill();
            else if (sid == MatchSkillIds.ChainDischargeSelfBuff)
                match.TryActivateChainDischargeSkill();
            else if (sid == MatchSkillIds.OpponentRedDeceptionVisual)
                match.RequestSendOpponentVisualSkill(MatchSkillIds.OpponentRedDeceptionVisual);
            else if (sid == MatchSkillIds.OpponentSmokeVeil)
                match.RequestSendOpponentVisualSkill(MatchSkillIds.OpponentSmokeVeil);
        }

        private void Update()
        {
            var m = match;
            if (m == null)
                return;

            ApplyIncomingDebuffHint(m);
            ApplySkillEnergyUi(m);

            for (var slot = 0; slot < 3; slot++)
            {
                var sid = PlayerSkillsRuntimeState.GetSkillIdInSlot(slot);
                ApplySlotPresentation(slot, sid);
                UpdateSlotStatus(slot, sid, m);
                var cost = GetEnergyCostForSkill(sid, m);
                ApplyCooldownVisual(
                    _buttons[slot],
                    _overlays[slot],
                    ref _graphicColors[slot],
                    ref _graphicCached[slot],
                    m.IsRunning && !m.HasEnoughSkillEnergy(cost),
                    Mathf.Max(0f, cost - m.SkillEnergyPercent),
                    Mathf.Max(0.01f, cost));
                _buttons[slot].interactable = m.IsRunning && m.HasEnoughSkillEnergy(cost);
            }
        }

        private static float GetEnergyCostForSkill(int skillId, MatchController m) =>
            skillId switch
            {
                MatchSkillIds.GiantCirclesSelfBuff => m.GiantCirclesSkillCostPercent,
                MatchSkillIds.OverheatSelfBuff => m.OverheatSkillCostPercent,
                MatchSkillIds.ChainDischargeSelfBuff => m.ChainDischargeSkillCostPercent,
                MatchSkillIds.OpponentRedDeceptionVisual => m.OpponentRedDeceptionCostPercent,
                MatchSkillIds.OpponentSmokeVeil => m.OpponentSmokeVeilCostPercent,
                _ => 100f,
            };

        private void ApplySlotPresentation(int slot, int skillId)
        {
            if (_lastSlotSkillIds[slot] == skillId)
                return;
            _lastSlotSkillIds[slot] = skillId;
            _idleCaptions[slot] = ResolveIdleCaption(slot, skillId);

            var icon = _iconImages[slot];
            if (icon == null)
                return;

            var spr = skillCatalog != null ? skillCatalog.GetIcon(skillId) : null;
            if (spr != null)
            {
                icon.enabled = true;
                icon.sprite = spr;
                return;
            }

            icon.sprite = null;
            icon.enabled = false;
        }

        private string ResolveSkillDisplayName(int skillId) =>
            skillCatalog != null ? skillCatalog.GetDisplayName(skillId) : SkillDefinitions.GetDisplayName(skillId);

        private string? ResolveIdleCaption(int slot, int skillId)
        {
            var suffix = skillCatalog != null ? skillCatalog.GetIdleSlotSuffix(skillId) : null;
            if (!string.IsNullOrWhiteSpace(suffix))
                return suffix;
            return _defaultIdleCaptions[slot];
        }

        private void UpdateSlotStatus(int slot, int skillId, MatchController m)
        {
            var tx = _statusTexts[slot];
            if (tx == null)
                return;
            var name = ResolveSkillDisplayName(skillId);
            var idle = _idleCaptions[slot];

            if (skillId == MatchSkillIds.GiantCirclesSelfBuff && m.IsGiantCirclesSkillActive)
                tx.text = $"{name} ({m.GiantCirclesSkillRemainingSeconds:0.#}с)";
            else if (skillId == MatchSkillIds.OverheatSelfBuff && m.IsOverheatSkillActive)
                tx.text = $"{name} ({m.OverheatSkillRemainingSeconds:0.#}с)";
            else if (skillId == MatchSkillIds.ChainDischargeSelfBuff && m.IsChainDischargeArmed)
                tx.text = $"{name} (заряд {m.ChainDischargeArmedRemainingSeconds:0.#}с)";
            else if (!string.IsNullOrEmpty(idle))
                tx.text = $"{name}\n{idle}";
            else
                tx.text = name;
        }

        private void CacheGraphicColors()
        {
            for (var i = 0; i < 3; i++)
                CacheOne(_buttons[i], ref _graphicColors[i], ref _graphicCached[i]);
        }

        private static void CacheOne(Button btn, ref Color color, ref bool cached)
        {
            if (cached || btn.targetGraphic is not Graphic g)
                return;
            color = g.color;
            cached = true;
        }

        private void EnsureCooldownOverlay(Transform buttonRoot, ref Image? overlayField)
        {
            if (overlayField != null)
                return;
            var existing = buttonRoot.Find("CooldownRadial");
            if (existing != null)
            {
                overlayField = existing.GetComponent<Image>();
                if (overlayField != null)
                    return;
            }

            var go = new GameObject("CooldownRadial", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(buttonRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.SetAsLastSibling();

            var img = go.GetComponent<Image>();
            img.sprite = GetOrCreateWhiteSprite();
            img.color = cooldownRadialTint;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Radial360;
            img.fillOrigin = (int)Image.Origin360.Top;
            img.fillClockwise = true;
            img.fillAmount = 0f;
            img.raycastTarget = false;
            overlayField = img;
        }

        private static Sprite GetOrCreateWhiteSprite()
        {
            if (_whiteRadialSprite != null)
                return _whiteRadialSprite;
            var tex = Texture2D.whiteTexture;
            _whiteRadialSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
            return _whiteRadialSprite;
        }

        private void ApplyCooldownVisual(
            Button btn,
            Image? overlay,
            ref Color normalColor,
            ref bool hasCachedColor,
            bool cooling,
            float remainingSec,
            float totalDurationSec)
        {
            if (!hasCachedColor && btn.targetGraphic is Graphic gx)
            {
                normalColor = gx.color;
                hasCachedColor = true;
            }

            var ratio = cooling && totalDurationSec > 0.001f
                ? Mathf.Clamp01(remainingSec / totalDurationSec)
                : 0f;

            if (overlay != null)
            {
                overlay.enabled = cooling && ratio > 0.001f;
                overlay.color = cooldownRadialTint;
                overlay.fillAmount = ratio;
            }

            if (btn.targetGraphic is Graphic g)
            {
                if (!hasCachedColor)
                    return;
                var dim = Mathf.Clamp01(cooldownDimBlend) * (cooling ? ratio : 0f);
                g.color = Color.Lerp(normalColor, skillOnCooldownDimColor, dim);
            }
        }

        private void ApplyIncomingDebuffHint(MatchController m)
        {
            if (incomingDebuffHintText == null)
                return;
            var red = m.IsOpponentRedDeceptionVisualActive;
            var smoke = m.IsOpponentSmokeVeilActive;
            if (red && smoke)
            {
                incomingDebuffHintText.text = skillCatalog != null
                    ? skillCatalog.FormatIncomingDebuffBoth(
                        m.OpponentRedDeceptionRemainingSeconds,
                        m.OpponentSmokeVeilRemainingSeconds)
                    : $"На вас: пелена {m.OpponentRedDeceptionRemainingSeconds:0.#}с, дым {m.OpponentSmokeVeilRemainingSeconds:0.#}с";
            }
            else if (red)
            {
                var fmt = skillCatalog?.GetIncomingDebuffSingleFormat(MatchSkillIds.OpponentRedDeceptionVisual);
                incomingDebuffHintText.text = fmt != null
                    ? string.Format(fmt, m.OpponentRedDeceptionRemainingSeconds)
                    : $"На вас: красная пелена ({m.OpponentRedDeceptionRemainingSeconds:0.#}с)";
            }
            else if (smoke)
            {
                var fmt = skillCatalog?.GetIncomingDebuffSingleFormat(MatchSkillIds.OpponentSmokeVeil);
                incomingDebuffHintText.text = fmt != null
                    ? string.Format(fmt, m.OpponentSmokeVeilRemainingSeconds)
                    : $"На вас: дымовая завеса ({m.OpponentSmokeVeilRemainingSeconds:0.#}с)";
            }
            else
                incomingDebuffHintText.text = string.Empty;
        }

        private void ApplySkillEnergyUi(MatchController m)
        {
            if (skillEnergyFill != null)
                skillEnergyFill.fillAmount = m.SkillEnergyNormalized;
            if (skillEnergyText != null)
                skillEnergyText.text = $"{m.SkillEnergyPercent:0}%";
        }
    }
}
