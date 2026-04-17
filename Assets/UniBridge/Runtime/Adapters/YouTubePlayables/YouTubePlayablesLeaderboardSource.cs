#if UNIBRIDGE_YTPLAYABLES && UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
using UnityEngine.Scripting;

namespace UniBridge
{
    /// <summary>
    /// ILeaderboardSource adapter for YouTube Playables.
    /// YouTube only supports sending a score (sendScore). There is no API to retrieve entries or player rank.
    /// DisplayMode is SubmitOnly — the game should use SimulatedLeaderboardSource for in-game display.
    /// </summary>
    [Preserve]
    public class YouTubePlayablesLeaderboardSource : ILeaderboardSource
    {
        [DllImport("__Internal")] private static extern void YTPlayables_SendScore(double score, Action<int> onSuccess, Action<int> onFail);

        // ytgame.engagement.sendScore requires value <= Number.MAX_SAFE_INTEGER (2^53 - 1).
        private const long MaxSafeInteger = 9007199254740991L;
        [DllImport("__Internal")] private static extern int  YTPlayables_InPlayablesEnv();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void RegisterAdapter()
        {
            VerboseLog.Log("YT:LB", "RegisterAdapter enter — AfterAssembliesLoaded");
            LeaderboardSourceRegistry.Register(
                "UNIBRIDGE_YTPLAYABLES",
                config => new YouTubePlayablesLeaderboardSource(),
                100);
            Debug.Log("[UniBridgeLeaderboards] YouTube Playables leaderboard adapter registered");
            VerboseLog.Log("YT:LB", "RegisterAdapter done");
        }

        public bool IsInitialized   { get; private set; }
        public bool IsSupported     => YTPlayables_InPlayablesEnv() == 1;
        public LeaderboardDisplayMode DisplayMode => LeaderboardDisplayMode.SubmitOnly;
        public bool IsAuthenticated => IsInitialized && IsSupported;
        public string LocalPlayerName => string.Empty; // YouTube does not expose player identity

        private static Action<bool> _submitCallback;

        public void Initialize(
            UniBridgeLeaderboardsConfig config,
            Action onSuccess,
            Action onFailed)
        {
            VerboseLog.Log("YT:LB", $"Initialize enter (inPlayablesEnv={YTPlayables_InPlayablesEnv() == 1})");
            IsInitialized = true;
            Debug.Log($"[{nameof(YouTubePlayablesLeaderboardSource)}] Initialized");
            onSuccess?.Invoke();
            VerboseLog.Log("YT:LB", "Initialize done");
        }

        public void SubmitScore(string leaderboardId, long score, Action<bool> onComplete)
        {
            VerboseLog.Log("YT:LB", $"SubmitScore enter (leaderboardId=\"{leaderboardId}\", score={score}, initialized={IsInitialized}, supported={IsSupported})");
            if (!IsInitialized)
            {
                VerboseLog.Warn("YT:LB", "SubmitScore: not initialized → false");
                onComplete?.Invoke(false);
                return;
            }
            if (!IsSupported)
            {
                VerboseLog.Warn("YT:LB", "SubmitScore: not in Playables env → false");
                onComplete?.Invoke(false);
                return;
            }

            long clamped = score > MaxSafeInteger ? MaxSafeInteger : (score < 0 ? 0 : score);
            if (clamped != score)
                VerboseLog.Warn("YT:LB", $"SubmitScore: value clamped {score} → {clamped}");

            if (_submitCallback != null)
                VerboseLog.Warn("YT:LB", "SubmitScore: previous callback not drained — overwriting");

            _submitCallback = onComplete;
            VerboseLog.Log("YT:LB", $"→ YTPlayables_SendScore({clamped}) dispatching");
            YTPlayables_SendScore((double)clamped, OnSubmitSuccess, OnSubmitFail);
        }

        [Preserve, MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnSubmitSuccess(int _)
        {
            VerboseLog.Log("YT:LB", "← SendScore success");
            var cb = _submitCallback;
            _submitCallback = null;
            cb?.Invoke(true);
        }

        [Preserve, MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnSubmitFail(int _)
        {
            VerboseLog.Warn("YT:LB", "← SendScore failed");
            var cb = _submitCallback;
            _submitCallback = null;
            cb?.Invoke(false);
        }

        public void GetEntries(
            string leaderboardId, int count, LeaderboardTimeScope timeScope,
            Action<bool, IReadOnlyList<LeaderboardEntry>> onComplete)
        {
            VerboseLog.Log("YT:LB", "GetEntries called — not supported on YouTube Playables, returning empty");
            onComplete?.Invoke(false, null);
        }

        public void GetPlayerEntry(
            string leaderboardId, LeaderboardTimeScope timeScope,
            Action<bool, LeaderboardEntry> onComplete)
        {
            VerboseLog.Log("YT:LB", "GetPlayerEntry called — not supported on YouTube Playables, returning null");
            onComplete?.Invoke(false, null);
        }
    }
}
#endif
