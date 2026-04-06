using UnityEditor;
using UnityEngine;

namespace UniBridge.Editor
{
    public class LevelPlaySettingsDrawer : EmptySettingsDrawer
    {
        private readonly UniBridgeConfig _config;

        public LevelPlaySettingsDrawer(UniBridgeConfig config)
        {
            _config = config;
        }

        protected override void OnDrawInspector()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("App Keys", EditorStyles.boldLabel);

            _config.LevelPlaySettings.AndroidKey =
                EditorGUILayout.TextField("Android Key", _config.LevelPlaySettings.AndroidKey);

            _config.LevelPlaySettings.IOSKey =
                EditorGUILayout.TextField("iOS Key", _config.LevelPlaySettings.IOSKey);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Banner Ad Units", EditorStyles.boldLabel);

            _config.LevelPlaySettings.BannerAdUnitAndroid =
                EditorGUILayout.TextField("Android", _config.LevelPlaySettings.BannerAdUnitAndroid);

            _config.LevelPlaySettings.BannerAdUnitIOS =
                EditorGUILayout.TextField("iOS", _config.LevelPlaySettings.BannerAdUnitIOS);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Interstitial Ad Units", EditorStyles.boldLabel);

            _config.LevelPlaySettings.InterstitialAdUnitAndroid =
                EditorGUILayout.TextField("Android", _config.LevelPlaySettings.InterstitialAdUnitAndroid);

            _config.LevelPlaySettings.InterstitialAdUnitIOS =
                EditorGUILayout.TextField("iOS", _config.LevelPlaySettings.InterstitialAdUnitIOS);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Reward Ad Units", EditorStyles.boldLabel);

            _config.LevelPlaySettings.RewardAdUnitAndroid =
                EditorGUILayout.TextField("Android", _config.LevelPlaySettings.RewardAdUnitAndroid);

            _config.LevelPlaySettings.RewardAdUnitIOS =
                EditorGUILayout.TextField("iOS", _config.LevelPlaySettings.RewardAdUnitIOS);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_config);
            }
        }
    }
}
