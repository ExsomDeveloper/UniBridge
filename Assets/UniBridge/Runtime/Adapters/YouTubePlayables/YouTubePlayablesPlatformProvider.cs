#if UNIBRIDGE_YTPLAYABLES && UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace UniBridge
{
    internal static class YouTubePlayablesPlatformProvider
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            VerboseLog.Log("YT:Provider", $"Register() enter — RuntimeInitializeOnLoad:SubsystemRegistration | frame={Time.frameCount} | realtime={Time.realtimeSinceStartup:F3}s");
            // Register unconditionally: ytgame.IN_PLAYABLES_ENV may not yet be populated at SubsystemRegistration time.
            // jslib functions guard on `typeof ytgame !== 'undefined'` internally — safe no-op outside YouTube.
            UniBridgeEnvironment.SetProvider(new YouTubePlayablesPlatformParamsProvider(), "YouTubePlayables");
            VerboseLog.Log("YT:Provider", "Register() done");
        }
    }

    internal sealed class YouTubePlayablesPlatformParamsProvider : IPlatformParamsProvider
    {
        [DllImport("__Internal")] private static extern int    YTPlayables_InPlayablesEnv();
        [DllImport("__Internal")] private static extern int    YTPlayables_IsAudioEnabled();
        [DllImport("__Internal")] private static extern IntPtr YTPlayables_GetLanguage();
        [DllImport("__Internal")] private static extern void   YTPlayables_FreeString(IntPtr ptr);
        [DllImport("__Internal")] private static extern void   YTPlayables_FetchLanguage(Action<string> callback);
        [DllImport("__Internal")] private static extern void   YTPlayables_RegisterAudioCallback(Action<int> callback, Action<int> statusCb);
        [DllImport("__Internal")] private static extern void   YTPlayables_RegisterPauseCallback(Action<int> callback, Action<int> statusCb);
        [DllImport("__Internal")] private static extern void   YTPlayables_RegisterResumeCallback(Action<int> callback, Action<int> statusCb);
        [DllImport("__Internal")] private static extern void   YTPlayables_FirstFrameReady(Action<int> statusCb);
        [DllImport("__Internal")] private static extern void   YTPlayables_GameReady(Action<int> statusCb);

        private static YouTubePlayablesPlatformParamsProvider _instance;

        private bool _isPaused;

        private static bool _platformIdLogged;
        public string GetPlatformId()
        {
            var inEnv = YTPlayables_InPlayablesEnv() == 1;
            if (!_platformIdLogged)
            {
                _platformIdLogged = true;
                VerboseLog.Log("YT:Provider", $"GetPlatformId first-call: inPlayablesEnv={inEnv} → \"{(inEnv ? "youtube" : "")}\"");
            }
            return inEnv ? "youtube" : "";
        }

        private static bool _getLanguageLogged;

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
                var result = string.IsNullOrEmpty(lang) ? "en" : lang;
                if (!_getLanguageLogged)
                {
                    _getLanguageLogged = true;
                    VerboseLog.Log("YT:Provider", $"GetLanguage first-read → \"{result}\"");
                }
                return result;
            }
            finally
            {
                if (ptr != IntPtr.Zero) YTPlayables_FreeString(ptr);
            }
        }

        private static bool _audioLogged, _pausedLogged, _visibleLogged;
        public bool IsAudioEnabled
        {
            get
            {
                var v = YTPlayables_IsAudioEnabled() == 1;
                if (!_audioLogged) { _audioLogged = true; VerboseLog.Log("YT:Provider", $"IsAudioEnabled first-check → {v}"); }
                return v;
            }
        }
        public bool IsPaused
        {
            get
            {
                if (!_pausedLogged) { _pausedLogged = true; VerboseLog.Log("YT:Provider", $"IsPaused first-check → {_isPaused}"); }
                return _isPaused;
            }
        }
        public bool IsVisible
        {
            get
            {
                var v = !_isPaused;
                if (!_visibleLogged) { _visibleLogged = true; VerboseLog.Log("YT:Provider", $"IsVisible first-check → {v} (pause-based)"); }
                return v;
            }
        }

        public event Action<bool> AudioStateChanged;
        public event Action<bool> PauseStateChanged;
        public event Action<bool> VisibilityStateChanged;

        public YouTubePlayablesPlatformParamsProvider()
        {
            _instance = this;
            VerboseLog.Log("YT:Provider", $"ctor begin | frame={Time.frameCount} | realtime={Time.realtimeSinceStartup:F3}s");

            VerboseLog.Log("YT:Provider", "RegisterAudioCallback dispatching…");
            YTPlayables_RegisterAudioCallback(OnAudioStateChanged, OnRegisterAudioStatus);

            VerboseLog.Log("YT:Provider", "RegisterPauseCallback dispatching…");
            YTPlayables_RegisterPauseCallback(OnPauseCallback, OnRegisterPauseStatus);

            VerboseLog.Log("YT:Provider", "RegisterResumeCallback dispatching…");
            YTPlayables_RegisterResumeCallback(OnResumeCallback, OnRegisterResumeStatus);

            VerboseLog.Log("YT:Provider", "FetchLanguage dispatching…");
            YTPlayables_FetchLanguage(OnLanguageFetched);

            VerboseLog.Log("YT:Provider", "ctor done");
        }

        public void SendMessage(PlatformMessage message)
        {
            VerboseLog.Log("YT:Provider", $"SendMessage({message}) — entering native dispatch | frame={Time.frameCount}");
            switch (message)
            {
                case PlatformMessage.FirstFrameReady:
                    VerboseLog.Log("YT:Provider", "→ YTPlayables_FirstFrameReady()");
                    YTPlayables_FirstFrameReady(OnFirstFrameReadyStatus);
                    break;
                case PlatformMessage.GameReady:
                    VerboseLog.Log("YT:Provider", "→ YTPlayables_GameReady()");
                    YTPlayables_GameReady(OnGameReadyStatus);
                    break;
                default:
                    VerboseLog.Log("YT:Provider", $"SendMessage({message}) — no YT equivalent, ignored");
                    break;
            }
        }

        private static string DecodeStatus(int code) => code switch
        {
            0 => "FAIL: ytgame undefined",
            1 => "FAIL: required submodule missing",
            2 => "OK",
            3 => "FAIL: native threw (see browser console)",
            _ => $"unknown({code})"
        };

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnFirstFrameReadyStatus(int code) => VerboseLog.Log("YT:Provider", $"firstFrameReady → {DecodeStatus(code)}");

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnGameReadyStatus(int code) => VerboseLog.Log("YT:Provider", $"gameReady → {DecodeStatus(code)}");

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnRegisterAudioStatus(int code) => VerboseLog.Log("YT:Provider", $"RegisterAudio → {DecodeStatus(code)}");

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnRegisterPauseStatus(int code) => VerboseLog.Log("YT:Provider", $"RegisterPause → {DecodeStatus(code)}");

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnRegisterResumeStatus(int code) => VerboseLog.Log("YT:Provider", $"RegisterResume → {DecodeStatus(code)}");

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnAudioStateChanged(int isEnabled)
        {
            VerboseLog.Log("YT:Provider", $"← onAudioEnabledChange callback: enabled={isEnabled == 1}");
            _instance?.AudioStateChanged?.Invoke(isEnabled == 1);
        }

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnPauseCallback(int _)
        {
            VerboseLog.Log("YT:Provider", "← onPause callback");
            if (_instance == null) { VerboseLog.Warn("YT:Provider", "onPause: _instance is null"); return; }
            _instance._isPaused = true;
            _instance.PauseStateChanged?.Invoke(true);
            _instance.VisibilityStateChanged?.Invoke(false);
        }

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnResumeCallback(int _)
        {
            VerboseLog.Log("YT:Provider", "← onResume callback");
            if (_instance == null) { VerboseLog.Warn("YT:Provider", "onResume: _instance is null"); return; }
            _instance._isPaused = false;
            _instance.PauseStateChanged?.Invoke(false);
            _instance.VisibilityStateChanged?.Invoke(true);
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void OnLanguageFetched(string lang)
        {
            VerboseLog.Log("YT:Provider", $"← FetchLanguage resolved: \"{lang}\"");
        }
    }
}
#endif
