using UnityEngine;

namespace UniBridge
{
    [System.Serializable]
    public class LevelPlaySettings
    {
        [field: SerializeField, Header("App Keys")]
        public string AndroidKey { get; set; }

        [field: SerializeField]
        public string IOSKey { get; set; }

        [field: SerializeField, Header("Banner Ad Units")]
        public string BannerAdUnitAndroid { get; set; }

        [field: SerializeField]
        public string BannerAdUnitIOS { get; set; }

        [field: SerializeField, Header("Interstitial Ad Units")]
        public string InterstitialAdUnitAndroid { get; set; }

        [field: SerializeField]
        public string InterstitialAdUnitIOS { get; set; }

        [field: SerializeField, Header("Reward Ad Units")]
        public string RewardAdUnitAndroid { get; set; }

        [field: SerializeField]
        public string RewardAdUnitIOS { get; set; }

        public string GetAppKey()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return IOSKey;
#else
            return AndroidKey;
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

        public string GetRewardAdUnitId()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return RewardAdUnitIOS;
#else
            return RewardAdUnitAndroid;
#endif
        }

        public string GetBannerAdUnitId()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return BannerAdUnitIOS;
#else
            return BannerAdUnitAndroid;
#endif
        }
    }
}
