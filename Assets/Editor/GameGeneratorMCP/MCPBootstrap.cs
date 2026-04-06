using UnityEditor;
using UnityEngine;

namespace GameGenerator.MCP
{
    [InitializeOnLoad]
    public static class MCPBootstrap
    {
        private const int DEFAULT_PORT = 9876;
        private const string PORT_KEY = "MCP_SERVER_PORT";

        private static string EnabledKey => $"MCP_ENABLED_{Application.dataPath.GetHashCode()}";

        static MCPBootstrap()
        {
            EditorApplication.delayCall += Initialize;
        }

        private static void Initialize()
        {
            EditorApplication.update += Update;
            EditorApplication.quitting += OnQuitting;

            if (!IsEnabled)
            {
                Debug.Log("[MCP] Disabled for this project. Enable via Tools/MCP/Enabled.");
                return;
            }

            // Initialize MCPTools early so it starts capturing logs immediately
            MCPTools.EnsureInitialized();

            var port = EditorPrefs.GetInt(PORT_KEY, DEFAULT_PORT);
            MCPServer.Instance.Start(port);

            Debug.Log($"[MCP] Bootstrap initialized. Server running on port {port}");
        }

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(EnabledKey, true);
            set => EditorPrefs.SetBool(EnabledKey, value);
        }

        private static void Update()
        {
            MCPServer.Instance.ProcessMainThreadQueue();
        }

        private static void OnQuitting()
        {
            MCPServer.Instance.Stop();
        }

        [MenuItem("Tools/MCP/Enabled")]
        public static void ToggleEnabled()
        {
            IsEnabled = !IsEnabled;
            if (IsEnabled)
            {
                MCPTools.EnsureInitialized();
                var port = EditorPrefs.GetInt(PORT_KEY, DEFAULT_PORT);
                MCPServer.Instance.Start(port);
                Debug.Log($"[MCP] Enabled. Server started on port {port}");
            }
            else
            {
                MCPServer.Instance.Stop();
                Debug.Log("[MCP] Disabled. Server stopped.");
            }
        }

        [MenuItem("Tools/MCP/Enabled", true)]
        public static bool ToggleEnabledValidate()
        {
            Menu.SetChecked("Tools/MCP/Enabled", IsEnabled);
            return true;
        }

        [MenuItem("Tools/MCP/Start Server")]
        public static void StartServer()
        {
            var port = EditorPrefs.GetInt(PORT_KEY, DEFAULT_PORT);
            MCPServer.Instance.Start(port);
        }

        [MenuItem("Tools/MCP/Stop Server")]
        public static void StopServer()
        {
            MCPServer.Instance.Stop();
        }

        [MenuItem("Tools/MCP/Show Status")]
        public static void ShowStatus()
        {
            var server = MCPServer.Instance;
            if (server.IsRunning)
            {
                Debug.Log($"[MCP] Server is RUNNING on port {server.Port}");
                Debug.Log($"[MCP] Health check: http://localhost:{server.Port}/health");
                Debug.Log($"[MCP] Tools list: http://localhost:{server.Port}/tools");
            }
            else
            {
                Debug.Log("[MCP] Server is STOPPED");
            }
        }

        [MenuItem("Tools/MCP/Set Port...")]
        public static void SetPort()
        {
            var currentPort = EditorPrefs.GetInt(PORT_KEY, DEFAULT_PORT);
            var input = EditorInputDialog.Show("MCP Server Port", "Enter port number:", currentPort.ToString());
            if (!string.IsNullOrEmpty(input) && int.TryParse(input, out var newPort))
            {
                EditorPrefs.SetInt(PORT_KEY, newPort);
                Debug.Log($"[MCP] Port set to {newPort}. Restart server for changes to take effect.");
            }
        }

        [MenuItem("Tools/MCP/Configure Claude")]
        public static void ConfigureClaude()
        {
            var userProfile = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            var claudeDir = System.IO.Path.Combine(userProfile, ".claude");
            var bridgeScriptDest = System.IO.Path.Combine(claudeDir, "mcp-bridge-unity.js");
            var settingsPath = System.IO.Path.Combine(claudeDir, "settings.json");

            // Find the bridge script in the project
            var bridgeScriptSrc = FindBridgeScript();
            if (string.IsNullOrEmpty(bridgeScriptSrc))
            {
                Debug.LogError("[MCP] Could not find mcp-bridge-unity.js in project");
                return;
            }

            // Ensure .claude directory exists
            if (!System.IO.Directory.Exists(claudeDir))
            {
                System.IO.Directory.CreateDirectory(claudeDir);
            }

            // Copy bridge script
            System.IO.File.Copy(bridgeScriptSrc, bridgeScriptDest, overwrite: true);
            Debug.Log($"[MCP] Copied bridge script to {bridgeScriptDest}");

            // Update settings.json
            var existingJson = "{}";
            if (System.IO.File.Exists(settingsPath))
            {
                existingJson = System.IO.File.ReadAllText(settingsPath);
            }

            var newJson = UpdateClaudeSettings(existingJson, bridgeScriptDest);
            System.IO.File.WriteAllText(settingsPath, newJson);

            Debug.Log($"[MCP] Claude configured! MCP server 'unity' added to {settingsPath}");
            Debug.Log("[MCP] Restart Claude Code for changes to take effect.");
        }

        private static string FindBridgeScript()
        {
            // Check common locations
            var projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath);
            var candidates = new[]
            {
                System.IO.Path.Combine(projectRoot, "mcp-unity", "mcp-bridge.js"),
                System.IO.Path.Combine(projectRoot, "mcp-unity", "mcp-bridge-unity.js"),
                System.IO.Path.Combine(projectRoot, "mcp-bridge.js"),
                System.IO.Path.Combine(Application.dataPath, "MCP", "mcp-bridge.js"),
                System.IO.Path.Combine(Application.dataPath, "Editor", "MCP", "mcp-bridge.js"),
            };

            foreach (var path in candidates)
            {
                if (System.IO.File.Exists(path))
                    return path;
            }

            // Search in Assets
            var guids = AssetDatabase.FindAssets("mcp-bridge");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".js"))
                {
                    var fullPath = System.IO.Path.Combine(projectRoot, path);
                    if (System.IO.File.Exists(fullPath))
                        return fullPath;
                }
            }

            return null;
        }

        private static string UpdateClaudeSettings(string existingJson, string bridgeScriptPath)
        {
            // Parse existing JSON to preserve other settings
            existingJson = existingJson?.Trim() ?? "{}";

            // Build the unity MCP server entry
            var unityEntry = $@"{{
      ""command"": ""node"",
      ""args"": [""{bridgeScriptPath.Replace("\\", "/")}""]
    }}";

            // Check if mcpServers already exists
            if (existingJson.Contains("\"mcpServers\""))
            {
                // Check if unity entry already exists
                if (existingJson.Contains("\"unity\""))
                {
                    // Replace existing unity entry - find and replace the unity block
                    var unityPattern = new System.Text.RegularExpressions.Regex(
                        @"""unity""\s*:\s*\{[^}]*\}",
                        System.Text.RegularExpressions.RegexOptions.Singleline);
                    existingJson = unityPattern.Replace(existingJson, $"\"unity\": {unityEntry}");
                }
                else
                {
                    // Add unity to existing mcpServers
                    var mcpPattern = new System.Text.RegularExpressions.Regex(
                        @"(""mcpServers""\s*:\s*\{)",
                        System.Text.RegularExpressions.RegexOptions.Singleline);
                    existingJson = mcpPattern.Replace(existingJson, $"$1\n    \"unity\": {unityEntry},");
                }
            }
            else
            {
                // Add mcpServers section
                if (existingJson == "{}")
                {
                    existingJson = $@"{{
  ""mcpServers"": {{
    ""unity"": {unityEntry}
  }}
}}";
                }
                else
                {
                    // Insert before closing brace
                    var lastBrace = existingJson.LastIndexOf('}');
                    var prefix = existingJson.Substring(0, lastBrace).TrimEnd();
                    if (!prefix.EndsWith("{"))
                        prefix += ",";
                    existingJson = prefix + $@"
  ""mcpServers"": {{
    ""unity"": {unityEntry}
  }}
}}";
                }
            }

            return existingJson;
        }
    }

    public class EditorInputDialog : EditorWindow
    {
        private string _input = "";
        private string _message = "";
        private bool _shouldClose;
        private bool _confirmed;

        public static string Show(string title, string message, string defaultValue = "")
        {
            var window = CreateInstance<EditorInputDialog>();
            window.titleContent = new GUIContent(title);
            window._message = message;
            window._input = defaultValue;
            window.minSize = new Vector2(300, 100);
            window.maxSize = new Vector2(300, 100);
            window.ShowModalUtility();

            return window._confirmed ? window._input : null;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(_message);
            _input = EditorGUILayout.TextField(_input);
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("OK"))
            {
                _confirmed = true;
                _shouldClose = true;
            }
            if (GUILayout.Button("Cancel"))
            {
                _shouldClose = true;
            }
            EditorGUILayout.EndHorizontal();

            if (_shouldClose)
                Close();
        }
    }
}
