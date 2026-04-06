using UnityEngine;

namespace UniBridge
{
    public class AuthSourceBuilder
    {
        public IAuthSource Build(UniBridgeAuthConfig config)
        {
#if UNITY_EDITOR
            return new DebugAuthSource();
#else
            if (config != null && config.PreferredAuthAdapter == "UNIBRIDGEAUTH_MOCK")
            {
                Debug.Log($"[{nameof(AuthSourceBuilder)}]: Принудительно выбрана MockAuthSource.");
                return new MockAuthSource();
            }

#if UNITY_IOS && UNIBRIDGE_STORE_APPSTORE
            if (config != null && config.PreferredAuthAdapter == "UNITY_IOS_GAMECENTER")
            {
                Debug.Log($"[{nameof(AuthSourceBuilder)}]: Принудительно выбрана GameCenterAuthSource.");
                return new GameCenterAuthSource();
            }
#endif

            var source = AuthSourceRegistry.Create(config);
            if (source != null)
                return source;

            Debug.LogWarning(
                $"[{nameof(AuthSourceBuilder)}]: " +
                "Нет подходящего адаптера авторизации. Используется MockAuthSource.");

            return new MockAuthSource();
#endif
        }
    }
}
