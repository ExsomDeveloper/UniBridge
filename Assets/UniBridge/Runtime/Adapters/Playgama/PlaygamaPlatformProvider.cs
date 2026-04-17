#if UNIBRIDGE_PLAYGAMA && UNITY_WEBGL
using System;
using Playgama;
using Playgama.Modules.Game;
using UnityEngine;
using PlaygamaPlatformMessage = Playgama.Modules.Platform.PlatformMessage;

namespace UniBridge
{
    internal static class PlaygamaPlatformProvider
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Register()
        {
            UniBridgeEnvironment.SetProvider(new PlaygamaPlatformParamsProvider(), "Playgama");
        }
    }

    internal sealed class PlaygamaPlatformParamsProvider : IPlatformParamsProvider
    {
        private bool _isPaused;

        public string GetPlatformId() => Bridge.platform.id;
        public string GetLanguage()  => Bridge.platform.language;
        public bool IsAudioEnabled   => Bridge.platform.isAudioEnabled;
        public bool IsPaused         => _isPaused;
        public bool IsVisible        => Bridge.game.visibilityState == VisibilityState.Visible;

        public event Action<bool> AudioStateChanged;
        public event Action<bool> PauseStateChanged;
        public event Action<bool> VisibilityStateChanged;

        public PlaygamaPlatformParamsProvider()
        {
            Bridge.platform.audioStateChanged  += OnAudioStateChanged;
            Bridge.platform.pauseStateChanged  += OnPauseStateChanged;
            Bridge.game.visibilityStateChanged += OnVisibilityStateChanged;
        }

        private void OnAudioStateChanged(bool enabled)
        {
            AudioStateChanged?.Invoke(enabled);
        }

        private void OnPauseStateChanged(bool paused)
        {
            _isPaused = paused;
            PauseStateChanged?.Invoke(paused);
        }

        private void OnVisibilityStateChanged(VisibilityState state)
        {
            VisibilityStateChanged?.Invoke(state == VisibilityState.Visible);
        }

        public void SendMessage(PlatformMessage message)
        {
            Bridge.platform.SendMessage(message switch
            {
                PlatformMessage.GameReady            => PlaygamaPlatformMessage.GameReady,
                PlatformMessage.InGameLoadingStarted => PlaygamaPlatformMessage.InGameLoadingStarted,
                PlatformMessage.InGameLoadingStopped => PlaygamaPlatformMessage.InGameLoadingStopped,
                PlatformMessage.GameplayStarted      => PlaygamaPlatformMessage.GameplayStarted,
                PlatformMessage.GameplayStopped      => PlaygamaPlatformMessage.GameplayStopped,
                PlatformMessage.PlayerGotAchievement => PlaygamaPlatformMessage.PlayerGotAchievement,
                PlatformMessage.FirstFrameReady      => PlaygamaPlatformMessage.GameReady, // Playgama не различает firstFrame/gameReady
                _                                    => throw new ArgumentOutOfRangeException(nameof(message), message, null)
            });
        }
    }
}
#endif
