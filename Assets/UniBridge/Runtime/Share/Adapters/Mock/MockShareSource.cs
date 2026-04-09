using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace UniBridge
{
    /// <summary>
    /// Stub for platforms without native sharing.
    /// IsSupported = false — allows hiding the share button in the UI.
    /// </summary>
    [Preserve]
    public class MockShareSource : IShareSource
    {
        public bool IsInitialized { get; private set; }
        public bool IsSupported   => false;

        public void Initialize(UniBridgeShareConfig config, Action onSuccess, Action onFailed)
        {
            IsInitialized = true;
            Debug.Log($"[{nameof(MockShareSource)}]: Инициализирован (шаринг не поддерживается на этой платформе)");
            onSuccess?.Invoke();
        }

        public void Share(ShareData data, Action<ShareSheetResult, ShareError> onComplete)
        {
            Debug.Log($"[{nameof(MockShareSource)}]: Share — платформа не поддерживает шаринг, вызов игнорируется");
            onComplete?.Invoke(new ShareSheetResult(ShareResultCode.Unknown), null);
        }
    }
}
