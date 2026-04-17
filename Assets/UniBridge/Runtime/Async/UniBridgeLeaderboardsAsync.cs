#if UNIBRIDGE_UNITASK
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UniBridge.Async
{
    /// <summary>
    /// Async wrappers over <see cref="UniBridgeLeaderboards"/>. Requires `com.cysharp.unitask` package.
    /// </summary>
    public static class UniBridgeLeaderboardsAsync
    {
        public static UniTask<bool> SubmitScoreAsync(string leaderboardId, long score, CancellationToken ct = default)
            => AsyncHelpers.Await<bool>(cb => UniBridgeLeaderboards.SubmitScore(leaderboardId, score, cb), ct);

        public static UniTask<(bool ok, IReadOnlyList<LeaderboardEntry> entries)> GetEntriesAsync(
            string leaderboardId,
            int count,
            LeaderboardTimeScope timeScope,
            CancellationToken ct = default)
            => AsyncHelpers.Await<bool, IReadOnlyList<LeaderboardEntry>>(
                cb => UniBridgeLeaderboards.GetEntries(leaderboardId, count, timeScope, cb), ct);

        public static UniTask<(bool ok, LeaderboardEntry entry)> GetPlayerEntryAsync(
            string leaderboardId,
            LeaderboardTimeScope timeScope,
            CancellationToken ct = default)
            => AsyncHelpers.Await<bool, LeaderboardEntry>(
                cb => UniBridgeLeaderboards.GetPlayerEntry(leaderboardId, timeScope, cb), ct);
    }
}
#endif
