using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniBridge
{
    public static class UniBridgePurchases
    {
        public static bool IsInitialized { get; private set; }
        public static bool IsSupported => _source?.IsSupported ?? false;
        public static string AdapterName => _source?.GetType().Name ?? "None";

        /// <summary>
        /// Cached product list from the last successful FetchProducts call.
        /// Null until at least one successful fetch.
        /// </summary>
        public static IReadOnlyList<ProductData> Products { get; private set; }

        /// <summary>
        /// True if product catalog has been successfully fetched at least once.
        /// </summary>
        public static bool AreProductsFetched => Products != null;

        public static event Action OnInitSuccess;
        public static event Action OnInitFailed;
        public static event Action OnProductsFetched;
        public static event Action<PurchaseResult> OnPurchaseSuccess;
        public static event Action<PurchaseResult> OnPurchaseFailed;
        public static event Action<string> OnRestoreSuccess;

        private static IPurchaseSource _source;
        private static UniBridgePurchasesConfig _config;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (IsInitialized) return;
            _source = null;
            Products = null;

            if (_config == null)
                _config = LoadConfig();

            if (_config != null && _config.AutoInitialize)
                SetupPurchases();
        }

        public static void Initialize()
        {
            if (_config == null)
                _config = LoadConfig();

            if (_config == null)
            {
                Debug.LogError($"[{nameof(UniBridgePurchases)}]: UniBridgePurchasesConfig not found! Create one via Assets > Create > UniBridge > Purchases Configuration");
                return;
            }

            SetupPurchases();
        }

        private static void SetupPurchases()
        {
            if (IsInitialized)
                return;

            var builder = new PurchaseSourceBuilder();
            _source = builder.Build(_config);

            if (_source == null)
            {
                Debug.Log($"[{nameof(UniBridgePurchases)}]: Purchases system disabled.");
                return;
            }

            _source.OnPurchaseSuccess += result => OnPurchaseSuccess?.Invoke(result);
            _source.OnPurchaseFailed  += result => OnPurchaseFailed?.Invoke(result);

            _source.Initialize(_config,
                () =>
                {
                    IsInitialized = true;
                    Debug.Log($"[{nameof(UniBridgePurchases)}]: Initialized with {_source.GetType().Name}");
                    _source.FetchProducts((success, products) =>
                    {
                        if (success && products != null)
                        {
                            Products = products;
                            OnProductsFetched?.Invoke();
                        }
                        OnInitSuccess?.Invoke();
                    });
                },
                () =>
                {
                    Debug.LogError($"[{nameof(UniBridgePurchases)}]: Initialization failed");
                    OnInitFailed?.Invoke();
                });
        }

        public static void Buy(string productId, Action<PurchaseResult> onComplete = null)
        {
            if (!EnsureInitialized())
            {
                onComplete?.Invoke(PurchaseResult.FromFailed(productId, PurchaseStatus.NotInitialized));
                return;
            }

            if (!IsSupported)
            {
                onComplete?.Invoke(PurchaseResult.FromFailed(productId, PurchaseStatus.NotSupported));
                return;
            }

            _source.Purchase(productId, onComplete);
        }

        public static bool IsPurchased(string productId)
        {
            if (!EnsureInitialized())
                return false;

            return _source.IsPurchased(productId);
        }

        public static void FetchProducts(Action<bool, IReadOnlyList<ProductData>> onComplete)
        {
            if (!EnsureInitialized())
            {
                onComplete?.Invoke(false, null);
                return;
            }

            _source.FetchProducts((success, products) =>
            {
                if (success && products != null)
                    Products = products;
                onComplete?.Invoke(success, products);
            });
        }

        public static void GetProduct(string productId, Action<ProductData> onComplete)
        {
            FetchProducts((success, products) =>
            {
                if (!success || products == null)
                {
                    onComplete?.Invoke(null);
                    return;
                }

                foreach (var p in products)
                {
                    if (p.ProductId == productId)
                    {
                        onComplete?.Invoke(p);
                        return;
                    }
                }

                onComplete?.Invoke(null);
            });
        }

        public static void RefreshPurchases(Action<bool> onComplete = null)
        {
            if (!EnsureInitialized())
            {
                onComplete?.Invoke(false);
                return;
            }

            _source.RefreshPurchases(onComplete);
        }

        public static void RestorePurchases(Action<bool> onComplete = null)
        {
            if (!EnsureInitialized())
            {
                onComplete?.Invoke(false);
                return;
            }

            _source.RestorePurchases((success, restoredIds) =>
            {
                if (success && restoredIds != null)
                    foreach (var id in restoredIds)
                        OnRestoreSuccess?.Invoke(id);
                onComplete?.Invoke(success);
            });
        }

        /// <summary>
        /// Returns the localized price string for a product from the cached catalog.
        /// Returns empty string if products haven't been fetched yet or the product isn't found.
        /// </summary>
        public static string GetLocalizedPrice(string productId)
        {
            if (Products == null) return string.Empty;
            foreach (var p in Products)
                if (p.ProductId == productId) return p.LocalizedPriceString;
            return string.Empty;
        }

        private static bool EnsureInitialized()
        {
            if (IsInitialized)
                return true;

            Debug.LogWarning($"[{nameof(UniBridgePurchases)}]: Not initialized!");
            return false;
        }

        private static UniBridgePurchasesConfig LoadConfig()
        {
            var config = Resources.Load<UniBridgePurchasesConfig>(nameof(UniBridgePurchasesConfig));
            if (config == null)
                Debug.LogWarning($"[{nameof(UniBridgePurchases)}]: UniBridgePurchasesConfig not found in Resources folder.");

            return config;
        }
    }
}
