using System.Diagnostics;

namespace UniBridge
{
    public static class VerboseLog
    {
        [Conditional("UNIBRIDGE_VERBOSE_LOG")]
        public static void Log(string source, string message) =>
            UnityEngine.Debug.Log($"[{source}] {message}");

        [Conditional("UNIBRIDGE_VERBOSE_LOG")]
        public static void Warn(string source, string message) =>
            UnityEngine.Debug.LogWarning($"[{source}] {message}");

        [Conditional("UNIBRIDGE_VERBOSE_LOG")]
        public static void Error(string source, string message) =>
            UnityEngine.Debug.LogError($"[{source}] {message}");
    }
}
