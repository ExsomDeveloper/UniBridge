using System;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;

namespace GameGenerator.MCP
{
    public static class MCPPositionTools
    {
        [Serializable]
        private class PathArgs
        {
            public string path;
        }

        [Serializable]
        private class ScreenPositionArgs
        {
            public string path;
            public string point = "center";
        }

        [Serializable]
        private class RaycastArgs
        {
            public float x;
            public float y;
            public bool includeUI = true;
            public bool include3D = true;
            public float maxDistance = 1000f;
        }

        public static string GetScreenPosition(string argsJson)
        {
            var args = MCPHelpers.ParseArgs<ScreenPositionArgs>(argsJson);
            var go = MCPHelpers.FindGameObject(args.path);

            if (go == null)
                return $"{{\"error\":\"GameObject not found: {MCPHelpers.EscapeJson(args.path)}\"}}";

            var rectTransform = go.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                return GetUIScreenPosition(rectTransform, args.point);
            }

            var camera = Camera.main;
            if (camera == null)
                camera = Camera.current;
            if (camera == null)
                return "{\"error\":\"No camera found\"}";

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && args.point == "bounds")
            {
                var bounds = renderer.bounds;
                var screenMin = CoordinateHelper.UnityToScreen(camera.WorldToScreenPoint(bounds.min));
                var screenMax = CoordinateHelper.UnityToScreen(camera.WorldToScreenPoint(bounds.max));
                var minY = Mathf.Min(screenMin.y, screenMax.y);
                var maxY = Mathf.Max(screenMin.y, screenMax.y);
                return $"{{\"min\":{{\"x\":{screenMin.x.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{minY.ToString(System.Globalization.CultureInfo.InvariantCulture)}}},\"max\":{{\"x\":{screenMax.x.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{maxY.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}}}";
            }

            var worldPos = go.transform.position;
            var screenPos = camera.WorldToScreenPoint(worldPos);
            var converted = CoordinateHelper.UnityToScreen(screenPos);
            var visible = screenPos.z > 0 && converted.x >= 0 && converted.x <= Screen.width && converted.y >= 0 && converted.y <= Screen.height;

            return $"{{\"x\":{converted.x.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{converted.y.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"visible\":{visible.ToString().ToLower()}}}";
        }

        private static string GetUIScreenPosition(RectTransform rectTransform, string point)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            var canvas = rectTransform.GetComponentInParent<Canvas>();
            if (canvas == null)
                return "{\"error\":\"No parent Canvas\"}";

            var rootCanvas = canvas.rootCanvas;
            Camera cam = null;

            if (rootCanvas.renderMode == RenderMode.ScreenSpaceCamera ||
                rootCanvas.renderMode == RenderMode.WorldSpace)
            {
                cam = rootCanvas.worldCamera;
            }

            var screenCorners = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                if (cam != null)
                {
                    var screenPoint = cam.WorldToScreenPoint(corners[i]);
                    screenCorners[i] = CoordinateHelper.UnityToScreen(screenPoint);
                }
                else
                {
                    screenCorners[i] = CoordinateHelper.UnityToScreen(corners[i].x, corners[i].y);
                }
            }

            float minX = Mathf.Min(screenCorners[0].x, screenCorners[1].x, screenCorners[2].x, screenCorners[3].x);
            float maxX = Mathf.Max(screenCorners[0].x, screenCorners[1].x, screenCorners[2].x, screenCorners[3].x);
            float minY = Mathf.Min(screenCorners[0].y, screenCorners[1].y, screenCorners[2].y, screenCorners[3].y);
            float maxY = Mathf.Max(screenCorners[0].y, screenCorners[1].y, screenCorners[2].y, screenCorners[3].y);

            var centerX = (minX + maxX) / 2;
            var centerY = (minY + maxY) / 2;

            if (point == "bounds")
            {
                return $"{{\"min\":{{\"x\":{minX.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{minY.ToString(System.Globalization.CultureInfo.InvariantCulture)}}},\"max\":{{\"x\":{maxX.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{maxY.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}}}";
            }

            return $"{{\"x\":{centerX.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{centerY.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"visible\":true}}";
        }

        public static string GetRectScreenRect(string argsJson)
        {
            var args = MCPHelpers.ParseArgs<PathArgs>(argsJson);
            var go = MCPHelpers.FindGameObject(args.path);

            if (go == null)
                return $"{{\"error\":\"GameObject not found: {MCPHelpers.EscapeJson(args.path)}\"}}";

            var rectTransform = go.GetComponent<RectTransform>();
            if (rectTransform == null)
                return "{\"error\":\"No RectTransform component\"}";

            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            var canvas = rectTransform.GetComponentInParent<Canvas>();
            if (canvas == null)
                return "{\"error\":\"No parent Canvas\"}";

            var rootCanvas = canvas.rootCanvas;
            Camera cam = null;

            if (rootCanvas.renderMode == RenderMode.ScreenSpaceCamera ||
                rootCanvas.renderMode == RenderMode.WorldSpace)
            {
                cam = rootCanvas.worldCamera;
            }

            var screenCorners = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                if (cam != null)
                {
                    var screenPoint = cam.WorldToScreenPoint(corners[i]);
                    screenCorners[i] = CoordinateHelper.UnityToScreen(screenPoint);
                }
                else
                {
                    screenCorners[i] = CoordinateHelper.UnityToScreen(corners[i].x, corners[i].y);
                }
            }

            float minX = Mathf.Min(screenCorners[0].x, screenCorners[1].x, screenCorners[2].x, screenCorners[3].x);
            float maxX = Mathf.Max(screenCorners[0].x, screenCorners[1].x, screenCorners[2].x, screenCorners[3].x);
            float minY = Mathf.Min(screenCorners[0].y, screenCorners[1].y, screenCorners[2].y, screenCorners[3].y);
            float maxY = Mathf.Max(screenCorners[0].y, screenCorners[1].y, screenCorners[2].y, screenCorners[3].y);

            var width = maxX - minX;
            var height = maxY - minY;
            var centerX = (minX + maxX) / 2;
            var centerY = (minY + maxY) / 2;

            return $"{{\"x\":{minX.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{minY.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"width\":{width.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"height\":{height.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"center\":{{\"x\":{centerX.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{centerY.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}}}";
        }

        public static string IsVisibleOnScreen(string argsJson)
        {
            var args = MCPHelpers.ParseArgs<PathArgs>(argsJson);
            var go = MCPHelpers.FindGameObject(args.path);

            if (go == null)
                return $"{{\"error\":\"GameObject not found: {MCPHelpers.EscapeJson(args.path)}\"}}";

            if (!go.activeInHierarchy)
                return "{\"visible\":false,\"reason\":\"inactive\"}";

            var canvasGroup = go.GetComponentInParent<CanvasGroup>();
            if (canvasGroup != null && canvasGroup.alpha <= 0)
                return "{\"visible\":false,\"reason\":\"canvas_group_hidden\"}";

            var rectTransform = go.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                return CheckUIVisibility(rectTransform);
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (!renderer.isVisible)
                    return "{\"visible\":false,\"reason\":\"culled\"}";

                var camera = Camera.main;
                if (camera == null)
                    camera = Camera.current;

                if (camera != null)
                {
                    var screenPos = camera.WorldToScreenPoint(go.transform.position);
                    if (screenPos.z > 0 && screenPos.x >= 0 && screenPos.x <= Screen.width &&
                        screenPos.y >= 0 && screenPos.y <= Screen.height)
                    {
                        return "{\"visible\":true}";
                    }
                    return "{\"visible\":false,\"reason\":\"off_screen\"}";
                }
            }

            return "{\"visible\":false,\"reason\":\"no_renderer_or_rectransform\"}";
        }

        private static string CheckUIVisibility(RectTransform rectTransform)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            var canvas = rectTransform.GetComponentInParent<Canvas>();
            if (canvas == null)
                return "{\"visible\":false,\"reason\":\"no_canvas\"}";

            var rootCanvas = canvas.rootCanvas;
            Camera cam = null;

            if (rootCanvas.renderMode == RenderMode.ScreenSpaceCamera ||
                rootCanvas.renderMode == RenderMode.WorldSpace)
            {
                cam = rootCanvas.worldCamera;
            }

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            for (int i = 0; i < 4; i++)
            {
                Vector2 screenPoint;
                if (cam != null)
                {
                    var sp = cam.WorldToScreenPoint(corners[i]);
                    screenPoint = new Vector2(sp.x, sp.y);
                }
                else
                {
                    screenPoint = new Vector2(corners[i].x, corners[i].y);
                }

                minX = Mathf.Min(minX, screenPoint.x);
                maxX = Mathf.Max(maxX, screenPoint.x);
                minY = Mathf.Min(minY, screenPoint.y);
                maxY = Mathf.Max(maxY, screenPoint.y);
            }

            bool onScreen = maxX >= 0 && minX <= Screen.width && maxY >= 0 && minY <= Screen.height;
            if (!onScreen)
                return "{\"visible\":false,\"reason\":\"off_screen\"}";

            return "{\"visible\":true}";
        }

        public static string RaycastFromScreen(string argsJson)
        {
            var args = MCPHelpers.ParseArgs<RaycastArgs>(argsJson);
            var sb = new StringBuilder();
            sb.Append("{\"hits\":[");

            var unityPos = CoordinateHelper.ScreenToUnity(args.x, args.y);
            bool first = true;

            if (args.includeUI && EventSystem.current != null)
            {
                var pointerData = new PointerEventData(EventSystem.current) { position = unityPos };
                var results = ListPool<RaycastResult>.Get();
                EventSystem.current.RaycastAll(pointerData, results);

                foreach (var result in results)
                {
                    if (!first) sb.Append(",");
                    first = false;
                    sb.Append($"{{\"type\":\"ui\",\"name\":\"{MCPHelpers.EscapeJson(result.gameObject.name)}\",\"path\":\"{MCPHelpers.EscapeJson(MCPHelpers.GetGameObjectPath(result.gameObject))}\",\"depth\":{result.depth}}}");
                }

                ListPool<RaycastResult>.Release(results);
            }

            if (args.include3D)
            {
                var camera = Camera.main;
                if (camera == null)
                    camera = Camera.current;

                if (camera != null)
                {
                    var ray = camera.ScreenPointToRay(unityPos);
                    var hits = Physics.RaycastAll(ray, args.maxDistance);

                    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                    foreach (var hit in hits)
                    {
                        if (!first) sb.Append(",");
                        first = false;
                        sb.Append($"{{\"type\":\"3d\",\"name\":\"{MCPHelpers.EscapeJson(hit.collider.gameObject.name)}\",\"path\":\"{MCPHelpers.EscapeJson(MCPHelpers.GetGameObjectPath(hit.collider.gameObject))}\",\"distance\":{hit.distance.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}");
                    }
                }
            }

            sb.Append("]}");
            return sb.ToString();
        }
    }
}
