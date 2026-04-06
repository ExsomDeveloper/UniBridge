using System;
using UnityEngine;

namespace UniBridge
{
    public static class UniBridgeRate
    {
        public static bool   IsInitialized { get; private set; }
        public static bool   IsSupported   => _source?.IsSupported ?? false;
        public static string AdapterName   => _source?.GetType().Name ?? "None";

        public static event Action OnInitSuccess;
        public static event Action OnInitFailed;

        private static IRateSource    _source;
        private static UniBridgeRateConfig  _config;

        // ── Auto-initialization ──────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            IsInitialized = false;
            _source       = null;

            if (_config == null)
                _config = LoadConfig();

            if (_config != null && _config.AutoInitialize)
                SetupRate();
        }

        // ── Manual initialization ────────────────────────────────────────────

        public static void Initialize()
        {
            if (_config == null)
                _config = LoadConfig();

            if (_config == null)
            {
                Debug.LogError(
                    $"[{nameof(UniBridgeRate)}]: UniBridgeRateConfig не найден! " +
                    "Создайте через Assets > Create > UniBridge > Rate Configuration");
                return;
            }

            SetupRate();
        }

        // ── Public API ───────────────────────────────────────────────────────

        public static void RequestReview(Action<bool> onComplete = null)
        {
            if (!EnsureInitialized())
            {
                onComplete?.Invoke(false);
                return;
            }

            _source.RequestReview(onComplete);
        }

        // ── Private methods ──────────────────────────────────────────────────

        private static void SetupRate()
        {
            if (IsInitialized)
                return;

            var builder = new RateSourceBuilder();
            _source = builder.Build(_config);

            if (_source == null)
            {
                Debug.Log($"[{nameof(UniBridgeRate)}]: Rate system disabled.");
                return;
            }

            _source.Initialize(
                _config,
                onSuccess: () =>
                {
                    IsInitialized = true;
                    Debug.Log(
                        $"[{nameof(UniBridgeRate)}]: " +
                        $"Инициализирован с {_source.GetType().Name}");
                    OnInitSuccess?.Invoke();
                },
                onFailed: () =>
                {
                    Debug.LogError(
                        $"[{nameof(UniBridgeRate)}]: Ошибка инициализации");
                    OnInitFailed?.Invoke();
                });
        }

        private static bool EnsureInitialized()
        {
            if (IsInitialized) return true;
            Debug.LogWarning($"[{nameof(UniBridgeRate)}]: Не инициализирован!");
            return false;
        }

        private static UniBridgeRateConfig LoadConfig()
        {
            var config = Resources.Load<UniBridgeRateConfig>(nameof(UniBridgeRateConfig));

            if (config == null)
                Debug.LogWarning(
                    $"[{nameof(UniBridgeRate)}]: " +
                    "UniBridgeRateConfig не найден в папке Resources.");

            return config;
        }
    }
}
