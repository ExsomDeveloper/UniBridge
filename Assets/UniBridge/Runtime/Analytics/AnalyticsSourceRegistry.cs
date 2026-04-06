using System;
using System.Collections.Generic;

namespace UniBridge
{
    /// <summary>
    /// Registry of analytics adapter factories.
    /// Adapters register via [RuntimeInitializeOnLoadMethod(BeforeSceneLoad)].
    /// </summary>
    public static class AnalyticsSourceRegistry
    {
        private static readonly Dictionary<string, (Func<UniBridgeAnalyticsConfig, IAnalyticsSource> Factory, int Priority)> _entries
            = new Dictionary<string, (Func<UniBridgeAnalyticsConfig, IAnalyticsSource>, int)>();

        /// <summary>
        /// Register an adapter factory keyed by the SDK define (e.g. "UNIBRIDGEANALYTICS_APPMETRICA").
        /// </summary>
        public static void Register(
            string sdkDefine,
            Func<UniBridgeAnalyticsConfig, IAnalyticsSource> factory,
            int priority)
        {
            if (!_entries.TryGetValue(sdkDefine, out var existing) || priority > existing.Priority)
                _entries[sdkDefine] = (factory, priority);
        }

        internal static IAnalyticsSource Create(UniBridgeAnalyticsConfig config)
        {
            if (config != null && !string.IsNullOrEmpty(config.PreferredAnalyticsAdapter))
            {
                if (_entries.TryGetValue(config.PreferredAnalyticsAdapter, out var preferred))
                    return preferred.Factory(config);
                return null; // SDK не установлен → Builder вернёт null (аналитика отключена)
            }

            Func<UniBridgeAnalyticsConfig, IAnalyticsSource> bestFactory = null;
            int bestPriority = -1;

            foreach (var entry in _entries.Values)
            {
                if (entry.Priority > bestPriority)
                {
                    bestPriority = entry.Priority;
                    bestFactory  = entry.Factory;
                }
            }

            return bestFactory?.Invoke(config);
        }

        internal static bool HasAny => _entries.Count > 0;
    }
}
