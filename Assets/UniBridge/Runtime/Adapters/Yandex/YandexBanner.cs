#if UNIBRIDGE_YANDEX
using System;
using UnityEngine;
using YandexMobileAds;
using YandexMobileAds.Base;

namespace UniBridge
{
    public class YandexBanner : IDisposable
    {
        public event Action OnAdLoaded;
        public event Action OnAdLoadFailed;

        private readonly float _baseRetryDelaySeconds = 10f;
        private readonly float _maxRetryDelaySeconds = 120f;
        private Banner _banner;
        private bool _isDisposed;
        private bool _isLoaded;
        private bool _isLoading;
        private bool _retryPending;
        private bool _isShowed;
        private int _retryAttempt;

        public YandexBanner(string adUnitId)
        {
            int screenWidthDp = ScreenUtils.ConvertPixelsToDp(Screen.width);
            _banner = new Banner(adUnitId, BannerAdSize.StickySize(screenWidthDp), AdPosition.BottomCenter);
            _banner.OnAdLoaded += HandleAdLoaded;
            _banner.OnAdFailedToLoad += HandleAdFailedToLoad;
            LoadAd();
        }

        public bool IsReady() => _isLoaded;

        public void LoadAd()
        {
            if (_isDisposed || _isLoaded || _isLoading || _retryPending)
                return;

            TryLoadOnce();
        }

        public void Show()
        {
            if (_isDisposed)
                return;

            _isShowed = true;
            if (_isLoaded)
                _banner.Show();
            else
                LoadAd();
        }

        public void Hide()
        {
            if (_isDisposed)
                return;

            _isShowed = false;
            _banner.Hide();
        }

        public void Destroy()
        {
            if (_isDisposed)
                return;

            _banner.Destroy();
        }

        private void TryLoadOnce()
        {
            Debug.Log($"[{nameof(YandexBanner)}]: Attempting to load banner ad...");
            _isLoading = true;
            _banner.LoadAd(new AdRequest.Builder().Build());
        }

        private void HandleAdLoaded(object sender, EventArgs args)
        {
            Debug.Log($"[{nameof(YandexBanner)}]: Banner ad successfully loaded.");
            _isLoaded = true;
            _isLoading = false;
            _retryAttempt = 0;

            if (_isShowed)
                _banner.Show();
            else
                _banner.Hide();

            OnAdLoaded?.Invoke();
        }

        private void HandleAdFailedToLoad(object sender, AdFailureEventArgs args)
        {
            Debug.LogWarning($"[{nameof(YandexBanner)}]: Banner load failed: {args.Message}");
            _isLoaded = false;
            _isLoading = false;
            OnAdLoadFailed?.Invoke();

            if (_isDisposed)
                return;

            _retryAttempt++;
            float delay = GetRetryDelay();
            _retryPending = true;
            Debug.LogWarning($"[{nameof(YandexBanner)}]: Retrying in {delay:F1} seconds (attempt {_retryAttempt})");
            RetryHelper.InvokeAfter(delay, RetryLoad);
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

        public void Dispose()
        {
            _isDisposed = true;
            _banner.OnAdLoaded -= HandleAdLoaded;
            _banner.OnAdFailedToLoad -= HandleAdFailedToLoad;
        }
    }
}
#endif
