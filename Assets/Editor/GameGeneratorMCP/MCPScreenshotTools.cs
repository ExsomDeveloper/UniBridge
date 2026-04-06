using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace GameGenerator.MCP
{
    public static class MCPScreenshotTools
    {
#if UNITY_EDITOR_WIN
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const int SW_RESTORE = 9;

        private static IntPtr _foundUnityWindow = IntPtr.Zero;

        private static bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam)
        {
            if (!IsWindowVisible(hWnd)) return true;

            var sb = new System.Text.StringBuilder(256);
            GetWindowText(hWnd, sb, 256);
            var title = sb.ToString();

            if (title.Contains("Unity") && (title.Contains(" - ") || title.Contains("Editor")))
            {
                _foundUnityWindow = hWnd;
                return false;
            }
            return true;
        }

        private static IntPtr FindUnityWindow()
        {
            _foundUnityWindow = IntPtr.Zero;
            EnumWindows(EnumWindowsCallback, IntPtr.Zero);
            return _foundUnityWindow;
        }
#endif

        // Fixed screenshot path - agent always knows where to find it
        private static string GetScreenshotPath()
        {
            var tempDir = Path.Combine(Application.dataPath, "..", "Temp");
            Directory.CreateDirectory(tempDir);
            return Path.GetFullPath(Path.Combine(tempDir, "screenshot.png"));
        }

        public static string TakeScreenshot(string argsJson)
        {
            try
            {
                var outputPath = GetScreenshotPath();

                if (!EditorApplication.isPlaying)
                {
                    return "{\"error\":\"Screenshot requires Play Mode. Use unity_play first, then unity_screenshot.\"}";
                }

                // Delete old screenshot if exists so bridge can detect when new one is ready
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }

                // Focus Unity Editor and Game View - required for ScreenCapture to work
                FocusUnityEditor();

                // Queue screenshot - will be captured at end of current frame
                // This captures the full game view including all UI (UGUI, UI Toolkit)
                ScreenCapture.CaptureScreenshot(outputPath);

                Debug.Log($"[MCP] Screenshot queued: {outputPath}");

                // Return immediately - bridge will poll for file
                return $"{{\"success\":true,\"path\":\"{MCPHelpers.EscapeJson(outputPath)}\",\"pending\":true}}";
            }
            catch (Exception ex)
            {
                return $"{{\"error\":\"{MCPHelpers.EscapeJson(ex.Message)}\"}}";
            }
        }

        private static void FocusUnityEditor()
        {
#if UNITY_EDITOR_WIN
            try
            {
                // Find Unity window by title
                var unityWindowHandle = FindUnityWindow();

                if (unityWindowHandle == IntPtr.Zero)
                {
                    unityWindowHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                }

                if (unityWindowHandle != IntPtr.Zero)
                {
                    ShowWindow(unityWindowHandle, SW_RESTORE);
                    SetForegroundWindow(unityWindowHandle);
                    Debug.Log($"[MCP] FocusUnityEditor: Focused window handle {unityWindowHandle}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MCP] FocusUnityEditor Windows error: {ex.Message}");
            }
#elif UNITY_EDITOR_OSX
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "osascript",
                    Arguments = "-e 'tell application \"Unity\" to activate'",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                System.Diagnostics.Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MCP] FocusUnityEditor Mac error: {ex.Message}");
            }
#endif
            // Focus game view within Unity
            FocusGameView();
        }

        private static void FocusGameView()
        {
            var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType == null) return;

            var gameView = EditorWindow.GetWindow(gameViewType, false, "Game", true);
            if (gameView != null)
            {
                gameView.Focus();
                gameView.Repaint();
            }
        }
    }
}
