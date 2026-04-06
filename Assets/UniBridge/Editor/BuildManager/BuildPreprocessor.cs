using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UniBridge.Editor
{
    public class BuildPreprocessor : IPreprocessBuildWithReport, IPostprocessBuild
    {
        public int callbackOrder => 10;

        public void OnPreprocessBuild(BuildReport report)
        {
            AdapterLinkXmlGenerator.Generate();

            if (report.summary.platform == BuildTarget.Android)
            {
                RuStoreAndroidConfigurator.EnsureAndroidLibBuildGradle();
                UniBridgeShareAndroidConfigurator.Configure();
            }

            EnsureShareConfigAsset();

            var activeDefines = GetActiveStoreDefines();

            if (activeDefines.Count == 0)
            {
                throw new BuildFailedException(
                    "[UniBridge] No UNIBRIDGE_STORE_* define is set. " +
                    "Open UniBridge > Build Manager to select a store platform before building.");
            }

            if (activeDefines.Count > 1)
            {
                throw new BuildFailedException(
                    "[UniBridge] Multiple UNIBRIDGE_STORE_* defines are set: " +
                    string.Join(", ", activeDefines) +
                    ". Only one store define should be active. Open UniBridge > Build Manager to fix this.");
            }

            var define = activeDefines[0];

            if (define == StorePlatformDefines.STORE_EDITOR)
            {
                throw new BuildFailedException(
                    "[UniBridge] Стор 'Unity Editor' предназначен только для тестирования в редакторе и не может использоваться для сборок. " +
                    "Откройте UniBridge > Build Manager и выберите платформенный стор перед сборкой.");
            }

            var expectedTarget = StorePlatformDefines.GetExpectedBuildTarget(define);
            var actualTarget   = report.summary.platform;

            if (actualTarget != expectedTarget)
            {
                throw new BuildFailedException(
                    $"[UniBridge] Store define {define} expects build target {expectedTarget}, " +
                    $"but current build target is {actualTarget}. " +
                    "Open UniBridge > Build Manager to fix the mismatch.");
            }

            ValidateSdkInstalled(define);
            ValidateAdapterSelection(define);
        }

        private static List<string> GetActiveStoreDefines()
        {
            var group   = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            var active  = new List<string>();

            foreach (var storeDefine in StorePlatformDefines.AllStoreDefines)
            {
                foreach (var d in defines.Split(';'))
                {
                    if (d.Trim() == storeDefine)
                    {
                        active.Add(storeDefine);
                        break;
                    }
                }
            }

            return active;
        }

        private static void ValidateSdkInstalled(string define)
        {
            var preset = FindPreset(define);
            if (preset == null || preset.sdkDefines == null || preset.sdkDefines.Count == 0)
                return;

            var group          = EditorUserBuildSettings.selectedBuildTargetGroup;
            var definesRaw     = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            var currentDefines = new HashSet<string>(definesRaw.Split(';'));

            foreach (var sdk in preset.sdkDefines)
            {
                if (string.IsNullOrEmpty(sdk)) continue;
                // Package IDs (e.g. com.google.*) cannot be validated via scripting defines — skip them
                if (sdk.Contains('.')) continue;
                if (!currentDefines.Contains(sdk))
                {
                    throw new BuildFailedException(
                        $"[UniBridge] Store '{preset.displayName}' requires SDK define '{sdk}', " +
                        "but it is not set. Install via UniBridge > SDK Installer.");
                }
            }
        }

        private static void ValidateAdapterSelection(string define)
        {
            var preset = FindPreset(define);
            if (preset == null) return;

            // WebGL always uses Playgama — no explicit selection needed
            if (preset.buildTarget == "WebGL") return;

            var config = Resources.Load<UniBridgeConfig>(nameof(UniBridgeConfig));
            if (config == null || string.IsNullOrEmpty(config.PreferredAdsAdapter))
            {
                UnityEngine.Debug.LogWarning(
                    $"[UniBridge] No preferred ad adapter set in UniBridgeConfig for '{preset.displayName}'. " +
                    "Falling back to priority-based selection. " +
                    "Open UniBridge > Build Manager and click 'Выбрать'.");
                return;
            }

            // "None" — system is intentionally disabled, no warning needed
            if (config.PreferredAdsAdapter == AdapterDefines.NoneAdapterKey) return;

            // Check that the preferred adapter's SDK define is installed
            var group      = EditorUserBuildSettings.selectedBuildTargetGroup;
            var definesRaw = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            if (!definesRaw.Contains(config.PreferredAdsAdapter))
                UnityEngine.Debug.LogWarning(
                    $"[UniBridge] Preferred ad adapter SDK '{config.PreferredAdsAdapter}' is not installed. " +
                    "The build will use Debug adapter at runtime.");
        }

        public void OnPostprocessBuild(BuildTarget target, string path)
        {
            AdapterLinkXmlGenerator.Delete();
        }

        private static StorePreset FindPreset(string define)
        {
            foreach (var p in StorePresetsManager.Load())
                if (p.define == define) return p;
            return null;
        }

        private static void EnsureShareConfigAsset()
        {
            const string assetPath = "Assets/UniBridge/Resources/UniBridgeShareConfig.asset";
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath) != null) return;

            var configType = System.Type.GetType("UniBridge.UniBridgeShareConfig, UniBridge.Share.Runtime");
            if (configType == null) return;

            if (!AssetDatabase.IsValidFolder("Assets/UniBridge/Resources"))
                AssetDatabase.CreateFolder("Assets/UniBridge", "Resources");

            var config = ScriptableObject.CreateInstance(configType);
            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[UniBridge] UniBridgeShareConfig создан автоматически перед сборкой.");
        }
    }
}
