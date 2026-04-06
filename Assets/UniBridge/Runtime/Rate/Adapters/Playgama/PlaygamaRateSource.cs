#if UNIBRIDGE_PLAYGAMA && UNITY_WEBGL
using System;
using Playgama;
using UnityEngine;

namespace UniBridge
{
    public class PlaygamaRateSource : IRateSource
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterAdapter()
        {
            RateSourceRegistry.Register("UNIBRIDGE_PLAYGAMA", _ => new PlaygamaRateSource(), 100);
            Debug.Log("[UniBridgeRate] Playgama rate adapter registered");
        }

        public bool IsInitialized { get; private set; }
        public bool IsSupported   => Bridge.social.isRateSupported;

        public void Initialize(UniBridgeRateConfig config, Action onSuccess, Action onFailed)
        {
            IsInitialized = true;
            Debug.Log($"[{nameof(PlaygamaRateSource)}]: Initialized, isRateSupported={IsSupported}");
            onSuccess?.Invoke();
        }

        public void RequestReview(Action<bool> onComplete)
        {
            if (!IsSupported)
            {
                onComplete?.Invoke(false);
                return;
            }

            Bridge.social.Rate(success =>
            {
                if (!success)
                    Debug.LogWarning($"[{nameof(PlaygamaRateSource)}]: Rate call returned false");
                onComplete?.Invoke(success);
            });
        }
    }
}
#endif
