using System;
using System.Collections.Generic;

namespace UniBridge
{
    /// <summary>
    /// Registry for ad source factories. Adapters register themselves at runtime.
    /// This pattern avoids cyclic assembly dependencies.
    /// </summary>
    public static class AdSourceRegistry
    {
        private static readonly Dictionary<string, (Func<UniBridgeConfig, IAdSource> Factory, int Priority)> _entries
            = new Dictionary<string, (Func<UniBridgeConfig, IAdSource>, int)>();

        /// <summary>
        /// Register an ad source factory keyed by the SDK define (e.g. "UNIBRIDGE_YANDEX").
        /// If a key is registered twice, the one with higher priority wins.
        /// </summary>
        public static void Register(string sdkDefine, Func<UniBridgeConfig, IAdSource> factory, int priority)
        {
            if (!_entries.TryGetValue(sdkDefine, out var existing) || priority > existing.Priority)
                _entries[sdkDefine] = (factory, priority);
        }

        internal static IAdSource Create(UniBridgeConfig config)
        {
            var preferred = config?.PreferredAdsAdapter;

            if (!string.IsNullOrEmpty(preferred))
            {
                // Preferred adapter explicitly set — use it if installed, else null → Debug fallback
                return _entries.TryGetValue(preferred, out var e) ? e.Factory(config) : null;
            }

            // No preference — fall back to highest-priority registered adapter
            Func<UniBridgeConfig, IAdSource> bestFactory = null;
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
