using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniBridge
{
    public static class UniBridgeLeaderboards
    {
        public static bool   IsInitialized   { get; private set; }
        public static bool   IsSupported     => _source?.IsSupported ?? false;
        public static LeaderboardDisplayMode DisplayMode => _source?.DisplayMode ?? LeaderboardDisplayMode.NotSupported;
        public static bool   IsAuthenticated => _source?.IsAuthenticated ?? false;
        public static string AdapterName     => _source?.GetType().Name ?? "None";
        public static string LocalPlayerName => _source?.LocalPlayerName ?? string.Empty;

        public static event Action OnInitSuccess;
        public static event Action OnInitFailed;

        private static ILeaderboardSource    _source;
        private static UniBridgeLeaderboardsConfig _config;

        // ── Auto-initialization ──────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            IsInitialized = false;
            _source       = null;

            if (_config == null)
                _config = LoadConfig();

            if (_config != null && _config.AutoInitialize)
                SetupLeaderboards();
        }

        // ── Manual initialization ────────────────────────────────────────────

        public static void Initialize()
        {
            if (_config == null)
                _config = LoadConfig();

            if (_config == null)
            {
                Debug.LogError(
                    $"[{nameof(UniBridgeLeaderboards)}]: UniBridgeLeaderboardsConfig не найден! " +
                    "Создайте через Assets > Create > UniBridge > Leaderboards Configuration");
                return;
            }

            SetupLeaderboards();
        }

        // ── Public API ───────────────────────────────────────────────────────

        public static void SubmitScore(
            string leaderboardId,
            long score,
            Action<bool> onComplete = null)
        {
            if (!EnsureInitialized())
            {
                onComplete?.Invoke(false);
                return;
            }

            _source.SubmitScore(leaderboardId, score, onComplete);
        }

        public static void GetEntries(
            string leaderboardId,
            int count,
            LeaderboardTimeScope timeScope,
            Action<bool, IReadOnlyList<LeaderboardEntry>> onComplete)
        {
            if (!EnsureInitialized())
            {
                onComplete?.Invoke(false, null);
                return;
            }

            _source.GetEntries(leaderboardId, count, timeScope, onComplete);
        }

        public static void GetPlayerEntry(
            string leaderboardId,
            LeaderboardTimeScope timeScope,
            Action<bool, LeaderboardEntry> onComplete)
        {
            if (!EnsureInitialized())
            {
                onComplete?.Invoke(false, null);
                return;
            }

            _source.GetPlayerEntry(leaderboardId, timeScope, onComplete);
        }

        // ── Private methods ──────────────────────────────────────────────────

        private static void SetupLeaderboards()
        {
            if (IsInitialized)
                return;

            var builder = new LeaderboardSourceBuilder();
            _source = builder.Build(_config);

            if (_source == null)
            {
                Debug.Log($"[{nameof(UniBridgeLeaderboards)}]: Leaderboard system disabled.");
                return;
            }

            _source.Initialize(
                _config,
                onSuccess: () =>
                {
                    IsInitialized = true;
                    Debug.Log(
                        $"[{nameof(UniBridgeLeaderboards)}]: " +
                        $"Инициализирован с {_source.GetType().Name}");
                    OnInitSuccess?.Invoke();
                },
                onFailed: () =>
                {
                    Debug.LogError(
                        $"[{nameof(UniBridgeLeaderboards)}]: Ошибка инициализации");
                    OnInitFailed?.Invoke();
                });
        }

        private static bool EnsureInitialized()
        {
            if (IsInitialized) return true;
            Debug.LogWarning($"[{nameof(UniBridgeLeaderboards)}]: Не инициализирован!");
            return false;
        }

        private static UniBridgeLeaderboardsConfig LoadConfig()
        {
            var config = Resources.Load<UniBridgeLeaderboardsConfig>(nameof(UniBridgeLeaderboardsConfig));

            if (config == null)
                Debug.LogWarning(
                    $"[{nameof(UniBridgeLeaderboards)}]: " +
                    "UniBridgeLeaderboardsConfig не найден в папке Resources.");

            return config;
        }
    }
}
