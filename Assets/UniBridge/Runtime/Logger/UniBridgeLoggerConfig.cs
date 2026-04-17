using UnityEngine;

namespace UniBridge
{
    public enum LogActivationGesture
    {
        Disabled,
        SixTapTopLeft,
    }

    [CreateAssetMenu(fileName = nameof(UniBridgeLoggerConfig), menuName = "UniBridge/Logger Configuration")]
    public class UniBridgeLoggerConfig : ScriptableObject
    {
        [Tooltip("Master switch. When off, no logs are collected and no overlay is available.")]
        public bool Enabled = true;

        [Tooltip("Max number of log entries kept in the ring buffer.")]
        public int BufferSize = 500;

        [Tooltip("How the overlay is summoned at runtime.")]
        public LogActivationGesture ActivationGesture = LogActivationGesture.SixTapTopLeft;

        [Tooltip("Include Unity stack traces in the export. Heavier payload; useful for dev builds.")]
        public bool IncludeStackTraces = true;

        [Tooltip("If true, the overlay opens automatically when the game starts (no gesture needed).")]
        public bool OpenAtStartup = false;
    }
}
