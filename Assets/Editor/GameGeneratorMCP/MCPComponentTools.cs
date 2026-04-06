using System;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace GameGenerator.MCP
{
    public static class MCPComponentTools
    {
        [Serializable]
        private class GetComponentArgs
        {
            public string path;
            public string componentType;
        }

        [Serializable]
        private class GetPropertyArgs
        {
            public string path;
            public string componentType;
            public string property;
        }

        [Serializable]
        private class SetPropertyArgs
        {
            public string path;
            public string componentType;
            public string property;
            public string value;
        }

        [Serializable]
        private class FindByComponentArgs
        {
            public string componentType;
            public bool includeInactive = false;
            public int limit = 50;
        }

        public static string GetComponent(string argsJson)
        {
            var args = MCPHelpers.ParseArgs<GetComponentArgs>(argsJson);
            var go = MCPHelpers.FindGameObject(args.path);

            if (go == null)
                return $"{{\"error\":\"GameObject not found: {MCPHelpers.EscapeJson(args.path)}\"}}";

            var component = MCPHelpers.FindComponent(go, args.componentType);
            if (component == null)
                return $"{{\"error\":\"Component not found: {MCPHelpers.EscapeJson(args.componentType)}\"}}";

            return SerializeComponent(component);
        }

        private static string SerializeComponent(Component component)
        {
            var sb = new StringBuilder();
            var type = component.GetType();
            sb.Append($"{{\"type\":\"{type.Name}\",\"fullType\":\"{type.FullName}\",\"properties\":{{");

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            bool first = true;

            foreach (var field in fields)
            {
                if (field.IsNotSerialized) continue;
                if (field.GetCustomAttribute<ObsoleteAttribute>() != null) continue;

                try
                {
                    var value = field.GetValue(component);
                    if (!first) sb.Append(",");
                    first = false;
                    sb.Append($"\"{field.Name}\":{MCPHelpers.SerializeValue(value)}");
                }
                catch { }
            }

            foreach (var prop in properties)
            {
                if (!prop.CanRead) continue;
                if (prop.GetIndexParameters().Length > 0) continue;
                if (prop.GetCustomAttribute<ObsoleteAttribute>() != null) continue;
                if (prop.Name == "mesh" || prop.Name == "material" || prop.Name == "materials" ||
                    prop.Name == "sharedMesh" || prop.Name == "sharedMaterial" || prop.Name == "sharedMaterials")
                    continue;

                try
                {
                    var value = prop.GetValue(component);
                    if (!first) sb.Append(",");
                    first = false;
                    sb.Append($"\"{prop.Name}\":{MCPHelpers.SerializeValue(value)}");
                }
                catch { }
            }

            sb.Append("}}");
            return sb.ToString();
        }

        public static string GetComponentProperty(string argsJson)
        {
            var args = MCPHelpers.ParseArgs<GetPropertyArgs>(argsJson);
            var go = MCPHelpers.FindGameObject(args.path);

            if (go == null)
                return $"{{\"error\":\"GameObject not found: {MCPHelpers.EscapeJson(args.path)}\"}}";

            var component = MCPHelpers.FindComponent(go, args.componentType);
            if (component == null)
                return $"{{\"error\":\"Component not found: {MCPHelpers.EscapeJson(args.componentType)}\"}}";

            var type = component.GetType();

            var field = type.GetField(args.property, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                try
                {
                    var value = field.GetValue(component);
                    return $"{{\"property\":\"{MCPHelpers.EscapeJson(args.property)}\",\"type\":\"{field.FieldType.Name}\",\"value\":{MCPHelpers.SerializeValue(value)}}}";
                }
                catch (Exception e)
                {
                    return $"{{\"error\":\"Failed to get field: {MCPHelpers.EscapeJson(e.Message)}\"}}";
                }
            }

            var prop = type.GetProperty(args.property, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanRead)
            {
                try
                {
                    var value = prop.GetValue(component);
                    return $"{{\"property\":\"{MCPHelpers.EscapeJson(args.property)}\",\"type\":\"{prop.PropertyType.Name}\",\"value\":{MCPHelpers.SerializeValue(value)}}}";
                }
                catch (Exception e)
                {
                    return $"{{\"error\":\"Failed to get property: {MCPHelpers.EscapeJson(e.Message)}\"}}";
                }
            }

            return $"{{\"error\":\"Property not found: {MCPHelpers.EscapeJson(args.property)}\"}}";
        }

        public static string SetComponentProperty(string argsJson)
        {
            var args = MCPHelpers.ParseArgs<SetPropertyArgs>(argsJson);
            var go = MCPHelpers.FindGameObject(args.path);

            if (go == null)
                return $"{{\"error\":\"GameObject not found: {MCPHelpers.EscapeJson(args.path)}\"}}";

            var component = MCPHelpers.FindComponent(go, args.componentType);
            if (component == null)
                return $"{{\"error\":\"Component not found: {MCPHelpers.EscapeJson(args.componentType)}\"}}";

            var type = component.GetType();

            var field = type.GetField(args.property, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                try
                {
                    var convertedValue = ConvertStringValue(args.value, field.FieldType);
                    field.SetValue(component, convertedValue);
                    return $"{{\"success\":true,\"property\":\"{MCPHelpers.EscapeJson(args.property)}\",\"newValue\":{MCPHelpers.SerializeValue(convertedValue)}}}";
                }
                catch (Exception e)
                {
                    return $"{{\"error\":\"Failed to set field: {MCPHelpers.EscapeJson(e.Message)}\"}}";
                }
            }

            var prop = type.GetProperty(args.property, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                try
                {
                    var convertedValue = ConvertStringValue(args.value, prop.PropertyType);
                    prop.SetValue(component, convertedValue);
                    return $"{{\"success\":true,\"property\":\"{MCPHelpers.EscapeJson(args.property)}\",\"newValue\":{MCPHelpers.SerializeValue(convertedValue)}}}";
                }
                catch (Exception e)
                {
                    return $"{{\"error\":\"Failed to set property: {MCPHelpers.EscapeJson(e.Message)}\"}}";
                }
            }

            return $"{{\"error\":\"Property not found or not writable: {MCPHelpers.EscapeJson(args.property)}\"}}";
        }

        private static object ConvertStringValue(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value) || value == "null")
                return null;

            if (targetType == typeof(string))
                return value;

            if (targetType == typeof(int))
                return int.Parse(value);
            if (targetType == typeof(float))
                return float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            if (targetType == typeof(double))
                return double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            if (targetType == typeof(bool))
                return bool.Parse(value);
            if (targetType == typeof(long))
                return long.Parse(value);

            if (targetType == typeof(Vector2))
            {
                var v2 = JsonUtility.FromJson<Vector2Wrapper>("{\"value\":" + value + "}");
                return v2.value;
            }
            if (targetType == typeof(Vector3))
            {
                var v3 = JsonUtility.FromJson<Vector3Wrapper>("{\"value\":" + value + "}");
                return v3.value;
            }
            if (targetType == typeof(Color))
            {
                var c = JsonUtility.FromJson<ColorWrapper>("{\"value\":" + value + "}");
                return c.value;
            }

            if (targetType.IsEnum)
                return Enum.Parse(targetType, value, true);

            return Convert.ChangeType(value, targetType);
        }

        [Serializable]
        private class Vector2Wrapper { public Vector2 value; }
        [Serializable]
        private class Vector3Wrapper { public Vector3 value; }
        [Serializable]
        private class ColorWrapper { public Color value; }

        public static string FindObjectsByComponent(string argsJson)
        {
            var args = MCPHelpers.ParseArgs<FindByComponentArgs>(argsJson);

            if (string.IsNullOrEmpty(args.componentType))
                return "{\"error\":\"componentType is required\"}";

            var sb = new StringBuilder();
            sb.Append("{\"results\":[");

            int count = 0;
            bool first = true;

            var type = FindTypeByName(args.componentType);
            if (type != null && typeof(Component).IsAssignableFrom(type))
            {
                var findType = args.includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
                var components = UnityEngine.Object.FindObjectsByType(type, findType, FindObjectsSortMode.None);

                foreach (Component comp in components)
                {
                    if (count >= args.limit) break;
                    if (comp == null) continue;

                    if (!first) sb.Append(",");
                    first = false;

                    var go = comp.gameObject;
                    sb.Append($"{{\"name\":\"{MCPHelpers.EscapeJson(go.name)}\",\"path\":\"{MCPHelpers.EscapeJson(MCPHelpers.GetGameObjectPath(go))}\",\"active\":{go.activeInHierarchy.ToString().ToLower()}}}");
                    count++;
                }
            }
            else
            {
                var allObjects = args.includeInactive
                    ? UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    : UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

                foreach (var go in allObjects)
                {
                    if (count >= args.limit) break;

                    var comp = MCPHelpers.FindComponent(go, args.componentType);
                    if (comp != null)
                    {
                        if (!first) sb.Append(",");
                        first = false;

                        sb.Append($"{{\"name\":\"{MCPHelpers.EscapeJson(go.name)}\",\"path\":\"{MCPHelpers.EscapeJson(MCPHelpers.GetGameObjectPath(go))}\",\"active\":{go.activeInHierarchy.ToString().ToLower()}}}");
                        count++;
                    }
                }
            }

            sb.Append($"],\"count\":{count}}}");
            return sb.ToString();
        }

        private static Type FindTypeByName(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;

            type = Type.GetType($"UnityEngine.{typeName}, UnityEngine.CoreModule");
            if (type != null) return type;

            type = Type.GetType($"UnityEngine.{typeName}, UnityEngine");
            if (type != null) return type;

            type = Type.GetType($"UnityEngine.UI.{typeName}, UnityEngine.UI");
            if (type != null) return type;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null) return type;

                foreach (var t in assembly.GetTypes())
                {
                    if (t.Name == typeName)
                        return t;
                }
            }

            return null;
        }
    }
}
