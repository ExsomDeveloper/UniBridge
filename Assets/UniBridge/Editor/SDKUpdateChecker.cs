using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;

namespace UniBridge.Editor
{
    [InitializeOnLoad]
    public static class SDKUpdateChecker
    {
        private const string LastCheckPrefKey = "UniBridge_LastUpdateCheck";
        private const double CheckIntervalDays = 2.0;
        private const int RequestTimeoutSeconds = 10;

        private static readonly string CacheDir =
            Path.Combine("Library", "UniBridge");
        private static readonly string CachePath =
            Path.Combine("Library", "UniBridge", "sdk_latest_versions.json");

        private static int _pendingRequests;
        private static Dictionary<string, string> _results;
        private static LatestVersionCache _cache;
        private static bool _checking;

        public static bool IsCheckInProgress => _checking;
        public static event Action OnCheckComplete;

        // ── UPM search state ────────────────────────────────────────────────

        private class UpmSearchState
        {
            public string Key;
            public SearchRequest Request;
        }

        private static readonly List<UpmSearchState> _upmSearches = new();

        // ── HTTP request state ──────────────────────────────────────────────

        private class HttpRequestState
        {
            public string Key;
            public UnityWebRequest Request;
            public UnityWebRequestAsyncOperation Op;
            public string SourceType; // "github", "openupm", "maven"
        }

        private static readonly List<HttpRequestState> _httpRequests = new();

        // ── Startup ─────────────────────────────────────────────────────────

        static SDKUpdateChecker()
        {
            EditorApplication.delayCall += CheckIfDue;
        }

        private static void CheckIfDue()
        {
            try
            {
                var lastStr = EditorPrefs.GetString(LastCheckPrefKey, "");
                if (!string.IsNullOrEmpty(lastStr) &&
                    DateTime.TryParse(lastStr, null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out var lastCheck))
                {
                    if ((DateTime.UtcNow - lastCheck).TotalDays < CheckIntervalDays)
                        return;
                }

                RunCheck();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UniBridge] Update check scheduling failed: {ex.Message}");
            }
        }

        [MenuItem("UniBridge/Check for SDK Updates", false, 61)]
        public static void ForceCheck()
        {
            RunCheck();
        }

        // ── Main check logic ────────────────────────────────────────────────

        private static void RunCheck()
        {
            if (_checking) return;
            _checking = true;
            _results = new Dictionary<string, string>();
            _upmSearches.Clear();
            _httpRequests.Clear();
            _pendingRequests = 0;

            var versions = SDKInstallerWindow.LoadRequiredVersions();
            if (versions == null)
            {
                Debug.LogWarning("[UniBridge] Could not load SDKVersions.json for update check");
                FinishCheck();
                return;
            }

            ScheduleSdk("levelplay", versions.levelplay);
            ScheduleSdk("playgama", versions.playgama);
            ScheduleSdk("edm4u", versions.edm4u);
            ScheduleSdk("yandex", versions.yandex);
            ScheduleSdk("unityiap", versions.unityiap);
            ScheduleSdk("rustore", versions.rustore);
            ScheduleSdk("gpgs", versions.gpgs);
            ScheduleSdk("googlePlayReview", versions.googlePlayReview);
            ScheduleSdk("rustoreReview", versions.rustoreReview);
            ScheduleSdk("appmetrica", versions.appmetrica);

            if (_pendingRequests == 0)
            {
                FinishCheck();
                return;
            }

            EditorApplication.update += PollRequests;
        }

        private static void ScheduleSdk(string key, SDKInfo info)
        {
            if (info?.latestCheckSource == null) return;
            var src = info.latestCheckSource;
            if (string.IsNullOrEmpty(src.type) || src.type == "none") return;

            try
            {
                switch (src.type)
                {
                    case "upm":
                        ScheduleUpmSearch(key, info.packageId);
                        break;
                    case "openupm":
                        ScheduleHttpRequest(key, $"https://package.openupm.com/{info.packageId}", "openupm");
                        break;
                    case "github":
                        if (!string.IsNullOrEmpty(src.repo))
                            ScheduleHttpRequest(key,
                                $"https://api.github.com/repos/{src.repo}/tags?per_page=1", "github");
                        break;
                    case "maven":
                        if (!string.IsNullOrEmpty(src.group) && !string.IsNullOrEmpty(src.artifact) &&
                            !string.IsNullOrEmpty(src.repoUrl))
                        {
                            var groupPath = src.group.Replace('.', '/');
                            ScheduleHttpRequest(key,
                                $"{src.repoUrl}/{groupPath}/{src.artifact}/maven-metadata.xml", "maven");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UniBridge] Failed to schedule update check for {key}: {ex.Message}");
            }
        }

        private static void ScheduleUpmSearch(string key, string packageId)
        {
            if (string.IsNullOrEmpty(packageId)) return;
            var req = Client.Search(packageId);
            _upmSearches.Add(new UpmSearchState { Key = key, Request = req });
            _pendingRequests++;
        }

        private static void ScheduleHttpRequest(string key, string url, string sourceType)
        {
            var req = UnityWebRequest.Get(url);
            req.timeout = RequestTimeoutSeconds;
            if (sourceType == "github")
                req.SetRequestHeader("User-Agent", "UniBridge-Unity-Editor");
            var op = req.SendWebRequest();
            _httpRequests.Add(new HttpRequestState
            {
                Key = key, Request = req, Op = op, SourceType = sourceType
            });
            _pendingRequests++;
        }

        // ── Polling ─────────────────────────────────────────────────────────

        private static void PollRequests()
        {
            try
            {
                PollUpmSearches();
                PollHttpRequests();

                if (_pendingRequests <= 0)
                {
                    EditorApplication.update -= PollRequests;
                    FinishCheck();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UniBridge] Update check polling error: {ex.Message}");
                EditorApplication.update -= PollRequests;
                FinishCheck();
            }
        }

        private static void PollUpmSearches()
        {
            for (int i = _upmSearches.Count - 1; i >= 0; i--)
            {
                var s = _upmSearches[i];
                if (!s.Request.IsCompleted) continue;

                _upmSearches.RemoveAt(i);
                _pendingRequests--;

                try
                {
                    if (s.Request.Status == StatusCode.Success && s.Request.Result != null &&
                        s.Request.Result.Length > 0)
                    {
                        var pkg = s.Request.Result[0];
                        var latest = pkg.versions.latest;
                        if (!string.IsNullOrEmpty(latest))
                            _results[s.Key] = latest;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[UniBridge] UPM search parse error for {s.Key}: {ex.Message}");
                }
            }
        }

        private static void PollHttpRequests()
        {
            for (int i = _httpRequests.Count - 1; i >= 0; i--)
            {
                var h = _httpRequests[i];
                if (!h.Request.isDone) continue;

                _httpRequests.RemoveAt(i);
                _pendingRequests--;

                try
                {
#if UNITY_2020_1_OR_NEWER
                    if (h.Request.result == UnityWebRequest.Result.Success)
#else
                    if (!h.Request.isNetworkError && !h.Request.isHttpError)
#endif
                    {
                        var body = h.Request.downloadHandler.text;
                        var version = ParseVersion(h.SourceType, body);
                        if (!string.IsNullOrEmpty(version))
                            _results[h.Key] = version;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[UniBridge] HTTP parse error for {h.Key}: {ex.Message}");
                }
                finally
                {
                    h.Request.Dispose();
                }
            }
        }

        // ── Version parsing ─────────────────────────────────────────────────

        private static string ParseVersion(string sourceType, string body)
        {
            switch (sourceType)
            {
                case "openupm":
                    return ParseOpenUpm(body);
                case "github":
                    return ParseGitHubTags(body);
                case "maven":
                    return ParseMavenMetadata(body);
                default:
                    return null;
            }
        }

        private static string ParseOpenUpm(string json)
        {
            // Look for "dist-tags":{"latest":"X.Y.Z"}
            var match = Regex.Match(json, @"""dist-tags""\s*:\s*\{[^}]*""latest""\s*:\s*""([^""]+)""");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string ParseGitHubTags(string json)
        {
            // Response is an array: [{"name":"v1.29.0",...}]
            var match = Regex.Match(json, @"""name""\s*:\s*""v?([^""]+)""");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string ParseMavenMetadata(string xml)
        {
            // Try <latest> first, then last <version> in <versions>
            var latestMatch = Regex.Match(xml, @"<latest>([^<]+)</latest>");
            if (latestMatch.Success) return latestMatch.Groups[1].Value;

            var versionMatches = Regex.Matches(xml, @"<version>([^<]+)</version>");
            if (versionMatches.Count > 0)
                return versionMatches[versionMatches.Count - 1].Groups[1].Value;

            return null;
        }

        // ── Finish & cache ──────────────────────────────────────────────────

        private static void FinishCheck()
        {
            _checking = false;

            EditorPrefs.SetString(LastCheckPrefKey, DateTime.UtcNow.ToString("o"));

            // Only write cache if we got at least one result
            if (_results != null && _results.Count > 0)
            {
                SaveCache(_results);
                _cache = null; // invalidate in-memory cache
            }

            // Clean up any lingering requests
            foreach (var h in _httpRequests)
            {
                try { h.Request?.Dispose(); } catch { }
            }
            _httpRequests.Clear();
            _upmSearches.Clear();

            OnCheckComplete?.Invoke();
        }

        private static void SaveCache(Dictionary<string, string> versions)
        {
            try
            {
                if (!Directory.Exists(CacheDir))
                    Directory.CreateDirectory(CacheDir);

                var cache = new LatestVersionCache
                {
                    lastChecked = DateTime.UtcNow.ToString("o"),
                    versions = new List<LatestVersionEntry>()
                };

                foreach (var kv in versions)
                    cache.versions.Add(new LatestVersionEntry { key = kv.Key, version = kv.Value });

                File.WriteAllText(CachePath, JsonUtility.ToJson(cache, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UniBridge] Failed to save update cache: {ex.Message}");
            }
        }

        // ── Public API ──────────────────────────────────────────────────────

        public static string GetLatestVersion(string sdkKey)
        {
            EnsureCacheLoaded();
            if (_cache?.versions == null) return null;

            foreach (var entry in _cache.versions)
            {
                if (entry.key == sdkKey)
                    return entry.version;
            }
            return null;
        }

        public static Dictionary<string, string> GetAllLatestVersions()
        {
            EnsureCacheLoaded();
            var dict = new Dictionary<string, string>();
            if (_cache?.versions == null) return dict;

            foreach (var entry in _cache.versions)
                dict[entry.key] = entry.version;
            return dict;
        }

        private static void EnsureCacheLoaded()
        {
            if (_cache != null) return;

            try
            {
                if (File.Exists(CachePath))
                {
                    var json = File.ReadAllText(CachePath);
                    _cache = JsonUtility.FromJson<LatestVersionCache>(json);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UniBridge] Failed to read update cache: {ex.Message}");
                _cache = new LatestVersionCache();
            }
        }

        // ── Cache data model ────────────────────────────────────────────────

        [Serializable]
        private class LatestVersionCache
        {
            public string lastChecked;
            public List<LatestVersionEntry> versions;
        }

        [Serializable]
        private class LatestVersionEntry
        {
            public string key;
            public string version;
        }
    }

    // ── Deserialization extension for latestCheckSource ──────────────────

    [Serializable]
    public class LatestCheckSource
    {
        public string type;     // "upm", "openupm", "github", "maven", "none"
        public string repo;     // GitHub: "owner/repo"
        public string group;    // Maven: "ru.rustore.sdk"
        public string artifact; // Maven: "pay"
        public string repoUrl;  // Maven: "https://..."
    }
}
