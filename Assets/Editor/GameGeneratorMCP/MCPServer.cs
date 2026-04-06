using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

namespace GameGenerator.MCP
{
    public class MCPServer
    {
        private static MCPServer _instance;
        public static MCPServer Instance => _instance ??= new MCPServer();

        private HttpListener _listener;
        private Thread _listenerThread;
        private bool _isRunning;
        private readonly object _lock = new object();

        public int Port { get; private set; } = 9876;
        public bool IsRunning => _isRunning;

        private readonly Queue<Action> _mainThreadQueue = new Queue<Action>();

        public void Start(int port = 9876)
        {
            if (_isRunning) return;

            Port = port;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");

            try
            {
                _listener.Start();
                _isRunning = true;

                _listenerThread = new Thread(ListenLoop) { IsBackground = true };
                _listenerThread.Start();

                Debug.Log($"[MCP] Server started on port {port}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Failed to start server: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _listener?.Stop();
            _listener?.Close();
            _listenerThread?.Join(1000);

            Debug.Log("[MCP] Server stopped");
        }

        public void ProcessMainThreadQueue()
        {
            lock (_lock)
            {
                while (_mainThreadQueue.Count > 0)
                {
                    var action = _mainThreadQueue.Dequeue();
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[MCP] Main thread action error: {ex.Message}");
                    }
                }
            }
        }

        private void ListenLoop()
        {
            while (_isRunning)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                        Debug.LogError($"[MCP] Listen error: {ex.Message}");
                }
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 200;
                    response.Close();
                    return;
                }

                string responseBody;

                if (request.Url.AbsolutePath == "/health")
                {
                    responseBody = "{\"status\":\"ok\",\"unity\":true}";
                }
                else if (request.Url.AbsolutePath == "/mcp")
                {
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    var body = reader.ReadToEnd();
                    responseBody = ProcessMCPRequest(body);
                }
                else if (request.Url.AbsolutePath == "/tools")
                {
                    responseBody = GetToolsList();
                }
                else
                {
                    response.StatusCode = 404;
                    responseBody = "{\"error\":\"Not found\"}";
                }

                var buffer = Encoding.UTF8.GetBytes(responseBody);
                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Request handling error: {ex.Message}");
            }
        }

        private string ProcessMCPRequest(string body)
        {
            try
            {
                var request = JsonUtility.FromJson<MCPRequest>(body);

                if (string.IsNullOrEmpty(request.tool))
                    return "{\"error\":\"Missing tool name\"}";

                string result = null;
                var waitHandle = new ManualResetEvent(false);
                Exception error = null;

                lock (_lock)
                {
                    _mainThreadQueue.Enqueue(() =>
                    {
                        try
                        {
                            result = MCPTools.ExecuteTool(request.tool, request.args);
                        }
                        catch (Exception ex)
                        {
                            error = ex;
                        }
                        finally
                        {
                            waitHandle.Set();
                        }
                    });
                }

                if (!waitHandle.WaitOne(30000))
                    return "{\"error\":\"Tool execution timed out\"}";

                if (error != null)
                    return $"{{\"error\":\"{EscapeJson(error.Message)}\"}}";

                return result ?? "{\"error\":\"No result\"}";
            }
            catch (Exception ex)
            {
                return $"{{\"error\":\"{EscapeJson(ex.Message)}\"}}";
            }
        }

        private string GetToolsList()
        {
            var tools = new[]
            {
                new { name = "execute_menu_item", description = "Execute a Unity menu item by path", parameters = new[] { "menuPath" } },
                new { name = "get_hierarchy", description = "Get scene hierarchy as JSON", parameters = new string[0] },
                new { name = "get_selection", description = "Get currently selected objects", parameters = new string[0] },
                new { name = "select_gameobject", description = "Select a GameObject by path", parameters = new[] { "path" } },
                new { name = "get_console_logs", description = "Get recent console logs", parameters = new[] { "count" } },
                new { name = "clear_console", description = "Clear the console", parameters = new string[0] },
                new { name = "get_project_info", description = "Get project information", parameters = new string[0] },
                new { name = "play", description = "Enter play mode", parameters = new string[0] },
                new { name = "stop", description = "Exit play mode", parameters = new string[0] },
                new { name = "pause", description = "Toggle pause", parameters = new string[0] },
                new { name = "refresh_assets", description = "Refresh asset database", parameters = new string[0] },
                new { name = "compile", description = "Request script compilation", parameters = new string[0] },
                new { name = "ping", description = "Check if server is responsive", parameters = new string[0] }
            };

            var sb = new StringBuilder();
            sb.Append("{\"tools\":[");
            for (int i = 0; i < tools.Length; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append($"{{\"name\":\"{tools[i].name}\",\"description\":\"{tools[i].description}\"}}");
            }
            sb.Append("]}");
            return sb.ToString();
        }

        private static string EscapeJson(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }

    [Serializable]
    public class MCPRequest
    {
        public string tool;
        public string args;
    }
}
