#if UNIBRIDGE_PLAYGAMA && UNITY_WEBGL
using System;
using Playgama;
using Playgama.Modules.Storage;
using UnityEngine;

namespace UniBridge
{
    public class PlaygamaSaveSource : ISaveSource
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void RegisterAdapter()
        {
            SaveSourceRegistry.Register("UNIBRIDGE_PLAYGAMA", () => new PlaygamaSaveSource(), 100);
            Debug.Log("[UniBridgeSaves] Playgama save adapter registered");
        }

        public void Save(string key, string json, Action<bool> onComplete)
        {
#if UNIBRIDGESAVES_VERBOSE_LOG
            VLog($"Save: key='{key}'");
#endif
            var storageType = GetStorageType();
            if (storageType == null) { onComplete?.Invoke(false); return; }
            try
            {
                Bridge.storage.Set(key, json, (success) =>
                {
#if UNIBRIDGESAVES_VERBOSE_LOG
                    VLog($"Save result: key='{key}' success={success}");
#endif
                    onComplete?.Invoke(success);
                }, storageType.Value);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(PlaygamaSaveSource)}]: Failed to save key '{key}': {e.Message}");
                onComplete?.Invoke(false);
            }
        }

        public void Load(string key, Action<bool, string> onComplete)
        {
#if UNIBRIDGESAVES_VERBOSE_LOG
            VLog($"Load: key='{key}'");
#endif
            var storageType = GetStorageType();
            if (storageType == null) { onComplete?.Invoke(false, null); return; }
            try
            {
                Bridge.storage.Get(key, (success, data) =>
                {
#if UNIBRIDGESAVES_VERBOSE_LOG
                    VLog($"Load result: key='{key}' found={success && data != null}");
#endif
                    if (success && data != null)
                        onComplete?.Invoke(true, data);
                    else
                        onComplete?.Invoke(false, null);
                }, storageType.Value);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(PlaygamaSaveSource)}]: Failed to load key '{key}': {e.Message}");
                onComplete?.Invoke(false, null);
            }
        }

        public void Delete(string key, Action<bool> onComplete)
        {
#if UNIBRIDGESAVES_VERBOSE_LOG
            VLog($"Delete: key='{key}'");
#endif
            var storageType = GetStorageType();
            if (storageType == null) { onComplete?.Invoke(false); return; }
            try
            {
                Bridge.storage.Delete(key, (success) =>
                {
#if UNIBRIDGESAVES_VERBOSE_LOG
                    VLog($"Delete result: key='{key}' success={success}");
#endif
                    onComplete?.Invoke(success);
                }, storageType.Value);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(PlaygamaSaveSource)}]: Failed to delete key '{key}': {e.Message}");
                onComplete?.Invoke(false);
            }
        }

        public void HasKey(string key, Action<bool> onComplete)
        {
#if UNIBRIDGESAVES_VERBOSE_LOG
            VLog($"HasKey: key='{key}'");
#endif
            var storageType = GetStorageType();
            if (storageType == null) { onComplete?.Invoke(false); return; }
            try
            {
                Bridge.storage.Get(key, (success, data) =>
                {
                    bool has = success && data != null;
#if UNIBRIDGESAVES_VERBOSE_LOG
                    VLog($"HasKey result: key='{key}' has={has}");
#endif
                    onComplete?.Invoke(has);
                }, storageType.Value);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(PlaygamaSaveSource)}]: Failed to check key '{key}': {e.Message}");
                onComplete?.Invoke(false);
            }
        }

        private static StorageType? GetStorageType()
        {
            var type = Bridge.storage.defaultType;
            if (!Bridge.storage.IsSupported(type) || !Bridge.storage.IsAvailable(type))
            {
                Debug.LogWarning($"[{nameof(PlaygamaSaveSource)}]: Storage type '{type}' is not supported or not available (user may not be signed in). Operation skipped.");
                return null;
            }
            return type;
        }

#if UNIBRIDGESAVES_VERBOSE_LOG
        private static void VLog(string msg) => Debug.Log($"[RAT] [{nameof(PlaygamaSaveSource)}] {msg}");
#endif
    }
}
#endif
