using System;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace UniBridge
{
    [Preserve]
    public class DebugAdSource : MonoBehaviour, IAdSource
    {
        [Header("Interstitial")]
        [SerializeField] private GameObject _interstitialPanel;
        [SerializeField] private Button _interstitialCloseButton;

        [Header("Reward")]
        [SerializeField] private GameObject _rewardPanel;
        [SerializeField] private Button _rewardCloseButton;
        [SerializeField] private Button _rewardGetRewardButton;

        [Header("Banner")]
        [SerializeField] private GameObject _bannerPanel;

        private DateTime _lastClickTime;
        private int _clickCount = 0;
        private bool _testAdDisabled = false;
        private bool _bannerWasShowing = false;

        public event Action OnInterstitialClosed;
        public event Action OnRewardClosed;
        public event Action OnBannerLoaded;

        public void Initialize(Action onInitSuccess, Action onInitFailed)
        {
            _lastClickTime = DateTime.Now;

            if (_interstitialPanel != null)
                _interstitialPanel.SetActive(false);
            if (_rewardPanel != null)
                _rewardPanel.SetActive(false);
            if (_bannerPanel != null)
                _bannerPanel.SetActive(false);

            onInitSuccess?.Invoke();
            OnBannerLoaded?.Invoke();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var delta = DateTime.Now.Subtract(_lastClickTime);
                if (delta.Seconds < 1)
                    _clickCount++;
                else
                    _clickCount = 1;

                _lastClickTime = DateTime.Now;
                if (_clickCount >= 6)
                {
                    _clickCount = 0;
                    _testAdDisabled = !_testAdDisabled;
                    Debug.Log($"[{nameof(DebugAdSource)}] Test ads {(_testAdDisabled ? "disabled" : "enabled")}");
                    if (_testAdDisabled)
                    {
                        _bannerWasShowing = _bannerPanel != null && _bannerPanel.activeSelf;
                        if (_bannerPanel != null)
                            _bannerPanel.SetActive(false);
                    }
                    else if (_bannerWasShowing && _bannerPanel != null)
                    {
                        _bannerPanel.SetActive(true);
                    }
                }
            }
        }

        public void ShowInterstitial(Action<AdStatus> endCallback, string placementName = "")
        {
            if (_testAdDisabled)
            {
                endCallback?.Invoke(AdStatus.Completed);
                return;
            }

            Debug.Log($"[{nameof(DebugAdSource)}] Show <Interstitial> ad!");

            var config = Resources.Load<UniBridgeConfig>("UniBridgeConfig");
            bool useCountdown = config != null
                && config.PlaygamaSettings.SimulateYandexCountdownInEditor
                && config.PlaygamaSettings.YandexInterstitialCountdownSeconds > 0;

            if (useCountdown)
            {
                var go = new GameObject("[DebugAds] InterstitialCountdown");
                var overlay = go.AddComponent<InterstitialCountdownOverlay>();
                overlay.Begin(config.PlaygamaSettings.YandexInterstitialCountdownSeconds,
                    () => ShowInterstitialPanel(endCallback));
            }
            else
            {
                ShowInterstitialPanel(endCallback);
            }
        }

        private void ShowInterstitialPanel(Action<AdStatus> endCallback)
        {
            if (_interstitialPanel == null)
            {
                Debug.Log($"[{nameof(DebugAdSource)}] Interstitial panel not configured, auto-completing.");
                endCallback?.Invoke(AdStatus.Completed);
                OnInterstitialClosed?.Invoke();
                return;
            }

            _interstitialPanel.SetActive(true);

            void CloseHandler()
            {
                _interstitialCloseButton.onClick.RemoveListener(CloseHandler);
                _interstitialPanel.SetActive(false);
                Debug.Log($"[{nameof(DebugAdSource)}] Interstitial ad - Completed!");
                endCallback?.Invoke(AdStatus.Completed);
                OnInterstitialClosed?.Invoke();
            }

            _interstitialCloseButton.onClick.AddListener(CloseHandler);
        }

        public void ShowReward(Action<AdStatus> endCallback, string placementName = "")
        {
            if (_testAdDisabled)
            {
                endCallback?.Invoke(AdStatus.Completed);
                return;
            }

            Debug.Log($"[{nameof(DebugAdSource)}] Show <Reward> ad!");

            if (_rewardPanel == null)
            {
                Debug.Log($"[{nameof(DebugAdSource)}] Reward panel not configured, auto-completing.");
                endCallback?.Invoke(AdStatus.Completed);
                OnRewardClosed?.Invoke();
                return;
            }

            _rewardPanel.SetActive(true);

            void CloseHandler()
            {
                _rewardCloseButton.onClick.RemoveListener(CloseHandler);
                _rewardGetRewardButton.onClick.RemoveListener(RewardHandler);
                _rewardPanel.SetActive(false);
                Debug.Log($"[{nameof(DebugAdSource)}] Reward ad - Failed/Canceled!");
                endCallback?.Invoke(AdStatus.Canceled);
                OnRewardClosed?.Invoke();
            }

            void RewardHandler()
            {
                _rewardCloseButton.onClick.RemoveListener(CloseHandler);
                _rewardGetRewardButton.onClick.RemoveListener(RewardHandler);
                _rewardPanel.SetActive(false);
                Debug.Log($"[{nameof(DebugAdSource)}] Reward ad - Completed!");
                endCallback?.Invoke(AdStatus.Completed);
                OnRewardClosed?.Invoke();
            }

            _rewardCloseButton.onClick.AddListener(CloseHandler);
            _rewardGetRewardButton.onClick.AddListener(RewardHandler);
        }

        public void ShowBanner()
        {
            if (_testAdDisabled)
                return;

            Debug.Log($"[{nameof(DebugAdSource)}] Show <Banner> ad!");

            if (_bannerPanel != null)
                _bannerPanel.SetActive(true);
        }

        public void HideBanner()
        {
            if (_testAdDisabled)
                return;

            Debug.Log($"[{nameof(DebugAdSource)}] Hide <Banner> ad!");

            if (_bannerPanel != null)
                _bannerPanel.SetActive(false);
        }

        public void DestroyBanner()
        {
            if (_testAdDisabled)
                return;

            Debug.Log($"[{nameof(DebugAdSource)}] Destroy <Banner> ad!");

            if (_bannerPanel != null)
                _bannerPanel.SetActive(false);
        }

        public void EnableYoungMode()
        {
            Debug.Log($"[{nameof(DebugAdSource)}] Enable young mode!");
        }

        public void DisableYoungMode()
        {
            Debug.Log($"[{nameof(DebugAdSource)}] Disabled young mode!");
        }

        public bool IsInterstitialReady() => true;
        public bool IsRewardReady() => true;
        public bool IsBannerReady() => true;
        public bool IsInterstitialSupported => true;
        public bool IsRewardedSupported => true;
        public bool IsBannerSupported => true;
    }
}
