using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UniBridge.Editor
{
    /// <summary>
    /// Generates a link.xml at build time to prevent IL2CPP managed linker from stripping
    /// adapter assemblies that are selected in Build Manager but have no direct game code references.
    /// </summary>
    public static class AdapterLinkXmlGenerator
    {
        private const string OutputPath = "Assets/UniBridge/Generated/link.xml";

        private static readonly Dictionary<string, string> AdapterAssemblies = new()
        {
            { "UNIBRIDGE_LEVELPLAY",         "UniBridge.LevelPlay" },
            { "UNIBRIDGE_YANDEX",            "UniBridge.Yandex" },
            { "UNIBRIDGE_PLAYGAMA",          "UniBridge.Playgama" },
            { "UNIBRIDGEPURCHASES_IAP",         "UniBridge.Purchases.UnityIAP" },
            { "UNIBRIDGEPURCHASES_RUSTORE",     "UniBridge.Purchases.RuStore" },
            { "UNIBRIDGELEADERBOARDS_GPGS",     "UniBridge.Leaderboards.GPGS" },
            { "UNIBRIDGERATE_GOOGLEPLAY",       "UniBridge.Rate.GooglePlay" },
            { "UNIBRIDGERATE_RUSTORE",          "UniBridge.Rate.RuStore" },
            { "UNIBRIDGESAVES_GPGS",            "UniBridge.Saves.GPGS" },
            { "UNIBRIDGE_PLAYGAMA_SHARE",       "UniBridge.Share.Playgama" },
            { "UNIBRIDGE_PLAYGAMA_RATE",        "UniBridge.Rate.Playgama" },
            { "UNIBRIDGEANALYTICS_APPMETRICA",     "UniBridge.Analytics.AppMetrica" },
        };

        public static void Generate()
        {
            var assemblies = CollectAssembliesToPreserve();

            if (assemblies.Count == 0)
            {
                Debug.Log("[UniBridge] AdapterLinkXmlGenerator: no adapter assemblies to preserve, skipping link.xml generation.");
                return;
            }

            var dir = Path.GetDirectoryName(OutputPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(OutputPath, BuildXml(assemblies));
            AssetDatabase.ImportAsset(OutputPath);

            Debug.Log($"[UniBridge] Generated link.xml preserving: {string.Join(", ", assemblies)}");
        }

        public static void Delete()
        {
            if (AssetDatabase.LoadAssetAtPath<TextAsset>(OutputPath) != null)
                AssetDatabase.DeleteAsset(OutputPath);

            // Also clean up the Generated folder if it is now empty
            var dir = Path.GetDirectoryName(OutputPath);
            if (Directory.Exists(dir) && Directory.GetFileSystemEntries(dir).Length == 0)
                AssetDatabase.DeleteAsset(dir.Replace('\\', '/'));
        }

        private static List<string> CollectAssembliesToPreserve()
        {
            var storeDefine = GetCurrentStoreDefine();
            var buildTarget = GetBuildTargetString();
            var defines     = new HashSet<string>();

            void AddAdapterOrFallback(string preferred, string[] storeFallbacks)
            {
                if (preferred == AdapterDefines.NoneAdapterKey)
                    return;
                if (!string.IsNullOrEmpty(preferred))
                    AddDefine(defines, preferred);
                else
                    foreach (var s in storeFallbacks)
                        AddDefine(defines, s);
            }

            AddAdapterOrFallback(
                Resources.Load<UniBridgeConfig>(nameof(UniBridgeConfig))?.PreferredAdsAdapter,
                AdapterDefines.GetAdAdapters(buildTarget));

            AddAdapterOrFallback(
                Resources.Load<UniBridgePurchasesConfig>(nameof(UniBridgePurchasesConfig))?.PreferredPurchaseAdapter,
                AdapterDefines.GetPurchaseAdapters(storeDefine));

            AddAdapterOrFallback(
                Resources.Load<UniBridgeLeaderboardsConfig>(nameof(UniBridgeLeaderboardsConfig))?.PreferredLeaderboardAdapter,
                AdapterDefines.GetLeaderboardAdapters(storeDefine));

            AddAdapterOrFallback(
                Resources.Load<UniBridgeRateConfig>(nameof(UniBridgeRateConfig))?.PreferredRateAdapter,
                AdapterDefines.GetRateAdapters(storeDefine));

            AddAdapterOrFallback(
                Resources.Load<UniBridgeSavesConfig>(nameof(UniBridgeSavesConfig))?.PreferredSavesAdapter,
                AdapterDefines.GetSaveAdapters(storeDefine));

            // Rate: UniBridge.Rate.Playgama needs explicit link.xml entry (autoReferenced=false)
            var rateAdapter = Resources.Load<UniBridgeRateConfig>(nameof(UniBridgeRateConfig))?.PreferredRateAdapter ?? "";
            if (rateAdapter == "UNIBRIDGE_PLAYGAMA")
                AddDefine(defines, "UNIBRIDGE_PLAYGAMA_RATE");

            // Share: only UniBridge.Share.Playgama needs a link.xml entry (autoReferenced=false)
            var shareConfig = Resources.Load<Object>("UniBridgeShareConfig");
            var shareAdapter = shareConfig != null
                ? new SerializedObject(shareConfig).FindProperty("PreferredShareAdapter")?.stringValue ?? ""
                : "";
            if (shareAdapter == "UNIBRIDGE_PLAYGAMA")
                AddDefine(defines, "UNIBRIDGE_PLAYGAMA_SHARE");

            // Analytics
            AddAdapterOrFallback(
                Resources.Load<UniBridgeAnalyticsConfig>(nameof(UniBridgeAnalyticsConfig))?.PreferredAnalyticsAdapter,
                AdapterDefines.GetAnalyticsAdapters(storeDefine));

            var result = new List<string>();
            foreach (var define in defines)
                if (AdapterAssemblies.TryGetValue(define, out var assembly))
                    result.Add(assembly);

            return result;
        }

        private static void AddDefine(HashSet<string> set, string define)
        {
            if (!string.IsNullOrEmpty(define) && define != AdapterDefines.NoneAdapterKey)
                set.Add(define);
        }

        private static string GetCurrentStoreDefine()
        {
            var group   = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = new HashSet<string>(
                PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';'));

            if (defines.Contains(StorePlatformDefines.STORE_GOOGLEPLAY)) return StorePlatformDefines.STORE_GOOGLEPLAY;
            if (defines.Contains(StorePlatformDefines.STORE_RUSTORE))    return StorePlatformDefines.STORE_RUSTORE;
            if (defines.Contains(StorePlatformDefines.STORE_APPSTORE))   return StorePlatformDefines.STORE_APPSTORE;
            if (defines.Contains(StorePlatformDefines.STORE_PLAYGAMA))   return StorePlatformDefines.STORE_PLAYGAMA;
            return StorePlatformDefines.STORE_EDITOR;
        }

        private static string GetBuildTargetString() =>
            EditorUserBuildSettings.activeBuildTarget switch
            {
                BuildTarget.Android => "Android",
                BuildTarget.iOS     => "iOS",
                BuildTarget.WebGL   => "WebGL",
                _                   => ""
            };

        private static string BuildXml(List<string> assemblyNames)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!-- Auto-generated by UniBridge BuildPreprocessor. Do not edit manually. -->");
            sb.AppendLine("<linker>");
            foreach (var name in assemblyNames)
                sb.AppendLine($"  <assembly fullname=\"{name}\" preserve=\"all\" />");
            sb.AppendLine("</linker>");
            return sb.ToString();
        }
    }
}
