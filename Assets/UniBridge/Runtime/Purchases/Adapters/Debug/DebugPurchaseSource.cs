using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniBridge
{
    public class DebugPurchaseSource : IPurchaseSource
    {
        public bool IsInitialized { get; private set; }
        public bool IsSupported => true;

        public event Action<PurchaseResult> OnPurchaseSuccess;
        public event Action<PurchaseResult> OnPurchaseFailed;

        private UniBridgePurchasesConfig _config;
        private readonly HashSet<string> _owned = new HashSet<string>();
        private DebugPurchasePanel _panel;

        private DebugPurchasePanel GetOrCreatePanel()
        {
            if (_panel != null) return _panel;
            var go = new GameObject("[UniBridge] DebugPurchasePanel");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _panel = go.AddComponent<DebugPurchasePanel>();
            return _panel;
        }

        public void Initialize(UniBridgePurchasesConfig config, Action onSuccess, Action onFailed)
        {
            _config = config;
            IsInitialized = true;
            Debug.Log($"[{nameof(DebugPurchaseSource)}]: Initialized");
            onSuccess?.Invoke();
        }

        public void FetchProducts(Action<bool, IReadOnlyList<ProductData>> onComplete)
        {
            var list = new List<ProductData>();

            if (_config?.Products != null)
            {
                foreach (var def in _config.Products)
                {
                    list.Add(new ProductData
                    {
                        ProductId            = def.ProjectId,
                        Type                 = def.Type,
                        LocalizedTitle       = $"[DEBUG] {def.ProjectId}",
                        LocalizedDescription = "Debug product",
                        LocalizedPriceString = "$0.99",
                        LocalizedPrice       = 0.99m,
                        IsoCurrencyCode      = "USD",
                        IsAvailable          = true
                    });
                }
            }

            Debug.Log($"[{nameof(DebugPurchaseSource)}]: FetchProducts — {list.Count} products");
            onComplete?.Invoke(true, list);
        }

        public void Purchase(string productId, Action<PurchaseResult> onComplete)
        {
            Debug.Log($"[{nameof(DebugPurchaseSource)}]: Purchase '{productId}'");

            bool isConsumable = true;
            if (_config?.Products != null)
            {
                foreach (var def in _config.Products)
                {
                    if (def.ProjectId == productId)
                    {
                        isConsumable = def.Type == ProductType.Consumable;
                        break;
                    }
                }
            }

            if (!isConsumable && _owned.Contains(productId))
            {
                var already = PurchaseResult.FromFailed(productId, PurchaseStatus.AlreadyOwned);
                onComplete?.Invoke(already);
                OnPurchaseFailed?.Invoke(already);
                return;
            }

            var typeLabel = isConsumable ? "CONSUMABLE" : "NON-CONSUMABLE";
            GetOrCreatePanel().Show(productId, "$0.99", typeLabel, confirmed =>
            {
                if (!confirmed)
                {
                    var cancelled = PurchaseResult.FromFailed(productId, PurchaseStatus.Cancelled);
                    onComplete?.Invoke(cancelled);
                    OnPurchaseFailed?.Invoke(cancelled);
                    return;
                }

                if (!isConsumable)
                    _owned.Add(productId);

                var result = PurchaseResult.FromSuccess(productId, Guid.NewGuid().ToString("N"));
                onComplete?.Invoke(result);
                OnPurchaseSuccess?.Invoke(result);
            });
        }

        public bool IsPurchased(string productId) => _owned.Contains(productId);

        public void RefreshPurchases(Action<bool> onComplete)
        {
            Debug.Log($"[{nameof(DebugPurchaseSource)}]: RefreshPurchases (no-op in editor)");
            onComplete?.Invoke(true);
        }

        public void RestorePurchases(Action<bool, IReadOnlyList<string>> onComplete)
        {
            Debug.Log($"[{nameof(DebugPurchaseSource)}]: RestorePurchases (no-op in editor)");
            foreach (var id in _owned)
                OnPurchaseSuccess?.Invoke(PurchaseResult.FromRestored(id));
            onComplete?.Invoke(true, Array.Empty<string>());
        }
    }
}
