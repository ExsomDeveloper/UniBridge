#if UNIBRIDGE_YANDEX
using System;
using UnityEngine;
using YandexMobileAds;
using YandexMobileAds.Base;

namespace UniBridge
{
    public class YandexRewardWrapper : IDisposable
    {
        public event Action OnAdLoaded;
        public event Action OnAdClosed;

        private readonly float _baseRetryDelaySeconds = 10f;
        private readonly float _maxRetryDelaySeconds = 120f;
        private readonly string _adUnitId;
        private RewardedAdLoader _loader;
        private RewardedAd _currentAd;
        private bool _isDisposed;
        private bool _isLoaded;
        private bool _isLoading;
        private bool _retryPending;
        private bool _advertisingShowing;
        private bool _rewardReceived;
        private int _retryAttempt;
        private Action<AdStatus> _callback;

        public YandexRewardWrapper(string adUnitId)
        {
            _adUnitId = adUnitId;
            LoadAd();
        }

        public bool IsReady() => _isLoaded && _currentAd != null;

        public void LoadAd()
        {
            if (_isDisposed || _isLoaded || _isLoading || _retryPending)
                return;

            TryLoadOnce();
        }

        public void ShowReward(Action<AdStatus> endCallback, string placementName = "")
        {
            if (_advertisingShowing)
            {
                Debug.LogWarning($"[{nameof(YandexRewardWrapper)}]: Rewarded ad already showing.");
                endCallback?.Invoke(AdStatus.AlreadyShowing);
                return;
            }

            if (_isDisposed || !IsReady())
            {
                Debug.LogWarning($"[{nameof(YandexRewardWrapper)}]: Ad not ready.");
                endCallback?.Invoke(AdStatus.NotLoaded);
                LoadAd();
                return;
            }

            _callback = endCallback;
            _advertisingShowing = true;
            _rewardReceived = false;
            _currentAd.Show();
        }

        private void TryLoadOnce()
        {
            Debug.Log($"[{nameof(YandexRewardWrapper)}]: Attempting to load rewarded ad...");
            _isLoading = true;

            CleanupLoader();
            _loader = new RewardedAdLoader();
            _loader.OnAdLoaded += HandleAdLoaded;
            _loader.OnAdFailedToLoad += HandleAdFailedToLoad;

            var config = new AdRequestConfiguration.Builder(_adUnitId).Build();
            _loader.LoadAd(config);
        }

        private void HandleAdLoaded(object sender, RewardedAdLoadedEventArgs args)
        {
            Debug.Log($"[{nameof(YandexRewardWrapper)}]: Rewarded ad successfully loaded.");
            _currentAd = args.RewardedAd;
            _currentAd.OnAdDismissed += HandleAdDismissed;
            _currentAd.OnAdFailedToShow += HandleAdFailedToShow;
            _currentAd.OnAdShown += HandleAdShown;
            _currentAd.OnAdClicked += HandleAdClicked;
            _currentAd.OnRewarded += HandleRewarded;

            _isLoaded = true;
            _isLoading = false;
            _retryAttempt = 0;
            OnAdLoaded?.Invoke();
        }

        private void HandleAdFailedToLoad(object sender, AdFailedToLoadEventArgs args)
        {
            Debug.LogWarning($"[{nameof(YandexRewardWrapper)}]: Ad load failed: {args.Message}");
            _isLoaded = false;
            _isLoading = false;

            if (_isDisposed)
                return;

            _retryAttempt++;
            float delay = GetRetryDelay();
            _retryPending = true;
            Debug.LogWarning($"[{nameof(YandexRewardWrapper)}]: Retrying in {delay:F1} seconds (attempt {_retryAttempt})");
            RetryHelper.InvokeAfter(delay, RetryLoad);
        }

        private void HandleRewarded(object sender, Reward args)
        {
            Debug.Log($"[{nameof(YandexRewardWrapper)}]: Reward earned: {args.amount} {args.type}");
            _rewardReceived = true;
        }

        private void HandleAdDismissed(object sender, EventArgs args)
        {
            _advertisingShowing = false;
            _isLoaded = false;

            if (_rewardReceived)
            {
                Debug.Log($"[{nameof(YandexRewardWrapper)}]: Ad closed with status - completed");
                _callback?.Invoke(AdStatus.Completed);
            }
            else
            {
                Debug.Log($"[{nameof(YandexRewardWrapper)}]: Ad closed with status - canceled");
                _callback?.Invoke(AdStatus.Canceled);
            }

            _callback = null;
            _rewardReceived = false;
            OnAdClosed?.Invoke();
            CleanupCurrentAd();
            LoadAd();
        }

        private void HandleAdFailedToShow(object sender, AdFailureEventArgs args)
        {
            Debug.LogWarning($"[{nameof(YandexRewardWrapper)}]: Ad failed to show: {args.Message}");
            _advertisingShowing = false;
            _isLoaded = false;
            _callback?.Invoke(AdStatus.Failed);
            _callback = null;
            _rewardReceived = false;
            CleanupCurrentAd();
            LoadAd();
        }

        private void HandleAdShown(object sender, EventArgs args)
        {
            Debug.Log($"[{nameof(YandexRewardWrapper)}]: Ad shown.");
        }

        private void HandleAdClicked(object sender, EventArgs args)
        {
            Debug.Log($"[{nameof(YandexRewardWrapper)}]: Ad clicked.");
        }

        private void RetryLoad()
        {
            _retryPending = false;
            if (_isDisposed || _isLoaded || _isLoading)
                return;
            TryLoadOnce();
        }

        private float GetRetryDelay()
        {
            float exponentialDelay = _baseRetryDelaySeconds * Mathf.Pow(2, _retryAttempt);
            float cappedDelay = Mathf.Min(exponentialDelay, _maxRetryDelaySeconds);
            return UnityEngine.Random.Range(0f, cappedDelay);
        }

        private void CleanupLoader()
        {
            if (_loader == null)
                return;
            _loader.OnAdLoaded -= HandleAdLoaded;
            _loader.OnAdFailedToLoad -= HandleAdFailedToLoad;
            _loader = null;
        }

        private void CleanupCurrentAd()
        {
            if (_currentAd == null)
                return;
            _currentAd.OnAdDismissed -= HandleAdDismissed;
            _currentAd.OnAdFailedToShow -= HandleAdFailedToShow;
            _currentAd.OnAdShown -= HandleAdShown;
            _currentAd.OnAdClicked -= HandleAdClicked;
            _currentAd.OnRewarded -= HandleRewarded;
            _currentAd.Destroy();
            _currentAd = null;
        }

        public void Dispose()
        {
            _isDisposed = true;
            CleanupLoader();
            CleanupCurrentAd();
        }
    }
}
#endif
