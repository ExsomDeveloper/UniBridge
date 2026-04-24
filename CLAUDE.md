# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**UniBridge** is a Unity package providing unified cross-platform APIs for:
- **Ads**: LevelPlay (Android/iOS), Yandex Mobile Ads (Android/iOS), Playgama (WebGL), YouTube Playables (WebGL, interstitial + rewarded), Debug mock (Editor)
- **Purchases**: Unity IAP (Google Play / App Store), RuStore Pay (Android), Playgama (WebGL), Debug mock (Editor)
- **Leaderboards**: Google Play Games Services (Android), Game Center (iOS), Playgama (WebGL), YouTube Playables (WebGL), Simulated fallback, Debug mock (Editor)
- **Saves**: Google Play Saved Games (Android), iCloud KV Store (iOS), Playgama (WebGL), YouTube Playables (WebGL), Simulated (PlayerPrefs), LocalSaveSource (file system, fallback)
- **Rate / Reviews**: Google Play Review (Android), RuStore Review (Android), App Store Review (iOS built-in), Playgama (WebGL), Mock (unsupported), Debug mock (Editor)
- **Share**: Android native, iOS native, Playgama Bridge (WebGL), Mock, Debug mock (Editor)
- **Analytics**: AppMetrica (Android/iOS), Debug mock (Editor)
- **Auth**: Google Play Games Services (Android), Game Center (iOS), Playgama (WebGL), Mock, Debug mock (Editor)
- **Environment**: language, audio/pause/visibility state, platform ID, platform messaging (Playgama, YouTube Playables)
- **StoreScreenshots** (tooling): Editor utility for capturing store screenshots at multiple resolutions

### Installed SDK Versions

| SDK | Version | Package ID | Define |
|-----|---------|------------|--------|
| LevelPlay | 9.4.0 | `com.unity.services.levelplay` | `UNIBRIDGE_LEVELPLAY` |
| Playgama Bridge | 1.29.0 | `com.playgama.bridge` | `UNIBRIDGE_PLAYGAMA` |
| Yandex Mobile Ads | 7.18.2 | (manual) | `UNIBRIDGE_YANDEX` |
| Unity Purchasing (IAP) | 5.2.1 | `com.unity.purchasing` | `UNIBRIDGEPURCHASES_IAP` |
| RuStore Pay | 10.2.0 | `ru.rustore.pay` (+ `ru.rustore.core`) | `UNIBRIDGEPURCHASES_RUSTORE` |
| Google Play Games Services | 2.1.0 | `com.google.play.games` | `UNIBRIDGELEADERBOARDS_GPGS` |
| Google Play Review | 1.8.4 | `com.google.play.review` | `UNIBRIDGERATE_GOOGLEPLAY` |
| RuStore Review | 10.2.0 | `ru.rustore.review` | `UNIBRIDGERATE_RUSTORE` |
| AppMetrica | 6.8.0 | `io.appmetrica.analytics` | `UNIBRIDGEANALYTICS_APPMETRICA` |
| EDM4U | 1.2.187 | `com.google.external-dependency-manager` | — |

## Unity Editor Workflows

No CLI build commands — all operations inside the Unity Editor:

- **SDK Installer**: `UniBridge > SDK Installer` — install/update/remove any SDK
- **Build Manager**: `UniBridge > Build Manager` — select store target before building (required)
- **Settings**: `UniBridge > Settings` — tabbed window with per-subsystem configuration
- **WebGL Templates**: `UniBridge > WebGL Templates > Install YouTube Playables` — installs WebGL template
- **Create Configurations**: `Assets > Create > UniBridge > *` for each subsystem config
- **StoreScreenshots**: `Window > StoreScreenshots > Screenshot Manager`
- **Before building**: exactly one `UNIBRIDGE_STORE_*` define must be set via Build Manager (not `UNIBRIDGE_STORE_EDITOR`)

## Architecture

### Static Facade Pattern

Top-level facades, all follow the same pattern — static class, `AutoInitialize()` via `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`, `IsInitialized`, `AdapterName`, `OnInitSuccess/OnInitFailed` events:

- `UniBridge` (`Runtime/UniBridgeAds.cs`) — ad operations; the class itself is named `UniBridge` though the file is `UniBridgeAds.cs`; MonoBehaviour root created as `DontDestroyOnLoad`
- `UniBridgePurchases` (`Runtime/Purchases/UniBridgePurchases.cs`)
- `UniBridgeLeaderboards` (`Runtime/Leaderboards/UniBridgeLeaderboards.cs`)
- `UniBridgeSaves` (`Runtime/UniBridgeSaves.cs`)
- `UniBridgeRate` (`Runtime/Rate/UniBridgeRate.cs`)
- `UniBridgeShare` (`Runtime/Share/UniBridgeShare.cs`)
- `UniBridgeAnalytics` (`Runtime/Analytics/UniBridgeAnalytics.cs`) — silently no-ops if no adapter configured
- `UniBridgeAuth` (`Runtime/Auth/UniBridgeAuth.cs`)
- `UniBridgeEnvironment` (`Runtime/UniBridgeEnvironment.cs`) — not a MonoBehaviour; platform env + messaging

### UniBridgeEnvironment

Static utility. Provides platform-agnostic access via `IPlatformParamsProvider` (`Runtime/IPlatformParamsProvider.cs`). Providers self-register at `AfterAssembliesLoaded`:
- `PlaygamaPlatformProvider` (`Runtime/Adapters/Playgama/PlaygamaPlatformProvider.cs`) — Playgama WebGL
- `YouTubePlayablesPlatformProvider` (`Runtime/Adapters/YouTubePlayables/YouTubePlayablesPlatformProvider.cs`) — YouTube Playables WebGL

**Public API:**
- `GetPlatformId()` — platform ID (e.g. `"yandex"`, `"youtube"`, `"vk"`); `""` on unsupported
- `GetLanguage()` — ISO 639-1 code; RuStore hardcoded to `"ru"`, others use provider or `Application.systemLanguage`
- `IsAudioEnabled`, `IsPaused`, `IsVisible` (defaults: true/false/true)
- `SendMessage(PlatformMessage)` — logs via `Debug.Log($"[{nameof(UniBridgeEnvironment)}] ...")` then forwards to provider; no-op if no provider
- Events: `AudioStateChanged`, `PauseStateChanged`, `VisibilityStateChanged`

`PlatformMessage` enum values: `GameReady`, `FirstFrameReady`, `InGameLoadingStarted`, `InGameLoadingStopped`, `GameplayStarted`, `GameplayStopped`, `PlayerGotAchievement`.

`PlatformId` enum (for `DisableIfNotSupported` blocklists): `GameDistribution`, `Telegram`, `Y8`, `Lagged`, `Huawei`, `Msn`, `Discord`, `GamePush`, `CrazyGames`, `Facebook`, `Yandex`, `YouTube`, `Xiaomi`, `Vk`.

### Adapter & Registry Pattern

Each subsystem uses the same structure:
1. **Interface** (`IAdSource`, `IPurchaseSource`, `ILeaderboardSource`, `ISaveSource`, `IRateSource`, `IShareSource`, `IAnalyticsSource`, `IAuthSource`)
2. **Registry** (Dictionary-based, keyed by SDK define string) — adapters self-register via `[RuntimeInitializeOnLoadMethod]` with a priority
3. **Builder** (`*SourceBuilder`) — selects adapter at runtime; Editor always returns Debug adapter

**Initialization ordering (Unity runtime stages):**
1. `SubsystemRegistration` — `UniBridgeLogger` subscribes to `Application.logMessageReceivedThreaded` (must run before any `Debug.Log` so early logs aren't lost).
2. `AfterAssembliesLoaded` — all adapters self-register via their respective registries (`AdSourceRegistry`, `LeaderboardSourceRegistry`, `AuthSourceRegistry`, `PurchaseSourceRegistry`, `SaveSourceRegistry`, `RateSourceRegistry`, `ShareSourceRegistry`, `AnalyticsSourceRegistry`) and platform providers via `UniBridgeEnvironment.SetProvider`.
3. `BeforeSceneLoad` — facades' `AutoInitialize` runs (`UniBridge`, `UniBridgePurchases`, `UniBridgeLeaderboards`, `UniBridgeSaves`, `UniBridgeRate`, `UniBridgeShare`, `UniBridgeAnalytics`, `UniBridgeAuth`). Also `UniBridgeLogger` creates the MonoBehaviour overlay.

This gives deterministic ordering Logger → Adapters → Facades without relying on `ModuleInitializer` or intra-stage luck. Within a single stage Unity does NOT guarantee method order, so each role must live on its own stage.

**Adapter selection** (`<Subsystem>SourceRegistry.Create(config)`): checks `config.Preferred*Adapter` — if registered, uses it; otherwise uses highest-priority registered adapter. Virtual keys (not real scripting defines) are handled explicitly in the Builder before consulting the registry:
- **Ads**: `"UNIBRIDGE_ADS_DEBUG"` → `DebugAdSource` on ALL platforms (not just Editor) — for QA builds that need mock-ad UI without real SDK credentials
- **Leaderboards**: `"UNIBRIDGELEADERBOARDS_SIMULATED"` → `SimulatedLeaderboardSource`; `"UNITY_IOS_GAMECENTER"` treated as always installed in Build Manager UI
- **Rate**: `"UNIBRIDGERATE_MOCK"` → `MockRateSource` (`IsSupported=false`); `"UNITY_IOS_STOREREVIEW"` → `AppStoreReviewSource` on iOS
- **Saves**: `"UNIBRIDGESAVES_SIMULATED"` → `SimulatedSaveSource`; `"UNITY_IOS_ICLOUD"` → `iCloudSaveSource` on iOS+AppStore; `"UNIBRIDGE_NONE"` / `""` → `LocalSaveSource`
- **Share**: `"UNIBRIDGE_NONE"` → null; Editor → `DebugShareSource`; `"UNIBRIDGESHARE_MOCK"` → `MockShareSource`; `UNITY_ANDROID` → `AndroidShareSource`; `UNITY_IOS` → `iOSShareSource`; fallback → registry → `MockShareSource`
- **Auth**: `"UNIBRIDGEAUTH_MOCK"` → `MockAuthSource`; fallback → registry → `MockAuthSource`
- **Analytics**: `"UNIBRIDGE_NONE"` → null (facade silently no-ops — no Debug fallback in release)

Adapter registration is gated by `#if` defines, so only the correct adapters for the current platform compile.

### Scripting Defines

| Define | Meaning | How set |
|--------|---------|---------|
| `UNIBRIDGE_LEVELPLAY` | LevelPlay SDK installed | Auto via `versionDefines` |
| `UNIBRIDGE_PLAYGAMA` | Playgama Bridge installed | Auto via `versionDefines` |
| `UNIBRIDGE_YANDEX` | Yandex Mobile Ads installed | Manual (SDK Installer) |
| `UNIBRIDGE_YTPLAYABLES` | YouTube Playables SDK wrapper installed | Manual (SDK Installer) |
| `UNIBRIDGEPURCHASES_IAP` | Unity Purchasing installed | Auto via `versionDefines` |
| `UNIBRIDGEPURCHASES_RUSTORE` | RuStore Billing tgz installed | Manual (SDK Installer) |
| `UNIBRIDGELEADERBOARDS_GPGS` | Google Play Games Services installed | Auto by `SDKVersionChecker` |
| `UNIBRIDGERATE_GOOGLEPLAY` | Google Play Review installed | Manual (SDK Installer) |
| `UNIBRIDGERATE_RUSTORE` | RuStore Review tgz installed | Manual (SDK Installer) |
| `UNIBRIDGEANALYTICS_APPMETRICA` | AppMetrica SDK installed | Manual (SDK Installer) |
| `UNIBRIDGE_STORE_GOOGLEPLAY` | Build target: Google Play | Build Manager |
| `UNIBRIDGE_STORE_RUSTORE` | Build target: RuStore | Build Manager |
| `UNIBRIDGE_STORE_APPSTORE` | Build target: App Store | Build Manager |
| `UNIBRIDGE_STORE_PLAYGAMA` | Build target: Playgama | Build Manager |
| `UNIBRIDGE_STORE_YOUTUBE` | Build target: YouTube Playables | Build Manager |
| `UNIBRIDGE_STORE_EDITOR` | Editor-only (no real platform, all Debug adapters) | Build Manager / auto on first install |
| `UNIBRIDGE_VERBOSE_LOG` | Verbose adapter logging | Manual |

**Config-only "pseudo-defines"** (stored as `Preferred*Adapter` string values, not in PlayerSettings):
`UNIBRIDGE_NONE`, `UNIBRIDGE_ADS_DEBUG`, `UNIBRIDGELEADERBOARDS_SIMULATED`, `UNIBRIDGESAVES_SIMULATED`, `UNIBRIDGERATE_MOCK`, `UNIBRIDGESHARE_MOCK`, `UNIBRIDGEAUTH_MOCK`, `UNITY_IOS_GAMECENTER`, `UNITY_IOS_ICLOUD`, `UNITY_IOS_STOREREVIEW`.

`ScriptingDefinesManager` batches define changes via `EditorApplication.delayCall` to avoid repeated recompiles. `SDKVersionChecker` auto-detects installed packages on editor load and syncs defines. On first install, if no `UNIBRIDGE_STORE_*` is active, it auto-selects `UNIBRIDGE_STORE_EDITOR`.

### Assembly Definitions

**Runtime base** (all `autoReferenced: true`, no define constraints):
- `UniBridge.Runtime` — core ads + shared; references `Newtonsoft.Json`, all other Runtime asmdefs below
- `UniBridge.Purchases.Runtime`, `UniBridge.Leaderboards.Runtime`, `UniBridge.Rate.Runtime`, `UniBridge.Share.Runtime`, `UniBridge.Analytics.Runtime`, `UniBridge.Auth.Runtime`

**SDK adapter asmdefs** (`autoReferenced: false`, gated by `defineConstraints`):

| asmdef | Define Constraint | Key References |
|--------|------------------|----------------|
| `UniBridge.LevelPlay` | `UNIBRIDGE_LEVELPLAY` | UniBridge.Runtime, Unity.LevelPlay |
| `UniBridge.Playgama` | `UNIBRIDGE_PLAYGAMA` | UniBridge.Runtime, Purchases.Runtime, Leaderboards.Runtime, Auth.Runtime, Playgama.Bridge |
| `UniBridge.Yandex` | `UNIBRIDGE_YANDEX` | UniBridge.Runtime, YandexMobileAds |
| `UniBridge.YouTubePlayables` | `UNIBRIDGE_YTPLAYABLES` | UniBridge.Runtime, Leaderboards.Runtime |
| `UniBridge.Purchases.UnityIAP` | `UNIBRIDGEPURCHASES_IAP` | Purchases.Runtime, UnityEngine.Purchasing |
| `UniBridge.Purchases.RuStore` | `UNIBRIDGEPURCHASES_RUSTORE` | Purchases.Runtime, RuStorePay, RuStoreCore |
| `UniBridge.Leaderboards.GPGS` | `UNIBRIDGELEADERBOARDS_GPGS` | Leaderboards.Runtime, Auth.Runtime, GooglePlayGames |
| `UniBridge.Saves.GPGS` | `UNIBRIDGELEADERBOARDS_GPGS` | UniBridge.Runtime, GooglePlayGames; `GPGSSaveSource` |
| `UniBridge.Rate.GooglePlay` | `UNIBRIDGERATE_GOOGLEPLAY` | Rate.Runtime, Google.Play.Review |
| `UniBridge.Rate.RuStore` | `UNIBRIDGERATE_RUSTORE` | Rate.Runtime; precompiled: RuStoreReview |
| `UniBridge.Rate.Playgama` | `UNIBRIDGE_PLAYGAMA` | Rate.Runtime, Playgama.Bridge |
| `UniBridge.Share.Playgama` | `UNIBRIDGE_PLAYGAMA` | Share.Runtime, Playgama.Bridge |
| `UniBridge.Analytics.AppMetrica` | `UNIBRIDGEANALYTICS_APPMETRICA` | Analytics.Runtime, AppMetrica |

**Editor asmdefs**: `UniBridge.Editor`, `UniBridge.Analytics.Editor`, `UniBridge.Auth.Editor`, `UniBridge.Leaderboards.Editor`, `UniBridge.Purchases.Editor`, `UniBridge.Rate.Editor`, `UniBridge.Share.Editor`.

**Optional async extension**: `UniBridge.Async.Runtime` (`Runtime/Async/`) — gated by `UNIBRIDGE_UNITASK` via `versionDefines` on `com.cysharp.unitask`. Compiles only when UniTask is installed; otherwise silently absent. Provides `UniTask`-based wrappers around every facade (see *Async API* section below).

**Tooling**: `StoreScreenshots.Runtime`, `StoreScreenshots.Editor`.

### Ad Adapters

| Adapter | Priority | `#if` Condition |
|---------|----------|-----------------|
| `LevelPlayAdapter` | 100 | `UNIBRIDGE_LEVELPLAY && (UNITY_ANDROID \|\| UNITY_IOS) && !UNITY_EDITOR` |
| `YandexAdapter` | 90 | `UNIBRIDGE_YANDEX && (UNITY_ANDROID \|\| UNITY_IOS) && !UNITY_EDITOR` |
| `PlaygamaAdapter` | 100 | `UNIBRIDGE_PLAYGAMA && UNITY_WEBGL` |
| `YouTubePlayablesAdSource` | 100 | `UNIBRIDGE_YTPLAYABLES && UNITY_WEBGL` (interstitial + rewarded; `placementName` serves as YouTube's `rewardId`) |
| `DebugAdSource` | — | always (Editor); also on any platform when `PreferredAdsAdapter == "UNIBRIDGE_ADS_DEBUG"` |

### Purchase Adapters

| Adapter | `#if` Condition |
|---------|-----------------|
| `UnityIAPPurchaseSource` | `(UNITY_ANDROID && UNIBRIDGE_STORE_GOOGLEPLAY) \|\| (UNITY_IOS && UNIBRIDGE_STORE_APPSTORE)` |
| `RuStorePurchaseSource` | `UNITY_ANDROID && UNIBRIDGE_STORE_RUSTORE && !UNITY_EDITOR` |
| `PlaygamaPurchaseSource` | `UNIBRIDGE_PLAYGAMA && UNITY_WEBGL` |
| `DebugPurchaseSource` | always (Editor) |

**`ProductDefinition` fields:**
- `ProjectId` — internal game-facing ID used in all game code (`IsPurchased`, `Purchase`, events, `ProductData.ProductId`). Set in Inspector.
- `ProductId` — store-facing SDK ID sent to Unity IAP / RuStore. Not used in game code — only inside adapters.
- `Type` — `Consumable`, `NonConsumable`, `Subscription`
- `PlaygamaProductId` — optional Playgama override; falls back to `ProductId` if empty
- `PlaygamaAmount` — Playgama price in Gam (1 Gam = $0.10 USD)

All adapter `_ownershipCache` dictionaries and `PurchaseResult.ProductId` use `ProjectId` as key/value. Adapters map `projectId → storeId` on Purchase calls and `storeId → projectId` in SDK callbacks. `ProductData.ProductId` is populated with `projectId`.

`IsPurchased()` is synchronous — each adapter maintains `_ownershipCache` populated during `Initialize` → `RefreshPurchases`. RuStore: `"PAID"` state purchases are auto-confirmed on refresh (crash recovery); only `"CONFIRMED"` populates cache.

`PurchaseStatus`: `None`, `Success`, `Failed`, `Cancelled`, `AlreadyOwned`, `NotInitialized`, `NotSupported`, `PendingConfirmation`, `Restored`. `PurchaseResult` helpers: `IsSuccess`, `IsRestore`, `FromRestored(productId, transactionId)`.

**`AutoInitialize` guard:** `UniBridgePurchases.AutoInitialize` starts with `if (IsInitialized) return;` — runs once per app lifetime.

**Initialization flow:** Facade auto-calls `FetchProducts` after adapter `Initialize` succeeds; `OnInitSuccess` fires only after this auto-fetch completes (graceful degradation on fetch failure).

**`RestorePurchases` — events and restored product list:** `IPurchaseSource.RestorePurchases` takes `Action<bool, IReadOnlyList<string>>` — second parameter is product IDs newly added to ownership cache (diff). The facade fires `OnRestoreSuccess(productId)` per newly-added ID.

Adapter nuances:
- **UnityIAP iOS**: `apple.RestoreTransactions` internally calls `ProcessPurchase` (fires `Success`). `Restored` fires only for the diff to avoid double-activation.
- **UnityIAP Android**: Google Play restores ownership on `Initialize`; `RestorePurchases` rebuilds cache from local receipts, diff is always empty.
- **RuStore / Debug**: `Restored` fires for all owned products after `RefreshPurchases`.
- **Playgama**: `Restored` fires for non-consumables; consumables filtered via `IsConsumable()` guard.

**`GetLocalizedPrice(productId)`** — synchronous helper reading `LocalizedPriceString` from cached `Products`. Returns `string.Empty` if unavailable.

### Leaderboard Adapters

| Adapter | Priority | `#if` Condition |
|---------|----------|-----------------|
| `GPGSLeaderboardSource` | 100 | `UNIBRIDGELEADERBOARDS_GPGS && UNITY_ANDROID && UNIBRIDGE_STORE_GOOGLEPLAY && !UNITY_EDITOR` |
| `GameCenterLeaderboardSource` | 90 | `UNITY_IOS && UNIBRIDGE_STORE_APPSTORE && !UNITY_EDITOR` |
| `PlaygamaLeaderboardSource` | 100 | `UNIBRIDGE_PLAYGAMA && UNITY_WEBGL` |
| `YouTubePlayablesLeaderboardSource` | 100 | `UNIBRIDGE_YTPLAYABLES && UNITY_WEBGL` |
| `SimulatedLeaderboardSource` | — | runtime fallback (virtual key) |
| `DebugLeaderboardSource` | — | always (Editor) |

`SimulatedLeaderboardSource` stores scores in PlayerPrefs (`unibridge_leaderboards_score_{id}`), timestamp (`unibridge_leaderboards_date_{id}`), and deterministic bot lists (`unibridge_leaderboards_bots_{id}` JSON) seeded from the leaderboard ID. `timeScope` is ignored. Bot names come from `SimulationSettings.BotNames` (deterministic shuffle; overflow bots get `_2`, `_3` suffixes).

### Rate Adapters

| Adapter | Priority | `#if` Condition |
|---------|----------|-----------------|
| `GooglePlayReviewSource` | 100 | `UNIBRIDGERATE_GOOGLEPLAY && UNITY_ANDROID && UNIBRIDGE_STORE_GOOGLEPLAY && !UNITY_EDITOR` |
| `RuStoreReviewSource` | 100 | `UNIBRIDGERATE_RUSTORE && UNITY_ANDROID && UNIBRIDGE_STORE_RUSTORE && !UNITY_EDITOR` |
| `AppStoreReviewSource` | 90 | `UNITY_IOS && UNIBRIDGE_STORE_APPSTORE && !UNITY_EDITOR` |
| `PlaygamaRateSource` | — | `UNIBRIDGE_PLAYGAMA && UNITY_WEBGL` |
| `MockRateSource` | — | runtime fallback; `IsSupported = false` |
| `DebugRateSource` | — | always (Editor) |

`GooglePlayReviewSource` uses `ReviewManager` from `Google.Play.Review` via a temporary `CoroutineRunner` MonoBehaviour. `RuStoreReviewSource` uses `RuStoreReviewManager.Instance`.

### Save Adapters

| Adapter | Priority | `#if` Condition |
|---------|----------|-----------------|
| `GPGSSaveSource` | 100 | `UNIBRIDGELEADERBOARDS_GPGS && UNITY_ANDROID && UNIBRIDGE_STORE_GOOGLEPLAY && !UNITY_EDITOR` |
| `PlaygamaSaveSource` | 100 | `UNIBRIDGE_PLAYGAMA && UNITY_WEBGL` |
| `YouTubePlayablesSaveSource` | — | `UNIBRIDGE_YTPLAYABLES && UNITY_WEBGL` |
| `iCloudSaveSource` | — | `UNITY_IOS && UNIBRIDGE_STORE_APPSTORE && !UNITY_EDITOR` (virtual key, handled in Builder) |
| `SimulatedSaveSource` | — | always (virtual key, PlayerPrefs `unibridge_saves_sim_` prefix) |
| `LocalSaveSource` | — | always (file system fallback) |

`GPGSSaveSource` stores all keys in a single GPGS snapshot named `"unibridge_saves_data"` as a JSON dict `{entries:[{k,v}]}`. Conflict resolution: `UseLongestPlaytime`. Requires `Social.localUser.authenticated`.

`iCloudSaveSource` uses `NSUbiquitousKeyValueStore` via native Obj-C plugin (`Assets/UniBridge/Plugins/iOS/UniBridgeSavesiOS.mm`). Falls back to `LocalSaveSource` if iCloud unavailable (entitlement missing or not signed in). Requires iCloud KV Storage capability in Xcode.

**Saves API nuances:**
- `UniBridgeSaves.Load<T>(key, Action<bool, T>)` — typed load; internally deserializes the stored JSON via `Newtonsoft.Json`. Will report `(false, default)` via `onComplete` on deserialization failure (error is logged).
- `UniBridgeSaves.LoadRaw(key, Action<bool, string>)` — returns the raw JSON string without deserialization. Use when the caller manages typing itself (e.g. session cache that feeds multiple typed consumers) or when the stored data is already a JSON blob to forward as-is. Avoids the `T = string` pitfall where `JsonConvert.DeserializeObject<string>` fails on object literals.
- `UniBridgeSaves.CurrentSource` — exposes the active `ISaveSource` for diagnostics, tests, or scenarios that need to bypass the JSON layer entirely. Null until `Initialize()` has run.

### Share Adapters

| Adapter | `#if` Condition |
|---------|-----------------|
| `AndroidShareSource` | `UNITY_ANDROID && !UNITY_EDITOR` |
| `iOSShareSource` | `UNITY_IOS && !UNITY_EDITOR` |
| `PlaygamaShareSource` | `UNIBRIDGE_PLAYGAMA && UNITY_WEBGL` (registers in registry, key `"UNIBRIDGE_PLAYGAMA"`, priority 100) |
| `MockShareSource` | runtime fallback; `IsSupported = false` |
| `DebugShareSource` | always (Editor) |

**Share public API:** `SharingServices.ShowShareSheet(callback, params ShareItem[])`. `ShareItem` factory: `Text(string)`, `Image(Texture2D)`, `ImageUrl(string)`, `Screenshot()` (captures at end of frame). Callback: `(ShareSheetResult, ShareError)` — `error` non-null only on init/data error; `result.Code` is `ShareResultCode.Unknown` on Android (`startActivity` is fire-and-forget). Internally converts items to `ShareData` and calls `UniBridgeShare.Share()`.

**`ShareData` model:** `Text`, `Image` (Texture2D — Android/iOS), `ImageUrl` (string — Playgama/web). Factories: `WithText`, `WithImage`, `WithImageUrl`, `WithTextAndImage`, `WithTextAndImageUrl`.

`AndroidShareSource` shares text via `Intent.ACTION_SEND` + `text/plain`. Images written to `Application.temporaryCachePath` and passed via JNI to `UniBridgeSharePlugin.java` (`Assets/UniBridge/Plugins/Android/UniBridgeSharePlugin.java`). API 29+ uses `MediaStore`, API 24–28 uses `FileProvider` (authority `{Application.identifier}.unibridgeshare.provider`), API 21–23 uses `Uri.fromFile()`. `<provider>` declared via `UniBridgeShareAndroidManifestInjector` (`IPostGenerateGradleAndroidProject`) — injected directly into the generated `unityLibrary/src/main/AndroidManifest.xml`. Resource `unibridgeshare_file_paths.xml` copied to `unityLibrary/src/main/res/xml/`.

`iOSShareSource` calls native Obj-C plugin (`Assets/UniBridge/Plugins/iOS/UniBridgeShareiOS.mm`) via `DllImport`. Uses `UIActivityViewController` with iPad popover support.

`PlaygamaShareSource` uses `Bridge.social.Share(options, callback)` with `text` and/or `imageUrl`.

### Analytics Adapters

| Adapter | `#if` Condition |
|---------|-----------------|
| `AppMetricaAnalyticsSource` | `UNIBRIDGEANALYTICS_APPMETRICA && (UNITY_ANDROID \|\| UNITY_IOS) && !UNITY_EDITOR` |
| `DebugAnalyticsSource` | always (Editor) |

`UniBridgeAnalytics.LogEvent(string, Dictionary<string, object>)` is the public API. No adapter → silently no-ops (no Debug fallback in release). `UniBridgeAnalyticsConfig` is git-ignored (per-project API keys) — create via `Assets > Create > UniBridge > Analytics Configuration`.

### Auth Adapters

| Adapter | Priority | `#if` Condition |
|---------|----------|-----------------|
| `GPGSAuthSource` | 100 | `UNIBRIDGELEADERBOARDS_GPGS && UNITY_ANDROID && UNIBRIDGE_STORE_GOOGLEPLAY && !UNITY_EDITOR` |
| `GameCenterAuthSource` | — | `UNITY_IOS && !UNITY_EDITOR` (virtual key `UNITY_IOS_GAMECENTER`) |
| `PlaygamaAuthSource` | 100 | `UNIBRIDGE_PLAYGAMA && UNITY_WEBGL` |
| `MockAuthSource` | — | runtime fallback; `IsAuthorized = false` |
| `DebugAuthSource` | — | always (Editor) |

### Configuration ScriptableObjects

All configs live in `Assets/UniBridge/Resources/` (loaded via `Resources.Load`), created via `Assets > Create > UniBridge > *`:

- **`UniBridgeConfig`** (class name `UniBridgeConfig` — this is the Ads config) — `AutoInitialize`, `MaxChildrenAge`/`DefaultUserAge` (COPPA), `AdsDisabledKey` (PlayerPrefs), `InterstitialMode`, `AutoInterstitialInterval`, `PreferredAdsAdapter`, plus `LevelPlaySettings`, `PlaygamaSettings`, `YandexSettings`.
- **`UniBridgePurchasesConfig`** — `AutoInitialize`, `Products` (`List<ProductDefinition>`), `PreferredPurchaseAdapter`, `UnityIAPSettings`, `RuStoreSettings` (ConsoleApplicationId, DeeplinkPrefix). Lookup: `FindByProjectId(projectId)`, `FindByStoreProductId(storeId)`.
- **`UniBridgeLeaderboardsConfig`** — `AutoInitialize`, `PreferredLeaderboardAdapter`, `Leaderboards` (`List<LeaderboardDefinition>` with `Id`, `DisplayName`, `GpgsId`, `GameCenterId`), `SimulationSettings` (BotCount, MinScore, MaxScore, PlayerName, BotNames, DailyGrowthMin/Max, LeaderboardSize).
- **`UniBridgeRateConfig`** — `AutoInitialize`, `PreferredRateAdapter`.
- **`UniBridgeSavesConfig`** — `AutoInitialize`, `PreferredSavesAdapter`.
- **`UniBridgeShareConfig`** — `AutoInitialize`, `PreferredShareAdapter`.
- **`UniBridgeAnalyticsConfig`** — `AutoInitialize`, `PreferredAnalyticsAdapter`, `AppMetrica.ApiKey`. Git-ignored.
- **`UniBridgeAuthConfig`** — `AutoInitialize`, `PreferredAuthAdapter`.

### Timer System

`AdTimer` runs coroutines on the `UniBridge` MonoBehaviour root, auto-pauses on `OnApplicationPause`. `InterstitialTimerHandler` is the default `ITimerHandler`; reward ads always pause the timer (resets on success, resumes on failure/cancel).

### Android Auto-Configuration

`StoreAndroidConfigurator.OnStoreChanged(previousDefine, newDefine, shareEnabled)` — single dispatch point for Android file generation, called from Build Manager's `OnSelectClicked()`.

- **`RuStoreAndroidConfigurator`** — auto-generates `Assets/Plugins/Android/UniBridgeMobileKit/RuStoreUnityPay/AndroidManifest.xml` (console app ID + deeplink activity) for RuStore store. Marker: `<!-- UNIBRIDGE_RUSTORE_GENERATED -->`. `EnsureAndroidLibBuildGradle()` writes `RuStoreSDKSettings.androidlib/build.gradle` at build time.
- **`UniBridgeShareAndroidManifestInjector`** (`IPostGenerateGradleAndroidProject`, callbackOrder=100) — injects `<provider>` into the generated `unityLibrary/src/main/AndroidManifest.xml` and copies `unibridgeshare_file_paths.xml` into `unityLibrary/src/main/res/xml/`. Bypasses Unity's manifest merging (same technique as Yandex/RuStore SDKs). Skips if adapter is `UNIBRIDGE_NONE` or `UNIBRIDGESHARE_MOCK`.

UniBridge-owned Android files: `Assets/UniBridge/Plugins/Android/UniBridgeSharePlugin.java`. Auto-generated files under `Assets/Plugins/Android/UniBridgeMobileKit/` (RuStore: committed; UniBridgeShare: git-ignored).

**Why NOT `Assets/Plugins/Android/AndroidManifest.xml`**: conflicts with Yandex SDK's `AndroidManifestPostprocessor` (crashes with NRE if `UnityPlayerActivity` not found) and RuStore's legacy path handling.

### WebGL Templates

- **YouTube Playables template** (`Assets/WebGLTemplates/YouTubePlayables/`) — loads `ytgame` SDK before game. `ytgame.game.firstFrameReady()` is **not** called from the template — the game must call `UniBridgeEnvironment.SendMessage(PlatformMessage.FirstFrameReady)` after the first meaningful frame to hide YouTube's loading spinner. `YouTubePlayablesTemplateInstaller` copies from `Assets/UniBridge/Editor/WebGLTemplates~/YouTubePlayables`. **MUST** (certification): never call `event.preventDefault()` on Escape key — Safari fullscreen uses Esc to exit, and blocking it traps the user in fullscreen. The template sets `keyboardListeningElement: canvas` in `createUnityInstance` and installs a capture-phase listener that logs a warning if any code sets `defaultPrevented` on an Esc event.
- **Playgama Bridge template** (`Assets/WebGLTemplates/Bridge/`) — loads `playgama-bridge.js`.

### Checklist Architecture (Build Manager & SDK Installer)

`IChecklistProvider` interface (`Title`, `GetItems() → ChecklistItem[]`). `ChecklistRegistry` maps defines to providers:
- Store checklists: `STORE_GOOGLEPLAY` → `UniBridgeShareChecklist`, `STORE_RUSTORE` → `UniBridgeShareChecklist`
- SDK checklists: `UNIBRIDGEPURCHASES_RUSTORE` → `RuStoreBillingChecklist`, `UNIBRIDGE_PLAYGAMA` → `PlaygamaChecklist`, `UNIBRIDGE_YTPLAYABLES` → `YouTubePlayablesChecklist`, `UNIBRIDGEANALYTICS_APPMETRICA` → `AppMetricaChecklist`

`ChecklistUIHelper.BuildGroup()` / `BuildItem()` renders shared UI. Build Manager composes: store checklist + SDK checklists for each installed SDK. SDK Installer shows the SDK's checklist in the detail panel.

### Runtime Utilities

- **`DisableOnStore`** (`Runtime/DisableOnStore.cs`) — MonoBehaviour that disables its `GameObject` in `Awake()` if the current store matches the mask. Compile-time `#if UNIBRIDGE_STORE_*` — no runtime overhead.
- **`StoreTargetMask`** (`Runtime/StoreTargetMask.cs`) — `[Flags]` enum: `None`, `GooglePlay`, `RuStore`, `AppStore`, `Playgama`, `Editor`.
- **`DisableIfNotSupported`** (`Runtime/DisableIfNotSupported.cs`) — MonoBehaviour that disables at runtime if the selected `RatSystemCheck` reports `IsSupported = false`. Selectable systems: `Share`, `Leaderboards`, `Purchases`, `Rate`. Subscribes to `OnInitSuccess/OnInitFailed` if not yet initialized. Also supports platform ID blocklist for Playgama web stores.
- **`DisableIfAdNotSupported`** (`Runtime/DisableIfAdNotSupported.cs`) — disables if no ad source or a specific format is unsupported.

## Key Files

| File | Role |
|------|------|
| `Assets/UniBridge/Runtime/UniBridgeAds.cs` | Main ads API (class `UniBridge`) |
| `Assets/UniBridge/Runtime/Purchases/UniBridgePurchases.cs` | Main purchases API |
| `Assets/UniBridge/Runtime/Purchases/ProductDefinition.cs` | Product config: `ProjectId`, `ProductId`, `Type`, `PlaygamaProductId`, `PlaygamaAmount` |
| `Assets/UniBridge/Runtime/Purchases/Adapters/Debug/DebugPurchasePanel.cs` | UI Toolkit purchase dialog for Editor testing |
| `Assets/UniBridge/Runtime/Leaderboards/UniBridgeLeaderboards.cs` | Main leaderboards API |
| `Assets/UniBridge/Runtime/UniBridgeSaves.cs` | Save/load API: `Save<T>`, `Load<T>`, `LoadRaw`, `Delete`, `HasKey`; `IsInitialized`, `AdapterName`, `CurrentSource` |
| `Assets/UniBridge/Runtime/UniBridgeSavesConfig.cs` | Saves config |
| `Assets/UniBridge/Runtime/ISaveSource.cs` | Save adapter interface |
| `Assets/UniBridge/Runtime/SaveSourceRegistry.cs` | Dictionary-based save adapter registry |
| `Assets/UniBridge/Runtime/SaveSourceBuilder.cs` | Selects save adapter; handles virtual keys |
| `Assets/UniBridge/Runtime/Rate/UniBridgeRate.cs` | Main rate/review API |
| `Assets/UniBridge/Runtime/Rate/RateSourceBuilder.cs` | Selects rate adapter |
| `Assets/UniBridge/Runtime/Share/SharingServices.cs` | Public share API — `ShowShareSheet(callback, params ShareItem[])` |
| `Assets/UniBridge/Runtime/Share/UniBridgeShare.cs` | Internal share facade |
| `Assets/UniBridge/Runtime/Share/ShareSourceBuilder.cs` | Selects share adapter |
| `Assets/UniBridge/Runtime/Analytics/UniBridgeAnalytics.cs` | `LogEvent(name, params)` API |
| `Assets/UniBridge/Runtime/Analytics/UniBridgeAnalyticsConfig.cs` | Analytics config (git-ignored) |
| `Assets/UniBridge/Runtime/Auth/UniBridgeAuth.cs` | Main auth API |
| `Assets/UniBridge/Runtime/UniBridgeEnvironment.cs` | Platform environment API |
| `Assets/UniBridge/Runtime/IPlatformParamsProvider.cs` | Platform provider interface |
| `Assets/UniBridge/Runtime/PlatformMessage.cs` | Platform message enum (FirstFrameReady, GameReady, etc.) |
| `Assets/UniBridge/Runtime/Adapters/Playgama/PlaygamaPlatformProvider.cs` | Playgama IPlatformParamsProvider impl |
| `Assets/UniBridge/Runtime/Adapters/YouTubePlayables/YouTubePlayablesPlatformProvider.cs` | YouTube Playables IPlatformParamsProvider impl |
| `Assets/UniBridge/Runtime/Adapters/YouTubePlayables/Plugins/YouTubePlayablesBridge.jslib` | JS bridge for ytgame API |
| `Assets/UniBridge/Runtime/StoreTargetMask.cs` | `[Flags]` enum — stores |
| `Assets/UniBridge/Runtime/DisableOnStore.cs` | Disable GameObject on selected stores |
| `Assets/UniBridge/Runtime/DisableIfNotSupported.cs` | Disable GameObject if subsystem unsupported |
| `Assets/UniBridge/Editor/BuildManager/BuildPreprocessor.cs` | Build validation; generates/deletes link.xml |
| `Assets/UniBridge/Editor/BuildManager/AdapterLinkXmlGenerator.cs` | Generates `Assets/UniBridge/Generated/link.xml` (IL2CPP stripping protection) |
| `Assets/UniBridge/Editor/BuildManager/AdapterDefines.cs` | Define → display-name maps for all subsystems |
| `Assets/UniBridge/Editor/BuildManager/StoreAndroidConfigurator.cs` | Dispatcher for Android file generation |
| `Assets/UniBridge/Editor/BuildManager/RuStoreAndroidConfigurator.cs` | RuStore Android manifest generation |
| `Assets/UniBridge/Editor/BuildManager/UniBridgeShareAndroidManifestInjector.cs` | `IPostGenerateGradleAndroidProject` — injects `<provider>` |
| `Assets/UniBridge/Editor/BuildManager/YouTubePlayablesTemplateInstaller.cs` | Installs WebGL template |
| `Assets/UniBridge/Editor/BuildManager/ChecklistRegistry.cs` | Maps defines → checklist providers |
| `Assets/UniBridge/Editor/BuildManager/StorePresetsManager.cs` | Store preset model + defaults (`storePresets.json`) |
| `Assets/UniBridge/Editor/ChecklistUIHelper.cs` | Shared checklist UI rendering |
| `Assets/UniBridge/Editor/ScriptingDefinesManager.cs` | Batched define management |
| `Assets/UniBridge/Editor/SDKInstallerWindow.cs` | UI Toolkit SDK Installer |
| `Assets/UniBridge/Editor/SDKVersionChecker.cs` | On-load SDK detection and define sync |
| `Assets/UniBridge/Editor/SDKVersions.json` | Canonical SDK versions and package IDs |
| `Assets/UniBridge/Editor/BuildManager/UniBridgeAdsBuildManagerUIWindow.cs` | UI Toolkit Build Manager |
| `Assets/UniBridge/Editor/BuildManager/StorePresets.json` | Store preset configurations |

## Build Validation Rules

`BuildPreprocessor` enforces before every build:
1. Generates `Assets/UniBridge/Generated/link.xml` via `AdapterLinkXmlGenerator.Generate()` (deleted in `OnPostprocessBuild`)
2. On Android: calls `RuStoreAndroidConfigurator.EnsureAndroidLibBuildGradle()`; if share android lib dir missing, calls its configurator (safety net)
3. Exactly one `UNIBRIDGE_STORE_*` must be set; `UNIBRIDGE_STORE_EDITOR` fails the build immediately
4. Store define must match build target (e.g. `UNIBRIDGE_STORE_APPSTORE` requires iOS)
5. Required SDK defines for target platform must be present
6. If `PreferredAdsAdapter` SDK not installed → warning (build proceeds with Debug adapter)

### IL2CPP Adapter Stripping

Adapter assemblies (`UniBridge.Yandex`, `UniBridge.LevelPlay`, etc.) have no direct references from game code — only `[RuntimeInitializeOnLoadMethod]` bootstrap. IL2CPP managed linker can strip the entire assembly before bootstrap runs → Builder falls back to Debug.

`AdapterLinkXmlGenerator` generates `link.xml` with `preserve="all"` for each adapter assembly whose define is selected in configs (`PreferredAdsAdapter`, `PreferredPurchaseAdapter`, `PreferredLeaderboardAdapter`, `PreferredRateAdapter`, `PreferredSavesAdapter`, `PreferredAnalyticsAdapter`, `PreferredAuthAdapter`, `PreferredShareAdapter`). Virtual keys are excluded — they live in core assemblies. Dictionary `AdapterLinkXmlGenerator.AdapterAssemblies` maps SDK define → assembly name.

`.gitignore` excludes auto-generated: `Assets/UniBridge/Generated/`, `Assets/Plugins/Android/UniBridgeMobileKit/UniBridgeShareAndroid.androidlib/`.

## Adding a New Subsystem

The Auth subsystem is the newest — use it as a reference implementation. Standard pattern:

1. **Facade** (`Runtime/NewSubsystem/UniBridgeNewSubsystem.cs`) — static class with `AutoInitialize()`, `IsInitialized`, `AdapterName`, events
2. **Config** (`Runtime/NewSubsystem/UniBridgeNewSubsystemConfig.cs`) — `[CreateAssetMenu("UniBridge/NewSubsystem Configuration")]`
3. **Interface** (`Runtime/NewSubsystem/INewSubsystemSource.cs`)
4. **Builder** (`Runtime/NewSubsystem/NewSubsystemSourceBuilder.cs`) — Editor→Debug adapter; else registry + Mock fallback
5. **Registry** (`Runtime/NewSubsystem/NewSubsystemSourceRegistry.cs`) — Dictionary keyed by SDK define
6. **Debug adapter** (`Runtime/NewSubsystem/Adapters/Debug/DebugNewSubsystemSource.cs`)
7. **asmdefs** — Runtime (`autoReferenced: true`) + Editor (`autoReferenced: false`)
8. **SDK adapters** — self-register via `[RuntimeInitializeOnLoadMethod(AfterAssembliesLoaded)]`, in adapter asmdef with `defineConstraints`
9. **`AdapterLinkXmlGenerator.AdapterAssemblies`** entry per non-virtual define

## Adding a New Ad Adapter

1. Implement `IAdSource`
2. Self-register in `[RuntimeInitializeOnLoadMethod(AfterAssembliesLoaded)]` via `AdSourceRegistry.Register("SDK_DEFINE_KEY", factory, priority)` — key = SDK scripting define
3. Add scripting define; create asmdef gated by it
4. Add to `AdapterDefines.AdsAdapterNames` and `AdapterDefines.GetAdAdapters()`
5. Drawer in `Assets/UniBridge/Editor/Drawers/` implementing `ISettingsDrawer`
6. Platform-specific settings class to `UniBridgeConfig`
7. Add to `StorePresets.json` / `StorePresetsManager.CreateDefaults()`
8. Add `{ "SDK_DEFINE_KEY", "AssemblyName" }` to `AdapterLinkXmlGenerator.AdapterAssemblies`

### Ad Adapter Retry Pattern

All ad wrappers (Banner, Interstitial, Reward) use exponential backoff via `RetryHelper`. Correct pattern uses `_retryPending` flag to prevent duplicate timers:
- `HandleAdFailedToLoad`: set `_retryPending = true` before `RetryHelper.InvokeAfter(delay, RetryLoad)`
- `LoadAd()`: guard with `|| _retryPending` alongside `_isLoading`
- `RetryLoad()`: set `_retryPending = false` first, then check `|| _isLoading` before calling `TryLoadOnce()`

## Adding a New Purchase Adapter

1. Implement `IPurchaseSource` (include `_ownershipCache` keyed by **`ProjectId`**)
2. Self-register via `PurchaseSourceRegistry.Register(factory, priority)`
3. **`Purchase(projectId, ...)`**: `config.FindByProjectId(projectId)` → store ID → SDK. Report `projectId` in `PurchaseResult.ProductId` and cache.
4. **SDK callbacks**: map store ID → `projectId` via `config.FindByStoreProductId(storeId)`. `ProductData.ProductId` = `projectId` in `FetchProducts`.
5. New scripting define; asmdef gated by it
6. Drawer in `Assets/UniBridge/Editor/Purchases/Drawers/`
7. Platform settings class to `UniBridgePurchasesConfig`
8. `AdapterLinkXmlGenerator.AdapterAssemblies` entry
9. In `RestorePurchases`: after cache refresh, fire `OnPurchaseSuccess?.Invoke(PurchaseResult.FromRestored(id))` for owned non-consumables. If platform replays via normal purchase callback (UnityIAP iOS), fire `Restored` only for the diff.

## Adding a New Leaderboard / Rate / Save / Share / Analytics / Auth Adapter

Same pattern as Ads (self-register, asmdef gated by define, `AdapterDefines` entry, store presets, `AdapterLinkXmlGenerator.AdapterAssemblies` for non-virtual keys). For virtual keys, add to `IsAdapterSdkInstalled()` in `UniBridgeAdsBuildManagerUIWindow` (returns `true` unconditionally) and handle explicitly in the Builder before consulting the registry.

## Adding a New Checklist Provider

1. Implement `IChecklistProvider` (`Title`, `GetItems()`)
2. Register in `ChecklistRegistry._sdkChecklists` (by define) or `_storeChecklists`

## Editor Testing

`DebugAdSource` simulates ads in the Editor. Debug canvas (`Resources/DebugAdCanvas.prefab`) shows mock UI panels. Tap 6 times rapidly to toggle debug ads.

`DebugLeaderboardSource` — in-memory mock (`IsAuthenticated = true`, `LocalPlayerName = "[DEBUG] You"`). `SimulatedLeaderboardSource` — runtime fallback with PlayerPrefs-persisted deterministic bots.

`DebugRateSource` — logs + immediately calls `onComplete(true)`.

`DebugPurchaseSource` — UI Toolkit overlay dialog. "Buy" → `Success`; "Cancel" → `Cancelled`. `AlreadyOwned` for non-consumables in cache returns immediately. Implemented via `DebugPurchasePanel` MonoBehaviour (built in pure C#, no UXML/USS). `EnsureUI()` lazy-called on first `Show()`. Looks for existing `UIDocument` to reuse its panel; else creates fallback `UIDocument`+`PanelSettings`. Labels/buttons get explicit font via `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`. **Pattern:** `OnConfirm`/`OnCancel` save `_callback` to local before calling `Hide()` (which nulls `_callback`), then invoke local.

`DebugShareSource`, `DebugAnalyticsSource`, `DebugAuthSource` — log + simulate success in Editor.

## Async API (optional)

Namespace: `UniBridge.Async`. Lives in `UniBridge.Async.Runtime` asmdef, gated by `UNIBRIDGE_UNITASK` (set automatically when `com.cysharp.unitask` is in `Packages/manifest.json`). If UniTask is not installed, the asmdef does not compile and nothing is exposed — callback-based APIs remain unchanged.

Static wrappers parallel each facade:

| Wrapper class | Methods |
|---|---|
| `UniBridgeSavesAsync` | `SaveAsync<T>`, `LoadAsync<T>`, `LoadRawAsync`, `DeleteAsync`, `HasKeyAsync` |
| `UniBridgeAdsAsync` | `ShowInterstitialAsync`, `ShowRewardAsync` |
| `UniBridgeLeaderboardsAsync` | `SubmitScoreAsync`, `GetEntriesAsync`, `GetPlayerEntryAsync` |
| `UniBridgePurchasesAsync` | `BuyAsync`, `FetchProductsAsync`, `GetProductAsync`, `RefreshPurchasesAsync`, `RestorePurchasesAsync` |
| `UniBridgeRateAsync` | `RequestReviewAsync` |
| `UniBridgeShareAsync` | `ShowShareSheetAsync(ct, params ShareItem[])` |
| `UniBridgeAuthAsync` | `AuthorizeAsync` |

Every method accepts an optional `CancellationToken`. Semantics:
- Pre-dispatch cancel → `UniTask.FromCanceled<T>(ct)`.
- Post-dispatch cancel → best-effort: completion source flips to canceled when the callback fires.
- No cancel → regular result propagation.

All wrappers route through `AsyncHelpers.Await<T>` / `Await<T1,T2>` (internal), which handles the `UniTaskCompletionSource` + `CancellationTokenRegistration` dance and disposes the registration on completion.

Usage example:
```csharp
using UniBridge.Async;

var (ok, data) = await UniBridgeSavesAsync.LoadAsync<SaveData>("LocalData", ct);
var status     = await UniBridgeAdsAsync.ShowInterstitialAsync("level_end", ct);
var result     = await UniBridgePurchasesAsync.BuyAsync("gems_100", ct);
```

Callback APIs remain the canonical surface — async wrappers are opt-in sugar. Don't add business logic to them; if a feature needs a new async-only behavior, it belongs in the underlying facade first.

## Logging Convention

Use `UnityEngine.Debug.Log($"[{nameof(CurrentClass)}] ...")` in all Unity C# code. Prefix is always the emitting class name via `nameof` (never a hardcoded string). Applies to facades and adapters.
