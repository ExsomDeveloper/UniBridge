#if UNIBRIDGE_YANDEX
using System;
using UnityEngine;
using YandexMobileAds;

namespace UniBridge
{
    public class YandexAdapter : IAdSource
    {
        [UnityEngine.RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void RegisterAdapter()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            AdSourceRegistry.Register("UNIBRIDGE_YANDEX", config => new YandexAdapter(config.YandexSettings), 90);
            Debug.Log("[UniBridge] Yandex adapter registered");
#endif
        }

        public event Action OnInterstitialClosed;
        public event Action OnRewardClosed;
        public event Action OnBannerLoaded;

        private readonly YandexSettings _settings;
        private YandexBanner _banner;
        private YandexInterstitial _interstitial;
        private YandexRewardWrapper _reward;
        private bool _initialized;
        private bool _youngMode;

        public YandexAdapter(YandexSettings settings)
        {
            _settings = settings;
        }

        public void Initialize(Action onInitSuccess, Action onInitFailed)
        {
            if (_initialized)
            {
                onInitSuccess?.Invoke();
                return;
            }

            try
            {
                MobileAds.SetAgeRestrictedUser(_youngMode);

                _banner = new YandexBanner(_settings.GetBannerAdUnitId());
                _interstitial = new YandexInterstitial(_settings.GetInterstitialAdUnitId());
                _reward = new YandexRewardWrapper(_settings.GetRewardedAdUnitId());

                _interstitial.OnAdClosed += () => OnInterstitialClosed?.Invoke();
                _reward.OnAdClosed += () => OnRewardClosed?.Invoke();
                _banner.OnAdLoaded += () => OnBannerLoaded?.Invoke();

                _initialized = true;
                Debug.Log("[YandexAdapter]: Initialized successfully");
                onInitSuccess?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[YandexAdapter]: Initialization failed: {ex.Message}");
                onInitFailed?.Invoke();
            }
        }

        public bool IsInterstitialReady() => _initialized && _interstitial != null && _interstitial.IsReady();
        public bool IsRewardReady() => _initialized && _reward != null && _reward.IsReady();
        public bool IsBannerReady() => _initialized && _banner != null && _banner.IsReady();
        public bool IsInterstitialSupported => true;
        public bool IsRewardedSupported => true;
        public bool IsBannerSupported => true;

        public void ShowInterstitial(Action<AdStatus> endCallback, string placementName = "")
        {
            if (!EnsureInitialized(endCallback))
                return;
#if UNIBRIDGE_VERBOSE_LOG
            VLog($"ShowInterstitial: placement={placementName}");
#endif
            _interstitial.ShowAd(endCallback, placementName);
        }

        public void ShowReward(Action<AdStatus> endCallback, string placementName = "")
        {
            if (!EnsureInitialized(endCallback))
                return;
#if UNIBRIDGE_VERBOSE_LOG
            VLog($"ShowReward: placement={placementName}");
#endif
            _reward.ShowReward(endCallback, placementName);
        }

        public void ShowBanner()
        {
            if (!EnsureInitialized())
                return;
#if UNIBRIDGE_VERBOSE_LOG
            VLog("ShowBanner");
#endif
            _banner.Show();
        }

        public void HideBanner()
        {
            if (!EnsureInitialized())
                return;
#if UNIBRIDGE_VERBOSE_LOG
            VLog("HideBanner");
#endif
            _banner.Hide();
        }

        public void DestroyBanner()
        {
            if (!EnsureInitialized())
                return;
            _banner.Destroy();
        }

        public void EnableYoungMode()
        {
            _youngMode = true;
            if (_initialized)
                MobileAds.SetAgeRestrictedUser(true);
        }

        public void DisableYoungMode()
        {
            _youngMode = false;
            if (_initialized)
                MobileAds.SetAgeRestrictedUser(false);
        }

        private bool EnsureInitialized(Action<AdStatus> callback = null)
        {
            if (_initialized)
                return true;

            Debug.LogWarning("[YandexAdapter]: Not initialized!");
            callback?.Invoke(AdStatus.NotLoaded);
            return false;
        }

#if UNIBRIDGE_VERBOSE_LOG
        private static void VLog(string msg) => Debug.Log($"[RAT] [{nameof(YandexAdapter)}] {msg}");
#endif
    }
}
#endif
