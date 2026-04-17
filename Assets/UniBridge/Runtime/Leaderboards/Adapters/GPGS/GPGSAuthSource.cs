#if UNIBRIDGELEADERBOARDS_GPGS && UNITY_ANDROID && UNIBRIDGE_STORE_GOOGLEPLAY && !UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace UniBridge
{
    public class GPGSAuthSource : IAuthSource
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Register()
        {
            AuthSourceRegistry.Register("UNIBRIDGELEADERBOARDS_GPGS", _ => new GPGSAuthSource(), 90);
            Debug.Log("[UniBridgeAuth] GPGS auth adapter registered");
        }

        public bool IsInitialized { get; private set; }
        public bool IsSupported   => true;
        public bool IsAuthorized  => Social.localUser.authenticated;

        public void Initialize(UniBridgeAuthConfig config, Action onSuccess, Action onFailed)
        {
            IsInitialized = true;
            Debug.Log(
                $"[{nameof(GPGSAuthSource)}]: Инициализирован " +
                $"(authenticated={IsAuthorized})");
            onSuccess?.Invoke();
        }

        public void Authorize(Action<bool> onComplete)
        {
            if (Social.localUser.authenticated)
            {
                onComplete?.Invoke(true);
                return;
            }

            Social.localUser.Authenticate(success =>
            {
                if (!success)
                    Debug.LogWarning($"[{nameof(GPGSAuthSource)}]: Авторизация GPGS отклонена");
                onComplete?.Invoke(success);
            });
        }
    }
}
#endif
