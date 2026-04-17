#if UNIBRIDGEPURCHASES_IAP
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace UniBridge
{
    public class UnityIAPPurchaseSource : IPurchaseSource, IDetailedStoreListener
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void RegisterAdapter()
        {
#if (UNITY_ANDROID && UNIBRIDGE_STORE_GOOGLEPLAY) || (UNITY_IOS && UNIBRIDGE_STORE_APPSTORE)
            PurchaseSourceRegistry.Register("UNIBRIDGEPURCHASES_IAP", config => new UnityIAPPurchaseSource(), 100);
            Debug.Log("[UniBridgePurchases] UnityIAP adapter registered");
#endif
        }

        public bool IsInitialized { get; private set; }
        public bool IsSupported => true;

        public event Action<PurchaseResult> OnPurchaseSuccess;
        public event Action<PurchaseResult> OnPurchaseFailed;

        private IStoreController _controller;
        private IExtensionProvider _extensions;
        private UniBridgePurchasesConfig _config;
        private Action _initSuccess;
        private Action _initFailed;
        private Action<PurchaseResult> _pendingCallback;

        // productId → owned (true for non-consumables with receipt)
        private readonly Dictionary<string, bool> _ownershipCache = new Dictionary<string, bool>();

        public void Initialize(UniBridgePurchasesConfig config, Action onSuccess, Action onFailed)
        {
            _config      = config;
            _initSuccess = onSuccess;
            _initFailed  = onFailed;

            var builder = ConfigurationBuilder.Instance();

            foreach (var def in config.Products)
            {
                var uType = def.Type == ProductType.Consumable
                    ? UnityEngine.Purchasing.ProductType.Consumable
                    : UnityEngine.Purchasing.ProductType.NonConsumable;

                builder.AddProduct(def.ProductId, uType);
            }

            UnityPurchasing.Initialize(this, builder);
        }

        // ── IDetailedStoreListener ────────────────────────────────────────────

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _controller  = controller;
            _extensions  = extensions;

            RebuildOwnershipCache();

            IsInitialized = true;
            Debug.Log($"[{nameof(UnityIAPPurchaseSource)}]: Initialized");
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
            VLog($"OnInitialized: {_controller.products.all.Length} products, {_ownershipCache.Count} owned");
#endif
            _initSuccess?.Invoke();
            _initSuccess = null;
            _initFailed  = null;
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogError($"[{nameof(UnityIAPPurchaseSource)}]: Init failed: {error}");
            _initFailed?.Invoke();
            _initSuccess = null;
            _initFailed  = null;
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"[{nameof(UnityIAPPurchaseSource)}]: Init failed: {error} — {message}");
            _initFailed?.Invoke();
            _initSuccess = null;
            _initFailed  = null;
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            var storeId   = args.purchasedProduct.definition.id;
            var txId      = args.purchasedProduct.transactionID;
            var def       = _config?.FindByStoreProductId(storeId);
            var projectId = def?.ProjectId ?? storeId;
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
            VLog($"ProcessPurchase: {storeId} → {projectId}, txId={txId}");
#endif
            if (args.purchasedProduct.definition.type == UnityEngine.Purchasing.ProductType.NonConsumable)
                _ownershipCache[projectId] = true;

            var result = PurchaseResult.FromSuccess(projectId, txId);

            var cb = _pendingCallback;
            _pendingCallback = null;
            cb?.Invoke(result);
            OnPurchaseSuccess?.Invoke(result);

            return PurchaseProcessingResult.Complete;
        }

        void IDetailedStoreListener.OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            var storeId   = product.definition.id;
            var def       = _config?.FindByStoreProductId(storeId);
            var projectId = def?.ProjectId ?? storeId;

            var status = failureDescription.reason switch
            {
                PurchaseFailureReason.UserCancelled      => PurchaseStatus.Cancelled,
                PurchaseFailureReason.DuplicateTransaction => PurchaseStatus.AlreadyOwned,
                _                                        => PurchaseStatus.Failed
            };

            var result = PurchaseResult.FromFailed(projectId, status, failureDescription.reason.ToString());

            var cb = _pendingCallback;
            _pendingCallback = null;
            cb?.Invoke(result);
            OnPurchaseFailed?.Invoke(result);
        }

        void IStoreListener.OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            // Handled by the overload above (IDetailedStoreListener).
        }

        // ── IPurchaseSource ───────────────────────────────────────────────────

        public void FetchProducts(Action<bool, IReadOnlyList<ProductData>> onComplete)
        {
            if (_controller == null)
            {
                onComplete?.Invoke(false, null);
                return;
            }
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
            VLog($"FetchProducts: {_controller.products.all.Length} products available");
#endif
            var list = new List<ProductData>();

            foreach (var product in _controller.products.all)
            {
                var def       = _config?.FindByStoreProductId(product.definition.id);
                var projectId = def?.ProjectId ?? product.definition.id;
                list.Add(new ProductData
                {
                    ProductId            = projectId,
                    Type                 = product.definition.type == UnityEngine.Purchasing.ProductType.Consumable
                        ? ProductType.Consumable
                        : ProductType.NonConsumable,
                    LocalizedTitle       = product.metadata.localizedTitle,
                    LocalizedDescription = product.metadata.localizedDescription,
                    LocalizedPriceString = product.metadata.localizedPriceString,
                    LocalizedPrice       = product.metadata.localizedPrice,
                    IsoCurrencyCode      = product.metadata.isoCurrencyCode,
                    IsAvailable          = product.availableToPurchase
                });
            }

            onComplete?.Invoke(true, list);
        }

        public void Purchase(string projectId, Action<PurchaseResult> onComplete)
        {
            if (_controller == null)
            {
                onComplete?.Invoke(PurchaseResult.FromFailed(projectId, PurchaseStatus.NotInitialized));
                return;
            }

            if (_pendingCallback != null)
            {
                onComplete?.Invoke(PurchaseResult.FromFailed(projectId, PurchaseStatus.Failed, "Purchase already in progress"));
                return;
            }

            var def     = _config?.FindByProjectId(projectId);
            var storeId = def?.ProductId ?? projectId;
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
            VLog($"Purchase: {projectId} → storeId={storeId}");
#endif
            _pendingCallback = onComplete;
            _controller.InitiatePurchase(storeId);
        }

        public bool IsPurchased(string productId)
        {
            return _ownershipCache.TryGetValue(productId, out var owned) && owned;
        }

        public void RefreshPurchases(Action<bool> onComplete)
        {
            RebuildOwnershipCache();
            onComplete?.Invoke(true);
        }

        public void RestorePurchases(Action<bool, IReadOnlyList<string>> onComplete)
        {
#if UNITY_IOS
            var apple = _extensions?.GetExtension<IAppleExtensions>();
            if (apple != null)
            {
                var before = new HashSet<string>();
                foreach (var kv in _ownershipCache)
                    if (kv.Value) before.Add(kv.Key);

                apple.RestoreTransactions(result =>
                {
                    RebuildOwnershipCache();
                    var restored = new List<string>();
                    foreach (var kv in _ownershipCache)
                    {
                        if (kv.Value && !before.Contains(kv.Key))
                        {
                            restored.Add(kv.Key);
                            OnPurchaseSuccess?.Invoke(PurchaseResult.FromRestored(kv.Key));
                        }
                    }
                    // Already-owned products: ProcessPurchase already fired Success for them.
                    onComplete?.Invoke(result, restored);
                });
            }
            else
            {
                RebuildOwnershipCache();
                onComplete?.Invoke(false, Array.Empty<string>());
            }
#else
            // On Android, purchases are restored automatically during Initialize.
            RebuildOwnershipCache();
            foreach (var kv in _ownershipCache)
                if (kv.Value)
                    OnPurchaseSuccess?.Invoke(PurchaseResult.FromRestored(kv.Key));
            onComplete?.Invoke(true, Array.Empty<string>());
#endif
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void RebuildOwnershipCache()
        {
            _ownershipCache.Clear();
            if (_controller == null) return;

            foreach (var product in _controller.products.all)
            {
                if (product.definition.type == UnityEngine.Purchasing.ProductType.NonConsumable)
                {
                    var def       = _config?.FindByStoreProductId(product.definition.id);
                    var projectId = def?.ProjectId ?? product.definition.id;
                    _ownershipCache[projectId] = product.hasReceipt;
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
                    VLog($"  RebuildCache: {projectId} hasReceipt={product.hasReceipt}");
#endif
                }
            }
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
            VLog($"RebuildOwnershipCache: {_ownershipCache.Count} owned");
#endif
        }

#if UNIBRIDGEPURCHASES_VERBOSE_LOG
        private static void VLog(string msg) => Debug.Log($"[RAT] [{nameof(UnityIAPPurchaseSource)}] {msg}");
#endif
    }
}
#endif
