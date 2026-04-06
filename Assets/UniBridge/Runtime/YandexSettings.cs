using UnityEngine;

namespace UniBridge
{
    [System.Serializable]
    public class YandexSettings
    {
        [field: SerializeField, Header("Banner Ad Units")]
        public string BannerAdUnitAndroid { get; set; }

        [field: SerializeField]
        public string BannerAdUnitIOS { get; set; }

        [field: SerializeField, Header("Interstitial Ad Units")]
        public string InterstitialAdUnitAndroid { get; set; }

        [field: SerializeField]
        public string InterstitialAdUnitIOS { get; set; }

        [field: SerializeField, Header("Rewarded Ad Units")]
        public string RewardedAdUnitAndroid { get; set; }

        [field: SerializeField]
        public string RewardedAdUnitIOS { get; set; }

        public string GetBannerAdUnitId()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return BannerAdUnitIOS;
#else
            return BannerAdUnitAndroid;
#endif
        }

        public string GetInterstitialAdUnitId()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return InterstitialAdUnitIOS;
#else
            return InterstitialAdUnitAndroid;
#endif
        }

        public string GetRewardedAdUnitId()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return RewardedAdUnitIOS;
#else
            return RewardedAdUnitAndroid;
#endif
        }
    }
}
