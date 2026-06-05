using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TapBrawl.Models;
using UnityEngine;
#if UNITY_PURCHASING
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

namespace TapBrawl.Network
{
    /// <summary>Unity IAP + серверная верификация Google Play.</summary>
    public sealed class IapManager : MonoBehaviour
#if UNITY_PURCHASING
        , IDetailedStoreListener
#endif
    {
        public static IapManager? Instance { get; private set; }

        // TODO(после лицензии Google Play): удалить UseDevPurchases и весь блок DevPurchaseAsync —
        // покупки только через реальный Google Play Billing + серверная валидация без bypass.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static bool UseDevPurchases => true;
#else
        public static bool UseDevPurchases => false;
#endif

        [SerializeField] private BackendConfig? backendConfig;
        [SerializeField] private string[] defaultProductIds =
        {
            "adipoint_100",
            "adipoint_500",
            "adipoint_1200",
        };

        public event Action<int>? GemsUpdated;
        public event Action<string>? PurchaseFailed;

        public bool IsReady { get; private set; }

#if UNITY_PURCHASING
        private IStoreController? _store;
        private IExtensionProvider? _extensions;
        private readonly HashSet<string> _productIds = new(StringComparer.Ordinal);
#endif
        private ApiClient? _api;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Configure(BackendConfig config) => backendConfig = config;

        public void BuyProduct(string productId)
        {
            if (UseDevPurchases)
            {
                _ = DevPurchaseAsync(productId);
                return;
            }

#if UNITY_PURCHASING
            if (!IsReady || _store == null)
            {
                PurchaseFailed?.Invoke("Магазин не готов.");
                return;
            }

            var product = _store.products.WithID(productId);
            if (product == null || !product.availableToPurchase)
            {
                PurchaseFailed?.Invoke("Товар недоступен.");
                return;
            }

            _store.InitiatePurchase(product);
#else
            PurchaseFailed?.Invoke("Unity IAP не установлен.");
#endif
        }

#if UNITY_PURCHASING
        public void EnsureInitialized(IEnumerable<string>? productIds = null)
        {
            if (UseDevPurchases || _store != null || backendConfig == null)
                return;

            MergeProductIds(productIds);
            MergeProductIds(defaultProductIds);

            if (_productIds.Count == 0)
                return;

            var module = StandardPurchasingModule.Instance();
            var builder = ConfigurationBuilder.Instance(module);
            foreach (var id in _productIds)
                builder.AddProduct(id, ProductType.Consumable);

            UnityPurchasing.Initialize(this, builder);
        }

        private void MergeProductIds(IEnumerable<string>? productIds)
        {
            if (productIds == null)
                return;

            foreach (var id in productIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    _productIds.Add(id.Trim());
            }
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _store = controller;
            _extensions = extensions;
            IsReady = true;
        }

        public void OnInitializeFailed(InitializationFailureReason error) =>
            OnInitializeFailed(error, null);

        public void OnInitializeFailed(InitializationFailureReason error, string? message)
        {
            IsReady = false;
            Debug.LogWarning($"[IAP] Init failed: {error} {message}");
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            _ = VerifyIapPurchaseAsync(args.purchasedProduct);
            return PurchaseProcessingResult.Pending;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            var id = product?.definition?.id ?? "?";
            PurchaseFailed?.Invoke($"Покупка {id}: {failureReason}");
        }

        private async Task VerifyIapPurchaseAsync(Product product)
        {
            try
            {
                if (!TryParsePurchaseToken(product.receipt, product.transactionID, out var purchaseToken))
                    throw new InvalidOperationException("Не удалось прочитать purchaseToken.");

                await VerifyOnServerAsync(product.definition.id, purchaseToken, () =>
                {
                    _store?.ConfirmPendingPurchase(product);
                }).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[IAP] Verify: " + ex.Message);
                PurchaseFailed?.Invoke(ex.Message);
            }
        }
#else
        public void EnsureInitialized(IEnumerable<string>? productIds = null) { }
#endif

        // TODO(после лицензии Google Play): удалить DevPurchaseAsync — тестовый обход IAP.
        private async Task DevPurchaseAsync(string productId)
        {
            try
            {
                var token = $"dev-{Guid.NewGuid():N}";
                await VerifyOnServerAsync(productId, token, onSuccess: null).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[IAP] Dev purchase: " + ex.Message);
                PurchaseFailed?.Invoke(ex.Message);
            }
        }

        private async Task VerifyOnServerAsync(string productId, string purchaseToken, Action? onSuccess)
        {
            if (backendConfig == null)
                throw new InvalidOperationException("BackendConfig не назначен.");

            _api ??= new ApiClient(backendConfig);
            var session = AuthContext.Current;
            if (session == null || string.IsNullOrEmpty(session.AccessToken))
                throw new InvalidOperationException("Нужна авторизация.");

            var body = new VerifyPurchaseRequestDto
            {
                ProductId = productId,
                PurchaseToken = purchaseToken,
            };

            var result = await _api.PurchasesGoogleVerifyAsync(session.AccessToken, body, CancellationToken.None)
                .ConfigureAwait(true);

            if (!result.Success || result.Data == null)
                throw new InvalidOperationException($"Верификация: HTTP {result.StatusCode} {result.ErrorBody}");

            session.Player.Gems = result.Data.Gems;
            AuthContext.Current = session;
            AuthStorage.Save(session);
            onSuccess?.Invoke();
            GemsUpdated?.Invoke(result.Data.Gems);
        }

        // TODO(после лицензии Google Play): оставить только парсинг Google Play receipt (убрать ветку fake/dev).
        private static bool TryParsePurchaseToken(string receipt, string transactionId, out string purchaseToken)
        {
            purchaseToken = string.Empty;
            if (string.IsNullOrWhiteSpace(receipt))
                return false;

            try
            {
                var root = JObject.Parse(receipt);
                var store = root["Store"]?.ToString();

                if (UseDevPurchases && string.Equals(store, "fake", StringComparison.OrdinalIgnoreCase))
                {
                    var tx = root["TransactionID"]?.ToString();
                    if (!string.IsNullOrEmpty(tx))
                    {
                        purchaseToken = $"fake-{tx}";
                        return true;
                    }
                }

                var payloadStr = root["Payload"]?.ToString();
                if (string.IsNullOrEmpty(payloadStr))
                    return false;

                var payload = JObject.Parse(payloadStr);
                var jsonStr = payload["json"]?.ToString();
                if (string.IsNullOrEmpty(jsonStr))
                    return false;

                var purchase = JObject.Parse(jsonStr);
                purchaseToken = purchase["purchaseToken"]?.ToString() ?? string.Empty;
                return !string.IsNullOrEmpty(purchaseToken);
            }
            catch
            {
                if (UseDevPurchases && !string.IsNullOrEmpty(transactionId))
                {
                    purchaseToken = $"fallback-{transactionId}";
                    return true;
                }

                return false;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
