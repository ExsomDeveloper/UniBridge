using System;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UniBridge.Editor
{
    public class BuildReportLogger : IPostprocessBuildWithReport
    {
        public int callbackOrder => 20; // after BuildPreprocessor (10)

        public void OnPostprocessBuild(BuildReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[UniBridge] Build Report");
            sb.AppendLine("──────────────────────────────────────────");

            var storeDefine = GetActiveStoreDefine();
            sb.AppendLine($"  Store:        {GetStoreName(storeDefine)}");
            sb.AppendLine($"  Target:       {report.summary.platform}");
            sb.AppendLine($"  Result:       {report.summary.result}  ({FormatTime(report.summary.totalTime)}, {FormatSize(report.summary.totalSize)})");

            sb.AppendLine("──────────────────────────────────────────");
            sb.AppendLine("  Adapters:");
            sb.AppendLine($"    Ads:          {ResolveAdapter(Resources.Load<UniBridgeConfig>(nameof(UniBridgeConfig))?.PreferredAdsAdapter, AdapterDefines.AdsAdapterNames)}");
            sb.AppendLine($"    Purchases:    {ResolveAdapter(Resources.Load<UniBridgePurchasesConfig>(nameof(UniBridgePurchasesConfig))?.PreferredPurchaseAdapter, AdapterDefines.PurchaseAdapterNames)}");
            sb.AppendLine($"    Leaderboards: {ResolveAdapter(Resources.Load<UniBridgeLeaderboardsConfig>(nameof(UniBridgeLeaderboardsConfig))?.PreferredLeaderboardAdapter, AdapterDefines.LeaderboardAdapterNames)}");
            sb.AppendLine($"    Reviews:      {ResolveAdapter(Resources.Load<UniBridgeRateConfig>(nameof(UniBridgeRateConfig))?.PreferredRateAdapter, AdapterDefines.RateAdapterNames)}");
            sb.AppendLine($"    Share:        {ResolveAdapter(ReadShareAdapter(), AdapterDefines.ShareAdapterNames)}");
            sb.AppendLine($"    Analytics:    {ResolveAdapter(Resources.Load<UniBridgeAnalyticsConfig>(nameof(UniBridgeAnalyticsConfig))?.PreferredAnalyticsAdapter, AdapterDefines.AnalyticsAdapterNames)}");
            sb.AppendLine($"    Auth:         {ResolveAdapter(Resources.Load<UniBridgeAuthConfig>(nameof(UniBridgeAuthConfig))?.PreferredAuthAdapter, AdapterDefines.AuthAdapterNames)}");
            sb.Append("──────────────────────────────────────────");

            Debug.Log(sb.ToString());
        }

        private static string GetActiveStoreDefine()
        {
            var group   = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);

            foreach (var storeDefine in StorePlatformDefines.AllStoreDefines)
                foreach (var d in defines.Split(';'))
                    if (d.Trim() == storeDefine)
                        return storeDefine;

            return null;
        }

        private static string GetStoreName(string storeDefine)
        {
            if (string.IsNullOrEmpty(storeDefine))
                return "Unknown";

            foreach (var preset in StorePresetsManager.Load())
                if (preset.define == storeDefine)
                    return preset.displayName;

            return storeDefine;
        }

        private static string ReadShareAdapter()
        {
            var config = Resources.Load<ScriptableObject>("UniBridgeShareConfig");
            if (config == null) return null;
            return new SerializedObject(config).FindProperty("PreferredShareAdapter")?.stringValue;
        }

        private static string ResolveAdapter(string define, System.Collections.Generic.Dictionary<string, string> names)
        {
            if (string.IsNullOrEmpty(define))
                return "Auto (priority-based)";

            return names.TryGetValue(define, out var name) ? name : define;
        }

        private static string FormatTime(TimeSpan t)
        {
            if (t.TotalSeconds < 60)
                return $"{t.Seconds}s";
            return $"{(int)t.TotalMinutes}m {t.Seconds:D2}s";
        }

        private static string FormatSize(ulong bytes)
        {
            if (bytes == 0)
                return "—";
            var mb = bytes / (1024.0 * 1024.0);
            return $"{mb:F1} MB";
        }
    }
}
