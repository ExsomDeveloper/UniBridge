#if UNIBRIDGE_PLAYGAMA && UNITY_WEBGL
using System;
using System.Collections.Generic;
using Playgama;
using Playgama.Modules.Leaderboards;
using UnityEngine;

namespace UniBridge
{
    public class PlaygamaLeaderboardSource : ILeaderboardSource
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterAdapter()
        {
            LeaderboardSourceRegistry.Register(
                "UNIBRIDGE_PLAYGAMA",
                config => new PlaygamaLeaderboardSource(),
                100);
            Debug.Log("[UniBridgeLeaderboards] Playgama leaderboard adapter registered");
        }

        public bool   IsInitialized   { get; private set; }
        public bool   IsSupported     => _type != LeaderboardType.NotAvailable;

        public LeaderboardDisplayMode DisplayMode => _type switch
        {
            LeaderboardType.InGame      => LeaderboardDisplayMode.InGame,
            LeaderboardType.NativePopup => LeaderboardDisplayMode.NativePopup,
            LeaderboardType.Native      => LeaderboardDisplayMode.SubmitOnly,
            _                           => LeaderboardDisplayMode.NotSupported,
        };

        public bool   IsAuthenticated =>
            IsInitialized &&
            IsSupported &&
            (!Bridge.player.isAuthorizationSupported || Bridge.player.isAuthorized);
        public string LocalPlayerName => Bridge.player.isAuthorized ? Bridge.player.name : string.Empty;

        // Leaderboard type is cached during Initialize() and used in all methods.
        // Bridge.leaderboards.type is set by the platform config and does not change at runtime.
        private LeaderboardType _type = LeaderboardType.NotAvailable;

        public void Initialize(
            UniBridgeLeaderboardsConfig config,
            Action onSuccess,
            Action onFailed)
        {
            _type = Bridge.leaderboards.type;

#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
            VLog($"Initialize: leaderboardType={_type}");
#endif
            if (_type == LeaderboardType.NotAvailable)
                Debug.Log($"[{nameof(PlaygamaLeaderboardSource)}]: Leaderboard не поддерживается на этой Playgama-платформе");
            else
                Debug.Log($"[{nameof(PlaygamaLeaderboardSource)}]: Инициализирован (type={_type})");

            if (_type != LeaderboardType.NotAvailable &&
                Bridge.player.isAuthorizationSupported &&
                !Bridge.player.isAuthorized)
                Debug.Log($"[{nameof(PlaygamaLeaderboardSource)}]: Игрок не авторизован — авторизация будет запрошена автоматически при первой операции");

            IsInitialized = true;
            onSuccess?.Invoke();
        }

        public void SubmitScore(
            string leaderboardId,
            long score,
            Action<bool> onComplete)
        {
            if (_type == LeaderboardType.NotAvailable)
            {
                onComplete?.Invoke(false);
                return;
            }

            PlaygamaAuthService.EnsureAuthorized(ok =>
            {
                if (!ok) { onComplete?.Invoke(false); return; }
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
                VLog($"SubmitScore: '{leaderboardId}' score={score}");
#endif
                Bridge.leaderboards.SetScore(leaderboardId, score.ToString(), success =>
                {
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
                    VLog($"SubmitScore result: '{leaderboardId}' success={success}");
#endif
                    if (!success)
                        Debug.LogWarning(
                            $"[{nameof(PlaygamaLeaderboardSource)}]: " +
                            $"SetScore не удался для '{leaderboardId}'");
                    onComplete?.Invoke(success);
                });
            });
        }

        public void GetEntries(
            string leaderboardId,
            int count,
            LeaderboardTimeScope timeScope,
            Action<bool, IReadOnlyList<LeaderboardEntry>> onComplete)
        {
            if (_type == LeaderboardType.NotAvailable)
            {
                onComplete?.Invoke(false, null);
                return;
            }

            if (_type == LeaderboardType.Native)
            {
                Debug.LogWarning(
                    $"[{nameof(PlaygamaLeaderboardSource)}]: " +
                    $"GetEntries недоступен для типа Native на '{leaderboardId}'");
                onComplete?.Invoke(false, null);
                return;
            }

            PlaygamaAuthService.EnsureAuthorized(ok =>
            {
                if (!ok) { onComplete?.Invoke(false, null); return; }

                if (_type == LeaderboardType.NativePopup)
                {
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
                    VLog($"GetEntries: '{leaderboardId}' NativePopup → ShowNativePopup");
#endif
                    Bridge.leaderboards.ShowNativePopup(leaderboardId, _ => onComplete?.Invoke(false, null));
                    return;
                }

#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
                VLog($"GetEntries: '{leaderboardId}' count={count}");
#endif
                Bridge.leaderboards.GetEntries(leaderboardId, (success, entries) =>
                {
                    if (!success)
                    {
                        onComplete?.Invoke(false, null);
                        return;
                    }

                    var result = new List<LeaderboardEntry>();
                    if (entries != null)
                    {
                        int rank = 1;
                        int take = Math.Min(count, entries.Count);
                        for (int i = 0; i < take; i++)
                        {
                            var raw = entries[i];
                            long.TryParse(raw.TryGetValue("score", out var sc) ? sc : "0", out long scoreVal);

                            result.Add(new LeaderboardEntry
                            {
                                PlayerId         = raw.TryGetValue("id",   out var id)   ? id   : "",
                                PlayerName       = raw.TryGetValue("name", out var name) ? name : "",
                                Score            = scoreVal,
                                Rank             = rank++,
                                IsCurrentPlayer  = raw.TryGetValue("isCurrentPlayer", out var icp) && icp == "true",
                                LastReportedDate = DateTime.MinValue
                            });
                        }
                    }
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
                    VLog($"GetEntries result: '{leaderboardId}' count={result.Count}");
#endif
                    onComplete?.Invoke(true, result);
                });
            });
        }

        public void GetPlayerEntry(
            string leaderboardId,
            LeaderboardTimeScope timeScope,
            Action<bool, LeaderboardEntry> onComplete)
        {
            if (_type == LeaderboardType.NotAvailable)
            {
                onComplete?.Invoke(false, null);
                return;
            }

            if (_type == LeaderboardType.Native ||
                _type == LeaderboardType.NativePopup)
            {
                onComplete?.Invoke(false, null);
                return;
            }

            PlaygamaAuthService.EnsureAuthorized(ok =>
            {
                if (!ok) { onComplete?.Invoke(false, null); return; }
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
                VLog($"GetPlayerEntry: '{leaderboardId}'");
#endif
                Bridge.leaderboards.GetEntries(leaderboardId, (success, entries) =>
                {
                    if (!success || entries == null)
                    {
                        onComplete?.Invoke(false, null);
                        return;
                    }

                    int rank = 1;
                    foreach (var raw in entries)
                    {
                        bool isCurrentPlayer = raw.TryGetValue("isCurrentPlayer", out var icp) && icp == "true";
                        if (isCurrentPlayer)
                        {
                            long.TryParse(raw.TryGetValue("score", out var sc) ? sc : "0", out long score);
#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
                            VLog($"GetPlayerEntry result: '{leaderboardId}' rank={rank} score={score}");
#endif
                            onComplete?.Invoke(true, new LeaderboardEntry
                            {
                                PlayerId         = raw.TryGetValue("id",   out var id)   ? id   : "",
                                PlayerName       = raw.TryGetValue("name", out var name) ? name : "",
                                Score            = score,
                                Rank             = rank,
                                IsCurrentPlayer  = true,
                                LastReportedDate = DateTime.MinValue
                            });
                            return;
                        }
                        rank++;
                    }

                    onComplete?.Invoke(false, null);
                });
            });
        }

#if UNIBRIDGELEADERBOARDS_VERBOSE_LOG
        private static void VLog(string msg) => Debug.Log($"[RAT] [{nameof(PlaygamaLeaderboardSource)}] {msg}");
#endif
    }
}
#endif
