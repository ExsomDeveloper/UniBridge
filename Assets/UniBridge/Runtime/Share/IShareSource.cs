using System;

namespace UniBridge
{
    public interface IShareSource
    {
        bool IsInitialized { get; }
        bool IsSupported   { get; }

        void Initialize(UniBridgeShareConfig config, Action onSuccess, Action onFailed);
        void Share(ShareData data, Action<ShareSheetResult, ShareError> onComplete);
    }
}
