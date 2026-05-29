using System.Collections;
using TapBrawl.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>
    /// Слева внизу: кнопка открытия списка пингов, выбор — отправка сопернику (онлайн) или эхо (тренировка).
    /// Перезарядка отправки задаётся в <see cref="MatchController"/> (по умолчанию 3 с).
    /// Входящий пинг от соперника и исходящий (ваш) — отдельные панели с иконкой, настраиваются в инспекторе.
    /// </summary>
    public sealed class PingPanelController : MonoBehaviour
    {
        [SerializeField] private MatchController? match;

        [Header("Иконки пинов (перетащите спрайты из PNG)")]
        [SerializeField] private Sprite? pinLikeSprite;
        [SerializeField] private Sprite? pinDislikeSprite;
        [SerializeField] private Sprite? pin67Sprite;

        [Header("Запасной вид, если спрайт не задан")]
        [SerializeField] private Color likeColor = new(0.35f, 0.85f, 0.45f, 1f);
        [SerializeField] private Color dislikeColor = new(0.95f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color sticker67Color = new(0.65f, 0.45f, 0.95f, 1f);

        [Header("Кнопка «Пинги» (если пусто — создаётся автоматически)")]
        [SerializeField] private Button? openPingButtonOverride;
        [SerializeField] private Text? openPingButtonCaptionOverride;

        [Header("Панель выбора пинов (если пусто — создаётся автоматически)")]
        [SerializeField] private RectTransform? pickerPanelOverride;

        [Header("Входящий пинг от соперника")]
        [Tooltip("Корень панели (RectTransform). Если пусто — создаётся дочерний IncomingPingDisplay.")]
        [SerializeField] private RectTransform? incomingDisplayPanelOverride;
        [Tooltip("Image для иконки пина. Если пусто — ищется дочерний IncomingIcon или создаётся.")]
        [SerializeField] private Image? incomingPingIconOverride;
        [Tooltip("Прозрачность панели. Если пусто — добавляется на корень панели.")]
        [SerializeField] private CanvasGroup? incomingDisplayCanvasGroupOverride;
        [SerializeField] private float incomingVisibleSeconds = 2.2f;
        [SerializeField] private float incomingFadeInSeconds = 0.12f;
        [SerializeField] private float incomingFadeOutSeconds = 0.18f;
        [SerializeField] private bool showIncomingOpponentCaption = true;
        [Tooltip("Подпись «Соперник». Если пусто и включена подпись — создаётся на авто-панели.")]
        [SerializeField] private Text? incomingCaptionOverride;

        [Header("Исходящий пинг (ваш отправленный)")]
        [Tooltip("Корень панели. Если пусто — создаётся дочерний OutgoingPingDisplay.")]
        [SerializeField] private RectTransform? outgoingDisplayPanelOverride;
        [Tooltip("Image для иконки. Если пусто — ищется OutgoingIcon или первый Image.")]
        [SerializeField] private Image? outgoingPingIconOverride;
        [SerializeField] private CanvasGroup? outgoingDisplayCanvasGroupOverride;
        [SerializeField] private float outgoingVisibleSeconds = 2.2f;
        [SerializeField] private float outgoingFadeInSeconds = 0.12f;
        [SerializeField] private float outgoingFadeOutSeconds = 0.18f;
        [SerializeField] private bool showOutgoingCaption = true;
        [SerializeField] private string outgoingCaptionText = "Вы";
        [Tooltip("Если пусто и включена подпись — создаётся на авто-панели.")]
        [SerializeField] private Text? outgoingCaptionOverride;

        private Button? _openButton;
        private Text? _openButtonCaption;
        private GameObject? _pickerRoot;
        private RectTransform? _incomingPanelRt;
        private Image? _incomingPingImage;
        private CanvasGroup? _incomingCanvasGroup;
        private Text? _incomingCaption;
        private Coroutine? _incomingRoutine;
        private RectTransform? _outgoingPanelRt;
        private Image? _outgoingPingImage;
        private CanvasGroup? _outgoingCanvasGroup;
        private Text? _outgoingCaption;
        private Coroutine? _outgoingRoutine;
        private Font? _uiFont;
        private bool _incomingPanelBuiltByScript;
        private bool _outgoingPanelBuiltByScript;

        private void Awake()
        {
            if (match == null)
                match = FindFirstObjectByType<MatchController>();
            _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUi();
        }

        private void OnEnable()
        {
            if (match != null)
            {
                match.PlayerPingReceivedFromNetwork += OnIncomingPing;
                match.LocalPlayerPingSent += OnOutgoingPingSent;
                match.GameplayCircleTapped += ClosePingPicker;
            }
        }

        private void OnDisable()
        {
            if (match != null)
            {
                match.PlayerPingReceivedFromNetwork -= OnIncomingPing;
                match.LocalPlayerPingSent -= OnOutgoingPingSent;
                match.GameplayCircleTapped -= ClosePingPicker;
            }
            if (_incomingRoutine != null)
            {
                StopCoroutine(_incomingRoutine);
                _incomingRoutine = null;
            }
            if (_outgoingRoutine != null)
            {
                StopCoroutine(_outgoingRoutine);
                _outgoingRoutine = null;
            }
        }

        private void Update()
        {
            var m = match;
            if (_openButton == null || _openButtonCaption == null || m == null)
                return;
            if (!m.IsRunning)
            {
                _openButton.interactable = false;
                _openButtonCaption.text = "Пинги";
                if (_pickerRoot != null)
                    _pickerRoot.SetActive(false);
                return;
            }

            var cd = m.PingSendCooldownRemainingSeconds;
            if (cd > 0.05f)
            {
                _openButton.interactable = false;
                _openButtonCaption.text = $"Пинги ({cd:0.#}с)";
            }
            else
            {
                _openButton.interactable = true;
                _openButtonCaption.text = "Пинги";
            }
        }

        private void LateUpdate()
        {
            TryDismissPingPickerFromOutsideTap();
        }

        private void ClosePingPicker()
        {
            if (_pickerRoot != null && _pickerRoot.activeSelf)
                _pickerRoot.SetActive(false);
        }

        private static Camera? GetCanvasCameraForUi(RectTransform rt)
        {
            var c = rt.GetComponentInParent<Canvas>();
            if (c == null)
                return null;
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;
            return c.worldCamera;
        }

        /// <summary>Закрывает выбор пинга при отпускании указателя вне панели и вне кнопки «Пинги» (тап по полю / UI).</summary>
        private void TryDismissPingPickerFromOutsideTap()
        {
            if (_pickerRoot == null || !_pickerRoot.activeSelf)
                return;
            if (match == null || !match.IsRunning)
                return;
            if (!MatchController.WasPrimaryPointerReleasedThisFrame())
                return;
            if (!MatchController.TryGetCurrentPointerScreen(out var screen))
                return;

            if (_openButton != null)
            {
                var openRt = (RectTransform)_openButton.transform;
                if (RectTransformUtility.RectangleContainsScreenPoint(openRt, screen, GetCanvasCameraForUi(openRt)))
                    return;
            }

            var pickRt = _pickerRoot.GetComponent<RectTransform>();
            if (pickRt != null &&
                RectTransformUtility.RectangleContainsScreenPoint(pickRt, screen, GetCanvasCameraForUi(pickRt)))
                return;

            ClosePingPicker();
        }

        private void BuildUi()
        {
            var root = (RectTransform)transform;
            root.anchorMin = new Vector2(0f, 0f);
            root.anchorMax = new Vector2(0f, 0f);
            root.pivot = new Vector2(0f, 0f);
            if (root.sizeDelta.sqrMagnitude < 10f)
                root.sizeDelta = new Vector2(260f, 240f);

            if (openPingButtonOverride != null)
            {
                _openButton = openPingButtonOverride;
                _openButton.onClick.RemoveListener(TogglePicker);
                _openButton.onClick.AddListener(TogglePicker);
                _openButtonCaption = openPingButtonCaptionOverride;
                if (_openButtonCaption == null)
                    _openButtonCaption = _openButton.GetComponentInChildren<Text>(true);
                if (_openButtonCaption == null)
                {
                    var capGo = new GameObject("Caption", typeof(RectTransform), typeof(Text));
                    capGo.transform.SetParent(_openButton.transform, false);
                    var capRt = capGo.GetComponent<RectTransform>();
                    capRt.anchorMin = Vector2.zero;
                    capRt.anchorMax = Vector2.one;
                    capRt.offsetMin = Vector2.zero;
                    capRt.offsetMax = Vector2.zero;
                    _openButtonCaption = capGo.GetComponent<Text>();
                    _openButtonCaption.font = _uiFont;
                    _openButtonCaption.fontSize = 20;
                    _openButtonCaption.alignment = TextAnchor.MiddleCenter;
                    _openButtonCaption.color = Color.white;
                    _openButtonCaption.raycastTarget = false;
                    _openButtonCaption.text = "Пинги";
                }
            }
            else
            {
                var openGo = new GameObject("PingOpen", typeof(RectTransform), typeof(Image), typeof(Button));
                openGo.transform.SetParent(root, false);
                var openRt = openGo.GetComponent<RectTransform>();
                openRt.anchorMin = new Vector2(0f, 0f);
                openRt.anchorMax = new Vector2(0f, 0f);
                openRt.pivot = new Vector2(0f, 0f);
                openRt.anchoredPosition = Vector2.zero;
                openRt.sizeDelta = new Vector2(118f, 44f);
                var openImg = openGo.GetComponent<Image>();
                openImg.color = new Color(0.12f, 0.13f, 0.2f, 0.94f);
                openImg.raycastTarget = true;
                _openButton = openGo.GetComponent<Button>();
                _openButton.targetGraphic = openImg;

                var capGo = new GameObject("Caption", typeof(RectTransform), typeof(Text));
                capGo.transform.SetParent(openGo.transform, false);
                var capRt = capGo.GetComponent<RectTransform>();
                capRt.anchorMin = Vector2.zero;
                capRt.anchorMax = Vector2.one;
                capRt.offsetMin = Vector2.zero;
                capRt.offsetMax = Vector2.zero;
                _openButtonCaption = capGo.GetComponent<Text>();
                _openButtonCaption.font = _uiFont;
                _openButtonCaption.fontSize = 20;
                _openButtonCaption.alignment = TextAnchor.MiddleCenter;
                _openButtonCaption.color = Color.white;
                _openButtonCaption.raycastTarget = false;
                _openButtonCaption.text = "Пинги";
                _openButton.onClick.AddListener(TogglePicker);
            }

            if (pickerPanelOverride != null)
            {
                _pickerRoot = pickerPanelOverride.gameObject;
                if (_pickerRoot.GetComponent<Image>() == null)
                {
                    var im = _pickerRoot.AddComponent<Image>();
                    im.color = new Color(0.08f, 0.09f, 0.14f, 0.96f);
                    im.raycastTarget = true;
                }

                _pickerRoot.SetActive(false);
            }
            else
            {
                _pickerRoot = new GameObject("PingPicker", typeof(RectTransform), typeof(Image));
                _pickerRoot.transform.SetParent(root, false);
                var pickRt = _pickerRoot.GetComponent<RectTransform>();
                pickRt.anchorMin = new Vector2(0f, 0f);
                pickRt.anchorMax = new Vector2(0f, 0f);
                pickRt.pivot = new Vector2(0f, 0f);
                pickRt.anchoredPosition = new Vector2(0f, 52f);
                pickRt.sizeDelta = new Vector2(200f, 168f);
                var pickBg = _pickerRoot.GetComponent<Image>();
                pickBg.color = new Color(0.08f, 0.09f, 0.14f, 0.96f);
                pickBg.raycastTarget = true;
                _pickerRoot.SetActive(false);
            }

            AddPingOption(MatchPingIds.Like, pinLikeSprite, likeColor, 0, "Лайк");
            AddPingOption(MatchPingIds.Dislike, pinDislikeSprite, dislikeColor, 1, "Дизлайк");
            AddPingOption(MatchPingIds.Sticker67, pin67Sprite, sticker67Color, 2, "67");

            BuildOutgoingPingDisplay(root);
            BuildIncomingPingDisplay(root);
            if (_incomingPanelBuiltByScript && _outgoingPanelBuiltByScript && _incomingPanelRt != null)
                _incomingPanelRt.anchoredPosition = new Vector2(0f, -74f);
        }

        private void BuildIncomingPingDisplay(RectTransform root)
        {
            if (incomingDisplayPanelOverride != null)
            {
                _incomingPanelRt = incomingDisplayPanelOverride;
                _incomingPanelBuiltByScript = false;
                if (incomingPingIconOverride != null)
                    _incomingPingImage = incomingPingIconOverride;
                else
                {
                    var t = _incomingPanelRt.Find("IncomingIcon");
                    if (t != null)
                        _incomingPingImage = t.GetComponent<Image>();
                    if (_incomingPingImage == null)
                        _incomingPingImage = _incomingPanelRt.GetComponentInChildren<Image>(true);
                }

                _incomingCanvasGroup = incomingDisplayCanvasGroupOverride;
                if (_incomingCanvasGroup == null)
                    _incomingCanvasGroup = _incomingPanelRt.GetComponent<CanvasGroup>();
                if (_incomingCanvasGroup == null)
                    _incomingCanvasGroup = _incomingPanelRt.gameObject.AddComponent<CanvasGroup>();

                _incomingCaption = incomingCaptionOverride;
                if (showIncomingOpponentCaption && _incomingCaption == null)
                    _incomingCaption = _incomingPanelRt.GetComponentInChildren<Text>(true);
            }
            else
            {
                _incomingPanelBuiltByScript = true;
                var panelGo = new GameObject("IncomingPingDisplay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                panelGo.transform.SetParent(root, false);
                var rt = panelGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(0f, 68f);
                var bg = panelGo.GetComponent<Image>();
                bg.color = new Color(0.06f, 0.07f, 0.12f, 0.92f);
                bg.raycastTarget = false;
                _incomingCanvasGroup = panelGo.GetComponent<CanvasGroup>();
                _incomingCanvasGroup.alpha = 0f;
                _incomingCanvasGroup.interactable = false;
                _incomingCanvasGroup.blocksRaycasts = false;

                var row = new GameObject("IncomingRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                row.transform.SetParent(panelGo.transform, false);
                var rowRt = row.GetComponent<RectTransform>();
                rowRt.anchorMin = Vector2.zero;
                rowRt.anchorMax = Vector2.one;
                rowRt.offsetMin = new Vector2(10f, 6f);
                rowRt.offsetMax = new Vector2(-10f, -6f);
                var h = row.GetComponent<HorizontalLayoutGroup>();
                h.childAlignment = TextAnchor.MiddleLeft;
                h.spacing = 10f;
                h.childControlWidth = false;
                h.childControlHeight = false;
                h.childForceExpandWidth = false;
                h.childForceExpandHeight = false;

                if (showIncomingOpponentCaption)
                {
                    var capGo = new GameObject("OpponentCaption", typeof(RectTransform), typeof(Text));
                    capGo.transform.SetParent(row.transform, false);
                    var capRt = capGo.GetComponent<RectTransform>();
                    capRt.sizeDelta = new Vector2(110f, 48f);
                    _incomingCaption = capGo.GetComponent<Text>();
                    _incomingCaption.font = _uiFont;
                    _incomingCaption.fontSize = 18;
                    _incomingCaption.alignment = TextAnchor.MiddleLeft;
                    _incomingCaption.color = new Color(0.85f, 0.88f, 0.95f, 1f);
                    _incomingCaption.raycastTarget = false;
                    _incomingCaption.text = "Соперник";
                }

                var iconGo = new GameObject("IncomingIcon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(row.transform, false);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.sizeDelta = new Vector2(52f, 52f);
                _incomingPingImage = iconGo.GetComponent<Image>();
                _incomingPingImage.preserveAspect = true;
                _incomingPingImage.raycastTarget = false;

                _incomingPanelRt = rt;
            }

            if (_incomingPanelRt != null)
            {
                if (_incomingPanelBuiltByScript)
                    _incomingPanelRt.gameObject.SetActive(false);
                else if (_incomingCanvasGroup != null)
                    _incomingCanvasGroup.alpha = 0f;
            }
        }

        private void BuildOutgoingPingDisplay(RectTransform root)
        {
            if (outgoingDisplayPanelOverride != null)
            {
                _outgoingPanelRt = outgoingDisplayPanelOverride;
                _outgoingPanelBuiltByScript = false;
                if (outgoingPingIconOverride != null)
                    _outgoingPingImage = outgoingPingIconOverride;
                else
                {
                    var t = _outgoingPanelRt.Find("OutgoingIcon");
                    if (t != null)
                        _outgoingPingImage = t.GetComponent<Image>();
                    if (_outgoingPingImage == null)
                        _outgoingPingImage = _outgoingPanelRt.GetComponentInChildren<Image>(true);
                }

                _outgoingCanvasGroup = outgoingDisplayCanvasGroupOverride;
                if (_outgoingCanvasGroup == null)
                    _outgoingCanvasGroup = _outgoingPanelRt.GetComponent<CanvasGroup>();
                if (_outgoingCanvasGroup == null)
                    _outgoingCanvasGroup = _outgoingPanelRt.gameObject.AddComponent<CanvasGroup>();

                _outgoingCaption = outgoingCaptionOverride;
                if (showOutgoingCaption && _outgoingCaption == null)
                    _outgoingCaption = _outgoingPanelRt.GetComponentInChildren<Text>(true);
            }
            else
            {
                _outgoingPanelBuiltByScript = true;
                var panelGo = new GameObject("OutgoingPingDisplay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                panelGo.transform.SetParent(root, false);
                var rt = panelGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(0f, 68f);
                var bg = panelGo.GetComponent<Image>();
                bg.color = new Color(0.08f, 0.09f, 0.14f, 0.9f);
                bg.raycastTarget = false;
                _outgoingCanvasGroup = panelGo.GetComponent<CanvasGroup>();
                _outgoingCanvasGroup.alpha = 0f;
                _outgoingCanvasGroup.interactable = false;
                _outgoingCanvasGroup.blocksRaycasts = false;

                var row = new GameObject("OutgoingRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                row.transform.SetParent(panelGo.transform, false);
                var rowRt = row.GetComponent<RectTransform>();
                rowRt.anchorMin = Vector2.zero;
                rowRt.anchorMax = Vector2.one;
                rowRt.offsetMin = new Vector2(10f, 6f);
                rowRt.offsetMax = new Vector2(-10f, -6f);
                var h = row.GetComponent<HorizontalLayoutGroup>();
                h.childAlignment = TextAnchor.MiddleLeft;
                h.spacing = 10f;
                h.childControlWidth = false;
                h.childControlHeight = false;
                h.childForceExpandWidth = false;
                h.childForceExpandHeight = false;

                if (showOutgoingCaption)
                {
                    var capGo = new GameObject("OutgoingCaption", typeof(RectTransform), typeof(Text));
                    capGo.transform.SetParent(row.transform, false);
                    var capRt = capGo.GetComponent<RectTransform>();
                    capRt.sizeDelta = new Vector2(110f, 48f);
                    _outgoingCaption = capGo.GetComponent<Text>();
                    _outgoingCaption.font = _uiFont;
                    _outgoingCaption.fontSize = 18;
                    _outgoingCaption.alignment = TextAnchor.MiddleLeft;
                    _outgoingCaption.color = new Color(0.75f, 0.92f, 1f, 1f);
                    _outgoingCaption.raycastTarget = false;
                    _outgoingCaption.text = string.IsNullOrEmpty(outgoingCaptionText) ? "Вы" : outgoingCaptionText;
                }

                var iconGo = new GameObject("OutgoingIcon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(row.transform, false);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.sizeDelta = new Vector2(52f, 52f);
                _outgoingPingImage = iconGo.GetComponent<Image>();
                _outgoingPingImage.preserveAspect = true;
                _outgoingPingImage.raycastTarget = false;

                _outgoingPanelRt = rt;
            }

            if (_outgoingPanelRt != null)
            {
                if (_outgoingPanelBuiltByScript)
                    _outgoingPanelRt.gameObject.SetActive(false);
                else if (_outgoingCanvasGroup != null)
                    _outgoingCanvasGroup.alpha = 0f;
            }
        }

        private void AddPingOption(int pingId, Sprite? icon, Color fallbackBg, int row, string fallbackLabel)
        {
            if (_pickerRoot == null)
                return;
            var go = new GameObject($"Ping_{pingId}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_pickerRoot.transform, false);
            var rt = go.GetComponent<RectTransform>();
            var top = 0.92f - row * 0.29f;
            var bottom = top - 0.24f;
            rt.anchorMin = new Vector2(0.06f, bottom);
            rt.anchorMax = new Vector2(0.94f, top);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            var id = pingId;
            btn.onClick.AddListener(() => OnPickPing(id));

            if (icon != null)
            {
                img.sprite = icon;
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else
            {
                img.color = fallbackBg;
                var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                textGo.transform.SetParent(go.transform, false);
                var trt = textGo.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(8f, 0f);
                trt.offsetMax = Vector2.zero;
                var tx = textGo.GetComponent<Text>();
                tx.font = _uiFont;
                tx.fontSize = 22;
                tx.alignment = TextAnchor.MiddleLeft;
                tx.color = Color.white;
                tx.raycastTarget = false;
                tx.text = fallbackLabel;
            }
        }

        private void TogglePicker()
        {
            if (_pickerRoot == null || match == null || !match.IsRunning)
                return;
            if (match.PingSendCooldownRemainingSeconds > 0.05f)
                return;
            _pickerRoot.SetActive(!_pickerRoot.activeSelf);
        }

        private void OnPickPing(int pingType)
        {
            ClosePingPicker();
            match?.RequestSendPing(pingType);
        }

        private void OnIncomingPing(int pingType)
        {
            if (_incomingPingImage == null || _incomingCanvasGroup == null || _incomingPanelRt == null)
                return;

            ApplyPingIconToImage(_incomingPingImage, pingType);

            if (_incomingCaption != null)
            {
                _incomingCaption.gameObject.SetActive(showIncomingOpponentCaption);
                if (showIncomingOpponentCaption)
                    _incomingCaption.text = "Соперник";
            }

            if (_incomingRoutine != null)
                StopCoroutine(_incomingRoutine);
            _incomingRoutine = StartCoroutine(IncomingPingDisplayRoutine());
        }

        private void OnOutgoingPingSent(int pingType)
        {
            if (_outgoingPingImage == null || _outgoingCanvasGroup == null || _outgoingPanelRt == null)
                return;

            ApplyPingIconToImage(_outgoingPingImage, pingType);

            if (_outgoingCaption != null)
            {
                _outgoingCaption.gameObject.SetActive(showOutgoingCaption);
                if (showOutgoingCaption)
                    _outgoingCaption.text = string.IsNullOrEmpty(outgoingCaptionText) ? "Вы" : outgoingCaptionText;
            }

            if (_outgoingRoutine != null)
                StopCoroutine(_outgoingRoutine);
            _outgoingRoutine = StartCoroutine(OutgoingPingDisplayRoutine());
        }

        private void ApplyPingIconToImage(Image img, int pingType)
        {
            var sp = SpriteForPingType(pingType);
            if (sp != null)
            {
                img.sprite = sp;
                img.type = Image.Type.Simple;
                img.color = Color.white;
            }
            else
            {
                img.sprite = null;
                img.color = FallbackColorForPing(pingType);
            }
        }

        private Sprite? SpriteForPingType(int pingType) => pingType switch
        {
            MatchPingIds.Like => pinLikeSprite,
            MatchPingIds.Dislike => pinDislikeSprite,
            MatchPingIds.Sticker67 => pin67Sprite,
            _ => null
        };

        private Color FallbackColorForPing(int pingType) => pingType switch
        {
            MatchPingIds.Like => likeColor,
            MatchPingIds.Dislike => dislikeColor,
            MatchPingIds.Sticker67 => sticker67Color,
            _ => new Color(0.5f, 0.5f, 0.55f, 1f)
        };

        private IEnumerator IncomingPingDisplayRoutine()
        {
            var cg = _incomingCanvasGroup!;
            var panelGo = _incomingPanelRt!.gameObject;
            panelGo.SetActive(true);
            cg.alpha = 0f;
            var t = 0f;
            var fin = Mathf.Max(0.01f, incomingFadeInSeconds);
            while (t < fin)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(t / fin);
                yield return null;
            }

            cg.alpha = 1f;
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, incomingVisibleSeconds));

            var fout = Mathf.Max(0.01f, incomingFadeOutSeconds);
            t = 0f;
            while (t < fout)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = 1f - Mathf.Clamp01(t / fout);
                yield return null;
            }

            cg.alpha = 0f;
            if (_incomingPanelBuiltByScript)
                panelGo.SetActive(false);
            _incomingRoutine = null;
        }

        private IEnumerator OutgoingPingDisplayRoutine()
        {
            var cg = _outgoingCanvasGroup!;
            var panelGo = _outgoingPanelRt!.gameObject;
            panelGo.SetActive(true);
            cg.alpha = 0f;
            var t = 0f;
            var fin = Mathf.Max(0.01f, outgoingFadeInSeconds);
            while (t < fin)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(t / fin);
                yield return null;
            }

            cg.alpha = 1f;
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, outgoingVisibleSeconds));

            var fout = Mathf.Max(0.01f, outgoingFadeOutSeconds);
            t = 0f;
            while (t < fout)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = 1f - Mathf.Clamp01(t / fout);
                yield return null;
            }

            cg.alpha = 0f;
            if (_outgoingPanelBuiltByScript)
                panelGo.SetActive(false);
            _outgoingRoutine = null;
        }
    }
}
