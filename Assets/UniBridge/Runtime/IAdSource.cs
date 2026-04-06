using System;

namespace UniBridge
{
    public interface IAdSource
    {
        event Action OnInterstitialClosed;
        event Action OnRewardClosed;
        event Action OnBannerLoaded;

        bool IsInterstitialReady();
        bool IsRewardReady();
        bool IsBannerReady();
        bool IsInterstitialSupported { get; }
        bool IsRewardedSupported { get; }
        bool IsBannerSupported { get; }
        void Initialize(Action onInitSuccess, Action onInitFailed);
        void ShowInterstitial(Action<AdStatus> endCallback, string placementName = "");
        void ShowReward(Action<AdStatus> endCallback, string placementName = "");
        void ShowBanner();
        void HideBanner();
        void DestroyBanner();
        void EnableYoungMode();
        void DisableYoungMode();
    }
}
