using System;
using System.Collections.Generic;

namespace UniBridge
{
    /// <summary>
    /// Registry of leaderboard adapter factories.
    /// Adapters register via [RuntimeInitializeOnLoadMethod(SubsystemRegistration)].
    /// </summary>
    public static class LeaderboardSourceRegistry
    {
        private static readonly Dictionary<string, (Func<UniBridgeLeaderboardsConfig, ILeaderboardSource> Factory, int Priority)> _entries
            = new Dictionary<string, (Func<UniBridgeLeaderboardsConfig, ILeaderboardSource>, int)>();

        /// <summary>
        /// Register an adapter factory keyed by the SDK define (e.g. "UNIBRIDGELEADERBOARDS_GPGS").
        /// </summary>
        public static void Register(
            string sdkDefine,
            Func<UniBridgeLeaderboardsConfig, ILeaderboardSource> factory,
            int priority)
        {
            if (!_entries.TryGetValue(sdkDefine, out var existing) || priority > existing.Priority)
                _entries[sdkDefine] = (factory, priority);
        }

        internal static ILeaderboardSource Create(UniBridgeLeaderboardsConfig config)
        {
            if (config != null && !string.IsNullOrEmpty(config.PreferredLeaderboardAdapter))
            {
                // "UNIBRIDGELEADERBOARDS_SIMULATED" is handled in the Builder, not in the Registry
                if (_entries.TryGetValue(config.PreferredLeaderboardAdapter, out var preferred))
                    return preferred.Factory(config);
                return null; // SDK не установлен → Builder откатится на Simulated
            }

            Func<UniBridgeLeaderboardsConfig, ILeaderboardSource> bestFactory = null;
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
