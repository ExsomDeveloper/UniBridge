#if UNIBRIDGE_LEVELPLAY
using System;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace UniBridge
{
    public class LevelPlayRewardWrapper : IDisposable
    {
        public event Action OnAdLoaded;
        public event Action OnAdClosed;
        public event Action OnRewardEarned;

        private readonly float _baseRetryDelaySeconds = 10f;
        private readonly float _maxRetryDelaySeconds = 120f;
        private readonly LevelPlayRewardedAd _rewardedAd;
        private bool _isDisposed;
        private bool _isLoaded;
        private bool _isLoading;
        private bool _retryPending;
        private bool _advertisingShowing;
        private int _retryAttempt;
        private bool _rewardReceived;
        private Action<AdStatus> _callback;

        public LevelPlayRewardWrapper(string adUnitId)
        {
            _rewardedAd = new LevelPlayRewardedAd(adUnitId);
            _rewardedAd.OnAdLoaded += RewardedOnAdLoadedEvent;
            _rewardedAd.OnAdLoadFailed += RewardedOnAdLoadFailedEvent;
            _rewardedAd.OnAdRewarded += RewardedOnAdRewardedEvent;
            _rewardedAd.OnAdClosed += RewardedOnAdClosedEvent;
            LoadAd();
        }

        private void RewardedOnAdRewardedEvent(LevelPlayAdInfo info, LevelPlayReward reward)
        {
            Debug.Log($"[{nameof(LevelPlayRewardWrapper)}]: Reward earned: {reward}");
            _rewardReceived = true;
            OnRewardEarned?.Invoke();
        }

        public void LoadAd()
        {
            if (_isDisposed || _isLoaded || _isLoading || _retryPending)
                return;

            if (_rewardedAd.IsAdReady())
                return;

            TryLoadOnce();
        }

        public bool IsReady()
        {
            return _rewardedAd.IsAdReady();
        }

        private void TryLoadOnce()
        {
            Debug.Log($"[{nameof(LevelPlayRewardWrapper)}]: Attempting to load rewarded ad...");
            _isLoading = true;
            _rewardedAd.LoadAd();
        }

        private float GetRetryDelay()
        {
            float exponentialDelay = _baseRetryDelaySeconds * Mathf.Pow(2, _retryAttempt);
            float cappedDelay = Mathf.Min(exponentialDelay, _maxRetryDelaySeconds);

            // Exponential Backoff + Jitter
            return UnityEngine.Random.Range(0f, cappedDelay);
        }

        public void ShowReward(Action<AdStatus> endCallback, string placementName = "")
        {
            if (_advertisingShowing)
            {
                endCallback?.Invoke(AdStatus.AlreadyShowing);
                return;
            }

            if (_isDisposed ||
                !_rewardedAd.IsAdReady() ||
                LevelPlayRewardedAd.IsPlacementCapped(placementName))
            {
                endCallback?.Invoke(AdStatus.NotLoaded);
                LoadAd();
                return;
            }

            _callback = endCallback;
            _advertisingShowing = true;
            _rewardReceived = false;
            _rewardedAd.ShowAd(placementName);
        }

        private void RewardedOnAdLoadedEvent(LevelPlayAdInfo info)
        {
            Debug.Log($"[{nameof(LevelPlayRewardWrapper)}]: Rewarded ad successfully loaded.");
            _isLoaded = true;
            _isLoading = false;
            _retryAttempt = 0;
            OnAdLoaded?.Invoke();
        }

        private void RewardedOnAdLoadFailedEvent(LevelPlayAdError error)
        {
            Debug.LogWarning($"[{nameof(LevelPlayRewardWrapper)}]: Ad load failed: {error.ErrorMessage}");
            _isLoaded = false;
            _isLoading = false;

            if (_isDisposed)
                return;

            _retryAttempt++;

            float delay = GetRetryDelay();
            _retryPending = true;
            Debug.LogWarning(
                $"[{nameof(LevelPlayRewardWrapper)}]: Retrying in {delay:F1} seconds (attempt {_retryAttempt})"
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

        private void RewardedOnAdClosedEvent(LevelPlayAdInfo info)
        {
            _advertisingShowing = false;
            _isLoaded = false;

            if (_rewardReceived)
            {
                Debug.Log($"[{nameof(LevelPlayRewardWrapper)}]: Ad closed with status - completed");
                _callback?.Invoke(AdStatus.Completed);
            }
            else
            {
                Debug.Log($"[{nameof(LevelPlayRewardWrapper)}]: Ad closed with status - canceled");
                _callback?.Invoke(AdStatus.Canceled);
            }

            _callback = null;
            _rewardReceived = false;

            OnAdClosed?.Invoke();
            LoadAd();
        }

        public void Dispose()
        {
            _isDisposed = true;
            _rewardedAd.OnAdLoaded -= RewardedOnAdLoadedEvent;
            _rewardedAd.OnAdLoadFailed -= RewardedOnAdLoadFailedEvent;
            _rewardedAd.OnAdRewarded -= RewardedOnAdRewardedEvent;
            _rewardedAd.OnAdClosed -= RewardedOnAdClosedEvent;
        }
    }
}
#endif
