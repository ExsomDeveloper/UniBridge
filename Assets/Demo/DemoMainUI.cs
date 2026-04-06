using System.Collections.Generic;
using UniBridge;
using UnityEngine;
using UnityEngine.UIElements;

public class DemoMainUI : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;

    // ── Navigation panels ───────────────────────────────────────────────
    private VisualElement _mainPage;
    private VisualElement _adsPage;
    private VisualElement _purchasesPage;
    private VisualElement _leaderboardsPage;

    // ── Main page ───────────────────────────────────────────────────────
    private Label _mainStatusLabel;

    // ── Ads page ────────────────────────────────────────────────────────
    private Label _adapterLabel;
    private Label _statusLabel;
    private Label _bannerReadyLabel;
    private Label _interstitialReadyLabel;
    private Label _rewardReadyLabel;
    private Label _initLabel;

    // ── Purchases page ──────────────────────────────────────────────────
    private Label      _purchaseAdapterLabel;
    private Label      _purchaseStatusLabel;
    private Label      _purchaseInitLabel;
    private ScrollView _purchaseList;

    // ── Saves page ───────────────────────────────────────────────────────
    private VisualElement _savesPage;
    private Label         _savesAdapterLabel;
    private Label         _savesStatusLabel;
    private Label         _savesInitLabel;
    private TextField     _savesKeyField;
    private TextField     _savesValueField;

    // ── Leaderboards page ────────────────────────────────────────────────
    private Label      _lbAdapterLabel;
    private Label      _lbStatusLabel;
    private Label      _lbInitLabel;
    private Label      _lbAuthLabel;
    private TextField  _lbIdField;
    private TextField  _lbScoreField;
    private ScrollView _lbEntriesList;

    // ── Rate page ────────────────────────────────────────────────────────
    private VisualElement _ratePage;
    private Label         _rateAdapterLabel;
    private Label         _rateStatusLabel;
    private Label         _rateSupportedLabel;
    private Label         _rateInitLabel;

    // ── Share page ───────────────────────────────────────────────────────
    private VisualElement _sharePage;
    private Label         _shareAdapterLabel;
    private Label         _shareStatusLabel;
    private Label         _shareSupportedLabel;
    private Label         _shareInitLabel;

    private void Awake()
    {
        if (_uiDocument == null)
            _uiDocument = GetComponent<UIDocument>();

        var root = _uiDocument.rootVisualElement;

        // ── Panels
        _mainPage         = root.Q("main-page");
        _adsPage          = root.Q("ads-page");
        _purchasesPage    = root.Q("purchases-page");
        _leaderboardsPage = root.Q("leaderboards-page");
        _ratePage         = root.Q("rate-page");
        _savesPage        = root.Q("saves-page");
        _sharePage        = root.Q("share-page");

        // ── Main page
        _mainStatusLabel = root.Q<Label>("main-status-label");
        root.Q<Button>("btn-go-ads").clicked          += ShowAdsPage;
        root.Q<Button>("btn-go-purchases").clicked    += ShowPurchasesPage;
        root.Q<Button>("btn-go-leaderboards").clicked += ShowLeaderboardsPage;
        root.Q<Button>("btn-go-rate").clicked         += ShowRatePage;
        root.Q<Button>("btn-go-saves").clicked        += ShowSavesPage;
        root.Q<Button>("btn-go-share").clicked        += ShowSharePage;

        // ── Ads page
        _adapterLabel           = root.Q<Label>("adapter-label");
        _statusLabel            = root.Q<Label>("status-label");
        _bannerReadyLabel       = root.Q<Label>("banner-ready-label");
        _interstitialReadyLabel = root.Q<Label>("interstitial-ready-label");
        _rewardReadyLabel       = root.Q<Label>("reward-ready-label");
        _initLabel              = root.Q<Label>("init-label");

        root.Q<Button>("btn-back-ads").clicked       += ShowMainPage;
        root.Q<Button>("btn-banner-show").clicked    += OnBannerShow;
        root.Q<Button>("btn-banner-hide").clicked    += OnBannerHide;
        root.Q<Button>("btn-banner-destroy").clicked += OnBannerDestroy;
        root.Q<Button>("btn-interstitial-show").clicked += OnInterstitialShow;
        root.Q<Button>("btn-reward-show").clicked    += OnRewardShow;
        root.Q<Button>("btn-disable-ads").clicked    += OnDisableAds;
        root.Q<Button>("btn-enable-ads").clicked     += OnEnableAds;
        root.Q<Button>("btn-refresh").clicked        += RefreshAdsStatus;

        // ── Purchases page
        _purchaseAdapterLabel = root.Q<Label>("purchase-adapter-label");
        _purchaseStatusLabel  = root.Q<Label>("purchase-status-label");
        _purchaseInitLabel    = root.Q<Label>("purchase-init-label");
        _purchaseList         = root.Q<ScrollView>("purchase-list");

        root.Q<Button>("btn-back-purchases").clicked    += ShowMainPage;
        root.Q<Button>("btn-refresh-purchases").clicked += OnRefreshPurchases;
        root.Q<Button>("btn-restore-purchases").clicked += OnRestorePurchases;

        // ── Saves page
        _savesAdapterLabel = root.Q<Label>("saves-adapter-label");
        _savesStatusLabel  = root.Q<Label>("saves-status-label");
        _savesInitLabel    = root.Q<Label>("saves-init-label");
        _savesKeyField     = root.Q<TextField>("saves-key-field");
        _savesValueField   = root.Q<TextField>("saves-value-field");

        root.Q<Button>("btn-back-saves").clicked   += ShowMainPage;
        root.Q<Button>("btn-saves-save").clicked   += OnSavesSave;
        root.Q<Button>("btn-saves-load").clicked   += OnSavesLoad;
        root.Q<Button>("btn-saves-delete").clicked += OnSavesDelete;
        root.Q<Button>("btn-saves-haskey").clicked += OnSavesHasKey;

        // ── Leaderboards page
        _lbAdapterLabel = root.Q<Label>("lb-adapter-label");
        _lbStatusLabel  = root.Q<Label>("lb-status-label");
        _lbInitLabel    = root.Q<Label>("lb-init-label");
        _lbAuthLabel    = root.Q<Label>("lb-auth-label");
        _lbIdField      = root.Q<TextField>("lb-id-field");
        _lbScoreField   = root.Q<TextField>("lb-score-field");
        _lbEntriesList  = root.Q<ScrollView>("lb-entries-list");

        root.Q<Button>("btn-back-leaderboards").clicked += ShowMainPage;
        root.Q<Button>("btn-authenticate").clicked      += OnAuthenticate;
        root.Q<Button>("btn-submit-score").clicked      += OnSubmitScore;
        root.Q<Button>("btn-get-entries").clicked       += OnGetEntries;
        root.Q<Button>("btn-get-my-entry").clicked      += OnGetMyEntry;
        root.Q<Button>("btn-refresh-lb").clicked        += RefreshLeaderboardsStatus;

        // ── Rate page
        _rateAdapterLabel  = root.Q<Label>("rate-adapter-label");
        _rateStatusLabel   = root.Q<Label>("rate-status-label");
        _rateSupportedLabel = root.Q<Label>("rate-supported-label");
        _rateInitLabel      = root.Q<Label>("rate-init-label");

        root.Q<Button>("btn-back-rate").clicked      += ShowMainPage;
        root.Q<Button>("btn-request-review").clicked += OnRequestReview;

        // ── Share page
        _shareAdapterLabel  = root.Q<Label>("share-adapter-label");
        _shareStatusLabel   = root.Q<Label>("share-status-label");
        _shareSupportedLabel = root.Q<Label>("share-supported-label");
        _shareInitLabel      = root.Q<Label>("share-init-label");

        root.Q<Button>("btn-back-share").clicked         += ShowMainPage;
        root.Q<Button>("btn-share-text").clicked         += OnShareText;
        root.Q<Button>("btn-share-image").clicked        += OnShareImage;
        root.Q<Button>("btn-share-text-image").clicked   += OnShareTextAndImage;

        // ── Events
        UniBridge.UniBridge.OnInitSuccess        += OnAdsInitSuccess;
        UniBridge.UniBridge.OnInitFailed         += OnAdsInitFailed;
        UniBridge.UniBridge.OnBannerLoaded       += RefreshAdsStatus;
        UniBridge.UniBridge.OnInterstitialClosed += RefreshAdsStatus;
        UniBridge.UniBridge.OnRewardClosed       += RefreshAdsStatus;

        UniBridgePurchases.OnInitSuccess     += OnPurchasesInitSuccess;
        UniBridgePurchases.OnInitFailed      += OnPurchasesInitFailed;
        UniBridgePurchases.OnPurchaseSuccess += OnGlobalPurchaseSuccess;
        UniBridgePurchases.OnPurchaseFailed  += OnGlobalPurchaseFailed;

        UniBridgeLeaderboards.OnInitSuccess += OnLeaderboardsInitSuccess;
        UniBridgeLeaderboards.OnInitFailed  += OnLeaderboardsInitFailed;

        UniBridgeRate.OnInitSuccess += OnRateInitSuccess;
        UniBridgeRate.OnInitFailed  += OnRateInitFailed;

        UniBridgeShare.OnInitSuccess += OnShareInitSuccess;
        UniBridgeShare.OnInitFailed  += OnShareInitFailed;

        ShowMainPage();
    }

    private void OnDestroy()
    {
        UniBridge.UniBridge.OnInitSuccess        -= OnAdsInitSuccess;
        UniBridge.UniBridge.OnInitFailed         -= OnAdsInitFailed;
        UniBridge.UniBridge.OnBannerLoaded       -= RefreshAdsStatus;
        UniBridge.UniBridge.OnInterstitialClosed -= RefreshAdsStatus;
        UniBridge.UniBridge.OnRewardClosed       -= RefreshAdsStatus;

        UniBridgePurchases.OnInitSuccess     -= OnPurchasesInitSuccess;
        UniBridgePurchases.OnInitFailed      -= OnPurchasesInitFailed;
        UniBridgePurchases.OnPurchaseSuccess -= OnGlobalPurchaseSuccess;
        UniBridgePurchases.OnPurchaseFailed  -= OnGlobalPurchaseFailed;

        UniBridgeLeaderboards.OnInitSuccess -= OnLeaderboardsInitSuccess;
        UniBridgeLeaderboards.OnInitFailed  -= OnLeaderboardsInitFailed;

        UniBridgeRate.OnInitSuccess -= OnRateInitSuccess;
        UniBridgeRate.OnInitFailed  -= OnRateInitFailed;

        UniBridgeShare.OnInitSuccess -= OnShareInitSuccess;
        UniBridgeShare.OnInitFailed  -= OnShareInitFailed;
    }

    // ── Navigation ──────────────────────────────────────────────────────

    private void HideAllPages()
    {
        _mainPage.style.display         = DisplayStyle.None;
        _adsPage.style.display          = DisplayStyle.None;
        _purchasesPage.style.display    = DisplayStyle.None;
        _leaderboardsPage.style.display = DisplayStyle.None;
        _ratePage.style.display         = DisplayStyle.None;
        _savesPage.style.display        = DisplayStyle.None;
        _sharePage.style.display        = DisplayStyle.None;
    }

    private void ShowMainPage()
    {
        HideAllPages();
        _mainPage.style.display = DisplayStyle.Flex;
        RefreshMainStatus();
    }

    private void ShowSavesPage()
    {
        HideAllPages();
        _savesPage.style.display = DisplayStyle.Flex;
        RefreshSavesStatus();
    }

    private void ShowLeaderboardsPage()
    {
        HideAllPages();
        _leaderboardsPage.style.display = DisplayStyle.Flex;
        RefreshLeaderboardsStatus();
    }

    private void ShowRatePage()
    {
        HideAllPages();
        _ratePage.style.display = DisplayStyle.Flex;
        RefreshRateStatus();
    }

    private void ShowAdsPage()
    {
        HideAllPages();
        _adsPage.style.display = DisplayStyle.Flex;
        RefreshAdsStatus();
    }

    private void ShowPurchasesPage()
    {
        HideAllPages();
        _purchasesPage.style.display = DisplayStyle.Flex;
        RefreshPurchasesStatus();
        BuildProductList();
    }

    private void ShowSharePage()
    {
        HideAllPages();
        _sharePage.style.display = DisplayStyle.Flex;
        RefreshShareStatus();
    }

    // ── Main page ───────────────────────────────────────────────────────

    private void RefreshMainStatus()
    {
        bool adsOk   = UniBridge.UniBridge.IsInitialized;
        bool purchOk = UniBridgePurchases.IsInitialized;
        bool lbOk    = UniBridgeLeaderboards.IsInitialized;
        bool rateOk  = UniBridgeRate.IsInitialized;
        bool savesOk = UniBridgeSaves.IsInitialized;
        _mainStatusLabel.text =
            $"Ads: {(adsOk ? "Ready" : "—")}  |  " +
            $"Purchases: {(purchOk ? "Ready" : "—")}  |  " +
            $"LB: {(lbOk ? "Ready" : "—")}  |  " +
            $"Rate: {(rateOk ? "Ready" : "—")}  |  " +
            $"Saves: {(savesOk ? "Ready" : "—")}  |  " +
            $"Share: {(SharingServices.IsInitialized ? "Ready" : "—")}";
    }

    // ── Ads page ────────────────────────────────────────────────────────

    private void OnBannerShow()
    {
        UniBridge.UniBridge.ShowBanner();
        SetAdsStatus("Banner: Show called");
        RefreshAdsStatus();
    }

    private void OnBannerHide()
    {
        UniBridge.UniBridge.HideBanner();
        SetAdsStatus("Banner: Hide called");
        RefreshAdsStatus();
    }

    private void OnBannerDestroy()
    {
        UniBridge.UniBridge.DestroyBanner();
        SetAdsStatus("Banner: Destroy called");
        RefreshAdsStatus();
    }

    private void OnInterstitialShow()
    {
        SetAdsStatus("Interstitial: Showing...");
        UniBridge.UniBridge.ShowInterstitial(status =>
        {
            SetAdsStatus($"Interstitial: {status}");
            RefreshAdsStatus();
        });
    }

    private void OnRewardShow()
    {
        SetAdsStatus("Reward: Showing...");
        UniBridge.UniBridge.ShowReward(status =>
        {
            SetAdsStatus($"Reward: {status}");
            RefreshAdsStatus();
        });
    }

    private void OnDisableAds()
    {
        UniBridge.UniBridge.DisableAds();
        SetAdsStatus("Ads disabled (PlayerPrefs persisted)");
        RefreshAdsStatus();
    }

    private void OnEnableAds()
    {
        UniBridge.UniBridge.EnableAds();
        SetAdsStatus("Ads enabled");
        RefreshAdsStatus();
    }

    private void RefreshAdsStatus()
    {
        bool isInit = UniBridge.UniBridge.IsInitialized;
        _adapterLabel.text           = $"Adapter: {UniBridge.UniBridge.AdapterName}";
        _initLabel.text              = $"Initialized: {isInit} | AdsDisabled: {(isInit ? UniBridge.UniBridge.AdsDisabled.ToString() : "—")}";
        _bannerReadyLabel.text       = $"Ready: {(isInit ? UniBridge.UniBridge.IsBannerReady().ToString() : "—")}";
        _interstitialReadyLabel.text = $"Ready: {(isInit ? UniBridge.UniBridge.IsInterstitialReady().ToString() : "—")}";
        _rewardReadyLabel.text       = $"Ready: {(isInit ? UniBridge.UniBridge.IsRewardReady().ToString() : "—")}";
    }

    private void SetAdsStatus(string message)
    {
        _statusLabel.text = $"Status: {message}";
        Debug.Log($"[DemoMainUI] Ads: {message}");
    }

    private void OnAdsInitSuccess()
    {
        SetAdsStatus("UniBridge initialized successfully");
        RefreshAdsStatus();
        RefreshMainStatus();
    }

    private void OnAdsInitFailed()
    {
        SetAdsStatus("UniBridge initialization FAILED");
        RefreshAdsStatus();
        RefreshMainStatus();
    }

    // ── Purchases page ──────────────────────────────────────────────────

    private void RefreshPurchasesStatus()
    {
        bool isInit = UniBridgePurchases.IsInitialized;
        _purchaseAdapterLabel.text = $"Adapter: {UniBridgePurchases.AdapterName}";
        _purchaseInitLabel.text    = $"Initialized: {isInit} | Supported: {(isInit ? UniBridgePurchases.IsSupported.ToString() : "—")}";
    }

    private void SetPurchasesStatus(string message)
    {
        _purchaseStatusLabel.text = $"Status: {message}";
        Debug.Log($"[DemoMainUI] Purchases: {message}");
    }

    private void BuildProductList()
    {
        _purchaseList.Clear();

        if (!UniBridgePurchases.IsInitialized)
        {
            var label = new Label("UniBridgePurchases not initialized");
            label.AddToClassList("status-label");
            _purchaseList.Add(label);
            return;
        }

        UniBridgePurchases.FetchProducts((success, products) =>
        {
            _purchaseList.Clear();

            if (!success || products == null || products.Count == 0)
            {
                var label = new Label(success ? "No products found" : "Failed to fetch products");
                label.AddToClassList("status-label");
                _purchaseList.Add(label);
                return;
            }

            foreach (var product in products)
                _purchaseList.Add(CreateProductRow(product));
        });
    }

    private VisualElement CreateProductRow(ProductData product)
    {
        var row = new VisualElement();
        row.AddToClassList("product-item");
        row.name = $"product-{product.ProductId}";

        var info = new VisualElement();
        info.AddToClassList("product-info");

        var nameLabel = new Label(product.LocalizedTitle ?? product.ProductId);
        nameLabel.AddToClassList("product-name");

        var priceLabel = new Label(product.LocalizedPriceString ?? "—");
        priceLabel.AddToClassList("product-price");

        info.Add(nameLabel);
        info.Add(priceLabel);

        bool isConsumable = product.Type == ProductType.Consumable;
        var typeBadge = new Label(isConsumable ? "CONSUMABLE" : "NON-CONSUMABLE");
        typeBadge.AddToClassList("badge");
        typeBadge.AddToClassList(isConsumable ? "badge-consumable" : "badge-nonconsumable");

        bool owned = UniBridgePurchases.IsPurchased(product.ProductId);
        var ownedBadge = new Label(owned ? "OWNED" : "—");
        ownedBadge.name = $"owned-{product.ProductId}";
        ownedBadge.AddToClassList("badge");
        ownedBadge.AddToClassList(owned ? "badge-owned" : "badge-not-owned");

        var buyBtn = new Button();
        buyBtn.text = "Buy";
        buyBtn.AddToClassList("btn");
        buyBtn.AddToClassList("btn-green");
        buyBtn.AddToClassList("btn-buy");

        string productId = product.ProductId;
        buyBtn.clicked += () => OnBuyProduct(productId, ownedBadge);

        row.Add(info);
        row.Add(typeBadge);
        row.Add(ownedBadge);
        row.Add(buyBtn);

        return row;
    }

    private void OnBuyProduct(string productId, Label ownedBadge)
    {
        SetPurchasesStatus($"Buying {productId}...");
        UniBridgePurchases.Buy(productId, result =>
        {
            SetPurchasesStatus($"Buy {productId}: {result.Status}");
            RefreshPurchasesStatus();

            if (result.IsSuccess || result.Status == PurchaseStatus.AlreadyOwned)
            {
                ownedBadge.text = "OWNED";
                ownedBadge.RemoveFromClassList("badge-not-owned");
                ownedBadge.AddToClassList("badge-owned");
            }
        });
    }

    private void OnRefreshPurchases()
    {
        SetPurchasesStatus("Refreshing purchases...");
        UniBridgePurchases.RefreshPurchases(success =>
        {
            SetPurchasesStatus(success ? "Purchases refreshed" : "Refresh failed");
            RefreshPurchasesStatus();
            BuildProductList();
        });
    }

    private void OnRestorePurchases()
    {
        SetPurchasesStatus("Restoring purchases...");
        UniBridgePurchases.RestorePurchases(success =>
        {
            SetPurchasesStatus(success ? "Purchases restored" : "Restore failed");
            RefreshPurchasesStatus();
            BuildProductList();
        });
    }

    private void OnPurchasesInitSuccess()
    {
        SetPurchasesStatus("UniBridgePurchases initialized");
        RefreshPurchasesStatus();
        RefreshMainStatus();
    }

    private void OnPurchasesInitFailed()
    {
        SetPurchasesStatus("UniBridgePurchases initialization FAILED");
        RefreshPurchasesStatus();
        RefreshMainStatus();
    }

    private void OnGlobalPurchaseSuccess(PurchaseResult result)
    {
        Debug.Log($"[DemoMainUI] Purchase success: {result.ProductId}");
    }

    private void OnGlobalPurchaseFailed(PurchaseResult result)
    {
        Debug.Log($"[DemoMainUI] Purchase failed: {result.ProductId} — {result.Status}");
    }

    // ── Leaderboards page ────────────────────────────────────────────────

    private void RefreshLeaderboardsStatus()
    {
        bool isInit = UniBridgeLeaderboards.IsInitialized;
        _lbAdapterLabel.text = $"Adapter: {UniBridgeLeaderboards.AdapterName}";
        _lbInitLabel.text    = $"Initialized: {isInit} | Supported: {(isInit ? UniBridgeLeaderboards.IsSupported.ToString() : "—")} | Mode: {(isInit ? UniBridgeLeaderboards.DisplayMode.ToString() : "—")}";
        _lbAuthLabel.text    = $"Authenticated: {(isInit ? UniBridgeLeaderboards.IsAuthenticated.ToString() : "—")} | Player: {(isInit && UniBridgeLeaderboards.IsAuthenticated ? UniBridgeLeaderboards.LocalPlayerName : "—")}";
    }

    private void SetLeaderboardsStatus(string message)
    {
        _lbStatusLabel.text = $"Status: {message}";
        Debug.Log($"[DemoMainUI] Leaderboards: {message}");
    }

    private void OnAuthenticate()
    {
        SetLeaderboardsStatus("Authenticating...");
        UniBridgeAuth.Authorize(success =>
        {
            SetLeaderboardsStatus(success ? $"Authenticated: {UniBridgeAuth.IsAuthorized}" : "Authentication failed");
            RefreshLeaderboardsStatus();
        });
    }

    private void OnSubmitScore()
    {
        string id = _lbIdField.value;
        if (!long.TryParse(_lbScoreField.value, out long score))
        {
            SetLeaderboardsStatus("Неверный формат очков");
            return;
        }
        SetLeaderboardsStatus($"Submitting {score} to '{id}'...");
        UniBridgeLeaderboards.SubmitScore(id, score, ok =>
        {
            SetLeaderboardsStatus(ok ? $"Score {score} submitted to '{id}'" : "Submit failed");
            RefreshLeaderboardsStatus();
        });
    }

    private void OnGetEntries()
    {
        string id = _lbIdField.value;
        SetLeaderboardsStatus($"Getting entries for '{id}'...");
        UniBridgeLeaderboards.GetEntries(id, 10, LeaderboardTimeScope.AllTime, (ok, entries) =>
        {
            if (!ok || entries == null) { SetLeaderboardsStatus("GetEntries failed"); return; }
            SetLeaderboardsStatus($"Got {entries.Count} entries for '{id}'");
            BuildEntriesList(entries);
        });
    }

    private void OnGetMyEntry()
    {
        string id = _lbIdField.value;
        SetLeaderboardsStatus($"Getting my entry for '{id}'...");
        UniBridgeLeaderboards.GetPlayerEntry(id, LeaderboardTimeScope.AllTime, (ok, entry) =>
        {
            if (!ok || entry == null) { SetLeaderboardsStatus("GetPlayerEntry failed"); return; }
            SetLeaderboardsStatus($"My entry: #{entry.Rank} {entry.PlayerName} — {entry.Score}");
            BuildEntriesList(new[] { entry });
        });
    }

    private void BuildEntriesList(IEnumerable<LeaderboardEntry> entries)
    {
        _lbEntriesList.Clear();
        foreach (var entry in entries)
            _lbEntriesList.Add(CreateEntryRow(entry));
    }

    private VisualElement CreateEntryRow(LeaderboardEntry entry)
    {
        var row = new VisualElement();
        row.AddToClassList("lb-entry");
        if (entry.IsCurrentPlayer)
            row.AddToClassList("lb-entry-current");

        var rank = new Label($"#{entry.Rank}");
        rank.AddToClassList("lb-rank");

        var name = new Label(entry.PlayerName);
        name.AddToClassList("lb-name");

        var score = new Label(entry.Score.ToString());
        score.AddToClassList("lb-score");

        row.Add(rank);
        row.Add(name);
        row.Add(score);

        if (entry.IsCurrentPlayer)
        {
            var badge = new Label("YOU");
            badge.AddToClassList("badge");
            badge.AddToClassList("badge-you");
            row.Add(badge);
        }

        return row;
    }

    private void OnLeaderboardsInitSuccess()
    {
        SetLeaderboardsStatus("UniBridgeLeaderboards initialized");
        RefreshLeaderboardsStatus();
        RefreshMainStatus();
    }

    private void OnLeaderboardsInitFailed()
    {
        SetLeaderboardsStatus("UniBridgeLeaderboards initialization FAILED");
        RefreshLeaderboardsStatus();
        RefreshMainStatus();
    }

    // ── Rate page ────────────────────────────────────────────────────────

    private void RefreshRateStatus()
    {
        bool isInit = UniBridgeRate.IsInitialized;
        _rateAdapterLabel.text   = $"Adapter: {UniBridgeRate.AdapterName}";
        _rateInitLabel.text      = $"Initialized: {isInit}";
        _rateSupportedLabel.text = $"Supported: {(isInit ? UniBridgeRate.IsSupported.ToString() : "—")}";
    }

    private void SetRateStatus(string message)
    {
        _rateStatusLabel.text = $"Status: {message}";
        Debug.Log($"[DemoMainUI] Rate: {message}");
    }

    private void OnRequestReview()
    {
        SetRateStatus("Запрашиваем оценку...");
        UniBridgeRate.RequestReview(ok =>
        {
            SetRateStatus(ok ? "Диалог оценки завершён" : "Запрос оценки не удался");
            RefreshRateStatus();
        });
    }

    private void OnRateInitSuccess()
    {
        SetRateStatus("UniBridgeRate initialized");
        RefreshRateStatus();
        RefreshMainStatus();
    }

    private void OnRateInitFailed()
    {
        SetRateStatus("UniBridgeRate initialization FAILED");
        RefreshRateStatus();
        RefreshMainStatus();
    }

    // ── Share page ────────────────────────────────────────────────────────

    private void RefreshShareStatus()
    {
        bool isInit = SharingServices.IsInitialized;
        _shareAdapterLabel.text   = $"Adapter: {UniBridgeShare.AdapterName}";
        _shareInitLabel.text      = $"Initialized: {isInit}";
        _shareSupportedLabel.text = $"Supported: {(isInit ? SharingServices.IsSupported.ToString() : "—")}";
    }

    private void SetShareStatus(string message)
    {
        _shareStatusLabel.text = $"Status: {message}";
        Debug.Log($"[DemoMainUI] Share: {message}");
    }

    private void OnShareText()
    {
        SetShareStatus("Шаринг текста...");
        SharingServices.ShowShareSheet(
            (result, error) =>
            {
                if (error != null)
                {
                    SetShareStatus($"Ошибка: {error.Description}");
                }
                else
                {
                    SetShareStatus($"Результат: {result.ResultCode}");
                }
                RefreshShareStatus();
            },
            ShareItem.Text("Привет из игры! #UniBridge")
        );
    }

    private void OnShareImage()
    {
        SetShareStatus("Шаринг скриншота...");
        SharingServices.ShowShareSheet(
            (result, error) =>
            {
                if (error != null)
                {
                    SetShareStatus($"Ошибка: {error.Description}");
                }
                else
                {
                    SetShareStatus($"Результат: {result.ResultCode}");
                }
                RefreshShareStatus();
            },
            ShareItem.Screenshot()
        );
    }

    private void OnShareTextAndImage()
    {
        SetShareStatus("Шаринг текста и скриншота...");
        SharingServices.ShowShareSheet(
            (result, error) =>
            {
                if (error != null)
                {
                    SetShareStatus($"Ошибка: {error.Description}");
                }
                else
                {
                    SetShareStatus($"Результат: {result.ResultCode}");
                }
                RefreshShareStatus();
            },
            ShareItem.Text("Моё достижение в игре! #UniBridge"),
            ShareItem.Screenshot()
        );
    }

    private void OnShareInitSuccess()
    {
        SetShareStatus("UniBridgeShare initialized");
        RefreshShareStatus();
        RefreshMainStatus();
    }

    private void OnShareInitFailed()
    {
        SetShareStatus("UniBridgeShare initialization FAILED");
        RefreshShareStatus();
        RefreshMainStatus();
    }

    // ── Saves page ────────────────────────────────────────────────────────

    private void RefreshSavesStatus()
    {
        bool isInit = UniBridgeSaves.IsInitialized;
        _savesAdapterLabel.text = $"Adapter: {UniBridgeSaves.AdapterName}";
        _savesInitLabel.text    = $"Initialized: {isInit}";
    }

    private void SetSavesStatus(string message)
    {
        _savesStatusLabel.text = $"Status: {message}";
        Debug.Log($"[DemoMainUI] Saves: {message}");
    }

    private void OnSavesSave()
    {
        string key  = _savesKeyField.value;
        string json = _savesValueField.value;
        SetSavesStatus($"Saving key '{key}'...");

        // Wrap plain string in a serializable container so JsonUtility doesn't complain
        UniBridgeSaves.Save(key, new RawJsonWrapper { data = json }, success =>
        {
            SetSavesStatus(success ? $"Saved key '{key}'" : $"Failed to save '{key}'");
            RefreshSavesStatus();
        });
    }

    private void OnSavesLoad()
    {
        string key = _savesKeyField.value;
        SetSavesStatus($"Loading key '{key}'...");

        UniBridgeSaves.Load<RawJsonWrapper>(key, (success, wrapper) =>
        {
            if (success)
            {
                _savesValueField.value = wrapper?.data ?? "";
                SetSavesStatus($"Loaded key '{key}'");
            }
            else
            {
                SetSavesStatus($"Key '{key}' not found or load failed");
            }
            RefreshSavesStatus();
        });
    }

    private void OnSavesDelete()
    {
        string key = _savesKeyField.value;
        SetSavesStatus($"Deleting key '{key}'...");

        UniBridgeSaves.Delete(key, success =>
        {
            SetSavesStatus(success ? $"Deleted key '{key}'" : $"Failed to delete '{key}'");
            RefreshSavesStatus();
        });
    }

    private void OnSavesHasKey()
    {
        string key = _savesKeyField.value;
        SetSavesStatus($"Checking key '{key}'...");

        UniBridgeSaves.HasKey(key, exists =>
        {
            SetSavesStatus($"Key '{key}': {(exists ? "EXISTS" : "not found")}");
            RefreshSavesStatus();
        });
    }

    [System.Serializable]
    private class RawJsonWrapper
    {
        public string data;
    }
}
