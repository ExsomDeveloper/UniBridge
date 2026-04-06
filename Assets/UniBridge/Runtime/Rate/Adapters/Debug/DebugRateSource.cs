using System;
using UnityEngine;

namespace UniBridge
{
    public class DebugRateSource : IRateSource
    {
        public bool IsInitialized { get; private set; }
        public bool IsSupported   => true;

        public void Initialize(UniBridgeRateConfig config, Action onSuccess, Action onFailed)
        {
            IsInitialized = true;
            Debug.Log($"[{nameof(DebugRateSource)}]: Инициализирован");
            onSuccess?.Invoke();
        }

        public void RequestReview(Action<bool> onComplete)
        {
            Debug.Log($"[{nameof(DebugRateSource)}]: RequestReview — запрос оценки (Editor mock)");
            onComplete?.Invoke(true);
        }
    }
}
