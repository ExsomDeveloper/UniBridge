#if UNIBRIDGERATE_RUSTORE && UNITY_ANDROID && UNIBRIDGE_STORE_RUSTORE && !UNITY_EDITOR
using System;
using RuStore.Review;
using UnityEngine;

namespace UniBridge
{
    public class RuStoreReviewSource : IRateSource
    {
        public bool IsInitialized { get; private set; }
        public bool IsSupported   => true;

        [UnityEngine.RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterAdapter()
        {
            RateSourceRegistry.Register("UNIBRIDGERATE_RUSTORE", _ => new RuStoreReviewSource(), 100);
            Debug.Log("[UniBridgeRate] RuStore review adapter registered");
        }

        public void Initialize(UniBridgeRateConfig config, Action onSuccess, Action onFailed)
        {
            RuStoreReviewManager.Instance.Init();
            IsInitialized = true;
            Debug.Log($"[{nameof(RuStoreReviewSource)}]: Инициализирован");
            onSuccess?.Invoke();
        }

        public void RequestReview(Action<bool> onComplete)
        {
#if UNIBRIDGERATE_VERBOSE_LOG
            VLog("RequestReview: starting");
#endif
            Debug.Log($"[{nameof(RuStoreReviewSource)}]: Запрашиваем ReviewFlow...");

            RuStoreReviewManager.Instance.RequestReviewFlow(
                onFailure: error =>
                {
                    Debug.LogError($"[{nameof(RuStoreReviewSource)}]: RequestReviewFlow ошибка: {error.name} — {error.description}");
                    onComplete?.Invoke(false);
                },
                onSuccess: () =>
                {
                    Debug.Log($"[{nameof(RuStoreReviewSource)}]: ReviewFlow готов, запускаем диалог...");
                    RuStoreReviewManager.Instance.LaunchReviewFlow(
                        onFailure: error =>
                        {
                            Debug.LogError($"[{nameof(RuStoreReviewSource)}]: LaunchReviewFlow ошибка: {error.name} — {error.description}");
                            onComplete?.Invoke(false);
                        },
                        onSuccess: () =>
                        {
#if UNIBRIDGERATE_VERBOSE_LOG
                            VLog("LaunchReviewFlow: completed successfully");
#endif
                            Debug.Log($"[{nameof(RuStoreReviewSource)}]: Диалог оценки завершён");
                            onComplete?.Invoke(true);
                        });
                });
        }

#if UNIBRIDGERATE_VERBOSE_LOG
        private static void VLog(string msg) => Debug.Log($"[RAT] [{nameof(RuStoreReviewSource)}] {msg}");
#endif
    }
}
#endif
