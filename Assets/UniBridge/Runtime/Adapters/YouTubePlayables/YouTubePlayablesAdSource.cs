#if UNIBRIDGE_YTPLAYABLES && UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
using UnityEngine.Scripting;

namespace UniBridge
{
    /// <summary>
    /// IAdSource adapter for YouTube Playables.
    /// YouTube only supports interstitial ads (preview API). Rewarded and banner are not available.
    /// </summary>
    [Preserve]
    public class YouTubePlayablesAdSource : IAdSource
    {
        [DllImport("__Internal")] private static extern void YTPlayables_RequestInterstitialAd(Action<int> onSuccess, Action<int> onFail);
        [DllImport("__Internal")] private static extern int  YTPlayables_InPlayablesEnv();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void RegisterAdapter()
        {
            VerboseLog.Log("YT:Ad", "RegisterAdapter enter — AfterAssembliesLoaded");
            AdSourceRegistry.Register("UNIBRIDGE_YTPLAYABLES", config => new YouTubePlayablesAdSource(), 100);
            Debug.Log("[UniBridge] YouTube Playables ad adapter registered");
            VerboseLog.Log("YT:Ad", "RegisterAdapter done (key=UNIBRIDGE_YTPLAYABLES, priority=100)");
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

        private static bool _interstitialReadyLogged;
        public bool IsInterstitialReady()
        {
            var inEnv = YTPlayables_InPlayablesEnv() == 1;
            var ready = _initialized && inEnv;
            if (!_interstitialReadyLogged)
            {
                _interstitialReadyLogged = true;
                VerboseLog.Log("YT:Ad", $"IsInterstitialReady first-check: init={_initialized}, inEnv={inEnv} → {ready}");
            }
            return ready;
        }
        public bool IsRewardReady()       => false;
        public bool IsBannerReady()       => false;

        public void Initialize(Action onInitSuccess, Action onInitFailed)
        {
            VerboseLog.Log("YT:Ad", $"Initialize enter (alreadyInit={_initialized}, inPlayablesEnv={YTPlayables_InPlayablesEnv() == 1})");
            if (_initialized) { onInitSuccess?.Invoke(); return; }
            _instance    = this;
            _initialized = true;
            Debug.Log($"[{nameof(YouTubePlayablesAdSource)}] Initialized");
            onInitSuccess?.Invoke();
            VerboseLog.Log("YT:Ad", "Initialize done");
        }

        public void ShowInterstitial(Action<AdStatus> endCallback, string placementName = "")
        {
            VerboseLog.Log("YT:Ad", $"ShowInterstitial enter (placement=\"{placementName}\", initialized={_initialized}, inEnv={YTPlayables_InPlayablesEnv() == 1})");
            if (!_initialized)
            {
                VerboseLog.Warn("YT:Ad", "ShowInterstitial: adapter not initialized → Failed");
                endCallback?.Invoke(AdStatus.Failed);
                OnInterstitialClosed?.Invoke();
                return;
            }
            if (YTPlayables_InPlayablesEnv() != 1)
            {
                VerboseLog.Warn("YT:Ad", "ShowInterstitial: not in Playables env → Failed");
                endCallback?.Invoke(AdStatus.Failed);
                OnInterstitialClosed?.Invoke();
                return;
            }

            if (_interstitialCallback != null)
                VerboseLog.Warn("YT:Ad", "ShowInterstitial: previous callback not drained — overwriting");

            _interstitialCallback = endCallback;
            VerboseLog.Log("YT:Ad", "→ YTPlayables_RequestInterstitialAd dispatching");
            YTPlayables_RequestInterstitialAd(OnInterstitialSuccess, OnInterstitialFail);
        }

        [Preserve, MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnInterstitialSuccess(int _)
        {
            VerboseLog.Log("YT:Ad", "← RequestInterstitialAd success");
            var cb = _interstitialCallback;
            _interstitialCallback = null;
            cb?.Invoke(AdStatus.Completed);
            _instance?.OnInterstitialClosed?.Invoke();
        }

        [Preserve, MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnInterstitialFail(int _)
        {
            VerboseLog.Warn("YT:Ad", "← RequestInterstitialAd failed");
            var cb = _interstitialCallback;
            _interstitialCallback = null;
            cb?.Invoke(AdStatus.Failed);
            _instance?.OnInterstitialClosed?.Invoke();
        }

        public void ShowReward(Action<AdStatus> endCallback, string placementName = "")
        {
            VerboseLog.Warn("YT:Ad", $"ShowReward(\"{placementName}\") called — rewarded ads not supported on YT Playables → Failed");
            Debug.LogWarning($"[{nameof(YouTubePlayablesAdSource)}] Rewarded ads are not supported on YouTube Playables");
            endCallback?.Invoke(AdStatus.Failed);
            OnRewardClosed?.Invoke();
        }

        public void ShowBanner()    { VerboseLog.Log("YT:Ad", "ShowBanner called — not supported on YT Playables, no-op"); }
        public void HideBanner()    { VerboseLog.Log("YT:Ad", "HideBanner called — not supported on YT Playables, no-op"); }
        public void DestroyBanner() { VerboseLog.Log("YT:Ad", "DestroyBanner called — not supported on YT Playables, no-op"); }
        public void EnableYoungMode()  { VerboseLog.Log("YT:Ad", "EnableYoungMode called — no-op on YT Playables"); }
        public void DisableYoungMode() { VerboseLog.Log("YT:Ad", "DisableYoungMode called — no-op on YT Playables"); }
    }
}
#endif
