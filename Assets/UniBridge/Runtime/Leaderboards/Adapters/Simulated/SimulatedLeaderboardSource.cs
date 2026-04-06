using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UniBridge
{
    /// <summary>
    /// Fallback leaderboard adapter for platforms without native support (RuStore, etc.).
    /// Player scores and bot data are persisted in PlayerPrefs.
    ///
    /// First run — bots are generated deterministically from the leaderboard ID.
    /// Each subsequent run — bots grow their scores relative to the player:
    ///   • Bot is ahead of the player → +random from DailyGrowthMin..DailyGrowthMax
    ///   • Player is ahead of the bot → bot catches up by a random % of the gap (1–99 %)
    ///   • 20 % chance to repeat growth once more
    ///   • 30 % chance to spawn a new bot at a random position
    /// </summary>
    public class SimulatedLeaderboardSource : ILeaderboardSource
    {
        public bool   IsInitialized   { get; private set; }
        public bool   IsSupported     => true;
        public LeaderboardDisplayMode DisplayMode => LeaderboardDisplayMode.InGame;
        public bool   IsAuthenticated => IsInitialized;
        public string LocalPlayerName => _settings?.PlayerName ?? "You";

        private SimulationSettings _settings;
        private UniBridgeLeaderboardsConfig _config;

        private const string ScoreKeyPrefix = "unibridge_leaderboards_score_";
        private const string BotsKeyPrefix  = "unibridge_leaderboards_bots_";
        private const string DateKeyPrefix  = "unibridge_leaderboards_date_";

        private const double BotSurgeChance = 0.20;
        private const double NewBotChance   = 0.30;

        // Helper types for serialization via JsonUtility
        [Serializable]
        private class BotListWrapper
        {
            public List<BotData> bots = new List<BotData>();
        }

        [Serializable]
        private class BotData
        {
            public string id;
            public string name;
            public long   score;
        }

        // In-memory bot data cache: leaderboardId → list of bots
        private readonly Dictionary<string, List<BotData>> _botsCache = new Dictionary<string, List<BotData>>();

        public void Initialize(
            UniBridgeLeaderboardsConfig config,
            Action onSuccess,
            Action onFailed)
        {
            _config   = config;
            _settings = config?.SimulationSettings ?? new SimulationSettings();
            IsInitialized = true;

            UpdateAllLeaderboardsOnNewSession();

            Debug.Log($"[{nameof(SimulatedLeaderboardSource)}]: Инициализирован");
            onSuccess?.Invoke();
        }

        public void SubmitScore(
            string leaderboardId,
            long score,
            Action<bool> onComplete)
        {
            string key    = ScoreKeyPrefix + leaderboardId;
            string stored = PlayerPrefs.GetString(key, "0");
            long.TryParse(stored, out long current);

            if (score > current)
            {
                PlayerPrefs.SetString(key, score.ToString());
                PlayerPrefs.SetString(DateKeyPrefix + leaderboardId, DateTime.UtcNow.ToBinary().ToString());
                PlayerPrefs.Save();
            }

            Debug.Log(
                $"[{nameof(SimulatedLeaderboardSource)}]: " +
                $"SubmitScore '{leaderboardId}' = {score} (лучший={Math.Max(score, current)})");

            onComplete?.Invoke(true);
        }

        public void GetEntries(
            string leaderboardId,
            int count,
            LeaderboardTimeScope timeScope,
            Action<bool, IReadOnlyList<LeaderboardEntry>> onComplete)
        {
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
            VLog($"GetEntries: '{leaderboardId}' count={count}");
#endif
            var all  = BuildSortedEntries(leaderboardId);
            int size = GetSim(leaderboardId).LeaderboardSize;
            int take = Math.Min(count, Math.Min(size, all.Count));

            var result = new List<LeaderboardEntry>(take + 1);
            for (int i = 0; i < take; i++)
                result.Add(all[i]);

            // If the player is not in the top — append them at the end
            bool playerInResult = false;
            foreach (var e in result)
                if (e.IsCurrentPlayer) { playerInResult = true; break; }

            if (!playerInResult)
                foreach (var e in all)
                    if (e.IsCurrentPlayer) { result.Add(e); break; }
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
            VLog($"GetEntries result: '{leaderboardId}' returned={result.Count}");
#endif
            onComplete?.Invoke(true, result);
        }

        public void GetPlayerEntry(
            string leaderboardId,
            LeaderboardTimeScope timeScope,
            Action<bool, LeaderboardEntry> onComplete)
        {
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
            VLog($"GetPlayerEntry: '{leaderboardId}'");
#endif
            var all = BuildSortedEntries(leaderboardId);

            foreach (var e in all)
            {
                if (e.IsCurrentPlayer)
                {
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
                    VLog($"GetPlayerEntry result: '{leaderboardId}' rank={e.Rank} score={e.Score}");
#endif
                    onComplete?.Invoke(true, e);
                    return;
                }
            }

            onComplete?.Invoke(false, null);
        }

        // ── Bot update on startup ────────────────────────────────────────────────────────────────────

        private void UpdateAllLeaderboardsOnNewSession()
        {
            if (_config?.Leaderboards == null || _config.Leaderboards.Count == 0)
                return;

            var rng = new System.Random(); // один экземпляр на все лидерборды во избежание одинакового seed

            foreach (var def in _config.Leaderboards)
            {
                if (string.IsNullOrEmpty(def.Id))
                    continue;

                // No bots yet — this is the first run, no update needed
                string prefsKey = BotsKeyPrefix + def.Id;
                if (string.IsNullOrEmpty(PlayerPrefs.GetString(prefsKey, null)))
                    continue;

                UpdateBotsOnNewSession(def.Id, rng);
            }
        }

        private void UpdateBotsOnNewSession(string leaderboardId, System.Random rng)
        {
            var bots = GetOrGenerateBots(leaderboardId);
            long playerScore = GetPlayerScore(leaderboardId);
            var sim = GetSim(leaderboardId);

            // Grow each bot
            foreach (var bot in bots)
            {
                GrowBot(bot, playerScore, rng, sim);

                if (rng.NextDouble() < BotSurgeChance)
                    GrowBot(bot, playerScore, rng, sim);
            }

            // 30 % chance to spawn a new bot
            if (rng.NextDouble() < NewBotChance)
                SpawnNewBot(bots, rng, leaderboardId);

            // Save updated bots
            SaveBots(leaderboardId, bots);

            Debug.Log(
                $"[{nameof(SimulatedLeaderboardSource)}]: " +
                $"UpdateBotsOnNewSession '{leaderboardId}', ботов: {bots.Count}");
        }

        private void GrowBot(BotData bot, long playerScore, System.Random rng, LeaderboardSimulationSettings sim)
        {
            long growthMin = sim.DailyGrowthMin;
            long growthMax = sim.DailyGrowthMax;

            if (growthMin > growthMax)
                (growthMin, growthMax) = (growthMax, growthMin);

            if (bot.score >= playerScore)
            {
                // Bot is ahead of or equal to the player — grows by a fixed random range
                bot.score += growthMin + (long)(rng.NextDouble() * (growthMax - growthMin));
            }
            else
            {
                // Player is ahead — bot catches up by a random % of the gap
                long diff = playerScore - bot.score;
                int percent = rng.Next(1, 100); // 1..99
                bot.score += (long)(percent / 100.0 * diff);
            }
        }

        private void SpawnNewBot(List<BotData> bots, System.Random rng, string leaderboardId)
        {
            if (bots.Count == 0)
                return;

            var sorted   = bots.OrderByDescending(b => b.score).ToList();
            int position = rng.Next(0, sorted.Count);
            int buffer   = rng.Next(1, 11); // 1..10 очков вперёд
            long newScore = sorted[position].score + buffer;

            // Evict the last bot (lowest score) and replace with a new one
            bots.Remove(sorted[sorted.Count - 1]);

            var namePool  = GetBotNames();
            var usedNames = new HashSet<string>(bots.Select(b => b.name));
            string name   = namePool.FirstOrDefault(n => !usedNames.Contains(n))
                         ?? $"{namePool[rng.Next(namePool.Count)]}_{rng.Next(2, 10)}";

            bots.Add(new BotData
            {
                id    = Guid.NewGuid().ToString(),
                name  = name,
                score = newScore
            });
        }

        // ── Building the sorted entry list ───────────────────────────────────────────────────────────────────────

        private List<LeaderboardEntry> BuildSortedEntries(string leaderboardId)
        {
            var bots        = GetOrGenerateBots(leaderboardId);
            long playerScore = GetPlayerScore(leaderboardId);
            string playerName = _settings?.PlayerName ?? "You";
            DateTime playerDate = GetPlayerDate(leaderboardId);

            var all = new List<LeaderboardEntry>(bots.Count + 1);

            foreach (var bot in bots)
            {
                all.Add(new LeaderboardEntry
                {
                    PlayerId         = bot.id,
                    PlayerName       = bot.name,
                    Score            = bot.score,
                    IsCurrentPlayer  = false,
                    LastReportedDate = DateTime.MinValue
                });
            }

            all.Add(new LeaderboardEntry
            {
                PlayerId         = "local_player",
                PlayerName       = playerName,
                Score            = playerScore,
                IsCurrentPlayer  = true,
                LastReportedDate = playerDate
            });

            all.Sort((a, b) => b.Score.CompareTo(a.Score));
            for (int i = 0; i < all.Count; i++)
                all[i].Rank = i + 1;

            return all;
        }

        // ── Bot management (cache + PlayerPrefs) ─────────────────────────────────────────────────────────────────

        private List<BotData> GetOrGenerateBots(string leaderboardId)
        {
            if (_botsCache.TryGetValue(leaderboardId, out var cached))
                return cached;

            string prefsKey = BotsKeyPrefix + leaderboardId;
            string json     = PlayerPrefs.GetString(prefsKey, null);

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<BotListWrapper>(json);
                    if (wrapper?.bots != null && wrapper.bots.Count > 0)
                    {
                        _botsCache[leaderboardId] = wrapper.bots;
                        return wrapper.bots;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning(
                        $"[{nameof(SimulatedLeaderboardSource)}]: " +
                        $"Ошибка десериализации ботов для '{leaderboardId}': {e.Message}. Перегенерация.");
                }
            }

            var bots = GenerateBots(leaderboardId);
            _botsCache[leaderboardId] = bots;
            SaveBots(leaderboardId, bots);

            return bots;
        }

        private void SaveBots(string leaderboardId, List<BotData> bots)
        {
            _botsCache[leaderboardId] = bots;
            var wrapper = new BotListWrapper { bots = bots };
            PlayerPrefs.SetString(BotsKeyPrefix + leaderboardId, JsonUtility.ToJson(wrapper));
            PlayerPrefs.Save();
        }

        private List<BotData> GenerateBots(string leaderboardId)
        {
            var sim = GetSim(leaderboardId);
            int  botCount = Math.Min(sim.BotCount, sim.LeaderboardSize);
            long minScore = sim.MinScore;
            long maxScore = sim.MaxScore;

            int seed = GetDeterministicSeed(leaderboardId);
            var rng  = new System.Random(seed);

            // Shuffle the name pool deterministically so names are not repeated
            var namePool = GetBotNames();
            var shuffled = namePool.ToList();
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            var bots = new List<BotData>(botCount);
            for (int i = 0; i < botCount; i++)
            {
                string name  = i < shuffled.Count
                    ? shuffled[i]
                    : $"{shuffled[i % shuffled.Count]}_{i / shuffled.Count + 1}";
                long score = (long)(rng.NextDouble() * (maxScore - minScore)) + minScore;

                bots.Add(new BotData
                {
                    id    = $"sim_bot_{leaderboardId}_{i}",
                    name  = name,
                    score = score
                });
            }

            return bots;
        }

        private IReadOnlyList<string> GetBotNames()
        {
            if (_settings?.BotNames != null && _settings.BotNames.Count > 0)
                return _settings.BotNames;
            return new[] { "Player1", "Player2", "Player3" };
        }

        private long GetPlayerScore(string leaderboardId)
        {
            string stored = PlayerPrefs.GetString(ScoreKeyPrefix + leaderboardId, "0");
            long.TryParse(stored, out long score);
            return score;
        }

        private DateTime GetPlayerDate(string leaderboardId)
        {
            string stored = PlayerPrefs.GetString(DateKeyPrefix + leaderboardId, null);
            if (string.IsNullOrEmpty(stored)) return DateTime.MinValue;
            if (long.TryParse(stored, out long binary))
                return DateTime.FromBinary(binary);
            return DateTime.MinValue;
        }

        private LeaderboardSimulationSettings GetSim(string leaderboardId)
        {
            if (_config?.Leaderboards != null)
                foreach (var def in _config.Leaderboards)
                    if (def.Id == leaderboardId && def.Simulation != null)
                        return def.Simulation;
            return new LeaderboardSimulationSettings();
        }

        private static int GetDeterministicSeed(string s)
        {
            int hash = 17;
            foreach (char c in s)
                hash = hash * 31 + c;
            return hash;
        }

#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
        private static void VLog(string msg) => Debug.Log($"[RAT] [{nameof(SimulatedLeaderboardSource)}] {msg}");
#endif
    }
}
