using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UniBridge.Editor
{
    public class PlaygamaPurchaseDrawer : ISettingsDrawer
    {
        private const string BridgeConfigPath =
            "Assets/WebGLTemplates/Bridge/playgama-bridge-config.json";

        private const float GamToUsd = 0.1f;

        private readonly UniBridgePurchasesConfig _config;
        private readonly SerializedObject   _serializedObject;

        public PlaygamaPurchaseDrawer(UniBridgePurchasesConfig config, SerializedObject serializedObject)
        {
            _config           = config;
            _serializedObject = serializedObject;
        }

        public void DrawInspector()
        {
            if (_config == null) return;

            var products = _config.Products;

            if (products == null || products.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Каталог продуктов пуст. Добавьте продукты на вкладке Products.",
                    MessageType.Info);
                return;
            }

            _serializedObject.Update();

            var productsProp = _serializedObject.FindProperty("_products");

            for (int i = 0; i < products.Count; i++)
            {
                var product     = products[i];
                var productProp = productsProp.GetArrayElementAtIndex(i);
                var amountProp  = productProp.FindPropertyRelative("_playgamaAmount");

                DrawProductRow(product, amountProp);
                EditorGUILayout.Space(4);
            }

            _serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8);

            bool configExists = File.Exists(BridgeConfigPath);
            if (!configExists)
            {
                EditorGUILayout.HelpBox(
                    $"Файл не найден:\n{BridgeConfigPath}\n\nУбедитесь, что WebGL Template: Bridge выбран в Player Settings.",
                    MessageType.Warning);
                return;
            }

            if (GUILayout.Button("Синхронизировать в Bridge конфиге", GUILayout.Height(30)))
                SyncPayments();
        }

        private static void DrawProductRow(ProductDefinition product, SerializedProperty amountProp)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // ── Header: ProjectId + PlaygamaProductId ──────────────────────────
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(product.ProjectId, EditorStyles.boldLabel);

            string playgamaId = product.PlaygamaProductId;
            bool hasCustomId  = !string.IsNullOrEmpty(playgamaId);

            if (hasCustomId)
            {
                var prev = GUI.color;
                GUI.color = new Color(0.65f, 0.65f, 0.65f);
                EditorGUILayout.LabelField($"id: {playgamaId}");
                GUI.color = prev;
            }
            else
            {
                EditorGUILayout.LabelField("PlaygamaProductId не задан",
                    new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.9f, 0.6f, 0.1f) } });
            }

            EditorGUILayout.EndHorizontal();

            // ── Price row: Gam ↔ USD ──────────────────────────────────────────
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Цена:", GUILayout.Width(42));

            EditorGUILayout.LabelField("Gam", GUILayout.Width(30));
            int currentGam = Mathf.Max(1, amountProp.intValue);
            int newGam     = EditorGUILayout.IntField(currentGam, GUILayout.Width(60));
            newGam = Mathf.Max(1, newGam);

            EditorGUILayout.LabelField("=", GUILayout.Width(14));
            EditorGUILayout.LabelField("USD $", GUILayout.Width(42));
            float currentUsd = currentGam * GamToUsd;
            float newUsd     = EditorGUILayout.FloatField(currentUsd, GUILayout.Width(60));

            if (newGam != currentGam)
                amountProp.intValue = newGam;
            else if (!Mathf.Approximately(newUsd, currentUsd))
                amountProp.intValue = Mathf.Max(1, Mathf.RoundToInt(newUsd / GamToUsd));

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void SyncPayments()
        {
            var products = _config.Products;
            var sb       = new StringBuilder();
            sb.AppendLine("[");
            int written = 0;

            for (int i = 0; i < products.Count; i++)
            {
                string id = products[i].PlaygamaProductId;
                if (string.IsNullOrEmpty(id)) continue;

                int amount = Mathf.Max(1, products[i].PlaygamaAmount);

                if (written > 0) sb.AppendLine(",");
                sb.Append($"        {{\"id\": \"{id}\", \"playgama\": {{\"amount\": {amount}}}, \"playdeck\": {{\"amount\": {amount}}}}}");
                written++;
            }

            if (written == 0)
            {
                EditorUtility.DisplayDialog("UniBridge",
                    "Ни у одного продукта не задан PlaygamaProductId.", "OK");
                return;
            }

            sb.AppendLine();
            sb.Append("    ]");

            string json    = File.ReadAllText(BridgeConfigPath);
            var    regex   = new Regex(@"""payments""\s*:\s*\[[\s\S]*?\]");
            string newJson = regex.Replace(json, $"\"payments\": {sb}");

            File.WriteAllText(BridgeConfigPath, newJson);
            AssetDatabase.Refresh();

            Debug.Log($"[UniBridge] Обновлено {written} продуктов в {BridgeConfigPath}");
        }
    }
}
