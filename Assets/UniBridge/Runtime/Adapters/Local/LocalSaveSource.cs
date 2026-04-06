using System;
using System.IO;
using UnityEngine;

namespace UniBridge
{
    public class LocalSaveSource : ISaveSource
    {
        private readonly string _savePath;

        public LocalSaveSource()
        {
            _savePath = Path.Combine(Application.persistentDataPath, "saves");
            if (!Directory.Exists(_savePath))
                Directory.CreateDirectory(_savePath);
        }

        public void Save(string key, string json, Action<bool> onComplete)
        {
#if UNIBRIDGESAVES_VERBOSE_LOG
            VLog($"Save: key='{key}'");
#endif
            try
            {
                var filePath = GetFilePath(key);
                File.WriteAllText(filePath, json);
#if UNIBRIDGESAVES_VERBOSE_LOG
                VLog($"Save success: key='{key}' path='{filePath}'");
#endif
                onComplete?.Invoke(true);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(LocalSaveSource)}]: Failed to save key '{key}': {e.Message}");
                onComplete?.Invoke(false);
            }
        }

        public void Load(string key, Action<bool, string> onComplete)
        {
#if UNIBRIDGESAVES_VERBOSE_LOG
            VLog($"Load: key='{key}'");
#endif
            try
            {
                var filePath = GetFilePath(key);
                if (!File.Exists(filePath))
                {
#if UNIBRIDGESAVES_VERBOSE_LOG
                    VLog($"Load: key='{key}' not found");
#endif
                    onComplete?.Invoke(false, null);
                    return;
                }

                var json = File.ReadAllText(filePath);
#if UNIBRIDGESAVES_VERBOSE_LOG
                VLog($"Load success: key='{key}'");
#endif
                onComplete?.Invoke(true, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(LocalSaveSource)}]: Failed to load key '{key}': {e.Message}");
                onComplete?.Invoke(false, null);
            }
        }

        public void Delete(string key, Action<bool> onComplete)
        {
#if UNIBRIDGESAVES_VERBOSE_LOG
            VLog($"Delete: key='{key}'");
#endif
            try
            {
                var filePath = GetFilePath(key);
                if (File.Exists(filePath))
                    File.Delete(filePath);
#if UNIBRIDGESAVES_VERBOSE_LOG
                VLog($"Delete success: key='{key}'");
#endif
                onComplete?.Invoke(true);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(LocalSaveSource)}]: Failed to delete key '{key}': {e.Message}");
                onComplete?.Invoke(false);
            }
        }

        public void HasKey(string key, Action<bool> onComplete)
        {
#if UNIBRIDGESAVES_VERBOSE_LOG
            VLog($"HasKey: key='{key}'");
#endif
            try
            {
                var filePath = GetFilePath(key);
                bool has = File.Exists(filePath);
#if UNIBRIDGESAVES_VERBOSE_LOG
                VLog($"HasKey result: key='{key}' has={has}");
#endif
                onComplete?.Invoke(has);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(LocalSaveSource)}]: Failed to check key '{key}': {e.Message}");
                onComplete?.Invoke(false);
            }
        }

#if UNIBRIDGESAVES_VERBOSE_LOG
        private static void VLog(string msg) => Debug.Log($"[RAT] [{nameof(LocalSaveSource)}] {msg}");
#endif

        private string GetFilePath(string key)
        {
            var sanitized = SanitizeFileName(key);
            return Path.Combine(_savePath, sanitized + ".json");
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            foreach (var c in invalid)
                name = name.Replace(c, '_');
            return name;
        }
    }
}
