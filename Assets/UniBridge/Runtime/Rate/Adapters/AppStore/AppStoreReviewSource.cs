#if UNITY_IOS && UNIBRIDGE_STORE_APPSTORE && !UNITY_EDITOR
using System;
using UnityEngine;

namespace UniBridge
{
    public class AppStoreReviewSource : IRateSource
    {
        public bool IsInitialized { get; private set; }
        public bool IsSupported   => true;

        [UnityEngine.RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterAdapter()
        {
            RateSourceRegistry.Register("UNITY_IOS_STOREREVIEW", _ => new AppStoreReviewSource(), 90);
            Debug.Log("[UniBridgeRate] AppStore review adapter registered");
        }

        public void Initialize(UniBridgeRateConfig config, Action onSuccess, Action onFailed)
        {
            IsInitialized = true;
            Debug.Log($"[{nameof(AppStoreReviewSource)}]: Инициализирован");
            onSuccess?.Invoke();
        }

        public void RequestReview(Action<bool> onComplete)
        {
#if UNIBRIDGERATE_VERBOSE_LOG
            VLog("RequestReview: calling RequestStoreReview");
#endif
            Debug.Log($"[{nameof(AppStoreReviewSource)}]: Запрос оценки App Store");
            bool presented = UnityEngine.iOS.Device.RequestStoreReview();
#if UNIBRIDGERATE_VERBOSE_LOG
            VLog($"RequestReview result: presented={presented}");
#endif
            Debug.Log($"[{nameof(AppStoreReviewSource)}]: RequestStoreReview вернул {presented}");
            onComplete?.Invoke(presented);
        }

#if UNIBRIDGERATE_VERBOSE_LOG
        private static void VLog(string msg) => Debug.Log($"[RAT] [{nameof(AppStoreReviewSource)}] {msg}");
#endif
    }
}
#endif
