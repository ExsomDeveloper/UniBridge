using System;
using System.Collections.Generic;

namespace UniBridge
{
    /// <summary>
    /// Registry of rating-request adapter factories.
    /// Adapters register via [RuntimeInitializeOnLoadMethod(BeforeSceneLoad)].
    /// </summary>
    public static class RateSourceRegistry
    {
        private static readonly Dictionary<string, (Func<UniBridgeRateConfig, IRateSource> Factory, int Priority)> _entries
            = new Dictionary<string, (Func<UniBridgeRateConfig, IRateSource>, int)>();

        /// <summary>
        /// Register an adapter factory keyed by the SDK define (e.g. "UNIBRIDGERATE_GOOGLEPLAY").
        /// </summary>
        public static void Register(
            string sdkDefine,
            Func<UniBridgeRateConfig, IRateSource> factory,
            int priority)
        {
            if (!_entries.TryGetValue(sdkDefine, out var existing) || priority > existing.Priority)
                _entries[sdkDefine] = (factory, priority);
        }

        internal static IRateSource Create(UniBridgeRateConfig config)
        {
            if (config != null && !string.IsNullOrEmpty(config.PreferredRateAdapter))
            {
                // "UNIBRIDGERATE_MOCK" and "UNITY_IOS_STOREREVIEW" are handled in the Builder
                if (_entries.TryGetValue(config.PreferredRateAdapter, out var preferred))
                    return preferred.Factory(config);
                return null; // SDK не установлен → Builder откатится на Mock
            }

            Func<UniBridgeRateConfig, IRateSource> bestFactory = null;
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
