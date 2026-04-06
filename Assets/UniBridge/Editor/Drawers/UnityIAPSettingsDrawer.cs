using UnityEditor;
using UnityEngine;

namespace UniBridge.Editor
{
    public class UnityIAPSettingsDrawer : ISettingsDrawer
    {
        public void DrawInspector()
        {
            EditorGUILayout.HelpBox(
                "Unity IAP is configured via the Product Catalog above.\n" +
                "Products are registered automatically during initialization.",
                MessageType.Info);
        }
    }
}
