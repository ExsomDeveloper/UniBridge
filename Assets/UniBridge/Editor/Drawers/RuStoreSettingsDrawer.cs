using UniBridge;
using UnityEditor;
using UnityEngine;

namespace UniBridge.Editor
{
    public class RuStoreSettingsDrawer : ISettingsDrawer
    {
        private readonly UniBridgePurchasesConfig _config;
        private readonly SerializedObject _serializedObject;
        private SerializedProperty _ruStoreProp;

        public RuStoreSettingsDrawer(UniBridgePurchasesConfig config, SerializedObject serializedObject)
        {
            _config           = config;
            _serializedObject = serializedObject;
            _ruStoreProp      = serializedObject.FindProperty("_ruStoreSettings");
        }

        public void DrawInspector()
        {
            if (_ruStoreProp == null)
                return;

            EditorGUI.BeginChangeCheck();

            var consoleIdProp = _ruStoreProp.FindPropertyRelative("_consoleApplicationId");
            var deeplinkProp  = _ruStoreProp.FindPropertyRelative("_deeplinkPrefix");

            if (consoleIdProp != null)
                EditorGUILayout.PropertyField(consoleIdProp,
                    new GUIContent("Console Application ID", "Application ID from RuStore Console (numeric)"));

            if (deeplinkProp != null)
                EditorGUILayout.PropertyField(deeplinkProp,
                    new GUIContent("Deeplink Prefix", "Deep link scheme, must match AndroidManifest.xml intent-filter (e.g. yourapp://rustore)"));

            if (EditorGUI.EndChangeCheck())
            {
                _serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_config);
            }
        }
    }
}
