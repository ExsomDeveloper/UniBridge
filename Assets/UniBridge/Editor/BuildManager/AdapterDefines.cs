using System;
using System.Collections.Generic;

namespace UniBridge.Editor
{
    public static class AdapterDefines
    {
        /// <summary>Virtual key meaning the system is disabled. The facade is not initialized and no adapter code runs.</summary>
        public const string NoneAdapterKey = "UNIBRIDGE_NONE";

        // ── SDK define → display name ─────────────────────────────────────────
        // Keys match the SDK defines used as AdSourceRegistry keys

        public static readonly Dictionary<string, string> AdsAdapterNames = new()
        {
            { "UNIBRIDGE_LEVELPLAY", "LevelPlay" },
            { "UNIBRIDGE_YANDEX",    "Yandex Ads" },
            { "UNIBRIDGE_PLAYGAMA",  "Playgama" },
            { NoneAdapterKey,     "Нет" },
        };

        public static readonly Dictionary<string, string> PurchaseAdapterNames = new()
        {
            { "UNIBRIDGEPURCHASES_IAP",     "Unity IAP" },
            { "UNIBRIDGEPURCHASES_RUSTORE", "RuStore Billing" },
            { "UNIBRIDGE_PLAYGAMA",      "Playgama" },
            { NoneAdapterKey,         "Нет" },
        };

        public static readonly Dictionary<string, string> LeaderboardAdapterNames = new()
        {
            { "UNIBRIDGELEADERBOARDS_GPGS",      "Google Play Games Services" },
            { "UNIBRIDGE_PLAYGAMA",           "Playgama" },
            { "UNITY_IOS_GAMECENTER",      "Game Center (built-in)" },
            { "UNIBRIDGELEADERBOARDS_SIMULATED", "Симуляция" },
            { NoneAdapterKey,              "Нет" },
        };

        public static readonly Dictionary<string, string> RateAdapterNames = new()
        {
            { "UNIBRIDGERATE_GOOGLEPLAY",    "Google Play Review" },
            { "UNIBRIDGERATE_RUSTORE",       "RuStore Review" },
            { "UNITY_IOS_STOREREVIEW", "App Store Review (built-in)" },
            { "UNIBRIDGE_PLAYGAMA",       "Playgama" },
            { "UNIBRIDGERATE_MOCK",          "Mock (не поддерживается)" },
            { NoneAdapterKey,          "Нет" },
        };

        public static readonly Dictionary<string, string> ShareAdapterNames = new()
        {
            { "UNIBRIDGESHARE_ANDROID", "Android Native Share" },
            { "UNIBRIDGESHARE_IOS",     "iOS Native Share" },
            { "UNIBRIDGE_PLAYGAMA",  "Playgama" },
            { "UNIBRIDGESHARE_MOCK",    "Mock (не поддерживается)" },
            { NoneAdapterKey,     "Нет" },
        };

        public static readonly Dictionary<string, string> SaveAdapterNames = new()
        {
            { "UNIBRIDGESAVES_GPGS",      "Google Play Saved Games" },
            { "UNITY_IOS_ICLOUD",   "iCloud Key-Value Store (built-in)" },
            { "UNIBRIDGE_PLAYGAMA",    "Playgama" },
            { "UNIBRIDGESAVES_SIMULATED", "Симуляция (PlayerPrefs)" },
            { NoneAdapterKey,       "Локальное хранилище" },
        };

        public static readonly Dictionary<string, string> AnalyticsAdapterNames = new()
        {
            { "UNIBRIDGEANALYTICS_APPMETRICA", "AppMetrica" },
            { NoneAdapterKey,            "Нет" },
        };

        public static readonly Dictionary<string, string> AuthAdapterNames = new()
        {
            { "UNIBRIDGELEADERBOARDS_GPGS", "Google Play Games Services" },
            { "UNITY_IOS_GAMECENTER", "Game Center (built-in)" },
            { "UNIBRIDGE_PLAYGAMA",      "Playgama" },
            { "UNIBRIDGEAUTH_MOCK",         "Mock (не поддерживается)" },
        };

        // ── Obsolete defines from previous implementation (cleanup on SetStoreDefine) ──

        public static readonly string[] ObsoleteAdapterDefines =
        {
            "UNIBRIDGE_ADS_ADAPTER_LEVELPLAY", "UNIBRIDGE_ADS_ADAPTER_YANDEX", "UNIBRIDGE_ADS_ADAPTER_PLAYGAMA",
            "UNIBRIDGEPURCHASES_ADAPTER_IAP", "UNIBRIDGEPURCHASES_ADAPTER_RUSTORE", "UNIBRIDGEPURCHASES_ADAPTER_PLAYGAMA",
        };

        // ── Available adapters per platform ───────────────────────────────────

        public static string[] GetAdAdapters(string buildTarget) => buildTarget switch
        {
            "Android" or "iOS" => new[] { "UNIBRIDGE_LEVELPLAY", "UNIBRIDGE_YANDEX", NoneAdapterKey },
            "WebGL"            => new[] { "UNIBRIDGE_PLAYGAMA", NoneAdapterKey },
            _                  => new[] { NoneAdapterKey },
        };

        public static string[] GetPurchaseAdapters(string storeDefine) => storeDefine switch
        {
            StorePlatformDefines.STORE_GOOGLEPLAY => new[] { "UNIBRIDGEPURCHASES_IAP", "UNIBRIDGEPURCHASES_RUSTORE", NoneAdapterKey },
            StorePlatformDefines.STORE_RUSTORE    => new[] { "UNIBRIDGEPURCHASES_RUSTORE", "UNIBRIDGEPURCHASES_IAP", NoneAdapterKey },
            StorePlatformDefines.STORE_APPSTORE   => new[] { "UNIBRIDGEPURCHASES_IAP", NoneAdapterKey },
            StorePlatformDefines.STORE_PLAYGAMA   => new[] { "UNIBRIDGE_PLAYGAMA", NoneAdapterKey },
            StorePlatformDefines.STORE_EDITOR     => new[] { NoneAdapterKey },
            _                                     => new[] { NoneAdapterKey },
        };

        public static string[] GetLeaderboardAdapters(string storeDefine) => storeDefine switch
        {
            StorePlatformDefines.STORE_GOOGLEPLAY => new[] { "UNIBRIDGELEADERBOARDS_GPGS", "UNIBRIDGELEADERBOARDS_SIMULATED", NoneAdapterKey },
            StorePlatformDefines.STORE_RUSTORE    => new[] { "UNIBRIDGELEADERBOARDS_SIMULATED", NoneAdapterKey },
            StorePlatformDefines.STORE_APPSTORE   => new[] { "UNITY_IOS_GAMECENTER", "UNIBRIDGELEADERBOARDS_SIMULATED", NoneAdapterKey },
            StorePlatformDefines.STORE_PLAYGAMA   => new[] { "UNIBRIDGE_PLAYGAMA", "UNIBRIDGELEADERBOARDS_SIMULATED", NoneAdapterKey },
            StorePlatformDefines.STORE_EDITOR     => new[] { NoneAdapterKey },
            _                                     => new[] { "UNIBRIDGELEADERBOARDS_SIMULATED", NoneAdapterKey },
        };

        public static string[] GetRateAdapters(string storeDefine) => storeDefine switch
        {
            StorePlatformDefines.STORE_GOOGLEPLAY => new[] { "UNIBRIDGERATE_GOOGLEPLAY", "UNIBRIDGERATE_MOCK", NoneAdapterKey },
            StorePlatformDefines.STORE_RUSTORE    => new[] { "UNIBRIDGERATE_RUSTORE",    "UNIBRIDGERATE_MOCK", NoneAdapterKey },
            StorePlatformDefines.STORE_APPSTORE   => new[] { "UNITY_IOS_STOREREVIEW", NoneAdapterKey },
            StorePlatformDefines.STORE_PLAYGAMA   => new[] { "UNIBRIDGE_PLAYGAMA", "UNIBRIDGERATE_MOCK", NoneAdapterKey },
            StorePlatformDefines.STORE_EDITOR     => new[] { NoneAdapterKey },
            _                                     => new[] { "UNIBRIDGERATE_MOCK", NoneAdapterKey },
        };

        public static string[] GetShareAdapters(string storeDefine) => storeDefine switch
        {
            StorePlatformDefines.STORE_GOOGLEPLAY => new[] { "UNIBRIDGESHARE_ANDROID", "UNIBRIDGESHARE_MOCK", NoneAdapterKey },
            StorePlatformDefines.STORE_RUSTORE    => new[] { "UNIBRIDGESHARE_ANDROID", "UNIBRIDGESHARE_MOCK", NoneAdapterKey },
            StorePlatformDefines.STORE_APPSTORE   => new[] { "UNIBRIDGESHARE_IOS",     "UNIBRIDGESHARE_MOCK", NoneAdapterKey },
            StorePlatformDefines.STORE_PLAYGAMA   => new[] { "UNIBRIDGE_PLAYGAMA",  "UNIBRIDGESHARE_MOCK", NoneAdapterKey },
            StorePlatformDefines.STORE_EDITOR     => new[] { "UNIBRIDGESHARE_MOCK",    NoneAdapterKey },
            _                                     => new[] { "UNIBRIDGESHARE_MOCK",    NoneAdapterKey },
        };

        public static string[] GetSaveAdapters(string storeDefine) => storeDefine switch
        {
            StorePlatformDefines.STORE_GOOGLEPLAY => new[] { "UNIBRIDGESAVES_GPGS",      "UNIBRIDGESAVES_SIMULATED", NoneAdapterKey },
            StorePlatformDefines.STORE_RUSTORE    => new[] { "UNIBRIDGESAVES_SIMULATED", NoneAdapterKey },
            StorePlatformDefines.STORE_APPSTORE   => new[] { "UNITY_IOS_ICLOUD",   "UNIBRIDGESAVES_SIMULATED", NoneAdapterKey },
            StorePlatformDefines.STORE_PLAYGAMA   => new[] { "UNIBRIDGE_PLAYGAMA",    "UNIBRIDGESAVES_SIMULATED", NoneAdapterKey },
            StorePlatformDefines.STORE_EDITOR     => new[] { "UNIBRIDGESAVES_SIMULATED", NoneAdapterKey },
            _                                     => new[] { "UNIBRIDGESAVES_SIMULATED", NoneAdapterKey },
        };

        public static string[] GetAnalyticsAdapters(string storeDefine) => storeDefine switch
        {
            StorePlatformDefines.STORE_EDITOR   => new[] { NoneAdapterKey },
            StorePlatformDefines.STORE_PLAYGAMA => new[] { NoneAdapterKey },
            _                                   => new[] { "UNIBRIDGEANALYTICS_APPMETRICA", NoneAdapterKey },
        };

        public static string[] GetAuthAdapters(string storeDefine) => storeDefine switch
        {
            StorePlatformDefines.STORE_GOOGLEPLAY => new[] { "UNIBRIDGELEADERBOARDS_GPGS", "UNIBRIDGEAUTH_MOCK" },
            StorePlatformDefines.STORE_RUSTORE    => new[] { "UNIBRIDGEAUTH_MOCK" },
            StorePlatformDefines.STORE_APPSTORE   => new[] { "UNITY_IOS_GAMECENTER", "UNIBRIDGEAUTH_MOCK" },
            StorePlatformDefines.STORE_PLAYGAMA   => new[] { "UNIBRIDGE_PLAYGAMA", "UNIBRIDGEAUTH_MOCK" },
            StorePlatformDefines.STORE_EDITOR     => new[] { "UNIBRIDGEAUTH_MOCK" },
            _                                     => new[] { "UNIBRIDGEAUTH_MOCK" },
        };
    }
}
