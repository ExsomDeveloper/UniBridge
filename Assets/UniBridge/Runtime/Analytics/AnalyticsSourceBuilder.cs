using UnityEngine;

namespace UniBridge
{
    public class AnalyticsSourceBuilder
    {
        public IAnalyticsSource Build(UniBridgeAnalyticsConfig config)
        {
            if (config != null && config.PreferredAnalyticsAdapter == "UNIBRIDGE_NONE") return null;

#if UNITY_EDITOR
            return new DebugAnalyticsSource();
#else
            var source = AnalyticsSourceRegistry.Create(config);
            if (source != null) return source;

            Debug.LogWarning($"[{nameof(AnalyticsSourceBuilder)}]: Нет адаптера аналитики. Аналитика отключена.");
            return null;
#endif
        }
    }
}
