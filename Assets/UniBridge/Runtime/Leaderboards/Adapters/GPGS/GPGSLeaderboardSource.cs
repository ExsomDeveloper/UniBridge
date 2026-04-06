#if UNIBRIDGELEADERBOARDS_GPGS && UNITY_ANDROID && UNIBRIDGE_STORE_GOOGLEPLAY && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using GooglePlayGames;
using GooglePlayGames.BasicApi;

namespace UniBridge
{
    public class GPGSLeaderboardSource : ILeaderboardSource
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterAdapter()
        {
            LeaderboardSourceRegistry.Register(
                "UNIBRIDGELEADERBOARDS_GPGS",
                config => new GPGSLeaderboardSource(config),
                100);
            Debug.Log("[UniBridgeLeaderboards] GPGS leaderboard adapter registered");
        }

        public bool   IsInitialized   { get; private set; }
        public bool   IsSupported     => true;
        public LeaderboardDisplayMode DisplayMode => LeaderboardDisplayMode.InGame;
        public bool   IsAuthenticated => _authenticated;
        public string LocalPlayerName => Social.localUser.userName;

        private UniBridgeLeaderboardsConfig _config;
        private bool _authenticated;

        public GPGSLeaderboardSource(UniBridgeLeaderboardsConfig config)
        {
            _config = config;
        }

        public void Initialize(
            UniBridgeLeaderboardsConfig config,
            Action onSuccess,
            Action onFailed)
        {
            _config = config;
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
            VLog("Initialize: activating GPGS platform");
#endif
            var configuration = new PlayGamesClientConfiguration.Builder().Build();
            PlayGamesPlatform.InitializeInstance(configuration);
            PlayGamesPlatform.Activate();

            Social.localUser.Authenticate(success =>
            {
                _authenticated = success;
                IsInitialized  = true;
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
                VLog($"Initialize: auth={success}");
#endif
                if (success)
                    Debug.Log($"[{nameof(GPGSLeaderboardSource)}]: Аутентификация успешна");
                else
                    Debug.LogWarning(
                        $"[{nameof(GPGSLeaderboardSource)}]: " +
                        "Аутентификация Google Play Games не удалась — операции лидерборда недоступны");

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

            string gpgsId = ResolveGpgsId(leaderboardId);
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
            VLog($"SubmitScore: '{leaderboardId}' (gpgsId={gpgsId}) score={score}");
#endif
            Social.ReportScore(score, gpgsId, success =>
            {
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
                VLog($"SubmitScore result: '{leaderboardId}' success={success}");
#endif
                if (!success)
                    Debug.LogWarning(
                        $"[{nameof(GPGSLeaderboardSource)}]: " +
                        $"ReportScore не удался для '{leaderboardId}' (gpgsId={gpgsId})");
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

            string gpgsId        = ResolveGpgsId(leaderboardId);
            string localPlayerId = Social.localUser.id;
            var    gpgsTimeSpan  = timeScope == LeaderboardTimeScope.Today
                ? LeaderboardTimeSpan.Today
                : LeaderboardTimeSpan.AllTime;
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
            VLog($"GetEntries: '{leaderboardId}' (gpgsId={gpgsId}) count={count} timeScope={timeScope}");
#endif
            PlayGamesPlatform.Instance.LoadScores(
                gpgsId,
                LeaderboardStart.TopScores,
                count,
                LeaderboardCollection.Public,
                gpgsTimeSpan,
                data =>
                {
                    if (!data.Valid)
                    {
                        Debug.LogWarning(
                            $"[{nameof(GPGSLeaderboardSource)}]: " +
                            $"LoadScores не удался для '{leaderboardId}' (gpgsId={gpgsId})");
                        onComplete?.Invoke(false, null);
                        return;
                    }

                    var result = new List<LeaderboardEntry>(data.Scores.Length);
                    foreach (var score in data.Scores)
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

            string gpgsId       = ResolveGpgsId(leaderboardId);
            var    gpgsTimeSpan = timeScope == LeaderboardTimeScope.Today
                ? LeaderboardTimeSpan.Today
                : LeaderboardTimeSpan.AllTime;
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
            VLog($"GetPlayerEntry: '{leaderboardId}' (gpgsId={gpgsId})");
#endif
            PlayGamesPlatform.Instance.LoadScores(
                gpgsId,
                LeaderboardStart.PlayerCentered,
                1,
                LeaderboardCollection.Public,
                gpgsTimeSpan,
                data =>
                {
                    if (!data.Valid)
                    {
                        Debug.LogWarning(
                            $"[{nameof(GPGSLeaderboardSource)}]: " +
                            $"LoadScores (PlayerCentered) не удался для '{leaderboardId}' (gpgsId={gpgsId})");
                        onComplete?.Invoke(false, null);
                        return;
                    }

                    var local = data.PlayerScore;
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


        private string ResolveGpgsId(string leaderboardId)
        {
            if (_config?.Leaderboards != null)
            {
                foreach (var def in _config.Leaderboards)
                    if (def.Id == leaderboardId)
                        return def.GpgsId;
            }

            return leaderboardId;
        }

#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
        private static void VLog(string msg) => Debug.Log($"[RAT] [{nameof(GPGSLeaderboardSource)}] {msg}");
#endif
    }
}
#endif
