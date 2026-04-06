#if UNIBRIDGE_LEVELPLAY
using System;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace UniBridge
{
    public class LevelPlayBanner : IDisposable
    {
        public event Action OnAdLoaded;
        public event Action OnAdLoadFailed;

        private readonly float _baseRetryDelaySeconds = 10f;
        private readonly float _maxRetryDelaySeconds = 120f;
        private readonly LevelPlayBannerAd _bannerAd;
        private bool _isDisposed;
        private bool _isLoaded;
        private bool _isLoading;
        private bool _retryPending;
        private int _retryAttempt;
        private bool _isShowed;

        public LevelPlayBanner(string adUnitId)
        {
            _bannerAd = new LevelPlayBannerAd(adUnitId);
            _bannerAd.OnAdLoaded += BannerOnAdLoadedEvent;
            _bannerAd.OnAdLoadFailed += BannerOnAdLoadFailedEvent;
            LoadAd();
            _bannerAd.HideAd();
        }

        public void LoadAd()
        {
            if (_isDisposed || _isLoaded || _isLoading || _retryPending)
                return;

            TryLoadOnce();
        }

        public bool IsReady()
        {
            return _isLoaded;
        }

        public void Show()
        {
            if (_isDisposed)
                return;

            if (_isLoaded)
            {
                _isShowed = true;
                _bannerAd.ShowAd();
            }
            else
            {
                LoadAd();
            }
        }

        public void Hide()
        {
            if (_isDisposed)
                return;

            _isShowed = false;
            _bannerAd.HideAd();
        }

        public void Destroy()
        {
            if (_isDisposed)
                return;

            _bannerAd.DestroyAd();
        }

        private void TryLoadOnce()
        {
            Debug.Log($"[{nameof(LevelPlayBanner)}]: Attempting to load banner ad...");
            _isLoading = true;
            _bannerAd.LoadAd();
        }

        private float GetRetryDelay()
        {
            float exponentialDelay = _baseRetryDelaySeconds * Mathf.Pow(2, _retryAttempt);
            float cappedDelay = Mathf.Min(exponentialDelay, _maxRetryDelaySeconds);

            // Exponential Backoff + Jitter
            return UnityEngine.Random.Range(0f, cappedDelay);
        }

        private void BannerOnAdLoadedEvent(LevelPlayAdInfo info)
        {
            Debug.Log($"[{nameof(LevelPlayBanner)}]: Banner ad successfully loaded.");

            _isLoaded = true;
            _isLoading = false;
            _retryAttempt = 0;

            if (_isShowed)
            {
                Show();
            }
            else
            {
                Hide();
            }

            OnAdLoaded?.Invoke();
        }

        private void BannerOnAdLoadFailedEvent(LevelPlayAdError error)
        {
            Debug.LogWarning($"[{nameof(LevelPlayBanner)}]: Banner load failed: {error.ErrorMessage}");

            _isLoaded = false;
            _isLoading = false;

            OnAdLoadFailed?.Invoke();

            if (_isDisposed)
                return;

            _retryAttempt++;

            float delay = GetRetryDelay();
            _retryPending = true;
            Debug.LogWarning(
                $"[{nameof(LevelPlayBanner)}]: Retrying in {delay:F1} seconds (attempt {_retryAttempt})"
            );

            RetryHelper.InvokeAfter(delay, RetryLoad);
        }

        private void RetryLoad()
        {
            _retryPending = false;
            if (_isDisposed || _isLoaded || _isLoading)
                return;

            TryLoadOnce();
        }

        public void Dispose()
        {
            _isDisposed = true;

            _bannerAd.OnAdLoaded -= BannerOnAdLoadedEvent;
            _bannerAd.OnAdLoadFailed -= BannerOnAdLoadFailedEvent;
        }
    }
}
#endif
