#if UNIBRIDGE_UNITASK
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UniBridge.Async
{
    /// <summary>
    /// Async wrappers over <see cref="UniBridgeRate"/>. Requires `com.cysharp.unitask` package.
    /// </summary>
    public static class UniBridgeRateAsync
    {
        public static UniTask<bool> RequestReviewAsync(CancellationToken ct = default)
            => AsyncHelpers.Await<bool>(cb => UniBridgeRate.RequestReview(cb), ct);
    }
}
#endif
