using System;

namespace UniBridge
{
    public interface IRateSource
    {
        bool IsInitialized { get; }
        bool IsSupported   { get; }

        void Initialize(UniBridgeRateConfig config, Action onSuccess, Action onFailed);
        void RequestReview(Action<bool> onComplete);
    }
}
