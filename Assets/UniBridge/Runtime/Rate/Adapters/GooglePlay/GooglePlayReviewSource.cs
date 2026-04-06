#if UNIBRIDGERATE_GOOGLEPLAY && UNITY_ANDROID && UNIBRIDGE_STORE_GOOGLEPLAY && !UNITY_EDITOR
using System;
using System.Collections;
using Google.Play.Review;
using UnityEngine;

namespace UniBridge
{
    public class GooglePlayReviewSource : IRateSource
    {
        public bool IsInitialized { get; private set; }
        public bool IsSupported   => true;

        private ReviewManager _reviewManager;

        [UnityEngine.RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterAdapter()
        {
            RateSourceRegistry.Register("UNIBRIDGERATE_GOOGLEPLAY", _ => new GooglePlayReviewSource(), 100);
            Debug.Log("[UniBridgeRate] Google Play review adapter registered");
        }

        public void Initialize(UniBridgeRateConfig config, Action onSuccess, Action onFailed)
        {
            _reviewManager = new ReviewManager();
            IsInitialized  = true;
            Debug.Log($"[{nameof(GooglePlayReviewSource)}]: Инициализирован");
            onSuccess?.Invoke();
        }

        public void RequestReview(Action<bool> onComplete)
        {
            if (_reviewManager == null)
            {
                Debug.LogError($"[{nameof(GooglePlayReviewSource)}]: ReviewManager не создан");
                onComplete?.Invoke(false);
                return;
            }
#if UNIBRIDGERATE_VERBOSE_LOG
            VLog("RequestReview: starting flow");
#endif
            // Run coroutine via a temporary MonoBehaviour
            var runner = new GameObject("[UniBridgeRate] ReviewRunner").AddComponent<CoroutineRunner>();
            runner.Run(DoReviewFlow(runner, onComplete));
        }

        private IEnumerator DoReviewFlow(CoroutineRunner runner, Action<bool> onComplete)
        {
            // Step 1: request ReviewInfo
#if UNIBRIDGERATE_VERBOSE_LOG
            VLog("DoReviewFlow: RequestReviewFlow...");
#endif
            var requestOp = _reviewManager.RequestReviewFlow();
            yield return requestOp;

            if (requestOp.Error != ReviewErrorCode.NoError)
            {
                Debug.LogError($"[{nameof(GooglePlayReviewSource)}]: RequestReviewFlow ошибка: {requestOp.Error}");
                UnityEngine.Object.Destroy(runner.gameObject);
                onComplete?.Invoke(false);
                yield break;
            }

            // Step 2: launch the review dialog
#if UNIBRIDGERATE_VERBOSE_LOG
            VLog("DoReviewFlow: LaunchReviewFlow...");
#endif
            var launchOp = _reviewManager.LaunchReviewFlow(requestOp.GetResult());
            yield return launchOp;

            UnityEngine.Object.Destroy(runner.gameObject);

            if (launchOp.Error != ReviewErrorCode.NoError)
            {
                Debug.LogError($"[{nameof(GooglePlayReviewSource)}]: LaunchReviewFlow ошибка: {launchOp.Error}");
                onComplete?.Invoke(false);
                yield break;
            }
#if UNIBRIDGERATE_VERBOSE_LOG
            VLog("DoReviewFlow: completed successfully");
#endif
            Debug.Log($"[{nameof(GooglePlayReviewSource)}]: Диалог оценки завершён");
            onComplete?.Invoke(true);
        }

        // Helper MonoBehaviour for running coroutines
        private class CoroutineRunner : MonoBehaviour
        {
            public void Run(IEnumerator routine) => StartCoroutine(routine);
        }

#if UNIBRIDGERATE_VERBOSE_LOG
        private static void VLog(string msg) => Debug.Log($"[RAT] [{nameof(GooglePlayReviewSource)}] {msg}");
#endif
    }
}
#endif
