using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace GameGenerator.MCP
{
    public static class MCPInputTools
    {
        [Serializable]
        private class ClickGameObjectArgs
        {
            public string path;
            public string button = "left";
        }

        [Serializable]
        private class SendKeyArgs
        {
            public string key;
            public string action = "press";
        }

        [Serializable]
        private class DragArgs
        {
            public float startX;
            public float startY;
            public float endX;
            public float endY;
            public float duration = 0.5f;
            public int steps = 10;
        }

        [Serializable]
        private class InvokeMethodArgs
        {
            public string gameObject;
            public string component;
            public string method;
            public string[] parameters;
        }

        private static bool? _hasNewInputSystem;

        private static bool HasNewInputSystem
        {
            get
            {
                if (!_hasNewInputSystem.HasValue)
                {
                    _hasNewInputSystem = Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem") != null;
                }
                return _hasNewInputSystem.Value;
            }
        }

        /// <summary>
        /// Invokes a method on a component attached to a GameObject.
        /// Can be used to trigger click handlers, custom methods, etc.
        /// </summary>
        public static string InvokeMethod(string argsJson)
        {
            var args = MCPHelpers.ParseArgs<InvokeMethodArgs>(argsJson);

            if (string.IsNullOrEmpty(args.gameObject))
                return "{\"error\":\"gameObject is required\"}";
            if (string.IsNullOrEmpty(args.component))
                return "{\"error\":\"component is required\"}";
            if (string.IsNullOrEmpty(args.method))
                return "{\"error\":\"method is required\"}";

            // Find the GameObject
            var go = MCPHelpers.FindGameObject(args.gameObject);
            if (go == null)
                return $"{{\"error\":\"GameObject not found: {MCPHelpers.EscapeJson(args.gameObject)}\"}}";

            // Find the component
            Component component = null;
            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                var typeName = comp.GetType().Name;
                var fullName = comp.GetType().FullName;
                if (typeName.Equals(args.component, StringComparison.OrdinalIgnoreCase) ||
                    fullName.Equals(args.component, StringComparison.OrdinalIgnoreCase))
                {
                    component = comp;
                    break;
                }
            }

            if (component == null)
                return $"{{\"error\":\"Component not found: {MCPHelpers.EscapeJson(args.component)} on {MCPHelpers.EscapeJson(args.gameObject)}\"}}";

            // Find the method
            var componentType = component.GetType();
            var methods = componentType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo targetMethod = null;

            foreach (var m in methods)
            {
                if (m.Name.Equals(args.method, StringComparison.OrdinalIgnoreCase))
                {
                    var paramCount = m.GetParameters().Length;
                    var providedCount = args.parameters?.Length ?? 0;

                    if (paramCount == providedCount)
                    {
                        targetMethod = m;
                        break;
                    }
                    // Keep looking for better match, but store this as fallback
                    if (targetMethod == null)
                        targetMethod = m;
                }
            }

            if (targetMethod == null)
                return $"{{\"error\":\"Method not found: {MCPHelpers.EscapeJson(args.method)} on {MCPHelpers.EscapeJson(args.component)}\"}}";

            // Parse and convert parameters
            var methodParams = targetMethod.GetParameters();
            object[] invokeParams = null;

            if (methodParams.Length > 0)
            {
                if (args.parameters == null || args.parameters.Length < methodParams.Length)
                {
                    // Check if remaining params have defaults
                    var providedCount = args.parameters?.Length ?? 0;
                    var requiredCount = 0;
                    foreach (var p in methodParams)
                    {
                        if (!p.HasDefaultValue) requiredCount++;
                    }

                    if (providedCount < requiredCount)
                        return $"{{\"error\":\"Method requires {requiredCount} parameters, but {providedCount} provided\"}}";
                }

                invokeParams = new object[methodParams.Length];
                for (int i = 0; i < methodParams.Length; i++)
                {
                    var param = methodParams[i];
                    if (args.parameters != null && i < args.parameters.Length)
                    {
                        var parsed = ParseParameter(args.parameters[i], param.ParameterType);
                        if (parsed.error != null)
                            return $"{{\"error\":\"Parameter {i} ({param.Name}): {MCPHelpers.EscapeJson(parsed.error)}\"}}";
                        invokeParams[i] = parsed.value;
                    }
                    else if (param.HasDefaultValue)
                    {
                        invokeParams[i] = param.DefaultValue;
                    }
                    else
                    {
                        return $"{{\"error\":\"Missing required parameter: {param.Name}\"}}";
                    }
                }
            }

            // Invoke the method
            try
            {
                var result = targetMethod.Invoke(component, invokeParams);

                var sb = new System.Text.StringBuilder();
                sb.Append("{\"success\":true");
                sb.Append($",\"gameObject\":\"{MCPHelpers.EscapeJson(go.name)}\"");
                sb.Append($",\"component\":\"{MCPHelpers.EscapeJson(componentType.Name)}\"");
                sb.Append($",\"method\":\"{MCPHelpers.EscapeJson(targetMethod.Name)}\"");

                if (targetMethod.ReturnType != typeof(void) && result != null)
                {
                    sb.Append($",\"returnValue\":{MCPHelpers.SerializeValue(result)}");
                    sb.Append($",\"returnType\":\"{targetMethod.ReturnType.Name}\"");
                }

                sb.Append("}");
                return sb.ToString();
            }
            catch (TargetInvocationException ex)
            {
                var inner = ex.InnerException ?? ex;
                return $"{{\"error\":\"Method threw exception: {MCPHelpers.EscapeJson(inner.Message)}\"}}";
            }
            catch (Exception ex)
            {
                return $"{{\"error\":\"Failed to invoke method: {MCPHelpers.EscapeJson(ex.Message)}\"}}";
            }
        }

        private static (object value, string error) ParseParameter(string paramStr, Type targetType)
        {
            if (string.IsNullOrEmpty(paramStr) || paramStr == "null")
            {
                if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                    return (null, $"Cannot pass null to value type {targetType.Name}");
                return (null, null);
            }

            try
            {
                // Handle common types
                if (targetType == typeof(string))
                    return (paramStr, null);

                if (targetType == typeof(int))
                    return (int.Parse(paramStr, CultureInfo.InvariantCulture), null);

                if (targetType == typeof(float))
                    return (float.Parse(paramStr, CultureInfo.InvariantCulture), null);

                if (targetType == typeof(double))
                    return (double.Parse(paramStr, CultureInfo.InvariantCulture), null);

                if (targetType == typeof(bool))
                    return (bool.Parse(paramStr), null);

                if (targetType == typeof(long))
                    return (long.Parse(paramStr, CultureInfo.InvariantCulture), null);

                // Handle Vector2 - expects "x,y" or JSON format
                if (targetType == typeof(Vector2))
                {
                    if (paramStr.Contains(","))
                    {
                        var parts = paramStr.Split(',');
                        if (parts.Length >= 2)
                        {
                            return (new Vector2(
                                float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                                float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture)
                            ), null);
                        }
                    }
                    return (null, "Vector2 format: x,y");
                }

                // Handle Vector3 - expects "x,y,z" format
                if (targetType == typeof(Vector3))
                {
                    if (paramStr.Contains(","))
                    {
                        var parts = paramStr.Split(',');
                        if (parts.Length >= 3)
                        {
                            return (new Vector3(
                                float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                                float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                                float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture)
                            ), null);
                        }
                    }
                    return (null, "Vector3 format: x,y,z");
                }

                // Handle Color - expects "r,g,b" or "r,g,b,a" format (0-1 range)
                if (targetType == typeof(Color))
                {
                    if (paramStr.Contains(","))
                    {
                        var parts = paramStr.Split(',');
                        if (parts.Length >= 3)
                        {
                            var r = float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture);
                            var g = float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture);
                            var b = float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture);
                            var a = parts.Length >= 4 ? float.Parse(parts[3].Trim(), CultureInfo.InvariantCulture) : 1f;
                            return (new Color(r, g, b, a), null);
                        }
                    }
                    return (null, "Color format: r,g,b or r,g,b,a (0-1 range)");
                }

                // Handle enums
                if (targetType.IsEnum)
                {
                    if (Enum.TryParse(targetType, paramStr, true, out var enumValue))
                        return (enumValue, null);
                    return (null, $"Invalid enum value. Valid values: {string.Join(", ", Enum.GetNames(targetType))}");
                }

                // Handle GameObject reference by name/path
                if (targetType == typeof(GameObject))
                {
                    var foundGo = MCPHelpers.FindGameObject(paramStr);
                    if (foundGo != null)
                        return (foundGo, null);
                    return (null, $"GameObject not found: {paramStr}");
                }

                // Handle Component reference - find by path/name and component type
                if (typeof(Component).IsAssignableFrom(targetType))
                {
                    var foundGo = MCPHelpers.FindGameObject(paramStr);
                    if (foundGo != null)
                    {
                        var foundComp = foundGo.GetComponent(targetType);
                        if (foundComp != null)
                            return (foundComp, null);
                        return (null, $"Component {targetType.Name} not found on {paramStr}");
                    }
                    return (null, $"GameObject not found: {paramStr}");
                }

                // Fallback to Convert
                return (Convert.ChangeType(paramStr, targetType, CultureInfo.InvariantCulture), null);
            }
            catch (Exception ex)
            {
                return (null, $"Failed to parse as {targetType.Name}: {ex.Message}");
            }
        }

        public static string ClickGameObject(string argsJson)
        {
            if (!Application.isPlaying)
                return "{\"error\":\"Play Mode required\"}";

            var args = MCPHelpers.ParseArgs<ClickGameObjectArgs>(argsJson);
            var go = MCPHelpers.FindGameObject(args.path);

            if (go == null)
                return $"{{\"error\":\"GameObject not found: {MCPHelpers.EscapeJson(args.path)}\"}}";

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
            if (rectTransform != null)
            {
                var corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);
                var center = (corners[0] + corners[2]) / 2;

                var pointerData = new PointerEventData(EventSystem.current)
                {
                    position = new Vector2(center.x, center.y),
                    button = GetPointerButton(args.button)
                };

                ExecuteEvents.Execute(go, pointerData, ExecuteEvents.pointerEnterHandler);
                ExecuteEvents.Execute(go, pointerData, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(go, pointerData, ExecuteEvents.pointerClickHandler);
                ExecuteEvents.Execute(go, pointerData, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(go, pointerData, ExecuteEvents.pointerExitHandler);

                return $"{{\"success\":true,\"clicked\":\"{MCPHelpers.EscapeJson(go.name)}\",\"type\":\"UI\"}}";
            }

            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                var camera = Camera.main;
                if (camera == null)
                    camera = Camera.current;

                if (camera != null)
                {
                    var screenPos = camera.WorldToScreenPoint(go.transform.position);
                    var pointerData = new PointerEventData(EventSystem.current)
                    {
                        position = new Vector2(screenPos.x, screenPos.y),
                        button = GetPointerButton(args.button)
                    };

                    var clickHandler = go.GetComponent<IPointerClickHandler>();
                    if (clickHandler != null)
                    {
                        ExecuteEvents.Execute(go, pointerData, ExecuteEvents.pointerClickHandler);
                        return $"{{\"success\":true,\"clicked\":\"{MCPHelpers.EscapeJson(go.name)}\",\"type\":\"3d\"}}";
                    }
                }

                return $"{{\"success\":true,\"found\":\"{MCPHelpers.EscapeJson(go.name)}\",\"type\":\"3d_no_handler\"}}";
            }

            return $"{{\"error\":\"GameObject has no clickable component: {MCPHelpers.EscapeJson(args.path)}\"}}";
        }

        public static string SendKey(string argsJson)
        {
            if (!Application.isPlaying)
                return "{\"error\":\"Play Mode required\"}";

            var args = MCPHelpers.ParseArgs<SendKeyArgs>(argsJson);

            if (string.IsNullOrEmpty(args.key))
                return "{\"error\":\"Key is required\"}";

            if (HasNewInputSystem)
            {
                return SendKeyNewInputSystem(args);
            }
            else
            {
                return SendKeyLegacyInput(args);
            }
        }

        private static string SendKeyNewInputSystem(SendKeyArgs args)
        {
            try
            {
                var inputSystemType = Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
                if (inputSystemType == null)
                    return "{\"error\":\"New Input System not available\"}";

                var keyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
                if (keyboardType == null)
                    return "{\"error\":\"Keyboard type not found\"}";

                var currentProperty = keyboardType.GetProperty("current", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (currentProperty == null)
                    return "{\"error\":\"Could not get current keyboard\"}";

                var keyboard = currentProperty.GetValue(null);
                if (keyboard == null)
                    return "{\"error\":\"No keyboard available\"}";

                return $"{{\"success\":true,\"key\":\"{MCPHelpers.EscapeJson(args.key)}\",\"action\":\"{args.action}\",\"inputSystem\":\"new\",\"note\":\"Key simulation via new Input System has limited support in runtime\"}}";
            }
            catch (Exception e)
            {
                return $"{{\"error\":\"New Input System error: {MCPHelpers.EscapeJson(e.Message)}\"}}";
            }
        }

        private static string SendKeyLegacyInput(SendKeyArgs args)
        {
            if (!Enum.TryParse<KeyCode>(args.key, true, out var keyCode))
            {
                return $"{{\"error\":\"Unknown key: {MCPHelpers.EscapeJson(args.key)}\"}}";
            }

            return $"{{\"success\":true,\"key\":\"{args.key}\",\"keyCode\":\"{keyCode}\",\"action\":\"{args.action}\",\"inputSystem\":\"legacy\",\"note\":\"Legacy Input.GetKey cannot be simulated directly, use EventSystem or Button.onClick instead\"}}";
        }

        public static string Drag(string argsJson)
        {
            if (!Application.isPlaying)
                return "{\"error\":\"Play Mode required\"}";

            var args = MCPHelpers.ParseArgs<DragArgs>(argsJson);

            if (EventSystem.current == null)
                return "{\"error\":\"No EventSystem in scene\"}";

            MCPCoroutineRunner.Instance.StartCoroutine(DragCoroutine(args));

            return $"{{\"success\":true,\"status\":\"drag_started\",\"from\":{{\"x\":{args.startX.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{args.startY.ToString(System.Globalization.CultureInfo.InvariantCulture)}}},\"to\":{{\"x\":{args.endX.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{args.endY.ToString(System.Globalization.CultureInfo.InvariantCulture)}}},\"duration\":{args.duration.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}";
        }

        private static IEnumerator DragCoroutine(DragArgs args)
        {
            var startUnity = CoordinateHelper.ScreenToUnity(args.startX, args.startY);
            var endUnity = CoordinateHelper.ScreenToUnity(args.endX, args.endY);

            var pointerData = new PointerEventData(EventSystem.current)
            {
                position = startUnity,
                button = PointerEventData.InputButton.Left
            };

            var results = ListPool<RaycastResult>.Get();
            EventSystem.current.RaycastAll(pointerData, results);

            GameObject dragTarget = null;
            if (results.Count > 0)
            {
                dragTarget = results[0].gameObject;
                pointerData.pointerCurrentRaycast = results[0];
                pointerData.pointerPressRaycast = results[0];
                pointerData.pointerPress = dragTarget;
                pointerData.pointerDrag = dragTarget;

                ExecuteEvents.Execute(dragTarget, pointerData, ExecuteEvents.pointerEnterHandler);
                ExecuteEvents.Execute(dragTarget, pointerData, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(dragTarget, pointerData, ExecuteEvents.initializePotentialDrag);
                ExecuteEvents.Execute(dragTarget, pointerData, ExecuteEvents.beginDragHandler);
            }

            ListPool<RaycastResult>.Release(results);

            float elapsed = 0f;
            int steps = Mathf.Max(1, args.steps);
            float stepDuration = args.duration / steps;

            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                var currentPos = Vector2.Lerp(startUnity, endUnity, t);
                pointerData.position = currentPos;
                pointerData.delta = (currentPos - (Vector2)pointerData.position);

                if (dragTarget != null)
                {
                    ExecuteEvents.Execute(dragTarget, pointerData, ExecuteEvents.dragHandler);
                }

                yield return new WaitForSeconds(stepDuration);
                elapsed += stepDuration;
            }

            pointerData.position = endUnity;

            if (dragTarget != null)
            {
                ExecuteEvents.Execute(dragTarget, pointerData, ExecuteEvents.endDragHandler);
                ExecuteEvents.Execute(dragTarget, pointerData, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(dragTarget, pointerData, ExecuteEvents.pointerExitHandler);

                results = ListPool<RaycastResult>.Get();
                EventSystem.current.RaycastAll(pointerData, results);

                if (results.Count > 0)
                {
                    var dropTarget = results[0].gameObject;
                    pointerData.pointerCurrentRaycast = results[0];
                    ExecuteEvents.Execute(dropTarget, pointerData, ExecuteEvents.dropHandler);
                }

                ListPool<RaycastResult>.Release(results);
            }
        }

        private static PointerEventData.InputButton GetPointerButton(string button)
        {
            switch (button?.ToLower())
            {
                case "right":
                    return PointerEventData.InputButton.Right;
                case "middle":
                    return PointerEventData.InputButton.Middle;
                default:
                    return PointerEventData.InputButton.Left;
            }
        }
    }
}
