#if UNIBRIDGEPURCHASES_RUSTORE
using System;
using System.Collections.Generic;
using UnityEngine;
using RuStore.PayClient;

namespace UniBridge
{
    public class RuStorePurchaseSource : IPurchaseSource
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void RegisterAdapter()
        {
#if UNITY_ANDROID && UNIBRIDGE_STORE_RUSTORE && !UNITY_EDITOR
            PurchaseSourceRegistry.Register("UNIBRIDGEPURCHASES_RUSTORE", config => new RuStorePurchaseSource(config.RuStoreSettings), 100);
            Debug.Log("[UniBridgePurchases] RuStore adapter registered");
#endif
        }

        public bool IsInitialized { get; private set; }
        public bool IsSupported => true;

        public event Action<PurchaseResult> OnPurchaseSuccess;
        public event Action<PurchaseResult> OnPurchaseFailed;

        private UniBridgePurchasesConfig _config;

        // productId → true for confirmed non-consumable purchases
        private readonly Dictionary<string, bool> _ownershipCache = new Dictionary<string, bool>();

        public RuStorePurchaseSource(RuStoreSettings settings)
        {
            // Pay SDK initializes automatically via AndroidManifest meta-data.
            // ConsoleApplicationId and DeeplinkScheme are read from res/values set by
            // RuStoreAndroidConfigurator; no explicit C# Init() call is needed.
            _ = settings;
        }

        public void Initialize(UniBridgePurchasesConfig config, Action onSuccess, Action onFailed)
        {
            _config = config;
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
            VLog("Initialize called");
#endif
            // Populate ownership cache; init is considered successful regardless of refresh outcome
            RefreshPurchases(refreshSuccess =>
            {
                IsInitialized = true;

                if (!refreshSuccess)
                    Debug.LogWarning($"[{nameof(RuStorePurchaseSource)}]: Init succeeded but purchase refresh failed");
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
                VLog($"Init refresh: {(refreshSuccess ? "ok" : "failed")}, owned={_ownershipCache.Count}");
#endif
                onSuccess?.Invoke();
            });
        }

        public void FetchProducts(Action<bool, IReadOnlyList<ProductData>> onComplete)
        {
            var ids = new ProductId[_config.Products.Count];
            for (int i = 0; i < _config.Products.Count; i++)
                ids[i] = new ProductId(_config.Products[i].ProductId);
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
            VLog($"FetchProducts: requesting {ids.Length} products");
#endif
            RuStorePayClient.Instance.GetProducts(
                productsId: ids,
                onFailure: error =>
                {
                    Debug.LogError($"[{nameof(RuStorePurchaseSource)}]: GetProducts failed — {error}");
                    onComplete?.Invoke(false, null);
                },
                onSuccess: products =>
                {
                    var list = new List<ProductData>();
                    foreach (var p in products)
                    {
                        var storeId   = p.productId.value;
                        var def       = _config.FindByStoreProductId(storeId);
                        var projectId = def?.ProjectId ?? storeId;
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
                        VLog($"  product: {storeId} → {projectId} | type={p.type} | price={p.amountLabel?.value} | title={p.title?.value}");
#endif
                        list.Add(new ProductData
                        {
                            ProductId            = projectId,
                            Type                 = p.type.ToString() == "NON_CONSUMABLE"
                                ? UniBridge.ProductType.NonConsumable
                                : UniBridge.ProductType.Consumable,
                            LocalizedTitle       = p.title?.value,
                            LocalizedDescription = p.description?.value,
                            LocalizedPriceString = FormatAmountLabel(p.amountLabel?.value),
                            // Price is in minimum currency units (kopecks for RUB)
                            LocalizedPrice       = p.price != null ? p.price.value / 100m : 0m,
                            IsoCurrencyCode      = p.currency?.value,
                            ImageUrl             = p.imageUrl?.value,
                            IsAvailable          = true
                        });
                    }
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
                    VLog($"FetchProducts: complete, {list.Count} products");
#endif
                    onComplete?.Invoke(true, list);
                });
        }

        public void Purchase(string projectId, Action<PurchaseResult> onComplete)
        {
            var def     = _config.FindByProjectId(projectId);
            var storeId = def?.ProductId ?? projectId;
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
            VLog($"Purchase: {projectId} → storeId={storeId}");
#endif
            var parameters = new ProductPurchaseParams(
                productId:        new ProductId(storeId),
                quantity:         new Quantity(1),
                orderId:          null,
                developerPayload: null,
                appUserId:        null,
                appUserEmail:     null
            );

            RuStorePayClient.Instance.Purchase(
                parameters:            parameters,
                preferredPurchaseType: PreferredPurchaseType.TWO_STEP,
                sdkTheme:              SdkTheme.DARK,
                onFailure: error =>
                {
                    var status = error is RuStorePaymentException.ProductPurchaseCancelled
                        ? PurchaseStatus.Cancelled
                        : PurchaseStatus.Failed;
                    var result = PurchaseResult.FromFailed(projectId, status, error?.ToString());
                    onComplete?.Invoke(result);
                    OnPurchaseFailed?.Invoke(result);
                },
                onSuccess: purchase =>
                {
                    string purchaseId = purchase.purchaseId.value;
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
                    VLog($"Purchase: SDK returned purchaseId={purchaseId}");
#endif
                    ConfirmAndNotify(projectId, purchaseId, onComplete);
                });
        }

        private void ConfirmAndNotify(string projectId, string purchaseId, Action<PurchaseResult> onComplete)
        {
            var def            = _config.FindByProjectId(projectId);
            bool isNonConsumable = def?.Type == UniBridge.ProductType.NonConsumable;
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
            VLog($"Confirm: purchaseId={purchaseId}, isNonConsumable={isNonConsumable}");
#endif
            RuStorePayClient.Instance.ConfirmTwoStepPurchase(
                purchaseId:       new PurchaseId(purchaseId),
                developerPayload: null,
                onFailure: error =>
                {
                    // Funds were taken but confirm failed — report PendingConfirmation
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
                    VLog($"Confirm failed (PendingConfirmation): {projectId}, error={error}");
#endif
                    var result = new PurchaseResult
                    {
                        Status        = PurchaseStatus.PendingConfirmation,
                        ProductId     = projectId,
                        TransactionId = purchaseId,
                        ErrorMessage  = error?.ToString()
                    };
                    onComplete?.Invoke(result);
                    OnPurchaseSuccess?.Invoke(result);
                },
                onSuccess: () =>
                {
                    if (isNonConsumable)
                        _ownershipCache[projectId] = true;
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
                    VLog($"Confirm success: {projectId}");
#endif
                    var result = PurchaseResult.FromSuccess(projectId, purchaseId);
                    onComplete?.Invoke(result);
                    OnPurchaseSuccess?.Invoke(result);
                });
        }

        public bool IsPurchased(string productId)
        {
            var owned = _ownershipCache.TryGetValue(productId, out var v) && v;
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
            VLog($"IsPurchased: {productId} → {owned}");
#endif
            return owned;
        }

        public void RefreshPurchases(Action<bool> onComplete)
        {
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
            VLog("RefreshPurchases: start");
#endif
            RuStorePayClient.Instance.GetPurchases(
                onFailure: error =>
                {
                    Debug.LogError($"[{nameof(RuStorePurchaseSource)}]: GetPurchases failed — {error}");
                    onComplete?.Invoke(false);
                },
                onSuccess: purchases =>
                {
                    _ownershipCache.Clear();
                    var paidPurchases = new List<(string purchaseId, string projectId)>();

                    foreach (var purchase in purchases)
                    {
                        if (purchase is ProductPurchase p)
                        {
                            var storeId   = p.productId.value;
                            var def       = _config.FindByStoreProductId(storeId);
                            var projectId = def?.ProjectId ?? storeId;

                            if (p.status == ProductPurchaseStatus.CONFIRMED)
                            {
                                _ownershipCache[projectId] = true;
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
                                VLog($"  RefreshPurchases: CONFIRMED {projectId}");
#endif
                            }
                            else if (p.status == ProductPurchaseStatus.PAID)
                            {
                                // Crash recovery: confirm purchases that were paid but not confirmed
                                paidPurchases.Add((p.purchaseId.value, projectId));
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
                                VLog($"  RefreshPurchases: PAID→crash recovery {projectId}");
#endif
                            }
                        }
                    }
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
                    VLog($"RefreshPurchases: owned={_ownershipCache.Count}, pendingConfirm={paidPurchases.Count}");
#endif
                    if (paidPurchases.Count == 0)
                    {
                        onComplete?.Invoke(true);
                        return;
                    }

                    int remaining = paidPurchases.Count;
                    foreach (var (purchaseId, projectId) in paidPurchases)
                    {
                        RuStorePayClient.Instance.ConfirmTwoStepPurchase(
                            purchaseId:       new PurchaseId(purchaseId),
                            developerPayload: null,
                            onFailure: _ =>
                            {
                                remaining--;
                                if (remaining <= 0) onComplete?.Invoke(true);
                            },
                            onSuccess: () =>
                            {
                                _ownershipCache[projectId] = true; // crash recovery: update cache immediately
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
                                VLog($"  CrashRecovery confirm success: {projectId}");
#endif
                                remaining--;
                                if (remaining <= 0) onComplete?.Invoke(true);
                            });
                    }
                });
        }

        public void RestorePurchases(Action<bool, IReadOnlyList<string>> onComplete)
        {
            var before = new HashSet<string>();
            foreach (var kv in _ownershipCache)
                if (kv.Value) before.Add(kv.Key);
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
            VLog($"RestorePurchases: start, owned before={before.Count}");
#endif
            // RuStore does not have a separate restore flow; GetPurchases is equivalent.
            RefreshPurchases(success =>
            {
                var restored = new List<string>();
                foreach (var kv in _ownershipCache)
                    if (kv.Value && !before.Contains(kv.Key))
                        restored.Add(kv.Key);
                foreach (var kv in _ownershipCache)
                    if (kv.Value)
                        OnPurchaseSuccess?.Invoke(PurchaseResult.FromRestored(kv.Key));
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
                VLog($"RestorePurchases: complete, {restored.Count} newly restored");
#endif
                onComplete?.Invoke(success, restored);
            });
        }

#if UNIBRIDGEPURCHASES_VERBOSE_LOG
        private static void VLog(string msg) => Debug.Log($"[{nameof(RuStorePurchaseSource)}] {msg}");
#endif

        private static string FormatAmountLabel(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            var s = raw.TrimEnd('.');
            s = System.Text.RegularExpressions.Regex.Replace(s, @"[,.]00(?=\s|$)", "");
            return s.Trim();
        }
    }
}
#endif
