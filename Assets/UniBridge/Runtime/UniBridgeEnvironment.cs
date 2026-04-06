using System;
using UnityEngine;

namespace UniBridge
{
    public static class UniBridgeEnvironment
    {
        private static IPlatformParamsProvider _provider;
        private static string                  _providerName;

        /// <summary>
        /// Name of the active provider, for debugging.
        /// </summary>
        public static string ProviderName
        {
            get
            {
                if (_providerName != null) return _providerName;
#if UNIBRIDGE_STORE_RUSTORE
                return "RuStore (fixed: ru)";
#else
                return "SystemLanguage";
#endif
            }
        }

        // ── Platform ID ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns the platform identifier (e.g. "yandex", "vk"). Returns "" on non-Playgama platforms.
        /// </summary>
        public static string GetPlatformId() => _provider?.GetPlatformId() ?? "";

        // ── Language ──────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the ISO 639-1 language code for the current platform ("ru", "en", "tr", …).
        /// </summary>
        public static string GetLanguage()
        {
            if (_provider != null) return _provider.GetLanguage();
#if UNIBRIDGE_STORE_RUSTORE
            return "ru";
#else
            return SystemLanguageToIsoCode(Application.systemLanguage);
#endif
        }

        // ── Audio ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Whether audio is enabled on the platform. Always true on platforms without SDK audio control.
        /// </summary>
        public static bool IsAudioEnabled => _provider?.IsAudioEnabled ?? true;

        /// <summary>
        /// Fires when the platform changes the audio state (Playgama).
        /// </summary>
        public static event Action<bool> AudioStateChanged;

        // ── Pause ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Whether the platform has paused the game. Tracked via events (no synchronous Poll API).
        /// Always false on platforms without SDK-level pause support.
        /// </summary>
        public static bool IsPaused => _provider?.IsPaused ?? false;

        /// <summary>
        /// Fires when the platform pauses or resumes the game (Playgama).
        /// </summary>
        public static event Action<bool> PauseStateChanged;

        // ── Visibility ────────────────────────────────────────────────────────

        /// <summary>
        /// Whether the game tab/window is visible. Always true on platforms without SDK visibility control.
        /// </summary>
        public static bool IsVisible => _provider?.IsVisible ?? true;

        /// <summary>
        /// Fires when the user hides or shows the tab (Playgama).
        /// </summary>
        public static event Action<bool> VisibilityStateChanged;

        // ── Provider registration ─────────────────────────────────────────────

        /// <summary>
        /// Called by the platform provider during initialization.
        /// </summary>
        public static void SetProvider(IPlatformParamsProvider provider, string name = "Unknown")
        {
            if (_provider != null)
            {
                _provider.AudioStateChanged      -= OnAudioStateChanged;
                _provider.PauseStateChanged      -= OnPauseStateChanged;
                _provider.VisibilityStateChanged -= OnVisibilityStateChanged;
            }

            _provider     = provider;
            _providerName = name;

            if (_provider != null)
            {
                _provider.AudioStateChanged      += OnAudioStateChanged;
                _provider.PauseStateChanged      += OnPauseStateChanged;
                _provider.VisibilityStateChanged += OnVisibilityStateChanged;
            }
        }

        // ── Platform messaging ────────────────────────────────────────────────

        /// <summary>
        /// Sends a message to the platform (Playgama). No-op on other platforms.
        /// </summary>
        public static void SendMessage(PlatformMessage message)
        {
            _provider?.SendMessage(message);
        }

        // ── Event relay ───────────────────────────────────────────────────────

        private static void OnAudioStateChanged(bool enabled)     => AudioStateChanged?.Invoke(enabled);
        private static void OnPauseStateChanged(bool paused)      => PauseStateChanged?.Invoke(paused);
        private static void OnVisibilityStateChanged(bool visible) => VisibilityStateChanged?.Invoke(visible);

        // ── Language helpers ──────────────────────────────────────────────────

        private static string SystemLanguageToIsoCode(SystemLanguage lang) => lang switch
        {
            SystemLanguage.Russian            => "ru",
            SystemLanguage.English            => "en",
            SystemLanguage.German             => "de",
            SystemLanguage.French             => "fr",
            SystemLanguage.Spanish            => "es",
            SystemLanguage.Italian            => "it",
            SystemLanguage.Portuguese         => "pt",
            SystemLanguage.Chinese            => "zh",
            SystemLanguage.ChineseSimplified  => "zh",
            SystemLanguage.ChineseTraditional => "zh",
            SystemLanguage.Japanese           => "ja",
            SystemLanguage.Korean             => "ko",
            SystemLanguage.Turkish            => "tr",
            SystemLanguage.Arabic             => "ar",
            SystemLanguage.Polish             => "pl",
            SystemLanguage.Dutch              => "nl",
            SystemLanguage.Swedish            => "sv",
            SystemLanguage.Norwegian          => "no",
            SystemLanguage.Danish             => "da",
            SystemLanguage.Finnish            => "fi",
            SystemLanguage.Romanian           => "ro",
            SystemLanguage.Czech              => "cs",
            SystemLanguage.Hungarian          => "hu",
            SystemLanguage.Ukrainian          => "uk",
            SystemLanguage.Thai               => "th",
            SystemLanguage.Vietnamese         => "vi",
            SystemLanguage.Indonesian         => "id",
            _                                => "en",
        };
    }
}
