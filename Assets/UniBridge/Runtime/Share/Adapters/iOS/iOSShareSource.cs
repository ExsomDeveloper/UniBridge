#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.IO;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace UniBridge
{
    public class iOSShareSource : IShareSource
    {
        // ── Native callback ──────────────────────────────────────────────────────────────────────────────

        // resultCode: 0 = Completed, 1 = Cancelled
        private delegate void UniBridgeShareNativeCallback(int resultCode);

        private static Action<ShareSheetResult, ShareError> _pendingCallback;

        // Static delegate — required for MonoPInvokeCallback (IL2CPP does not support lambdas)
        private static readonly UniBridgeShareNativeCallback _nativeCallback = OnNativeResult;

        [MonoPInvokeCallback(typeof(UniBridgeShareNativeCallback))]
        private static void OnNativeResult(int resultCode)
        {
            var cb = _pendingCallback;
            _pendingCallback = null;

            var code = resultCode == 0 ? ShareResultCode.Completed : ShareResultCode.Cancelled;
#if UNIBRIDGESHARE_VERBOSE_LOG
            VLog($"OnNativeResult: resultCode={resultCode} ({code})");
#endif
            cb?.Invoke(new ShareSheetResult(code), null);
        }

        // ── DllImport ─────────────────────────────────────────────────────────

        [DllImport("__Internal")]
        private static extern void _UniBridgeShare_Text(string text, UniBridgeShareNativeCallback callback);

        [DllImport("__Internal")]
        private static extern void _UniBridgeShare_Image(string imagePath, UniBridgeShareNativeCallback callback);

        [DllImport("__Internal")]
        private static extern void _UniBridgeShare_TextAndImage(string text, string imagePath, UniBridgeShareNativeCallback callback);

        // ── IShareSource ──────────────────────────────────────────────────────

        public bool IsInitialized { get; private set; }
        public bool IsSupported   => true;

        public void Initialize(UniBridgeShareConfig config, Action onSuccess, Action onFailed)
        {
            IsInitialized = true;
            Debug.Log($"[{nameof(iOSShareSource)}]: Инициализирован");
            onSuccess?.Invoke();
        }

        public void Share(ShareData data, Action<ShareSheetResult, ShareError> onComplete)
        {
#if UNIBRIDGESHARE_VERBOSE_LOG
            VLog($"Share: hasText={!string.IsNullOrEmpty(data.Text)} hasImage={data.Image != null}");
#endif
            try
            {
                if (_pendingCallback != null)
                {
                    Debug.LogWarning($"[{nameof(iOSShareSource)}]: Шаринг уже выполняется");
                    onComplete?.Invoke(null, new ShareError("Шаринг уже выполняется"));
                    return;
                }

                _pendingCallback = onComplete;

                bool hasText  = !string.IsNullOrEmpty(data.Text);
                bool hasImage = data.Image != null;

                if (hasText && hasImage)
                {
                    var path = SaveImageToFile(data.Image);
#if UNIBRIDGESHARE_VERBOSE_LOG
                    VLog($"Share: TextAndImage path='{path}'");
#endif
                    _UniBridgeShare_TextAndImage(data.Text, path, _nativeCallback);
                }
                else if (hasImage)
                {
                    var path = SaveImageToFile(data.Image);
#if UNIBRIDGESHARE_VERBOSE_LOG
                    VLog($"Share: Image path='{path}'");
#endif
                    _UniBridgeShare_Image(path, _nativeCallback);
                }
                else
                {
#if UNIBRIDGESHARE_VERBOSE_LOG
                    VLog("Share: TextOnly");
#endif
                    _UniBridgeShare_Text(data.Text ?? "", _nativeCallback);
                }
            }
            catch (Exception e)
            {
                _pendingCallback = null;
                Debug.LogError($"[{nameof(iOSShareSource)}]: Ошибка шаринга: {e.Message}");
                onComplete?.Invoke(null, new ShareError(e.Message));
            }
        }

        private static string SaveImageToFile(Texture2D image)
        {
            var bytes    = image.EncodeToPNG();
            var filePath = Path.Combine(Application.temporaryCachePath, "unibridgeshare_image.png");
            File.WriteAllBytes(filePath, bytes);
            return filePath;
        }

#if UNIBRIDGESHARE_VERBOSE_LOG
        private static void VLog(string msg) => Debug.Log($"[RAT] [{nameof(iOSShareSource)}] {msg}");
#endif
    }
}
#endif
