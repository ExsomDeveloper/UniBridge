using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniBridge.Editor
{
    public class YouTubePlayablesTemplateWindow : EditorWindow
    {
        private const string TemplatePath = "Assets/WebGLTemplates/YouTubePlayables/index.html";

        private static readonly Regex PortraitModeRegex =
            new(@"var\s+PORTRAIT_MODE\s*=\s*(true|false)\s*;", RegexOptions.Compiled);

        private static readonly Regex PortraitRatioRegex =
            new(@"var\s+PORTRAIT_RATIO\s*=\s*(\d+)\s*/\s*(\d+)\s*;", RegexOptions.Compiled);

        private static readonly Regex PillarboxColorRegex =
            new(@"var\s+PILLARBOX_COLOR\s*=\s*'([^']*)'\s*;", RegexOptions.Compiled);

        private enum AspectRatioPreset
        {
            _9x16,
            _3x4,
            _10x16,
            _2x3,
            Custom
        }

        private Toggle _portraitToggle;
        private PopupField<string> _ratioPopup;
        private IntegerField _ratioW;
        private IntegerField _ratioH;
        private ColorField _colorField;
        private VisualElement _portraitSettings;
        private VisualElement _customRatioRow;
        private Label _statusLabel;

        private static readonly string[] RatioLabels = { "9:16 (телефон)", "3:4 (планшет)", "10:16 (широкий)", "2:3", "Свой" };
        private static readonly int[][] RatioValues = { new[] { 9, 16 }, new[] { 3, 4 }, new[] { 10, 16 }, new[] { 2, 3 } };

        public static void Open()
        {
            var w = GetWindow<YouTubePlayablesTemplateWindow>("YouTube Playables Template");
            w.minSize = new Vector2(380, 300);
            w.maxSize = new Vector2(500, 400);
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingTop = 8;
            root.style.paddingBottom = 8;
            root.style.paddingLeft = 12;
            root.style.paddingRight = 12;

            var header = new Label("Настройки WebGL-шаблона YouTube Playables");
            header.style.fontSize = 13;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 10;
            root.Add(header);

            if (!File.Exists(TemplatePath))
            {
                root.Add(new Label($"Файл не найден:\n{TemplatePath}") { style = { color = new StyleColor(Color.red) } });
                return;
            }

            _portraitToggle = new Toggle("Portrait Mode (pillarbox)");
            _portraitToggle.RegisterValueChangedCallback(evt => SetPortraitSettingsVisible(evt.newValue));
            root.Add(_portraitToggle);

            _portraitSettings = new VisualElement();
            _portraitSettings.style.marginLeft = 16;
            _portraitSettings.style.marginTop = 6;

            _ratioPopup = new PopupField<string>("Соотношение сторон", new System.Collections.Generic.List<string>(RatioLabels), 0);
            _ratioPopup.RegisterValueChangedCallback(_ => OnRatioPresetChanged());
            _portraitSettings.Add(_ratioPopup);

            _customRatioRow = new VisualElement();
            _customRatioRow.style.flexDirection = FlexDirection.Row;
            _customRatioRow.style.marginTop = 4;
            _customRatioRow.style.marginLeft = 2;

            _ratioW = new IntegerField("Ширина") { value = 9 };
            _ratioW.style.width = 120;
            _ratioW.style.marginRight = 8;
            _customRatioRow.Add(_ratioW);

            var sep = new Label(":");
            sep.style.alignSelf = Align.Center;
            sep.style.marginRight = 8;
            _customRatioRow.Add(sep);

            _ratioH = new IntegerField("Высота") { value = 16 };
            _ratioH.style.width = 120;
            _customRatioRow.Add(_ratioH);

            _portraitSettings.Add(_customRatioRow);

            _colorField = new ColorField("Цвет полос");
            _colorField.showAlpha = false;
            _colorField.style.marginTop = 6;
            _portraitSettings.Add(_colorField);

            root.Add(_portraitSettings);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            root.Add(spacer);

            _statusLabel = new Label();
            _statusLabel.style.fontSize = 10;
            _statusLabel.style.marginBottom = 4;
            root.Add(_statusLabel);

            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.justifyContent = Justify.FlexEnd;

            var openBtn = new Button(() =>
            {
                EditorUtility.RevealInFinder(TemplatePath);
            }) { text = "Показать в проводнике" };
            openBtn.style.marginRight = 6;
            btnRow.Add(openBtn);

            var applyBtn = new Button(ApplySettings) { text = "Применить" };
            btnRow.Add(applyBtn);

            root.Add(btnRow);

            LoadFromTemplate();
        }

        private void LoadFromTemplate()
        {
            var content = File.ReadAllText(TemplatePath);

            var modeMatch = PortraitModeRegex.Match(content);
            if (modeMatch.Success)
                _portraitToggle.value = modeMatch.Groups[1].Value == "true";

            var ratioMatch = PortraitRatioRegex.Match(content);
            if (ratioMatch.Success)
            {
                int w = int.Parse(ratioMatch.Groups[1].Value);
                int h = int.Parse(ratioMatch.Groups[2].Value);
                _ratioW.value = w;
                _ratioH.value = h;
                SetRatioPopupFromValues(w, h);
            }

            var colorMatch = PillarboxColorRegex.Match(content);
            if (colorMatch.Success)
            {
                if (ColorUtility.TryParseHtmlString(colorMatch.Groups[1].Value, out var c))
                    _colorField.value = c;
            }

            SetPortraitSettingsVisible(_portraitToggle.value);
            _statusLabel.text = "";
        }

        private void ApplySettings()
        {
            if (_ratioW.value <= 0 || _ratioH.value <= 0)
            {
                _statusLabel.text = "Ширина и высота должны быть > 0";
                _statusLabel.style.color = new StyleColor(new Color(0.85f, 0.3f, 0.2f));
                return;
            }

            if (_ratioW.value >= _ratioH.value)
            {
                _statusLabel.text = "Для portrait ширина должна быть < высоты";
                _statusLabel.style.color = new StyleColor(new Color(0.85f, 0.3f, 0.2f));
                return;
            }

            var content = File.ReadAllText(TemplatePath);

            var modeStr = _portraitToggle.value ? "true" : "false";
            content = PortraitModeRegex.Replace(content,
                $"var PORTRAIT_MODE   = {modeStr};");

            content = PortraitRatioRegex.Replace(content,
                $"var PORTRAIT_RATIO  = {_ratioW.value} / {_ratioH.value};");

            var hex = "#" + ColorUtility.ToHtmlStringRGB(_colorField.value);
            content = PillarboxColorRegex.Replace(content,
                $"var PILLARBOX_COLOR = '{hex}';");

            File.WriteAllText(TemplatePath, content);
            AssetDatabase.Refresh();

            _statusLabel.text = "Сохранено";
            _statusLabel.style.color = new StyleColor(new Color(0.25f, 0.8f, 0.25f));
        }

        private void SetPortraitSettingsVisible(bool visible)
        {
            _portraitSettings.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnRatioPresetChanged()
        {
            int idx = System.Array.IndexOf(RatioLabels, _ratioPopup.value);
            if (idx >= 0 && idx < RatioValues.Length)
            {
                _ratioW.value = RatioValues[idx][0];
                _ratioH.value = RatioValues[idx][1];
            }
            _customRatioRow.style.display =
                _ratioPopup.value == RatioLabels[RatioLabels.Length - 1] ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetRatioPopupFromValues(int w, int h)
        {
            for (int i = 0; i < RatioValues.Length; i++)
            {
                if (RatioValues[i][0] == w && RatioValues[i][1] == h)
                {
                    _ratioPopup.value = RatioLabels[i];
                    _customRatioRow.style.display = DisplayStyle.None;
                    return;
                }
            }
            _ratioPopup.value = RatioLabels[RatioLabels.Length - 1];
            _customRatioRow.style.display = DisplayStyle.Flex;
        }
    }
}
