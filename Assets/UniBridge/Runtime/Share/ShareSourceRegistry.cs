using System;
using System.Collections.Generic;

namespace UniBridge
{
    /// <summary>
    /// Registry of share adapter factories.
    /// Adapters register via [RuntimeInitializeOnLoadMethod(BeforeSceneLoad)].
    /// Virtual keys (UNIBRIDGESHARE_ANDROID, UNIBRIDGESHARE_IOS, UNIBRIDGESHARE_MOCK) are handled in the Builder.
    /// </summary>
    public static class ShareSourceRegistry
    {
        private static readonly Dictionary<string, (Func<UniBridgeShareConfig, IShareSource> Factory, int Priority)> _entries
            = new Dictionary<string, (Func<UniBridgeShareConfig, IShareSource>, int)>();

        /// <summary>
        /// Register an adapter factory keyed by the SDK define (e.g. "UNIBRIDGE_PLAYGAMA").
        /// </summary>
        public static void Register(
            string sdkDefine,
            Func<UniBridgeShareConfig, IShareSource> factory,
            int priority)
        {
            if (!_entries.TryGetValue(sdkDefine, out var existing) || priority > existing.Priority)
                _entries[sdkDefine] = (factory, priority);
        }

        internal static IShareSource Create(UniBridgeShareConfig config)
        {
            if (config != null && !string.IsNullOrEmpty(config.PreferredShareAdapter))
            {
                // Virtual keys are handled in the Builder, not here
                if (_entries.TryGetValue(config.PreferredShareAdapter, out var preferred))
                    return preferred.Factory(config);
                return null; // SDK не установлен → Builder откатится на Mock
            }

            Func<UniBridgeShareConfig, IShareSource> bestFactory = null;
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
