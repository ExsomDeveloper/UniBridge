using System;

namespace UniBridge
{
    public interface IAuthSource
    {
        bool IsInitialized { get; }
        bool IsSupported   { get; }
        bool IsAuthorized  { get; }

        void Initialize(UniBridgeAuthConfig config, Action onSuccess, Action onFailed);
        void Authorize(Action<bool> onComplete);
    }
}
