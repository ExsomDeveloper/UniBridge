using UnityEditor;
using UnityEngine;

namespace UniBridge.Editor
{
    [CustomEditor(typeof(UniBridgeConfig))]
    public class UniBridgeConfigEditor : UnityEditor.Editor
    {
        private UniBridgeConfig _config;
        private LevelPlaySettingsDrawer _levelPlayDrawer;
        private PlaygamaSettingsDrawer _playgamaDrawer;
        private YandexSettingsDrawer _yandexDrawer;

        private void OnEnable()
        {
            _config = (UniBridgeConfig)target;
            _levelPlayDrawer = new LevelPlaySettingsDrawer(_config);
            _playgamaDrawer = new PlaygamaSettingsDrawer(_config);
            _yandexDrawer = new YandexSettingsDrawer(_config);
        }

        public override void OnInspectorGUI()
        {
            _config = (target as UniBridgeConfig);
            DrawConfigInspector(_config);
        }

        private void DrawConfigInspector(UniBridgeConfig config)
        {
            // Header
            GUILayout.Label("UniBridge Configuration", GetTitleStyle(), GUILayout.ExpandWidth(true));
            EditorGUILayout.Space(5);

            // General Settings
            EditorGUI.BeginChangeCheck();

            GUILayout.Label("General Settings", GetSubTitleStyle(), GUILayout.ExpandWidth(true));
            config.AutoInitialize = EditorGUILayout.Toggle("Auto Initialize", config.AutoInitialize);
            config.SuccessfulRewardResetInterstitial = EditorGUILayout.Toggle(
                new GUIContent("Reward Resets Interstitial Timer",
                    "If enabled, completing a rewarded ad will reset the interstitial timer"),
                config.SuccessfulRewardResetInterstitial);

            EditorGUILayout.Space(5);
            GUILayout.Label("Age Settings", GetSubTitleStyle(), GUILayout.ExpandWidth(true));
            config.MaxChildrenAge = EditorGUILayout.IntField(
                new GUIContent("Max Children Age",
                    "Users at or below this age will have COPPA-compliant ads"),
                config.MaxChildrenAge);
            config.DefaultUserAge = EditorGUILayout.IntField(
                new GUIContent("Default User Age",
                    "Age used when Initialize() is called without an age parameter"),
                config.DefaultUserAge);

            EditorGUILayout.Space(5);
            GUILayout.Label("Storage", GetSubTitleStyle(), GUILayout.ExpandWidth(true));
            config.AdsDisabledKey = EditorGUILayout.TextField(
                new GUIContent("Ads Disabled Key",
                    "PlayerPrefs key used to persist ad-free status"),
                config.AdsDisabledKey);

            EditorGUILayout.Space(5);
            GUILayout.Label("Saves", GetSubTitleStyle(), GUILayout.ExpandWidth(true));
            config.AutoInitializeSaves = EditorGUILayout.Toggle(
                new GUIContent("Auto Initialize Saves", "Automatically initialize the save system on startup"),
                config.AutoInitializeSaves);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(config);
            }

            // Platform-specific settings
            EditorGUILayout.Space(10);
            DrawPlatformSettings();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPlatformSettings()
        {
            // Show SDK status
            GUILayout.Label("Platform SDK Status", GetTitleStyle(), GUILayout.ExpandWidth(true));
            EditorGUILayout.Space(5);

            // Active store
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Active Store:", GUILayout.Width(150));
            var storeDefine = StorePlatformDefines.GetCurrentStoreDefine();
            if (storeDefine != null)
                EditorGUILayout.LabelField(StorePlatformDefines.GetDisplayName(storeDefine), GetInstalledStyle());
            else
                EditorGUILayout.LabelField("Not Set", GetNotInstalledStyle());
            EditorGUILayout.EndHorizontal();

            // LevelPlay status
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("LevelPlay (Android/iOS):", GUILayout.Width(150));
#if UNIBRIDGE_LEVELPLAY
            EditorGUILayout.LabelField("Installed", GetInstalledStyle());
#else
            EditorGUILayout.LabelField("Not Installed", GetNotInstalledStyle());
#endif
            EditorGUILayout.EndHorizontal();

            // Playgama status
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Playgama (WebGL):", GUILayout.Width(150));
#if UNIBRIDGE_PLAYGAMA
            EditorGUILayout.LabelField("Installed", GetInstalledStyle());
#else
            EditorGUILayout.LabelField("Not Installed", GetNotInstalledStyle());
#endif
            EditorGUILayout.EndHorizontal();

            // Yandex status
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Yandex (Android/iOS):", GUILayout.Width(150));
#if UNIBRIDGE_YANDEX
            EditorGUILayout.LabelField("Installed", GetInstalledStyle());
#else
            EditorGUILayout.LabelField("Not Installed", GetNotInstalledStyle());
#endif
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Always show LevelPlay settings
            GUILayout.Label("LevelPlay Settings (Android/iOS)", GetSubTitleStyle(), GUILayout.ExpandWidth(true));
            _levelPlayDrawer?.DrawInspector();

            EditorGUILayout.Space(10);

            // Always show Playgama settings
            GUILayout.Label("Playgama Settings (WebGL)", GetSubTitleStyle(), GUILayout.ExpandWidth(true));
            _playgamaDrawer?.DrawInspector();

            EditorGUILayout.Space(10);

            // Always show Yandex settings
            GUILayout.Label("Yandex Settings (Android/iOS)", GetSubTitleStyle(), GUILayout.ExpandWidth(true));
            _yandexDrawer?.DrawInspector();
        }

        private GUIStyle GetTitleStyle()
        {
            return new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14
            };
        }

        private GUIStyle GetSubTitleStyle()
        {
            return new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };
        }

        private GUIStyle GetInstalledStyle()
        {
            var style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = new Color(0.2f, 0.8f, 0.2f);
            return style;
        }

        private GUIStyle GetNotInstalledStyle()
        {
            var style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = new Color(0.8f, 0.8f, 0.2f);
            return style;
        }
    }
}
