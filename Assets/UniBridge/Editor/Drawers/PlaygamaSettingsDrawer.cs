using UnityEditor;

namespace UniBridge.Editor
{
    public class PlaygamaSettingsDrawer : EmptySettingsDrawer
    {
        private readonly UniBridgeConfig _config;
        private SerializedObject _serializedConfig;
        private SerializedProperty _disableBannerProp;
        private SerializedProperty _disableInterstitialProp;
        private SerializedProperty _disableRewardProp;

        public PlaygamaSettingsDrawer(UniBridgeConfig config)
        {
            _config = config;
        }

        private void EnsureSerializedProperties()
        {
            if (_serializedConfig != null) return;
            _serializedConfig = new SerializedObject(_config);

            const string settingsPath = "_playgamaSettings.";
            const string backingField = "k__BackingField";
            _disableBannerProp       = _serializedConfig.FindProperty($"{settingsPath}<DisableBannerOnPlatforms>{backingField}");
            _disableInterstitialProp = _serializedConfig.FindProperty($"{settingsPath}<DisableInterstitialOnPlatforms>{backingField}");
            _disableRewardProp       = _serializedConfig.FindProperty($"{settingsPath}<DisableRewardOnPlatforms>{backingField}");
        }

        protected override void OnDrawInspector()
        {
            EnsureSerializedProperties();
            _serializedConfig.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Banner Settings", EditorStyles.boldLabel);

            _config.PlaygamaSettings.AutoShowBanner =
                EditorGUILayout.Toggle("Auto Show Banner", _config.PlaygamaSettings.AutoShowBanner);

            if (_disableBannerProp != null)
                EditorGUILayout.PropertyField(_disableBannerProp, new UnityEngine.GUIContent("Disable Banner On Platforms"), true);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Interstitial Settings", EditorStyles.boldLabel);

            _config.PlaygamaSettings.MinInterstitialInterval =
                EditorGUILayout.IntField("Min Interval (seconds)", _config.PlaygamaSettings.MinInterstitialInterval);

            _config.PlaygamaSettings.YandexInterstitialCountdownSeconds =
                EditorGUILayout.IntField("Yandex Countdown (seconds)", _config.PlaygamaSettings.YandexInterstitialCountdownSeconds);

            _config.PlaygamaSettings.SimulateYandexCountdownInEditor =
                EditorGUILayout.Toggle("Simulate Yandex Countdown (Editor)", _config.PlaygamaSettings.SimulateYandexCountdownInEditor);

            if (_disableInterstitialProp != null)
                EditorGUILayout.PropertyField(_disableInterstitialProp, new UnityEngine.GUIContent("Disable Interstitial On Platforms"), true);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Rewarded Settings", EditorStyles.boldLabel);

            _config.PlaygamaSettings.PreloadRewardedAds =
                EditorGUILayout.Toggle("Preload Rewarded Ads", _config.PlaygamaSettings.PreloadRewardedAds);

            if (_disableRewardProp != null)
                EditorGUILayout.PropertyField(_disableRewardProp, new UnityEngine.GUIContent("Disable Reward On Platforms"), true);

            if (EditorGUI.EndChangeCheck())
            {
                _serializedConfig.ApplyModifiedProperties();
                EditorUtility.SetDirty(_config);
            }
        }
    }
}
