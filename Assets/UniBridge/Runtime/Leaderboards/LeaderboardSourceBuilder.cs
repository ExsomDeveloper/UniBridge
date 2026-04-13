using UnityEngine;

namespace UniBridge
{
    public class LeaderboardSourceBuilder
    {
        public ILeaderboardSource Build(UniBridgeLeaderboardsConfig config)
        {
            if (config != null && config.PreferredLeaderboardAdapter == UniBridgeAdapterKeys.None) return null;

#if UNITY_EDITOR
            if (config != null && !string.IsNullOrEmpty(config.PreferredLeaderboardAdapter))
            {
                Debug.Log($"[{nameof(LeaderboardSourceBuilder)}]: Editor — используется SimulatedLeaderboardSource (адаптер: {config.PreferredLeaderboardAdapter}).");
                return new SimulatedLeaderboardSource();
            }
            return new DebugLeaderboardSource();
#else
            if (config != null && config.PreferredLeaderboardAdapter == "UNIBRIDGELEADERBOARDS_SIMULATED")
            {
                Debug.Log($"[{nameof(LeaderboardSourceBuilder)}]: Принудительно выбрана SimulatedLeaderboardSource.");
                return new SimulatedLeaderboardSource();
            }

            var source = LeaderboardSourceRegistry.Create(config);
            if (source != null)
                return source;

            Debug.Log(
                $"[{nameof(LeaderboardSourceBuilder)}]: " +
                "Нет подходящего адаптера лидерборда. Используется SimulatedLeaderboardSource.");

            return new SimulatedLeaderboardSource();
#endif
        }
    }
}
