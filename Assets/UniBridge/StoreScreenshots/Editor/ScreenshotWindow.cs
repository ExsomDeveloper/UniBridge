using UnityEditor;
using UnityEngine;

namespace StoreScreenshots.Editor
{
    public class ScreenshotWindow : EditorWindow
    {
        private static readonly Color ColorBorder = new(0.15f, 0.15f, 0.15f);
        private static readonly Color ColorLine   = new(0.28f, 0.28f, 0.28f);

        private ScreenshotConfig _config;
        private SerializedObject _serialized;
        private Vector2 _scrollPos;

        private static readonly ScreenshotResolution[] Presets =
        {
            new("iPhone 6.7\"",       1290, 2796),
            new("iPhone 6.5\"",       1284, 2778),
            new("iPhone 5.5\"",       1242, 2208),
            new("iPad 13\"",          2048, 2732),
            new("Android Phone",      1080, 1920),
            new("Android Tablet 7\"", 1200, 1920),
            new("Android Tablet 10\"",1600, 2560),
        };

        public static void Open()
        {
            var w = GetWindow<ScreenshotWindow>("Store Screenshots");
            w.minSize = new Vector2(420, 340);
            w.LoadConfig();
        }

        private void OnEnable()
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            _config = Resources.Load<ScreenshotConfig>(nameof(ScreenshotConfig));

            if (_config == null)
            {
                EnsureResourcesFolder();
                _config = CreateInstance<ScreenshotConfig>();
                AssetDatabase.CreateAsset(_config, "Assets/UniBridge/Resources/ScreenshotConfig.asset");
                AssetDatabase.SaveAssets();
            }

            _serialized = new SerializedObject(_config);
        }

        private void OnGUI()
        {
            if (_config == null)
            {
                EditorGUILayout.HelpBox(
                    "ScreenshotConfig not found. Click below to create one.",
                    MessageType.Warning);
                if (GUILayout.Button("Create Configuration"))
                    LoadConfig();
                return;
            }

            _serialized.Update();
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // ── Hotkey ──────────────────────────────────────────────────────
            SectionHeader("Горячая клавиша");

            EditorGUI.BeginChangeCheck();

            _config.Hotkey = (KeyCode)EditorGUILayout.EnumPopup("Клавиша", _config.Hotkey);
            _config.RequireShift = EditorGUILayout.Toggle("+ Shift", _config.RequireShift);

            string hotkeyLabel = _config.RequireShift ? $"Shift+{_config.Hotkey}" : _config.Hotkey.ToString();
            EditorGUILayout.HelpBox($"Текущая комбинация: {hotkeyLabel}", MessageType.None);

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_config);

            EditorGUILayout.Space(8);

            // ── Output ──────────────────────────────────────────────────────
            SectionHeader("Папка вывода");

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            _config.OutputFolder = EditorGUILayout.TextField(_config.OutputFolder);
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_config);

            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                var path = EditorUtility.OpenFolderPanel("Select Output Folder",
                    _config.OutputFolder, "");
                if (!string.IsNullOrEmpty(path))
                {
                    var projectDir = System.IO.Directory.GetCurrentDirectory();
                    if (path.StartsWith(projectDir))
                        path = path.Substring(projectDir.Length + 1);
                    _config.OutputFolder = path;
                    EditorUtility.SetDirty(_config);
                }
            }

            if (GUILayout.Button("Открыть", GUILayout.Width(60)))
            {
                var fullPath = System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), _config.OutputFolder));
                if (System.IO.Directory.Exists(fullPath))
                    EditorUtility.RevealInFinder(fullPath);
                else
                    Debug.LogWarning($"[StoreScreenshots] Папка не найдена: {fullPath}");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // ── Resolutions ─────────────────────────────────────────────────
            SectionHeader("Разрешения");

            var resProp = _serialized.FindProperty("Resolutions");
            if (resProp != null)
                EditorGUILayout.PropertyField(resProp, new GUIContent("Список разрешений"), true);

            EditorGUILayout.Space(4);

            // ── Presets ─────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Пресеты", EditorStyles.miniLabel);

            int columns = 3;
            for (int i = 0; i < Presets.Length; i += columns)
            {
                EditorGUILayout.BeginHorizontal();
                for (int j = i; j < i + columns && j < Presets.Length; j++)
                {
                    var preset = Presets[j];
                    if (GUILayout.Button($"+ {preset.Label}", EditorStyles.miniButton))
                    {
                        _config.Resolutions.Add(new ScreenshotResolution(
                            preset.Label, preset.Width, preset.Height));
                        EditorUtility.SetDirty(_config);
                        _serialized.Update();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(12);

            // ── Capture Button ──────────────────────────────────────────────
            EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying || ScreenshotCapture.Instance == null);
            if (GUILayout.Button("Сделать скриншоты", GUILayout.Height(32)))
            {
                ScreenshotCapture.Instance.StartCoroutine(
                    ScreenshotCapture.Instance.CaptureAll(_config));
            }
            EditorGUI.EndDisabledGroup();

            if (!EditorApplication.isPlaying)
                EditorGUILayout.HelpBox("Войдите в Play Mode для захвата скриншотов.", MessageType.Info);

            EditorGUILayout.EndScrollView();
            _serialized.ApplyModifiedProperties();
        }

        private static void SectionHeader(string title)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            var rect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, ColorLine);
            EditorGUILayout.Space(4);
        }

        private static void EnsureResourcesFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/UniBridge/Resources"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/UniBridge"))
                    AssetDatabase.CreateFolder("Assets", "UniBridge");
                AssetDatabase.CreateFolder("Assets/UniBridge", "Resources");
            }
        }
    }
}
