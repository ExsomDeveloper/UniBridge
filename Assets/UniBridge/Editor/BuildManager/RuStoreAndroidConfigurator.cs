using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UniBridge.Editor
{
    public readonly struct ChecklistItem
    {
        public readonly string Label;
        public readonly bool   Ok;
        public readonly string Hint;
        public readonly bool   IsOptional;

        public ChecklistItem(string label, bool ok, string hint = "", bool isOptional = false)
        { Label = label; Ok = ok; Hint = hint; IsOptional = isOptional; }
    }

    public static class RuStoreAndroidConfigurator
    {
        private const string XmlMarker      = "<!-- UNIBRIDGE_RUSTORE_GENERATED -->";
        private const string JavaMarker     = "// UNIBRIDGE_RUSTORE_GENERATED";
        private const string ManifestDir    = "Assets/Plugins/Android/UniBridgeMobileKit/RuStoreUnityPay.androidlib";
        private const string ManifestPath   = "Assets/Plugins/Android/UniBridgeMobileKit/RuStoreUnityPay.androidlib/AndroidManifest.xml";
        private const string JavaFilePath   = "Assets/Plugins/Android/UniBridgeMobileKit/RuStoreIntentFilterActivity.java";
        // Legacy paths: cleaned up if present, no longer created.
        private const string LegacyPayDir      = "Assets/Plugins/Android/UniBridgeMobileKit/RuStoreUnityPay"; // pre-androidlib
        private const string ActivityPath      = "Assets/Plugins/Android/RuStoreUnityActivity.java";
        private const string LegacyManifestPath = "Assets/Plugins/Android/AndroidManifest.xml";
        private const string SdkSettingsLib = "Assets/Plugins/Android/RuStoreSDKSettings.androidlib";

        private const string PayClientSettingsDir  = "Assets/RustoreSDK/Editor";
        private const string PayClientSettingsPath = "Assets/RustoreSDK/Editor/PayClientSettings.asset";
        // Script GUID is stable — part of the RuStore SDK package (ru.rustore.pay).
        private const string PayClientSettingsScriptGuid = "c3e591d99a91dca4ba692a69b0d0dd43";
        private const string ValuesXmlPath = "Assets/Plugins/Android/RuStoreSDKSettings.androidlib/res/values/values.xml";

        private const string DefaultDeeplink = "yourapp://rustore";

        // ── Public API ────────────────────────────────────────────────────────

        public static void Configure()
        {
            EnsurePluginsAndroidDirectory();

            // Remove legacy top-level manifest (old location before RuStoreUnityPay/ subdirectory).
            DeleteIfGenerated(LegacyManifestPath, XmlMarker);

            var (scheme, consoleAppId) = GetConfigParts();
            WriteManifest(scheme, consoleAppId);
            WriteIntentFilterActivity();
            EnsurePayClientSettings(consoleAppId, scheme);

            AssetDatabase.Refresh();
            Debug.Log($"[UniBridge] RuStore Android manifest configured (scheme={scheme}).");
        }

        public static void Cleanup()
        {
            bool deleted = false;

            if (Directory.Exists(ManifestDir))
            {
                FileUtil.DeleteFileOrDirectory(ManifestDir);
                FileUtil.DeleteFileOrDirectory(ManifestDir + ".meta");
                deleted = true;
            }

            // Legacy: pre-androidlib directory
            if (Directory.Exists(LegacyPayDir))
            {
                FileUtil.DeleteFileOrDirectory(LegacyPayDir);
                FileUtil.DeleteFileOrDirectory(LegacyPayDir + ".meta");
                deleted = true;
            }

            // Java file for the deeplink activity
            deleted |= DeleteIfGenerated(JavaFilePath, JavaMarker);

            // Legacy: activity file from old BillingClient integration
            deleted |= DeleteIfGenerated(ActivityPath, JavaMarker);
            // Legacy: top-level manifest from before RuStoreUnityPay/ subdirectory
            deleted |= DeleteIfGenerated(LegacyManifestPath, XmlMarker);

            if (deleted)
            {
                AssetDatabase.Refresh();
                Debug.Log("[UniBridge] RuStore Android files removed.");
            }
        }

        // ── Config helpers ────────────────────────────────────────────────────

        private static (string scheme, string consoleAppId) GetConfigParts()
        {
            var config = LoadConfig();
            string prefix       = config?.RuStoreSettings.DeeplinkPrefix ?? "";
            string consoleAppId = config?.RuStoreSettings.ConsoleApplicationId ?? "";

            if (string.IsNullOrEmpty(prefix))
            {
                Debug.LogWarning("[UniBridge] DeeplinkPrefix is empty. " +
                                 "Using default 'yourapp://rustore'. Update it in UniBridge > Purchases > Settings.");
                prefix = DefaultDeeplink;
            }

            var parts = prefix.Split(new[] { "://" }, 2, StringSplitOptions.None);
            string scheme = parts[0];

            return (scheme, consoleAppId);
        }

        internal static UniBridgePurchasesConfig LoadConfig()
        {
            var guids = AssetDatabase.FindAssets("t:UniBridgePurchasesConfig");
            if (guids.Length == 0) return null;

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<UniBridgePurchasesConfig>(path);
        }

        // ── File writers ──────────────────────────────────────────────────────

        private static void WriteManifest(string scheme, string consoleAppId)
        {
            if (File.Exists(ManifestPath))
            {
                string existing = File.ReadAllText(ManifestPath);
                if (!existing.Contains(XmlMarker))
                {
                    Debug.LogWarning("[UniBridge] Unexpected AndroidManifest.xml in RuStoreUnityPay — not overwriting.");
                    return;
                }
            }

            string content = ManifestTemplate
                .Replace("{SCHEME}",         scheme)
                .Replace("{CONSOLE_APP_ID}", consoleAppId);

            File.WriteAllText(ManifestPath, content);
        }

        // ── Delete helper ─────────────────────────────────────────────────────

        private static bool DeleteIfGenerated(string assetPath, string marker)
        {
            if (!File.Exists(assetPath)) return false;

            if (!File.ReadAllText(assetPath).Contains(marker))
            {
                Debug.LogWarning($"[UniBridge] Skipping deletion of '{assetPath}' — not generated by UniBridge.");
                return false;
            }

            AssetDatabase.DeleteAsset(assetPath);
            return true;
        }

        private static void EnsurePluginsAndroidDirectory()
        {
            const string ratMobileKitDir = "Assets/Plugins/Android/UniBridgeMobileKit";
            if (!Directory.Exists(ratMobileKitDir))
                Directory.CreateDirectory(ratMobileKitDir);
            if (!Directory.Exists(ManifestDir))
                Directory.CreateDirectory(ManifestDir);

            // Ensure proper Gradle library structure so Unity merges the manifest.
            File.WriteAllText(Path.Combine(ManifestDir, "build.gradle"),      RuStoreUnityPayBuildGradle);
            File.WriteAllText(Path.Combine(ManifestDir, "project.properties"), "android.library=true\n");
        }

        private static void WriteIntentFilterActivity()
        {
            File.WriteAllText(JavaFilePath, IntentFilterActivityJava);
        }

        // ── RuStoreSDKSettings.androidlib build.gradle ────────────────────────

        /// <summary>
        /// Writes build.gradle into RuStoreSDKSettings.androidlib so Unity's Gradle build
        /// system treats it as a proper library project. Always overwrites — the file is
        /// SDK-managed, not user-edited. Without explicit buildToolsVersion AGP auto-downloads
        /// build-tools;30.0.3 whose license may not be accepted, causing build failure.
        /// </summary>
        public static void EnsureAndroidLibBuildGradle()
        {
            if (!Directory.Exists(SdkSettingsLib)) return;

            var buildGradlePath = Path.Combine(SdkSettingsLib, "build.gradle");
            File.WriteAllText(buildGradlePath, SdkSettingsBuildGradle);
            AssetDatabase.ImportAsset(buildGradlePath);
            Debug.Log("[UniBridge] RuStoreSDKSettings.androidlib build.gradle updated.");

            // Ensure consoleApplicationId reaches the SDK via both PayClientSettings and values.xml.
            var (scheme, consoleAppId) = GetConfigParts();
            EnsurePayClientSettings(consoleAppId, scheme);
            EnsureValuesXml(consoleAppId, scheme);
        }

        // ── PayClientSettings & values.xml ───────────────────────────────────

        /// <summary>
        /// Creates or updates Assets/RustoreSDK/Editor/PayClientSettings.asset so that the
        /// RuStore SDK's own IPreprocessBuildWithReport (RuStoreSDKSettings) generates a
        /// correct values.xml during the build. The natively-read consoleApplicationId comes
        /// from string resources, NOT from AndroidManifest meta-data.
        /// </summary>
        private static void EnsurePayClientSettings(string consoleAppId, string scheme)
        {
            if (!Directory.Exists(SdkSettingsLib)) return; // RuStore SDK not installed

            // Try to update an existing asset via reflection (SDK type not directly referenced).
            var existingGuids = AssetDatabase.FindAssets("PayClientSettings t:ScriptableObject");
            foreach (var guid in existingGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (asset == null) continue;

                var type = asset.GetType();
                SetField(type, asset, "consoleApplicationId", consoleAppId);
                SetField(type, asset, "deeplinkScheme",       scheme);
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                Debug.Log($"[UniBridge] PayClientSettings updated (consoleApplicationId={consoleAppId}, deeplinkScheme={scheme}).");
                return;
            }

            // Asset not found — create it from YAML template.
            if (!Directory.Exists(PayClientSettingsDir))
                Directory.CreateDirectory(PayClientSettingsDir);

            string yaml = PayClientSettingsTemplate
                .Replace("{CONSOLE_APP_ID}", consoleAppId)
                .Replace("{DEEPLINK_SCHEME}", scheme);
            File.WriteAllText(PayClientSettingsPath, yaml);
            AssetDatabase.ImportAsset(PayClientSettingsPath);
            Debug.Log($"[UniBridge] PayClientSettings.asset created (consoleApplicationId={consoleAppId}, deeplinkScheme={scheme}).");
        }

        private static void SetField(Type type, object target, string fieldName, string value)
        {
            var field = type.GetField(fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
                field.SetValue(target, value);
            else
                Debug.LogWarning($"[UniBridge] PayClientSettings: field '{fieldName}' not found via reflection.");
        }

        /// <summary>
        /// Safety-net: writes values.xml directly so that even if the RuStore SDK's
        /// preprocessor runs first with an empty PayClientSettings, our values still win.
        /// </summary>
        private static void EnsureValuesXml(string consoleAppId, string scheme)
        {
            if (!Directory.Exists(SdkSettingsLib)) return;

            var dir = Path.GetDirectoryName(ValuesXmlPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string xml = ValuesXmlTemplate
                .Replace("{CONSOLE_APP_ID}", consoleAppId)
                .Replace("{DEEPLINK_SCHEME}", scheme);
            File.WriteAllText(ValuesXmlPath, xml);
            Debug.Log($"[UniBridge] RuStoreSDKSettings values.xml written (consoleApplicationId={consoleAppId}).");
        }

        // ── Templates ─────────────────────────────────────────────────────────

        private const string RuStoreUnityPayBuildGradle =
@"apply plugin: 'com.android.library'

android {
    namespace 'ru.rustore.unitysdk.unibridge'
    compileSdkVersion 34
    buildToolsVersion '34.0.0'

    defaultConfig {
        minSdkVersion 24
    }

    buildFeatures {
        buildConfig false
    }
}
";

        // Source from ru.rustore.pay Samples~/RuStorePayExample/Java/RuStoreIntentFilterActivity.java
        private const string IntentFilterActivityJava =
@"// UNIBRIDGE_RUSTORE_GENERATED
package ru.rustore.unitysdk;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import com.unity3d.player.UnityPlayerActivity;
import ru.rustore.unitysdk.payclient.RuStoreUnityPayClient;

public class RuStoreIntentFilterActivity extends Activity {

    private final Class<?> UNITY_PLAYER_ACTIVITY_CLASS = UnityPlayerActivity.class;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        if (savedInstanceState == null) {
            RuStoreUnityPayClient.INSTANCE.proceedIntent(getIntent());
        }

        if (!isTaskRoot()) {
            finish();
            return;
        }

        startGameActivity();
        finish();
    }

    @Override
    public void onNewIntent(Intent intent) {
        super.onNewIntent(intent);
        RuStoreUnityPayClient.INSTANCE.proceedIntent(intent);
    }

    private void startGameActivity() {
        Intent intent = new Intent(this, UNITY_PLAYER_ACTIVITY_CLASS);
        intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
        startActivity(intent);
    }
}
";

        private const string SdkSettingsBuildGradle =
@"apply plugin: 'com.android.library'

android {
    namespace 'ru.rustore.unitysdk.settings'
    compileSdkVersion 34
    buildToolsVersion '34.0.0'

    defaultConfig {
        minSdkVersion 24
    }

    buildFeatures {
        buildConfig false
    }
}
";

        private const string ManifestTemplate =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<!-- UNIBRIDGE_RUSTORE_GENERATED -->
<manifest xmlns:android=""http://schemas.android.com/apk/res/android"">
  <application>
    <!-- RuStore Pay SDK: console application ID -->
    <meta-data
        android:name=""console_app_id_value""
        android:value=""{CONSOLE_APP_ID}"" />
    <!-- RuStore Pay SDK: deeplink handler (provided by ru.rustore.pay package) -->
    <activity
        android:name=""ru.rustore.unitysdk.RuStoreIntentFilterActivity""
        android:theme=""@android:style/Theme.NoDisplay""
        android:exported=""true"">
      <intent-filter>
        <action android:name=""android.intent.action.VIEW"" />
        <category android:name=""android.intent.category.DEFAULT"" />
        <category android:name=""android.intent.category.BROWSABLE"" />
        <data android:scheme=""{SCHEME}"" />
      </intent-filter>
    </activity>
  </application>
</manifest>
";

        // PayClientSettings YAML — script GUID is stable, part of the RuStore SDK tgz.
        private const string PayClientSettingsTemplate =
@"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: " + PayClientSettingsScriptGuid + @", type: 3}
  m_Name: PayClientSettings
  m_EditorClassIdentifier:
  consoleApplicationId: {CONSOLE_APP_ID}
  deeplinkScheme: {DEEPLINK_SCHEME}
";

        private const string ValuesXmlTemplate =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<resources>
    <string name=""ru_rustore_console_app_id"">{CONSOLE_APP_ID}</string>
    <string name=""ru_rustore_deeplink_scheme"">{DEEPLINK_SCHEME}</string>
</resources>
";
    }
}
