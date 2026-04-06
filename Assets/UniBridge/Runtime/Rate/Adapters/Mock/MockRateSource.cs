using System;
using UnityEngine;

namespace UniBridge
{
    /// <summary>
    /// Stub for platforms without a native rating request (e.g. Playgama).
    /// IsSupported = false — allows hiding the "Rate" button in the UI.
    /// </summary>
    public class MockRateSource : IRateSource
    {
        public bool IsInitialized { get; private set; }
        public bool IsSupported   => false;

        public void Initialize(UniBridgeRateConfig config, Action onSuccess, Action onFailed)
        {
            IsInitialized = true;
            Debug.Log($"[{nameof(MockRateSource)}]: Инициализирован (оценка не поддерживается на этой платформе)");
            onSuccess?.Invoke();
        }

        public void RequestReview(Action<bool> onComplete)
        {
            Debug.Log($"[{nameof(MockRateSource)}]: RequestReview — платформа не поддерживает оценку, вызов игнорируется");
            onComplete?.Invoke(true);
        }
    }
}
