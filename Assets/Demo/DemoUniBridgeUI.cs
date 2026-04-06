using UnityEngine;
using UnityEngine.UIElements;

public class DemoUniBridgeUI : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;

    private Label _adapterLabel;
    private Label _statusLabel;
    private Label _bannerReadyLabel;
    private Label _interstitialReadyLabel;
    private Label _rewardReadyLabel;
    private Label _initLabel;

    private void Awake()
    {
        if (_uiDocument == null)
            _uiDocument = GetComponent<UIDocument>();

        var root = _uiDocument.rootVisualElement;

        _adapterLabel           = root.Q<Label>("adapter-label");
        _statusLabel            = root.Q<Label>("status-label");
        _bannerReadyLabel       = root.Q<Label>("banner-ready-label");
        _interstitialReadyLabel = root.Q<Label>("interstitial-ready-label");
        _rewardReadyLabel       = root.Q<Label>("reward-ready-label");
        _initLabel              = root.Q<Label>("init-label");

        root.Q<Button>("btn-banner-show").clicked    += OnBannerShow;
        root.Q<Button>("btn-banner-hide").clicked    += OnBannerHide;
        root.Q<Button>("btn-banner-destroy").clicked += OnBannerDestroy;

        root.Q<Button>("btn-interstitial-show").clicked += OnInterstitialShow;

        root.Q<Button>("btn-reward-show").clicked += OnRewardShow;

        root.Q<Button>("btn-disable-ads").clicked += OnDisableAds;
        root.Q<Button>("btn-enable-ads").clicked  += OnEnableAds;
        root.Q<Button>("btn-refresh").clicked     += RefreshStatus;

        UniBridge.UniBridge.OnInitSuccess        += OnInitSuccess;
        UniBridge.UniBridge.OnInitFailed         += OnInitFailed;
        UniBridge.UniBridge.OnBannerLoaded       += RefreshStatus;
        UniBridge.UniBridge.OnInterstitialClosed += RefreshStatus;
        UniBridge.UniBridge.OnRewardClosed       += RefreshStatus;

        RefreshStatus();
    }

    private void OnDestroy()
    {
        UniBridge.UniBridge.OnInitSuccess        -= OnInitSuccess;
        UniBridge.UniBridge.OnInitFailed         -= OnInitFailed;
        UniBridge.UniBridge.OnBannerLoaded       -= RefreshStatus;
        UniBridge.UniBridge.OnInterstitialClosed -= RefreshStatus;
        UniBridge.UniBridge.OnRewardClosed       -= RefreshStatus;
    }

    // ── Banner ──────────────────────────────────────────────────────────

    private void OnBannerShow()
    {
        UniBridge.UniBridge.ShowBanner();
        SetStatus("Banner: Show called");
        RefreshStatus();
    }

    private void OnBannerHide()
    {
        UniBridge.UniBridge.HideBanner();
        SetStatus("Banner: Hide called");
        RefreshStatus();
    }

    private void OnBannerDestroy()
    {
        UniBridge.UniBridge.DestroyBanner();
        SetStatus("Banner: Destroy called");
        RefreshStatus();
    }

    // ── Interstitial ────────────────────────────────────────────────────

    private void OnInterstitialShow()
    {
        SetStatus("Interstitial: Showing...");
        UniBridge.UniBridge.ShowInterstitial(status =>
        {
            SetStatus($"Interstitial: {status}");
            RefreshStatus();
        });
    }

    // ── Reward ──────────────────────────────────────────────────────────

    private void OnRewardShow()
    {
        SetStatus("Reward: Showing...");
        UniBridge.UniBridge.ShowReward(status =>
        {
            SetStatus($"Reward: {status}");
            RefreshStatus();
        });
    }

    // ── Misc ────────────────────────────────────────────────────────────

    private void OnDisableAds()
    {
        UniBridge.UniBridge.DisableAds();
        SetStatus("Ads disabled (PlayerPrefs persisted)");
        RefreshStatus();
    }

    private void OnEnableAds()
    {
        UniBridge.UniBridge.EnableAds();
        SetStatus("Ads enabled");
        RefreshStatus();
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private void RefreshStatus()
    {
        bool isInit = UniBridge.UniBridge.IsInitialized;
        _adapterLabel.text           = $"Adapter: {UniBridge.UniBridge.AdapterName}";
        _initLabel.text              = $"Initialized: {isInit} | AdsDisabled: {(isInit ? UniBridge.UniBridge.AdsDisabled.ToString() : "—")}";
        _bannerReadyLabel.text       = $"Ready: {(isInit ? UniBridge.UniBridge.IsBannerReady().ToString() : "—")}";
        _interstitialReadyLabel.text = $"Ready: {(isInit ? UniBridge.UniBridge.IsInterstitialReady().ToString() : "—")}";
        _rewardReadyLabel.text       = $"Ready: {(isInit ? UniBridge.UniBridge.IsRewardReady().ToString() : "—")}";
    }

    private void SetStatus(string message)
    {
        if (_statusLabel != null)
            _statusLabel.text = $"Status: {message}";

        Debug.Log($"[DemoUniBridgeUI] {message}");
    }

    private void OnInitSuccess()
    {
        SetStatus("UniBridge initialized successfully");
        RefreshStatus();
    }

    private void OnInitFailed()
    {
        SetStatus("UniBridge initialization FAILED");
        RefreshStatus();
    }
}
