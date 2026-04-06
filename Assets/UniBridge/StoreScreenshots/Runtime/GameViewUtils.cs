#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace StoreScreenshots
{
    /// <summary>
    /// Reflection-based helper for manipulating the Game View window size.
    /// Uses Resources.FindObjectsOfTypeAll to find existing windows without creating new ones.
    /// </summary>
    public static class GameViewUtils
    {
        private static readonly Type GameViewType;
        private static readonly PropertyInfo SelectedSizeIndexProp;
        private static readonly Type GameViewSizesType;
        private static readonly Type GameViewSizeType;
        private static readonly Type GameViewSizeTypeEnum;

        static GameViewUtils()
        {
            GameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            GameViewSizesType = Type.GetType("UnityEditor.GameViewSizes,UnityEditor");
            GameViewSizeType = Type.GetType("UnityEditor.GameViewSize,UnityEditor");
            GameViewSizeTypeEnum = Type.GetType("UnityEditor.GameViewSizeType,UnityEditor");

            if (GameViewType != null)
                SelectedSizeIndexProp = GameViewType.GetProperty(
                    "selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        public static bool IsAvailable =>
            GameViewType != null && SelectedSizeIndexProp != null &&
            GameViewSizesType != null && GameViewSizeType != null && GameViewSizeTypeEnum != null;

        public static EditorWindow GetGameView()
        {
            if (GameViewType == null) return null;

            // Find existing Game View — do NOT create a new one
            var windows = Resources.FindObjectsOfTypeAll(GameViewType);
            if (windows != null && windows.Length > 0)
                return (EditorWindow)windows[0];

            return null;
        }

        public static int GetSelectedSizeIndex()
        {
            var gameView = GetGameView();
            if (gameView == null || SelectedSizeIndexProp == null) return -1;
            return (int)SelectedSizeIndexProp.GetValue(gameView);
        }

        public static void SetSelectedSizeIndex(int index)
        {
            var gameView = GetGameView();
            if (gameView == null || SelectedSizeIndexProp == null) return;
            SelectedSizeIndexProp.SetValue(gameView, index);
            gameView.Repaint();
        }

        /// <summary>
        /// Adds a custom fixed-resolution size to GameViewSizes for the current platform.
        /// Returns the index of the newly added size, or -1 on failure.
        /// </summary>
        public static int AddCustomSize(int width, int height, string label)
        {
            var group = GetCurrentGroup();
            if (group == null)
            {
                Debug.LogError("[GameViewUtils] GetCurrentGroup() returned null");
                return -1;
            }

            var fixedRes = Enum.Parse(GameViewSizeTypeEnum, "FixedResolution");

            // Try all known constructor signatures
            var ctor = GameViewSizeType.GetConstructor(new[]
                { GameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string) });

            if (ctor == null)
            {
                // Some Unity versions use (int, int, int, string) where first int is the enum value
                ctor = GameViewSizeType.GetConstructor(new[]
                    { typeof(int), typeof(int), typeof(int), typeof(string) });
                if (ctor != null)
                    Debug.Log("[GameViewUtils] Using int-based constructor");
            }

            if (ctor == null)
            {
                // Log all available constructors for diagnostics
                var ctors = GameViewSizeType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var c in ctors)
                {
                    var ps = c.GetParameters();
                    var sig = string.Join(", ", Array.ConvertAll(ps, p => $"{p.ParameterType.Name} {p.Name}"));
                    Debug.Log($"[GameViewUtils] Available ctor: ({sig})");
                }
                Debug.LogError("[GameViewUtils] No matching GameViewSize constructor found");
                return -1;
            }

            object newSize;
            var ctorParams = ctor.GetParameters();
            if (ctorParams[0].ParameterType == typeof(int))
                newSize = ctor.Invoke(new object[] { (int)(object)fixedRes, width, height, label });
            else
                newSize = ctor.Invoke(new object[] { fixedRes, width, height, label });

            var addMethod = group.GetType().GetMethod("AddCustomSize",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (addMethod == null)
            {
                Debug.LogError("[GameViewUtils] AddCustomSize method not found on group");
                return -1;
            }

            addMethod.Invoke(group, new[] { newSize });

            var getTotalCount = group.GetType().GetMethod("GetTotalCount",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (getTotalCount == null)
            {
                Debug.LogError("[GameViewUtils] GetTotalCount method not found on group");
                return -1;
            }

            return (int)getTotalCount.Invoke(group, null) - 1;
        }

        /// <summary>
        /// Removes a custom size at the given index from GameViewSizes.
        /// </summary>
        public static void RemoveCustomSize(int index)
        {
            var group = GetCurrentGroup();
            if (group == null) return;

            var removeMethod = group.GetType().GetMethod("RemoveCustomSize",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            removeMethod?.Invoke(group, new object[] { index });
        }

        private static object GetCurrentGroup()
        {
            if (GameViewSizesType == null) return null;

            // GameViewSizes is a ScriptableObject singleton — find via FindObjectsOfTypeAll
            var instances = Resources.FindObjectsOfTypeAll(GameViewSizesType);
            if (instances == null || instances.Length == 0) return null;

            var instance = instances[0];

            var currentGroupProp = instance.GetType().GetProperty("currentGroup",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return currentGroupProp?.GetValue(instance);
        }
    }
}
#endif
