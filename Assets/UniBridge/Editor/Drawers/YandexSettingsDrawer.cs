using UnityEditor;

namespace UniBridge.Editor
{
    public class YandexSettingsDrawer : EmptySettingsDrawer
    {
        private readonly UniBridgeConfig _config;

        public YandexSettingsDrawer(UniBridgeConfig config)
        {
            _config = config;
        }

        protected override void OnDrawInspector()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Banner Ad Units", EditorStyles.boldLabel);
            _config.YandexSettings.BannerAdUnitAndroid =
                EditorGUILayout.TextField("Android", _config.YandexSettings.BannerAdUnitAndroid);
            _config.YandexSettings.BannerAdUnitIOS =
                EditorGUILayout.TextField("iOS", _config.YandexSettings.BannerAdUnitIOS);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Interstitial Ad Units", EditorStyles.boldLabel);
            _config.YandexSettings.InterstitialAdUnitAndroid =
                EditorGUILayout.TextField("Android", _config.YandexSettings.InterstitialAdUnitAndroid);
            _config.YandexSettings.InterstitialAdUnitIOS =
                EditorGUILayout.TextField("iOS", _config.YandexSettings.InterstitialAdUnitIOS);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Rewarded Ad Units", EditorStyles.boldLabel);
            _config.YandexSettings.RewardedAdUnitAndroid =
                EditorGUILayout.TextField("Android", _config.YandexSettings.RewardedAdUnitAndroid);
            _config.YandexSettings.RewardedAdUnitIOS =
                EditorGUILayout.TextField("iOS", _config.YandexSettings.RewardedAdUnitIOS);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_config);
            }
        }
    }
}
