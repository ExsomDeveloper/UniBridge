#if UNIBRIDGE_PLAYGAMA && UNITY_WEBGL
using System;
using Playgama;
using UnityEngine;

namespace UniBridge
{
    public class PlaygamaAuthSource : IAuthSource
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            AuthSourceRegistry.Register("UNIBRIDGE_PLAYGAMA", _ => new PlaygamaAuthSource(), 100);
            Debug.Log("[UniBridgeAuth] Playgama auth adapter registered");
        }

        public bool IsInitialized { get; private set; }
        public bool IsSupported   => Bridge.player.isAuthorizationSupported;
        public bool IsAuthorized  => Bridge.player.isAuthorized;

        public void Initialize(UniBridgeAuthConfig config, Action onSuccess, Action onFailed)
        {
            IsInitialized = true;
            Debug.Log(
                $"[{nameof(PlaygamaAuthSource)}]: Инициализирован " +
                $"(isAuthorizationSupported={IsSupported}, isAuthorized={IsAuthorized})");
            onSuccess?.Invoke();
        }

        public void Authorize(Action<bool> onComplete)
        {
            PlaygamaAuthService.EnsureAuthorized(onComplete);
        }
    }
}
#endif
