#if UNIBRIDGE_LEVELPLAY
using System;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace UniBridge
{
    public class LevelPlayAdapter : IAdSource
    {
        /// <summary>
        /// Register this adapter with the runtime. Called automatically at startup.
        /// </summary>
        [UnityEngine.RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterAdapter()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            AdSourceRegistry.Register("UNIBRIDGE_LEVELPLAY", config => new LevelPlayAdapter(config.LevelPlaySettings), 100);
            Debug.Log("[UniBridge] LevelPlay adapter registered");
#endif
        }

        public event Action OnInterstitialAdLoaded;
        public event Action OnRewardedAdLoaded;
        public event Action OnBannerLoaded;
        public event Action OnInterstitialClosed;
        public event Action OnRewardClosed;

        private readonly LevelPlaySettings _settings;
        private LevelPlayBanner _banner;
        private LevelPlayInterstitial _interstitial;
        private LevelPlayRewardWrapper _reward;
        private bool _initialized = false;
        private bool _initializationInProcess = false;

        public LevelPlayAdapter(LevelPlaySettings levelPlaySettings)
        {
            _settings = levelPlaySettings;
            var gameFocusHandler = new GameObject("GameFocusHandler");
            gameFocusHandler.AddComponent<GameFocusHandler>();
            UnityEngine.Object.DontDestroyOnLoad(gameFocusHandler);
        }

        public void Initialize(Action onInitSuccess, Action onInitFailed)
        {
            if (_initialized)
                throw new Exception("Already initialized");

            if (_initializationInProcess)
                throw new Exception("Initialization in process");

            InitializeSdk(onInitSuccess, onInitFailed);
        }

        private void InitializeSdk(Action onInitSuccess, Action onInitFailed)
        {
            Debug.Log($"[{nameof(LevelPlayAdapter)}]: Start init sdk");

#if DEVELOPMENT_BUILD
            LevelPlay.ValidateIntegration();
            LevelPlay.SetMetaData("is_test_suite", "enable");
#endif

            var developerSettings = Resources.Load<LevelPlayMediationSettings>(nameof(LevelPlayMediationSettings));
            Debug.Log($"[{nameof(LevelPlayAdapter)}]: developerSettings: {developerSettings}");

#if UNITY_ANDROID
            string appKey = developerSettings != null ? developerSettings.AndroidAppKey : _settings.AndroidKey;
#elif UNITY_IOS
            string appKey = developerSettings != null ? developerSettings.IOSAppKey : _settings.IOSKey;
#else
            string appKey = "";
#endif

            if (string.IsNullOrEmpty(appKey))
            {
                Debug.LogError($"[{nameof(LevelPlayAdapter)}]: Cannot init without AppKey!");
                onInitFailed?.Invoke();
                return;
            }
#if UNIBRIDGE_VERBOSE_LOG
            VLog($"InitializeSdk: appKey={appKey}, developerSettings={developerSettings != null}");
#endif
            LevelPlay.OnInitSuccess += (configuration) =>
            {
                _initializationInProcess = false;
#if DEVELOPMENT_BUILD
                LevelPlay.LaunchTestSuite();
#endif
                onInitSuccess?.Invoke();
                Debug.Log($"[{nameof(LevelPlayAdapter)}]: SDK loaded!");
                InitializeCallbacks();
                _initialized = true;
            };

            LevelPlay.OnInitFailed += (configuration) =>
            {
                _initializationInProcess = false;
                onInitFailed?.Invoke();
                Debug.LogError($"[{nameof(LevelPlayAdapter)}]: SDK initialization failed!");
            };

            _initializationInProcess = true;
            LevelPlay.Init(appKey);
        }

        private void InitializeCallbacks()
        {
            _banner = new LevelPlayBanner(_settings.GetBannerAdUnitId());
            _interstitial = new LevelPlayInterstitial(_settings.GetInterstitialAdUnitId());
            _reward = new LevelPlayRewardWrapper(_settings.GetRewardAdUnitId());

            _interstitial.OnAdClosed += () => OnInterstitialClosed?.Invoke();
            _reward.OnAdClosed += () => OnRewardClosed?.Invoke();

            _banner.OnAdLoaded += () => OnBannerLoaded?.Invoke();
            _reward.OnAdLoaded += () => OnRewardedAdLoaded?.Invoke();
            _interstitial.OnAdLoaded += () => OnInterstitialAdLoaded?.Invoke();
        }

        private bool EnsureInitialized()
        {
            if (_initialized)
                return true;

            Debug.LogWarning($"[{nameof(LevelPlayAdapter)}]: LevelPlayAdapter is not initialized!");
            return false;
        }

        public bool IsInterstitialReady()
        {
            return EnsureInitialized() && _interstitial.IsReady();
        }

        public bool IsRewardReady()
        {
            return EnsureInitialized() && _reward.IsReady();
        }

        public bool IsBannerReady()
        {
            return EnsureInitialized() && _banner.IsReady();
        }

        public bool IsInterstitialSupported => true;
        public bool IsRewardedSupported => true;
        public bool IsBannerSupported => true;

        public void ShowBanner()
        {
            if (!EnsureInitialized())
                return;

            _banner.Show();
        }

        public void HideBanner()
        {
            if (!EnsureInitialized())
                return;

            _banner.Hide();
        }

        public void DestroyBanner()
        {
            if (!EnsureInitialized())
                return;

            _banner.Destroy();
        }

        public void SetConsent(bool value)
        {
            LevelPlay.SetConsent(value);
        }

        public void EnableYoungMode()
        {
            LevelPlay.SetMetaData("is_child_directed", "true");
            LevelPlay.SetMetaData("AdMob_TFCD", "true");
            LevelPlay.SetMetaData("AdColony_COPPA", "true");
            LevelPlay.SetMetaData("AppLovin_AgeRestrictedUser", "true");
            LevelPlay.SetMetaData("InMobi_AgeRestricted", "true");
            LevelPlay.SetMetaData("Mintegral_COPPA", "true");
            LevelPlay.SetMetaData("Vungle_coppa", "true");
            LevelPlay.SetMetaData("Pangle_COPPA", "1");
            LevelPlay.SetMetaData("UnityAds_coppa", "true");
            LevelPlay.SetMetaData("AdMob_MaxContentRating", "MAX_AD_CONTENT_RATING_PG");
            SetupSegment("children", 12);
        }

        public void DisableYoungMode()
        {
            LevelPlay.SetMetaData("is_child_directed", "false");
            LevelPlay.SetMetaData("AdMob_TFCD", "false");
            LevelPlay.SetMetaData("AdColony_COPPA", "false");
            LevelPlay.SetMetaData("AppLovin_AgeRestrictedUser", "false");
            LevelPlay.SetMetaData("InMobi_AgeRestricted", "false");
            LevelPlay.SetMetaData("Mintegral_COPPA", "false");
            LevelPlay.SetMetaData("Vungle_coppa", "false");
            LevelPlay.SetMetaData("Pangle_COPPA", "0");
            LevelPlay.SetMetaData("UnityAds_coppa", "false");
            LevelPlay.SetMetaData("AdMob_MaxContentRating", "MAX_AD_CONTENT_RATING_MA");
        }

        private void SetupSegment(string segmentName, int userAge)
        {
            LevelPlaySegment levelPlaySegment = new LevelPlaySegment();
            levelPlaySegment.SegmentName = segmentName;
            levelPlaySegment.SetCustom("age", userAge.ToString());
            LevelPlay.SetSegment(levelPlaySegment);
        }

        public void ShowInterstitial(Action<AdStatus> endCallback, string placementName = "")
        {
#if UNIBRIDGE_VERBOSE_LOG
            VLog($"ShowInterstitial: placement={placementName}");
#endif
            _interstitial.ShowAd(endCallback, placementName);
        }

        public void ShowReward(Action<AdStatus> endCallback, string placementName = "")
        {
#if UNIBRIDGE_VERBOSE_LOG
            VLog($"ShowReward: placement={placementName}");
#endif
            _reward.ShowReward(endCallback, placementName);
        }

#if UNIBRIDGE_VERBOSE_LOG
        private static void VLog(string msg) => Debug.Log($"[RAT] [{nameof(LevelPlayAdapter)}] {msg}");
#endif
    }
}
#endif
