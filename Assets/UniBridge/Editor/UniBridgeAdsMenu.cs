using UnityEditor;
using UnityEngine;

namespace UniBridge.Editor
{
    public class UniBridgeMenu : UnityEditor.Editor
    {
        [MenuItem("UniBridge/Settings", false, 0)]
        public static void OpenSettings()
        {
            UniBridgeSettingsWindow.Open(0);
        }

        [MenuItem("UniBridge/Screenshots", false, 60)]
        public static void OpenScreenshots()
        {
            StoreScreenshots.Editor.ScreenshotWindow.Open();
        }

        [MenuItem("UniBridge/Documentation", false, 200)]
        public static void OpenDocumentation()
        {
            var readme = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/UniBridge/README.md");
            if (readme != null)
            {
                Selection.activeObject = readme;
            }
            else
            {
                Debug.Log("[UniBridge] README.md not found in Assets/UniBridge/");
            }
        }
    }
}
