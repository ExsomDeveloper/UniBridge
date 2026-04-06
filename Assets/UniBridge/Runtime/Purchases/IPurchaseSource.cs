using System;
using System.Collections.Generic;

namespace UniBridge
{
    public interface IPurchaseSource
    {
        bool IsInitialized { get; }
        bool IsSupported { get; }

        event Action<PurchaseResult> OnPurchaseSuccess;
        event Action<PurchaseResult> OnPurchaseFailed;

        void Initialize(UniBridgePurchasesConfig config, Action onSuccess, Action onFailed);
        void FetchProducts(Action<bool, IReadOnlyList<ProductData>> onComplete);
        void Purchase(string productId, Action<PurchaseResult> onComplete);

        /// <summary>
        /// Returns true if a non-consumable product is owned.
        /// Reads from local cache populated during Initialize / RefreshPurchases.
        /// </summary>
        bool IsPurchased(string productId);

        /// <summary>
        /// Refreshes ownership cache from the store. Updates what IsPurchased returns.
        /// </summary>
        void RefreshPurchases(Action<bool> onComplete);

        /// <summary>
        /// Restores non-consumable purchases (required on iOS App Store).
        /// Returns a list of product IDs that were newly restored in this call.
        /// </summary>
        void RestorePurchases(Action<bool, IReadOnlyList<string>> onComplete);
    }
}
