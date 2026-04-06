using System;
using UnityEngine;

namespace UniBridge
{
    [Serializable]
    public class LeaderboardDefinition
    {
        public string Id;
        public string DisplayName;
        public string GpgsId;
        public string GameCenterId;

        [Header("Симуляция")]
        public LeaderboardSimulationSettings Simulation = new LeaderboardSimulationSettings();
    }
}
