#if UNITY_IOS && UNIBRIDGE_STORE_APPSTORE && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.SocialPlatforms.GameCenter;

namespace UniBridge
{
    public class GameCenterLeaderboardSource : ILeaderboardSource
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterAdapter()
        {
            LeaderboardSourceRegistry.Register(
                "UNITY_IOS_GAMECENTER",
                config => new GameCenterLeaderboardSource(config),
                90);
            Debug.Log("[UniBridgeLeaderboards] Game Center leaderboard adapter registered");
        }

        public bool   IsInitialized   { get; private set; }
        public bool   IsSupported     => true;
        public LeaderboardDisplayMode DisplayMode => LeaderboardDisplayMode.InGame;
        public bool   IsAuthenticated => _authenticated;
        public string LocalPlayerName => Social.localUser.userName;

        private UniBridgeLeaderboardsConfig _config;
        private bool _authenticated;

        public GameCenterLeaderboardSource(UniBridgeLeaderboardsConfig config)
        {
            _config = config;
        }

        public void Initialize(
            UniBridgeLeaderboardsConfig config,
            Action onSuccess,
            Action onFailed)
        {
            _config = config;
            GameCenterPlatform.ShowDefaultAchievementCompletionBanner(true);
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
            VLog("Initialize: authenticating with Game Center");
#endif
            Social.localUser.Authenticate(success =>
            {
                _authenticated = success;
                IsInitialized  = true;
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
                VLog($"Initialize: auth={success}");
#endif
                if (success)
                    Debug.Log($"[{nameof(GameCenterLeaderboardSource)}]: Аутентификация успешна");
                else
                    Debug.LogWarning(
                        $"[{nameof(GameCenterLeaderboardSource)}]: " +
                        "Аутентификация Game Center не удалась — операции лидерборда недоступны");

                // Always call onSuccess — UniBridgeLeaderboards initializes,
                // individual calls will fail when !_authenticated
                onSuccess?.Invoke();
            });
        }

        public void SubmitScore(
            string leaderboardId,
            long score,
            Action<bool> onComplete)
        {
            if (!_authenticated)
            {
                onComplete?.Invoke(false);
                return;
            }

            string gcId = ResolveGameCenterId(leaderboardId);
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
            VLog($"SubmitScore: '{leaderboardId}' (gcId={gcId}) score={score}");
#endif
            Social.ReportScore(score, gcId, success =>
            {
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
                VLog($"SubmitScore result: '{leaderboardId}' success={success}");
#endif
                if (!success)
                    Debug.LogWarning(
                        $"[{nameof(GameCenterLeaderboardSource)}]: " +
                        $"ReportScore не удался для '{leaderboardId}' (gcId={gcId})");
                onComplete?.Invoke(success);
            });
        }

        public void GetEntries(
            string leaderboardId,
            int count,
            LeaderboardTimeScope timeScope,
            Action<bool, IReadOnlyList<LeaderboardEntry>> onComplete)
        {
            if (!_authenticated)
            {
                onComplete?.Invoke(false, null);
                return;
            }

            string gcId = ResolveGameCenterId(leaderboardId);
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
            VLog($"GetEntries: '{leaderboardId}' (gcId={gcId}) count={count} timeScope={timeScope}");
#endif
            var lb = Social.CreateLeaderboard();
            lb.id        = gcId;
            lb.range     = new Range(1, count);
            lb.timeScope = timeScope == LeaderboardTimeScope.Today ? TimeScope.Today : TimeScope.AllTime;

            lb.LoadScores(success =>
            {
                if (!success)
                {
                    onComplete?.Invoke(false, null);
                    return;
                }

                string localPlayerId = Social.localUser.id;
                var result = new List<LeaderboardEntry>(lb.scores.Length);

                foreach (var score in lb.scores)
                {
                    result.Add(new LeaderboardEntry
                    {
                        PlayerId         = score.userID,
                        PlayerName       = score.userName,
                        Score            = score.value,
                        Rank             = score.rank,
                        IsCurrentPlayer  = score.userID == localPlayerId,
                        LastReportedDate = score.date
                    });
                }
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
                VLog($"GetEntries result: '{leaderboardId}' count={result.Count}");
#endif
                onComplete?.Invoke(true, result);
            });
        }

        public void GetPlayerEntry(
            string leaderboardId,
            LeaderboardTimeScope timeScope,
            Action<bool, LeaderboardEntry> onComplete)
        {
            if (!_authenticated)
            {
                onComplete?.Invoke(false, null);
                return;
            }

            string gcId = ResolveGameCenterId(leaderboardId);
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
            VLog($"GetPlayerEntry: '{leaderboardId}' (gcId={gcId})");
#endif
            var lb = Social.CreateLeaderboard();
            lb.id        = gcId;
            lb.range     = new Range(1, 1);
            lb.timeScope = timeScope == LeaderboardTimeScope.Today ? TimeScope.Today : TimeScope.AllTime;

            lb.LoadScores(success =>
            {
                if (!success)
                {
                    onComplete?.Invoke(false, null);
                    return;
                }

                var local = lb.localUserScore;
                if (local == null)
                {
                    onComplete?.Invoke(false, null);
                    return;
                }
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
                VLog($"GetPlayerEntry result: '{leaderboardId}' rank={local.rank} score={local.value}");
#endif
                onComplete?.Invoke(true, new LeaderboardEntry
                {
                    PlayerId         = Social.localUser.id,
                    PlayerName       = Social.localUser.userName,
                    Score            = local.value,
                    Rank             = local.rank,
                    IsCurrentPlayer  = true,
                    LastReportedDate = local.date
                });
            });
        }


        private string ResolveGameCenterId(string leaderboardId)
        {
            if (_config?.Leaderboards != null)
            {
                foreach (var def in _config.Leaderboards)
                    if (def.Id == leaderboardId)
                        return def.GameCenterId;
            }

            return leaderboardId;
        }

#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
        private static void VLog(string msg) => Debug.Log($"[RAT] [{nameof(GameCenterLeaderboardSource)}] {msg}");
#endif
    }
}
#endif
