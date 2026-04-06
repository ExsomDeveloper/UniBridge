using UnityEngine;

namespace UniBridge
{
    public enum InterstitialMode { Manual, Automatic }

    [CreateAssetMenu(fileName = nameof(UniBridgeConfig), menuName = "UniBridge/Configuration")]
    public class UniBridgeConfig : ScriptableObject
    {
        [Header("General")]
        public bool AutoInitialize = true;
        public int MaxChildrenAge = 12;
        public int DefaultUserAge = 18;
        public string AdsDisabledKey = "unibridge_disabled";
        public bool SuccessfulRewardResetInterstitial = true;

        [Header("Interstitial")]
        public InterstitialMode InterstitialMode = InterstitialMode.Manual;
        [Min(5)] public int AutoInterstitialInterval  = 60;
        public bool EnableManualCooldown              = false;
        [Min(5)] public int ManualCooldownInterval    = 60;

        [Header("Saves")]
        public bool AutoInitializeSaves = true;

        [Header("Adapter Selection")]
        public string PreferredAdsAdapter; // SDK define: "UNIBRIDGE_YANDEX" | "UNIBRIDGE_LEVELPLAY" | "" (auto priority)

        [Header("LevelPlay (Android/iOS)")]
        [SerializeField] private LevelPlaySettings _levelPlaySettings = new LevelPlaySettings();
        public LevelPlaySettings LevelPlaySettings => _levelPlaySettings;

        [Header("Playgama (WebGL)")]
        [SerializeField] private PlaygamaSettings _playgamaSettings = new PlaygamaSettings();
        public PlaygamaSettings PlaygamaSettings => _playgamaSettings;

        [Header("Yandex Mobile Ads (Android/iOS)")]
        [SerializeField] private YandexSettings _yandexSettings = new YandexSettings();
        public YandexSettings YandexSettings => _yandexSettings;
    }
}
