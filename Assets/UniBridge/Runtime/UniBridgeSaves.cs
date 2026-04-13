using System;
using Newtonsoft.Json;
using UnityEngine;

namespace UniBridge
{
    public static class UniBridgeSaves
    {
        public static bool IsInitialized { get; private set; }
        public static string AdapterName => _source?.GetType().Name ?? UniBridgeAdapterKeys.None;

        private static ISaveSource _source;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            // Reset statics on domain reload
            IsInitialized = false;
            _source = null;

            var config = Resources.Load<UniBridgeSavesConfig>(nameof(UniBridgeSavesConfig));

            bool autoInit;
            if (config != null)
            {
                autoInit = config.AutoInitialize;
            }
            else
            {
                // Backward compatibility: fall back to UniBridgeConfig.AutoInitializeSaves
                var legacyConfig = Resources.Load<UniBridgeConfig>(nameof(UniBridgeConfig));
                autoInit = legacyConfig == null || legacyConfig.AutoInitializeSaves;
            }

            if (autoInit)
                Initialize();
        }

        public static void Initialize()
        {
            if (IsInitialized)
                return;

            var config = Resources.Load<UniBridgeSavesConfig>(nameof(UniBridgeSavesConfig));
            _source = SaveSourceBuilder.Build(config);
            IsInitialized = true;
            Debug.Log($"[{nameof(UniBridgeSaves)}]: Initialized with {_source.GetType().Name}");
        }

        public static void Save<T>(string key, T data, Action<bool> onComplete = null)
        {
            if (!EnsureInitialized())
            {
                onComplete?.Invoke(false);
                return;
            }

            string json;
            try
            {
                json = JsonConvert.SerializeObject(data);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(UniBridgeSaves)}]: Failed to serialize data for key '{key}': {e.Message}");
                onComplete?.Invoke(false);
                return;
            }

            _source.Save(key, json, onComplete);
        }

        public static void Load<T>(string key, Action<bool, T> onComplete)
        {
            if (!EnsureInitialized())
            {
                onComplete?.Invoke(false, default);
                return;
            }

            _source.Load(key, (success, json) =>
            {
                if (!success || string.IsNullOrEmpty(json))
                {
                    onComplete?.Invoke(false, default);
                    return;
                }

                try
                {
                    T data = JsonConvert.DeserializeObject<T>(json);
                    onComplete?.Invoke(true, data);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[{nameof(UniBridgeSaves)}]: Failed to deserialize data for key '{key}': {e.Message}");
                    onComplete?.Invoke(false, default);
                }
            });
        }

        public static void Delete(string key, Action<bool> onComplete = null)
        {
            if (!EnsureInitialized())
            {
                onComplete?.Invoke(false);
                return;
            }

            _source.Delete(key, onComplete);
        }

        public static void HasKey(string key, Action<bool> onComplete)
        {
            if (!EnsureInitialized())
            {
                onComplete?.Invoke(false);
                return;
            }

            _source.HasKey(key, onComplete);
        }

        private static bool EnsureInitialized()
        {
            if (IsInitialized)
                return true;

            Debug.LogWarning($"[{nameof(UniBridgeSaves)}]: Not initialized!");
            return false;
        }
    }
}
