using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniBridge
{
    /// <summary>
    /// Editor stub. Returns deterministic fake data.
    /// </summary>
    public class DebugLeaderboardSource : ILeaderboardSource
    {
        public bool   IsInitialized   { get; private set; }
        public bool   IsSupported     => true;
        public LeaderboardDisplayMode DisplayMode => LeaderboardDisplayMode.InGame;
        public bool   IsAuthenticated => IsInitialized;
        public string LocalPlayerName => _settings?.PlayerName ?? "You";

        private SimulationSettings _settings;
        private readonly Dictionary<string, long>                  _playerScores = new Dictionary<string, long>();
        private readonly Dictionary<string, List<LeaderboardEntry>> _cache        = new Dictionary<string, List<LeaderboardEntry>>();

        public void Initialize(
            UniBridgeLeaderboardsConfig config,
            Action onSuccess,
            Action onFailed)
        {
            _settings = config?.SimulationSettings ?? new SimulationSettings();
            IsInitialized = true;
            Debug.Log($"[{nameof(DebugLeaderboardSource)}]: Инициализирован");
            onSuccess?.Invoke();
        }

        public void SubmitScore(
            string leaderboardId,
            long score,
            Action<bool> onComplete)
        {
            Debug.Log($"[{nameof(DebugLeaderboardSource)}]: SubmitScore '{leaderboardId}' = {score}");

            if (!_playerScores.TryGetValue(leaderboardId, out var current) || score > current)
            {
                _playerScores[leaderboardId] = score;
                _cache.Remove(leaderboardId);
            }

            onComplete?.Invoke(true);
        }

        public void GetEntries(
            string leaderboardId,
            int count,
            LeaderboardTimeScope timeScope,
            Action<bool, IReadOnlyList<LeaderboardEntry>> onComplete)
        {
            Debug.Log($"[{nameof(DebugLeaderboardSource)}]: GetEntries '{leaderboardId}' count={count}");
            var entries = BuildEntries(leaderboardId, count);
            onComplete?.Invoke(true, entries);
        }

        public void GetPlayerEntry(
            string leaderboardId,
            LeaderboardTimeScope timeScope,
            Action<bool, LeaderboardEntry> onComplete)
        {
            Debug.Log($"[{nameof(DebugLeaderboardSource)}]: GetPlayerEntry '{leaderboardId}'");
            var all = BuildEntries(leaderboardId, int.MaxValue);

            foreach (var e in all)
            {
                if (e.IsCurrentPlayer)
                {
                    onComplete?.Invoke(true, e);
                    return;
                }
            }

            onComplete?.Invoke(false, null);
        }

        private List<LeaderboardEntry> BuildEntries(string leaderboardId, int count)
        {
            if (!_cache.TryGetValue(leaderboardId, out var cached))
            {
                long playerScore = _playerScores.TryGetValue(leaderboardId, out var s) ? s : 500L;

                var all = new List<LeaderboardEntry>();

                var botNames = (_settings?.BotNames != null && _settings.BotNames.Count > 0)
                    ? _settings.BotNames
                    : null;

                for (int i = 0; i < 10; i++)
                {
                    string botName = (botNames != null && i < botNames.Count)
                        ? botNames[i]
                        : $"Bot_{i}";

                    all.Add(new LeaderboardEntry
                    {
                        PlayerId         = $"debug_bot_{i}",
                        PlayerName       = botName,
                        Score            = 1000L - i * 80L,
                        IsCurrentPlayer  = false,
                        LastReportedDate = DateTime.MinValue
                    });
                }

                all.Add(new LeaderboardEntry
                {
                    PlayerId         = "debug_player",
                    PlayerName       = _settings?.PlayerName ?? "You",
                    Score            = playerScore,
                    IsCurrentPlayer  = true,
                    LastReportedDate = DateTime.Now
                });

                all.Sort((a, b) => b.Score.CompareTo(a.Score));
                for (int i = 0; i < all.Count; i++)
                    all[i].Rank = i + 1;

                _cache[leaderboardId] = all;
                cached = all;
            }

            int take   = Math.Min(count, cached.Count);
            var result = new List<LeaderboardEntry>(take);
            for (int i = 0; i < take; i++)
                result.Add(cached[i]);

            return result;
        }
    }
}
