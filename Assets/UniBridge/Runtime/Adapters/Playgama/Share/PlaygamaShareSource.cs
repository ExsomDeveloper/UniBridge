#if UNIBRIDGE_PLAYGAMA && UNITY_WEBGL
using System;
using System.Collections.Generic;
using Playgama;
using UnityEngine;

namespace UniBridge
{
    public class PlaygamaShareSource : IShareSource
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void RegisterAdapter()
        {
            ShareSourceRegistry.Register(
                "UNIBRIDGE_PLAYGAMA",
                _ => new PlaygamaShareSource(),
                100);
            Debug.Log("[UniBridgeShare] Playgama share adapter registered");
        }

        public bool IsInitialized { get; private set; }
        public bool IsSupported   => Bridge.social.isShareSupported;

        public void Initialize(UniBridgeShareConfig config, Action onSuccess, Action onFailed)
        {
            IsInitialized = true;

            if (!Bridge.social.isShareSupported)
                Debug.Log($"[{nameof(PlaygamaShareSource)}]: Шаринг не поддерживается на этой Playgama-платформе");
            else
                Debug.Log($"[{nameof(PlaygamaShareSource)}]: Инициализирован");

            onSuccess?.Invoke();
        }

        public void Share(ShareData data, Action<ShareSheetResult, ShareError> onComplete)
        {
#if UNIBRIDGESHARE_VERBOSE_LOG
            VLog($"Share: hasText={!string.IsNullOrEmpty(data.Text)} hasImageUrl={!string.IsNullOrEmpty(data.ImageUrl)}");
#endif
            if (!IsSupported)
            {
                Debug.Log($"[{nameof(PlaygamaShareSource)}]: Шаринг не поддерживается");
                onComplete?.Invoke(new ShareSheetResult(ShareResultCode.Unknown), null);
                return;
            }

            var options = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(data.Text))     options["text"]     = data.Text;
            if (!string.IsNullOrEmpty(data.ImageUrl)) options["imageUrl"] = data.ImageUrl;

            Bridge.social.Share(options, success =>
            {
#if UNIBRIDGESHARE_VERBOSE_LOG
                VLog($"Share result: success={success}");
#endif
                if (!success)
                    Debug.LogWarning($"[{nameof(PlaygamaShareSource)}]: Шаринг не удался");

                var resultCode = success ? ShareResultCode.Completed : ShareResultCode.Unknown;
                onComplete?.Invoke(new ShareSheetResult(resultCode), null);
            });
        }

#if UNIBRIDGESHARE_VERBOSE_LOG
        private static void VLog(string msg) => Debug.Log($"[RAT] [{nameof(PlaygamaShareSource)}] {msg}");
#endif
    }
}
#endif
