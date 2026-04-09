using System;
using UnityEngine;

namespace UniBridge
{
    public static class UniBridgeShare
    {
        public static bool   IsInitialized { get; private set; }
        public static bool   IsSupported   => _source?.IsSupported ?? false;
        public static string AdapterName   => _source?.GetType().Name ?? "None";

        public static event Action OnInitSuccess;
        public static event Action OnInitFailed;

        private static IShareSource   _source;
        private static UniBridgeShareConfig _config;

        // ── Auto-initialization ──────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            IsInitialized = false;
            _source       = null;

            if (_config == null)
                _config = LoadConfig();

            if (_config != null && _config.AutoInitialize)
                SetupShare();
        }

        // ── Manual initialization ────────────────────────────────────────────

        public static void Initialize()
        {
            if (_config == null)
                _config = LoadConfig();

            if (_config == null)
            {
                Debug.LogError(
                    $"[{nameof(UniBridgeShare)}]: UniBridgeShareConfig не найден! " +
                    "Создайте через Assets > Create > UniBridge > Share Configuration");
                return;
            }

            SetupShare();
        }

        // ── Public API ───────────────────────────────────────────────────────

        internal static void Share(ShareData data, Action<ShareSheetResult, ShareError> onComplete = null)
        {
            if (!EnsureInitialized())
            {
                onComplete?.Invoke(null, new ShareError("UniBridgeShare не инициализирован"));
                return;
            }

            if (data == null)
            {
                Debug.LogWarning($"[{nameof(UniBridgeShare)}]: ShareData is null");
                onComplete?.Invoke(null, new ShareError("ShareData is null"));
                return;
            }

            _source.Share(data, onComplete);
        }

        // ── Private methods ──────────────────────────────────────────────────

        private static void SetupShare()
        {
            if (IsInitialized)
                return;

            var builder = new ShareSourceBuilder();
            _source = builder.Build(_config);

            if (_source == null)
            {
                Debug.Log($"[{nameof(UniBridgeShare)}]: Share system disabled.");
                return;
            }

            _source.Initialize(
                _config,
                onSuccess: () =>
                {
                    IsInitialized = true;
                    Debug.Log(
                        $"[{nameof(UniBridgeShare)}]: " +
                        $"Инициализирован с {_source.GetType().Name}");
                    OnInitSuccess?.Invoke();
                },
                onFailed: () =>
                {
                    Debug.LogError(
                        $"[{nameof(UniBridgeShare)}]: Ошибка инициализации");
                    OnInitFailed?.Invoke();
                });
        }

        private static bool EnsureInitialized()
        {
            if (IsInitialized) return true;
            Debug.LogWarning($"[{nameof(UniBridgeShare)}]: Не инициализирован!");
            return false;
        }

        private static UniBridgeShareConfig LoadConfig()
        {
            var config = Resources.Load<UniBridgeShareConfig>(nameof(UniBridgeShareConfig));

            if (config == null)
                Debug.LogWarning(
                    $"[{nameof(UniBridgeShare)}]: " +
                    "UniBridgeShareConfig не найден в папке Resources.");

            return config;
        }
    }
}
