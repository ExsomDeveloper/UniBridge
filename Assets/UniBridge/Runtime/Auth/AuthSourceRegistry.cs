using System;
using System.Collections.Generic;

namespace UniBridge
{
    /// <summary>
    /// Registry of authorization adapter factories.
    /// Adapters register via [RuntimeInitializeOnLoadMethod(SubsystemRegistration)].
    /// </summary>
    public static class AuthSourceRegistry
    {
        private static readonly Dictionary<string, (Func<UniBridgeAuthConfig, IAuthSource> Factory, int Priority)> _entries
            = new Dictionary<string, (Func<UniBridgeAuthConfig, IAuthSource>, int)>();

        /// <summary>
        /// Register an adapter factory keyed by the SDK define (e.g. "UNIBRIDGE_PLAYGAMA").
        /// </summary>
        public static void Register(
            string sdkDefine,
            Func<UniBridgeAuthConfig, IAuthSource> factory,
            int priority)
        {
            if (!_entries.TryGetValue(sdkDefine, out var existing) || priority > existing.Priority)
                _entries[sdkDefine] = (factory, priority);
        }

        internal static IAuthSource Create(UniBridgeAuthConfig config)
        {
            if (config != null && !string.IsNullOrEmpty(config.PreferredAuthAdapter))
            {
                // "UNIBRIDGEAUTH_MOCK" and "UNITY_IOS_GAMECENTER" are handled in the Builder
                if (_entries.TryGetValue(config.PreferredAuthAdapter, out var preferred))
                    return preferred.Factory(config);
                return null; // SDK не установлен → Builder откатится на Mock
            }

            Func<UniBridgeAuthConfig, IAuthSource> bestFactory = null;
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
