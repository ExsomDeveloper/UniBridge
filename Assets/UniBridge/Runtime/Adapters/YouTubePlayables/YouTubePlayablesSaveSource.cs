#if UNIBRIDGE_YTPLAYABLES && UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace UniBridge
{
    /// <summary>
    /// ISaveSource adapter for YouTube Playables.
    /// YouTube provides a single-blob cloud save (loadData/saveData, max 3 MiB).
    /// This adapter layers key-value semantics on top using a JSON dictionary cached in memory.
    /// </summary>
    public class YouTubePlayablesSaveSource : ISaveSource
    {
        [DllImport("__Internal")] private static extern void YTPlayables_LoadData(Action<string> onSuccess, Action<int> onFail);
        [DllImport("__Internal")] private static extern void YTPlayables_SaveData(string data, Action<int> onSuccess, Action<int> onFail);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterAdapter()
        {
            SaveSourceRegistry.Register("UNIBRIDGE_YTPLAYABLES", () => new YouTubePlayablesSaveSource(), 100);
            Debug.Log("[UniBridgeSaves] YouTube Playables save adapter registered");
        }

        [Serializable]
        private class SaveBlobData
        {
            public List<SaveEntry> entries = new();
        }

        [Serializable]
        private class SaveEntry
        {
            public string k;
            public string v;
        }

        private static YouTubePlayablesSaveSource _instance;
        private Dictionary<string, string> _cache;
        private bool _loaded;
        private bool _loading;
        private readonly List<Action> _pendingOps = new();

        public YouTubePlayablesSaveSource()
        {
            _instance = this;
            LoadBlob();
        }

        public void Save(string key, string json, Action<bool> onComplete)
        {
            EnsureLoaded(() =>
            {
                _cache[key] = json;
                FlushBlob(onComplete);
            });
        }

        public void Load(string key, Action<bool, string> onComplete)
        {
            EnsureLoaded(() =>
            {
                if (_cache.TryGetValue(key, out var value))
                    onComplete?.Invoke(true, value);
                else
                    onComplete?.Invoke(false, null);
            });
        }

        public void Delete(string key, Action<bool> onComplete)
        {
            EnsureLoaded(() =>
            {
                _cache.Remove(key);
                FlushBlob(onComplete);
            });
        }

        public void HasKey(string key, Action<bool> onComplete)
        {
            EnsureLoaded(() =>
            {
                onComplete?.Invoke(_cache.ContainsKey(key));
            });
        }

        private void EnsureLoaded(Action op)
        {
            if (_loaded) { op(); return; }
            _pendingOps.Add(op);
            if (!_loading) LoadBlob();
        }

        private void LoadBlob()
        {
            _loading = true;
            YTPlayables_LoadData(OnLoadSuccess, OnLoadFail);
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void OnLoadSuccess(string data)
        {
            var self = _instance;
            if (self == null) return;
            self._cache = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(data))
            {
                try
                {
                    var blob = JsonUtility.FromJson<SaveBlobData>(data);
                    if (blob?.entries != null)
                        foreach (var e in blob.entries)
                            self._cache[e.k] = e.v;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[YouTubePlayablesSaveSource] Failed to parse save blob: {ex.Message}");
                }
            }
            self._loaded  = true;
            self._loading = false;
            self.DrainPending();
        }

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnLoadFail(int _)
        {
            var self = _instance;
            if (self == null) return;
            Debug.LogWarning("[YouTubePlayablesSaveSource] loadData failed, starting with empty cache");
            self._cache   = new Dictionary<string, string>();
            self._loaded  = true;
            self._loading = false;
            self.DrainPending();
        }

        private void DrainPending()
        {
            var ops = new List<Action>(_pendingOps);
            _pendingOps.Clear();
            foreach (var op in ops) op();
        }

        private readonly List<Action<bool>> _flushCallbacks = new();
        private bool _flushing;
        private bool _dirtyWhileFlushing;

        private void FlushBlob(Action<bool> onComplete)
        {
            _flushCallbacks.Add(onComplete);
            if (_flushing)
            {
                _dirtyWhileFlushing = true;
                return;
            }
            DoFlush();
        }

        private void DoFlush()
        {
            _flushing = true;
            _dirtyWhileFlushing = false;
            var blob = new SaveBlobData();
            foreach (var kvp in _cache)
                blob.entries.Add(new SaveEntry { k = kvp.Key, v = kvp.Value });

            var json = JsonUtility.ToJson(blob);
            YTPlayables_SaveData(json, OnSaveSuccess, OnSaveFail);
        }

        private void OnFlushComplete(bool success)
        {
            if (_dirtyWhileFlushing && success)
            {
                // New writes arrived while flushing — do one more flush with fresh cache.
                // Keep accumulated callbacks for the next round.
                DoFlush();
                return;
            }

            var callbacks = new List<Action<bool>>(_flushCallbacks);
            _flushCallbacks.Clear();
            _flushing = false;
            foreach (var cb in callbacks) cb?.Invoke(success);
        }

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnSaveSuccess(int _) { _instance?.OnFlushComplete(true); }

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnSaveFail(int _)    { _instance?.OnFlushComplete(false); }
    }
}
#endif
