# UniBridge - Universal Advertisement Package

A unified advertisement integration package for Unity supporting multiple platforms:
- **Android/iOS**: LevelPlay (IronSource)
- **WebGL**: Playgama Bridge

## Installation

### Via Unity Package Manager (Git URL)
1. Open **Window > Package Manager**
2. Click **+** > **Add package from git URL**
3. Enter the repository URL
4. LevelPlay SDK will be automatically installed as a dependency

### Manual Installation
Copy the `Assets/UniBridge` folder to your project's Assets directory.

## Setup

### 1. Create Configuration
- Go to **UniBridge > Create Configuration** in the Unity menu
- Or create via **Assets > Create > UniBridge > Configuration**

### 2. Install Platform SDKs

#### For Android/iOS (LevelPlay)
**Automatically installed** as a package dependency. Configure ad unit IDs in UniBridge Settings.

#### For WebGL (Playgama)
1. Go to **UniBridge > Update SDKs**
2. Click **Install Playgama**
3. Wait for installation to complete

### 3. Configure Settings
Open **UniBridge > Settings** and configure:
- Auto Initialize (on/off)
- Age settings for COPPA compliance
- Platform-specific ad unit IDs

## Usage

### Basic Initialization
```csharp
// Auto-initialization (if enabled in config)
// Ads will initialize automatically on app start

// Manual initialization
UniBridge.Initialize(); // Uses default age from config
UniBridge.Initialize(25); // With specific user age
```

### Showing Ads

```csharp
// Interstitial
if (UniBridge.IsInterstitialReady())
{
    UniBridge.ShowInterstitial((status) =>
    {
        Debug.Log($"Interstitial result: {status}");
    });
}

// Rewarded
if (UniBridge.IsRewardReady())
{
    UniBridge.ShowReward((status) =>
    {
        if (status == AdStatus.Completed)
        {
            // Grant reward to user
        }
    });
}

// Banner
UniBridge.ShowBanner();
UniBridge.HideBanner();
```

### Interstitial Timer
```csharp
// Start a timer that shows interstitials every 60 seconds
UniBridge.StartInterstitialTimer(60, loop: true);

// Control the timer
UniBridge.PauseInterstitialTimer();
UniBridge.UnpauseInterstitialTimer();
UniBridge.ResetInterstitialTimer();
UniBridge.StopInterstitialTimer();
```

### Disable Ads (for premium users)
```csharp
UniBridge.DisableAds(); // Persists across sessions
```

### Events
```csharp
UniBridge.OnInitSuccess += () => Debug.Log("Ads initialized!");
UniBridge.OnInitFailed += () => Debug.Log("Ads failed to initialize");
UniBridge.OnInterstitialClosed += () => Debug.Log("Interstitial closed");
UniBridge.OnRewardClosed += () => Debug.Log("Reward closed");
UniBridge.OnBannerLoaded += () => Debug.Log("Banner loaded");
```

## API Reference

### Static Properties
| Property | Type | Description |
|----------|------|-------------|
| `IsInitialized` | bool | Whether UniBridge has been initialized |

### Static Methods
| Method | Description |
|--------|-------------|
| `Initialize(int userAge = -1)` | Initialize the ad system |
| `IsInterstitialReady()` | Check if interstitial is ready |
| `IsRewardReady()` | Check if rewarded ad is ready |
| `IsBannerReady()` | Check if banner is ready |
| `ShowInterstitial(callback, placement)` | Show interstitial ad |
| `ShowReward(callback, placement)` | Show rewarded ad |
| `ShowBanner()` | Show banner ad |
| `HideBanner()` | Hide banner ad |
| `DisableAds()` | Permanently disable ads |
| `StartInterstitialTimer(seconds, loop)` | Start interstitial timer |
| `PauseInterstitialTimer()` | Pause the timer |
| `UnpauseInterstitialTimer()` | Resume the timer |
| `ResetInterstitialTimer()` | Reset the timer |
| `StopInterstitialTimer()` | Stop the timer |

### AdStatus Enum
| Value | Description |
|-------|-------------|
| `None` | Default/unset |
| `NotLoaded` | Ad not loaded yet |
| `Completed` | Ad shown successfully |
| `Failed` | Ad failed to show |
| `Disabled` | Ads are disabled |
| `AlreadyShowing` | An ad is already showing |
| `Canceled` | User canceled the ad |

## Editor Testing

In the Unity Editor, a debug adapter is used that simulates ad behavior:
- Mock UI panels for interstitials and rewards
- 6 rapid clicks anywhere to toggle debug ads on/off
- All ad readiness checks return true

## Platform Detection

UniBridge automatically selects the appropriate adapter:
- **Editor**: Debug adapter (mock ads)
- **Android/iOS** with LevelPlay installed: LevelPlay adapter
- **WebGL** with Playgama installed: Playgama adapter
- **Fallback**: Debug adapter

## COPPA Compliance

Configure `MaxChildrenAge` in the config. Users at or below this age will have:
- Child-directed ad settings enabled
- COPPA-compliant metadata set for all ad networks

## Troubleshooting

### "UniBridgeConfig not found"
Create a configuration via **UniBridge > Create Configuration**

### Ads not showing in builds
1. Verify SDK is installed (check Package Manager)
2. Check ad unit IDs are configured
3. Check console for initialization errors

### LevelPlay issues
- Ensure `com.unity.services.levelplay` package is installed
- Configure app keys in LevelPlayMediationSettings

### Playgama issues
- Only works in WebGL builds
- Must be deployed to a Playgama-supported platform

## License

Internal use only.
