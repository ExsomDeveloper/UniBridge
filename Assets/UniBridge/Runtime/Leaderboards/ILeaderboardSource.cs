using System;
using System.Collections.Generic;

namespace UniBridge
{
    public interface ILeaderboardSource
    {
        bool IsInitialized  { get; }
        bool IsSupported    { get; }
        LeaderboardDisplayMode DisplayMode { get; }
        bool IsAuthenticated { get; }
        string LocalPlayerName { get; }

        void Initialize(UniBridgeLeaderboardsConfig config, Action onSuccess, Action onFailed);

        void SubmitScore(string leaderboardId, long score, Action<bool> onComplete);

        void GetEntries(string leaderboardId, int count, LeaderboardTimeScope timeScope,
                        Action<bool, IReadOnlyList<LeaderboardEntry>> onComplete);

        void GetPlayerEntry(string leaderboardId, LeaderboardTimeScope timeScope,
                            Action<bool, LeaderboardEntry> onComplete);
    }
}
