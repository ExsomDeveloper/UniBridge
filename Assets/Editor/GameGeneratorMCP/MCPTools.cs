using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameGenerator.MCP
{
    public static class MCPTools
    {
        private static readonly List<LogEntry> _logBuffer = new List<LogEntry>();
        private const int MAX_LOG_ENTRIES = 100;

        // Track compilation errors across requests
        private static bool _lastCompilationHadErrors = false;
        private static string _lastCompilationError = "";

        /// <summary>
        /// Call this early to ensure log capture starts immediately.
        /// Simply accessing this method triggers the static constructor.
        /// </summary>
        public static void EnsureInitialized()
        {
            // Static constructor runs on first access - nothing else needed
        }

        static MCPTools()
        {
            Application.logMessageReceived += OnLogReceived;

            // Subscribe to compilation events to track errors
            CompilationPipeline.assemblyCompilationFinished += (assemblyPath, messages) =>
            {
                foreach (var msg in messages)
                {
                    if (msg.type == CompilerMessageType.Error)
                    {
                        _lastCompilationHadErrors = true;
                        if (string.IsNullOrEmpty(_lastCompilationError))
                        {
                            _lastCompilationError = $"{msg.file}({msg.line}): {msg.message}";
                        }
                    }
                }
            };

            CompilationPipeline.compilationStarted += (obj) =>
            {
                _lastCompilationHadErrors = false;
                _lastCompilationError = "";
            };
        }

        private static void OnLogReceived(string message, string stackTrace, LogType type)
        {
            lock (_logBuffer)
            {
                _logBuffer.Add(new LogEntry
                {
                    message = message,
                    stackTrace = stackTrace,
                    type = type.ToString(),
                    timestamp = DateTime.Now.ToString("HH:mm:ss.fff")
                });

                while (_logBuffer.Count > MAX_LOG_ENTRIES)
                    _logBuffer.RemoveAt(0);
            }
        }

        public static string ExecuteTool(string toolName, string args)
        {
            switch (toolName.ToLower())
            {
                case "execute_menu_item":
                    return ExecuteMenuItem(args);
                case "get_hierarchy":
                    return GetHierarchy();
                case "get_selection":
                    return GetSelection();
                case "select_gameobject":
                    return SelectGameObject(args);
                case "get_console_logs":
                    return GetConsoleLogs(args);
                case "clear_console":
                    return ClearConsole();
                case "get_project_info":
                    return GetProjectInfo();
                case "play":
                    return EnterPlayMode();
                case "stop":
                    return ExitPlayMode();
                case "pause":
                    return TogglePause();
                case "refresh_assets":
                    return RefreshAssets();
                case "open_scene":
                    return OpenScene(args);
                case "get_scenes":
                    return GetScenes();
                case "ping":
                    return "{\"status\":\"pong\",\"time\":\"" + DateTime.Now.ToString("o") + "\"}";

                case "invoke_method":
                    return MCPInputTools.InvokeMethod(args);
                case "click_gameobject":
                    return MCPInputTools.ClickGameObject(args);
                case "send_key":
                    return MCPInputTools.SendKey(args);
                case "drag":
                    return MCPInputTools.Drag(args);

                case "get_screen_position":
                    return MCPPositionTools.GetScreenPosition(args);
                case "get_rect_screen_rect":
                    return MCPPositionTools.GetRectScreenRect(args);
                case "is_visible_on_screen":
                    return MCPPositionTools.IsVisibleOnScreen(args);
                case "raycast_from_screen":
                    return MCPPositionTools.RaycastFromScreen(args);

                case "get_ugui_hierarchy":
                    return MCPUGUITools.GetHierarchy(args);
                case "query_ugui_element":
                    return MCPUGUITools.QueryElement(args);
                case "get_ugui_element_info":
                    return MCPUGUITools.GetElementInfo(args);
                case "click_ugui_element":
                    return MCPUGUITools.ClickElement(args);

                case "get_uitoolkit_hierarchy":
                    return MCPUIToolkitTools.GetHierarchy(args);
                case "query_uitoolkit_element":
                    return MCPUIToolkitTools.QueryElement(args);
                case "get_uitoolkit_element_info":
                    return MCPUIToolkitTools.GetElementInfo(args);
                case "click_uitoolkit_element":
                    return MCPUIToolkitTools.ClickElement(args);

                case "get_component":
                    return MCPComponentTools.GetComponent(args);
                case "get_component_property":
                    return MCPComponentTools.GetComponentProperty(args);
                case "set_component_property":
                    return MCPComponentTools.SetComponentProperty(args);
                case "find_objects_by_component":
                    return MCPComponentTools.FindObjectsByComponent(args);

                case "screenshot":
                    return MCPScreenshotTools.TakeScreenshot(args);

                case "compile_and_wait":
                    return CompileAndWait(args);
                case "get_compilation_status":
                    return GetCompilationStatus();

                default:
                    return $"{{\"error\":\"Unknown tool: {toolName}\"}}";
            }
        }

        private static string ExecuteMenuItem(string menuPath)
        {
            if (string.IsNullOrEmpty(menuPath))
                return "{\"error\":\"Menu path is required\"}";

            var result = EditorApplication.ExecuteMenuItem(menuPath);
            return $"{{\"success\":{result.ToString().ToLower()},\"menuPath\":\"{EscapeJson(menuPath)}\"}}";
        }

        private static string GetHierarchy()
        {
            var sb = new StringBuilder();
            sb.Append("{\"scenes\":[");

            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                var scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;

                if (s > 0) sb.Append(",");
                sb.Append($"{{\"name\":\"{EscapeJson(scene.name)}\",\"path\":\"{EscapeJson(scene.path)}\",\"objects\":[");

                var roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    if (i > 0) sb.Append(",");
                    AppendGameObjectHierarchy(sb, roots[i], 0);
                }
                sb.Append("]}");
            }

            sb.Append("]}");
            return sb.ToString();
        }

        private static void AppendGameObjectHierarchy(StringBuilder sb, GameObject go, int depth)
        {
            sb.Append($"{{\"name\":\"{EscapeJson(go.name)}\",\"active\":{go.activeSelf.ToString().ToLower()}");
            sb.Append($",\"path\":\"{EscapeJson(GetGameObjectPath(go))}\"");

            var components = go.GetComponents<Component>();
            sb.Append(",\"components\":[");
            for (int c = 0; c < components.Length; c++)
            {
                if (components[c] == null) continue;
                if (c > 0) sb.Append(",");
                sb.Append($"\"{components[c].GetType().Name}\"");
            }
            sb.Append("]");

            if (go.transform.childCount > 0 && depth < 10)
            {
                sb.Append(",\"children\":[");
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    if (i > 0) sb.Append(",");
                    AppendGameObjectHierarchy(sb, go.transform.GetChild(i).gameObject, depth + 1);
                }
                sb.Append("]");
            }

            sb.Append("}");
        }

        private static string GetGameObjectPath(GameObject go)
        {
            var path = go.name;
            var parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        private static string GetSelection()
        {
            var selection = Selection.gameObjects;
            var sb = new StringBuilder();
            sb.Append("{\"selected\":[");

            for (int i = 0; i < selection.Length; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append($"{{\"name\":\"{EscapeJson(selection[i].name)}\",\"path\":\"{EscapeJson(GetGameObjectPath(selection[i]))}\"}}");
            }

            sb.Append($"],\"count\":{selection.Length}}}");
            return sb.ToString();
        }

        private static string SelectGameObject(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "{\"error\":\"Path is required\"}";

            var go = GameObject.Find(path);
            if (go == null)
            {
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    var found = FindByPath(root.transform, path);
                    if (found != null)
                    {
                        go = found.gameObject;
                        break;
                    }
                }
            }

            if (go == null)
                return $"{{\"error\":\"GameObject not found: {EscapeJson(path)}\"}}";

            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            return $"{{\"success\":true,\"selected\":\"{EscapeJson(go.name)}\"}}";
        }

        private static Transform FindByPath(Transform root, string path)
        {
            if (GetGameObjectPath(root.gameObject) == path)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindByPath(root.GetChild(i), path);
                if (found != null) return found;
            }
            return null;
        }

        private static string GetConsoleLogs(string countStr)
        {
            int count = 20;
            if (!string.IsNullOrEmpty(countStr))
                int.TryParse(countStr, out count);

            var sb = new StringBuilder();
            sb.Append("{\"logs\":[");

            lock (_logBuffer)
            {
                var startIndex = Math.Max(0, _logBuffer.Count - count);
                for (int i = startIndex; i < _logBuffer.Count; i++)
                {
                    if (i > startIndex) sb.Append(",");
                    var log = _logBuffer[i];
                    sb.Append($"{{\"type\":\"{log.type}\",\"message\":\"{EscapeJson(log.message)}\",\"time\":\"{log.timestamp}\"}}");
                }
            }

            sb.Append("]}");
            return sb.ToString();
        }

        private static string ClearConsole()
        {
            var logEntries = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
            if (logEntries != null)
            {
                var clearMethod = logEntries.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
                clearMethod?.Invoke(null, null);
            }

            lock (_logBuffer)
            {
                _logBuffer.Clear();
            }

            return "{\"success\":true}";
        }

        private static string GetProjectInfo()
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"productName\":\"{EscapeJson(Application.productName)}\",");
            sb.Append($"\"companyName\":\"{EscapeJson(Application.companyName)}\",");
            sb.Append($"\"version\":\"{EscapeJson(Application.version)}\",");
            sb.Append($"\"unityVersion\":\"{Application.unityVersion}\",");
            sb.Append($"\"platform\":\"{Application.platform}\",");
            sb.Append($"\"dataPath\":\"{EscapeJson(Application.dataPath)}\",");
            sb.Append($"\"isPlaying\":{EditorApplication.isPlaying.ToString().ToLower()},");
            sb.Append($"\"isPaused\":{EditorApplication.isPaused.ToString().ToLower()},");
            sb.Append($"\"isCompiling\":{EditorApplication.isCompiling.ToString().ToLower()}");
            sb.Append("}");
            return sb.ToString();
        }

        private static string EnterPlayMode()
        {
            if (EditorApplication.isPlaying)
                return "{\"success\":false,\"reason\":\"Already in play mode\"}";

            EditorApplication.isPlaying = true;
            return "{\"success\":true,\"action\":\"entering_play_mode\"}";
        }

        private static string ExitPlayMode()
        {
            if (!EditorApplication.isPlaying)
                return "{\"success\":false,\"reason\":\"Not in play mode\"}";

            EditorApplication.isPlaying = false;
            return "{\"success\":true,\"action\":\"exiting_play_mode\"}";
        }

        private static string TogglePause()
        {
            EditorApplication.isPaused = !EditorApplication.isPaused;
            return $"{{\"success\":true,\"isPaused\":{EditorApplication.isPaused.ToString().ToLower()}}}";
        }

        private static string RefreshAssets()
        {
            AssetDatabase.Refresh();
            return "{\"success\":true,\"action\":\"assets_refreshed\"}";
        }

        [Serializable]
        private class CompileArgs
        {
            public int timeoutMs = 60000;
            public bool waitForCompletion = true;
        }

        private static string CompileAndWait(string argsJson)
        {
            try
            {
                var args = MCPHelpers.ParseArgs<CompileArgs>(argsJson);

                // Stop play mode if running - compilation requires editor mode
                bool wasPlaying = EditorApplication.isPlaying;
                if (wasPlaying)
                {
                    EditorApplication.isPlaying = false;
                }

                // Check if already compiling
                bool isCompiling = EditorApplication.isCompiling;

                if (isCompiling)
                {
                    // Already compiling - return status for polling
                    return $"{{\"success\":true,\"status\":\"compiling\",\"isCompiling\":true,\"stoppedPlayMode\":{wasPlaying.ToString().ToLower()}}}";
                }

                // Not currently compiling - request compilation
                CompilationPipeline.RequestScriptCompilation();

                // Return immediately - let the bridge poll for completion
                // This is non-blocking so Unity's editor loop can process the compilation
                return $"{{\"success\":true,\"status\":\"compilation_requested\",\"isCompiling\":false,\"stoppedPlayMode\":{wasPlaying.ToString().ToLower()},\"message\":\"Compilation requested. Poll get_project_info for isCompiling status.\"}}";
            }
            catch (Exception ex)
            {
                return $"{{\"error\":\"{EscapeJson(ex.Message)}\"}}";
            }
        }

        private static string GetCompilationStatus()
        {
            bool isCompiling = EditorApplication.isCompiling;

            if (isCompiling)
            {
                return "{\"isCompiling\":true,\"status\":\"compiling\"}";
            }

            // Compilation finished
            if (_lastCompilationHadErrors)
            {
                return $"{{\"isCompiling\":false,\"status\":\"completed\",\"hasErrors\":true,\"error\":\"{EscapeJson(_lastCompilationError)}\"}}";
            }

            return "{\"isCompiling\":false,\"status\":\"completed\",\"hasErrors\":false}";
        }

        private static string OpenScene(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
                return "{\"error\":\"Scene path is required\"}";

            if (!scenePath.EndsWith(".unity"))
                scenePath += ".unity";

            if (!scenePath.StartsWith("Assets/"))
            {
                var foundPath = FindSceneByName(scenePath.Replace(".unity", ""));
                if (foundPath != null)
                    scenePath = foundPath;
            }

            if (!System.IO.File.Exists(scenePath))
            {
                var assetsPath = System.IO.Path.Combine(Application.dataPath, "..", scenePath);
                if (!System.IO.File.Exists(assetsPath))
                    return $"{{\"error\":\"Scene not found: {EscapeJson(scenePath)}\"}}";
            }

            if (EditorApplication.isPlaying)
                return "{\"error\":\"Cannot open scene while in Play mode\"}";

            var currentScene = SceneManager.GetActiveScene();
            if (currentScene.isDirty)
            {
                if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return "{\"error\":\"Scene change cancelled by user\"}";
            }

            var openedScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            if (openedScene.IsValid())
                return $"{{\"success\":true,\"scene\":\"{EscapeJson(openedScene.name)}\",\"path\":\"{EscapeJson(openedScene.path)}\"}}";

            return $"{{\"error\":\"Failed to open scene: {EscapeJson(scenePath)}\"}}";
        }

        private static string FindSceneByName(string sceneName)
        {
            var guids = AssetDatabase.FindAssets($"t:Scene {sceneName}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (name.Equals(sceneName, StringComparison.OrdinalIgnoreCase))
                    return path;
            }
            return null;
        }

        private static string GetScenes()
        {
            var sb = new StringBuilder();
            sb.Append("{\"scenes\":[");

            var guids = AssetDatabase.FindAssets("t:Scene");
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var name = System.IO.Path.GetFileNameWithoutExtension(path);

                if (i > 0) sb.Append(",");
                sb.Append($"{{\"name\":\"{EscapeJson(name)}\",\"path\":\"{EscapeJson(path)}\"}}");
            }

            sb.Append("],\"count\":" + guids.Length + "}");
            return sb.ToString();
        }

        private static string EscapeJson(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            return str
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        private struct LogEntry
        {
            public string message;
            public string stackTrace;
            public string type;
            public string timestamp;
        }
    }
}
