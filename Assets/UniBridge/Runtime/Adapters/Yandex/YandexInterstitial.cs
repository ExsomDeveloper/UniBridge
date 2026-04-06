#if UNIBRIDGE_YANDEX
using System;
using UnityEngine;
using YandexMobileAds;
using YandexMobileAds.Base;

namespace UniBridge
{
    public class YandexInterstitial : IDisposable
    {
        public event Action OnAdLoaded;
        public event Action OnAdClosed;

        private readonly float _baseRetryDelaySeconds = 10f;
        private readonly float _maxRetryDelaySeconds = 120f;
        private readonly string _adUnitId;
        private InterstitialAdLoader _loader;
        private Interstitial _currentAd;
        private bool _isDisposed;
        private bool _isLoaded;
        private bool _isLoading;
        private bool _retryPending;
        private bool _advertisingShowing;
        private int _retryAttempt;
        private Action<AdStatus> _callback;

        public YandexInterstitial(string adUnitId)
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

        public void ShowAd(Action<AdStatus> endCallback, string placementName = "")
        {
            if (_advertisingShowing)
            {
                Debug.LogWarning($"[{nameof(YandexInterstitial)}]: Interstitial already showing.");
                endCallback?.Invoke(AdStatus.AlreadyShowing);
                return;
            }

            if (_isDisposed || !IsReady())
            {
                Debug.LogWarning($"[{nameof(YandexInterstitial)}]: Ad not ready.");
                endCallback?.Invoke(AdStatus.NotLoaded);
                LoadAd();
                return;
            }

            _callback = endCallback;
            _advertisingShowing = true;
            _currentAd.Show();
        }

        private void TryLoadOnce()
        {
            Debug.Log($"[{nameof(YandexInterstitial)}]: Attempting to load interstitial ad...");
            _isLoading = true;

            CleanupLoader();
            _loader = new InterstitialAdLoader();
            _loader.OnAdLoaded += HandleAdLoaded;
            _loader.OnAdFailedToLoad += HandleAdFailedToLoad;

            var config = new AdRequestConfiguration.Builder(_adUnitId).Build();
            _loader.LoadAd(config);
        }

        private void HandleAdLoaded(object sender, InterstitialAdLoadedEventArgs args)
        {
            Debug.Log($"[{nameof(YandexInterstitial)}]: Interstitial ad successfully loaded.");
            _currentAd = args.Interstitial;
            _currentAd.OnAdDismissed += HandleAdDismissed;
            _currentAd.OnAdFailedToShow += HandleAdFailedToShow;
            _currentAd.OnAdShown += HandleAdShown;
            _currentAd.OnAdClicked += HandleAdClicked;

            _isLoaded = true;
            _isLoading = false;
            _retryAttempt = 0;
            OnAdLoaded?.Invoke();
        }

        private void HandleAdFailedToLoad(object sender, AdFailedToLoadEventArgs args)
        {
            Debug.LogWarning($"[{nameof(YandexInterstitial)}]: Ad load failed: {args.Message}");
            _isLoaded = false;
            _isLoading = false;

            if (_isDisposed)
                return;

            _retryAttempt++;
            float delay = GetRetryDelay();
            _retryPending = true;
            Debug.LogWarning($"[{nameof(YandexInterstitial)}]: Retrying in {delay:F1} seconds (attempt {_retryAttempt})");
            RetryHelper.InvokeAfter(delay, RetryLoad);
        }

        private void HandleAdDismissed(object sender, EventArgs args)
        {
            Debug.Log($"[{nameof(YandexInterstitial)}]: Ad dismissed.");
            _advertisingShowing = false;
            _isLoaded = false;
            _callback?.Invoke(AdStatus.Completed);
            _callback = null;
            OnAdClosed?.Invoke();
            CleanupCurrentAd();
            LoadAd();
        }

        private void HandleAdFailedToShow(object sender, AdFailureEventArgs args)
        {
            Debug.LogWarning($"[{nameof(YandexInterstitial)}]: Ad failed to show: {args.Message}");
            _advertisingShowing = false;
            _isLoaded = false;
            _callback?.Invoke(AdStatus.Failed);
            _callback = null;
            CleanupCurrentAd();
            LoadAd();
        }

        private void HandleAdShown(object sender, EventArgs args)
        {
            Debug.Log($"[{nameof(YandexInterstitial)}]: Ad shown.");
        }

        private void HandleAdClicked(object sender, EventArgs args)
        {
            Debug.Log($"[{nameof(YandexInterstitial)}]: Ad clicked.");
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
