using System.Collections.Generic;
using UnityEngine;

namespace UniBridge
{
    [CreateAssetMenu(
        fileName = nameof(UniBridgeLeaderboardsConfig),
        menuName = "UniBridge/Leaderboards Configuration")]
    public class UniBridgeLeaderboardsConfig : ScriptableObject
    {
        [Header("Общие настройки")]
        public bool AutoInitialize = true;
        public string PreferredLeaderboardAdapter;

        [Header("Лидерборды")]
        [SerializeField]
        private List<LeaderboardDefinition> _leaderboards = new List<LeaderboardDefinition>();
        public IReadOnlyList<LeaderboardDefinition> Leaderboards => _leaderboards;

        [Header("Симуляция (резерв для платформ без нативного лидерборда)")]
        [SerializeField]
        private SimulationSettings _simulationSettings = new SimulationSettings();
        public SimulationSettings SimulationSettings => _simulationSettings;
    }
}
