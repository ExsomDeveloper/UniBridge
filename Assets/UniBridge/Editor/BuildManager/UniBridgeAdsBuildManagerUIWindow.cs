using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniBridge.Editor
{
    public class UniBridgeBuildManagerUIWindow : EditorWindow
    {
        // ── State ─────────────────────────────────────────────────────────────

        private List<StorePreset> _presets;
        private StorePreset       _selected;
        private List<(string identifier, string displayName, bool useManifest)> _availableSdks;
        private readonly Dictionary<StorePreset, VisualElement> _listItems = new();

        // ── UI references ─────────────────────────────────────────────────────

        private VisualElement _listContainer;
        private VisualElement _detailPlaceholder;
        private VisualElement _detailPanel;
        private Label         _detailTitle;
        private Label         _detailDefine;
        private Label         _detailBuildTarget;
        private VisualElement _sdkChipsContainer;
        private Button        _sdkAddBtn;
        private VisualElement _adaptersSection;
        private VisualElement _checklistSection;
        private VisualElement _checklistItemsContainer;
        private Button        _selectBtn;

        // ── Colors ────────────────────────────────────────────────────────────

        private static readonly Color ColorGreen  = new(0.25f, 0.80f, 0.25f);
        private static readonly Color ColorGrey   = new(0.55f, 0.55f, 0.55f);
        private static readonly Color ColorBorder = new(0.15f, 0.15f, 0.15f);
        private static readonly Color ColorBg     = new(0.18f, 0.18f, 0.18f);
        private static readonly Color ColorBgSel         = new(0.24f, 0.37f, 0.57f);
        private static readonly Color ColorBgSdkInstalled = new(0.15f, 0.38f, 0.15f);

        // ── Entry point ───────────────────────────────────────────────────────

        [MenuItem("UniBridge/Build Manager", false, 51)]
        public static void ShowWindow()
        {
            var w = GetWindow<UniBridgeBuildManagerUIWindow>("Build Manager");
            w.minSize = new Vector2(580, 420);
            w.Show();
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void CreateGUI()
        {
            _presets = StorePresetsManager.Load();
            LoadAvailableSdks();
            ReorderPresets();
            BuildUI();

            foreach (var p in _presets)
            {
                if (IsActive(p)) { SelectEntry(p); break; }
            }
        }

        // ── Data ──────────────────────────────────────────────────────────────

        private static bool IsActive(StorePreset p) =>
            !string.IsNullOrEmpty(p.define) &&
            StorePlatformDefines.GetCurrentStoreDefine() == p.define;

        private void LoadAvailableSdks()
        {
            _availableSdks = new List<(string, string, bool)>();
            var versions = SDKInstallerWindow.LoadRequiredVersions();
            if (versions == null) return;

            foreach (var field in typeof(SDKVersions).GetFields())
            {
                var info = field.GetValue(versions) as SDKInfo;
                if (info == null) continue;

                // Use define if available, otherwise fall back to packageId
                string identifier = !string.IsNullOrEmpty(info.define)   ? info.define    :
                                    !string.IsNullOrEmpty(info.packageId) ? info.packageId : null;
                if (identifier == null) continue;

                string name = !string.IsNullOrEmpty(info.displayName) ? info.displayName : field.Name;

                bool useManifest = info.checkMode == "package";
                if (string.IsNullOrEmpty(info.checkMode))
                    useManifest = string.IsNullOrEmpty(info.define) && !string.IsNullOrEmpty(info.packageId);

                _availableSdks.Add((identifier, name, useManifest));
            }
        }

        private void ReorderPresets()
        {
            for (int i = 0; i < _presets.Count; i++)
            {
                if (!IsActive(_presets[i])) continue;
                var active = _presets[i];
                _presets.RemoveAt(i);
                _presets.Insert(0, active);
                break;
            }
        }

        // ── UI construction ───────────────────────────────────────────────────

        private void BuildUI()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Row;
            root.style.flexGrow      = 1;

            root.Add(BuildLeftPanel());
            root.Add(BuildRightPanel());
        }

        private VisualElement BuildLeftPanel()
        {
            var panel = new VisualElement();
            panel.style.width            = 220;
            panel.style.flexShrink       = 0;
            panel.style.backgroundColor  = new StyleColor(ColorBg);
            panel.style.borderRightWidth = 1;
            panel.style.borderRightColor = new StyleColor(ColorBorder);
            panel.style.flexDirection    = FlexDirection.Column;

            panel.Add(BuildLeftPanelHeader());

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;

            _listContainer = scroll.contentContainer;
            _listContainer.style.flexDirection = FlexDirection.Column;

            foreach (var p in _presets)
            {
                var item = BuildListItem(p);
                _listItems[p] = item;
                _listContainer.Add(item);
            }

            panel.Add(scroll);
            return panel;
        }

        private VisualElement BuildLeftPanelHeader()
        {
            var header = new VisualElement();
            header.style.flexDirection     = FlexDirection.Row;
            header.style.alignItems        = Align.Center;
            header.style.paddingTop        = 8;
            header.style.paddingBottom     = 8;
            header.style.paddingLeft       = 12;
            header.style.paddingRight      = 6;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new StyleColor(ColorBorder);

            var label = new Label("Магазины");
            label.style.fontSize                = 12;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.flexGrow                = 1;

            header.Add(label);
            return header;
        }

        private VisualElement BuildListItem(StorePreset preset)
        {
            var item = new VisualElement();
            item.style.paddingTop    = 8;
            item.style.paddingBottom = 8;
            item.style.paddingLeft   = 12;
            item.style.paddingRight  = 10;
            item.style.flexDirection = FlexDirection.Column;

            var topRow = new VisualElement();
            topRow.style.flexDirection = FlexDirection.Row;
            topRow.style.alignItems    = Align.Center;

            var dot = new VisualElement();
            dot.name = "dot";
            dot.style.width            = 8;
            dot.style.height           = 8;
            dot.style.borderTopLeftRadius     = 4;
            dot.style.borderTopRightRadius    = 4;
            dot.style.borderBottomLeftRadius  = 4;
            dot.style.borderBottomRightRadius = 4;
            dot.style.marginRight      = 6;
            dot.style.backgroundColor  = new StyleColor(IsActive(preset) ? ColorGreen : ColorGrey);

            var nameLabel = new Label(preset.displayName);
            nameLabel.name = "nameLabel";
            nameLabel.style.fontSize                = 12;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.flexGrow                = 1;

            topRow.Add(dot);
            topRow.Add(nameLabel);

            var subLabel = new Label(preset.buildTarget);
            subLabel.name = "subLabel";
            subLabel.style.fontSize    = 10;
            subLabel.style.color       = new StyleColor(ColorGrey);
            subLabel.style.paddingLeft = 14;
            subLabel.style.marginTop   = 2;

            item.Add(topRow);
            item.Add(subLabel);

            item.RegisterCallback<ClickEvent>(_ => SelectEntry(preset));
            item.RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (_selected != preset)
                    item.style.backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.22f));
            });
            item.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                if (_selected != preset)
                    item.style.backgroundColor = StyleKeyword.Null;
            });

            return item;
        }

        private VisualElement BuildRightPanel()
        {
            var panel = new VisualElement();
            panel.style.flexGrow      = 1;
            panel.style.flexDirection = FlexDirection.Column;

            // Placeholder
            _detailPlaceholder = new VisualElement();
            _detailPlaceholder.style.flexGrow       = 1;
            _detailPlaceholder.style.alignItems     = Align.Center;
            _detailPlaceholder.style.justifyContent = Justify.Center;
            var hint = new Label("← Выберите магазин из списка");
            hint.style.color    = new StyleColor(ColorGrey);
            hint.style.fontSize = 13;
            _detailPlaceholder.Add(hint);

            // Detail panel
            _detailPanel = new VisualElement();
            _detailPanel.style.display       = DisplayStyle.None;
            _detailPanel.style.flexDirection = FlexDirection.Column;
            _detailPanel.style.flexGrow      = 1;

            // Title (fixed header)
            _detailTitle = new Label();
            _detailTitle.style.fontSize                  = 16;
            _detailTitle.style.unityFontStyleAndWeight   = FontStyle.Bold;
            _detailTitle.style.paddingTop                = 16;
            _detailTitle.style.paddingBottom             = 12;
            _detailTitle.style.paddingLeft               = 16;
            _detailTitle.style.paddingRight              = 16;
            _detailTitle.style.borderBottomWidth         = 1;
            _detailTitle.style.borderBottomColor         = new StyleColor(ColorBorder);
            _detailPanel.Add(_detailTitle);

            // Scrollable content area
            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow                   = 1;
            scrollView.verticalScrollerVisibility       = ScrollerVisibility.Auto;
            scrollView.horizontalScrollerVisibility     = ScrollerVisibility.Hidden;

            var innerContent = new VisualElement();
            innerContent.style.flexDirection = FlexDirection.Column;
            innerContent.style.paddingTop    = 12;
            innerContent.style.paddingBottom = 8;
            innerContent.style.paddingLeft   = 16;
            innerContent.style.paddingRight  = 16;
            scrollView.Add(innerContent);

            // Info rows
            innerContent.Add(BuildInfoRow("Define:", out _detailDefine));
            innerContent.Add(BuildInfoRow("Платформа:", out _detailBuildTarget));

            // SDK section
            var sdkLabel = new Label("SDK");
            sdkLabel.style.fontSize                = 12;
            sdkLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            sdkLabel.style.color                   = new StyleColor(ColorGrey);
            sdkLabel.style.marginTop               = 8;
            sdkLabel.style.marginBottom            = 6;
            innerContent.Add(sdkLabel);

            _sdkChipsContainer = new VisualElement();
            _sdkChipsContainer.style.flexDirection = FlexDirection.Row;
            _sdkChipsContainer.style.flexWrap      = Wrap.Wrap;
            _sdkChipsContainer.style.marginBottom  = 4;
            innerContent.Add(_sdkChipsContainer);

            _sdkAddBtn = new Button(ShowAddSdkMenu) { text = "+ Добавить SDK" };
            _sdkAddBtn.style.fontSize          = 11;
            _sdkAddBtn.style.paddingTop        = 4;
            _sdkAddBtn.style.paddingBottom     = 4;
            _sdkAddBtn.style.paddingLeft       = 8;
            _sdkAddBtn.style.paddingRight      = 8;
            _sdkAddBtn.style.borderTopLeftRadius     = 3;
            _sdkAddBtn.style.borderTopRightRadius    = 3;
            _sdkAddBtn.style.borderBottomLeftRadius  = 3;
            _sdkAddBtn.style.borderBottomRightRadius = 3;
            innerContent.Add(_sdkAddBtn);

            // Adapters section
            _adaptersSection = new VisualElement();
            _adaptersSection.style.marginTop = 8;
            innerContent.Add(_adaptersSection);

            // Checklist section
            _checklistSection = BuildChecklistSection();
            innerContent.Add(_checklistSection);

            _detailPanel.Add(scrollView);

            // Fixed footer: separator + button
            var footer = new VisualElement();
            footer.style.flexDirection  = FlexDirection.Column;
            footer.style.paddingTop     = 0;
            footer.style.paddingBottom  = 16;
            footer.style.paddingLeft    = 16;
            footer.style.paddingRight   = 16;

            var sep = new VisualElement();
            sep.style.height          = 1;
            sep.style.marginTop       = 12;
            sep.style.marginBottom    = 12;
            sep.style.backgroundColor = new StyleColor(ColorBorder);
            footer.Add(sep);

            _selectBtn = new Button(OnSelectClicked) { text = "Выбрать" };
            _selectBtn.style.paddingLeft             = 20;
            _selectBtn.style.paddingRight            = 20;
            _selectBtn.style.paddingTop              = 8;
            _selectBtn.style.paddingBottom           = 8;
            _selectBtn.style.fontSize                = 12;
            _selectBtn.style.borderTopLeftRadius     = 4;
            _selectBtn.style.borderTopRightRadius    = 4;
            _selectBtn.style.borderBottomLeftRadius  = 4;
            _selectBtn.style.borderBottomRightRadius = 4;
            footer.Add(_selectBtn);

            _detailPanel.Add(footer);

            panel.Add(_detailPlaceholder);
            panel.Add(_detailPanel);
            return panel;
        }

        private VisualElement BuildInfoRow(string caption, out Label valueLabel)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems    = Align.Center;
            row.style.marginBottom  = 8;

            var lbl = new Label(caption);
            lbl.style.color    = new StyleColor(ColorGrey);
            lbl.style.fontSize = 12;
            lbl.style.minWidth = 100;
            lbl.style.maxWidth = 100;

            var val = new Label();
            val.style.fontSize = 12;

            row.Add(lbl);
            row.Add(val);
            valueLabel = val;
            return row;
        }

        // ── SDK chips ─────────────────────────────────────────────────────────

        private void RebuildSdkChips()
        {
            _sdkChipsContainer.Clear();
            if (_selected == null) return;

            if (_selected.sdkDefines != null)
            {
                foreach (var sdkDefine in _selected.sdkDefines)
                    _sdkChipsContainer.Add(BuildSdkChip(GetSdkDisplayName(sdkDefine), sdkDefine));
            }

            bool allAdded = _availableSdks.Count > 0 &&
                            _availableSdks.TrueForAll(
                                s => _selected.sdkDefines != null && _selected.sdkDefines.Contains(s.identifier));
            _sdkAddBtn.style.display = allAdded ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private bool IsSdkInstalled(string sdkIdentifier)
        {
            bool useManifest = false;
            foreach (var (id, _, manifest) in _availableSdks)
                if (id == sdkIdentifier) { useManifest = manifest; break; }

            if (!useManifest)
                return ScriptingDefinesManager.GetScriptingDefineSymbolsForSelectedBuildTarget()
                           .Contains(sdkIdentifier);

            var manifestPath = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");
            return File.Exists(manifestPath) &&
                   File.ReadAllText(manifestPath).Contains($"\"{sdkIdentifier}\":");
        }

        private VisualElement BuildSdkChip(string name, string sdkDefine)
        {
            var chip = new VisualElement();
            chip.style.flexDirection          = FlexDirection.Row;
            chip.style.alignItems             = Align.Center;
            bool installed = IsSdkInstalled(sdkDefine);
            chip.style.backgroundColor        = new StyleColor(installed ? ColorBgSdkInstalled : new Color(0.28f, 0.28f, 0.28f));
            chip.style.borderTopLeftRadius     = 3;
            chip.style.borderTopRightRadius    = 3;
            chip.style.borderBottomLeftRadius  = 3;
            chip.style.borderBottomRightRadius = 3;
            chip.style.paddingLeft             = 7;
            chip.style.paddingRight            = 4;
            chip.style.paddingTop              = 3;
            chip.style.paddingBottom           = 3;
            chip.style.marginRight             = 4;
            chip.style.marginBottom            = 4;

            var nameLabel = new Label(name);
            nameLabel.style.fontSize = 11;

            var removeBtn = new Button(() => RemoveSdk(sdkDefine)) { text = "×" };
            removeBtn.style.fontSize          = 11;
            removeBtn.style.marginLeft        = 4;
            removeBtn.style.paddingTop        = 0;
            removeBtn.style.paddingBottom     = 0;
            removeBtn.style.paddingLeft       = 3;
            removeBtn.style.paddingRight      = 3;
            removeBtn.style.borderTopWidth    = 0;
            removeBtn.style.borderBottomWidth = 0;
            removeBtn.style.borderLeftWidth   = 0;
            removeBtn.style.borderRightWidth  = 0;

            chip.Add(nameLabel);
            chip.Add(removeBtn);
            return chip;
        }

        private void ShowAddSdkMenu()
        {
            if (_selected == null) return;
            var menu = new GenericMenu();
            foreach (var (sdkIdentifier, sdkName, _) in _availableSdks)
            {
                if (_selected.sdkDefines != null && _selected.sdkDefines.Contains(sdkIdentifier))
                    continue;
                var captured = sdkIdentifier;
                menu.AddItem(new GUIContent(sdkName), false, () => AddSdk(captured));
            }
            menu.ShowAsContext();
        }

        private void AddSdk(string sdkDefine)
        {
            if (_selected == null) return;
            if (_selected.sdkDefines == null) _selected.sdkDefines = new List<string>();
            if (!_selected.sdkDefines.Contains(sdkDefine))
                _selected.sdkDefines.Add(sdkDefine);
            StorePresetsManager.Save(_presets);
            RebuildSdkChips();
        }

        private void RemoveSdk(string sdkDefine)
        {
            if (_selected == null) return;
            _selected.sdkDefines?.Remove(sdkDefine);
            StorePresetsManager.Save(_presets);
            RebuildSdkChips();
        }

        private string GetSdkDisplayName(string sdkDefine)
        {
            foreach (var (id, name, _) in _availableSdks)
                if (id == sdkDefine) return name;
            return sdkDefine;
        }

        // ── Selection ─────────────────────────────────────────────────────────

        private void SelectEntry(StorePreset preset)
        {
            foreach (var kvp in _listItems)
            {
                kvp.Value.style.backgroundColor = kvp.Key == preset
                    ? new StyleColor(ColorBgSel)
                    : StyleKeyword.Null;
            }

            _selected = preset;

            _detailTitle.text       = preset.displayName;
            _detailDefine.text      = preset.define ?? "";
            _detailBuildTarget.text = preset.buildTarget ?? "";
            RebuildSdkChips();
            RebuildAdaptersSection();
            RebuildChecklist();

            RefreshSelectButton();
            _detailPlaceholder.style.display = DisplayStyle.None;
            _detailPanel.style.display       = DisplayStyle.Flex;
        }

        private void RefreshSelectButton()
        {
            if (_selected == null || _selectBtn == null) return;
            bool active    = IsActive(_selected);
            bool hasDefine = !string.IsNullOrEmpty(_selected.define);
            _selectBtn.SetEnabled(hasDefine);
            _selectBtn.text = active ? "Применить" : "Выбрать";
        }

        // ── Select button handler ─────────────────────────────────────────────

        private void OnSelectClicked()
        {
            if (_selected == null) return;

            bool isEditorOnly = _selected.buildTarget == "Editor" || string.IsNullOrEmpty(_selected.buildTarget);
            BuildTarget target = BuildTarget.NoTarget;
            BuildTargetGroup targetGroup = BuildTargetGroup.Unknown;

            if (!isEditorOnly)
            {
                if (!System.Enum.TryParse<BuildTarget>(_selected.buildTarget, out target))
                {
                    EditorUtility.DisplayDialog("Ошибка",
                        $"Неверный BuildTarget: '{_selected.buildTarget}'", "OK");
                    return;
                }
                targetGroup = BuildPipeline.GetBuildTargetGroup(target);
            }

            bool active        = IsActive(_selected);
            string dialogTitle = active ? "Обновить настройки" : "Сменить магазин";
            string dialogMsg   = active
                ? $"Применить настройки для {_selected.displayName}?\n\n• Define: {_selected.define}"
                : isEditorOnly
                    ? $"Переключиться на {_selected.displayName}?\n\n• Define: {_selected.define}\n• Платформа не переключается (только для редактора)"
                    : $"Переключиться на {_selected.displayName}?\n\n• Define: {_selected.define}\n• Build Target: {_selected.buildTarget}";
            string dialogConfirm = active ? "Применить" : "Переключить";

            bool confirmed = EditorUtility.DisplayDialog(dialogTitle, dialogMsg, dialogConfirm, "Отмена");
            if (!confirmed) return;

            var previousDefine = StorePlatformDefines.GetCurrentStoreDefine();

            // Remove built-in SDK defines from previous store
            if (previousDefine != null)
            {
                var prevPreset = StorePresetsManager.Load().Find(p => p.define == previousDefine);
                if (prevPreset?.sdkDefines != null)
                    foreach (var sdk in prevPreset.sdkDefines)
                        if (IsBuiltInSdkDefine(sdk))
                            ScriptingDefinesManager.RemoveDefine(sdk);
            }

            StorePlatformDefines.SetStoreDefine(_selected.define);

            // Add built-in SDK defines for selected store
            if (_selected.sdkDefines != null)
                foreach (var sdk in _selected.sdkDefines)
                    if (IsBuiltInSdkDefine(sdk))
                        ScriptingDefinesManager.AddDefine(sdk);

            ScriptingDefinesManager.Flush();

            // Write preferred adapter to UniBridgeConfig (runtime reads this to select adapter)
            var adsConfig = EditorConfigHelper.EnsureProjectAsset<UniBridgeConfig>(nameof(UniBridgeConfig));
            if (adsConfig != null)
            {
                adsConfig.PreferredAdsAdapter = _selected.adsAdapter;
                EditorUtility.SetDirty(adsConfig);
                AssetDatabase.SaveAssets();
            }

            // Write preferred purchase adapter to UniBridgePurchasesConfig (runtime reads this to select adapter)
            var purchasesConfig = EditorConfigHelper.EnsureProjectAsset<UniBridgePurchasesConfig>(nameof(UniBridgePurchasesConfig));
            if (purchasesConfig != null)
            {
                purchasesConfig.PreferredPurchaseAdapter = _selected.purchasesAdapter;
                EditorUtility.SetDirty(purchasesConfig);
                AssetDatabase.SaveAssets();
            }

            // Write preferred leaderboard adapter to UniBridgeLeaderboardsConfig (runtime reads this to select adapter)
            var leaderboardsConfig = EditorConfigHelper.EnsureProjectAsset<UniBridgeLeaderboardsConfig>(nameof(UniBridgeLeaderboardsConfig));
            if (leaderboardsConfig != null)
            {
                leaderboardsConfig.PreferredLeaderboardAdapter = _selected.leaderboardsAdapter;
                EditorUtility.SetDirty(leaderboardsConfig);
                AssetDatabase.SaveAssets();
            }

            // Write preferred rate adapter to UniBridgeRateConfig (runtime reads this to select adapter)
            var rateConfig = EditorConfigHelper.EnsureProjectAsset<UniBridgeRateConfig>(nameof(UniBridgeRateConfig));
            if (rateConfig != null)
            {
                rateConfig.PreferredRateAdapter = _selected.rateAdapter;
                EditorUtility.SetDirty(rateConfig);
                AssetDatabase.SaveAssets();
            }

            // Write preferred saves adapter to UniBridgeSavesConfig (runtime reads this to select adapter)
            var savesConfig = EditorConfigHelper.EnsureProjectAsset<UniBridgeSavesConfig>(nameof(UniBridgeSavesConfig));
            if (savesConfig != null)
            {
                savesConfig.PreferredSavesAdapter = _selected.savesAdapter ?? "";
                EditorUtility.SetDirty(savesConfig);
                AssetDatabase.SaveAssets();
            }

            // Write preferred auth adapter to UniBridgeAuthConfig (runtime reads this to select adapter)
            var authConfig = EditorConfigHelper.EnsureProjectAsset<UniBridgeAuthConfig>(nameof(UniBridgeAuthConfig));
            if (authConfig != null)
            {
                authConfig.PreferredAuthAdapter = _selected.authAdapter;
                EditorUtility.SetDirty(authConfig);
                AssetDatabase.SaveAssets();
            }

            // Write preferred analytics adapter to UniBridgeAnalyticsConfig
            var analyticsConfig = EditorConfigHelper.EnsureProjectAsset<UniBridgeAnalyticsConfig>(nameof(UniBridgeAnalyticsConfig));
            if (analyticsConfig != null)
            {
                analyticsConfig.PreferredAnalyticsAdapter = _selected.analyticsAdapter;
                EditorUtility.SetDirty(analyticsConfig);
                AssetDatabase.SaveAssets();
            }

            string shareAdapter = _selected.shareAdapter ?? "";
            bool wantShareEnabled = !string.IsNullOrEmpty(shareAdapter) &&
                                    shareAdapter != "UNIBRIDGESHARE_MOCK" &&
                                    shareAdapter != AdapterDefines.NoneAdapterKey;

            SessionState.SetString("UniBridge_PendingShareAdapter", shareAdapter);
            ApplyShareConfig(shareAdapter);

            StoreAndroidConfigurator.OnStoreChanged(previousDefine, _selected.define, wantShareEnabled);

            if (_selected.define == StorePlatformDefines.STORE_YOUTUBE)
                YouTubePlayablesTemplateInstaller.EnsureInstalled();

            if (!isEditorOnly && EditorUserBuildSettings.activeBuildTarget != target)
                EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, target);

            var current = _selected;
            ReorderPresets();
            RebuildList();
            SelectEntry(current);
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            string pending = SessionState.GetString("UniBridge_PendingShareAdapter", "");
            if (string.IsNullOrEmpty(pending)) return;
            SessionState.EraseString("UniBridge_PendingShareAdapter");
            ApplyShareConfig(pending);
        }

        // Writes PreferredShareAdapter via SerializedObject — without a direct reference to the UniBridgeShareConfig type
        private static void ApplyShareConfig(string adapter)
        {
            const string assetPath = "Assets/UniBridge/Resources/UniBridgeShareConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (config == null)
            {
                var configType = System.Type.GetType("UniBridge.UniBridgeShareConfig, UniBridge.Share.Runtime");
                if (configType == null) return;

                if (!AssetDatabase.IsValidFolder("Assets/UniBridge"))
                    AssetDatabase.CreateFolder("Assets", "UniBridge");
                if (!AssetDatabase.IsValidFolder("Assets/UniBridge/Resources"))
                    AssetDatabase.CreateFolder("Assets/UniBridge", "Resources");

                config = ScriptableObject.CreateInstance(configType);
                AssetDatabase.CreateAsset(config, assetPath);
                AssetDatabase.SaveAssets();
                config = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            }
            if (config == null) return;
            var so   = new SerializedObject(config);
            var prop = so.FindProperty("PreferredShareAdapter");
            if (prop == null) return;
            prop.stringValue = adapter;
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        // ── Checklist ─────────────────────────────────────────────────────────

        private VisualElement BuildChecklistSection()
        {
            var section = new VisualElement();
            section.style.marginTop = 12;
            section.style.display   = DisplayStyle.None;

            _checklistItemsContainer = new VisualElement();
            _checklistItemsContainer.style.flexDirection = FlexDirection.Column;
            section.Add(_checklistItemsContainer);

            return section;
        }

        private void RebuildChecklist()
        {
            _checklistItemsContainer.Clear();
            bool hasAny = false;

            var storeList = ChecklistRegistry.GetStoreChecklist(_selected?.define);
            if (storeList != null)
            {
                var storeItems = storeList.GetItems();
                if (storeItems.Length > 0)
                {
                    _checklistItemsContainer.Add(ChecklistUIHelper.BuildGroup(storeList.Title, storeItems));
                    hasAny = true;
                }
            }

            if (_selected?.sdkDefines != null)
            {
                foreach (var sdk in _selected.sdkDefines)
                {
                    if (!IsSdkInstalled(sdk)) continue;
                    var sdkList = ChecklistRegistry.GetSdkChecklist(sdk);
                    if (sdkList != null)
                    {
                        _checklistItemsContainer.Add(ChecklistUIHelper.BuildGroup(sdkList.Title, sdkList.GetItems()));
                        hasAny = true;

                        if (sdk == "UNIBRIDGE_YTPLAYABLES")
                        {
                            var btn = new Button(() => YouTubePlayablesTemplateWindow.Open())
                                { text = "Настроить шаблон" };
                            btn.style.marginLeft = 14;
                            btn.style.marginTop = 4;
                            btn.style.marginBottom = 4;
                            _checklistItemsContainer.Add(btn);
                        }
                    }
                }
            }

            _checklistSection.style.display = hasAny ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ── Adapters section ──────────────────────────────────────────────────

        private void RebuildAdaptersSection()
        {
            _adaptersSection.Clear();
            if (_selected == null) return;

            var adAdapters          = new List<string>(AdapterDefines.GetAdAdapters(_selected.buildTarget));
            var purchaseAdapters    = new List<string>(AdapterDefines.GetPurchaseAdapters(_selected.define));
            var leaderboardAdapters = new List<string>(AdapterDefines.GetLeaderboardAdapters(_selected.define));
            var rateAdapters        = new List<string>(AdapterDefines.GetRateAdapters(_selected.define));
            var shareAdapters       = new List<string>(AdapterDefines.GetShareAdapters(_selected.define));
            var saveAdapters        = new List<string>(AdapterDefines.GetSaveAdapters(_selected.define));
            var analyticsAdapters   = new List<string>(AdapterDefines.GetAnalyticsAdapters(_selected.define));

            if (adAdapters.Count == 0 && purchaseAdapters.Count == 0 && leaderboardAdapters.Count == 0 && rateAdapters.Count == 0) return;

            var installedDefines = new HashSet<string>(
                ScriptingDefinesManager.GetScriptingDefineSymbolsForSelectedBuildTarget());

            var header = new Label("Адаптеры");
            header.style.fontSize                = 12;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color                   = new StyleColor(ColorGrey);
            header.style.marginBottom            = 6;
            _adaptersSection.Add(header);

            if (adAdapters.Count > 0)
                _adaptersSection.Add(BuildAdapterRow("Реклама:", adAdapters, _selected.adsAdapter,
                    installedDefines,
                    v => { _selected.adsAdapter = v; StorePresetsManager.Save(_presets); }));

            if (purchaseAdapters.Count > 0)
                _adaptersSection.Add(BuildAdapterRow("Покупки:", purchaseAdapters, _selected.purchasesAdapter,
                    installedDefines,
                    v => { _selected.purchasesAdapter = v; StorePresetsManager.Save(_presets); }));

            if (leaderboardAdapters.Count > 0)
                _adaptersSection.Add(BuildAdapterRow("Лидерборды:", leaderboardAdapters, _selected.leaderboardsAdapter,
                    installedDefines,
                    v => { _selected.leaderboardsAdapter = v; StorePresetsManager.Save(_presets); }));

            if (rateAdapters.Count > 0)
                _adaptersSection.Add(BuildAdapterRow("Оценка:", rateAdapters, _selected.rateAdapter,
                    installedDefines,
                    v => { _selected.rateAdapter = v; StorePresetsManager.Save(_presets); }));

            if (shareAdapters.Count > 0)
                _adaptersSection.Add(BuildAdapterRow("Шаринг:", shareAdapters, _selected.shareAdapter,
                    installedDefines,
                    v => { _selected.shareAdapter = v; StorePresetsManager.Save(_presets); }));

            if (saveAdapters.Count > 0)
                _adaptersSection.Add(BuildAdapterRow("Сохранения:", saveAdapters, _selected.savesAdapter,
                    installedDefines,
                    v => { _selected.savesAdapter = v; StorePresetsManager.Save(_presets); }));

            if (analyticsAdapters.Count > 0)
                _adaptersSection.Add(BuildAdapterRow("Аналитика:", analyticsAdapters, _selected.analyticsAdapter,
                    installedDefines,
                    v => { _selected.analyticsAdapter = v; StorePresetsManager.Save(_presets); }));

            var authAdapters = new List<string>(AdapterDefines.GetAuthAdapters(_selected.define));
            if (authAdapters.Count > 0)
                _adaptersSection.Add(BuildAdapterRow("Авторизация:", authAdapters, _selected.authAdapter,
                    installedDefines,
                    v => { _selected.authAdapter = v; StorePresetsManager.Save(_presets); }));
        }

        private VisualElement BuildAdapterRow(string caption, List<string> adapters, string current,
            HashSet<string> installedDefines, System.Action<string> onChange)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems    = Align.Center;
            row.style.marginBottom  = 6;

            var lbl = new Label(caption);
            lbl.style.color    = new StyleColor(ColorGrey);
            lbl.style.fontSize = 12;
            lbl.style.minWidth = 74;

            row.Add(lbl);

            if (adapters.Count == 1)
            {
                // Single option — show as label (no choice to make)
                var val = new Label(GetAdapterDisplayName(adapters[0]));
                val.style.fontSize = 12;
                val.style.color    = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
                row.Add(val);
            }
            else
            {
                // Multiple options — show dropdown; mark uninstalled adapters
                var choices     = new System.Collections.Generic.List<string>();
                int selectedIdx = 0;
                for (int i = 0; i < adapters.Count; i++)
                {
                    bool installed = IsAdapterSdkInstalled(adapters[i], installedDefines);
                    string label   = GetAdapterDisplayName(adapters[i]);
                    choices.Add(installed ? label : label + " (не установлен)");
                    if (adapters[i] == current) selectedIdx = i;
                }

                var dropdown = new DropdownField(choices, selectedIdx);
                dropdown.style.minWidth = 160;
                dropdown.RegisterValueChangedCallback(evt =>
                {
                    int idx = choices.IndexOf(evt.newValue);
                    if (idx >= 0) onChange(adapters[idx]);
                });

                row.Add(dropdown);
            }

            return row;
        }

        /// <summary>
        /// SDK defines that are bundled with UniBridge (no external package to install)
        /// but still need to be real scripting defines for asmdef defineConstraints.
        /// </summary>
        private static bool IsBuiltInSdkDefine(string sdk)
        {
            if (string.IsNullOrEmpty(sdk)) return false;
            return sdk == "UNIBRIDGE_YTPLAYABLES";
        }

        private static bool IsAdapterSdkInstalled(string sdkDefine, HashSet<string> installedDefines)
        {
            if (sdkDefine == AdapterDefines.NoneAdapterKey) return true; // виртуальный, система отключена
            if (sdkDefine == "UNIBRIDGELEADERBOARDS_SIMULATED") return true; // виртуальный, всегда доступен
            if (sdkDefine == "UNITY_IOS_GAMECENTER")      return true; // встроенный в iOS, всегда доступен
            if (sdkDefine == "UNITY_IOS_STOREREVIEW")     return true; // встроен в Unity, всегда доступен
            if (sdkDefine == "UNIBRIDGERATE_MOCK")              return true; // заглушка, всегда доступна
            if (sdkDefine == "UNIBRIDGEAUTH_MOCK")              return true; // заглушка, всегда доступна
            if (sdkDefine == "UNIBRIDGESHARE_ANDROID")          return true; // нативный Android, всегда доступен
            if (sdkDefine == "UNIBRIDGESHARE_IOS")              return true; // нативный iOS, всегда доступен
            if (sdkDefine == "UNIBRIDGESHARE_MOCK")             return true; // заглушка, всегда доступна
            if (sdkDefine == "UNIBRIDGESAVES_SIMULATED")        return true; // симуляция, всегда доступна
            if (sdkDefine == "UNITY_IOS_ICLOUD")          return true; // встроенный iOS, всегда доступен
            if (sdkDefine == "UNIBRIDGE_YTPLAYABLES")     return true; // YouTube Playables SDK загружается через <script> tag, всегда доступен
            if (sdkDefine == "UNIBRIDGESAVES_GPGS")             return installedDefines.Contains("UNIBRIDGELEADERBOARDS_GPGS"); // требует GPGS
            return installedDefines.Contains(sdkDefine);
        }

        private static string GetAdapterDisplayName(string sdkDefine)
        {
            if (AdapterDefines.AdsAdapterNames.TryGetValue(sdkDefine, out var name))
                return name;
            if (AdapterDefines.PurchaseAdapterNames.TryGetValue(sdkDefine, out name))
                return name;
            if (AdapterDefines.LeaderboardAdapterNames.TryGetValue(sdkDefine, out name))
                return name;
            if (AdapterDefines.RateAdapterNames.TryGetValue(sdkDefine, out name))
                return name;
            if (AdapterDefines.ShareAdapterNames.TryGetValue(sdkDefine, out name))
                return name;
            if (AdapterDefines.SaveAdapterNames.TryGetValue(sdkDefine, out name))
                return name;
            if (AdapterDefines.AnalyticsAdapterNames.TryGetValue(sdkDefine, out name))
                return name;
            if (AdapterDefines.AuthAdapterNames.TryGetValue(sdkDefine, out name))
                return name;
            return sdkDefine;
        }

        // ── List rebuild ──────────────────────────────────────────────────────

        private void RebuildList()
        {
            _listContainer.Clear();
            _listItems.Clear();
            foreach (var p in _presets)
            {
                var item = BuildListItem(p);
                _listItems[p] = item;
                _listContainer.Add(item);
            }
        }
    }
}
