using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameGenerator.MCP
{
    public static class CoordinateHelper
    {
        public static Vector2 ScreenToUnity(float x, float y)
        {
            return new Vector2(x, Screen.height - y);
        }

        public static Vector2 UnityToScreen(float x, float y)
        {
            return new Vector2(x, Screen.height - y);
        }

        public static Vector2 UnityToScreen(Vector3 unityScreenPos)
        {
            return new Vector2(unityScreenPos.x, Screen.height - unityScreenPos.y);
        }
    }

    public static class MCPHelpers
    {
        public static GameObject FindGameObject(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            var go = GameObject.Find(path);
            if (go != null)
                return go;

            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                var scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    var found = FindByPath(root.transform, path);
                    if (found != null)
                        return found.gameObject;
                }
            }

            return null;
        }

        public static Transform FindByPath(Transform root, string path)
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

        public static string GetGameObjectPath(GameObject go)
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

        public static string EscapeJson(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            return str
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        public static T ParseArgs<T>(string json) where T : new()
        {
            if (string.IsNullOrEmpty(json))
                return new T();

            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch
            {
                return new T();
            }
        }

        public static string SerializeValue(object value)
        {
            if (value == null) return "null";

            var type = value.GetType();

            if (type == typeof(string))
                return $"\"{EscapeJson((string)value)}\"";
            if (type == typeof(bool))
                return value.ToString().ToLower();
            if (type == typeof(float))
                return ((float)value).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (type == typeof(double))
                return ((double)value).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (type.IsPrimitive)
                return value.ToString();
            if (value is Vector2 v2)
                return $"{{\"x\":{v2.x.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{v2.y.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}";
            if (value is Vector3 v3)
                return $"{{\"x\":{v3.x.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{v3.y.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"z\":{v3.z.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}";
            if (value is Vector4 v4)
                return $"{{\"x\":{v4.x.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{v4.y.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"z\":{v4.z.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"w\":{v4.w.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}";
            if (value is Quaternion q)
                return $"{{\"x\":{q.x.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{q.y.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"z\":{q.z.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"w\":{q.w.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}";
            if (value is Color c)
                return $"{{\"r\":{c.r.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"g\":{c.g.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"b\":{c.b.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"a\":{c.a.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}";
            if (value is Color32 c32)
                return $"{{\"r\":{c32.r},\"g\":{c32.g},\"b\":{c32.b},\"a\":{c32.a}}}";
            if (value is Rect rect)
                return $"{{\"x\":{rect.x.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{rect.y.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"width\":{rect.width.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"height\":{rect.height.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}";
            if (value is Bounds bounds)
                return $"{{\"center\":{SerializeValue(bounds.center)},\"size\":{SerializeValue(bounds.size)}}}";
            if (value is UnityEngine.Object obj)
                return $"{{\"name\":\"{EscapeJson(obj.name)}\",\"type\":\"{obj.GetType().Name}\"}}";
            if (type.IsEnum)
                return $"\"{value}\"";
            if (value is System.Collections.IList list)
            {
                var sb = new StringBuilder();
                sb.Append("[");
                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append(SerializeValue(list[i]));
                }
                sb.Append("]");
                return sb.ToString();
            }

            return $"\"{EscapeJson(value.ToString())}\"";
        }

        public static object ConvertValue(object value, Type targetType)
        {
            if (value == null)
                return null;

            if (targetType.IsAssignableFrom(value.GetType()))
                return value;

            if (targetType == typeof(string))
                return value.ToString();

            if (targetType == typeof(int))
                return Convert.ToInt32(value);
            if (targetType == typeof(float))
                return Convert.ToSingle(value);
            if (targetType == typeof(double))
                return Convert.ToDouble(value);
            if (targetType == typeof(bool))
                return Convert.ToBoolean(value);
            if (targetType == typeof(long))
                return Convert.ToInt64(value);

            if (targetType.IsEnum && value is string strVal)
                return Enum.Parse(targetType, strVal, true);

            return Convert.ChangeType(value, targetType);
        }

        public static Component FindComponent(GameObject go, string componentType)
        {
            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                if (comp.GetType().Name == componentType ||
                    comp.GetType().FullName == componentType)
                {
                    return comp;
                }
            }
            return null;
        }
    }

    public class MCPCoroutineRunner : MonoBehaviour
    {
        private static MCPCoroutineRunner _instance;

        public static MCPCoroutineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[MCP Coroutine Runner]");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    _instance = go.AddComponent<MCPCoroutineRunner>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void OnDestroy()
        {
            _instance = null;
        }
    }
}
