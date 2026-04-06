using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UniBridge.Editor
{
    [Serializable]
    public class StorePreset
    {
        public string displayName;
        public string define;
        public string buildTarget;       // "Android" | "iOS" | "WebGL"
        public string adsAdapter;          // SDK define: "UNIBRIDGE_YANDEX" | "UNIBRIDGE_LEVELPLAY" | null (auto priority)
        public string purchasesAdapter;    // SDK define: "UNIBRIDGEPURCHASES_IAP" | "UNIBRIDGEPURCHASES_RUSTORE" | null (display-only)
        public string leaderboardsAdapter; // SDK define: "UNIBRIDGELEADERBOARDS_GPGS" | "UNITY_IOS_GAMECENTER" | "UNIBRIDGELEADERBOARDS_SIMULATED"
        public string rateAdapter;         // SDK define: "UNIBRIDGERATE_GOOGLEPLAY" | "UNIBRIDGERATE_RUSTORE" | "UNITY_IOS_STOREREVIEW" | "UNIBRIDGERATE_MOCK"
        public string shareAdapter;        // SDK define: "UNIBRIDGESHARE_ANDROID" | "UNIBRIDGESHARE_IOS" | "UNIBRIDGE_PLAYGAMA" | "UNIBRIDGESHARE_MOCK"
        public string savesAdapter;        // SDK define: "UNIBRIDGESAVES_GPGS" | "UNITY_IOS_ICLOUD" | "UNIBRIDGESAVES_SIMULATED" | "UNIBRIDGE_PLAYGAMA" | "UNIBRIDGE_NONE" | "" (local)
        public string analyticsAdapter;   // SDK define: "UNIBRIDGEANALYTICS_APPMETRICA" | "UNIBRIDGE_NONE"
        public string authAdapter;        // SDK define: "UNIBRIDGELEADERBOARDS_GPGS" | "UNITY_IOS_GAMECENTER" | "UNIBRIDGE_PLAYGAMA" | "UNIBRIDGEAUTH_MOCK"
        public List<string> sdkDefines;
    }

    [Serializable]
    internal class StorePresetList
    {
        public List<StorePreset> presets;
    }

    public static class StorePresetsManager
    {
        // ── Path ─────────────────────────────────────────────────────────────

        private static string JsonPath =>
            Path.Combine(Application.dataPath, "UniBridge/Editor/BuildManager/StorePresets.json");

        // ── Cache ─────────────────────────────────────────────────────────────

        private static List<StorePreset> _cache;

        // ── Public API ────────────────────────────────────────────────────────

        public static List<StorePreset> Load()
        {
            if (_cache != null)
                return _cache;

            if (File.Exists(JsonPath))
            {
                try
                {
                    var json    = File.ReadAllText(JsonPath);
                    var wrapper = JsonUtility.FromJson<StorePresetList>(json);
                    if (wrapper?.presets != null && wrapper.presets.Count > 0)
                    {
                        _cache = wrapper.presets;
                        return _cache;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[UniBridge] Failed to parse StorePresets.json: {e.Message}");
                }
            }

            _cache = CreateDefaults();
            return _cache;
        }

        public static void Save(List<StorePreset> presets)
        {
            _cache = presets;

            try
            {
                var dir = Path.GetDirectoryName(JsonPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var wrapper = new StorePresetList { presets = presets };
                var json    = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(JsonPath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UniBridge] Failed to save StorePresets.json: {e.Message}");
            }
        }

        public static void InvalidateCache() => _cache = null;

        // ── Defaults ──────────────────────────────────────────────────────────

        private static List<StorePreset> CreateDefaults() => new List<StorePreset>
        {
            new StorePreset
            {
                displayName          = "Unity Editor",
                define               = StorePlatformDefines.STORE_EDITOR,
                buildTarget          = "Editor",
                adsAdapter           = AdapterDefines.NoneAdapterKey,
                purchasesAdapter     = AdapterDefines.NoneAdapterKey,
                leaderboardsAdapter  = AdapterDefines.NoneAdapterKey,
                rateAdapter          = AdapterDefines.NoneAdapterKey,
                shareAdapter         = "UNIBRIDGESHARE_MOCK",
                savesAdapter         = "UNIBRIDGESAVES_SIMULATED",
                analyticsAdapter     = AdapterDefines.NoneAdapterKey,
                authAdapter          = "UNIBRIDGEAUTH_MOCK",
                sdkDefines           = new List<string>()
            },
            new StorePreset
            {
                displayName          = "Google Play",
                define               = StorePlatformDefines.STORE_GOOGLEPLAY,
                buildTarget          = "Android",
                adsAdapter           = "UNIBRIDGE_YANDEX",
                purchasesAdapter     = "UNIBRIDGEPURCHASES_IAP",
                leaderboardsAdapter  = "UNIBRIDGELEADERBOARDS_GPGS",
                rateAdapter          = "UNIBRIDGERATE_GOOGLEPLAY",
                shareAdapter         = "UNIBRIDGESHARE_ANDROID",
                savesAdapter         = "UNIBRIDGESAVES_GPGS",
                analyticsAdapter     = AdapterDefines.NoneAdapterKey,
                authAdapter          = "UNIBRIDGELEADERBOARDS_GPGS",
                sdkDefines           = new List<string> { "UNIBRIDGE_YANDEX", "UNIBRIDGEPURCHASES_IAP" }
            },
            new StorePreset
            {
                displayName          = "RuStore",
                define               = StorePlatformDefines.STORE_RUSTORE,
                buildTarget          = "Android",
                adsAdapter           = "UNIBRIDGE_LEVELPLAY",
                purchasesAdapter     = "UNIBRIDGEPURCHASES_RUSTORE",
                leaderboardsAdapter  = "UNIBRIDGELEADERBOARDS_SIMULATED",
                rateAdapter          = "UNIBRIDGERATE_RUSTORE",
                shareAdapter         = "UNIBRIDGESHARE_ANDROID",
                savesAdapter         = "UNIBRIDGESAVES_SIMULATED",
                analyticsAdapter     = AdapterDefines.NoneAdapterKey,
                authAdapter          = "UNIBRIDGEAUTH_MOCK",
                sdkDefines           = new List<string> { "UNIBRIDGE_LEVELPLAY", "UNIBRIDGEPURCHASES_RUSTORE" }
            },
            new StorePreset
            {
                displayName          = "App Store",
                define               = StorePlatformDefines.STORE_APPSTORE,
                buildTarget          = "iOS",
                adsAdapter           = "UNIBRIDGE_LEVELPLAY",
                purchasesAdapter     = "UNIBRIDGEPURCHASES_IAP",
                leaderboardsAdapter  = "UNITY_IOS_GAMECENTER",
                rateAdapter          = "UNITY_IOS_STOREREVIEW",
                shareAdapter         = "UNIBRIDGESHARE_IOS",
                savesAdapter         = "UNITY_IOS_ICLOUD",
                analyticsAdapter     = AdapterDefines.NoneAdapterKey,
                authAdapter          = "UNITY_IOS_GAMECENTER",
                sdkDefines           = new List<string> { "UNIBRIDGE_LEVELPLAY", "UNIBRIDGEPURCHASES_IAP" }
            },
            new StorePreset
            {
                displayName          = "Playgama",
                define               = StorePlatformDefines.STORE_PLAYGAMA,
                buildTarget          = "WebGL",
                adsAdapter           = "UNIBRIDGE_PLAYGAMA",
                purchasesAdapter     = "UNIBRIDGE_PLAYGAMA",
                leaderboardsAdapter  = "UNIBRIDGE_PLAYGAMA",
                rateAdapter          = "UNIBRIDGE_PLAYGAMA",
                shareAdapter         = "UNIBRIDGE_PLAYGAMA",
                savesAdapter         = "UNIBRIDGE_PLAYGAMA",
                analyticsAdapter     = AdapterDefines.NoneAdapterKey,
                authAdapter          = "UNIBRIDGE_PLAYGAMA",
                sdkDefines           = new List<string> { "UNIBRIDGE_PLAYGAMA" }
            }
        };
    }
}
