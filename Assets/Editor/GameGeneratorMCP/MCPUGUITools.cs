using System;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace GameGenerator.MCP
{
    public static class MCPUGUITools
    {
        [Serializable]
        private class HierarchyArgs
        {
            public string canvasName;
            public int maxDepth = 10;
        }

        [Serializable]
        private class QueryArgs
        {
            public string name;
            public string exactName;
            public string tag;
            public string componentType;
            public int limit = 20;
        }

        [Serializable]
        private class PathArgs
        {
            public string path;
        }

        public static string GetHierarchy(string argsJson)
        {
            var args = MCPHelpers.ParseArgs<HierarchyArgs>(argsJson);
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);

            var sb = new StringBuilder();
            sb.Append("{\"canvases\":[");

            bool first = true;
            foreach (var canvas in canvases)
            {
                if (canvas.transform.parent != null && canvas.transform.parent.GetComponent<Canvas>() != null)
                    continue;

                if (!string.IsNullOrEmpty(args.canvasName) && canvas.name != args.canvasName)
                    continue;

                if (!first) sb.Append(",");
                first = false;

                sb.Append($"{{\"name\":\"{MCPHelpers.EscapeJson(canvas.name)}\",\"renderMode\":\"{canvas.renderMode}\",\"sortingOrder\":{canvas.sortingOrder},\"elements\":[");
                AppendUGUIElement(sb, canvas.transform, 0, args.maxDepth, true, canvas);
                sb.Append("]}");
            }

            sb.Append("]}");
            return sb.ToString();
        }

        private static void AppendUGUIElement(StringBuilder sb, Transform t, int depth, int maxDepth, bool isFirst, Canvas rootCanvas)
        {
            if (depth > maxDepth) return;

            if (!isFirst) sb.Append(",");

            var go = t.gameObject;
            sb.Append($"{{\"name\":\"{MCPHelpers.EscapeJson(go.name)}\",\"active\":{go.activeSelf.ToString().ToLower()},\"path\":\"{MCPHelpers.EscapeJson(MCPHelpers.GetGameObjectPath(go))}\"");

            AppendComponentInfo(sb, go);
            AppendScreenPosition(sb, go, rootCanvas);

            if (t.childCount > 0 && depth < maxDepth)
            {
                sb.Append(",\"children\":[");
                for (int i = 0; i < t.childCount; i++)
                {
                    AppendUGUIElement(sb, t.GetChild(i), depth + 1, maxDepth, i == 0, rootCanvas);
                }
                sb.Append("]");
            }

            sb.Append("}");
        }

        private static void AppendComponentInfo(StringBuilder sb, GameObject go)
        {
            var button = go.GetComponent<Button>();
            if (button != null)
            {
                sb.Append($",\"type\":\"Button\",\"interactable\":{button.interactable.ToString().ToLower()}");
                return;
            }

            var tmpText = go.GetComponent<TMPro.TMP_Text>();
            if (tmpText != null)
            {
                sb.Append($",\"type\":\"TMP_Text\",\"text\":\"{MCPHelpers.EscapeJson(tmpText.text)}\"");
                return;
            }

            var text = go.GetComponent<Text>();
            if (text != null)
            {
                sb.Append($",\"type\":\"Text\",\"text\":\"{MCPHelpers.EscapeJson(text.text)}\"");
                return;
            }

            var tmpInputField = go.GetComponent<TMPro.TMP_InputField>();
            if (tmpInputField != null)
            {
                sb.Append($",\"type\":\"TMP_InputField\",\"text\":\"{MCPHelpers.EscapeJson(tmpInputField.text)}\"");
                return;
            }

            var inputField = go.GetComponent<InputField>();
            if (inputField != null)
            {
                sb.Append($",\"type\":\"InputField\",\"text\":\"{MCPHelpers.EscapeJson(inputField.text)}\"");
                return;
            }

            var toggle = go.GetComponent<Toggle>();
            if (toggle != null)
            {
                sb.Append($",\"type\":\"Toggle\",\"isOn\":{toggle.isOn.ToString().ToLower()},\"interactable\":{toggle.interactable.ToString().ToLower()}");
                return;
            }

            var slider = go.GetComponent<Slider>();
            if (slider != null)
            {
                sb.Append($",\"type\":\"Slider\",\"value\":{slider.value.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"interactable\":{slider.interactable.ToString().ToLower()}");
                return;
            }

            var dropdown = go.GetComponent<TMPro.TMP_Dropdown>();
            if (dropdown != null)
            {
                sb.Append($",\"type\":\"TMP_Dropdown\",\"value\":{dropdown.value},\"interactable\":{dropdown.interactable.ToString().ToLower()}");
                return;
            }

            var legacyDropdown = go.GetComponent<Dropdown>();
            if (legacyDropdown != null)
            {
                sb.Append($",\"type\":\"Dropdown\",\"value\":{legacyDropdown.value},\"interactable\":{legacyDropdown.interactable.ToString().ToLower()}");
                return;
            }

            var scrollRect = go.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                sb.Append($",\"type\":\"ScrollRect\"");
                return;
            }

            var image = go.GetComponent<Image>();
            if (image != null)
            {
                sb.Append($",\"type\":\"Image\",\"raycastTarget\":{image.raycastTarget.ToString().ToLower()}");
                return;
            }

            var rawImage = go.GetComponent<RawImage>();
            if (rawImage != null)
            {
                sb.Append($",\"type\":\"RawImage\",\"raycastTarget\":{rawImage.raycastTarget.ToString().ToLower()}");
                return;
            }
        }

        private static void AppendScreenPosition(StringBuilder sb, GameObject go, Canvas rootCanvas)
        {
            var rectTransform = go.GetComponent<RectTransform>();
            if (rectTransform == null) return;

            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

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
                    screenPoint = CoordinateHelper.UnityToScreen(sp);
                }
                else
                {
                    screenPoint = CoordinateHelper.UnityToScreen(corners[i].x, corners[i].y);
                }

                minX = Mathf.Min(minX, screenPoint.x);
                maxX = Mathf.Max(maxX, screenPoint.x);
                minY = Mathf.Min(minY, screenPoint.y);
                maxY = Mathf.Max(maxY, screenPoint.y);
            }

            var centerX = (minX + maxX) / 2;
            var centerY = (minY + maxY) / 2;

            sb.Append($",\"screenPos\":{{\"x\":{centerX.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{centerY.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}");
        }

        public static string QueryElement(string argsJson)
        {
            var args = MCPHelpers.ParseArgs<QueryArgs>(argsJson);
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);

            var sb = new StringBuilder();
            sb.Append("{\"results\":[");

            int count = 0;
            bool first = true;

            foreach (var canvas in canvases)
            {
                if (canvas.transform.parent != null && canvas.transform.parent.GetComponent<Canvas>() != null)
                    continue;

                SearchInTransform(canvas.transform, args, sb, ref count, ref first, canvas);
                if (count >= args.limit) break;
            }

            sb.Append($"],\"count\":{count}}}");
            return sb.ToString();
        }

        private static void SearchInTransform(Transform t, QueryArgs args, StringBuilder sb, ref int count, ref bool first, Canvas rootCanvas)
        {
            if (count >= args.limit) return;

            var go = t.gameObject;
            bool matches = true;

            if (!string.IsNullOrEmpty(args.exactName))
            {
                matches = go.name == args.exactName;
            }
            else if (!string.IsNullOrEmpty(args.name))
            {
                matches = go.name.IndexOf(args.name, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (matches && !string.IsNullOrEmpty(args.tag))
            {
                matches = go.CompareTag(args.tag);
            }

            if (matches && !string.IsNullOrEmpty(args.componentType))
            {
                matches = HasComponentType(go, args.componentType);
            }

            if (matches && (!string.IsNullOrEmpty(args.name) || !string.IsNullOrEmpty(args.exactName) ||
                            !string.IsNullOrEmpty(args.tag) || !string.IsNullOrEmpty(args.componentType)))
            {
                if (!first) sb.Append(",");
                first = false;

                sb.Append($"{{\"name\":\"{MCPHelpers.EscapeJson(go.name)}\",\"path\":\"{MCPHelpers.EscapeJson(MCPHelpers.GetGameObjectPath(go))}\"");
                AppendComponentInfo(sb, go);
                AppendScreenPosition(sb, go, rootCanvas);
                sb.Append("}");
                count++;
            }

            for (int i = 0; i < t.childCount && count < args.limit; i++)
            {
                SearchInTransform(t.GetChild(i), args, sb, ref count, ref first, rootCanvas);
            }
        }

        private static bool HasComponentType(GameObject go, string componentType)
        {
            switch (componentType.ToLower())
            {
                case "button":
                    return go.GetComponent<Button>() != null;
                case "text":
                    return go.GetComponent<Text>() != null || go.GetComponent<TMPro.TMP_Text>() != null;
                case "tmp_text":
                    return go.GetComponent<TMPro.TMP_Text>() != null;
                case "inputfield":
                    return go.GetComponent<InputField>() != null || go.GetComponent<TMPro.TMP_InputField>() != null;
                case "tmp_inputfield":
                    return go.GetComponent<TMPro.TMP_InputField>() != null;
                case "image":
                    return go.GetComponent<Image>() != null;
                case "rawimage":
                    return go.GetComponent<RawImage>() != null;
                case "toggle":
                    return go.GetComponent<Toggle>() != null;
                case "slider":
                    return go.GetComponent<Slider>() != null;
                case "dropdown":
                    return go.GetComponent<Dropdown>() != null || go.GetComponent<TMPro.TMP_Dropdown>() != null;
                case "scrollrect":
                    return go.GetComponent<ScrollRect>() != null;
                default:
                    return MCPHelpers.FindComponent(go, componentType) != null;
            }
        }

        public static string GetElementInfo(string argsJson)
        {
            var args = MCPHelpers.ParseArgs<PathArgs>(argsJson);
            var go = MCPHelpers.FindGameObject(args.path);

            if (go == null)
                return $"{{\"error\":\"Element not found: {MCPHelpers.EscapeJson(args.path)}\"}}";

            var sb = new StringBuilder();
            sb.Append($"{{\"name\":\"{MCPHelpers.EscapeJson(go.name)}\",\"path\":\"{MCPHelpers.EscapeJson(MCPHelpers.GetGameObjectPath(go))}\",\"active\":{go.activeInHierarchy.ToString().ToLower()}");

            var rectTransform = go.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                var canvas = rectTransform.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    AppendScreenPosition(sb, go, canvas.rootCanvas);
                    AppendDetailedRectInfo(sb, rectTransform, canvas.rootCanvas);
                }
            }

            AppendDetailedComponentInfo(sb, go);

            sb.Append("}");
            return sb.ToString();
        }

        private static void AppendDetailedRectInfo(StringBuilder sb, RectTransform rectTransform, Canvas rootCanvas)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

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
                    screenPoint = CoordinateHelper.UnityToScreen(sp);
                }
                else
                {
                    screenPoint = CoordinateHelper.UnityToScreen(corners[i].x, corners[i].y);
                }

                minX = Mathf.Min(minX, screenPoint.x);
                maxX = Mathf.Max(maxX, screenPoint.x);
                minY = Mathf.Min(minY, screenPoint.y);
                maxY = Mathf.Max(maxY, screenPoint.y);
            }

            sb.Append($",\"screenRect\":{{\"x\":{minX.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{minY.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"width\":{(maxX - minX).ToString(System.Globalization.CultureInfo.InvariantCulture)},\"height\":{(maxY - minY).ToString(System.Globalization.CultureInfo.InvariantCulture)}}}");
        }

        private static void AppendDetailedComponentInfo(StringBuilder sb, GameObject go)
        {
            var button = go.GetComponent<Button>();
            if (button != null)
            {
                sb.Append($",\"type\":\"Button\",\"interactable\":{button.interactable.ToString().ToLower()}");
                var buttonText = button.GetComponentInChildren<TMPro.TMP_Text>();
                if (buttonText != null)
                {
                    sb.Append($",\"buttonText\":\"{MCPHelpers.EscapeJson(buttonText.text)}\"");
                }
                else
                {
                    var legacyText = button.GetComponentInChildren<Text>();
                    if (legacyText != null)
                        sb.Append($",\"buttonText\":\"{MCPHelpers.EscapeJson(legacyText.text)}\"");
                }
                return;
            }

            var tmpText = go.GetComponent<TMPro.TMP_Text>();
            if (tmpText != null)
            {
                sb.Append($",\"type\":\"TMP_Text\",\"text\":\"{MCPHelpers.EscapeJson(tmpText.text)}\",\"fontSize\":{tmpText.fontSize},\"color\":{MCPHelpers.SerializeValue(tmpText.color)}");
                return;
            }

            var text = go.GetComponent<Text>();
            if (text != null)
            {
                sb.Append($",\"type\":\"Text\",\"text\":\"{MCPHelpers.EscapeJson(text.text)}\",\"fontSize\":{text.fontSize},\"color\":{MCPHelpers.SerializeValue(text.color)}");
                return;
            }

            var tmpInputField = go.GetComponent<TMPro.TMP_InputField>();
            if (tmpInputField != null)
            {
                sb.Append($",\"type\":\"TMP_InputField\",\"text\":\"{MCPHelpers.EscapeJson(tmpInputField.text)}\",\"placeholder\":\"{MCPHelpers.EscapeJson(tmpInputField.placeholder?.GetComponent<TMPro.TMP_Text>()?.text ?? "")}\",\"interactable\":{tmpInputField.interactable.ToString().ToLower()}");
                return;
            }

            var inputField = go.GetComponent<InputField>();
            if (inputField != null)
            {
                sb.Append($",\"type\":\"InputField\",\"text\":\"{MCPHelpers.EscapeJson(inputField.text)}\",\"placeholder\":\"{MCPHelpers.EscapeJson(inputField.placeholder?.GetComponent<Text>()?.text ?? "")}\",\"interactable\":{inputField.interactable.ToString().ToLower()}");
                return;
            }

            var toggle = go.GetComponent<Toggle>();
            if (toggle != null)
            {
                sb.Append($",\"type\":\"Toggle\",\"isOn\":{toggle.isOn.ToString().ToLower()},\"interactable\":{toggle.interactable.ToString().ToLower()}");
                return;
            }

            var slider = go.GetComponent<Slider>();
            if (slider != null)
            {
                sb.Append($",\"type\":\"Slider\",\"value\":{slider.value.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"minValue\":{slider.minValue.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"maxValue\":{slider.maxValue.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"interactable\":{slider.interactable.ToString().ToLower()}");
                return;
            }

            var dropdown = go.GetComponent<TMPro.TMP_Dropdown>();
            if (dropdown != null)
            {
                sb.Append($",\"type\":\"TMP_Dropdown\",\"value\":{dropdown.value},\"interactable\":{dropdown.interactable.ToString().ToLower()},\"options\":[");
                for (int i = 0; i < dropdown.options.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append($"\"{MCPHelpers.EscapeJson(dropdown.options[i].text)}\"");
                }
                sb.Append("]");
                return;
            }

            var image = go.GetComponent<Image>();
            if (image != null)
            {
                sb.Append($",\"type\":\"Image\",\"raycastTarget\":{image.raycastTarget.ToString().ToLower()},\"color\":{MCPHelpers.SerializeValue(image.color)}");
                if (image.sprite != null)
                    sb.Append($",\"sprite\":\"{MCPHelpers.EscapeJson(image.sprite.name)}\"");
                return;
            }
        }

        public static string ClickElement(string argsJson)
        {
            if (!Application.isPlaying)
                return "{\"error\":\"Play Mode required\"}";

            var args = MCPHelpers.ParseArgs<PathArgs>(argsJson);
            var go = MCPHelpers.FindGameObject(args.path);

            if (go == null)
                return $"{{\"error\":\"Element not found: {MCPHelpers.EscapeJson(args.path)}\"}}";

            var button = go.GetComponent<Button>();
            if (button != null)
            {
                if (!button.interactable)
                    return "{\"error\":\"Button not interactable\"}";

                button.onClick.Invoke();
                return $"{{\"success\":true,\"clicked\":\"{MCPHelpers.EscapeJson(go.name)}\",\"type\":\"Button\"}}";
            }

            var toggle = go.GetComponent<Toggle>();
            if (toggle != null)
            {
                if (!toggle.interactable)
                    return "{\"error\":\"Toggle not interactable\"}";

                toggle.isOn = !toggle.isOn;
                return $"{{\"success\":true,\"clicked\":\"{MCPHelpers.EscapeJson(go.name)}\",\"type\":\"Toggle\",\"isOn\":{toggle.isOn.ToString().ToLower()}}}";
            }

            var rectTransform = go.GetComponent<RectTransform>();
            if (rectTransform == null)
                return "{\"error\":\"Not a UI element\"}";

            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            var center = (corners[0] + corners[2]) / 2;

            var pointerData = new PointerEventData(EventSystem.current)
            {
                position = new Vector2(center.x, center.y)
            };

            ExecuteEvents.Execute(go, pointerData, ExecuteEvents.pointerClickHandler);
            return $"{{\"success\":true,\"clicked\":\"{MCPHelpers.EscapeJson(go.name)}\"}}";
        }
    }
}
