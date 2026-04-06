using System;
using UnityEngine;

namespace UniBridge
{
    [CreateAssetMenu(
        fileName = nameof(UniBridgeAnalyticsConfig),
        menuName = "UniBridge/Analytics Configuration")]
    public class UniBridgeAnalyticsConfig : ScriptableObject
    {
        [Header("Общие настройки")]
        public bool AutoInitialize = true;
        public string PreferredAnalyticsAdapter;

        [Header("AppMetrica")]
        public AppMetricaSettings AppMetrica = new();

        [Serializable]
        public class AppMetricaSettings
        {
            public string ApiKey;
        }
    }
}
