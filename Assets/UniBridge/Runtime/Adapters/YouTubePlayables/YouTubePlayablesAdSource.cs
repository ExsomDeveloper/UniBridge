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
    /// Supports interstitial (ytgame.ads.requestInterstitialAd) and rewarded (ytgame.ads.requestRewardedAd)
    /// — both are preview APIs. Banner is not available on YouTube Playables.
    /// Feature-detect via HasInterstitialApi/HasRewardedApi protects against SDK breaking changes.
    /// </summary>
    [Preserve]
    public class YouTubePlayablesAdSource : IAdSource
    {
        [DllImport("__Internal")] private static extern void YTPlayables_RequestInterstitialAd(Action<int> onSuccess, Action<int> onFail);
        [DllImport("__Internal")] private static extern int  YTPlayables_InPlayablesEnv();
        [DllImport("__Internal")] private static extern int  YTPlayables_HasInterstitialApi();
        [DllImport("__Internal")] private static extern int  YTPlayables_HasRewardedApi();
        [DllImport("__Internal")] private static extern void YTPlayables_RequestRewardedAd(
            string rewardId, Action<int> onEarned, Action<int> onFail);

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

        private bool? _interstitialSupportedCache;
        public bool IsInterstitialSupported
        {
            get
            {
                if (_interstitialSupportedCache.HasValue) return _interstitialSupportedCache.Value;
                var has = YTPlayables_HasInterstitialApi() == 1;
                _interstitialSupportedCache = has;
                VerboseLog.Log("YT:Ad", $"IsInterstitialSupported first-check: hasApi={has}");
                return has;
            }
        }

        private bool? _rewardedSupportedCache;
        public bool IsRewardedSupported
        {
            get
            {
                if (_rewardedSupportedCache.HasValue) return _rewardedSupportedCache.Value;
                var has = YTPlayables_HasRewardedApi() == 1;
                _rewardedSupportedCache = has;
                VerboseLog.Log("YT:Ad", $"IsRewardedSupported first-check: hasApi={has}");
                return has;
            }
        }

        public bool IsBannerSupported => false; // YouTube Playables does not expose banner slots.

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
        private static bool _rewardedReadyLogged;
        public bool IsRewardReady()
        {
            var inEnv = YTPlayables_InPlayablesEnv() == 1;
            var ready = _initialized && inEnv && IsRewardedSupported;
            if (!_rewardedReadyLogged)
            {
                _rewardedReadyLogged = true;
                VerboseLog.Log("YT:Ad", $"IsRewardReady first-check: init={_initialized}, inEnv={inEnv}, supported={IsRewardedSupported} → {ready}");
            }
            return ready;
        }
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

        private static Action<AdStatus> _rewardedCallback;

        /// <summary>
        /// Shows a rewarded ad via ytgame.ads.requestRewardedAd (YouTube Playables preview API).
        /// </summary>
        /// <param name="endCallback">
        /// Called with <see cref="AdStatus.Completed"/> if the user earned the reward,
        /// <see cref="AdStatus.Failed"/> if the user closed the ad early or the request failed.
        /// </param>
        /// <param name="placementName">
        /// Used as YouTube's <c>rewardId</c> (stable unique identifier for the reward type).
        /// MUST be non-empty and consistent across uses for the same reward — YouTube tracks analytics per rewardId
        /// and may reject unregistered ids. Register each rewardId in the YouTube Playables Developer Portal.
        /// Good: "extra-life-v1", "booster-destroy-v1". Bad: random GUID per call.
        /// Note: on non-YouTube adapters (LevelPlay / Yandex / Playgama) this parameter keeps its usual "placement name" semantics.
        /// </param>
        public void ShowReward(Action<AdStatus> endCallback, string placementName = "")
        {
            VerboseLog.Log("YT:Ad", $"ShowReward enter (rewardId=\"{placementName}\", initialized={_initialized}, inEnv={YTPlayables_InPlayablesEnv() == 1})");
            if (!_initialized)
            {
                VerboseLog.Warn("YT:Ad", "ShowReward: adapter not initialized → Failed");
                endCallback?.Invoke(AdStatus.Failed);
                OnRewardClosed?.Invoke();
                return;
            }
            if (YTPlayables_InPlayablesEnv() != 1)
            {
                VerboseLog.Warn("YT:Ad", "ShowReward: not in Playables env → Failed");
                endCallback?.Invoke(AdStatus.Failed);
                OnRewardClosed?.Invoke();
                return;
            }
            if (!IsRewardedSupported)
            {
                VerboseLog.Warn("YT:Ad", "ShowReward: SDK does not expose requestRewardedAd → Failed");
                endCallback?.Invoke(AdStatus.Failed);
                OnRewardClosed?.Invoke();
                return;
            }
            if (string.IsNullOrEmpty(placementName))
            {
                VerboseLog.Warn("YT:Ad", "ShowReward: placementName (rewardId) is empty → Failed. YouTube spec: rewardId must be non-empty and stable.");
                endCallback?.Invoke(AdStatus.Failed);
                OnRewardClosed?.Invoke();
                return;
            }

            if (_rewardedCallback != null)
                VerboseLog.Warn("YT:Ad", "ShowReward: previous callback not drained — overwriting");

            _rewardedCallback = endCallback;
            VerboseLog.Log("YT:Ad", "→ YTPlayables_RequestRewardedAd dispatching");
            YTPlayables_RequestRewardedAd(placementName, OnRewardEarned, OnRewardFail);
        }

        [Preserve, MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnRewardEarned(int earned)
        {
            VerboseLog.Log("YT:Ad", $"← RequestRewardedAd success earned={earned}");
            var cb = _rewardedCallback;
            _rewardedCallback = null;
            cb?.Invoke(earned == 1 ? AdStatus.Completed : AdStatus.Failed);
            _instance?.OnRewardClosed?.Invoke();
        }

        [Preserve, MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnRewardFail(int _)
        {
            VerboseLog.Warn("YT:Ad", "← RequestRewardedAd failed");
            var cb = _rewardedCallback;
            _rewardedCallback = null;
            cb?.Invoke(AdStatus.Failed);
            _instance?.OnRewardClosed?.Invoke();
        }

        public void ShowBanner()    { VerboseLog.Log("YT:Ad", "ShowBanner called — not supported on YT Playables, no-op"); }
        public void HideBanner()    { VerboseLog.Log("YT:Ad", "HideBanner called — not supported on YT Playables, no-op"); }
        public void DestroyBanner() { VerboseLog.Log("YT:Ad", "DestroyBanner called — not supported on YT Playables, no-op"); }
        public void EnableYoungMode()  { VerboseLog.Log("YT:Ad", "EnableYoungMode called — no-op on YT Playables"); }
        public void DisableYoungMode() { VerboseLog.Log("YT:Ad", "DisableYoungMode called — no-op on YT Playables"); }
    }
}
#endif
