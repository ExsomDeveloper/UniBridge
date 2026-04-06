using System;
using UnityEngine;

namespace UniBridge
{
    /// <summary>
    /// Stub for platforms without native authorization (e.g. RuStore).
    /// IsSupported = false — allows hiding UI elements that require authorization.
    /// </summary>
    public class MockAuthSource : IAuthSource
    {
        public bool IsInitialized { get; private set; }
        public bool IsSupported   => false;
        public bool IsAuthorized  => false;

        public void Initialize(UniBridgeAuthConfig config, Action onSuccess, Action onFailed)
        {
            IsInitialized = true;
            Debug.Log($"[{nameof(MockAuthSource)}]: Инициализирован (авторизация не поддерживается на этой платформе)");
            onSuccess?.Invoke();
        }

        public void Authorize(Action<bool> onComplete)
        {
            Debug.Log($"[{nameof(MockAuthSource)}]: Authorize — платформа не поддерживает авторизацию, вызов игнорируется");
            onComplete?.Invoke(false);
        }
    }
}
