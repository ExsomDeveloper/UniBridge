using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniBridge
{
    [Serializable]
    public class SimulationSettings
    {
        [Tooltip("Отображаемое имя текущего игрока")]
        public string PlayerName = "You";

        [Header("Имена ботов")]
        [Tooltip("Пул имён для генерации ботов. Имена не повторяются при генерации.")]
        public List<string> BotNames = new List<string>
        {
            "Oliver", "Ethan", "Liam", "Noah", "Mason",
            "Lucas", "Aiden", "Logan", "Jackson", "Sebastian",
            "Carter", "Owen", "Caleb", "Henry", "Ryan",
            "Nathan", "Wyatt", "Tyler", "Brandon", "Dylan",
            "Emma", "Sophia", "Olivia", "Ava", "Isabella",
            "Mia", "Charlotte", "Harper", "Amelia", "Evelyn"
        };
    }
}
