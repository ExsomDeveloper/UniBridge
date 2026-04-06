#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace UniBridge
{
    public class AndroidShareSource : IShareSource
    {
        public bool IsInitialized { get; private set; }
        public bool IsSupported   => true;

        public void Initialize(UniBridgeShareConfig config, Action onSuccess, Action onFailed)
        {
            IsInitialized = true;
            Debug.Log($"[{nameof(AndroidShareSource)}]: Инициализирован");
            onSuccess?.Invoke();
        }

        public void Share(ShareData data, Action<ShareSheetResult, ShareError> onComplete)
        {
#if UNIBRIDGESHARE_VERBOSE_LOG
            VLog($"Share: hasText={!string.IsNullOrEmpty(data.Text)} hasImage={data.Image != null}");
#endif
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity    = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                if (data.Image != null)
                    ShareWithImage(activity, data);
                else
                    ShareTextOnly(activity, data.Text ?? "");

                // Android has no API to detect whether the user selected an app or dismissed
                // the chooser after startActivity — result is always Unknown.
#if UNIBRIDGESHARE_VERBOSE_LOG
                VLog("Share: startActivity fired (result=Unknown)");
#endif
                onComplete?.Invoke(new ShareSheetResult(ShareResultCode.Unknown), null);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(AndroidShareSource)}]: Ошибка шаринга: {e.Message}");
                onComplete?.Invoke(null, new ShareError(e.Message));
            }
        }

        private static void ShareTextOnly(AndroidJavaObject activity, string text)
        {
            using var plugin = new AndroidJavaClass("com.unibridge.share.UniBridgeSharePlugin");
            plugin.CallStatic("shareText", activity, text);
        }

        private static void ShareWithImage(AndroidJavaObject activity, ShareData data)
        {
            var bytes    = data.Image.EncodeToPNG();
            var filePath = Path.Combine(Application.temporaryCachePath, "unibridgeshare_image.png");
            File.WriteAllBytes(filePath, bytes);
#if UNIBRIDGESHARE_VERBOSE_LOG
            VLog($"ShareWithImage: filePath='{filePath}' hasText={!string.IsNullOrEmpty(data.Text)}");
#endif
            using var plugin = new AndroidJavaClass("com.unibridge.share.UniBridgeSharePlugin");
            plugin.CallStatic("shareImage", activity, filePath, data.Text ?? "");
        }

#if UNIBRIDGESHARE_VERBOSE_LOG
        private static void VLog(string msg) => Debug.Log($"[RAT] [{nameof(AndroidShareSource)}] {msg}");
#endif
    }
}
#endif
