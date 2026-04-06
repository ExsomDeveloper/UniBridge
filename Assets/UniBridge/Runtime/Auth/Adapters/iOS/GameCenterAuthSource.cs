#if UNITY_IOS && UNIBRIDGE_STORE_APPSTORE && !UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.SocialPlatforms.GameCenter;

namespace UniBridge
{
    public class GameCenterAuthSource : IAuthSource
    {
        public bool IsInitialized { get; private set; }
        public bool IsSupported   => true;
        public bool IsAuthorized  => Social.localUser.authenticated;

        public void Initialize(UniBridgeAuthConfig config, Action onSuccess, Action onFailed)
        {
            GameCenterPlatform.ShowDefaultAchievementCompletionBanner(true);
            Social.localUser.Authenticate(success =>
            {
                IsInitialized = true;
                if (success)
                    Debug.Log($"[{nameof(GameCenterAuthSource)}]: Аутентификация Game Center успешна");
                else
                    Debug.LogWarning(
                        $"[{nameof(GameCenterAuthSource)}]: " +
                        "Аутентификация Game Center не удалась — вызовы Authorize() будут неуспешными");

                // Always call onSuccess — UniBridgeAuth initializes,
                // individual Authorize() calls will fail when !IsAuthorized
                onSuccess?.Invoke();
            });
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
                    Debug.LogWarning($"[{nameof(GameCenterAuthSource)}]: Авторизация Game Center отклонена");
                onComplete?.Invoke(success);
            });
        }
    }
}
#endif
