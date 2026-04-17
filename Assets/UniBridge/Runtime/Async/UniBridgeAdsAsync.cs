#if UNIBRIDGE_UNITASK
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UniBridge.Async
{
    /// <summary>
    /// Async wrappers over <see cref="UniBridge"/> (Ads facade). Requires `com.cysharp.unitask` package.
    /// </summary>
    public static class UniBridgeAdsAsync
    {
        /// <summary>
        /// Shows an interstitial ad. Resolves with <see cref="AdStatus"/> describing the outcome
        /// (Finished, NotLoaded, AlreadyShowing, Disabled).
        /// </summary>
        public static UniTask<AdStatus> ShowInterstitialAsync(string placementName = "", CancellationToken ct = default)
            => AsyncHelpers.Await<AdStatus>(cb => UniBridge.ShowInterstitial(cb, placementName), ct);

        /// <summary>
        /// Shows a rewarded ad. Resolves with <see cref="AdStatus"/>.
        /// </summary>
        public static UniTask<AdStatus> ShowRewardAsync(
            string placementName = "",
            bool resetInterstitialTimer = true,
            CancellationToken ct = default)
            => AsyncHelpers.Await<AdStatus>(cb => UniBridge.ShowReward(cb, placementName, resetInterstitialTimer), ct);
    }
}
#endif
