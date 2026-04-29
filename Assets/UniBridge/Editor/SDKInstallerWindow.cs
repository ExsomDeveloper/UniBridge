using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace UniBridge.Editor
{
    public class SDKInstallerWindow : EditorWindow
    {
        // ── Data model ──────────────────────────────────────────────────────

        private class SDKEntry
        {
            public string DisplayName;
            public string RequiredVersion;
            public string InstalledVersion; // null = not installed
            public string PackageId;        // UPM registry ID, may be empty
            public string GitUrl;           // git URL or download URL, may be empty
            public string CoreUrl;          // dependency package URL to install before GitUrl (e.g. ru.rustore.core)
            public string CorePackageId;    // UPM name of the dependency package (e.g. ru.rustore.core)
            public bool   IsCoreInstalled;  // populated during OnListProgress; true → skip core install step
            public string Define;           // e.g. UNIBRIDGE_LEVELPLAY
            public string RegistryUrl;      // scoped registry URL (e.g. OpenUPM), empty = not needed
            public string RegistryScopes;   // semicolon-separated scopes, e.g. "com.yandex;com.google"
            public string SdkKey;           // key in SDKVersions.json (e.g. "levelplay", "playgama")
            public string LatestVersion;    // latest upstream version from cache, null = unknown

            public bool IsInstalled => InstalledVersion != null;
            public bool NeedsUpdate => IsInstalled && InstalledVersion != RequiredVersion;
            public bool HasNewerUpstream => !string.IsNullOrEmpty(LatestVersion) &&
                                            LatestVersion != RequiredVersion &&
                                            IsVersionNewer(LatestVersion, RequiredVersion);
            // True if we can rewrite the install spec to point at LatestVersion:
            // empty GitUrl → registry install (packageId@version always works);
            // non-empty GitUrl → only if it ends with a #vX.Y.Z tag we can substitute
            // (excludes RuStore's gitflic download URLs which have opaque hashes).
            public bool CanInstallLatest =>
                HasNewerUpstream &&
                (string.IsNullOrEmpty(GitUrl) || Regex.IsMatch(GitUrl, @"#v\d+(?:\.\d+)*$"));
            public string Source    => !string.IsNullOrEmpty(PackageId) ? PackageId : GitUrl;

            private static bool IsVersionNewer(string a, string b)
            {
                if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
                var pa = a.Split('.');
                var pb = b.Split('.');
                int len = Math.Max(pa.Length, pb.Length);
                for (int i = 0; i < len; i++)
                {
                    int va = i < pa.Length && int.TryParse(pa[i], out var x) ? x : 0;
                    int vb = i < pb.Length && int.TryParse(pb[i], out var y) ? y : 0;
                    if (va > vb) return true;
                    if (va < vb) return false;
                }
                return false;
            }
        }

        private class MediationEntry
        {
            public string DisplayName;
            public string AdapterKey;
            public string Version;
            public string DetectionFile;
            public bool   IsInstalled;

            public string DownloadUrl =>
                $"https://github.com/yandexmobile/yandex-ads-unity-plugin/releases/download/{Version}/mobileads-{AdapterKey}-mediation-{Version}.unitypackage";
        }

        // ── State ────────────────────────────────────────────────────────────

        private readonly List<SDKEntry>      _entries          = new();
        private readonly List<MediationEntry> _mediationEntries = new();
        private SDKEntry      _selected;
        private bool _isOperating;

        private ListRequest   _listRequest;
        private AddRequest    _addRequest;
        private RemoveRequest _removeRequest;
        private SDKEntry      _pendingEntry;
        private Action        _nextInstallAction; // chain: called instead of completion when set
        private Action        _nextRemoveAction;  // chain: called after main package removal (e.g. remove core)
        private bool          _isRemovingCoreDep; // true while executing the core-removal step of a chain

        // SessionState keys for surviving domain reload mid-chain-install
        private const string PendingChainUrlKey  = "UniBridge_PendingChainInstall_Url";
        private const string PendingChainNameKey = "UniBridge_PendingChainInstall_Name";

        private VisualElement  _mediationSection;
        private UnityWebRequest _activeWebRequest;
        private MediationEntry  _pendingMediation;

        // ── UI references ────────────────────────────────────────────────────

        private VisualElement         _listContainer;
        private VisualElement         _detailPlaceholder;
        private VisualElement         _detailPanel;
        private Label                 _detailTitle;
        private Label                 _detailSource;
        private Label                 _detailDefine;
        private Label                 _detailRequired;
        private Label                 _detailInstalled;
        private Label                 _detailLatest;
        private Label                 _statusLabel;
        private VisualElement         _sdkChecklistSection;
        private Button                _installBtn;
        private Button                _updateBtn;
        private Button                _latestBtn;
        private Button                _removeBtn;
        private Button                _installFromFileBtn;

        // ── Colors ───────────────────────────────────────────────────────────

        private static readonly Color ColorGreen  = new(0.25f, 0.80f, 0.25f);
        private static readonly Color ColorYellow = new(0.90f, 0.75f, 0.15f);
        private static readonly Color ColorGrey   = new(0.55f, 0.55f, 0.55f);
        private static readonly Color ColorBlue   = new(0.40f, 0.65f, 0.95f);
        private static readonly Color ColorBorder = new(0.15f, 0.15f, 0.15f);
        private static readonly Color ColorBg     = new(0.18f, 0.18f, 0.18f);
        private static readonly Color ColorBgSel  = new(0.24f, 0.37f, 0.57f);

        // ── Entry point ──────────────────────────────────────────────────────

        [MenuItem("UniBridge/SDK Installer", false, 55)]
        public static void ShowWindow()
        {
            var w = GetWindow<SDKInstallerWindow>("SDK Installer");
            w.minSize = new Vector2(600, 380);
            w.Show();
        }

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void CreateGUI()
        {
            LoadEntries();
            BuildUI();
            StartRefresh();
        }

        private void OnEnable()
        {
            // After domain reload CreateGUI may not fire immediately.
            // Schedule a refresh as a safety net.
            EditorApplication.delayCall += EnsureRefreshed;
            SDKUpdateChecker.OnCheckComplete += OnUpdateCheckComplete;
        }

        private void EnsureRefreshed()
        {
            if (_isOperating) return;
            // Reset potentially stale request left over from domain reload
            EditorApplication.update -= OnListProgress;
            _listRequest = null;
            StartRefresh();
        }

        private void OnDisable()
        {
            EditorApplication.delayCall -= EnsureRefreshed;
            EditorApplication.update -= OnListProgress;
            EditorApplication.update -= OnAddProgress;
            EditorApplication.update -= OnRemoveProgress;
            SDKUpdateChecker.OnCheckComplete -= OnUpdateCheckComplete;
            _activeWebRequest?.Abort();
            _activeWebRequest?.Dispose();
            _activeWebRequest = null;
        }

        private void OnUpdateCheckComplete()
        {
            // Refresh latest versions from cache when background check finishes
            var latestVersions = SDKUpdateChecker.GetAllLatestVersions();
            foreach (var entry in _entries)
            {
                if (!string.IsNullOrEmpty(entry.SdkKey) && latestVersions.TryGetValue(entry.SdkKey, out var latest))
                    entry.LatestVersion = latest;
            }
            foreach (var e in _entries) RefreshListItem(e);
            RefreshDetailPanel();
        }

        // ── Data loading ─────────────────────────────────────────────────────

        private void LoadEntries()
        {
            _entries.Clear();
            var versions = LoadRequiredVersions();
            if (versions == null) return;

            if (versions.levelplay != null)
                _entries.Add(new SDKEntry
                {
                    SdkKey          = "levelplay",
                    DisplayName     = !string.IsNullOrEmpty(versions.levelplay.displayName) ? versions.levelplay.displayName : "LevelPlay",
                    RequiredVersion = versions.levelplay.version,
                    PackageId       = versions.levelplay.packageId,
                    GitUrl          = versions.levelplay.gitUrl,
                    Define          = versions.levelplay.define ?? ""
                });

            if (versions.playgama != null)
                _entries.Add(new SDKEntry
                {
                    SdkKey          = "playgama",
                    DisplayName     = !string.IsNullOrEmpty(versions.playgama.displayName) ? versions.playgama.displayName : "Playgama",
                    RequiredVersion = versions.playgama.version,
                    PackageId       = versions.playgama.packageId,
                    GitUrl          = versions.playgama.gitUrl,
                    Define          = versions.playgama.define ?? ""
                });

            if (versions.edm4u != null)
                _entries.Add(new SDKEntry
                {
                    SdkKey          = "edm4u",
                    DisplayName     = !string.IsNullOrEmpty(versions.edm4u.displayName) ? versions.edm4u.displayName : "EDM4U",
                    RequiredVersion = versions.edm4u.version,
                    PackageId       = versions.edm4u.packageId,
                    GitUrl          = versions.edm4u.gitUrl,
                    Define          = versions.edm4u.define ?? ""
                });

            if (versions.yandex != null)
                _entries.Add(new SDKEntry
                {
                    SdkKey          = "yandex",
                    DisplayName     = !string.IsNullOrEmpty(versions.yandex.displayName) ? versions.yandex.displayName : "Yandex Mobile Ads",
                    RequiredVersion = versions.yandex.version,
                    PackageId       = versions.yandex.packageId,
                    GitUrl          = versions.yandex.gitUrl,
                    Define          = versions.yandex.define ?? "",
                    RegistryUrl     = versions.yandex.registryUrl ?? "",
                    RegistryScopes  = versions.yandex.registryScopes ?? ""
                });

            if (versions.unityiap != null)
                _entries.Add(new SDKEntry
                {
                    SdkKey          = "unityiap",
                    DisplayName     = !string.IsNullOrEmpty(versions.unityiap.displayName) ? versions.unityiap.displayName : "Unity IAP",
                    RequiredVersion = versions.unityiap.version,
                    PackageId       = versions.unityiap.packageId,
                    GitUrl          = versions.unityiap.gitUrl,
                    Define          = versions.unityiap.define ?? ""
                });

            if (versions.rustore != null)
                _entries.Add(new SDKEntry
                {
                    SdkKey          = "rustore",
                    DisplayName     = !string.IsNullOrEmpty(versions.rustore.displayName) ? versions.rustore.displayName : "RuStore Pay",
                    RequiredVersion = versions.rustore.version,
                    PackageId       = versions.rustore.packageId,
                    GitUrl          = versions.rustore.gitUrl,
                    CoreUrl         = versions.rustore.coreUrl     ?? "",
                    CorePackageId   = versions.rustore.corePackageId ?? "",
                    Define          = versions.rustore.define ?? ""
                });

            if (versions.gpgs != null)
                _entries.Add(new SDKEntry
                {
                    SdkKey          = "gpgs",
                    DisplayName     = !string.IsNullOrEmpty(versions.gpgs.displayName) ? versions.gpgs.displayName : "Google Play Games Services",
                    RequiredVersion = versions.gpgs.version,
                    PackageId       = versions.gpgs.packageId,
                    GitUrl          = versions.gpgs.gitUrl,
                    Define          = versions.gpgs.define ?? ""
                });

            if (versions.googlePlayReview != null)
                _entries.Add(new SDKEntry
                {
                    SdkKey          = "googlePlayReview",
                    DisplayName     = !string.IsNullOrEmpty(versions.googlePlayReview.displayName) ? versions.googlePlayReview.displayName : "Google Play Review",
                    RequiredVersion = versions.googlePlayReview.version,
                    PackageId       = versions.googlePlayReview.packageId,
                    GitUrl          = versions.googlePlayReview.gitUrl,
                    Define          = versions.googlePlayReview.define ?? "",
                    RegistryUrl     = versions.googlePlayReview.registryUrl ?? "",
                    RegistryScopes  = versions.googlePlayReview.registryScopes ?? ""
                });

            if (versions.rustoreReview != null)
                _entries.Add(new SDKEntry
                {
                    SdkKey          = "rustoreReview",
                    DisplayName     = !string.IsNullOrEmpty(versions.rustoreReview.displayName) ? versions.rustoreReview.displayName : "RuStore Review",
                    RequiredVersion = versions.rustoreReview.version,
                    PackageId       = versions.rustoreReview.packageId,
                    GitUrl          = versions.rustoreReview.gitUrl,
                    CoreUrl         = versions.rustoreReview.coreUrl     ?? "",
                    CorePackageId   = versions.rustoreReview.corePackageId ?? "",
                    Define          = versions.rustoreReview.define ?? ""
                });

            if (versions.appmetrica != null)
                _entries.Add(new SDKEntry
                {
                    SdkKey          = "appmetrica",
                    DisplayName     = !string.IsNullOrEmpty(versions.appmetrica.displayName) ? versions.appmetrica.displayName : "AppMetrica",
                    RequiredVersion = versions.appmetrica.version,
                    PackageId       = versions.appmetrica.packageId,
                    GitUrl          = versions.appmetrica.gitUrl,
                    Define          = versions.appmetrica.define ?? ""
                });

            // Populate latest upstream versions from cache
            var latestVersions = SDKUpdateChecker.GetAllLatestVersions();
            foreach (var entry in _entries)
            {
                if (!string.IsNullOrEmpty(entry.SdkKey) && latestVersions.TryGetValue(entry.SdkKey, out var latest))
                    entry.LatestVersion = latest;
            }

            _mediationEntries.Clear();
            if (versions.yandexMediationAdapters != null)
                foreach (var info in versions.yandexMediationAdapters)
                    _mediationEntries.Add(new MediationEntry
                    {
                        DisplayName   = info.displayName,
                        AdapterKey    = info.adapterKey,
                        Version       = info.version,
                        DetectionFile = info.detectionFile
                    });
        }

        // ── UI construction ──────────────────────────────────────────────────

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

            // Header
            var header = new Label("SDKs");
            header.style.paddingTop    = 10;
            header.style.paddingBottom = 8;
            header.style.paddingLeft   = 12;
            header.style.paddingRight  = 12;
            header.style.fontSize      = 12;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new StyleColor(ColorBorder);
            panel.Add(header);

            // Refresh button — absolute so scroll view can't cover it
            var refreshBtn = new Button(() =>
            {
                if (_isOperating) return;
                // Force-reset any stuck list request and start fresh
                EditorApplication.update -= OnListProgress;
                _listRequest = null;
                StartRefresh();
            }) { text = "↺" };
            refreshBtn.style.position      = Position.Absolute;
            refreshBtn.style.right         = 6;
            refreshBtn.style.top           = 4;
            refreshBtn.style.paddingLeft   = 6;
            refreshBtn.style.paddingRight  = 6;
            refreshBtn.style.paddingTop    = 2;
            refreshBtn.style.paddingBottom = 2;
            refreshBtn.style.fontSize      = 14;
            refreshBtn.style.color         = new StyleColor(ColorGrey);
            refreshBtn.style.backgroundColor     = new StyleColor(new Color(0.28f, 0.28f, 0.28f));
            refreshBtn.style.borderTopWidth      = 0;
            refreshBtn.style.borderBottomWidth   = 0;
            refreshBtn.style.borderLeftWidth     = 0;
            refreshBtn.style.borderRightWidth    = 0;
            refreshBtn.style.borderTopLeftRadius     = 3;
            refreshBtn.style.borderTopRightRadius    = 3;
            refreshBtn.style.borderBottomLeftRadius  = 3;
            refreshBtn.style.borderBottomRightRadius = 3;
            // Scroll list
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;

            _listContainer = scroll.contentContainer;
            _listContainer.style.flexDirection = FlexDirection.Column;

            foreach (var entry in _entries)
                _listContainer.Add(BuildListItem(entry));

            panel.Add(scroll);

            // Add refresh button last so it renders on top of scroll
            panel.Add(refreshBtn);
            return panel;
        }

        private VisualElement BuildListItem(SDKEntry entry)
        {
            var item = new VisualElement();
            item.name = "item_" + entry.DisplayName;
            item.style.paddingTop    = 8;
            item.style.paddingBottom = 8;
            item.style.paddingLeft   = 12;
            item.style.paddingRight  = 10;
            item.style.flexDirection = FlexDirection.Column;

            // Top row: name + badge dot
            var topRow = new VisualElement();
            topRow.style.flexDirection = FlexDirection.Row;
            topRow.style.alignItems    = Align.Center;

            var dot = new VisualElement();
            dot.name = "dot_" + entry.DisplayName;
            dot.style.width            = 8;
            dot.style.height           = 8;
            dot.style.borderTopLeftRadius     = 4;
            dot.style.borderTopRightRadius    = 4;
            dot.style.borderBottomLeftRadius  = 4;
            dot.style.borderBottomRightRadius = 4;
            dot.style.marginRight      = 6;
            dot.style.backgroundColor  = new StyleColor(ColorGrey);

            var nameLabel = new Label(entry.DisplayName);
            nameLabel.style.fontSize             = 12;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.flexGrow             = 1;

            topRow.Add(dot);
            topRow.Add(nameLabel);

            // Bottom row: version / status text
            var versionLabel = new Label("Проверка...");
            versionLabel.name = "version_" + entry.DisplayName;
            versionLabel.style.fontSize   = 10;
            versionLabel.style.color      = new StyleColor(ColorGrey);
            versionLabel.style.paddingLeft = 14;
            versionLabel.style.marginTop  = 2;

            item.Add(topRow);
            item.Add(versionLabel);

            item.RegisterCallback<ClickEvent>(_ => SelectEntry(entry));
            item.RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (_selected != entry)
                    item.style.backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.22f));
            });
            item.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                if (_selected != entry)
                    item.style.backgroundColor = StyleKeyword.Null;
            });

            return item;
        }

        private VisualElement BuildRightPanel()
        {
            var panel = new VisualElement();
            panel.style.flexGrow      = 1;
            panel.style.paddingTop    = 16;
            panel.style.paddingBottom = 16;
            panel.style.paddingLeft   = 16;
            panel.style.paddingRight  = 16;
            panel.style.flexDirection = FlexDirection.Column;

            // Placeholder
            _detailPlaceholder = new VisualElement();
            _detailPlaceholder.style.flexGrow      = 1;
            _detailPlaceholder.style.alignItems    = Align.Center;
            _detailPlaceholder.style.justifyContent = Justify.Center;
            var hint = new Label("← Выберите SDK из списка");
            hint.style.color    = new StyleColor(ColorGrey);
            hint.style.fontSize = 13;
            _detailPlaceholder.Add(hint);

            // Detail panel (hidden initially)
            _detailPanel = new VisualElement();
            _detailPanel.style.display = DisplayStyle.None;
            _detailPanel.style.flexDirection = FlexDirection.Column;
            _detailPanel.style.flexGrow = 1;

            _detailTitle = new Label();
            _detailTitle.style.fontSize             = 16;
            _detailTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            _detailTitle.style.marginBottom         = 16;

            _detailPanel.Add(_detailTitle);
            _detailPanel.Add(BuildInfoRow("Источник:",  out _detailSource));
            _detailPanel.Add(BuildInfoRow("Define:",    out _detailDefine));
            _detailPanel.Add(BuildInfoRow("Требуется:", out _detailRequired));
            _detailPanel.Add(BuildInfoRow("Установлен:", out _detailInstalled));
            _detailPanel.Add(BuildInfoRow("Последняя:",  out _detailLatest));

            // Status label
            _statusLabel = new Label();
            _statusLabel.style.marginTop = 12;
            _statusLabel.style.fontSize  = 11;
            _statusLabel.style.color     = new StyleColor(ColorGrey);
            _detailPanel.Add(_statusLabel);

            // SDK checklist section
            _sdkChecklistSection = new VisualElement();
            _sdkChecklistSection.style.marginTop = 8;
            _sdkChecklistSection.style.display   = DisplayStyle.None;
            _detailPanel.Add(_sdkChecklistSection);

            // Mediation adapters section (only visible when Yandex is selected)
            _mediationSection = new VisualElement();
            _mediationSection.style.display   = DisplayStyle.None;
            _mediationSection.style.marginTop = 12;
            _detailPanel.Add(_mediationSection);

            // Separator
            var sep = new VisualElement();
            sep.style.height          = 1;
            sep.style.marginTop       = 16;
            sep.style.marginBottom    = 16;
            sep.style.backgroundColor = new StyleColor(ColorBorder);
            _detailPanel.Add(sep);

            // Buttons
            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.flexWrap      = Wrap.Wrap;

            _installBtn         = CreateButton("Установить",    ColorGreen,                     OnInstallClicked);
            _updateBtn          = CreateButton("Обновить",      ColorYellow,                    OnUpdateClicked);
            _latestBtn          = CreateButton("Последняя",     ColorBlue,                      OnLatestClicked);
            _removeBtn          = CreateButton("Удалить",       new Color(0.8f, 0.3f, 0.3f),   OnRemoveClicked);
            _installFromFileBtn = CreateButton("Из файла...",   new Color(0.4f, 0.4f, 0.8f),   OnInstallFromFileClicked);

            btnRow.Add(_installBtn);
            btnRow.Add(_updateBtn);
            btnRow.Add(_latestBtn);
            btnRow.Add(_removeBtn);
            btnRow.Add(_installFromFileBtn);
            _detailPanel.Add(btnRow);

            panel.Add(_detailPlaceholder);
            panel.Add(_detailPanel);
            return panel;
        }

        private VisualElement BuildInfoRow(string labelText, out Label valueLabel)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom  = 8;
            row.style.alignItems    = Align.Center;

            var lbl = new Label(labelText);
            lbl.style.width    = 100;
            lbl.style.color    = new StyleColor(ColorGrey);
            lbl.style.fontSize = 12;

            valueLabel = new Label();
            valueLabel.style.fontSize = 12;
            valueLabel.style.flexGrow = 1;
            valueLabel.style.unityTextOverflowPosition = TextOverflowPosition.End;

            row.Add(lbl);
            row.Add(valueLabel);
            return row;
        }

        private Button CreateButton(string text, Color color, System.Action onClick)
        {
            var btn = new Button(onClick) { text = text };
            btn.style.marginRight       = 8;
            btn.style.paddingLeft       = 14;
            btn.style.paddingRight      = 14;
            btn.style.paddingTop        = 6;
            btn.style.paddingBottom     = 6;
            btn.style.color             = new StyleColor(Color.white);
            btn.style.backgroundColor   = new StyleColor(color * 0.75f);
            btn.style.borderTopLeftRadius     = 4;
            btn.style.borderTopRightRadius    = 4;
            btn.style.borderBottomLeftRadius  = 4;
            btn.style.borderBottomRightRadius = 4;
            btn.style.borderTopWidth    = 0;
            btn.style.borderBottomWidth = 0;
            btn.style.borderLeftWidth   = 0;
            btn.style.borderRightWidth  = 0;
            return btn;
        }

        // ── Selection ────────────────────────────────────────────────────────

        private void SelectEntry(SDKEntry entry)
        {
            // Update highlight
            foreach (var e in _entries)
            {
                var item = _listContainer.Q("item_" + e.DisplayName);
                if (item != null)
                    item.style.backgroundColor = e == entry
                        ? new StyleColor(ColorBgSel)
                        : StyleKeyword.Null;
            }

            _selected = entry;
            RefreshDetailPanel();
        }

        // ── Detail panel refresh ─────────────────────────────────────────────

        private void RefreshDetailPanel()
        {
            if (_selected == null)
            {
                _detailPlaceholder.style.display = DisplayStyle.Flex;
                _detailPanel.style.display       = DisplayStyle.None;
                return;
            }

            _detailPlaceholder.style.display = DisplayStyle.None;
            _detailPanel.style.display       = DisplayStyle.Flex;

            var e = _selected;
            _detailTitle.text = e.DisplayName;
            _detailSource.text   = e.Source;
            _detailDefine.text   = !string.IsNullOrEmpty(e.Define) ? e.Define : "—";
            _detailRequired.text = e.RequiredVersion ?? "—";

            if (!e.IsInstalled)
            {
                _detailInstalled.text  = "Не установлен";
                _detailInstalled.style.color = new StyleColor(ColorGrey);
            }
            else if (e.NeedsUpdate)
            {
                _detailInstalled.text  = $"{e.InstalledVersion}  ↑ доступна {e.RequiredVersion}";
                _detailInstalled.style.color = new StyleColor(ColorYellow);
            }
            else
            {
                _detailInstalled.text  = $"{e.InstalledVersion}  ✓";
                _detailInstalled.style.color = new StyleColor(ColorGreen);
            }

            // Latest upstream version
            if (e.HasNewerUpstream)
            {
                _detailLatest.text        = $"v{e.LatestVersion}";
                _detailLatest.style.color = new StyleColor(ColorBlue);
                _detailLatest.parent.style.display = DisplayStyle.Flex;
            }
            else
            {
                _detailLatest.parent.style.display = DisplayStyle.None;
            }

            // SDK checklist
            _sdkChecklistSection.Clear();
            var sdkList = ChecklistRegistry.GetSdkChecklist(e.Define);
            if (sdkList != null)
            {
                _sdkChecklistSection.Add(ChecklistUIHelper.BuildGroup(sdkList.Title, sdkList.GetItems()));
                _sdkChecklistSection.style.display = DisplayStyle.Flex;
            }
            else
            {
                _sdkChecklistSection.style.display = DisplayStyle.None;
            }

            // Mediation section — only for Yandex Mobile Ads
            if (e.Define == "UNIBRIDGE_YANDEX" && _mediationEntries.Count > 0)
            {
                _mediationSection.style.display = DisplayStyle.Flex;
                RefreshMediationSection();
            }
            else
            {
                _mediationSection.style.display = DisplayStyle.None;
            }

            bool busy = _isOperating;
            _installBtn.style.display = (!e.IsInstalled && !busy)   ? DisplayStyle.Flex : DisplayStyle.None;
            _updateBtn.style.display  = (e.NeedsUpdate   && !busy)  ? DisplayStyle.Flex : DisplayStyle.None;
            _removeBtn.style.display  = (e.IsInstalled   && !busy)  ? DisplayStyle.Flex : DisplayStyle.None;

            if (e.CanInstallLatest && !busy)
            {
                _latestBtn.text = $"v{e.LatestVersion} (последняя)";
                _latestBtn.style.display = DisplayStyle.Flex;
            }
            else
            {
                _latestBtn.style.display = DisplayStyle.None;
            }

            bool hasDownloadUrl = !string.IsNullOrEmpty(e.CoreUrl) || IsDownloadUrl(e.GitUrl ?? "");
            _installFromFileBtn.style.display = (hasDownloadUrl && !busy) ? DisplayStyle.Flex : DisplayStyle.None;

            if (!_isOperating)
                _statusLabel.text = "";
        }

        // ── List item refresh ────────────────────────────────────────────────

        private void RefreshListItem(SDKEntry entry)
        {
            var dot     = _listContainer.Q("dot_"     + entry.DisplayName);
            var version = _listContainer.Q<Label>("version_" + entry.DisplayName);
            if (dot == null || version == null) return;

            if (!entry.IsInstalled)
            {
                dot.style.backgroundColor = new StyleColor(ColorGrey);
                version.text  = entry.HasNewerUpstream
                    ? $"Не установлен  (v{entry.LatestVersion})"
                    : "Не установлен";
                version.style.color = new StyleColor(ColorGrey);
            }
            else if (entry.NeedsUpdate)
            {
                dot.style.backgroundColor = new StyleColor(ColorYellow);
                version.text  = $"v{entry.InstalledVersion}  ↑";
                version.style.color = new StyleColor(ColorYellow);
            }
            else if (entry.HasNewerUpstream)
            {
                dot.style.backgroundColor = new StyleColor(ColorBlue);
                version.text  = $"v{entry.InstalledVersion}  ↑ {entry.LatestVersion}";
                version.style.color = new StyleColor(ColorBlue);
            }
            else
            {
                dot.style.backgroundColor = new StyleColor(ColorGreen);
                version.text  = $"v{entry.InstalledVersion}  ✓";
                version.style.color = new StyleColor(ColorGreen);
            }
        }

        // ── Package status refresh ───────────────────────────────────────────

        private void StartRefresh()
        {
            if (_listRequest != null) return;
            _listRequest = Client.List(true);
            EditorApplication.update += OnListProgress;
        }

        private void OnListProgress()
        {
            if (_listRequest == null || !_listRequest.IsCompleted) return;
            EditorApplication.update -= OnListProgress;

            if (_listRequest.Status == StatusCode.Success)
            {
                // Reset installed versions
                foreach (var e in _entries)
                    e.InstalledVersion = null;

                // Collect all installed package names for dependency detection
                var installedNames = new System.Collections.Generic.HashSet<string>();
                foreach (var pkg in _listRequest.Result)
                {
                    installedNames.Add(pkg.name);
                    foreach (var e in _entries)
                    {
                        if (!string.IsNullOrEmpty(e.PackageId) && pkg.name == e.PackageId)
                            e.InstalledVersion = pkg.version;
                        else if (string.IsNullOrEmpty(e.PackageId) && pkg.name.ToLower().Contains(e.DisplayName.ToLower()))
                            e.InstalledVersion = pkg.version;
                    }
                }

                // Detect whether dependency (core) packages are installed
                foreach (var e in _entries)
                    if (!string.IsNullOrEmpty(e.CorePackageId))
                        e.IsCoreInstalled = installedNames.Contains(e.CorePackageId);
            }
            else
            {
                Debug.LogWarning($"[UniBridge SDKInstaller] Failed to list packages: {_listRequest.Error?.message}");
            }

            _listRequest = null;

            foreach (var e in _entries)
            {
                if (!string.IsNullOrEmpty(e.Define))
                {
                    // Only auto-manage defines for packages with a known PackageId.
                    // Packages installed via tgz (no PackageId) are managed manually by the installer.
                    if (!string.IsNullOrEmpty(e.PackageId))
                    {
                        if (e.IsInstalled)
                            ScriptingDefinesManager.AddDefine(e.Define);
                        else
                            ScriptingDefinesManager.RemoveDefine(e.Define);
                    }
                }
            }

            foreach (var e in _entries) RefreshListItem(e);
            RefreshDetailPanel();
            TryResumeChainInstall();
        }

        // If a domain reload interrupted a chain install (e.g. core→pay), resume the
        // second package install after the post-reload list refresh completes.
        private void TryResumeChainInstall()
        {
            if (_isOperating) return;

            string url  = SessionState.GetString(PendingChainUrlKey,  "");
            string name = SessionState.GetString(PendingChainNameKey, "");
            if (string.IsNullOrEmpty(url)) return;

            var entry = _entries.Find(e => e.DisplayName == name);
            if (entry == null || entry.IsInstalled)
            {
                // Already installed or entry not found — nothing to do
                SessionState.EraseString(PendingChainUrlKey);
                SessionState.EraseString(PendingChainNameKey);
                return;
            }

            Debug.Log($"[UniBridge SDKInstaller] Resuming chain install of {name} (2/2) after domain reload");
            _isOperating  = true;
            _pendingEntry = entry;
            SetStatus($"Возобновляю установку {name} (2/2)...");
            // SessionState is cleared inside OnAddProgress on success or failure
            StartDownloadAndInstall(url);
        }

        // ── Button handlers ──────────────────────────────────────────────────

        private void OnInstallClicked()
        {
            if (_selected == null || _isOperating) return;
            StartInstall(_selected);
        }

        private void OnUpdateClicked()
        {
            if (_selected == null || _isOperating) return;
            StartInstall(_selected);
        }

        private void OnLatestClicked()
        {
            if (_selected == null || _isOperating) return;
            var entry = _selected;
            string latest = entry.LatestVersion;
            if (string.IsNullOrEmpty(latest)) return;

            // Mutate in-memory entry: StartInstall reads RequiredVersion / GitUrl directly.
            entry.RequiredVersion = latest;
            if (!string.IsNullOrEmpty(entry.GitUrl))
                entry.GitUrl = Regex.Replace(entry.GitUrl, @"#v\d+(?:\.\d+)*$", $"#v{latest}");

            // Persist to SDKVersions.json so the team and CI pin the same upgraded version.
            if (!string.IsNullOrEmpty(entry.SdkKey))
                WriteSdkVersionToJson(entry.SdkKey, latest, entry.GitUrl);

            StartInstall(entry);
        }

        private static void WriteSdkVersionToJson(string sdkKey, string newVersion, string newGitUrl)
        {
            try
            {
                const string path = "Assets/UniBridge/Editor/SDKVersions.json";
                if (!File.Exists(path)) return;
                string text = File.ReadAllText(path);

                int keyIdx = text.IndexOf($"\"{sdkKey}\":", StringComparison.Ordinal);
                if (keyIdx < 0) return;

                int braceOpen = text.IndexOf('{', keyIdx);
                if (braceOpen < 0) return;

                int depth = 0, braceClose = -1;
                for (int i = braceOpen; i < text.Length; i++)
                {
                    if (text[i] == '{') depth++;
                    else if (text[i] == '}') { depth--; if (depth == 0) { braceClose = i; break; } }
                }
                if (braceClose < 0) return;

                string block = text.Substring(braceOpen, braceClose - braceOpen + 1);

                string newBlock = Regex.Replace(block,
                    @"(""version""\s*:\s*"")[^""]*("")",
                    m => m.Groups[1].Value + newVersion + m.Groups[2].Value);

                if (!string.IsNullOrEmpty(newGitUrl))
                {
                    newBlock = Regex.Replace(newBlock,
                        @"(""gitUrl""\s*:\s*"")[^""]*("")",
                        m => m.Groups[1].Value + newGitUrl + m.Groups[2].Value);
                }

                if (newBlock == block) return;

                string newText = text.Substring(0, braceOpen) + newBlock + text.Substring(braceClose + 1);
                File.WriteAllText(path, newText);
                AssetDatabase.ImportAsset(path);
                Debug.Log($"[UniBridge SDKInstaller] SDKVersions.json: {sdkKey} bumped to {newVersion}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UniBridge SDKInstaller] Failed to update SDKVersions.json for {sdkKey}: {ex.Message}");
            }
        }

        private void OnRemoveClicked()
        {
            if (_selected == null || _isOperating) return;

            if (!string.IsNullOrEmpty(_selected.PackageId))
                StartRemove(_selected);
            else
                Debug.LogWarning($"[UniBridge SDKInstaller] Cannot remove {_selected.DisplayName}: no PackageId (git package).");
        }

        private void OnInstallFromFileClicked()
        {
            if (_selected == null || _isOperating) return;
            var entry = _selected;

            _isOperating  = true;
            _pendingEntry = entry;

            if (!string.IsNullOrEmpty(entry.CoreUrl) && !entry.IsCoreInstalled)
            {
                // Core + main package needed — open two file dialogs sequentially
                string corePath = EditorUtility.OpenFilePanel(
                    $"Выберите {entry.CorePackageId}.tgz (1/2)", "", "tgz");
                if (string.IsNullOrEmpty(corePath)) { _isOperating = false; return; }

                string payPath = EditorUtility.OpenFilePanel(
                    $"Выберите {entry.PackageId}.tgz (2/2)",
                    Path.GetDirectoryName(corePath), "tgz");
                if (string.IsNullOrEmpty(payPath)) { _isOperating = false; return; }

                string payFileUrl = $"file:{payPath.Replace('\\', '/')}";
                SessionState.SetString(PendingChainUrlKey,  payFileUrl);
                SessionState.SetString(PendingChainNameKey, entry.DisplayName);

                _nextInstallAction = () =>
                {
                    SetStatus($"Устанавливаю {entry.DisplayName} (2/2)...");
                    StartDownloadAndInstall(payFileUrl);
                };
                SetStatus($"Устанавливаю {entry.DisplayName} (1/2)...");
                string coreFileUrl = $"file:{corePath.Replace('\\', '/')}";
                Debug.Log($"[UniBridge SDKInstaller] Installing core from local file: {coreFileUrl}");
                StartDownloadAndInstall(coreFileUrl);
            }
            else
            {
                string path = EditorUtility.OpenFilePanel(
                    $"Выберите {entry.PackageId}.tgz", "", "tgz");
                if (string.IsNullOrEmpty(path)) { _isOperating = false; return; }

                SetStatus($"Устанавливаю {entry.DisplayName}...");
                string fileUrl = $"file:{path.Replace('\\', '/')}";
                Debug.Log($"[UniBridge SDKInstaller] Installing from local file: {fileUrl}");
                StartDownloadAndInstall(fileUrl);
            }

            RefreshDetailPanel();
        }

        // ── Install ──────────────────────────────────────────────────────────

        private void StartInstall(SDKEntry entry)
        {
            _isOperating  = true;
            _pendingEntry = entry;

            if (!string.IsNullOrEmpty(entry.RegistryUrl))
                EnsureScopedRegistry(entry.RegistryUrl, entry.RegistryScopes);

            string spec = !string.IsNullOrEmpty(entry.GitUrl)
                ? entry.GitUrl
                : $"{entry.PackageId}@{entry.RequiredVersion}";

            // If there's a core dependency and it is NOT yet installed, install it first
            if (!string.IsNullOrEmpty(entry.CoreUrl) && !entry.IsCoreInstalled)
            {
                // Persist the pay URL to SessionState so it survives a domain reload
                // that may be triggered by the core package install.
                SessionState.SetString(PendingChainUrlKey,  spec);
                SessionState.SetString(PendingChainNameKey, entry.DisplayName);

                _nextInstallAction = () =>
                {
                    SetStatus($"Устанавливаю {entry.DisplayName} (2/2)...");
                    StartDownloadAndInstall(spec);
                };
                SetStatus($"Устанавливаю {entry.DisplayName} (1/2)...");
                Debug.Log($"[UniBridge SDKInstaller] Installing core dependency: {entry.CoreUrl}");
                StartDownloadAndInstall(entry.CoreUrl);
                return;
            }

            // Core already installed (or no core dependency) — install main package directly
            if (!string.IsNullOrEmpty(entry.CoreUrl) && entry.IsCoreInstalled)
                Debug.Log($"[UniBridge SDKInstaller] Core dependency already present, skipping to {spec}");

            SetStatus($"Устанавливаю {entry.DisplayName}...");
            Debug.Log($"[UniBridge SDKInstaller] Installing {spec}");
            StartDownloadAndInstall(spec);
        }

        // Downloads (if HTTP) and installs a single package URL.
        private void StartDownloadAndInstall(string url)
        {
            bool isHttpUrl = url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                             url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
            bool isGitUrl  = url.Contains(".git");

            if (isHttpUrl && !isGitUrl)
            {
                // Must download first; Client.Add() doesn't accept arbitrary HTTP URLs.
                // Use async UnityWebRequest so the main thread stays unblocked —
                // a synchronous download would stall Unity's package resolver and cause
                // "exclusive access" errors when chaining two Client.Add() calls (RuStore core→pay).
                string fileName = Path.GetFileName(url);
                // URLs like gitflic /download have no file extension — use content hash as name
                if (string.IsNullOrEmpty(Path.GetExtension(fileName)))
                    fileName = $"unibridge_pkg_{(uint)url.GetHashCode()}.tgz";

                string localPath = Path.Combine(Application.temporaryCachePath, fileName);
                Debug.Log($"[UniBridge SDKInstaller] Downloading {url}");

                _activeWebRequest = UnityWebRequest.Get(url);
                _activeWebRequest.SetRequestHeader("User-Agent",      "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                _activeWebRequest.SetRequestHeader("Accept",          "application/octet-stream, */*");
                _activeWebRequest.SetRequestHeader("Accept-Language", "ru-RU,ru;q=0.9,en;q=0.8");
                _activeWebRequest.SetRequestHeader("Referer",         "https://gitflic.ru/");
                _activeWebRequest.timeout = 120; // 2-min cap; prevents infinite hang on slow/dead connections
                var op = _activeWebRequest.SendWebRequest();
                op.completed += _ =>
                {
                    // If OnDisable() ran (domain reload), _activeWebRequest was Abort+Dispose+null'd.
                    // In that case skip everything — TryResumeChainInstall handles recovery.
                    var req = _activeWebRequest;
                    _activeWebRequest = null;
                    if (req == null) return;

                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        File.WriteAllBytes(localPath, req.downloadHandler.data);
                        req.Dispose();
                        string fileUrl = $"file:{localPath.Replace('\\', '/')}";
                        Debug.Log($"[UniBridge SDKInstaller] Installing from local file: {fileUrl}");
                        _addRequest = Client.Add(fileUrl);
                        EditorApplication.update += OnAddProgress;
                    }
                    else
                    {
                        Debug.LogError($"[UniBridge SDKInstaller] Download failed: {req.error}");
                        SetStatus($"Ошибка загрузки: {req.error}");
                        req.Dispose();
                        _isOperating       = false;
                        _nextInstallAction = null;
                        SessionState.EraseString(PendingChainUrlKey);
                        SessionState.EraseString(PendingChainNameKey);
                        RefreshDetailPanel();
                    }
                };
                RefreshDetailPanel();
                return;
            }

            _addRequest = Client.Add(url);
            EditorApplication.update += OnAddProgress;
            RefreshDetailPanel();
        }

        private static void EnsureScopedRegistry(string url, string scopesStr)
        {
            string manifestPath = Path.Combine(
                Application.dataPath, "..", "Packages", "manifest.json");
            string content = File.ReadAllText(manifestPath);

            if (content.Contains($"\"url\": \"{url}\""))
            {
                Debug.Log($"[UniBridge SDKInstaller] Scoped registry '{url}' already present.");
                return;
            }

            string[] scopes = scopesStr.Split(';');
            string scopesJson = string.Join(", ",
                Array.ConvertAll(scopes, s => $"\"{s.Trim()}\""));
            string registryBlock =
                $"{{ \"name\": \"OpenUPM\", \"url\": \"{url}\", \"scopes\": [{scopesJson}] }}";

            if (content.Contains("\"scopedRegistries\""))
            {
                int arrayStart = content.IndexOf('[', content.IndexOf("\"scopedRegistries\"")) + 1;
                content = content.Insert(arrayStart, "\n    " + registryBlock + ",");
            }
            else
            {
                int lastBrace = content.LastIndexOf('}');
                content = content.Insert(lastBrace,
                    $",\n  \"scopedRegistries\": [\n    {registryBlock}\n  ]\n");
            }

            File.WriteAllText(manifestPath, content);
            Debug.Log($"[UniBridge SDKInstaller] Scoped registry '{url}' added to manifest.json");
        }

        private void OnAddProgress()
        {
            if (_addRequest == null || !_addRequest.IsCompleted) return;
            EditorApplication.update -= OnAddProgress;

            if (_addRequest.Status == StatusCode.Success)
            {
                string ver = _addRequest.Result.version;
                Debug.Log($"[UniBridge SDKInstaller] Package {ver} installed.");

                // If there's a chained install (e.g. core → pay), run it now
                if (_nextInstallAction != null)
                {
                    _addRequest = null;
                    var next = _nextInstallAction;
                    _nextInstallAction = null;
                    next();
                    return;
                }

                // Final package in the chain installed successfully — clear recovery state
                SessionState.EraseString(PendingChainUrlKey);
                SessionState.EraseString(PendingChainNameKey);

                _pendingEntry.InstalledVersion = ver;

                if (!string.IsNullOrEmpty(_pendingEntry.Define))
                {
                    var currentDefines = ScriptingDefinesManager.GetScriptingDefineSymbolsForSelectedBuildTarget();
                    if (!currentDefines.Contains(_pendingEntry.Define))
                    {
                        ScriptingDefinesManager.AddDefine(_pendingEntry.Define);
                        ScriptingDefinesManager.Flush(); // Apply before domain reload
                        SDKVersionChecker.ResetCheck();  // Re-validate on next domain reload
                    }
                }
                SetStatus($"{_pendingEntry.DisplayName} {ver} установлен.");
            }
            else
            {
                Debug.LogError($"[UniBridge SDKInstaller] Install failed: {_addRequest.Error?.message}");
                SetStatus($"Ошибка: {_addRequest.Error?.message}");
                _nextInstallAction = null;
                SessionState.EraseString(PendingChainUrlKey);
                SessionState.EraseString(PendingChainNameKey);
            }

            _addRequest   = null;
            _isOperating  = false;

            RefreshListItem(_pendingEntry);
            RefreshDetailPanel();
            _pendingEntry = null;
        }

        // ── Remove ───────────────────────────────────────────────────────────

        private void StartRemove(SDKEntry entry)
        {
            _isOperating       = true;
            _pendingEntry      = entry;
            _isRemovingCoreDep = false;

            // If the entry has an installed core dependency, queue its removal after the main package
            if (!string.IsNullOrEmpty(entry.CorePackageId) && entry.IsCoreInstalled)
            {
                string coreId = entry.CorePackageId;
                _nextRemoveAction = () =>
                {
                    _isRemovingCoreDep = true;
                    SetStatus($"Удаляю зависимость {coreId}...");
                    Debug.Log($"[UniBridge SDKInstaller] Removing core dependency: {coreId}");
                    _removeRequest = Client.Remove(coreId);
                    EditorApplication.update += OnRemoveProgress;
                };
            }

            SetStatus($"Удаляю {entry.DisplayName}...");
            Debug.Log($"[UniBridge SDKInstaller] Removing {entry.PackageId}");

            _removeRequest = Client.Remove(entry.PackageId);
            EditorApplication.update += OnRemoveProgress;
            RefreshDetailPanel();
        }

        private void OnRemoveProgress()
        {
            if (_removeRequest == null || !_removeRequest.IsCompleted) return;
            EditorApplication.update -= OnRemoveProgress;

            if (_removeRequest.Status == StatusCode.Success)
            {
                if (!_isRemovingCoreDep)
                {
                    // Main package (pay) removed — handle define, then chain to core if needed
                    Debug.Log($"[UniBridge SDKInstaller] {_pendingEntry.DisplayName} removed.");
                    _pendingEntry.InstalledVersion = null;

                    if (!string.IsNullOrEmpty(_pendingEntry.Define))
                    {
                        var currentDefines = ScriptingDefinesManager.GetScriptingDefineSymbolsForSelectedBuildTarget();
                        if (currentDefines.Contains(_pendingEntry.Define))
                        {
                            ScriptingDefinesManager.RemoveDefine(_pendingEntry.Define);
                            ScriptingDefinesManager.Flush();
                            SDKVersionChecker.ResetCheck();
                        }
                    }

                    if (_nextRemoveAction != null)
                    {
                        _removeRequest = null;
                        var next = _nextRemoveAction;
                        _nextRemoveAction = null;
                        next(); // remove core dependency
                        return;
                    }

                    SetStatus($"{_pendingEntry.DisplayName} удалён.");
                }
                else
                {
                    // Core dependency removed
                    Debug.Log($"[UniBridge SDKInstaller] Core dependency removed.");
                    _pendingEntry.IsCoreInstalled = false;
                    SetStatus($"{_pendingEntry.DisplayName} удалён полностью.");
                }
            }
            else
            {
                Debug.LogError($"[UniBridge SDKInstaller] Remove failed: {_removeRequest.Error?.message}");
                SetStatus($"Ошибка: {_removeRequest.Error?.message}");
                _nextRemoveAction  = null;
                _isRemovingCoreDep = false;
            }

            _removeRequest     = null;
            _isOperating       = false;
            _isRemovingCoreDep = false;

            RefreshListItem(_pendingEntry);
            RefreshDetailPanel();
            _pendingEntry = null;
        }

        // ── Mediation adapters ───────────────────────────────────────────────

        private void RefreshMediationSection()
        {
            _mediationSection.Clear();

            var header = new Label("Mediation Adapters");
            header.style.fontSize                    = 12;
            header.style.unityFontStyleAndWeight     = FontStyle.Bold;
            header.style.marginBottom                = 6;
            _mediationSection.Add(header);

            foreach (var m in _mediationEntries)
            {
                var guids = AssetDatabase.FindAssets(m.DetectionFile);
                m.IsInstalled = guids.Length > 0;
                _mediationSection.Add(BuildMediationRow(m));
            }
        }

        private VisualElement BuildMediationRow(MediationEntry m)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems    = Align.Center;
            row.style.marginBottom  = 4;

            var dot = new VisualElement();
            dot.style.width  = 7;
            dot.style.height = 7;
            dot.style.borderTopLeftRadius     = 3.5f;
            dot.style.borderTopRightRadius    = 3.5f;
            dot.style.borderBottomLeftRadius  = 3.5f;
            dot.style.borderBottomRightRadius = 3.5f;
            dot.style.marginRight     = 6;
            dot.style.backgroundColor = new StyleColor(m.IsInstalled ? ColorGreen : ColorGrey);
            row.Add(dot);

            var name = new Label(m.DisplayName);
            name.style.fontSize = 11;
            name.style.flexGrow = 1;
            row.Add(name);

            if (!m.IsInstalled)
            {
                var btn = CreateButton("Установить", ColorGreen, () =>
                {
                    if (!_isOperating) StartDownloadAdapter(m);
                });
                btn.style.paddingTop    = 2;
                btn.style.paddingBottom = 2;
                btn.style.paddingLeft   = 8;
                btn.style.paddingRight  = 8;
                btn.style.fontSize      = 10;
                btn.SetEnabled(!_isOperating);
                row.Add(btn);
            }
            else
            {
                var btn = CreateButton("Удалить", new Color(0.8f, 0.3f, 0.3f), () =>
                    EditorUtility.DisplayDialog(
                        "Удаление адаптера",
                        $"Для удаления {m.DisplayName} используйте 'YandexAds > Adapters Info' " +
                        "или вручную удалите файлы адаптера из папки Assets/.",
                        "OK"));
                btn.style.paddingTop    = 2;
                btn.style.paddingBottom = 2;
                btn.style.paddingLeft   = 8;
                btn.style.paddingRight  = 8;
                btn.style.fontSize      = 10;
                row.Add(btn);
            }

            return row;
        }

        private void StartDownloadAdapter(MediationEntry entry)
        {
            _isOperating      = true;
            _pendingMediation = entry;
            SetStatus($"Загружаю {entry.DisplayName}...");
            RefreshDetailPanel();

            _activeWebRequest = UnityWebRequest.Get(entry.DownloadUrl);
            var op = _activeWebRequest.SendWebRequest();
            op.completed += _ => EditorApplication.delayCall += OnDownloadComplete;
        }

        private void OnDownloadComplete()
        {
            var req   = _activeWebRequest;
            var entry = _pendingMediation;
            _activeWebRequest = null;
            _pendingMediation = null;

            try
            {
                if (req.result == UnityWebRequest.Result.Success)
                {
                    string tmpPath = Path.Combine(Path.GetTempPath(),
                        $"mobileads-{entry.AdapterKey}-mediation-{entry.Version}.unitypackage");
                    File.WriteAllBytes(tmpPath, req.downloadHandler.data);
                    AssetDatabase.ImportPackage(tmpPath, false);
                    File.Delete(tmpPath);
                    SetStatus($"{entry.DisplayName} установлен.");
                }
                else
                {
                    SetStatus($"Ошибка: {req.error}");
                    Debug.LogError($"[UniBridge SDKInstaller] Adapter download failed: {req.error}");
                }
            }
            finally
            {
                req.Dispose();
                _isOperating = false;
                RefreshDetailPanel();
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static bool IsDownloadUrl(string url) =>
            (url.StartsWith("http://",  StringComparison.OrdinalIgnoreCase) ||
             url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) &&
            !url.Contains(".git");

        private void SetStatus(string text)
        {
            if (_statusLabel != null)
                _statusLabel.text = text;
        }

        public static SDKVersions LoadRequiredVersions()
        {
            var guids = AssetDatabase.FindAssets("SDKVersions t:TextAsset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("UniBridge") && path.EndsWith("SDKVersions.json"))
                {
                    var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    if (textAsset != null)
                        return JsonUtility.FromJson<SDKVersions>(textAsset.text);
                }
            }

            var directPath = "Assets/UniBridge/Editor/SDKVersions.json";
            if (File.Exists(directPath))
                return JsonUtility.FromJson<SDKVersions>(File.ReadAllText(directPath));

            var packagePath = "Packages/com.unibridge.core/Editor/SDKVersions.json";
            if (File.Exists(packagePath))
                return JsonUtility.FromJson<SDKVersions>(File.ReadAllText(packagePath));

            return null;
        }
    }

    [Serializable]
    public class SDKVersions
    {
        public SDKInfo levelplay;
        public SDKInfo playgama;
        public SDKInfo edm4u;
        public SDKInfo yandex;
        public SDKInfo unityiap;
        public SDKInfo rustore;
        public SDKInfo gpgs;
        public SDKInfo googlePlayReview;
        public SDKInfo rustoreReview;
        public SDKInfo appmetrica;
        public YandexMediationAdapterInfo[] yandexMediationAdapters;
    }

    [Serializable]
    public class SDKInfo
    {
        public string version;
        public string packageId;
        public string corePackageId;  // UPM name of the dependency package (e.g. ru.rustore.core)
        public string gitUrl;
        public string coreUrl;        // dependency package URL to install before gitUrl (e.g. ru.rustore.core)
        public string define;
        public string displayName;
        public string checkMode;      // "define" or "package"
        public string registryUrl;    // scoped registry URL, empty = use Unity registry or git
        public string registryScopes; // semicolon-separated scopes
        public LatestCheckSource latestCheckSource; // how to check for latest upstream version
    }

    [Serializable]
    public class YandexMediationAdapterInfo
    {
        public string displayName;
        public string adapterKey;    // e.g. "google", "applovin"
        public string version;
        public string detectionFile; // search pattern for AssetDatabase.FindAssets
    }
}
