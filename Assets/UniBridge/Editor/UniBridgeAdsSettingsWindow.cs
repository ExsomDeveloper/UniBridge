using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniBridge.Editor
{
    public class UniBridgeSettingsWindow : EditorWindow
    {
        // ── Colors (matching SDKInstallerWindow palette) ─────────────────────────

        private static readonly Color ColorBorder = new(0.15f, 0.15f, 0.15f);
        private static readonly Color ColorBg     = new(0.18f, 0.18f, 0.18f);
        private static readonly Color ColorBgSel  = new(0.24f, 0.37f, 0.57f);
        private static readonly Color ColorBgHov  = new(0.22f, 0.22f, 0.22f);
        private static readonly Color ColorGrey   = new(0.55f, 0.55f, 0.55f);
        private static readonly Color ColorLine   = new(0.28f, 0.28f, 0.28f);

        // ── Nav ──────────────────────────────────────────────────────────────────

        private static readonly string[] NavItems = { "UniBridge", "UniBridgePurchases", "UniBridgeLeaderboards", "UniBridgeRate", "UniBridgeAnalytics", "General" };

        private int _selectedIndex;

        // ── Sub-tab state ─────────────────────────────────────────────────────────

        private int _adsSubIndex;
        private int _purchasesSubIndex;
        private int _leaderboardsSubIndex;

        private static readonly string[] LeaderboardsSubTabs = { "Common", "Simulation" };

        // ── Configs ──────────────────────────────────────────────────────────────

        private UniBridgeConfig          _adsConfig;
        private UniBridgePurchasesConfig    _purchasesConfig;
        private UniBridgeLeaderboardsConfig _leaderboardsConfig;
        private UniBridgeRateConfig         _rateConfig;
        private UniBridgeAnalyticsConfig    _analyticsConfig;
        private SerializedObject      _serializedAds;
        private SerializedObject      _serializedPurchases;
        private SerializedObject      _serializedLeaderboards;
        private SerializedObject      _serializedRate;
        private SerializedObject      _serializedAnalytics;

        // ── Drawers ──────────────────────────────────────────────────────────────

        private LevelPlaySettingsDrawer _levelPlay;
        private PlaygamaSettingsDrawer  _playgama;
        private YandexSettingsDrawer    _yandex;
        private UnityIAPSettingsDrawer   _iap;
        private RuStoreSettingsDrawer    _ruStore;
        private PlaygamaPurchaseDrawer   _playgamaPurchase;

        // ── IMGUI scroll ─────────────────────────────────────────────────────────

        private Vector2 _scrollPos;

        // ── UI references ─────────────────────────────────────────────────────────

        private IMGUIContainer _content;

        // ── Public API ────────────────────────────────────────────────────────────

        public static void Open(int index = 0)
        {
            var w = GetWindow<UniBridgeSettingsWindow>("UniBridge Settings");
            w.minSize        = new Vector2(580, 420);
            w._selectedIndex = index;
            w.LoadConfigs();
            w.CreateRoot();
        }

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            LoadConfigs();
            CreateRoot();
        }

        private void LoadConfigs()
        {
            CreateAdsConfig();
            CreatePurchasesConfig();
            CreateLeaderboardsConfig();
            CreateRateConfig();
            CreateAnalyticsConfig();

            _adsConfig          = EditorConfigHelper.EnsureProjectAsset<UniBridgeConfig>(nameof(UniBridgeConfig));
            _purchasesConfig    = EditorConfigHelper.EnsureProjectAsset<UniBridgePurchasesConfig>(nameof(UniBridgePurchasesConfig));
            _leaderboardsConfig = EditorConfigHelper.EnsureProjectAsset<UniBridgeLeaderboardsConfig>(nameof(UniBridgeLeaderboardsConfig));
            _rateConfig         = EditorConfigHelper.EnsureProjectAsset<UniBridgeRateConfig>(nameof(UniBridgeRateConfig));
            _analyticsConfig    = EditorConfigHelper.EnsureProjectAsset<UniBridgeAnalyticsConfig>(nameof(UniBridgeAnalyticsConfig));

            _serializedAds      = new SerializedObject(_adsConfig);
            _levelPlay          = new LevelPlaySettingsDrawer(_adsConfig);
            _playgama           = new PlaygamaSettingsDrawer(_adsConfig);
            _yandex             = new YandexSettingsDrawer(_adsConfig);

            _serializedPurchases = new SerializedObject(_purchasesConfig);
            _iap              = new UnityIAPSettingsDrawer();
            _ruStore          = new RuStoreSettingsDrawer(_purchasesConfig, _serializedPurchases);
            _playgamaPurchase = new PlaygamaPurchaseDrawer(_purchasesConfig, _serializedPurchases);

            _serializedLeaderboards = new SerializedObject(_leaderboardsConfig);
            _serializedRate         = new SerializedObject(_rateConfig);
            _serializedAnalytics    = new SerializedObject(_analyticsConfig);
        }

        // ── UI Construction ───────────────────────────────────────────────────────

        private void CreateRoot()
        {
            var root = rootVisualElement;
            root.Clear();
            root.style.flexDirection = FlexDirection.Row;
            root.style.flexGrow      = 1;

            root.Add(BuildLeftPanel());
            root.Add(BuildRightPanel());
        }

        private VisualElement BuildLeftPanel()
        {
            var panel = new VisualElement();
            panel.style.width            = 160;
            panel.style.flexShrink       = 0;
            panel.style.borderRightWidth = 1;
            panel.style.borderRightColor = new StyleColor(ColorBorder);
            panel.style.backgroundColor  = new StyleColor(ColorBg);

            var header = new Label("SETTINGS");
            header.style.fontSize                = 10;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color                   = new StyleColor(ColorGrey);
            header.style.marginLeft              = 12;
            header.style.marginTop               = 14;
            header.style.marginBottom            = 6;
            panel.Add(header);

            for (int i = 0; i < NavItems.Length; i++)
                panel.Add(BuildNavItem(i));

            return panel;
        }

        private VisualElement BuildNavItem(int index)
        {
            var item = new VisualElement();
            item.name                = "nav_" + index;
            item.style.paddingLeft   = 14;
            item.style.paddingRight  = 8;
            item.style.paddingTop    = 7;
            item.style.paddingBottom = 7;

            if (index == _selectedIndex)
                item.style.backgroundColor = new StyleColor(ColorBgSel);

            var lbl = new Label(NavItems[index]);
            lbl.style.fontSize = 12;
            item.Add(lbl);

            item.RegisterCallback<ClickEvent>(_ => SelectNav(index));
            item.RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (index != _selectedIndex)
                    item.style.backgroundColor = new StyleColor(ColorBgHov);
            });
            item.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                if (index != _selectedIndex)
                    item.style.backgroundColor = StyleKeyword.Null;
            });

            return item;
        }

        private VisualElement BuildRightPanel()
        {
            _content = new IMGUIContainer(DrawContent);
            _content.style.flexGrow     = 1;
            _content.style.paddingLeft  = 20;
            _content.style.paddingRight = 20;
            _content.style.paddingTop   = 4;
            return _content;
        }

        // ── Selection ────────────────────────────────────────────────────────────

        private void SelectNav(int index)
        {
            _selectedIndex = index;
            _scrollPos     = Vector2.zero;

            for (int i = 0; i < NavItems.Length; i++)
            {
                var item = rootVisualElement.Q("nav_" + i);
                if (item == null) continue;
                item.style.backgroundColor = i == index
                    ? new StyleColor(ColorBgSel)
                    : StyleKeyword.Null;
            }

            _content?.MarkDirtyRepaint();
        }

        // ── IMGUI Content ─────────────────────────────────────────────────────────

        private void DrawContent()
        {
            _serializedAds?.Update();
            _serializedPurchases?.Update();
            _serializedLeaderboards?.Update();
            _serializedRate?.Update();
            _serializedAnalytics?.Update();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            switch (_selectedIndex)
            {
                case 0: DrawUniBridge();            break;
                case 1: DrawUniBridgePurchases();      break;
                case 2: DrawUniBridgeLeaderboards();   break;
                case 3: DrawUniBridgeRate();           break;
                case 4: DrawUniBridgeAnalytics();      break;
                case 5: DrawGeneral();           break;
            }

            EditorGUILayout.EndScrollView();

            _serializedAds?.ApplyModifiedProperties();
            _serializedPurchases?.ApplyModifiedProperties();
            _serializedLeaderboards?.ApplyModifiedProperties();
            _serializedRate?.ApplyModifiedProperties();
            _serializedAnalytics?.ApplyModifiedProperties();
        }

        // ── Section: UniBridge ──────────────────────────────────────────────────────

        private void DrawUniBridge()
        {
            // ── Interstitial ─────────────────────────────────────────────────────
            SectionHeader("Интерстициал");
            var modeProp = _serializedAds.FindProperty("InterstitialMode");
            EditorGUILayout.PropertyField(modeProp, new GUIContent("Режим"));

            if (modeProp.enumValueIndex == (int)InterstitialMode.Automatic)
            {
                var intervalProp = _serializedAds.FindProperty("AutoInterstitialInterval");
                EditorGUILayout.PropertyField(intervalProp, new GUIContent("Интервал авто-показа (сек)"));
            }
            else
            {
                var cooldownProp = _serializedAds.FindProperty("EnableManualCooldown");
                EditorGUILayout.PropertyField(cooldownProp, new GUIContent("Ограничение по времени"));
                if (cooldownProp.boolValue)
                {
                    var intervalProp = _serializedAds.FindProperty("ManualCooldownInterval");
                    EditorGUILayout.PropertyField(intervalProp, new GUIContent("Минимальный интервал (сек)"));
                }
            }

            EditorGUILayout.Space(8);

            // ── SDK settings ──────────────────────────────────────────────────────
            var labels  = new List<string>();
            var actions = new List<Action>();

#if UNIBRIDGE_LEVELPLAY
            labels.Add("LevelPlay");
            actions.Add(() => _levelPlay?.DrawInspector());
#endif
#if UNIBRIDGE_PLAYGAMA
            labels.Add("Playgama");
            actions.Add(() => _playgama?.DrawInspector());
#endif
#if UNIBRIDGE_YANDEX
            labels.Add("Yandex");
            actions.Add(() => _yandex?.DrawInspector());
#endif

            if (labels.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Нет установленных SDK рекламы. Используйте UniBridge > SDK Installer.",
                    MessageType.Info);
                return;
            }

            _adsSubIndex = Mathf.Clamp(_adsSubIndex, 0, labels.Count - 1);
            _adsSubIndex = GUILayout.Toolbar(_adsSubIndex, labels.ToArray());
            EditorGUILayout.Space(8);
            actions[_adsSubIndex]();
            EditorGUILayout.Space(8);
        }

        // ── Section: UniBridgePurchases ─────────────────────────────────────────────────

        private void DrawUniBridgePurchases()
        {
            var labels  = new List<string> { "Products" };
            var actions = new List<Action> { DrawPurchasesProducts };

#if UNIBRIDGEPURCHASES_IAP
            labels.Add("Unity IAP");
            actions.Add(() => _iap?.DrawInspector());
#endif
#if UNIBRIDGEPURCHASES_RUSTORE
            labels.Add("RuStore");
            actions.Add(() => _ruStore?.DrawInspector());
#endif
#if UNIBRIDGE_PLAYGAMA
            labels.Add("Playgama");
            actions.Add(() => _playgamaPurchase?.DrawInspector());
#endif

            _purchasesSubIndex = Mathf.Clamp(_purchasesSubIndex, 0, labels.Count - 1);
            _purchasesSubIndex = GUILayout.Toolbar(_purchasesSubIndex, labels.ToArray());
            EditorGUILayout.Space(8);
            actions[_purchasesSubIndex]();
            EditorGUILayout.Space(8);
        }

        private void DrawPurchasesProducts()
        {
            SectionHeader("Product Catalog");
            var productsProp = _serializedPurchases.FindProperty("_products");
            if (productsProp != null)
                EditorGUILayout.PropertyField(productsProp, new GUIContent("Products"), true);
        }

        // ── Section: UniBridgeLeaderboards ──────────────────────────────────────────────

        private void DrawUniBridgeLeaderboards()
        {
            _leaderboardsSubIndex = GUILayout.Toolbar(_leaderboardsSubIndex, LeaderboardsSubTabs);
            EditorGUILayout.Space(8);

            switch (_leaderboardsSubIndex)
            {
                case 0: DrawLeaderboardsCommon();     break;
                case 1: DrawLeaderboardsSimulation(); break;
            }

            EditorGUILayout.Space(8);
        }

        private void DrawLeaderboardsCommon()
        {
            SectionHeader("General");

            EditorGUI.BeginChangeCheck();
            _leaderboardsConfig.AutoInitialize = EditorGUILayout.Toggle(
                new GUIContent("Auto Initialize", "Автоматически инициализировать UniBridgeLeaderboards при запуске"),
                _leaderboardsConfig.AutoInitialize);
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_leaderboardsConfig);

            EditorGUILayout.Space(12);
            SectionHeader("Leaderboard Definitions");
            var lbProp = _serializedLeaderboards?.FindProperty("_leaderboards");
            if (lbProp != null)
                EditorGUILayout.PropertyField(lbProp, new GUIContent("Leaderboards"), true);
        }

        private void DrawLeaderboardsSimulation()
        {
            var simProp = _serializedLeaderboards?.FindProperty("_simulationSettings");
            if (simProp != null)
                EditorGUILayout.PropertyField(simProp, new GUIContent("Simulation Settings"), true);
        }

        // ── Section: UniBridgeRate ──────────────────────────────────────────────────────

        private void DrawUniBridgeRate()
        {
            SectionHeader("General");

            EditorGUI.BeginChangeCheck();
            _rateConfig.AutoInitialize = EditorGUILayout.Toggle(
                new GUIContent("Auto Initialize", "Автоматически инициализировать UniBridgeRate при запуске"),
                _rateConfig.AutoInitialize);
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_rateConfig);

            EditorGUILayout.Space(8);
        }

        // ── Section: UniBridgeAnalytics ─────────────────────────────────────────────────

        private void DrawUniBridgeAnalytics()
        {
            if (_analyticsConfig == null) return;

            SectionHeader("General");

            EditorGUI.BeginChangeCheck();
            _analyticsConfig.AutoInitialize = EditorGUILayout.Toggle(
                new GUIContent("Auto Initialize", "Автоматически инициализировать UniBridgeAnalytics при запуске"),
                _analyticsConfig.AutoInitialize);
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_analyticsConfig);

#if UNIBRIDGEANALYTICS_APPMETRICA
            EditorGUILayout.Space(12);
            SectionHeader("AppMetrica");

            EditorGUI.BeginChangeCheck();
            _analyticsConfig.AppMetrica.ApiKey = EditorGUILayout.TextField(
                new GUIContent("API Key", "AppMetrica API Key из консоли AppMetrica"),
                _analyticsConfig.AppMetrica.ApiKey);
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_analyticsConfig);
#else
            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "AppMetrica не установлена. Используйте UniBridge > SDK Installer для установки.",
                MessageType.Info);
#endif

            EditorGUILayout.Space(8);
        }

        // ── Section: General ──────────────────────────────────────────────────────

        private void DrawGeneral()
        {
            SectionHeader("Ads");

            EditorGUI.BeginChangeCheck();

            _adsConfig.AutoInitialize = EditorGUILayout.Toggle(
                new GUIContent("Auto Initialize", "Automatically initialize UniBridge on startup"),
                _adsConfig.AutoInitialize);

            _adsConfig.SuccessfulRewardResetInterstitial = EditorGUILayout.Toggle(
                new GUIContent("Reward Resets Interstitial Timer",
                    "Completing a rewarded ad resets the interstitial cooldown timer"),
                _adsConfig.SuccessfulRewardResetInterstitial);

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_adsConfig);

            EditorGUILayout.Space(12);
            SectionHeader("Age (COPPA)");

            EditorGUI.BeginChangeCheck();

            _adsConfig.MaxChildrenAge = EditorGUILayout.IntField(
                new GUIContent("Max Children Age", "Users at or below this age get COPPA-compliant ads"),
                _adsConfig.MaxChildrenAge);

            _adsConfig.DefaultUserAge = EditorGUILayout.IntField(
                new GUIContent("Default User Age", "Age used when Initialize() is called without an age parameter"),
                _adsConfig.DefaultUserAge);

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_adsConfig);

            EditorGUILayout.Space(12);
            SectionHeader("Storage");

            EditorGUI.BeginChangeCheck();

            _adsConfig.AdsDisabledKey = EditorGUILayout.TextField(
                new GUIContent("Ads Disabled Key", "PlayerPrefs key used to persist ad-free status"),
                _adsConfig.AdsDisabledKey);

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_adsConfig);

            EditorGUILayout.Space(12);
            SectionHeader("Saves");

            EditorGUI.BeginChangeCheck();

            _adsConfig.AutoInitializeSaves = EditorGUILayout.Toggle(
                new GUIContent("Auto Initialize Saves", "Automatically initialize the save system on startup"),
                _adsConfig.AutoInitializeSaves);

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_adsConfig);

            EditorGUILayout.Space(12);
            SectionHeader("Purchases");

            EditorGUI.BeginChangeCheck();

            _purchasesConfig.AutoInitialize = EditorGUILayout.Toggle(
                new GUIContent("Auto Initialize", "Automatically initialize UniBridgePurchases on startup"),
                _purchasesConfig.AutoInitialize);

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_purchasesConfig);

            EditorGUILayout.Space(12);
            SectionHeader("Leaderboards");

            EditorGUI.BeginChangeCheck();

            _leaderboardsConfig.AutoInitialize = EditorGUILayout.Toggle(
                new GUIContent("Auto Initialize", "Автоматически инициализировать UniBridgeLeaderboards при запуске"),
                _leaderboardsConfig.AutoInitialize);

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_leaderboardsConfig);

            EditorGUILayout.Space(12);
            SectionHeader("Rate");

            EditorGUI.BeginChangeCheck();
            _rateConfig.AutoInitialize = EditorGUILayout.Toggle(
                new GUIContent("Auto Initialize", "Автоматически инициализировать UniBridgeRate при запуске"),
                _rateConfig.AutoInitialize);
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_rateConfig);

            EditorGUILayout.Space(12);
            SectionHeader("Verbose Logging");
            EditorGUILayout.HelpBox(
                "Включение добавляет scripting define. Unity перекомпилирует — логи компилируются или вырезаются полностью (нулевой оверхед в release).",
                MessageType.None);
            EditorGUILayout.Space(4);
            DrawVerboseToggle("Ads",          "UNIBRIDGE_VERBOSE_LOG");
            DrawVerboseToggle("Purchases",    "UNIBRIDGEPURCHASES_VERBOSE_LOG");
            DrawVerboseToggle("Leaderboards", "UNIBRIDGELEADERBOARDS_VERBOSE_LOG");
            DrawVerboseToggle("Saves",        "UNIBRIDGESAVES_VERBOSE_LOG");
            DrawVerboseToggle("Rate",         "UNIBRIDGERATE_VERBOSE_LOG");
            DrawVerboseToggle("Analytics",    "UNIBRIDGEANALYTICS_VERBOSE_LOG");
            DrawVerboseToggle("Share",        "UNIBRIDGESHARE_VERBOSE_LOG");

            EditorGUILayout.Space(8);
        }

        // ── Config creation ───────────────────────────────────────────────────────

        internal static void EnsureAllConfigs()
        {
            CreateAdsConfig();
            CreatePurchasesConfig();
            CreateLeaderboardsConfig();
            CreateRateConfig();
            CreateAnalyticsConfig();
        }

        private static void CreateAdsConfig()
        {
            const string configPath = "Assets/UniBridge/Resources/UniBridgeConfig.asset";
            if (!AssetDatabase.IsValidFolder("Assets/UniBridge/Resources"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/UniBridge"))
                    AssetDatabase.CreateFolder("Assets", "UniBridge");
                AssetDatabase.CreateFolder("Assets/UniBridge", "Resources");
            }
            if (Resources.Load<UniBridgeConfig>(nameof(UniBridgeConfig)) != null) return;
            var config = ScriptableObject.CreateInstance<UniBridgeConfig>();
            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateLeaderboardsConfig()
        {
            const string configPath = "Assets/UniBridge/Resources/UniBridgeLeaderboardsConfig.asset";
            if (!AssetDatabase.IsValidFolder("Assets/UniBridge/Resources"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/UniBridge"))
                    AssetDatabase.CreateFolder("Assets", "UniBridge");
                AssetDatabase.CreateFolder("Assets/UniBridge", "Resources");
            }
            var existing = Resources.Load<UniBridgeLeaderboardsConfig>(nameof(UniBridgeLeaderboardsConfig));
            if (existing != null) return;
            var config = ScriptableObject.CreateInstance<UniBridgeLeaderboardsConfig>();
            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateRateConfig()
        {
            const string configPath = "Assets/UniBridge/Resources/UniBridgeRateConfig.asset";
            if (!AssetDatabase.IsValidFolder("Assets/UniBridge/Resources"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/UniBridge"))
                    AssetDatabase.CreateFolder("Assets", "UniBridge");
                AssetDatabase.CreateFolder("Assets/UniBridge", "Resources");
            }
            var existing = Resources.Load<UniBridgeRateConfig>(nameof(UniBridgeRateConfig));
            if (existing != null) return;
            var config = ScriptableObject.CreateInstance<UniBridgeRateConfig>();
            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateAnalyticsConfig()
        {
            const string configPath = "Assets/UniBridge/Resources/UniBridgeAnalyticsConfig.asset";
            if (!AssetDatabase.IsValidFolder("Assets/UniBridge/Resources"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/UniBridge"))
                    AssetDatabase.CreateFolder("Assets", "UniBridge");
                AssetDatabase.CreateFolder("Assets/UniBridge", "Resources");
            }
            if (Resources.Load<UniBridgeAnalyticsConfig>(nameof(UniBridgeAnalyticsConfig)) != null) return;
            var config = ScriptableObject.CreateInstance<UniBridgeAnalyticsConfig>();
            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreatePurchasesConfig()
        {
            const string configPath = "Assets/UniBridge/Resources/UniBridgePurchasesConfig.asset";
            if (!AssetDatabase.IsValidFolder("Assets/UniBridge/Resources"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/UniBridge"))
                    AssetDatabase.CreateFolder("Assets", "UniBridge");
                AssetDatabase.CreateFolder("Assets/UniBridge", "Resources");
            }
            var existing = Resources.Load<UniBridgePurchasesConfig>(nameof(UniBridgePurchasesConfig));
            if (existing != null) return;
            var config = ScriptableObject.CreateInstance<UniBridgePurchasesConfig>();
            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static void DrawVerboseToggle(string label, string define)
        {
            var defines = ScriptingDefinesManager.GetScriptingDefineSymbolsForSelectedBuildTarget();
            bool current = defines.Contains(define);
            EditorGUI.BeginChangeCheck();
            bool next = EditorGUILayout.Toggle(label, current);
            if (EditorGUI.EndChangeCheck() && next != current)
            {
                if (next) ScriptingDefinesManager.AddDefine(define);
                else      ScriptingDefinesManager.RemoveDefine(define);
            }
        }

        private static void SectionHeader(string title)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            var rect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, ColorLine);
            EditorGUILayout.Space(4);
        }

    }
}
