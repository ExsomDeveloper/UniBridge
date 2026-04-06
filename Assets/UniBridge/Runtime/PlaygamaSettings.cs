using UnityEngine;

namespace UniBridge
{
    [System.Serializable]
    public class PlaygamaSettings
    {
        [field: SerializeField, Header("Banner Settings")]
        [field: Tooltip("Show banner automatically after initialization")]
        public bool AutoShowBanner { get; set; } = false;

        [field: SerializeField]
        [field: Tooltip("Platforms where banners are disabled")]
        public PlatformId[] DisableBannerOnPlatforms { get; set; } = new PlatformId[0];

        [field: SerializeField, Header("Interstitial Settings")]
        [field: Tooltip("Minimum interval between interstitials in seconds")]
        public int MinInterstitialInterval { get; set; } = 60;

        [field: SerializeField]
        [field: Tooltip("Countdown duration in seconds before showing interstitial on Yandex platform. Set to 0 to disable.")]
        public int YandexInterstitialCountdownSeconds { get; set; } = 3;

        [field: SerializeField]
        [field: Tooltip("Show Yandex interstitial countdown in Editor (for testing). Uses YandexInterstitialCountdownSeconds value.")]
        public bool SimulateYandexCountdownInEditor { get; set; } = false;

        [field: SerializeField]
        [field: Tooltip("Platforms where interstitials are disabled")]
        public PlatformId[] DisableInterstitialOnPlatforms { get; set; } = new PlatformId[0];

        [field: SerializeField, Header("Rewarded Settings")]
        [field: Tooltip("Preload rewarded ads on initialization")]
        public bool PreloadRewardedAds { get; set; } = true;

        [field: SerializeField]
        [field: Tooltip("Platforms where rewarded ads are disabled")]
        public PlatformId[] DisableRewardOnPlatforms { get; set; } = new PlatformId[0];
    }
}
