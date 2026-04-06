#if UNIBRIDGE_LEVELPLAY
using System;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace UniBridge
{
    public class LevelPlayInterstitial : IDisposable
    {
        public event Action OnAdLoaded;
        public event Action OnAdClosed;
        public event Action OnAdClicked;
        public event Action OnAdDisplayed;

        private readonly float _baseRetryDelaySeconds = 10f;
        private readonly float _maxRetryDelaySeconds = 120f;
        private readonly LevelPlayInterstitialAd _interstitialAd;
        private bool _isDisposed;
        private bool _isLoaded;
        private bool _isLoading;
        private bool _retryPending;
        private bool _advertisingShowing;
        private int _retryAttempt;
        private Action<AdStatus> _callback;

        public LevelPlayInterstitial(string adUnitId)
        {
            _interstitialAd = new LevelPlayInterstitialAd(adUnitId);
            _interstitialAd.OnAdLoaded += InterstitialOnAdLoadedEvent;
            _interstitialAd.OnAdLoadFailed += InterstitialOnAdLoadFailedEvent;
            _interstitialAd.OnAdDisplayed += InterstitialOnAdDisplayedEvent;
            _interstitialAd.OnAdDisplayFailed += InterstitialOnAdDisplayFailedEvent;
            _interstitialAd.OnAdClicked += InterstitialOnAdClickedEvent;
            _interstitialAd.OnAdClosed += InterstitialOnAdClosedEvent;
            LoadAd();
        }

        public void LoadAd()
        {
            if (_isDisposed || _isLoaded || _isLoading || _retryPending)
                return;

            if (_interstitialAd.IsAdReady())
                return;

            TryLoadOnce();
        }

        public bool IsReady()
        {
            return _interstitialAd.IsAdReady();
        }

        private void TryLoadOnce()
        {
            Debug.Log($"[{nameof(LevelPlayInterstitial)}]: Attempting to load interstitial ad...");
            _isLoading = true;
            _interstitialAd.LoadAd();
        }

        public void ShowAd(Action<AdStatus> endCallback, string placementName = "")
        {
            _callback = null;
            if (_advertisingShowing)
            {
                Debug.LogWarning($"[{nameof(LevelPlayInterstitial)}]: Interstitial showing.");
                endCallback?.Invoke(AdStatus.AlreadyShowing);
                return;
            }

            if (_isDisposed ||
                !_interstitialAd.IsAdReady() ||
                LevelPlayInterstitialAd.IsPlacementCapped(placementName))
            {
                Debug.LogWarning($"[{nameof(LevelPlayInterstitial)}]: Ad not ready or placement capped.");
                endCallback?.Invoke(AdStatus.NotLoaded);

                LoadAd();
                return;
            }

            _callback = endCallback;
            _advertisingShowing = true;
            _interstitialAd.ShowAd(placementName);
        }

        private void InterstitialOnAdLoadedEvent(LevelPlayAdInfo info)
        {
            Debug.Log($"[{nameof(LevelPlayInterstitial)}]: Interstitial ad successfully loaded.");
            _isLoaded = true;
            _isLoading = false;
            _retryAttempt = 0;
            OnAdLoaded?.Invoke();
        }

        private void InterstitialOnAdLoadFailedEvent(LevelPlayAdError error)
        {
            Debug.LogWarning($"[{nameof(LevelPlayInterstitial)}]: Ad load failed: {error.ErrorMessage}");
            _isLoaded = false;
            _isLoading = false;

            if (_isDisposed)
                return;

            _retryAttempt++;

            float delay = GetRetryDelay();
            _retryPending = true;
            Debug.LogWarning(
                $"[{nameof(LevelPlayInterstitial)}]: Retrying in {delay:F1} seconds (attempt {_retryAttempt})"
            );

            RetryHelper.InvokeAfter(delay, RetryLoad);
        }

        private void InterstitialOnAdDisplayFailedEvent(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            Debug.LogWarning($"[{nameof(LevelPlayInterstitial)}]: Display failed: {error.ErrorMessage}");
            _advertisingShowing = false;
            _isLoaded = false;
            _callback?.Invoke(AdStatus.Failed);
            LoadAd();
        }

        private void InterstitialOnAdDisplayedEvent(LevelPlayAdInfo info)
        {
            Debug.Log($"[{nameof(LevelPlayInterstitial)}]: Ad displayed.");
            OnAdDisplayed?.Invoke();
        }

        private void InterstitialOnAdClickedEvent(LevelPlayAdInfo info)
        {
            Debug.Log($"[{nameof(LevelPlayInterstitial)}]: Ad clicked.");
            OnAdClicked?.Invoke();
        }

        private void InterstitialOnAdClosedEvent(LevelPlayAdInfo info)
        {
            Debug.Log($"[{nameof(LevelPlayInterstitial)}]: Ad closed.");

            _advertisingShowing = false;
            _isLoaded = false;

            _callback?.Invoke(AdStatus.Completed);
            _callback = null;

            OnAdClosed?.Invoke();
            LoadAd();
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

            // Exponential Backoff + Jitter
            return UnityEngine.Random.Range(0f, cappedDelay);
        }

        public void Dispose()
        {
            _isDisposed = true;
            _interstitialAd.OnAdLoaded -= InterstitialOnAdLoadedEvent;
            _interstitialAd.OnAdLoadFailed -= InterstitialOnAdLoadFailedEvent;
            _interstitialAd.OnAdDisplayed -= InterstitialOnAdDisplayedEvent;
            _interstitialAd.OnAdDisplayFailed -= InterstitialOnAdDisplayFailedEvent;
            _interstitialAd.OnAdClicked -= InterstitialOnAdClickedEvent;
            _interstitialAd.OnAdClosed -= InterstitialOnAdClosedEvent;
        }
    }
}
#endif
