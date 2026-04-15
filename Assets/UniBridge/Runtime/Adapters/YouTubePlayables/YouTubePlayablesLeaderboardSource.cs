#if UNIBRIDGE_YTPLAYABLES && UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace UniBridge
{
    /// <summary>
    /// ILeaderboardSource adapter for YouTube Playables.
    /// YouTube only supports sending a score (sendScore). There is no API to retrieve entries or player rank.
    /// DisplayMode is SubmitOnly — the game should use SimulatedLeaderboardSource for in-game display.
    /// </summary>
    public class YouTubePlayablesLeaderboardSource : ILeaderboardSource
    {
        [DllImport("__Internal")] private static extern void YTPlayables_SendScore(double score, Action<int> onSuccess, Action<int> onFail);

        // ytgame.engagement.sendScore requires value <= Number.MAX_SAFE_INTEGER (2^53 - 1).
        private const long MaxSafeInteger = 9007199254740991L;
        [DllImport("__Internal")] private static extern int  YTPlayables_InPlayablesEnv();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterAdapter()
        {
            LeaderboardSourceRegistry.Register(
                "UNIBRIDGE_YTPLAYABLES",
                config => new YouTubePlayablesLeaderboardSource(),
                100);
            Debug.Log("[UniBridgeLeaderboards] YouTube Playables leaderboard adapter registered");
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
            IsInitialized = true;
            Debug.Log($"[{nameof(YouTubePlayablesLeaderboardSource)}] Initialized");
            onSuccess?.Invoke();
        }

        public void SubmitScore(string leaderboardId, long score, Action<bool> onComplete)
        {
            if (!IsInitialized || !IsSupported)
            {
                onComplete?.Invoke(false);
                return;
            }

            long clamped = score > MaxSafeInteger ? MaxSafeInteger : (score < 0 ? 0 : score);
            _submitCallback = onComplete;
            YTPlayables_SendScore((double)clamped, OnSubmitSuccess, OnSubmitFail);
        }

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnSubmitSuccess(int _)
        {
            var cb = _submitCallback;
            _submitCallback = null;
            cb?.Invoke(true);
        }

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnSubmitFail(int _)
        {
            var cb = _submitCallback;
            _submitCallback = null;
            cb?.Invoke(false);
        }

        public void GetEntries(
            string leaderboardId, int count, LeaderboardTimeScope timeScope,
            Action<bool, IReadOnlyList<LeaderboardEntry>> onComplete)
        {
            // YouTube Playables does not provide a leaderboard entries API
            onComplete?.Invoke(false, null);
        }

        public void GetPlayerEntry(
            string leaderboardId, LeaderboardTimeScope timeScope,
            Action<bool, LeaderboardEntry> onComplete)
        {
            // YouTube Playables does not provide player entry retrieval
            onComplete?.Invoke(false, null);
        }
    }
}
#endif
