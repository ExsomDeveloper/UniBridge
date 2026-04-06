using UnityEngine;

namespace UniBridge
{
    public enum AdTypeCheck { Interstitial, Rewarded, Banner }

    public class DisableIfAdNotSupported : MonoBehaviour
    {
        [SerializeField] private AdTypeCheck _adType = AdTypeCheck.Rewarded;

        private bool _subscribed;

        private void Awake()
        {
            if (UniBridge.IsInitialized)
            {
                ApplyVisibility();
            }
            else
            {
                _subscribed = true;
                UniBridge.OnInitSuccess += OnInitResult;
                UniBridge.OnInitFailed += OnInitResult;
            }
        }

        private void OnDestroy()
        {
            if (!_subscribed) return;
            UniBridge.OnInitSuccess -= OnInitResult;
            UniBridge.OnInitFailed -= OnInitResult;
        }

        private void OnInitResult()
        {
            _subscribed = false;
            UniBridge.OnInitSuccess -= OnInitResult;
            UniBridge.OnInitFailed -= OnInitResult;
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            bool supported = _adType switch
            {
                AdTypeCheck.Interstitial => UniBridge.IsInterstitialSupported,
                AdTypeCheck.Rewarded     => UniBridge.IsRewardedSupported,
                AdTypeCheck.Banner       => UniBridge.IsBannerSupported,
                _                        => false
            };

            if (!supported)
                gameObject.SetActive(false);
        }
    }
}
