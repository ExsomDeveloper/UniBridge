using System;

namespace UniBridge
{
    public interface IPlatformParamsProvider
    {
        string GetLanguage();
        bool IsAudioEnabled { get; }
        bool IsPaused       { get; }
        bool IsVisible      { get; }

        event Action<bool> AudioStateChanged;
        event Action<bool> PauseStateChanged;
        event Action<bool> VisibilityStateChanged;

        void SendMessage(PlatformMessage message);

        string GetPlatformId();
    }
}
