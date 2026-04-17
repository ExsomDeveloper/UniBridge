#if UNIBRIDGE_PLAYGAMA && UNITY_WEBGL
using System;
using Playgama;
using Playgama.Modules.Advertisement;
using UnityEngine;

namespace UniBridge
{
    public class PlaygamaAdapter : IAdSource
    {
        /// <summary>
        /// Register this adapter with the runtime. Called automatically at startup.
        /// </summary>
        [UnityEngine.RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void RegisterAdapter()
        {
            AdSourceRegistry.Register("UNIBRIDGE_PLAYGAMA", config => new PlaygamaAdapter(config.PlaygamaSettings), 100);
            Debug.Log("[UniBridge] Playgama adapter registered");
        }

        public event Action OnInterstitialClosed;
        public event Action OnRewardClosed;
        public event Action OnBannerLoaded;

        private readonly PlaygamaSettings _settings;
        private bool _initialized = false;
        private bool _bannerShowing = false;
        private bool _interstitialShowing = false;
        private bool _rewardShowing = false;
        private Action<AdStatus> _interstitialCallback;
        private Action<AdStatus> _rewardCallback;
        private bool _rewardReceived = false;

        public PlaygamaAdapter(PlaygamaSettings settings)
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
                // Subscribe to Playgama Bridge events
                Bridge.advertisement.interstitialStateChanged += OnInterstitialStateChanged;
                Bridge.advertisement.rewardedStateChanged += OnRewardedStateChanged;
                Bridge.advertisement.bannerStateChanged += OnBannerStateChanged;

                _initialized = true;
                Debug.Log($"[{nameof(PlaygamaAdapter)}]: Initialized successfully");
                onInitSuccess?.Invoke();

                // Notify banner loaded immediately since Playgama handles banner internally
                OnBannerLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(PlaygamaAdapter)}]: Initialization failed: {ex.Message}");
                onInitFailed?.Invoke();
            }
        }

        public bool IsInterstitialReady()
        {
            return _initialized && !_interstitialShowing
                && Bridge.advertisement.isInterstitialSupported
                && !IsPlatformInList(_settings.DisableInterstitialOnPlatforms);
        }

        public bool IsRewardReady()
        {
            return _initialized && !_rewardShowing
                && Bridge.advertisement.isRewardedSupported
                && !IsPlatformInList(_settings.DisableRewardOnPlatforms);
        }

        public bool IsBannerReady()
        {
            return _initialized && Bridge.advertisement.isBannerSupported
                && !IsPlatformInList(_settings.DisableBannerOnPlatforms);
        }

        public bool IsInterstitialSupported => _initialized && Bridge.advertisement.isInterstitialSupported;
        public bool IsRewardedSupported => _initialized && Bridge.advertisement.isRewardedSupported;
        public bool IsBannerSupported => _initialized && Bridge.advertisement.isBannerSupported;

        public void ShowInterstitial(Action<AdStatus> endCallback, string placementName = "")
        {
            if (!_initialized)
            {
                endCallback?.Invoke(AdStatus.NotLoaded);
                return;
            }

            if (_interstitialShowing)
            {
                endCallback?.Invoke(AdStatus.AlreadyShowing);
                return;
            }

            if (IsPlatformInList(_settings.DisableInterstitialOnPlatforms))
            {
                endCallback?.Invoke(AdStatus.Disabled);
                return;
            }
#if UNIBRIDGE_VERBOSE_LOG
            VLog("ShowInterstitial");
#endif
            _interstitialCallback = endCallback;
            _interstitialShowing = true;

            int countdown = _settings.YandexInterstitialCountdownSeconds;
            if (Bridge.platform.id == "yandex" && countdown > 0)
            {
                var go = new GameObject("[PlaygamaAds] InterstitialCountdown");
                var overlay = go.AddComponent<InterstitialCountdownOverlay>();
                overlay.Begin(countdown, () => Bridge.advertisement.ShowInterstitial(placementName));
            }
            else
            {
                Bridge.advertisement.ShowInterstitial(placementName);
            }
        }

        public void ShowReward(Action<AdStatus> endCallback, string placementName = "")
        {
            if (!_initialized)
            {
                endCallback?.Invoke(AdStatus.NotLoaded);
                return;
            }

            if (_rewardShowing)
            {
                endCallback?.Invoke(AdStatus.AlreadyShowing);
                return;
            }

            if (IsPlatformInList(_settings.DisableRewardOnPlatforms))
            {
                endCallback?.Invoke(AdStatus.Disabled);
                return;
            }
#if UNIBRIDGE_VERBOSE_LOG
            VLog("ShowReward");
#endif
            _rewardCallback = endCallback;
            _rewardShowing = true;
            _rewardReceived = false;
            Bridge.advertisement.ShowRewarded(placementName);
        }

        public void ShowBanner()
        {
            if (!_initialized || IsPlatformInList(_settings.DisableBannerOnPlatforms))
                return;
#if UNIBRIDGE_VERBOSE_LOG
            VLog("ShowBanner");
#endif
            _bannerShowing = true;
            Bridge.advertisement.ShowBanner();
        }

        public void HideBanner()
        {
            if (!_initialized)
                return;
#if UNIBRIDGE_VERBOSE_LOG
            VLog("HideBanner");
#endif
            _bannerShowing = false;
            Bridge.advertisement.HideBanner();
        }

        public void DestroyBanner()
        {
            HideBanner();
        }

        public void EnableYoungMode()
        {
            Debug.Log($"[{nameof(PlaygamaAdapter)}]: Young mode enabled (handled by platform)");
        }

        public void DisableYoungMode()
        {
            Debug.Log($"[{nameof(PlaygamaAdapter)}]: Young mode disabled");
        }

        private void OnInterstitialStateChanged(InterstitialState state)
        {
            Debug.Log($"[{nameof(PlaygamaAdapter)}]: Interstitial state changed: {state}");

            switch (state)
            {
                case InterstitialState.Loading:
                case InterstitialState.Opened:
                    break;
                case InterstitialState.Closed:
                    _interstitialShowing = false;
                    _interstitialCallback?.Invoke(AdStatus.Completed);
                    _interstitialCallback = null;
                    OnInterstitialClosed?.Invoke();
                    break;
                case InterstitialState.Failed:
                    _interstitialShowing = false;
                    _interstitialCallback?.Invoke(AdStatus.Failed);
                    _interstitialCallback = null;
                    OnInterstitialClosed?.Invoke();
                    break;
            }
        }

        private void OnRewardedStateChanged(RewardedState state)
        {
            Debug.Log($"[{nameof(PlaygamaAdapter)}]: Rewarded state changed: {state}");

            switch (state)
            {
                case RewardedState.Loading:
                case RewardedState.Opened:
                    break;
                case RewardedState.Rewarded:
                    _rewardReceived = true;
                    break;
                case RewardedState.Closed:
                    _rewardShowing = false;
                    if (_rewardReceived)
                    {
                        _rewardCallback?.Invoke(AdStatus.Completed);
                    }
                    else
                    {
                        _rewardCallback?.Invoke(AdStatus.Canceled);
                    }
                    _rewardCallback = null;
                    _rewardReceived = false;
                    OnRewardClosed?.Invoke();
                    break;
                case RewardedState.Failed:
                    _rewardShowing = false;
                    _rewardCallback?.Invoke(AdStatus.Failed);
                    _rewardCallback = null;
                    _rewardReceived = false;
                    OnRewardClosed?.Invoke();
                    break;
            }
        }

        private void OnBannerStateChanged(BannerState state)
        {
            Debug.Log($"[{nameof(PlaygamaAdapter)}]: Banner state changed: {state}");

            switch (state)
            {
                case BannerState.Loading:
                    break;
                case BannerState.Shown:
                    OnBannerLoaded?.Invoke();
                    break;
                case BannerState.Hidden:
                case BannerState.Failed:
                    break;
            }
        }

        private static bool IsPlatformInList(PlatformId[] list)
        {
            if (list == null || list.Length == 0) return false;
            var currentId = Bridge.platform.id;
            if (string.IsNullOrEmpty(currentId)) return false;
            for (int i = 0; i < list.Length; i++)
                if (list[i].ToStringId() == currentId)
                    return true;
            return false;
        }

#if UNIBRIDGE_VERBOSE_LOG
        private static void VLog(string msg) => Debug.Log($"[RAT] [{nameof(PlaygamaAdapter)}] {msg}");
#endif
    }
}
#endif
