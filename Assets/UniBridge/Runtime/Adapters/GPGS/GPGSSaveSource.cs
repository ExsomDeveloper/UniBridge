#if UNIBRIDGELEADERBOARDS_GPGS && UNITY_ANDROID && UNIBRIDGE_STORE_GOOGLEPLAY && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace UniBridge
{
    /// <summary>
    /// Google Play Saved Games cloud save adapter.
    /// All keys are stored in a single snapshot named "unibridge_saves_data" as a JSON dictionary.
    /// Requires GPGS SDK (UNIBRIDGELEADERBOARDS_GPGS) and Google Play store target.
    /// </summary>
    public class GPGSSaveSource : ISaveSource
    {
        private const string SnapshotName = "unibridge_saves_data";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void RegisterAdapter()
        {
            SaveSourceRegistry.Register("UNIBRIDGESAVES_GPGS", () => new GPGSSaveSource(), 100);
            Debug.Log("[UniBridgeSaves] GPGS save adapter registered");
        }

        private static ISavedGameClient SavedGameClient =>
            PlayGamesPlatform.Instance.SavedGame;

        private static bool IsAuthenticated =>
            Social.localUser.authenticated;

        // ── ISaveSource ──────────────────────────────────────────────────────

        public void Save(string key, string json, Action<bool> onComplete)
        {
            if (!CheckAuth(onComplete)) return;
#if UNIBRIDGESAVES_VERBOSE_LOG
            VLog($"Save: key='{key}'");
#endif
            ReadThenWrite(dict =>
            {
                dict[key] = json;
            }, onComplete);
        }

        public void Load(string key, Action<bool, string> onComplete)
        {
            if (!IsAuthenticated)
            {
                Debug.LogWarning($"[{nameof(GPGSSaveSource)}]: Not authenticated. Cannot load key '{key}'.");
                onComplete?.Invoke(false, null);
                return;
            }

#if UNIBRIDGESAVES_VERBOSE_LOG
            VLog($"Load: key='{key}'");
#endif
            OpenSnapshot((openStatus, metadata) =>
            {
                if (openStatus != SavedGameRequestStatus.Success)
                {
                    Debug.LogWarning($"[{nameof(GPGSSaveSource)}]: Failed to open snapshot for Load (key='{key}'): {openStatus}");
                    onComplete?.Invoke(false, null);
                    return;
                }

                SavedGameClient.ReadBinaryData(metadata, (readStatus, data) =>
                {
                    if (readStatus != SavedGameRequestStatus.Success)
                    {
                        Debug.LogWarning($"[{nameof(GPGSSaveSource)}]: ReadBinaryData failed for key '{key}': {readStatus}");
                        onComplete?.Invoke(false, null);
                        return;
                    }

                    var dict = ParseDict(data);
                    bool found = dict.TryGetValue(key, out string value);
#if UNIBRIDGESAVES_VERBOSE_LOG
                    VLog($"Load result: key='{key}' found={found}");
#endif
                    if (found)
                        onComplete?.Invoke(true, value);
                    else
                        onComplete?.Invoke(false, null);
                });
            });
        }

        public void Delete(string key, Action<bool> onComplete)
        {
            if (!CheckAuth(onComplete)) return;
#if UNIBRIDGESAVES_VERBOSE_LOG
            VLog($"Delete: key='{key}'");
#endif
            ReadThenWrite(dict =>
            {
                dict.Remove(key);
            }, onComplete);
        }

        public void HasKey(string key, Action<bool> onComplete)
        {
            if (!IsAuthenticated)
            {
                onComplete?.Invoke(false);
                return;
            }
#if UNIBRIDGESAVES_VERBOSE_LOG
            VLog($"HasKey: key='{key}'");
#endif
            OpenSnapshot((openStatus, metadata) =>
            {
                if (openStatus != SavedGameRequestStatus.Success)
                {
                    onComplete?.Invoke(false);
                    return;
                }

                SavedGameClient.ReadBinaryData(metadata, (readStatus, data) =>
                {
                    if (readStatus != SavedGameRequestStatus.Success)
                    {
                        onComplete?.Invoke(false);
                        return;
                    }

                    var dict = ParseDict(data);
                    bool has = dict.ContainsKey(key);
#if UNIBRIDGESAVES_VERBOSE_LOG
                    VLog($"HasKey result: key='{key}' has={has}");
#endif
                    onComplete?.Invoke(has);
                });
            });
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private bool CheckAuth(Action<bool> onComplete)
        {
            if (IsAuthenticated) return true;
            Debug.LogWarning($"[{nameof(GPGSSaveSource)}]: Not authenticated.");
            onComplete?.Invoke(false);
            return false;
        }

        /// <summary>Open → ReadBinaryData → modify dict → CommitUpdate.</summary>
        private void ReadThenWrite(Action<Dictionary<string, string>> modify, Action<bool> onComplete)
        {
            OpenSnapshot((openStatus, metadata) =>
            {
                if (openStatus != SavedGameRequestStatus.Success)
                {
                    Debug.LogWarning($"[{nameof(GPGSSaveSource)}]: Failed to open snapshot: {openStatus}");
                    onComplete?.Invoke(false);
                    return;
                }

                SavedGameClient.ReadBinaryData(metadata, (readStatus, data) =>
                {
                    if (readStatus != SavedGameRequestStatus.Success)
                    {
                        Debug.LogWarning($"[{nameof(GPGSSaveSource)}]: ReadBinaryData failed: {readStatus}");
                        onComplete?.Invoke(false);
                        return;
                    }

                    var dict = ParseDict(data);
                    modify(dict);

                    byte[] bytes = Encoding.UTF8.GetBytes(SerializeDict(dict));
                    var update = new SavedGameMetadataUpdate.Builder().Build();

                    SavedGameClient.CommitUpdate(metadata, update, bytes, (commitStatus, _) =>
                    {
                        if (commitStatus != SavedGameRequestStatus.Success)
                            Debug.LogWarning($"[{nameof(GPGSSaveSource)}]: CommitUpdate failed: {commitStatus}");
                        onComplete?.Invoke(commitStatus == SavedGameRequestStatus.Success);
                    });
                });
            });
        }

        private static void OpenSnapshot(Action<SavedGameRequestStatus, ISavedGameMetadata> callback)
        {
            SavedGameClient.OpenWithManualConflictResolution(
                SnapshotName,
                DataSource.ReadNetworkOnly,
                true,
                (resolver, original, _, unmerged, __) =>
                {
                    // Keep the version with longer total play time (most progress)
                    resolver.ChooseMetadata(
                        original.TotalTimePlayed >= unmerged.TotalTimePlayed ? original : unmerged);
                },
                callback);
        }

        private static Dictionary<string, string> ParseDict(byte[] data)
        {
            if (data == null || data.Length == 0)
                return new Dictionary<string, string>();

            try
            {
                string json = Encoding.UTF8.GetString(data);
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(json)
                       ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        private static string SerializeDict(Dictionary<string, string> dict)
        {
            return JsonConvert.SerializeObject(dict);
        }

#if UNIBRIDGESAVES_VERBOSE_LOG
        private static void VLog(string msg) => Debug.Log($"[RAT] [{nameof(GPGSSaveSource)}] {msg}");
#endif
    }
}
#endif
