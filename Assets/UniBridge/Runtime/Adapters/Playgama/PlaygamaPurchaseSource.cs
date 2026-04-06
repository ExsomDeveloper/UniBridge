#if UNIBRIDGE_PLAYGAMA && UNITY_WEBGL
using System;
using System.Collections.Generic;
using Playgama;
using UnityEngine;

namespace UniBridge
{
    public class PlaygamaPurchaseSource : IPurchaseSource
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterAdapter()
        {
            PurchaseSourceRegistry.Register("UNIBRIDGE_PLAYGAMA", config => new PlaygamaPurchaseSource(config), 50);
            Debug.Log("[UniBridgePurchases] Playgama purchase adapter registered");
        }

        public bool IsInitialized { get; private set; }
        public bool IsSupported => Bridge.payments.isSupported;

        public event Action<PurchaseResult> OnPurchaseSuccess;
        public event Action<PurchaseResult> OnPurchaseFailed;

        private UniBridgePurchasesConfig _config;

        // productId → purchase token (non-consumables)
        private readonly Dictionary<string, string> _ownershipCache = new Dictionary<string, string>();

        public PlaygamaPurchaseSource(UniBridgePurchasesConfig config)
        {
            _config = config;
        }

        public void Initialize(UniBridgePurchasesConfig config, Action onSuccess, Action onFailed)
        {
            _config = config;
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
            VLog($"Initialize called, paymentsSupported={Bridge.payments.isSupported}");
#endif
            if (!Bridge.payments.isSupported)
            {
                Debug.Log($"[{nameof(PlaygamaPurchaseSource)}]: Payments not supported on this platform");
                IsInitialized = true;
                onSuccess?.Invoke();
                return;
            }

            RefreshPurchases(success =>
            {
                IsInitialized = true;

                if (!success)
                    Debug.LogWarning($"[{nameof(PlaygamaPurchaseSource)}]: Init succeeded but purchase refresh failed");
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
                VLog($"Init refresh: {(success ? "ok" : "failed")}, owned={_ownershipCache.Count}");
#endif
                onSuccess?.Invoke();
            });
        }

        public void FetchProducts(Action<bool, IReadOnlyList<ProductData>> onComplete)
        {
            if (!Bridge.payments.isSupported)
            {
                onComplete?.Invoke(false, null);
                return;
            }
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
            VLog("FetchProducts: requesting catalog");
#endif
            Bridge.payments.GetCatalog((success, catalog) =>
            {
                if (!success)
                {
                    onComplete?.Invoke(false, null);
                    return;
                }

                var list = new List<ProductData>();
                if (catalog != null)
                {
                    foreach (var item in catalog)
                    {
                        var playgamaId = item.TryGetValue("id", out var id) ? id : "";
                        var projectId  = ResolveProjectId(playgamaId);
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
                        VLog($"  FetchProducts: {playgamaId} → {projectId}");
#endif
                        list.Add(new ProductData
                        {
                            ProductId            = projectId,
                            LocalizedTitle       = item.TryGetValue("title", out var title)     ? title : "",
                            LocalizedDescription = item.TryGetValue("description", out var desc) ? desc  : "",
                            LocalizedPriceString = item.TryGetValue("price", out var price)     ? price : "",
                            LocalizedPrice       = decimal.TryParse(
                                item.TryGetValue("priceValue", out var pv) ? pv : "0",
                                out var d) ? d : 0m,
                            IsoCurrencyCode      = item.TryGetValue("priceCurrencyCode", out var cc) ? cc  : "",
                            ImageUrl             = item.TryGetValue("imageURI", out var img)    ? img   : "",
                            IsAvailable          = true
                        });
                    }
                }

#if UNIBRIDGEPURCHASES_VERBOSE_LOG
                VLog($"FetchProducts: complete, {list.Count} products");
#endif
                onComplete?.Invoke(true, list);
            });
        }

        public void Purchase(string projectId, Action<PurchaseResult> onComplete)
        {
            string playgamaId = ResolvePlaygamaId(projectId);
            bool isConsumable = IsConsumable(projectId);
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
            VLog($"Purchase: {projectId} → playgamaId={playgamaId}, isConsumable={isConsumable}");
#endif

            Bridge.payments.Purchase(playgamaId, (success, purchase) =>
            {
                if (!success)
                {
                    var fail = PurchaseResult.FromFailed(projectId, PurchaseStatus.Failed);
                    onComplete?.Invoke(fail);
                    OnPurchaseFailed?.Invoke(fail);
                    return;
                }

                string token = purchase != null && purchase.TryGetValue("id", out var t) ? t : "";

                if (isConsumable)
                {
                    Bridge.payments.ConsumePurchase(playgamaId, (consumeSuccess, _) =>
                    {
                        var result = PurchaseResult.FromSuccess(projectId, token);
                        onComplete?.Invoke(result);
                        OnPurchaseSuccess?.Invoke(result);
                    });
                }
                else
                {
                    _ownershipCache[projectId] = token;
                    var result = PurchaseResult.FromSuccess(projectId, token);
                    onComplete?.Invoke(result);
                    OnPurchaseSuccess?.Invoke(result);
                }
            });
        }

        public bool IsPurchased(string productId) => _ownershipCache.ContainsKey(productId);

        public void RefreshPurchases(Action<bool> onComplete)
        {
            if (!Bridge.payments.isSupported)
            {
                onComplete?.Invoke(true);
                return;
            }
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
            VLog("RefreshPurchases: start");
#endif
            Bridge.payments.GetPurchases((success, purchases) =>
            {
                if (!success)
                {
                    onComplete?.Invoke(false);
                    return;
                }

                _ownershipCache.Clear();
                if (purchases != null)
                {
                    foreach (var p in purchases)
                    {
                        string playgamaId = p.TryGetValue("id", out var id) ? id : "";
                        string token      = p.TryGetValue("id", out var t) ? t : "";
                        if (!string.IsNullOrEmpty(playgamaId))
                        {
                            string projectId = ResolveProjectId(playgamaId);
                            _ownershipCache[projectId] = token;
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
                            VLog($"  RefreshPurchases: {playgamaId} → {projectId}");
#endif
                        }
                    }
                }
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
                VLog($"RefreshPurchases: complete, owned={_ownershipCache.Count}");
#endif
                onComplete?.Invoke(true);
            });
        }

        public void RestorePurchases(Action<bool, IReadOnlyList<string>> onComplete)
        {
            var before = new HashSet<string>(_ownershipCache.Keys);
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
            VLog($"RestorePurchases: start, owned before={before.Count}");
#endif
            // Playgama: GetPurchases is the restore equivalent.
            RefreshPurchases(success =>
            {
                var restored = new List<string>();
                foreach (var key in _ownershipCache.Keys)
                    if (!before.Contains(key))
                        restored.Add(key);
                foreach (var kv in _ownershipCache)
                    if (!IsConsumable(kv.Key))
                        OnPurchaseSuccess?.Invoke(PurchaseResult.FromRestored(kv.Key, kv.Value));
#if UNIBRIDGEPURCHASES_VERBOSE_LOG
                VLog($"RestorePurchases: complete, {restored.Count} newly restored");
#endif
                onComplete?.Invoke(success, restored);
            });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private string ResolvePlaygamaId(string projectId)
        {
            if (_config?.Products != null)
            {
                foreach (var def in _config.Products)
                {
                    if (def.ProjectId == projectId)
                        return def.PlaygamaProductId;
                }
            }
            return projectId;
        }

        private string ResolveProjectId(string playgamaId)
        {
            if (_config?.Products != null)
            {
                foreach (var def in _config.Products)
                {
                    if (def.PlaygamaProductId == playgamaId)
                        return def.ProjectId;
                }
            }
            return playgamaId;
        }

        private bool IsConsumable(string projectId)
        {
            if (_config?.Products != null)
            {
                foreach (var def in _config.Products)
                {
                    if (def.ProjectId == projectId)
                        return def.Type == ProductType.Consumable;
                }
            }
            return true;
        }

#if UNIBRIDGEPURCHASES_VERBOSE_LOG
        private static void VLog(string msg) => Debug.Log($"[RAT] [{nameof(PlaygamaPurchaseSource)}] {msg}");
#endif
    }
}
#endif
