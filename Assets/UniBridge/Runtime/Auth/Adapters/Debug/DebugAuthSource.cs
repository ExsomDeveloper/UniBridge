using System;
using UnityEngine;

namespace UniBridge
{
    public class DebugAuthSource : IAuthSource
    {
        public bool IsInitialized { get; private set; }
        public bool IsSupported   => true;
        public bool IsAuthorized  => true;

        public void Initialize(UniBridgeAuthConfig config, Action onSuccess, Action onFailed)
        {
            IsInitialized = true;
            Debug.Log($"[{nameof(DebugAuthSource)}]: Инициализирован");
            onSuccess?.Invoke();
        }

        public void Authorize(Action<bool> onComplete)
        {
            Debug.Log($"[{nameof(DebugAuthSource)}]: Authorize — авторизация (Editor mock)");
            onComplete?.Invoke(true);
        }
    }
}
