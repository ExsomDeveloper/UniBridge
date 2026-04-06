using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UniBridge.Editor
{
    [InitializeOnLoad]
    public static class SDKVersionChecker
    {
        private const string CheckedSessionKey = "UniBridge_SDKVersionChecked";
        private static ListRequest _listRequest;

        static SDKVersionChecker()
        {
            // Ensure config assets exist on every domain reload (safe — idempotent check)
            EditorApplication.delayCall += UniBridgeSettingsWindow.EnsureAllConfigs;

            if (SessionState.GetBool(CheckedSessionKey, false))
                return;

            SessionState.SetBool(CheckedSessionKey, true);
            EditorApplication.delayCall += CheckSDKVersions;
        }

        private static void CheckSDKVersions()
        {
            var versions = SDKInstallerWindow.LoadRequiredVersions();
            if (versions == null)
            {
                Debug.LogWarning("[UniBridge] Could not load SDKVersions.json");
                return;
            }

            _listRequest = Client.List(true);
            EditorApplication.update += OnListComplete;
        }

        private static void OnListComplete()
        {
            if (_listRequest == null || !_listRequest.IsCompleted)
                return;

            EditorApplication.update -= OnListComplete;

            if (_listRequest.Status != StatusCode.Success)
            {
                Debug.LogWarning($"[UniBridge] Failed to list packages: {_listRequest.Error?.message}");
                _listRequest = null;
                return;
            }

            var versions = SDKInstallerWindow.LoadRequiredVersions();
            if (versions == null)
            {
                _listRequest = null;
                return;
            }

            bool levelPlayInstalled        = false;
            bool playgamaInstalled         = false;
            bool yandexInstalled           = false;
            bool unityIAPInstalled         = false;
            bool gpgsInstalled             = false;
            bool ruStoreInstalled          = false;
            bool googlePlayReviewInstalled = false;
            bool rustoreReviewInstalled    = false;
            bool appMetricaInstalled       = false;

            foreach (var package in _listRequest.Result)
            {
                if (versions.levelplay != null && package.name == versions.levelplay.packageId)
                    levelPlayInstalled = true;
                else if (versions.playgama != null && !string.IsNullOrEmpty(versions.playgama.packageId) && package.name == versions.playgama.packageId)
                    playgamaInstalled = true;
                else if (versions.playgama != null && string.IsNullOrEmpty(versions.playgama.packageId) && package.name.Contains("playgama"))
                    playgamaInstalled = true;
                else if (versions.yandex != null && !string.IsNullOrEmpty(versions.yandex.packageId) && package.name == versions.yandex.packageId)
                    yandexInstalled = true;
                else if (versions.unityiap != null && !string.IsNullOrEmpty(versions.unityiap.packageId) && package.name == versions.unityiap.packageId)
                    unityIAPInstalled = true;
                else if (versions.gpgs != null && !string.IsNullOrEmpty(versions.gpgs.packageId) && package.name == versions.gpgs.packageId)
                    gpgsInstalled = true;
                else if (versions.rustore != null && !string.IsNullOrEmpty(versions.rustore.packageId) && package.name == versions.rustore.packageId)
                    ruStoreInstalled = true;
                else if (versions.googlePlayReview != null && !string.IsNullOrEmpty(versions.googlePlayReview.packageId) && package.name == versions.googlePlayReview.packageId)
                    googlePlayReviewInstalled = true;
                else if (versions.rustoreReview != null && !string.IsNullOrEmpty(versions.rustoreReview.packageId) && package.name == versions.rustoreReview.packageId)
                    rustoreReviewInstalled = true;
                else if (versions.appmetrica != null && !string.IsNullOrEmpty(versions.appmetrica.packageId) && package.name == versions.appmetrica.packageId)
                    appMetricaInstalled = true;
            }

            if (versions.levelplay != null && !string.IsNullOrEmpty(versions.levelplay.define))
            {
                if (levelPlayInstalled)
                    ScriptingDefinesManager.AddDefine(versions.levelplay.define);
                else
                    ScriptingDefinesManager.RemoveDefine(versions.levelplay.define);
            }

            if (versions.playgama != null && !string.IsNullOrEmpty(versions.playgama.define))
            {
                if (playgamaInstalled)
                    ScriptingDefinesManager.AddDefine(versions.playgama.define);
                else
                    ScriptingDefinesManager.RemoveDefine(versions.playgama.define);
            }

            if (versions.yandex != null && !string.IsNullOrEmpty(versions.yandex.define))
            {
                if (yandexInstalled)
                    ScriptingDefinesManager.AddDefine(versions.yandex.define);
                else
                    ScriptingDefinesManager.RemoveDefine(versions.yandex.define);
            }

            if (versions.unityiap != null && !string.IsNullOrEmpty(versions.unityiap.define))
            {
                if (unityIAPInstalled)
                    ScriptingDefinesManager.AddDefine(versions.unityiap.define);
                else
                    ScriptingDefinesManager.RemoveDefine(versions.unityiap.define);
            }

            if (versions.gpgs != null && !string.IsNullOrEmpty(versions.gpgs.define))
            {
                if (gpgsInstalled)
                    ScriptingDefinesManager.AddDefine(versions.gpgs.define);
                else
                    ScriptingDefinesManager.RemoveDefine(versions.gpgs.define);
            }

            if (versions.rustore != null && !string.IsNullOrEmpty(versions.rustore.define))
            {
                if (ruStoreInstalled)
                    ScriptingDefinesManager.AddDefine(versions.rustore.define);
                else
                    ScriptingDefinesManager.RemoveDefine(versions.rustore.define);
            }

            if (versions.googlePlayReview != null && !string.IsNullOrEmpty(versions.googlePlayReview.define))
            {
                if (googlePlayReviewInstalled)
                    ScriptingDefinesManager.AddDefine(versions.googlePlayReview.define);
                else
                    ScriptingDefinesManager.RemoveDefine(versions.googlePlayReview.define);
            }

            if (versions.rustoreReview != null && !string.IsNullOrEmpty(versions.rustoreReview.define))
            {
                if (rustoreReviewInstalled)
                    ScriptingDefinesManager.AddDefine(versions.rustoreReview.define);
                else
                    ScriptingDefinesManager.RemoveDefine(versions.rustoreReview.define);
            }

            if (versions.appmetrica != null && !string.IsNullOrEmpty(versions.appmetrica.define))
            {
                if (appMetricaInstalled)
                    ScriptingDefinesManager.AddDefine(versions.appmetrica.define);
                else
                    ScriptingDefinesManager.RemoveDefine(versions.appmetrica.define);
            }

            ScriptingDefinesManager.Flush();

            // On first package install (no UNIBRIDGE_STORE_* defined) — automatically select the Editor store
            if (StorePlatformDefines.GetCurrentStoreDefine() == null)
            {
                StorePlatformDefines.SetStoreDefine(StorePlatformDefines.STORE_EDITOR);
                ScriptingDefinesManager.Flush();
            }

            _listRequest = null;
        }

        [MenuItem("UniBridge/Check SDK Versions", false, 60)]
        public static void ForceCheck()
        {
            SessionState.SetBool(CheckedSessionKey, false);
            CheckSDKVersions();
        }

        public static void ResetCheck()
        {
            SessionState.SetBool(CheckedSessionKey, false);
        }
    }
}
