using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameGenerator.MCP
{
    public static class MCPUIToolkitTools
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

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;

        private static IntPtr _foundUnityWindow = IntPtr.Zero;

        private static bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam)
        {
            if (!IsWindowVisible(hWnd)) return true;

            var sb = new System.Text.StringBuilder(256);
            GetWindowText(hWnd, sb, 256);
            var title = sb.ToString();

            // Look for Unity Editor window - it contains "Unity" and often the project name
            if (title.Contains("Unity") && (title.Contains(" - ") || title.Contains("Editor")))
            {
                _foundUnityWindow = hWnd;
                return false; // Stop enumerating
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

        private static void FocusUnityEditor()
        {
#if UNITY_EDITOR_WIN
            try
            {
                // Find Unity window by title
                var unityWindowHandle = FindUnityWindow();

                if (unityWindowHandle == IntPtr.Zero)
                {
                    // Fallback to process main window
                    unityWindowHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                }

                if (unityWindowHandle != IntPtr.Zero)
                {
                    ShowWindow(unityWindowHandle, SW_RESTORE);
                    SetForegroundWindow(unityWindowHandle);
                    Debug.Log($"[MCP] FocusUnityEditor: Focused window handle {unityWindowHandle}");
                }
                else
                {
                    Debug.LogWarning("[MCP] FocusUnityEditor: Could not find Unity window");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MCP] FocusUnityEditor Windows error: {ex.Message}");
            }
#elif UNITY_EDITOR_OSX
            try
            {
                // On Mac, use System.Diagnostics to run osascript
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
            // Cross-platform: Focus game view within Unity
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

        private static void ForceGameViewRepaint()
        {
#if UNITY_EDITOR_WIN
            // Use PowerShell to focus Unity window (external process has better focus permissions)
            try
            {
                var windowHandle = FindUnityWindow();
                if (windowHandle != IntPtr.Zero)
                {
                    var script = $@"
Add-Type @'
using System;
using System.Runtime.InteropServices;
public class Win32Focus {{
    [DllImport(""user32.dll"")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport(""user32.dll"")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}}
'@
$hwnd = [IntPtr]{windowHandle.ToInt64()}
[Win32Focus]::ShowWindow($hwnd, 9) | Out-Null
[Win32Focus]::SetForegroundWindow($hwnd) | Out-Null
";
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    };
                    var process = System.Diagnostics.Process.Start(startInfo);
                    process?.WaitForExit(1000);

                    // Wait a moment for focus to take effect and frame to render
                    System.Threading.Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MCP] ForceGameViewRepaint PowerShell error: {ex.Message}");
            }
#endif
            // Also do internal focus
            FocusGameView();

            // Queue player loop update
            EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
        [Serializable]
        private class HierarchyArgs
        {
            public string documentName;
            public int maxDepth = 10;
        }

        [Serializable]
        private class QueryArgs
        {
            public string selector;
            public string name;
            public string documentName;
            public int limit = 20;
        }

        public static string GetHierarchy(string argsJson)
        {
            var args = MCPHelpers.ParseArgs<HierarchyArgs>(argsJson);
            var documents = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);

            var sb = new StringBuilder();
            sb.Append("{\"documents\":[");

            bool first = true;
            foreach (var doc in documents)
            {
                if (!string.IsNullOrEmpty(args.documentName) && doc.name != args.documentName)
                    continue;

                if (doc.rootVisualElement == null)
                    continue;

                if (!first) sb.Append(",");
                first = false;

                sb.Append($"{{\"name\":\"{MCPHelpers.EscapeJson(doc.name)}\",\"elements\":[");
                AppendVisualElement(sb, doc.rootVisualElement, 0, args.maxDepth, true);
                sb.Append("]}");
            }

            sb.Append("]}");
            return sb.ToString();
        }

        private static void AppendVisualElement(StringBuilder sb, VisualElement element, int depth, int maxDepth, bool isFirst)
        {
            if (depth > maxDepth) return;

            if (!isFirst) sb.Append(",");

            sb.Append($"{{\"name\":\"{MCPHelpers.EscapeJson(element.name)}\",\"type\":\"{element.GetType().Name}\"");

            if (!string.IsNullOrEmpty(element.viewDataKey))
                sb.Append($",\"viewDataKey\":\"{MCPHelpers.EscapeJson(element.viewDataKey)}\"");

            var classes = element.GetClasses().ToArray();
            if (classes.Length > 0)
            {
                sb.Append(",\"classes\":[");
                for (int i = 0; i < classes.Length; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append($"\"{MCPHelpers.EscapeJson(classes[i])}\"");
                }
                sb.Append("]");
            }

            var visible = element.resolvedStyle.display != DisplayStyle.None && element.resolvedStyle.visibility == Visibility.Visible;
            sb.Append($",\"visible\":{visible.ToString().ToLower()}");

            var worldBound = element.worldBound;
            sb.Append($",\"rect\":{{\"x\":{worldBound.x.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{worldBound.y.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"width\":{worldBound.width.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"height\":{worldBound.height.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}");

            AppendElementTypeInfo(sb, element);

            if (element.childCount > 0 && depth < maxDepth)
            {
                sb.Append(",\"children\":[");
                for (int i = 0; i < element.childCount; i++)
                {
                    AppendVisualElement(sb, element[i], depth + 1, maxDepth, i == 0);
                }
                sb.Append("]");
            }

            sb.Append("}");
        }

        private static void AppendElementTypeInfo(StringBuilder sb, VisualElement element)
        {
            if (element is Button btn)
            {
                sb.Append($",\"elementType\":\"Button\",\"text\":\"{MCPHelpers.EscapeJson(btn.text)}\"");
            }
            else if (element is Label lbl)
            {
                sb.Append($",\"elementType\":\"Label\",\"text\":\"{MCPHelpers.EscapeJson(lbl.text)}\"");
            }
            else if (element is TextField tf)
            {
                sb.Append($",\"elementType\":\"TextField\",\"value\":\"{MCPHelpers.EscapeJson(tf.value)}\",\"label\":\"{MCPHelpers.EscapeJson(tf.label)}\"");
            }
            else if (element is Toggle tgl)
            {
                sb.Append($",\"elementType\":\"Toggle\",\"value\":{tgl.value.ToString().ToLower()},\"label\":\"{MCPHelpers.EscapeJson(tgl.label)}\"");
            }
            else if (element is Slider sld)
            {
                sb.Append($",\"elementType\":\"Slider\",\"value\":{sld.value.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"lowValue\":{sld.lowValue.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"highValue\":{sld.highValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            }
            else if (element is SliderInt sldInt)
            {
                sb.Append($",\"elementType\":\"SliderInt\",\"value\":{sldInt.value},\"lowValue\":{sldInt.lowValue},\"highValue\":{sldInt.highValue}");
            }
            else if (element is DropdownField dd)
            {
                sb.Append($",\"elementType\":\"DropdownField\",\"value\":\"{MCPHelpers.EscapeJson(dd.value)}\",\"index\":{dd.index}");
                if (dd.choices != null && dd.choices.Count > 0)
                {
                    sb.Append(",\"choices\":[");
                    for (int i = 0; i < dd.choices.Count; i++)
                    {
                        if (i > 0) sb.Append(",");
                        sb.Append($"\"{MCPHelpers.EscapeJson(dd.choices[i])}\"");
                    }
                    sb.Append("]");
                }
            }
            else if (element is IntegerField intField)
            {
                sb.Append($",\"elementType\":\"IntegerField\",\"value\":{intField.value}");
            }
            else if (element is FloatField floatField)
            {
                sb.Append($",\"elementType\":\"FloatField\",\"value\":{floatField.value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            }
            else if (element is ProgressBar pb)
            {
                sb.Append($",\"elementType\":\"ProgressBar\",\"value\":{pb.value.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"title\":\"{MCPHelpers.EscapeJson(pb.title)}\"");
            }
            else if (element is ScrollView sv)
            {
                sb.Append(",\"elementType\":\"ScrollView\"");
            }
            else if (element is ListView lv)
            {
                sb.Append($",\"elementType\":\"ListView\",\"itemCount\":{lv.itemsSource?.Count ?? 0}");
            }
            else if (element is Foldout fo)
            {
                sb.Append($",\"elementType\":\"Foldout\",\"text\":\"{MCPHelpers.EscapeJson(fo.text)}\",\"value\":{fo.value.ToString().ToLower()}");
            }
        }

        public static string QueryElement(string argsJson)
        {
            var args = MCPHelpers.ParseArgs<QueryArgs>(argsJson);
            var documents = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);

            var sb = new StringBuilder();
            sb.Append("{\"results\":[");

            int count = 0;
            bool first = true;

            foreach (var doc in documents)
            {
                if (!string.IsNullOrEmpty(args.documentName) && doc.name != args.documentName)
                    continue;

                if (doc.rootVisualElement == null)
                    continue;

                if (!string.IsNullOrEmpty(args.selector))
                {
                    var found = doc.rootVisualElement.Query<VisualElement>(args.selector).ToList();
                    foreach (var el in found)
                    {
                        if (count >= args.limit) break;
                        AppendQueryResult(sb, el, ref first);
                        count++;
                    }
                }
                else if (!string.IsNullOrEmpty(args.name))
                {
                    SearchByName(doc.rootVisualElement, args.name, sb, ref count, ref first, args.limit);
                }

                if (count >= args.limit) break;
            }

            sb.Append($"],\"count\":{count}}}");
            return sb.ToString();
        }

        private static void SearchByName(VisualElement element, string name, StringBuilder sb, ref int count, ref bool first, int limit)
        {
            if (count >= limit) return;

            if (!string.IsNullOrEmpty(element.name) && element.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AppendQueryResult(sb, element, ref first);
                count++;
            }

            for (int i = 0; i < element.childCount && count < limit; i++)
            {
                SearchByName(element[i], name, sb, ref count, ref first, limit);
            }
        }

        private static void AppendQueryResult(StringBuilder sb, VisualElement element, ref bool first)
        {
            if (!first) sb.Append(",");
            first = false;

            var worldBound = element.worldBound;
            sb.Append($"{{\"name\":\"{MCPHelpers.EscapeJson(element.name)}\",\"type\":\"{element.GetType().Name}\",\"rect\":{{\"x\":{worldBound.x.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{worldBound.y.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"width\":{worldBound.width.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"height\":{worldBound.height.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}");
            AppendElementTypeInfo(sb, element);
            sb.Append("}");
        }

        public static string GetElementInfo(string argsJson)
        {
            var args = MCPHelpers.ParseArgs<QueryArgs>(argsJson);
            var documents = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);

            foreach (var doc in documents)
            {
                if (!string.IsNullOrEmpty(args.documentName) && doc.name != args.documentName)
                    continue;

                if (doc.rootVisualElement == null)
                    continue;

                VisualElement element = null;

                if (!string.IsNullOrEmpty(args.selector))
                {
                    element = doc.rootVisualElement.Q<VisualElement>(args.selector);
                }
                else if (!string.IsNullOrEmpty(args.name))
                {
                    element = doc.rootVisualElement.Q<VisualElement>(args.name);
                }

                if (element != null)
                {
                    return BuildDetailedElementInfo(element, doc.name);
                }
            }

            return "{\"error\":\"Element not found\"}";
        }

        private static string BuildDetailedElementInfo(VisualElement element, string documentName)
        {
            var sb = new StringBuilder();
            sb.Append($"{{\"name\":\"{MCPHelpers.EscapeJson(element.name)}\",\"document\":\"{MCPHelpers.EscapeJson(documentName)}\",\"type\":\"{element.GetType().Name}\"");

            var visible = element.resolvedStyle.display != DisplayStyle.None && element.resolvedStyle.visibility == Visibility.Visible;
            sb.Append($",\"visible\":{visible.ToString().ToLower()}");
            sb.Append($",\"enabledSelf\":{element.enabledSelf.ToString().ToLower()}");
            sb.Append($",\"enabledInHierarchy\":{element.enabledInHierarchy.ToString().ToLower()}");

            var worldBound = element.worldBound;
            sb.Append($",\"rect\":{{\"x\":{worldBound.x.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{worldBound.y.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"width\":{worldBound.width.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"height\":{worldBound.height.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}");

            var classes = element.GetClasses().ToArray();
            if (classes.Length > 0)
            {
                sb.Append(",\"classes\":[");
                for (int i = 0; i < classes.Length; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append($"\"{MCPHelpers.EscapeJson(classes[i])}\"");
                }
                sb.Append("]");
            }

            AppendElementTypeInfo(sb, element);

            sb.Append($",\"childCount\":{element.childCount}");

            sb.Append("}");
            return sb.ToString();
        }

        public static string ClickElement(string argsJson)
        {
            if (!Application.isPlaying)
                return "{\"error\":\"Play Mode required\"}";

            // Focus Unity Editor and game view to ensure UI events are processed
            FocusUnityEditor();

            // Small delay to let focus take effect
            System.Threading.Thread.Sleep(100);

            // Repaint to ensure UI is ready
            var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType != null)
            {
                var gameView = EditorWindow.GetWindow(gameViewType, false, "Game", false);
                if (gameView != null)
                {
                    gameView.Repaint();
                }
            }

            var args = MCPHelpers.ParseArgs<QueryArgs>(argsJson);
            var documents = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);

            foreach (var doc in documents)
            {
                if (!string.IsNullOrEmpty(args.documentName) && doc.name != args.documentName)
                    continue;

                if (doc.rootVisualElement == null)
                    continue;

                VisualElement element = null;

                if (!string.IsNullOrEmpty(args.selector))
                {
                    element = doc.rootVisualElement.Q<VisualElement>(args.selector);
                }
                else if (!string.IsNullOrEmpty(args.name))
                {
                    element = doc.rootVisualElement.Q<VisualElement>(args.name);
                }

                if (element != null)
                {
                    var worldBound = element.worldBound;
                    Debug.Log($"[MCP] ClickElement: Found '{element.name}' type={element.GetType().Name} worldBound={worldBound} enabled={element.enabledInHierarchy}");

                    if (!element.enabledInHierarchy)
                        return "{\"error\":\"Element not enabled\"}";

                    if (element is Button button)
                    {
                        string method = "unknown";

                        try
                        {
                            // UI Toolkit: Button has a public 'clickable' property of type Clickable
                            // Clickable has an internal 'Invoke' method or we can get its clicked event
                            var clickable = button.clickable;
                            Debug.Log($"[MCP] ClickElement: Got button.clickable = {clickable}");

                            if (clickable != null)
                            {
                                // Try to find and invoke the clicked event backing field
                                // In C#, event backing fields are usually named same as the event
                                var allFields = typeof(Clickable).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                                Debug.Log($"[MCP] ClickElement: Clickable fields = {string.Join(", ", allFields.Select(f => $"{f.Name}:{f.FieldType.Name}"))}");

                                // Look for the clicked event backing field
                                foreach (var field in allFields)
                                {
                                    if (field.Name.ToLower().Contains("click") && field.FieldType == typeof(Action))
                                    {
                                        var action = field.GetValue(clickable) as Action;
                                        if (action != null)
                                        {
                                            Debug.Log($"[MCP] ClickElement: Found Action in field '{field.Name}', invoking...");
                                            action.Invoke();
                                            method = $"Clickable.{field.Name}";

                                            // Force repaint after click
                                            ForceGameViewRepaint();

                                            return $"{{\"success\":true,\"clicked\":\"{MCPHelpers.EscapeJson(element.name)}\",\"type\":\"Button\",\"method\":\"{method}\"}}";
                                        }
                                    }
                                }

                                // Try InvokeClicked method
                                var invokeMethod = typeof(Clickable).GetMethod("InvokeClicked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                if (invokeMethod != null)
                                {
                                    Debug.Log($"[MCP] ClickElement: Found InvokeClicked method, invoking...");
                                    invokeMethod.Invoke(clickable, null);
                                    method = "Clickable.InvokeClicked";
                                    return $"{{\"success\":true,\"clicked\":\"{MCPHelpers.EscapeJson(element.name)}\",\"type\":\"Button\",\"method\":\"{method}\"}}";
                                }

                                // Try Invoke method
                                var invokeMethod2 = typeof(Clickable).GetMethod("Invoke", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                if (invokeMethod2 != null)
                                {
                                    Debug.Log($"[MCP] ClickElement: Found Invoke method, invoking...");
                                    invokeMethod2.Invoke(clickable, null);
                                    method = "Clickable.Invoke";
                                    return $"{{\"success\":true,\"clicked\":\"{MCPHelpers.EscapeJson(element.name)}\",\"type\":\"Button\",\"method\":\"{method}\"}}";
                                }

                                // List all methods for debugging
                                var allMethods = typeof(Clickable).GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                Debug.Log($"[MCP] ClickElement: Clickable methods = {string.Join(", ", allMethods.Select(m => m.Name))}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[MCP] ClickElement reflection error: {ex.Message}\n{ex.StackTrace}");
                        }

                        // Fallback: Use NavigationSubmitEvent
                        Debug.Log($"[MCP] ClickElement: Falling back to NavigationSubmitEvent");
                        using (var submitEvent = NavigationSubmitEvent.GetPooled())
                        {
                            submitEvent.target = button;
                            button.SendEvent(submitEvent);
                        }
                        method = "NavigationSubmit";

                        return $"{{\"success\":true,\"clicked\":\"{MCPHelpers.EscapeJson(element.name)}\",\"type\":\"Button\",\"method\":\"{method}\",\"worldBound\":{{\"x\":{worldBound.x},\"y\":{worldBound.y},\"w\":{worldBound.width},\"h\":{worldBound.height}}}}}";
                    }

                    if (element is Toggle toggle)
                    {
                        toggle.value = !toggle.value;
                        return $"{{\"success\":true,\"clicked\":\"{MCPHelpers.EscapeJson(element.name)}\",\"type\":\"Toggle\",\"value\":{toggle.value.ToString().ToLower()}}}";
                    }

                    // For other elements, try NavigationSubmitEvent
                    using (var submitEvent = NavigationSubmitEvent.GetPooled())
                    {
                        submitEvent.target = element;
                        element.SendEvent(submitEvent);
                    }
                    return $"{{\"success\":true,\"clicked\":\"{MCPHelpers.EscapeJson(element.name)}\",\"type\":\"{element.GetType().Name}\"}}";
                }
            }

            return "{\"error\":\"Element not found\"}";
        }
    }
}
