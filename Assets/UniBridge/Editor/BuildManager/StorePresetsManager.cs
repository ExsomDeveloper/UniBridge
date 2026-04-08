using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
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
        // ── Cache ─────────────────────────────────────────────────────────────

        private static List<StorePreset> _cache;

        // ── Path Resolution ───────────────────────────────────────────────────

        private static string FindJsonPath()
        {
            var guids = AssetDatabase.FindAssets("StorePresets t:TextAsset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("UniBridge") && path.EndsWith("StorePresets.json"))
                    return path;
            }

            var directPath = "Assets/UniBridge/Editor/BuildManager/StorePresets.json";
            if (File.Exists(directPath))
                return directPath;

            var packagePath = "Packages/com.unibridge.core/Editor/BuildManager/StorePresets.json";
            if (File.Exists(packagePath))
                return packagePath;

            return null;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public static List<StorePreset> Load()
        {
            if (_cache != null)
                return _cache;

            var jsonPath = FindJsonPath();

            if (jsonPath != null)
            {
                try
                {
                    var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
                    var json      = textAsset != null ? textAsset.text : File.ReadAllText(jsonPath);
                    var wrapper   = JsonUtility.FromJson<StorePresetList>(json);
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

            var jsonPath = FindJsonPath();

            if (jsonPath != null && jsonPath.StartsWith("Packages/"))
            {
                Debug.LogWarning("[UniBridge] StorePresets.json is inside a read-only UPM package; changes are saved in memory only.");
                return;
            }

            var writePath = jsonPath ?? "Assets/UniBridge/Editor/BuildManager/StorePresets.json";

            try
            {
                var dir = Path.GetDirectoryName(writePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var wrapper = new StorePresetList { presets = presets };
                var json    = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(writePath, json);
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
            },
            new StorePreset
            {
                displayName          = "YouTube Playables",
                define               = StorePlatformDefines.STORE_YOUTUBE,
                buildTarget          = "WebGL",
                adsAdapter           = "UNIBRIDGE_YTPLAYABLES",
                purchasesAdapter     = AdapterDefines.NoneAdapterKey,
                leaderboardsAdapter  = "UNIBRIDGE_YTPLAYABLES",
                rateAdapter          = AdapterDefines.NoneAdapterKey,
                shareAdapter         = AdapterDefines.NoneAdapterKey,
                savesAdapter         = "UNIBRIDGE_YTPLAYABLES",
                analyticsAdapter     = AdapterDefines.NoneAdapterKey,
                authAdapter          = "UNIBRIDGEAUTH_MOCK",
                sdkDefines           = new List<string> { "UNIBRIDGE_YTPLAYABLES" }
            }
        };
    }
}
