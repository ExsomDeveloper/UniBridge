#if UNIBRIDGE_PLAYGAMA && UNITY_WEBGL
using System;
using System.Collections.Generic;
using Playgama;
using UnityEngine;

namespace UniBridge
{
    internal static class PlaygamaAuthService
    {
        private static bool _isAuthInProgress;
        private static readonly List<Action<bool>> _pending = new List<Action<bool>>();

        /// <summary>
        /// Ensures the player is authorized. If already authorized, calls onComplete(true) immediately.
        /// Multiple concurrent calls show ONE authorization dialog;
        /// all callbacks are invoked after it completes.
        /// </summary>
        internal static void EnsureAuthorized(Action<bool> onComplete)
        {
            if (Bridge.player.isAuthorized)
            {
                onComplete?.Invoke(true);
                return;
            }

            _pending.Add(onComplete);

            if (_isAuthInProgress)
                return;

            _isAuthInProgress = true;
            Bridge.player.Authorize(null, success =>
            {
                _isAuthInProgress = false;
                var snapshot = new List<Action<bool>>(_pending);
                _pending.Clear();
                if (!success)
                    Debug.LogWarning("[PlaygamaAuthService]: Авторизация отклонена пользователем или завершилась с ошибкой");
                foreach (var cb in snapshot)
                    cb?.Invoke(success);
            });
        }
    }
}
#endif
