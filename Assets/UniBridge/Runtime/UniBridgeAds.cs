using System;
using System.Collections;
using UnityEngine;

namespace UniBridge
{
    public class UniBridge : MonoBehaviour
    {
        public static bool IsInitialized { get; private set; }
        public static bool IsSupported => _adSource != null;
        public static string AdapterName => _adSource?.GetType().Name ?? UniBridgeAdapterKeys.None;
        public static event Action OnInitSuccess;
        public static event Action OnInitFailed;
        public static event Action OnInterstitialClosed;
        public static event Action OnRewardClosed;
        public static event Action OnBannerLoaded;

        private static AdTimer _timer;
        private static IAdSource _adSource;
        private static bool _adsDisabled = false;
        private static UniBridge _root;
        private static Action<AdStatus> _interstitialCallback;
        private static Action<AdStatus> _rewardCallback;
        private static bool _resetInterstitialTimer;
        private static bool _interstitialCooldownActive;
        private static UniBridgeConfig _config;
        private static bool _bannerShowing;
        private static float _savedAudioVolume = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (_config == null)
                _config = LoadConfig();

            if (_config != null && _config.AutoInitialize)
                SetupAds(_config.DefaultUserAge);
        }

        public static void Initialize(int userAge = -1)
        {
            if (_config == null)
                _config = LoadConfig();

            if (_config == null)
            {
                Debug.LogError($"[{nameof(UniBridge)}]: UniBridgeConfig not found! Create one via Assets > Create > UniBridge > Configuration");
                return;
            }

            int age = userAge >= 0 ? userAge : _config.DefaultUserAge;
            SetupAds(age);
        }

        private static void SetupAds(int userAge)
        {
            if (IsInitialized)
                return;

            CreateRootObject();
            var sourceBuilder = new AdSourceBuilder();
            bool isYoungMode = userAge <= _config.MaxChildrenAge;

            if (isYoungMode)
            {
                Debug.Log($"[{nameof(UniBridge)}]: Enabled young mode!");
            }
            else
            {
                Debug.Log($"[{nameof(UniBridge)}]: Enabled default mode!");
            }

            _adsDisabled = PlayerPrefs.HasKey(_config.AdsDisabledKey);
            _adSource = sourceBuilder.Build(_config, isYoungMode, _root.transform);

            if (_adSource == null)
            {
                Debug.Log($"[{nameof(UniBridge)}]: Ads system disabled.");
                return;
            }

            _adSource.Initialize(OnInitSuccessHandler, OnInitFailedHandler);
            _timer = new AdTimer(_root);

            _adSource.OnInterstitialClosed += () => OnInterstitialClosed?.Invoke();
            _adSource.OnRewardClosed += () => OnRewardClosed?.Invoke();
            _adSource.OnBannerLoaded += () =>
            {
                if (_bannerShowing)
                    ShowBanner();
                else
                    HideBanner();

                OnBannerLoaded?.Invoke();
            };

            Debug.Log($"[{nameof(UniBridge)}]: UniBridge is initialized!");
            IsInitialized = true;

            void OnInitSuccessHandler()
            {
                if (_config.InterstitialMode == InterstitialMode.Automatic)
                    StartInterstitialTimer(_config.AutoInterstitialInterval);
                OnInitSuccess?.Invoke();
            }

            void OnInitFailedHandler()
            {
                OnInitFailed?.Invoke();
            }
        }

        public static void StartInterstitialTimer(int seconds, bool loop = true, ITimerHandler timerHandler = null)
        {
            if (!EnsureInitialized())
                return;

            if (timerHandler == null)
                timerHandler = new InterstitialTimerHandler();

            timerHandler.Initialize();
            _timer.Start(seconds, loop, timerHandler);
        }

        public static void PauseInterstitialTimer()
        {
            if (!EnsureInitialized())
                return;

            _timer.Pause();
        }

        public static void UnpauseInterstitialTimer() => _timer?.Unpause();
        public static void ResetInterstitialTimer() => _timer?.Reset();
        public static void StopInterstitialTimer() => _timer?.Stop();

        public static bool IsInterstitialReady()
        {
            if (!EnsureInitialized())
                return false;

            return _adSource.IsInterstitialReady();
        }

        public static bool IsRewardReady()
        {
            if (!EnsureInitialized())
                return false;

            return _adSource.IsRewardReady();
        }

        public static bool IsBannerReady()
        {
            if (!EnsureInitialized())
                return false;

            return _adSource.IsBannerReady();
        }

        public static bool IsInterstitialSupported => _adSource?.IsInterstitialSupported ?? false;
        public static bool IsRewardedSupported => _adSource?.IsRewardedSupported ?? false;
        public static bool IsBannerSupported => _adSource?.IsBannerSupported ?? false;

        private static bool EnsureInitialized()
        {
            if (IsInitialized)
                return true;

            Debug.LogWarning($"[{nameof(UniBridge)}]: Not initialized!");
            return false;
        }

        public static bool AdsDisabled => _adsDisabled;

        public static void DisableAds()
        {
            if (!EnsureInitialized())
                return;

            _adsDisabled = true;
            DestroyBanner();
            PlayerPrefs.SetInt(_config.AdsDisabledKey, 1);
            PlayerPrefs.Save();
            Debug.Log($"[{nameof(UniBridge)}]: Ads disabled!");
        }

        public static void EnableAds()
        {
            if (!EnsureInitialized())
                return;

            _adsDisabled = false;
            PlayerPrefs.DeleteKey(_config.AdsDisabledKey);
            PlayerPrefs.Save();
            Debug.Log($"[{nameof(UniBridge)}]: Ads enabled!");
        }

        public static void ShowBanner()
        {
            if (!EnsureInitialized())
                return;

            if (_adsDisabled)
                return;

            _bannerShowing = true;
            _adSource.ShowBanner();
        }

        public static void HideBanner()
        {
            if (!EnsureInitialized())
                return;

            if (_adsDisabled)
                return;

            _bannerShowing = false;
            _adSource.HideBanner();
        }

        public static void DestroyBanner()
        {
            if (!EnsureInitialized())
                return;

            _adSource.DestroyBanner();
        }

        public static void ShowInterstitial(
            Action<AdStatus> endCallback,
            string placementName = "")
        {
            if (_interstitialCallback != null)
            {
                endCallback?.Invoke(AdStatus.AlreadyShowing);
                return;
            }

            if (!EnsureInitialized())
            {
                endCallback?.Invoke(AdStatus.NotLoaded);
                return;
            }

            if (_config.InterstitialMode == InterstitialMode.Manual &&
                _config.EnableManualCooldown && _interstitialCooldownActive)
            {
                endCallback?.Invoke(AdStatus.NotLoaded);
                return;
            }

            if (_adsDisabled)
            {
                endCallback?.Invoke(AdStatus.Disabled);
                return;
            }

            _interstitialCallback = endCallback;
            MuteAudio();
            _adSource.ShowInterstitial(OnInterstitialFinished, placementName);
        }

        public static void ShowReward(
            Action<AdStatus> endCallback,
            string placementName = "",
            bool resetInterstitialTimer = true)
        {
            if (_rewardCallback != null)
            {
                endCallback?.Invoke(AdStatus.AlreadyShowing);
                return;
            }

            if (!EnsureInitialized())
            {
                endCallback?.Invoke(AdStatus.NotLoaded);
                return;
            }

            _rewardCallback = endCallback;
            _resetInterstitialTimer = resetInterstitialTimer;
            _timer.Pause();
            MuteAudio();
            _adSource.ShowReward(OnRewardFinished, placementName);
        }

        private static void MuteAudio()
        {
            _savedAudioVolume = AudioListener.volume;
            AudioListener.volume = 0f;
        }

        private static void UnmuteAudio()
        {
            AudioListener.volume = _savedAudioVolume;
        }

        private static void OnInterstitialFinished(AdStatus status)
        {
            UnmuteAudio();
            bool adWasShown = status != AdStatus.NotLoaded;

            if (adWasShown)
            {
                if (_config.InterstitialMode == InterstitialMode.Automatic)
                {
                    _timer.Reset();
                }
                else if (_config.EnableManualCooldown)
                {
                    _interstitialCooldownActive = true;
                    _timer.Stop();
                    _timer.Start(_config.ManualCooldownInterval, false,
                        new CooldownTimerHandler(() => _interstitialCooldownActive = false));
                }
            }
            else
            {
                _timer.Unpause();
            }

            try
            {
                _interstitialCallback?.Invoke(status);
            }
            catch (Exception e)
            {
                _interstitialCallback?.Invoke(AdStatus.Failed);
                Debug.LogException(e);
            }
            finally
            {
                _interstitialCallback = null;
            }
        }

        private static void OnRewardFinished(AdStatus status)
        {
            UnmuteAudio();
            if (_resetInterstitialTimer)
            {
                if (status == AdStatus.Completed)
                    _timer.Reset();
                else
                    _timer.Unpause();
            }
            else
            {
                _timer.Unpause();
            }

            var callback = _rewardCallback;
            _rewardCallback = null;

            try
            {
                callback?.Invoke(status);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static UniBridgeConfig LoadConfig()
        {
            var config = Resources.Load<UniBridgeConfig>(nameof(UniBridgeConfig));
            if (config == null)
            {
                Debug.LogWarning($"[{nameof(UniBridge)}]: UniBridgeConfig not found in Resources folder.");
            }

            return config;
        }

        private static void CreateRootObject()
        {
            _root = new GameObject(nameof(UniBridge))
                .AddComponent<UniBridge>();

            DontDestroyOnLoad(_root);
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
                _timer?.Pause();
            else
                _timer?.Unpause();
        }

        private void OnDestroy()
        {
            _timer?.Stop();
            _interstitialCallback = null;
            _rewardCallback = null;
            _interstitialCooldownActive = false;
        }

        private class CooldownTimerHandler : ITimerHandler
        {
            private readonly Action _onExpired;
            public CooldownTimerHandler(Action onExpired) { _onExpired = onExpired; }
            public void Initialize() { }
            public IEnumerator Execute() { _onExpired?.Invoke(); yield break; }
        }
    }
}
