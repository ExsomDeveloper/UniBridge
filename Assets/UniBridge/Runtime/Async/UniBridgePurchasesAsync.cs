#if UNIBRIDGE_UNITASK
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UniBridge.Async
{
    /// <summary>
    /// Async wrappers over <see cref="UniBridgePurchases"/>. Requires `com.cysharp.unitask` package.
    /// </summary>
    public static class UniBridgePurchasesAsync
    {
        /// <summary>
        /// Initiates a purchase. Resolves with <see cref="PurchaseResult"/> (inspect <see cref="PurchaseResult.Status"/>).
        /// </summary>
        public static UniTask<PurchaseResult> BuyAsync(string productId, CancellationToken ct = default)
            => AsyncHelpers.Await<PurchaseResult>(cb => UniBridgePurchases.Buy(productId, cb), ct);

        public static UniTask<(bool ok, IReadOnlyList<ProductData> products)> FetchProductsAsync(CancellationToken ct = default)
            => AsyncHelpers.Await<bool, IReadOnlyList<ProductData>>(cb => UniBridgePurchases.FetchProducts(cb), ct);

        public static UniTask<ProductData> GetProductAsync(string productId, CancellationToken ct = default)
            => AsyncHelpers.Await<ProductData>(cb => UniBridgePurchases.GetProduct(productId, cb), ct);

        public static UniTask<bool> RefreshPurchasesAsync(CancellationToken ct = default)
            => AsyncHelpers.Await<bool>(cb => UniBridgePurchases.RefreshPurchases(cb), ct);

        public static UniTask<bool> RestorePurchasesAsync(CancellationToken ct = default)
            => AsyncHelpers.Await<bool>(cb => UniBridgePurchases.RestorePurchases(cb), ct);
    }
}
#endif
