using UniBridge;
using UnityEditor;
using UnityEngine;

namespace UniBridge.Editor
{
    [CustomEditor(typeof(UniBridgePurchasesConfig))]
    public class UniBridgePurchasesConfigEditor : UnityEditor.Editor
    {
        private UniBridgePurchasesConfig _config;
        private UnityIAPSettingsDrawer _iapDrawer;
        private RuStoreSettingsDrawer _ruStoreDrawer;

        private void OnEnable()
        {
            _config       = (UniBridgePurchasesConfig)target;
            _iapDrawer    = new UnityIAPSettingsDrawer();
            _ruStoreDrawer = new RuStoreSettingsDrawer(_config, serializedObject);
        }

        public override void OnInspectorGUI()
        {
            _config = (UniBridgePurchasesConfig)target;
            serializedObject.Update();

            DrawHeader();
            DrawGeneralSettings();
            DrawProductCatalog();
            DrawPlatformStatus();
            DrawPlatformSettings();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            GUILayout.Label("UniBridgePurchases Configuration", GetTitleStyle(), GUILayout.ExpandWidth(true));
            EditorGUILayout.Space(5);
        }

        private void DrawGeneralSettings()
        {
            GUILayout.Label("General Settings", GetSubTitleStyle(), GUILayout.ExpandWidth(true));

            EditorGUI.BeginChangeCheck();
            _config.AutoInitialize = EditorGUILayout.Toggle(
                new GUIContent("Auto Initialize", "Automatically initialize UniBridgePurchases on startup"),
                _config.AutoInitialize);

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_config);

            EditorGUILayout.Space(10);
        }

        private void DrawProductCatalog()
        {
            GUILayout.Label("Product Catalog", GetSubTitleStyle(), GUILayout.ExpandWidth(true));
            EditorGUILayout.Space(3);

            var productsProp = serializedObject.FindProperty("_products");
            if (productsProp != null)
            {
                EditorGUILayout.PropertyField(productsProp, new GUIContent("Products"), true);
            }

            EditorGUILayout.Space(10);
        }

        private void DrawPlatformStatus()
        {
            GUILayout.Label("Platform SDK Status", GetTitleStyle(), GUILayout.ExpandWidth(true));
            EditorGUILayout.Space(5);

            DrawStatusRow("Unity IAP (Android/iOS):",
#if UNIBRIDGEPURCHASES_IAP
                true
#else
                false
#endif
            );

            DrawStatusRow("RuStore Billing (Android):",
#if UNIBRIDGEPURCHASES_RUSTORE
                true
#else
                false
#endif
            );

            DrawStatusRow("Playgama Payments (WebGL):",
#if UNIBRIDGE_PLAYGAMA
                true
#else
                false
#endif
            );

            EditorGUILayout.Space(10);
        }

        private void DrawStatusRow(string label, bool installed)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(200));
            EditorGUILayout.LabelField(installed ? "Installed" : "Not Installed",
                installed ? GetInstalledStyle() : GetNotInstalledStyle());
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPlatformSettings()
        {
            EditorGUILayout.Space(5);
            GUILayout.Label("Unity IAP Settings (Google Play / App Store)", GetSubTitleStyle(), GUILayout.ExpandWidth(true));
            _iapDrawer?.DrawInspector();

            EditorGUILayout.Space(10);
            GUILayout.Label("RuStore Billing Settings", GetSubTitleStyle(), GUILayout.ExpandWidth(true));
            _ruStoreDrawer?.DrawInspector();
        }

        private GUIStyle GetTitleStyle() => new GUIStyle(EditorStyles.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize  = 14
        };

        private GUIStyle GetSubTitleStyle() => new GUIStyle(EditorStyles.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize  = 12
        };

        private GUIStyle GetInstalledStyle()
        {
            var style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = new Color(0.2f, 0.8f, 0.2f);
            return style;
        }

        private GUIStyle GetNotInstalledStyle()
        {
            var style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = new Color(0.8f, 0.8f, 0.2f);
            return style;
        }
    }
}
