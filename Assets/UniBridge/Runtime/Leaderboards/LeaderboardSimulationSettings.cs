using System;
using UnityEngine;

namespace UniBridge
{
    [Serializable]
    public class LeaderboardSimulationSettings
    {
        [Tooltip("Количество ботов при первом обращении к лидерборду")]
        public int BotCount = 50;

        [Tooltip("Минимальные очки бота")]
        public long MinScore = 100;

        [Tooltip("Максимальные очки бота")]
        public long MaxScore = 100000;

        [Header("Рост ботов (каждая новая сессия)")]
        [Tooltip("Минимальный прирост очков бота за сессию (когда бот впереди игрока)")]
        public long DailyGrowthMin = 100;

        [Tooltip("Максимальный прирост очков бота за сессию (когда бот впереди игрока)")]
        public long DailyGrowthMax = 1000;

        [Header("Отображение")]
        [Tooltip("Сколько позиций показывать в таблице лидеров")]
        public int LeaderboardSize = 10;
    }
}
