using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TapBrawl.Core.Economy;
using TapBrawl.Models;
using TapBrawl.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>Магазин Adipoint: паки кристаллов + покупка (IAP / dev).</summary>
    public sealed class ShopModalView : MonoBehaviour
    {
        [SerializeField] private BackendConfig? backendConfig;
        [SerializeField] private string title = "Магазин Adipoint";
        [SerializeField] private Text? titleText;
        [SerializeField] private Text? balanceText;
        [SerializeField] private Text? statusText;
        [SerializeField] private Transform? productsContainer;
        [SerializeField] private Button? productButtonPrefab;

        private readonly List<GameObject> _spawnedPacks = new();
        private readonly List<GameObject> _spawnedExchangePacks = new();
        private readonly List<Button> _exchangeButtons = new();
        private readonly List<ExchangePackDto> _exchangePacks = new();
        private IapManager? _iap;
        private bool _loading;
        private bool _exchanging;
        private bool _layoutReady;
        private Transform? _exchangeContainer;
        private static Sprite? _whiteSprite;

        private void Awake()
        {
            backendConfig = BackendConfigLocator.Resolve(backendConfig);
            TryAutoWire();
            EnsureShopLayout();
            ApplyStaticTexts();
        }

        private void OnEnable()
        {
            backendConfig = BackendConfigLocator.Resolve(backendConfig);
            EnsureShopLayout();
            ApplyBalance();
            EnsureIap();
            CurrencyState.BalancesUpdated -= OnBalancesUpdated;
            CurrencyState.BalancesUpdated += OnBalancesUpdated;
            _ = LoadProductsAsync();
        }

        private void OnDisable()
        {
            CurrencyState.BalancesUpdated -= OnBalancesUpdated;
            if (_iap != null)
            {
                _iap.GemsUpdated -= OnGemsUpdated;
                _iap.PurchaseFailed -= OnPurchaseFailed;
            }
        }

        public void TryAutoWire()
        {
            if (titleText == null)
                titleText = transform.Find("Panel/Header/Title Text")?.GetComponent<Text>();
            if (balanceText == null)
                balanceText = transform.Find("Panel/Balance Text")?.GetComponent<Text>();
            if (statusText == null)
                statusText = transform.Find("Panel/Status Text")?.GetComponent<Text>();
            if (productsContainer == null)
            {
                productsContainer = transform.Find("Panel/Products Container");
                if (productsContainer == null)
                    productsContainer = transform.Find("Panel/Products Scroll/Viewport/Products Container");
            }
        }

        private void EnsureShopLayout()
        {
            var panel = transform.Find("Panel");
            if (panel == null)
                return;

            var legacyMessage = panel.Find("Message Text");
            if (legacyMessage != null)
                legacyMessage.gameObject.SetActive(false);

            if (!_layoutReady)
            {
                var oldScroll = panel.Find("Products Scroll");
                if (oldScroll != null)
                    Destroy(oldScroll.gameObject);

                productsContainer = panel.Find("Products Container");
                if (productsContainer == null)
                    productsContainer = CreateProductsContainer(panel);

                _exchangeContainer = panel.Find("Exchange Container");
                if (_exchangeContainer == null)
                    _exchangeContainer = CreateExchangeContainer(panel);

                _layoutReady = true;
            }

            if (balanceText == null)
                balanceText = CreateBalanceText(panel);
            if (statusText == null)
                statusText = CreateStatusText(panel);
        }

        private static Text CreateBalanceText(Transform panel)
        {
            var go = new GameObject("Balance Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(panel, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.08f, 0.84f);
            rt.anchorMax = new Vector2(0.92f, 0.9f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var text = go.GetComponent<Text>();
            ConfigureLegacyText(text, "Adipoint: 0", 30, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.color = UiModalStyle.ProfileAccentTextColor;
            return text;
        }

        private static Text CreateStatusText(Transform panel)
        {
            var go = new GameObject("Status Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(panel, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.08f, 0.1f);
            rt.anchorMax = new Vector2(0.92f, 0.16f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var text = go.GetComponent<Text>();
            ConfigureLegacyText(text, string.Empty, 24, FontStyle.Normal, TextAnchor.MiddleCenter);
            text.color = new Color(0.75f, 0.85f, 1f, 1f);
            return text;
        }

        private static Transform CreateProductsContainer(Transform panel)
        {
            var go = new GameObject("Products Container", typeof(RectTransform), typeof(VerticalLayoutGroup));
            go.transform.SetParent(panel, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.08f, 0.52f);
            rt.anchorMax = new Vector2(0.92f, 0.82f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return go.transform;
        }

        private static Transform CreateExchangeContainer(Transform panel)
        {
            var go = new GameObject("Exchange Container", typeof(RectTransform), typeof(VerticalLayoutGroup));
            go.transform.SetParent(panel, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.08f, 0.2f);
            rt.anchorMax = new Vector2(0.92f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var headerGo = new GameObject("Exchange Header", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            headerGo.transform.SetParent(go.transform, false);
            var header = headerGo.GetComponent<Text>();
            ConfigureLegacyText(header, "Обмен на монеты", 28, FontStyle.Bold, TextAnchor.MiddleLeft);
            header.color = UiModalStyle.ProfileAccentTextColor;
            var headerLe = headerGo.GetComponent<LayoutElement>();
            headerLe.preferredHeight = 36f;
            headerLe.minHeight = 36f;

            return go.transform;
        }

        private void ApplyStaticTexts()
        {
            if (titleText != null)
                titleText.text = title;
            ApplyBalance();
        }

        private void ApplyBalance()
        {
            if (balanceText == null)
                return;
            var gems = AuthContext.Current?.Player.Gems ?? 0;
            balanceText.text = CurrencyDisplay.FormatShopBalance(gems);
            RefreshExchangeButtons();
        }

        private void OnBalancesUpdated(int coins, int gems)
        {
            _ = coins;
            ApplyBalance();
        }

        private void EnsureIap()
        {
            backendConfig = BackendConfigLocator.Resolve(backendConfig);
            if (backendConfig == null)
                return;

            _iap = IapManager.Instance;
            if (_iap == null)
            {
                var go = new GameObject("IapManager");
                _iap = go.AddComponent<IapManager>();
            }

            _iap.Configure(backendConfig);
            _iap.GemsUpdated -= OnGemsUpdated;
            _iap.PurchaseFailed -= OnPurchaseFailed;
            _iap.GemsUpdated += OnGemsUpdated;
            _iap.PurchaseFailed += OnPurchaseFailed;
        }

        private async Task LoadProductsAsync()
        {
            if (_loading)
                return;

            _loading = true;

            if (ShopCatalogCache.HasProducts && ShopCatalogCache.HasExchangePacks)
            {
                RenderFromCache();
                _loading = false;
                return;
            }

            SetStatus("Загрузка товаров…");

            var products = GetFallbackProducts();
            try
            {
                backendConfig = BackendConfigLocator.Resolve(backendConfig);
                if (backendConfig != null)
                {
                    var api = new ApiClient(backendConfig);
                    var result = await api.ShopProductsAsync(CancellationToken.None).ConfigureAwait(true);
                    if (result.Success && result.Data is { Count: > 0 })
                    {
                        products = result.Data;
                        ShopCatalogCache.SetProducts(result.Data);
                    }
                }

                if (IapManager.UseDevPurchases)
                {
                    RebuildProductPacks(products);
                    SetStatus(backendConfig == null
                        ? "DEV: нет BackendConfig"
                        : "DEV: тест без Google Play");
                }
                else
                {
                    _iap?.EnsureInitialized(CollectIds(products));
                    RebuildProductPacks(products);
                    SetStatus(_iap is { IsReady: true } ? string.Empty : "Подключение к магазину…");
                }

                await LoadExchangePacksAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                RebuildProductPacks(products);
                RebuildExchangePacks(GetFallbackExchangePacks());
                SetStatus(ex.Message);
            }
            finally
            {
                _loading = false;
            }
        }

        private void RenderFromCache()
        {
            var products = ShopCatalogCache.Products!;
            if (IapManager.UseDevPurchases)
            {
                RebuildProductPacks(products);
                SetStatus(backendConfig == null ? "DEV: нет BackendConfig" : "DEV: тест без Google Play");
            }
            else
            {
                _iap?.EnsureInitialized(CollectIds(products));
                RebuildProductPacks(products);
                SetStatus(_iap is { IsReady: true } ? string.Empty : "Подключение к магазину…");
            }

            RebuildExchangePacks(ShopCatalogCache.ExchangePacks!);
        }

        private async Task LoadExchangePacksAsync()
        {
            var packs = GetFallbackExchangePacks();
            backendConfig = BackendConfigLocator.Resolve(backendConfig);
            if (backendConfig != null)
            {
                var api = new ApiClient(backendConfig);
                var result = await api.ShopExchangePacksAsync(CancellationToken.None).ConfigureAwait(true);
                if (result.Success && result.Data is { Count: > 0 })
                {
                    packs = result.Data;
                    ShopCatalogCache.SetExchangePacks(result.Data);
                }
            }

            RebuildExchangePacks(packs);
        }

        private static List<ExchangePackDto> GetFallbackExchangePacks()
        {
            var list = new List<ExchangePackDto>();
            foreach (var pack in GemsExchangeBalance.Packs)
            {
                list.Add(new ExchangePackDto
                {
                    PackId = pack.PackId,
                    DisplayName = pack.DisplayName,
                    GemsCost = pack.GemsCost,
                    CoinsReward = pack.CoinsReward,
                    BonusPercent = pack.BonusPercent,
                });
            }

            return list;
        }

        private static List<ShopProductDto> GetFallbackProducts() =>
            new()
            {
                new ShopProductDto { ProductId = "adipoint_100", GemsAmount = 100 },
                new ShopProductDto { ProductId = "adipoint_500", GemsAmount = 500 },
                new ShopProductDto { ProductId = "adipoint_1200", GemsAmount = 1200 },
            };

        private static IEnumerable<string> CollectIds(IReadOnlyList<ShopProductDto> products)
        {
            foreach (var p in products)
            {
                if (!string.IsNullOrWhiteSpace(p.ProductId))
                    yield return p.ProductId;
            }
        }

        private void RebuildProductPacks(IReadOnlyList<ShopProductDto> products)
        {
            foreach (var go in _spawnedPacks)
            {
                if (go != null)
                    Destroy(go);
            }

            _spawnedPacks.Clear();

            if (productsContainer == null)
                return;

            for (var i = 0; i < products.Count; i++)
            {
                var pack = CreatePackCard(products[i], i);
                if (pack != null)
                    _spawnedPacks.Add(pack);
            }
        }

        private GameObject? CreatePackCard(ShopProductDto product, int index)
        {
            if (productsContainer == null)
                return null;

            var sku = product.ProductId;

            if (productButtonPrefab != null)
            {
                var btn = Instantiate(productButtonPrefab, productsContainer);
                var label = btn.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = FormatPackLabel(product);
                btn.onClick.AddListener(() => OnBuyClicked(sku));
                return btn.gameObject;
            }

            var go = new GameObject(
                $"Pack_{product.ProductId}",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            go.transform.SetParent(productsContainer, false);

            var layoutEl = go.GetComponent<LayoutElement>();
            layoutEl.preferredHeight = 112f;
            layoutEl.minHeight = 100f;

            var bg = go.GetComponent<Image>();
            ApplyPanelImage(bg, PackColor(index));

            var btnComp = go.GetComponent<Button>();
            btnComp.targetGraphic = bg;
            var colors = btnComp.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            btnComp.colors = colors;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            StretchFull(labelGo.GetComponent<RectTransform>());

            var amountTmp = CreateTmp(labelGo.transform, $"{product.GemsAmount}", 44, Color.white, TextAlignmentOptions.Left);
            amountTmp.fontStyle = FontStyles.Bold;
            var amountRt = amountTmp.rectTransform;
            amountRt.anchorMin = new Vector2(0.06f, 0.5f);
            amountRt.anchorMax = new Vector2(0.45f, 0.95f);
            amountRt.offsetMin = Vector2.zero;
            amountRt.offsetMax = Vector2.zero;

            var subtitleTmp = CreateTmp(labelGo.transform, "Adipoint", 24, new Color(0.85f, 0.92f, 1f), TextAlignmentOptions.Left);
            var subtitleRt = subtitleTmp.rectTransform;
            subtitleRt.anchorMin = new Vector2(0.06f, 0.08f);
            subtitleRt.anchorMax = new Vector2(0.45f, 0.5f);
            subtitleRt.offsetMin = Vector2.zero;
            subtitleRt.offsetMax = Vector2.zero;

            var buyTmp = CreateTmp(labelGo.transform, "Купить", 32, new Color(1f, 0.92f, 0.55f), TextAlignmentOptions.Right);
            buyTmp.fontStyle = FontStyles.Bold;
            var buyRt = buyTmp.rectTransform;
            buyRt.anchorMin = new Vector2(0.5f, 0.15f);
            buyRt.anchorMax = new Vector2(0.94f, 0.85f);
            buyRt.offsetMin = Vector2.zero;
            buyRt.offsetMax = Vector2.zero;

            btnComp.onClick.AddListener(() => OnBuyClicked(sku));
            return go;
        }

        private static string FormatPackLabel(ShopProductDto product) =>
            $"{product.GemsAmount} Adipoint — Купить";

        private static Color PackColor(int index) => index switch
        {
            0 => new Color(0.15f, 0.38f, 0.72f, 1f),
            1 => new Color(0.18f, 0.52f, 0.58f, 1f),
            _ => new Color(0.42f, 0.28f, 0.72f, 1f),
        };

        private void RebuildExchangePacks(IReadOnlyList<ExchangePackDto> packs)
        {
            foreach (var go in _spawnedExchangePacks)
            {
                if (go != null)
                    Destroy(go);
            }

            _spawnedExchangePacks.Clear();
            _exchangeButtons.Clear();
            _exchangePacks.Clear();

            if (_exchangeContainer == null)
                return;

            _exchangePacks.AddRange(packs);
            for (var i = 0; i < packs.Count; i++)
            {
                var card = CreateExchangeCard(packs[i], i);
                if (card != null)
                    _spawnedExchangePacks.Add(card);
            }

            RefreshExchangeButtons();
        }

        private GameObject? CreateExchangeCard(ExchangePackDto pack, int index)
        {
            if (_exchangeContainer == null)
                return null;

            var go = new GameObject(
                $"Exchange_{pack.PackId}",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            go.transform.SetParent(_exchangeContainer, false);

            var layoutEl = go.GetComponent<LayoutElement>();
            layoutEl.preferredHeight = 96f;
            layoutEl.minHeight = 88f;

            var bg = go.GetComponent<Image>();
            ApplyPanelImage(bg, ExchangePackColor(index));

            var btnComp = go.GetComponent<Button>();
            btnComp.targetGraphic = bg;
            _exchangeButtons.Add(btnComp);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            StretchFull(labelGo.GetComponent<RectTransform>());

            var titleTmp = CreateTmp(
                labelGo.transform,
                pack.DisplayName,
                22,
                new Color(0.85f, 0.92f, 1f),
                TextAlignmentOptions.Left);
            var titleRt = titleTmp.rectTransform;
            titleRt.anchorMin = new Vector2(0.06f, 0.55f);
            titleRt.anchorMax = new Vector2(0.62f, 0.92f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;

            var amountTmp = CreateTmp(
                labelGo.transform,
                CurrencyDisplay.FormatExchangePack(pack.GemsCost, pack.CoinsReward, pack.BonusPercent),
                26,
                Color.white,
                TextAlignmentOptions.Left);
            amountTmp.fontStyle = FontStyles.Bold;
            var amountRt = amountTmp.rectTransform;
            amountRt.anchorMin = new Vector2(0.06f, 0.1f);
            amountRt.anchorMax = new Vector2(0.62f, 0.55f);
            amountRt.offsetMin = Vector2.zero;
            amountRt.offsetMax = Vector2.zero;

            var buyTmp = CreateTmp(labelGo.transform, "Обменять", 28, new Color(1f, 0.92f, 0.55f), TextAlignmentOptions.Right);
            buyTmp.fontStyle = FontStyles.Bold;
            var buyRt = buyTmp.rectTransform;
            buyRt.anchorMin = new Vector2(0.62f, 0.15f);
            buyRt.anchorMax = new Vector2(0.94f, 0.85f);
            buyRt.offsetMin = Vector2.zero;
            buyRt.offsetMax = Vector2.zero;

            var packId = pack.PackId;
            btnComp.onClick.AddListener(() => OnExchangeClicked(packId));
            return go;
        }

        private static Color ExchangePackColor(int index) => index switch
        {
            0 => new Color(0.2f, 0.34f, 0.58f, 1f),
            1 => new Color(0.22f, 0.44f, 0.5f, 1f),
            2 => new Color(0.34f, 0.3f, 0.62f, 1f),
            _ => new Color(0.5f, 0.28f, 0.42f, 1f),
        };

        private void RefreshExchangeButtons()
        {
            var gems = AuthContext.Current?.Player.Gems ?? 0;
            for (var i = 0; i < _exchangeButtons.Count && i < _exchangePacks.Count; i++)
            {
                var btn = _exchangeButtons[i];
                if (btn == null)
                    continue;
                btn.interactable = !_exchanging && gems >= _exchangePacks[i].GemsCost;
            }
        }

        private async void OnExchangeClicked(string packId)
        {
            if (_exchanging)
                return;

            backendConfig = BackendConfigLocator.Resolve(backendConfig);
            if (backendConfig == null)
            {
                SetStatus("Backend Config не назначен.");
                return;
            }

            var session = AuthContext.Current;
            if (session == null)
            {
                SetStatus("Нужен вход.");
                return;
            }

            _exchanging = true;
            RefreshExchangeButtons();
            SetStatus("Обмен…");

            try
            {
                var api = new ApiClient(backendConfig);
                var result = await api.PlayersMeExchangeGemsAsync(session.AccessToken, packId, CancellationToken.None)
                    .ConfigureAwait(true);
                if (!result.Success || result.Data == null)
                {
                    SetStatus($"Ошибка обмена: HTTP {result.StatusCode} {result.ErrorBody}");
                    return;
                }

                CurrencyState.ApplyBalances(result.Data.Coins, result.Data.Gems);
                ApplyBalance();
                SetStatus($"+{result.Data.CoinsGranted:N0} монет");
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message);
            }
            finally
            {
                _exchanging = false;
                RefreshExchangeButtons();
            }
        }

        private void OnBuyClicked(string productId)
        {
            backendConfig = BackendConfigLocator.Resolve(backendConfig);
            if (_iap == null && backendConfig != null)
                EnsureIap();

            if (_iap == null)
            {
                SetStatus("IAP не инициализирован (нет BackendConfig).");
                return;
            }

            SetStatus("Оформление покупки…");
            _iap.BuyProduct(productId);
        }

        private void OnGemsUpdated(int gems)
        {
            var session = AuthContext.Current;
            if (session != null)
                CurrencyState.ApplyBalances(session.Player.Coins, gems);

            ApplyBalance();
            SetStatus("Покупка успешна!");
        }

        private void OnPurchaseFailed(string message) => SetStatus(message);

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        private static void ApplyPanelImage(Image image, Color color)
        {
            image.sprite = GetWhiteSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = true;
        }

        private static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null)
                return _whiteSprite;

            var tex = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
            _whiteSprite.hideFlags = HideFlags.HideAndDontSave;
            return _whiteSprite;
        }

        private static TMP_Text CreateTmp(
            Transform parent,
            string content,
            int fontSize,
            Color color,
            TextAlignmentOptions alignment)
        {
            var go = new GameObject("TMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;

            var font = TMP_Settings.defaultFontAsset;
            if (font == null)
                font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font != null)
                tmp.font = font;

            return tmp;
        }

        private static void ConfigureLegacyText(Text text, string content, int size, FontStyle style, TextAnchor anchor)
        {
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }
}
