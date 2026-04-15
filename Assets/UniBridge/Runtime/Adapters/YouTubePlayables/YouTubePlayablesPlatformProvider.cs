#if UNIBRIDGE_YTPLAYABLES && UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace UniBridge
{
    internal static class YouTubePlayablesPlatformProvider
    {
        [DllImport("__Internal")] private static extern int YTPlayables_InPlayablesEnv();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            if (YTPlayables_InPlayablesEnv() != 1)
            {
                Debug.Log($"[{nameof(YouTubePlayablesPlatformProvider)}] Not in Playables env, skipping provider registration");
                return;
            }
            UniBridgeEnvironment.SetProvider(new YouTubePlayablesPlatformParamsProvider(), "YouTubePlayables");
        }
    }

    internal sealed class YouTubePlayablesPlatformParamsProvider : IPlatformParamsProvider
    {
        [DllImport("__Internal")] private static extern int    YTPlayables_InPlayablesEnv();
        [DllImport("__Internal")] private static extern int    YTPlayables_IsAudioEnabled();
        [DllImport("__Internal")] private static extern IntPtr YTPlayables_GetLanguage();
        [DllImport("__Internal")] private static extern void   YTPlayables_FreeString(IntPtr ptr);
        [DllImport("__Internal")] private static extern void   YTPlayables_FetchLanguage(Action<string> callback);
        [DllImport("__Internal")] private static extern void   YTPlayables_RegisterAudioCallback(Action<int> callback);
        [DllImport("__Internal")] private static extern void   YTPlayables_RegisterPauseCallback(Action<int> callback);
        [DllImport("__Internal")] private static extern void   YTPlayables_RegisterResumeCallback(Action<int> callback);
        [DllImport("__Internal")] private static extern void   YTPlayables_FirstFrameReady();
        [DllImport("__Internal")] private static extern void   YTPlayables_GameReady();

        private static YouTubePlayablesPlatformParamsProvider _instance;

        private bool _isPaused;

        public string GetPlatformId() => YTPlayables_InPlayablesEnv() == 1 ? "youtube" : "";

        /// <summary>
        /// Returns the full BCP-47 language tag reported by YouTube (e.g. "en-US", "es-419", "zh-Hans").
        /// Returns "en" fallback until the async YTPlayables_FetchLanguage resolves on first frames.
        /// </summary>
        public string GetLanguage()
        {
            var ptr = YTPlayables_GetLanguage();
            try
            {
                var lang = Marshal.PtrToStringUTF8(ptr);
                return string.IsNullOrEmpty(lang) ? "en" : lang;
            }
            finally
            {
                if (ptr != IntPtr.Zero) YTPlayables_FreeString(ptr);
            }
        }

        public bool IsAudioEnabled => YTPlayables_IsAudioEnabled() == 1;
        public bool IsPaused       => _isPaused;
        public bool IsVisible      => !_isPaused; // YouTube uses pause/resume instead of visibility

        public event Action<bool> AudioStateChanged;
        public event Action<bool> PauseStateChanged;
        public event Action<bool> VisibilityStateChanged;

        public YouTubePlayablesPlatformParamsProvider()
        {
            _instance = this;
            YTPlayables_RegisterAudioCallback(OnAudioStateChanged);
            YTPlayables_RegisterPauseCallback(OnPauseCallback);
            YTPlayables_RegisterResumeCallback(OnResumeCallback);
            YTPlayables_FetchLanguage(OnLanguageFetched);
        }

        public void SendMessage(PlatformMessage message)
        {
            switch (message)
            {
                case PlatformMessage.FirstFrameReady:
                    YTPlayables_FirstFrameReady();
                    break;
                case PlatformMessage.GameReady:
                    YTPlayables_GameReady();
                    break;
                // YouTube Playables does not have equivalents for other messages
            }
        }

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnAudioStateChanged(int isEnabled)
        {
            _instance?.AudioStateChanged?.Invoke(isEnabled == 1);
        }

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnPauseCallback(int _)
        {
            if (_instance == null) return;
            _instance._isPaused = true;
            _instance.PauseStateChanged?.Invoke(true);
            _instance.VisibilityStateChanged?.Invoke(false);
        }

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnResumeCallback(int _)
        {
            if (_instance == null) return;
            _instance._isPaused = false;
            _instance.PauseStateChanged?.Invoke(false);
            _instance.VisibilityStateChanged?.Invoke(true);
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void OnLanguageFetched(string lang)
        {
            // Language is cached on the JS side; nothing to do here.
        }
    }
}
#endif
