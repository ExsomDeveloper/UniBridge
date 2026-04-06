#if UNITY_IOS && UNIBRIDGE_STORE_APPSTORE && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UniBridge
{
    /// <summary>
    /// iOS iCloud Key-Value Store save adapter.
    /// Uses NSUbiquitousKeyValueStore via a native Obj-C plugin.
    /// Virtual key "UNITY_IOS_ICLOUD" — built-in iOS, no separate SDK needed.
    /// Requires iCloud Key-Value capability enabled in Xcode.
    /// Falls back to LocalSaveSource when iCloud is unavailable (user not signed in to Apple ID).
    /// </summary>
    public class iCloudSaveSource : ISaveSource
    {
        [DllImport("__Internal")] private static extern bool    UniBridgeSaves_IsAvailable();
        [DllImport("__Internal")] private static extern string  UniBridgeSaves_GetString(string key);
        [DllImport("__Internal")] private static extern void    UniBridgeSaves_SetString(string key, string value);
        [DllImport("__Internal")] private static extern void    UniBridgeSaves_Remove(string key);
        [DllImport("__Internal")] private static extern bool    UniBridgeSaves_HasKey(string key);
        [DllImport("__Internal")] private static extern void    UniBridgeSaves_Synchronize();

        private readonly ISaveSource _fallback;

        public iCloudSaveSource()
        {
            if (!UniBridgeSaves_IsAvailable())
            {
                Debug.LogWarning($"[{nameof(iCloudSaveSource)}]: iCloud Key-Value Store is not available " +
                                 "(user not signed in to Apple ID or iCloud capability missing). " +
                                 "Falling back to local file storage.");
                _fallback = new LocalSaveSource();
            }
        }

        public void Save(string key, string json, Action<bool> onComplete)
        {
            if (_fallback != null) { _fallback.Save(key, json, onComplete); return; }
#if UNIBRIDGESAVES_VERBOSE_LOG
            VLog($"Save: key='{key}'");
#endif
            try
            {
                UniBridgeSaves_SetString(key, json);
                UniBridgeSaves_Synchronize();
#if UNIBRIDGESAVES_VERBOSE_LOG
                VLog($"Save success: key='{key}'");
#endif
                onComplete?.Invoke(true);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(iCloudSaveSource)}]: Failed to save key '{key}': {e.Message}");
                onComplete?.Invoke(false);
            }
        }

        public void Load(string key, Action<bool, string> onComplete)
        {
            if (_fallback != null) { _fallback.Load(key, onComplete); return; }
#if UNIBRIDGESAVES_VERBOSE_LOG
            VLog($"Load: key='{key}'");
#endif
            try
            {
                string value = UniBridgeSaves_GetString(key);
                bool found = !string.IsNullOrEmpty(value);
#if UNIBRIDGESAVES_VERBOSE_LOG
                VLog($"Load result: key='{key}' found={found}");
#endif
                if (found)
                    onComplete?.Invoke(true, value);
                else
                    onComplete?.Invoke(false, null);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(iCloudSaveSource)}]: Failed to load key '{key}': {e.Message}");
                onComplete?.Invoke(false, null);
            }
        }

        public void Delete(string key, Action<bool> onComplete)
        {
            if (_fallback != null) { _fallback.Delete(key, onComplete); return; }
#if UNIBRIDGESAVES_VERBOSE_LOG
            VLog($"Delete: key='{key}'");
#endif
            try
            {
                UniBridgeSaves_Remove(key);
                UniBridgeSaves_Synchronize();
#if UNIBRIDGESAVES_VERBOSE_LOG
                VLog($"Delete success: key='{key}'");
#endif
                onComplete?.Invoke(true);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(iCloudSaveSource)}]: Failed to delete key '{key}': {e.Message}");
                onComplete?.Invoke(false);
            }
        }

        public void HasKey(string key, Action<bool> onComplete)
        {
            if (_fallback != null) { _fallback.HasKey(key, onComplete); return; }
#if UNIBRIDGESAVES_VERBOSE_LOG
            VLog($"HasKey: key='{key}'");
#endif
            try
            {
                bool has = UniBridgeSaves_HasKey(key);
#if UNIBRIDGESAVES_VERBOSE_LOG
                VLog($"HasKey result: key='{key}' has={has}");
#endif
                onComplete?.Invoke(has);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(iCloudSaveSource)}]: Failed to check key '{key}': {e.Message}");
                onComplete?.Invoke(false);
            }
        }

#if UNIBRIDGESAVES_VERBOSE_LOG
        private static void VLog(string msg) => Debug.Log($"[RAT] [{nameof(iCloudSaveSource)}] {msg}");
#endif
    }
}
#endif
