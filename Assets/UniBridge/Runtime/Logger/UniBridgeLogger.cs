using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UniBridge
{
    public static class UniBridgeLogger
    {
        public readonly struct Entry
        {
            public readonly DateTime Timestamp;
            public readonly LogType  Type;
            public readonly string   Condition;
            public readonly string   StackTrace;

            public Entry(DateTime timestamp, LogType type, string condition, string stackTrace)
            {
                Timestamp  = timestamp;
                Type       = type;
                Condition  = condition;
                StackTrace = stackTrace;
            }
        }

        private static UniBridgeLoggerConfig _config;
        private static UniBridgeLoggerRuntime _runtime;
        private static readonly Queue<Entry> _buffer = new();
        private static int _capacity = 500;

        public static UniBridgeLoggerConfig Config => _config;
        public static bool IsEnabled => _config != null && _config.Enabled;

        // SubsystemRegistration — earliest runtime stage. Subscribes before any facade
        // BeforeSceneLoad init or adapter self-registration logs, so no early logs are lost.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void SubscribeEarly()
        {
            _config = Resources.Load<UniBridgeLoggerConfig>(nameof(UniBridgeLoggerConfig));
            if (_config == null || !_config.Enabled) return;

            _capacity = Mathf.Max(32, _config.BufferSize);
            Application.logMessageReceivedThreaded += OnLogReceived;
        }

        // BeforeSceneLoad — safe to create MonoBehaviours (GameObject creation is invalid
        // during SubsystemRegistration on some platforms).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (_config == null || !_config.Enabled) return;

            var go = new GameObject(nameof(UniBridgeLoggerRuntime));
            UnityEngine.Object.DontDestroyOnLoad(go);
            _runtime = go.AddComponent<UniBridgeLoggerRuntime>();

            // Build the overlay UI immediately (hidden) so Show() is instant and we can detect render issues at startup.
            _runtime.EnsurePanel();

            if (_config.ActivationGesture != LogActivationGesture.Disabled)
                go.AddComponent<LogOverlayActivator>();

            if (_config.OpenAtStartup)
                _runtime.ShowPanel();

            Debug.Log($"[{nameof(UniBridgeLogger)}] Initialized (buffer={_capacity}, gesture={_config.ActivationGesture})");
        }

        private static void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            var entry = new Entry(DateTime.UtcNow, type, condition, stackTrace);
            lock (_buffer)
            {
                _buffer.Enqueue(entry);
                while (_buffer.Count > _capacity) _buffer.Dequeue();
            }
        }

        public static Entry[] GetSnapshot()
        {
            lock (_buffer) return _buffer.ToArray();
        }

        public static void Clear()
        {
            lock (_buffer) _buffer.Clear();
        }

        public static string ExportAsText()
        {
            var includeStack = _config != null && _config.IncludeStackTraces;
            var entries = GetSnapshot();
            var sb = new StringBuilder(entries.Length * 128);
            sb.Append("UniBridge log export — ").Append(DateTime.UtcNow.ToString("o")).Append('\n');
            sb.Append("Unity ").Append(Application.unityVersion).Append(" | ");
            sb.Append(Application.productName).Append(' ').Append(Application.version).Append(" | ");
            sb.Append(Application.platform).Append('\n');
            sb.Append("Entries: ").Append(entries.Length).Append("\n\n");

            foreach (var e in entries)
            {
                sb.Append('[').Append(e.Timestamp.ToString("HH:mm:ss.fff")).Append("] ");
                sb.Append(e.Type).Append(": ").Append(e.Condition).Append('\n');
                if (includeStack && !string.IsNullOrEmpty(e.StackTrace))
                    sb.Append(e.StackTrace).Append('\n');
            }
            return sb.ToString();
        }

        public static void ShowOverlay()
        {
            if (!IsEnabled || _runtime == null) return;
            _runtime.ShowPanel();
        }

        public static void HideOverlay()
        {
            if (_runtime == null) return;
            _runtime.HidePanel();
        }

        public static void ToggleOverlay()
        {
            if (!IsEnabled || _runtime == null) return;
            _runtime.TogglePanel();
        }
    }

    internal class UniBridgeLoggerRuntime : MonoBehaviour
    {
        private LogOverlayPanel _panel;

        public void EnsurePanel()
        {
            if (_panel == null)
            {
                // Host overlay on its own GameObject so the UIDocument renders independently.
                var host = new GameObject("UniBridgeLoggerOverlay");
                DontDestroyOnLoad(host);
                _panel = host.AddComponent<LogOverlayPanel>();
            }
        }

        public void ShowPanel()
        {
            EnsurePanel();
            _panel.Show();
        }

        public void HidePanel()
        {
            if (_panel != null) _panel.Hide();
        }

        public void TogglePanel()
        {
            EnsurePanel();
            _panel.Toggle();
        }

        private void Update()
        {
            // Universal hotkey for desktop testers: F12 toggles overlay.
            if (Input.GetKeyDown(KeyCode.F12)) TogglePanel();
        }
    }
}
