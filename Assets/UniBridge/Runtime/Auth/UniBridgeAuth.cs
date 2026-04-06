using System;
using UnityEngine;

namespace UniBridge
{
    public static class UniBridgeAuth
    {
        public static bool   IsInitialized { get; private set; }
        public static bool   IsSupported   => _source?.IsSupported  ?? false;
        public static bool   IsAuthorized  => _source?.IsAuthorized ?? false;
        public static string AdapterName   => _source?.GetType().Name ?? "None";

        public static event Action OnInitSuccess;
        public static event Action OnInitFailed;

        private static IAuthSource   _source;
        private static UniBridgeAuthConfig _config;

        // ── Auto-initialization ──────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            IsInitialized = false;
            _source       = null;

            if (_config == null)
                _config = LoadConfig();

            if (_config != null && _config.AutoInitialize)
                SetupAuth();
        }

        // ── Manual initialization ────────────────────────────────────────────

        public static void Initialize()
        {
            if (_config == null)
                _config = LoadConfig();

            if (_config == null)
            {
                Debug.LogError(
                    $"[{nameof(UniBridgeAuth)}]: UniBridgeAuthConfig не найден! " +
                    "Создайте через Assets > Create > UniBridge > Auth Configuration");
                return;
            }

            SetupAuth();
        }

        // ── Public API ───────────────────────────────────────────────────────

        public static void Authorize(Action<bool> onComplete = null)
        {
            if (!EnsureInitialized())
            {
                onComplete?.Invoke(false);
                return;
            }

            _source.Authorize(onComplete);
        }

        // ── Private methods ──────────────────────────────────────────────────

        private static void SetupAuth()
        {
            if (IsInitialized)
                return;

            var builder = new AuthSourceBuilder();
            _source = builder.Build(_config);

            if (_source == null)
            {
                Debug.Log($"[{nameof(UniBridgeAuth)}]: Auth system disabled.");
                return;
            }

            _source.Initialize(
                _config,
                onSuccess: () =>
                {
                    IsInitialized = true;
                    Debug.Log(
                        $"[{nameof(UniBridgeAuth)}]: " +
                        $"Инициализирован с {_source.GetType().Name}");
                    OnInitSuccess?.Invoke();
                },
                onFailed: () =>
                {
                    Debug.LogError(
                        $"[{nameof(UniBridgeAuth)}]: Ошибка инициализации");
                    OnInitFailed?.Invoke();
                });
        }

        private static bool EnsureInitialized()
        {
            if (IsInitialized) return true;
            Debug.LogWarning($"[{nameof(UniBridgeAuth)}]: Не инициализирован!");
            return false;
        }

        private static UniBridgeAuthConfig LoadConfig()
        {
            var config = Resources.Load<UniBridgeAuthConfig>(nameof(UniBridgeAuthConfig));

            if (config == null)
                Debug.LogWarning(
                    $"[{nameof(UniBridgeAuth)}]: " +
                    "UniBridgeAuthConfig не найден в папке Resources.");

            return config;
        }
    }
}
