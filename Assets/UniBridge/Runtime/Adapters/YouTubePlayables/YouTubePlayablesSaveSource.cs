#if UNIBRIDGE_YTPLAYABLES && UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
using UnityEngine.Scripting;

namespace UniBridge
{
    /// <summary>
    /// ISaveSource adapter for YouTube Playables.
    /// YouTube provides a single-blob cloud save (loadData/saveData, max 3 MiB).
    /// This adapter layers key-value semantics on top using a JSON dictionary cached in memory.
    /// </summary>
    [Preserve]
    public class YouTubePlayablesSaveSource : ISaveSource
    {
        [DllImport("__Internal")] private static extern void YTPlayables_LoadData(Action<string> onSuccess, Action<int> onFail);
        [DllImport("__Internal")] private static extern void YTPlayables_SaveData(string data, Action<int> onSuccess, Action<int> onFail);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void RegisterAdapter()
        {
            VerboseLog.Log("YT:Save", "RegisterAdapter enter — AfterAssembliesLoaded");
            SaveSourceRegistry.Register("UNIBRIDGE_YTPLAYABLES", () => new YouTubePlayablesSaveSource(), 100);
            Debug.Log("[UniBridgeSaves] YouTube Playables save adapter registered");
            VerboseLog.Log("YT:Save", "RegisterAdapter done");
        }

        [Serializable, Preserve]
        private class SaveBlobData
        {
            [Preserve] public List<SaveEntry> entries = new();
        }

        [Serializable, Preserve]
        private class SaveEntry
        {
            [Preserve] public string k;
            [Preserve] public string v;
        }

        private const int MaxLoadRetries = 3;

        private static YouTubePlayablesSaveSource _instance;
        private Dictionary<string, string> _cache;
        private bool _loaded;
        private bool _loading;
        private int _loadRetryCount;
        private readonly List<Action> _pendingOps = new();

        public YouTubePlayablesSaveSource()
        {
            VerboseLog.Log("YT:Save", "ctor begin");
            _instance = this;
            UniBridgeEnvironment.PauseStateChanged += OnPauseStateChanged;
            LoadBlob();
            VerboseLog.Log("YT:Save", "ctor done — LoadBlob dispatched");
        }

        private void OnPauseStateChanged(bool paused)
        {
            VerboseLog.Log("YT:Save", $"OnPauseStateChanged paused={paused} loaded={_loaded} flushing={_flushing} cacheCount={_cache?.Count ?? -1}");
            if (!paused || !_loaded || _flushing || _cache == null) return;
            Debug.Log($"[{nameof(YouTubePlayablesSaveSource)}] Pause received — forcing flush");
            DoFlush();
        }

        public void Save(string key, string json, Action<bool> onComplete)
        {
            VerboseLog.Log("YT:Save", $"Save(key=\"{key}\", payloadBytes={json?.Length ?? 0}) — loaded={_loaded}");
            EnsureLoaded(() =>
            {
                _cache[key] = json;
                FlushBlob(onComplete);
            });
        }

        public void Load(string key, Action<bool, string> onComplete)
        {
            VerboseLog.Log("YT:Save", $"Load(key=\"{key}\") — loaded={_loaded}");
            EnsureLoaded(() =>
            {
                if (_cache.TryGetValue(key, out var value))
                {
                    VerboseLog.Log("YT:Save", $"Load hit (key=\"{key}\", {value?.Length ?? 0} chars)");
                    onComplete?.Invoke(true, value);
                }
                else
                {
                    VerboseLog.Log("YT:Save", $"Load miss (key=\"{key}\")");
                    onComplete?.Invoke(false, null);
                }
            });
        }

        public void Delete(string key, Action<bool> onComplete)
        {
            VerboseLog.Log("YT:Save", $"Delete(key=\"{key}\")");
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
                var has = _cache.ContainsKey(key);
                VerboseLog.Log("YT:Save", $"HasKey(\"{key}\") → {has}");
                onComplete?.Invoke(has);
            });
        }

        private void EnsureLoaded(Action op)
        {
            if (_loaded) { op(); return; }
            VerboseLog.Log("YT:Save", $"EnsureLoaded: queuing op (pending={_pendingOps.Count + 1}, loading={_loading})");
            _pendingOps.Add(op);
            if (!_loading) LoadBlob();
        }

        private void LoadBlob()
        {
            VerboseLog.Log("YT:Save", $"LoadBlob → YTPlayables_LoadData (retry #{_loadRetryCount})");
            _loading = true;
            YTPlayables_LoadData(OnLoadSuccess, OnLoadFail);
        }

        [Preserve, MonoPInvokeCallback(typeof(Action<string>))]
        private static void OnLoadSuccess(string data)
        {
            VerboseLog.Log("YT:Save", $"← LoadData success ({data?.Length ?? 0} chars)");
            var self = _instance;
            if (self == null) { VerboseLog.Warn("YT:Save", "OnLoadSuccess: _instance is null"); return; }
            self._cache = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(data))
            {
                try
                {
                    var blob = JsonUtility.FromJson<SaveBlobData>(data);
                    if (blob?.entries != null)
                    {
                        foreach (var e in blob.entries)
                            self._cache[e.k] = e.v;
                        VerboseLog.Log("YT:Save", $"OnLoadSuccess parsed {blob.entries.Count} entries");
                    }
                    else
                    {
                        VerboseLog.Warn("YT:Save", "OnLoadSuccess: blob.entries is null after deserialize");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[{nameof(YouTubePlayablesSaveSource)}] Failed to parse save blob: {ex.Message}");
                    VerboseLog.Error("YT:Save", $"OnLoadSuccess parse failure: {ex}");
                }
            }
            else
            {
                VerboseLog.Log("YT:Save", "OnLoadSuccess: empty blob (first run)");
            }
            self._loaded       = true;
            self._loading      = false;
            self._loadRetryCount = 0;
            VerboseLog.Log("YT:Save", $"OnLoadSuccess: draining {self._pendingOps.Count} pending ops");
            self.DrainPending();
        }

        [Preserve, MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnLoadFail(int _)
        {
            VerboseLog.Warn("YT:Save", "← LoadData failed");
            var self = _instance;
            if (self == null) { VerboseLog.Warn("YT:Save", "OnLoadFail: _instance is null"); return; }
            self._loading = false;

            if (self._loadRetryCount < MaxLoadRetries)
            {
                float delay = 1f * (1 << self._loadRetryCount); // 1s, 2s, 4s
                self._loadRetryCount++;
                VerboseLog.Warn("YT:Save", $"OnLoadFail: scheduling retry {self._loadRetryCount}/{MaxLoadRetries} in {delay}s");
                Debug.LogWarning($"[{nameof(YouTubePlayablesSaveSource)}] loadData failed, retry {self._loadRetryCount}/{MaxLoadRetries} in {delay}s");
                RetryHelper.InvokeAfter(delay, () =>
                {
                    if (_instance != null && !_instance._loaded && !_instance._loading)
                        _instance.LoadBlob();
                    else
                        VerboseLog.Log("YT:Save", "OnLoadFail retry skipped (state changed while waiting)");
                });
                return;
            }

            VerboseLog.Error("YT:Save", $"LoadData abandoned after {MaxLoadRetries} retries — writes disabled until next successful load");
            Debug.LogError($"[{nameof(YouTubePlayablesSaveSource)}] loadData failed after {MaxLoadRetries} retries; saves disabled until next successful load");
            self.FailPending();
        }

        private void FailPending()
        {
            VerboseLog.Warn("YT:Save", $"FailPending: dropping {_pendingOps.Count} queued ops (callers will not receive callbacks for this attempt)");
            _pendingOps.Clear();
            _loadRetryCount = 0;
        }

        private void DrainPending()
        {
            var ops = new List<Action>(_pendingOps);
            _pendingOps.Clear();
            VerboseLog.Log("YT:Save", $"DrainPending: running {ops.Count} queued ops");
            foreach (var op in ops) op();
        }

        private readonly List<Action<bool>> _flushCallbacks = new();
        private bool _flushing;
        private bool _dirtyWhileFlushing;

        private void FlushBlob(Action<bool> onComplete)
        {
            VerboseLog.Log("YT:Save", $"FlushBlob enter (flushing={_flushing}, queuedCallbacks={_flushCallbacks.Count + 1})");
            _flushCallbacks.Add(onComplete);
            if (_flushing)
            {
                _dirtyWhileFlushing = true;
                VerboseLog.Log("YT:Save", "FlushBlob: flush already in progress — marking dirty, will chain");
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
            VerboseLog.Log("YT:Save", $"DoFlush → YTPlayables_SaveData (entries={blob.entries.Count}, payload={json.Length} chars)");
            YTPlayables_SaveData(json, OnSaveSuccess, OnSaveFail);
        }

        private void OnFlushComplete(bool success)
        {
            VerboseLog.Log("YT:Save", $"OnFlushComplete success={success} dirty={_dirtyWhileFlushing} queuedCallbacks={_flushCallbacks.Count}");
            if (_dirtyWhileFlushing && success)
            {
                VerboseLog.Log("YT:Save", "OnFlushComplete: re-flushing due to dirty-while-flushing");
                DoFlush();
                return;
            }

            var callbacks = new List<Action<bool>>(_flushCallbacks);
            _flushCallbacks.Clear();
            _flushing = false;
            VerboseLog.Log("YT:Save", $"OnFlushComplete: firing {callbacks.Count} callbacks with success={success}");
            foreach (var cb in callbacks) cb?.Invoke(success);
        }

        [Preserve, MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnSaveSuccess(int _) { VerboseLog.Log("YT:Save", "← SaveData success"); _instance?.OnFlushComplete(true); }

        [Preserve, MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnSaveFail(int _)    { VerboseLog.Warn("YT:Save", "← SaveData failed"); _instance?.OnFlushComplete(false); }
    }
}
#endif
