#if UNIBRIDGE_YTPLAYABLES && UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace UniBridge
{
    /// <summary>
    /// IAdSource adapter for YouTube Playables.
    /// YouTube only supports interstitial ads (preview API). Rewarded and banner are not available.
    /// </summary>
    public class YouTubePlayablesAdSource : IAdSource
    {
        [DllImport("__Internal")] private static extern void YTPlayables_RequestInterstitialAd(Action<int> onSuccess, Action<int> onFail);
        [DllImport("__Internal")] private static extern int  YTPlayables_InPlayablesEnv();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterAdapter()
        {
            AdSourceRegistry.Register("UNIBRIDGE_YTPLAYABLES", config => new YouTubePlayablesAdSource(), 100);
            Debug.Log("[UniBridge] YouTube Playables ad adapter registered");
        }

        public event Action OnInterstitialClosed;
        public event Action OnRewardClosed;
        public event Action OnBannerLoaded;

        private static YouTubePlayablesAdSource _instance;
        private bool _initialized;
        private static Action<AdStatus> _interstitialCallback;

        public bool IsInterstitialSupported => true;
        public bool IsRewardedSupported     => false;
        public bool IsBannerSupported       => false;

        public bool IsInterstitialReady() => _initialized && YTPlayables_InPlayablesEnv() == 1;
        public bool IsRewardReady()       => false;
        public bool IsBannerReady()       => false;

        public void Initialize(Action onInitSuccess, Action onInitFailed)
        {
            if (_initialized) { onInitSuccess?.Invoke(); return; }
            _instance    = this;
            _initialized = true;
            Debug.Log($"[{nameof(YouTubePlayablesAdSource)}] Initialized");
            onInitSuccess?.Invoke();
        }

        public void ShowInterstitial(Action<AdStatus> endCallback, string placementName = "")
        {
            if (!_initialized || YTPlayables_InPlayablesEnv() != 1)
            {
                endCallback?.Invoke(AdStatus.Failed);
                OnInterstitialClosed?.Invoke();
                return;
            }

            _interstitialCallback = endCallback;
            YTPlayables_RequestInterstitialAd(OnInterstitialSuccess, OnInterstitialFail);
        }

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnInterstitialSuccess(int _)
        {
            var cb = _interstitialCallback;
            _interstitialCallback = null;
            cb?.Invoke(AdStatus.Completed);
            _instance?.OnInterstitialClosed?.Invoke();
        }

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnInterstitialFail(int _)
        {
            var cb = _interstitialCallback;
            _interstitialCallback = null;
            cb?.Invoke(AdStatus.Failed);
            _instance?.OnInterstitialClosed?.Invoke();
        }

        public void ShowReward(Action<AdStatus> endCallback, string placementName = "")
        {
            Debug.LogWarning($"[{nameof(YouTubePlayablesAdSource)}] Rewarded ads are not supported on YouTube Playables");
            endCallback?.Invoke(AdStatus.Failed);
            OnRewardClosed?.Invoke();
        }

        public void ShowBanner()    { }
        public void HideBanner()    { }
        public void DestroyBanner() { }
        public void EnableYoungMode()  { }
        public void DisableYoungMode() { }
    }
}
#endif
