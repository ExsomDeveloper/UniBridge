# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**RatAds** is a Unity advertisement + purchase + leaderboard + rating integration package providing unified APIs across multiple platforms:
- **Ads**: LevelPlay (Android/iOS), Yandex Mobile Ads (Android/iOS), Playgama (WebGL), Debug mock (Editor)
- **Purchases**: Unity IAP (Google Play / App Store), RuStore Pay (Android), Playgama (WebGL), Debug mock (Editor)
- **Leaderboards**: Google Play Games Services (Android), Game Center (iOS), Playgama (WebGL), Simulated fallback (all platforms), Debug mock (Editor)
- **Saves**: Google Play Saved Games (Android), iCloud KV Store (iOS), Playgama (WebGL), Simulated (PlayerPrefs, all platforms), LocalSaveSource (file system, fallback)
- **Reviews/Ratings**: Google Play Review (Android), RuStore Review (Android), App Store Review (iOS built-in), Mock (WebGL/unsupported), Debug mock (Editor)
- **Share**: Android native (Google Play / RuStore), iOS native (App Store), Playgama Bridge (WebGL), Debug mock (Editor)
- **Analytics**: AppMetrica (Android/iOS), Debug mock (Editor)
- **SDKs**: LevelPlay `9.3.0`, Playgama `1.27.0`, Yandex `7.18.1`, EDM4U `1.2.185`, Unity IAP `4.12.2`, RuStore Pay `10.1.1` (ru.rustore.core `10.1.0` + ru.rustore.pay `10.1.1`), GPGS `1.0.0`, Google Play Review `1.2.4` (`com.google.play.review`), RuStore Review `10.1.0` (`ru.rustore.review`)

## Unity Editor Workflows

No CLI build commands — all operations inside the Unity Editor:

- **SDK Installer**: `RatAds > SDK Installer` — install/update/remove any SDK
- **Build Manager**: `RatAds > Build Manager` — select store target before building (required)
- **All Configs**: `RatAds > Settings` — tabbed window with RatAds / RatPurchases / RatLeaderboards / RatRate / General tabs
- **Create Configurations**: `RatAds > Create Configuration` — creates `RatAdsConfig`; use Assets > Create > RatAds > * for other configs
- **Before building**: exactly one `RATADS_STORE_*` define must be set via Build Manager

## Architecture

### Static Facade Pattern

Top-level facades, all follow the same pattern:
- `RatAds` (`Runtime/RatAds.cs`) — ad operations; MonoBehaviour creating a `DontDestroyOnLoad` root
- `RatPurchases` (`Runtime/Purchases/RatPurchases.cs`) — purchase operations
- `RatLeaderboards` (`Runtime/Leaderboards/RatLeaderboards.cs`) — leaderboard operations
- `RatSaves` (`Runtime/RatSaves.cs`) — save/load operations
- `RatRate` (`Runtime/Rate/RatRate.cs`) — app review/rating requests
- `RatShare` (`Runtime/Share/RatShare.cs`) — share text/image to social networks
- `RatAnalytics` (`Runtime/Analytics/RatAnalytics.cs`) — event analytics; silently disabled (no-op) if no adapter is configured
- `RatEnvironment` (`Runtime/RatEnvironment.cs`) — platform environment: language, audio/pause/visibility state, platform ID, platform messaging

### RatEnvironment

Static utility (not a MonoBehaviour). Provides platform-agnostic access to environment parameters via `IPlatformParamsProvider` (`Runtime/IPlatformParamsProvider.cs`). On Playgama WebGL, `PlaygamaPlatformProvider` (`Runtime/Adapters/Playgama/PlaygamaPlatformProvider.cs`) registers at `SubsystemRegistration` and bridges all calls to `Bridge.platform.*`. On other platforms, defaults are used (no provider needed).

**Public API:**
- `GetPlatformId()` — platform identifier string (e.g. `"yandex"`, `"vk"`, `"crazy_games"`); returns `""` on non-Playgama platforms
- `GetLanguage()` — ISO 639-1 language code; RuStore hardcoded to `"ru"`, others use `Bridge.platform.language` or `Application.systemLanguage`
- `IsAudioEnabled` — platform audio state (default `true`)
- `IsPaused` — platform pause state (default `false`)
- `IsVisible` — tab/window visibility (default `true`)
- `SendMessage(PlatformMessage)` — sends lifecycle messages to the platform (Playgama); no-op elsewhere
- Events: `AudioStateChanged`, `PauseStateChanged`, `VisibilityStateChanged`

### Adapter & Registry Pattern

Each subsystem uses the same structure:
1. **Interface** (`IAdSource`, `IPurchaseSource`, `ILeaderboardSource`, `ISaveSource`, `IRateSource`, `IAnalyticsSource`) — unified API contract
2. **Registry** (`AdSourceRegistry`, `PurchaseSourceRegistry`, `LeaderboardSourceRegistry`, `SaveSourceRegistry`, `RateSourceRegistry`, `AnalyticsSourceRegistry`) — adapters self-register via `[RuntimeInitializeOnLoadMethod]` with a priority
3. **Builder** (`AdSourceBuilder`, `PurchaseSourceBuilder`, `LeaderboardSourceBuilder`, `SaveSourceBuilder`, `RateSourceBuilder`, `AnalyticsSourceBuilder`) — selects adapter at runtime; always returns Debug adapter in the Editor

**Ad adapter registration timing:** Ad adapters use `SubsystemRegistration` (earliest Unity init level) so they are always registered before `RatAds.AutoInitialize` runs at `BeforeSceneLoad`. Purchase/Save/Leaderboard/Rate/Analytics adapters use `BeforeSceneLoad`.

**Ad adapter selection:** `AdSourceRegistry` is Dictionary-based, keyed by SDK define string (e.g. `"RATADS_YANDEX"`). `AdSourceRegistry.Create(config)` checks `config.PreferredAdsAdapter` — if that key is registered, uses it; if not registered (SDK not installed) returns `null` → `AdSourceBuilder` falls back to `DebugAdSource`. If `PreferredAdsAdapter` is empty, falls back to highest-priority registered adapter. `PreferredAdsAdapter` is written to `RatAdsConfig` by Build Manager when "Выбрать" is clicked.

**Leaderboard adapter selection:** Same pattern as ads. `LeaderboardSourceRegistry.Create(config)` respects `config.PreferredLeaderboardAdapter`. Two virtual adapter keys exist that are not real scripting defines:
- `"RATLEADERBOARDS_SIMULATED"` — always available on all platforms; `LeaderboardSourceBuilder` handles it explicitly before consulting the registry, returning `SimulatedLeaderboardSource` directly
- `"UNITY_IOS_GAMECENTER"` — built-in iOS, treated as always installed in Build Manager UI

**Rate adapter selection:** `RateSourceRegistry.Create(config)` respects `config.PreferredRateAdapter`. Two virtual keys are handled directly in `RateSourceBuilder` before consulting the registry:
- `"RATRATE_MOCK"` — always available; `RateSourceBuilder` returns `MockRateSource` directly (IsSupported=false, no-op)
- `"UNITY_IOS_STOREREVIEW"` — built-in iOS; `RateSourceBuilder` returns `AppStoreReviewSource` directly when on iOS

**Save adapter selection:** `SaveSourceBuilder.Build(config)` respects `config.PreferredSavesAdapter`. Virtual keys are handled directly before consulting the registry:
- `"RATSAVES_SIMULATED"` — always available; returns `SimulatedSaveSource` (PlayerPrefs backend, prefix `ratsaves_sim_`)
- `"UNITY_IOS_ICLOUD"` — built-in iOS; returns `iCloudSaveSource` when on iOS+AppStore target (not registered in the registry)
- `"RATADS_NONE"` / `""` → `LocalSaveSource` (file system)
- Otherwise: consults registry → highest-priority registered → `LocalSaveSource` fallback

**Share adapter selection:** `ShareSourceBuilder.Build(config)` handles platform adapters explicitly (no registry needed — all are virtual keys or built-in): `"RATADS_NONE"`→`null`; Editor→`DebugShareSource`; `"RATSHARE_MOCK"`→`MockShareSource`; `UNITY_ANDROID`→`AndroidShareSource`; `UNITY_IOS`→`iOSShareSource`; falls back to registry (for Playgama); final fallback→`MockShareSource`.

**Analytics adapter selection:** `AnalyticsSourceBuilder.Build(config)` returns `null` if `PreferredAnalyticsAdapter == "RATADS_NONE"`. In Editor returns `DebugAnalyticsSource`. Otherwise consults `AnalyticsSourceRegistry` → highest-priority registered. If no adapter found at runtime, returns `null` → `RatAnalytics` facade silently no-ops (no Debug fallback in release builds).

**Share public API:** `SharingServices.ShowShareSheet(callback, params ShareItem[])` is the entry point. `ShareItem` factory methods: `Text(string)`, `Image(Texture2D)`, `ImageUrl(string)`, `Screenshot()` (captures at end of frame). Callback receives `(ShareSheetResult, ShareError)` — `error` is non-null only on init/data error; `result.Code` is `ShareResultCode.Unknown` on Android (`startActivity` is fire-and-forget — no completion callback available). Internally `SharingServices` converts items to `ShareData` and calls `RatShare.Share()` (internal).

**Share data model:** `ShareData` has three properties: `Text` (string), `Image` (Texture2D — for Android/iOS), `ImageUrl` (string — for Playgama/web). Factory methods: `WithText`, `WithImage`, `WithImageUrl`, `WithTextAndImage`, `WithTextAndImageUrl`. Used internally; prefer `SharingServices`/`ShareItem` from game code.

Adapter registration is gated by `#if` defines, so only the correct adapter for the current platform compiles and registers.

### Scripting Defines

| Define | Meaning | How set |
|--------|---------|---------|
| `RATADS_LEVELPLAY` | LevelPlay SDK installed | Auto via `versionDefines` in asmdef |
| `RATADS_PLAYGAMA` | Playgama Bridge installed | Auto via `versionDefines` in asmdef |
| `RATADS_YANDEX` | Yandex Mobile Ads installed | Manually by SDK Installer |
| `RATPURCHASES_IAP` | Unity Purchasing installed | Auto via `versionDefines` in asmdef |
| `RATPURCHASES_RUSTORE` | RuStore Billing tgz installed | Manually by SDK Installer |
| `RATLEADERBOARDS_GPGS` | Google Play Games Services installed | Auto by `SDKVersionChecker` |
| `RATRATE_GOOGLEPLAY` | Google Play Review SDK installed | Manually by SDK Installer |
| `RATRATE_RUSTORE` | RuStore Review tgz installed | Manually by SDK Installer |
| `RATANALYTICS_APPMETRICA` | AppMetrica SDK installed | Manually by SDK Installer |
| `RATADS_STORE_GOOGLEPLAY` | Build target: Google Play | Build Manager |
| `RATADS_STORE_RUSTORE` | Build target: RuStore | Build Manager |
| `RATADS_STORE_APPSTORE` | Build target: App Store | Build Manager |
| `RATADS_STORE_PLAYGAMA` | Build target: Playgama | Build Manager |
| `RATADS_STORE_EDITOR` | Editor-only mode (no real platform, all Debug adapters) | Build Manager / auto on first install |

`ScriptingDefinesManager` batches define changes via `EditorApplication.delayCall` to avoid repeated recompiles. `SDKVersionChecker` auto-detects installed packages on editor load and syncs defines for LevelPlay/Playgama/Unity IAP/GPGS. After syncing, if no `RATADS_STORE_*` is active (first install), it auto-selects `RATADS_STORE_EDITOR`.

### Assembly Definitions

| asmdef | Define Constraint | Key References |
|--------|------------------|----------------|
| `RatAds.Runtime` | — | Core runtime, no SDK deps |
| `RatAds.Editor` | — | RatAds.Runtime, RatPurchases.Runtime, RatLeaderboards.Runtime, RatAnalytics.Runtime |
| `RatAds.LevelPlay` | `RATADS_LEVELPLAY` | RatAds.Runtime, Unity.LevelPlay |
| `RatAds.Playgama` | `RATADS_PLAYGAMA` | RatAds.Runtime |
| `RatAds.Yandex` | `RATADS_YANDEX` | RatAds.Runtime, YandexMobileAds |
| `RatPurchases.Runtime` | — | Core purchases, no SDK deps |
| `RatPurchases.Editor` | — | RatPurchases.Runtime, RatAds.Editor |
| `RatPurchases.UnityIAP` | `RATPURCHASES_IAP` | RatPurchases.Runtime, UnityEngine.Purchasing |
| `RatPurchases.RuStore` | `RATPURCHASES_RUSTORE` | RatPurchases.Runtime |
| `RatLeaderboards.Runtime` | — | Core leaderboards, no SDK deps; autoReferenced |
| `RatLeaderboards.Editor` | Editor only | RatLeaderboards.Runtime, RatAds.Editor |
| `RatLeaderboards.GPGS` | `RATLEADERBOARDS_GPGS` | RatLeaderboards.Runtime |
| `RatSaves.GPGS` | `RATLEADERBOARDS_GPGS` | RatAds.Runtime, GooglePlayGames; `GPGSSaveSource` |
| `RatRate.Runtime` | — | Core rate/review, includes AppStore + Mock adapters |
| `RatRate.Editor` | Editor only | RatRate.Runtime, RatAds.Editor |
| `RatRate.GooglePlay` | `RATRATE_GOOGLEPLAY` | RatRate.Runtime, Google.Play.Review |
| `RatRate.RuStore` | `RATRATE_RUSTORE` | RatRate.Runtime; precompiledReferences: RuStoreReview |
| `RatRate.Playgama` | `RATADS_PLAYGAMA` | RatRate.Runtime, Playgama.Bridge; autoReferenced=false; PlaygamaRateSource |
| `RatShare.Runtime` | — | Core share, autoReferenced; contains all adapters except Playgama |
| `RatShare.Playgama` | `RATADS_PLAYGAMA` | RatShare.Runtime, Playgama.Bridge; autoReferenced=false; PlaygamaShareSource |
| `RatShare.Editor` | Editor only | RatShare.Runtime, RatAds.Editor |
| `RatAnalytics.Runtime` | — | Core analytics, autoReferenced; includes Debug + AppMetrica adapters behind `#if` |
| `RatAnalytics.AppMetrica` | `RATANALYTICS_APPMETRICA` | RatAnalytics.Runtime; autoReferenced=false |
| `RatAnalytics.Editor` | Editor only | RatAnalytics.Runtime, RatAds.Editor |

Note: `RatLeaderboards.Runtime` is listed explicitly in `RatAds.Editor.asmdef` references — `autoReferenced` alone is insufficient for editor assemblies to see it.

### Ad Adapters (Priority → Activation Condition)

| Adapter | Priority | `#if` Condition |
|---------|----------|-----------------|
| `LevelPlayAdapter` | 100 | `RATADS_LEVELPLAY && (UNITY_ANDROID \|\| UNITY_IOS) && !UNITY_EDITOR` |
| `YandexAdapter` | 90 | `RATADS_YANDEX && (UNITY_ANDROID \|\| UNITY_IOS) && !UNITY_EDITOR` |
| `PlaygamaAdapter` | 100 | `RATADS_PLAYGAMA && UNITY_WEBGL` |
| `DebugAdSource` | lowest | always (Editor) |

### Purchase Adapters (Priority → Activation Condition)

| Adapter | Priority | `#if` Condition |
|---------|----------|-----------------|
| `UnityIAPPurchaseSource` | 100 | `(UNITY_ANDROID && RATADS_STORE_GOOGLEPLAY) \|\| (UNITY_IOS && RATADS_STORE_APPSTORE)` |
| `RuStorePurchaseSource` | 100 | `UNITY_ANDROID && RATADS_STORE_RUSTORE && !UNITY_EDITOR` |
| `PlaygamaPurchaseSource` | 50 | `RATADS_PLAYGAMA && UNITY_WEBGL` |
| `DebugPurchaseSource` | — | always (Editor) |

**`ProductDefinition` fields:**
- `ProjectId` — internal game-facing ID used in all game code calls (`IsPurchased`, `Purchase`, events, `ProductData.ProductId`). Set in Inspector.
- `ProductId` — store-facing SDK ID sent to Unity IAP / RuStore. **Not used in game code** — only inside adapters.
- `PlaygamaProductId` — optional Playgama override; falls back to `ProductId` if empty.

All adapter `_ownershipCache` dictionaries and `PurchaseResult.ProductId` use `ProjectId` as key/value. Adapters map `projectId → storeId` on Purchase calls and `storeId → projectId` in SDK callbacks. `ProductData.ProductId` is populated with `projectId` so existing game code that reads `product.ProductId` continues to work.

`IsPurchased()` is synchronous — each adapter maintains an `_ownershipCache` populated during `Initialize` → `RefreshPurchases`. RuStore: purchases in `"PAID"` state are auto-confirmed on refresh (crash recovery); only `"CONFIRMED"` state populates the cache.

`PurchaseStatus` values: `None`, `Success`, `Failed`, `Cancelled`, `AlreadyOwned`, `NotInitialized`, `NotSupported`, `PendingConfirmation`, `Restored`. `PurchaseResult` helpers: `IsSuccess` (`Status == Success`), `IsRestore` (`Status == Restored`), `FromRestored(productId, transactionId)`.

**`AutoInitialize` guard:** `RatPurchases.AutoInitialize` (`BeforeSceneLoad`) starts with `if (IsInitialized) return;` — initialization runs once per app lifetime regardless of scene loads. Subsequent scene loads are no-ops.

**Initialization flow:** `RatPurchases` automatically calls `FetchProducts` after the adapter's `Initialize` succeeds. `OnInitSuccess` fires only after this auto-fetch completes (success or fail — graceful degradation). This guarantees that `Products` is populated and `GetLocalizedPrice()` works synchronously by the time `OnInitSuccess` fires. `OnProductsFetched` fires only when the auto-fetch succeeds.

**`RestorePurchases` — events and restored product list:** `IPurchaseSource.RestorePurchases` takes `Action<bool, IReadOnlyList<string>>` — the second parameter is the list of product IDs newly added to the ownership cache (snapshot/diff). The `RatPurchases` facade fires `OnRestoreSuccess(productId)` per newly-added ID and keeps the public `RestorePurchases(Action<bool>)` signature backwards-compatible.

In addition, each adapter fires `OnPurchaseSuccess` with `PurchaseStatus.Restored` for owned non-consumables after restore. `PurchaseResult.IsRestore` (`Status == Restored`) distinguishes a restore from a new purchase (`IsSuccess`). `PurchaseResult.FromRestored(productId, transactionId)` is the factory. Per-adapter nuance:
- **UnityIAP iOS**: `apple.RestoreTransactions` internally calls `ProcessPurchase` for each restored transaction (fires `Success`). To avoid double-activation, `Restored` fires only for the diff — products not in the cache before `RestoreTransactions` was called.
- **UnityIAP Android**: Google Play restores ownership on `Initialize`; `RestorePurchases` just rebuilds the cache from local receipts and fires `Restored` for all owned products. The diff list is always empty so `OnRestoreSuccess` never fires on Android.
- **RuStore / Debug**: `Restored` fires for all owned products in `_ownershipCache` after `RefreshPurchases`. RuStore crash-recovery (PAID→CONFIRMED confirmation) also populates the cache before events fire.
- **Playgama**: `Restored` fires for all non-consumable entries in `_ownershipCache`. Consumables are filtered via the existing `IsConsumable()` guard to prevent accidental re-grant from products that failed the consume call.

**`GetLocalizedPrice(productId)`** — synchronous helper on `RatPurchases` facade; reads `LocalizedPriceString` from the cached `Products` list. Returns `string.Empty` if `Products` is null or the product isn't found.

### Leaderboard Adapters (Priority → Activation Condition)

| Adapter | Priority | `#if` Condition |
|---------|----------|-----------------|
| `GPGSLeaderboardSource` | 100 | `RATLEADERBOARDS_GPGS && UNITY_ANDROID && RATADS_STORE_GOOGLEPLAY && !UNITY_EDITOR` |
| `GameCenterLeaderboardSource` | 90 | `UNITY_IOS && RATADS_STORE_APPSTORE && !UNITY_EDITOR` |
| `PlaygamaLeaderboardSource` | 100 | `RATADS_PLAYGAMA && UNITY_WEBGL` |
| `SimulatedLeaderboardSource` | — | runtime fallback (no native adapter, or `PreferredLeaderboardAdapter == "RATLEADERBOARDS_SIMULATED"`) |
| `DebugLeaderboardSource` | — | always (Editor) |

`SimulatedLeaderboardSource` stores player scores in PlayerPrefs (`ratleaderboards_score_{id}`), score submission timestamp (`ratleaderboards_date_{id}` as `DateTime.ToBinary()` string), and generates persistent deterministic bot lists (`ratleaderboards_bots_{id}` as JSON) from the leaderboard ID seed. `timeScope` параметр игнорируется — симуляция показывает единый рейтинг без временных диапазонов. Имена ботов берутся из `SimulationSettings.BotNames` (настраивается в Inspector); при генерации пул перемешивается детерминированно и имена раздаются без повторений — если ботов больше чем имён в пуле, overflow-боты получают суффикс `_2`, `_3` и т.д. При появлении нового бота (`SpawnNewBot`) выбирается первое незанятое имя из пула.

### Rate Adapters (Priority → Activation Condition)

| Adapter | Priority | `#if` Condition |
|---------|----------|-----------------|
| `GooglePlayReviewSource` | 100 | `RATRATE_GOOGLEPLAY && UNITY_ANDROID && RATADS_STORE_GOOGLEPLAY && !UNITY_EDITOR` |
| `RuStoreReviewSource` | 100 | `RATRATE_RUSTORE && UNITY_ANDROID && RATADS_STORE_RUSTORE && !UNITY_EDITOR` |
| `AppStoreReviewSource` | 90 | `UNITY_IOS && RATADS_STORE_APPSTORE && !UNITY_EDITOR` |
| `MockRateSource` | — | runtime fallback (no adapter registered, or `PreferredRateAdapter == "RATRATE_MOCK"`); `IsSupported = false` |
| `DebugRateSource` | — | always (Editor) |

`GooglePlayReviewSource` uses `ReviewManager` from `Google.Play.Review` — flow is async: `RequestReviewFlow` → `LaunchReviewFlow` via a temporary `CoroutineRunner` MonoBehaviour. `RuStoreReviewSource` uses `RuStoreReviewManager.Instance` (namespace `RuStore.Review`): `Init()` on Initialize, then `RequestReviewFlow` → `LaunchReviewFlow`.

### Save Adapters (Priority → Activation Condition)

| Adapter | Priority | `#if` Condition |
|---------|----------|-----------------|
| `GPGSSaveSource` | 100 | `RATLEADERBOARDS_GPGS && UNITY_ANDROID && RATADS_STORE_GOOGLEPLAY && !UNITY_EDITOR` |
| `PlaygamaSaveSource` | 100 | `RATADS_PLAYGAMA && UNITY_WEBGL` |
| `iCloudSaveSource` | — | `UNITY_IOS && RATADS_STORE_APPSTORE && !UNITY_EDITOR` (virtual key, handled in Builder) |
| `SimulatedSaveSource` | — | always (virtual key, handled in Builder; PlayerPrefs backend) |
| `LocalSaveSource` | — | always (file system fallback) |

`GPGSSaveSource` stores all keys in a single GPGS snapshot named `"ratsaves_data"` as a JSON dictionary `{entries:[{k,v}]}`. Snapshot conflict resolution uses `UseLongestPlaytime` — keeps the version with longer `TotalTimePlayed`. Requires authentication (`Social.localUser.authenticated`). Reuses GPGS SDK already installed for leaderboards.

`iCloudSaveSource` uses `NSUbiquitousKeyValueStore` via native Obj-C plugin (`Assets/RatAds/Plugins/iOS/RatSavesiOS.mm`). If iCloud is unavailable (entitlement missing or user not signed in — detected by `synchronize()` returning `false`), automatically falls back to `LocalSaveSource` with a LogWarning. Requires iCloud Key-Value Storage capability in Xcode.

`SimulatedSaveSource` uses PlayerPrefs with `ratsaves_sim_` prefix — provides cloud-like callback behavior for Editor testing without any SDK dependency.

### Share Adapters (Activation Condition)

| Adapter | `#if` Condition |
|---------|-----------------|
| `AndroidShareSource` | `UNITY_ANDROID && !UNITY_EDITOR` |
| `iOSShareSource` | `UNITY_IOS && !UNITY_EDITOR` |
| `PlaygamaShareSource` | `RATADS_PLAYGAMA && UNITY_WEBGL` |
| `MockShareSource` | runtime fallback (or `PreferredShareAdapter == "RATSHARE_MOCK"`); `IsSupported = false` |
| `DebugShareSource` | always (Editor) |

`AndroidShareSource` shares text via `Intent.ACTION_SEND` + `text/plain`. Images are written to `Application.temporaryCachePath` and passed to `RatSharePlugin.java` (`Assets/RatAds/Plugins/Android/RatSharePlugin.java`) via JNI. The plugin handles image URI creation in an API-aware manner: API 29+ uses `MediaStore` (no FileProvider needed), API 24–28 uses `FileProvider` (authority: `{Application.identifier}.ratshare.provider`), API 21–23 uses `Uri.fromFile()`. Uses `startActivity` (fire-and-forget) — callback fires immediately with `ShareResultCode.Unknown` since Android provides no completion signal for `ACTION_SEND` intents. The `<provider>` declaration is injected by `RatShareAndroidManifestInjector` (`IPostGenerateGradleAndroidProject`) directly into the generated `unityLibrary/src/main/AndroidManifest.xml` during Gradle project generation — this bypasses Unity's unreliable manifest merging. The `ratshare_file_paths.xml` resource is also written directly to `unityLibrary/src/main/res/xml/` by the same injector. The `RatShareAndroid.androidlib/` directory provides the resource for Unity 6 compatibility (Unity 6 no longer supports plain `Assets/Plugins/Android/res/`). Files are created when "Выбрать" is clicked with `RATSHARE_ANDROID` adapter on an Android store.

`iOSShareSource` calls a native Obj-C plugin (`Assets/RatAds/Plugins/iOS/RatShareiOS.mm`) via `DllImport`. Plugin presents `UIActivityViewController` with iPad popover support. Images are saved to `temporaryCachePath` and passed as a file path.

`PlaygamaShareSource` registers in `ShareSourceRegistry` at `BeforeSceneLoad` with key `"RATADS_PLAYGAMA"`, priority 100. Uses `Bridge.social.Share(options, callback)` where options may include `text` and/or `imageUrl`.

### Analytics Adapters (Activation Condition)

| Adapter | `#if` Condition |
|---------|-----------------|
| `AppMetricaAnalyticsSource` | `RATANALYTICS_APPMETRICA && (UNITY_ANDROID \|\| UNITY_IOS) && !UNITY_EDITOR` |
| `DebugAnalyticsSource` | always (Editor) |

`RatAnalytics.LogEvent(string eventName, Dictionary<string, object> parameters)` is the public API. If no adapter is configured at runtime, the facade silently no-ops (unlike other subsystems there is no Mock/Debug fallback in release builds — analytics failure is non-critical). `RatAnalyticsConfig` is git-ignored (each project sets its own API keys) — create it via `Assets > Create > RatAds > Analytics Configuration`.

### Configuration ScriptableObjects

All configs live in `Assets/RatAds/Resources/` and are loaded via `Resources.Load`.

**`RatAdsConfig`** — `AutoInitialize`, `MaxChildrenAge`/`DefaultUserAge` (COPPA), `AdsDisabledKey` (PlayerPrefs), `PreferredAdsAdapter` (SDK define string — written by Build Manager), plus `LevelPlaySettings`, `PlaygamaSettings`, `YandexSettings`.

**`RatPurchasesConfig`** — `AutoInitialize`, `Products` (`List<ProductDefinition>`), plus `UnityIAPSettings`, `RuStoreSettings` (ConsoleApplicationId, DeeplinkPrefix). Lookup helpers: `FindByProjectId(projectId)` and `FindByStoreProductId(storeId)` — used by adapters for bidirectional mapping.

**`RatLeaderboardsConfig`** — `AutoInitialize`, `PreferredLeaderboardAdapter` (SDK define string — written by Build Manager), `Leaderboards` (`List<LeaderboardDefinition>` with `Id`, `DisplayName`, `GpgsId`, `GameCenterId`), `SimulationSettings` (BotCount, MinScore, MaxScore, PlayerName, BotNames, DailyGrowthMin, DailyGrowthMax, LeaderboardSize).

**`RatRateConfig`** — `AutoInitialize`, `PreferredRateAdapter` (SDK define string — written by Build Manager).

**`RatSavesConfig`** — `AutoInitialize`, `PreferredSavesAdapter` (SDK define string or virtual key — written by Build Manager when "Выбрать" is clicked). Falls back to `RatAdsConfig.AutoInitializeSaves` for backward compatibility if the asset doesn't exist.

**`RatShareConfig`** — `AutoInitialize`, `PreferredShareAdapter` (SDK define string — written by Build Manager when "Выбрать" is clicked).

**`RatAnalyticsConfig`** — `AutoInitialize`, `PreferredAnalyticsAdapter`, `AppMetrica.ApiKey`. Git-ignored — not committed to version control.

### Timer System

`AdTimer` runs coroutines on the `RatAds` root MonoBehaviour, auto-pauses on `OnApplicationPause`. `InterstitialTimerHandler` is the default `ITimerHandler`; reward ads always pause the timer (resets on success, resumes on failure/cancel).

### Android Auto-Configuration

`StoreAndroidConfigurator.OnStoreChanged(previousDefine, newDefine, ratShareEnabled)` is the single dispatch point for all Android-specific file generation — called from Build Manager's `OnSelectClicked()`.

**`RuStoreAndroidConfigurator`** auto-generates `Assets/Plugins/Android/RatMobileKit/RuStoreUnityPay/AndroidManifest.xml` (console app ID + deeplink activity) when RuStore store is selected. Marker: `<!-- RATADS_RUSTORE_GENERATED -->`. Also writes `RuStoreSDKSettings.androidlib/build.gradle` at build time via `EnsureAndroidLibBuildGradle()` (SDK-managed directory, stays at root of `Assets/Plugins/Android/`).

**`RatShareAndroidConfigurator`** auto-generates `Assets/Plugins/Android/RatMobileKit/RatShareAndroid.androidlib/` — Gradle subproject containing `res/xml/ratshare_file_paths.xml` + minimal `AndroidManifest.xml` + `build.gradle` + `project.properties`. Used for the XML resource (Unity 6 does not support plain `Assets/Plugins/Android/res/`). Marker: `// RATADS_RATSHARE_GENERATED` (in build.gradle).

RatAds-owned Android files: `Assets/RatAds/Plugins/Android/RatSharePlugin.java` (hand-written, part of the package). Auto-generated files live under `Assets/Plugins/Android/RatMobileKit/`: `RatShareAndroid.androidlib/` (git-ignored), `RuStoreUnityPay/` (committed). `RuStoreSDKSettings.androidlib/` is the only RatAds-touched file outside `RatMobileKit/` — its directory is created by the RuStore SDK tgz installation.

**`RatShareAndroidManifestInjector`** (`IPostGenerateGradleAndroidProject`, callbackOrder=100) — injects the `<provider>` declaration directly into the generated `unityLibrary/src/main/AndroidManifest.xml` and copies `ratshare_file_paths.xml` into `unityLibrary/src/main/res/xml/`. This is the most reliable approach: it runs after Unity generates the complete Gradle project (before Gradle builds it), bypasses Unity's manifest merging entirely, and is the same technique used by Yandex and RuStore SDKs. Runs at Gradle project generation time regardless of Share adapter — skips if adapter is `RATADS_NONE` or `RATSHARE_MOCK` via runtime check inside `OnPostGenerateGradleAndroidProject`.

**Why NOT `Assets/Plugins/Android/AndroidManifest.xml`**: That approach conflicts with Yandex SDK's `AndroidManifestPostprocessor` (which looks for `UnityPlayerActivity` in the generated manifest and crashes with NullReferenceException if not found). It also conflicts with `RuStoreAndroidConfigurator` which treats that path as a legacy location.

Generated directories are excluded from version control (see `.gitignore`).

### Checklist Architecture (Build Manager & SDK Installer)

`IChecklistProvider` interface (`Title`, `GetItems() → ChecklistItem[]`). `ChecklistRegistry` maps defines to provider instances:
- `_storeChecklists`: `STORE_GOOGLEPLAY` → `RatShareChecklist`, `STORE_RUSTORE` → `RatShareChecklist`
- `_sdkChecklists`: `"RATPURCHASES_RUSTORE"` → `RuStoreBillingChecklist`, `"RATADS_PLAYGAMA"` → `PlaygamaChecklist`

`ChecklistUIHelper.BuildGroup()` / `BuildItem()` renders shared UI in both windows. Build Manager composes: store checklist + SDK checklists for each installed SDK of the selected store. SDK Installer shows the SDK's checklist in the detail panel.

### Runtime Utilities

**`DisableOnStore`** (`Runtime/DisableOnStore.cs`) — MonoBehaviour that disables its `GameObject` in `Awake()` if the current build's store matches the selected mask. Add to any GameObject and select stores in the `Disable On Stores` field (multi-select checkboxes). Uses compile-time `#if RATADS_STORE_*` checks — no runtime overhead.

**`StoreTargetMask`** (`Runtime/StoreTargetMask.cs`) — `[Flags]` enum used by `DisableOnStore`: `None`, `GooglePlay`, `RuStore`, `AppStore`, `Playgama`, `Editor`.

**`DisableIfNotSupported`** (`Runtime/DisableIfNotSupported.cs`) — MonoBehaviour that disables its `GameObject` at runtime if the selected RatAds system reports `IsSupported = false`. Selectable via `RatSystemCheck` enum (`Share`, `Leaderboards`, `Purchases`, `Rate`). In `Awake()`: if the system is already initialized, applies visibility immediately; otherwise subscribes to `OnInitSuccess`/`OnInitFailed` and applies on init completion. Handles graceful degradation for Playgama adapters where support varies by web store.

## Key Files

| File | Role |
|------|------|
| `Assets/RatAds/Runtime/RatAds.cs` | Main ads API — all ad operations |
| `Assets/RatAds/Runtime/Purchases/RatPurchases.cs` | Main purchases API |
| `Assets/RatAds/Runtime/Purchases/ProductDefinition.cs` | Product config: `ProjectId` (game-facing), `ProductId` (store SDK), `PlaygamaProductId` (Playgama override) |
| `Assets/RatAds/Runtime/Purchases/Adapters/Debug/DebugPurchasePanel.cs` | MonoBehaviour — UI Toolkit purchase dialog for Editor testing (pure C#, no UXML/USS) |
| `Assets/RatAds/Runtime/Leaderboards/RatLeaderboards.cs` | Main leaderboards API |
| `Assets/RatAds/Runtime/RatSaves.cs` | Save/load API; `AdapterName` property |
| `Assets/RatAds/Runtime/RatSavesConfig.cs` | Saves config ScriptableObject |
| `Assets/RatAds/Runtime/ISaveSource.cs` | Save adapter interface |
| `Assets/RatAds/Runtime/SaveSourceRegistry.cs` | Dictionary-based save adapter registry, keyed by define string |
| `Assets/RatAds/Runtime/SaveSourceBuilder.cs` | Selects save adapter; handles virtual keys (`RATSAVES_SIMULATED`, `UNITY_IOS_ICLOUD`) |
| `Assets/RatAds/Runtime/Adapters/Simulated/SimulatedSaveSource.cs` | PlayerPrefs-backed save adapter for Editor/simulation |
| `Assets/RatAds/Runtime/Adapters/GPGS/GPGSSaveSource.cs` | Google Play Saved Games adapter (one snapshot, JSON dict) |
| `Assets/RatAds/Runtime/Adapters/iOS/iCloudSaveSource.cs` | iCloud Key-Value Store adapter; fallback to LocalSaveSource |
| `Assets/RatAds/Plugins/iOS/RatSavesiOS.mm` | Native Obj-C plugin for `NSUbiquitousKeyValueStore` |
| `Assets/RatAds/Runtime/IAdSource.cs` | Ad adapter interface |
| `Assets/RatAds/Runtime/Purchases/IPurchaseSource.cs` | Purchase adapter interface |
| `Assets/RatAds/Runtime/Leaderboards/ILeaderboardSource.cs` | Leaderboard adapter interface |
| `Assets/RatAds/Runtime/AdSourceBuilder.cs` | Selects ad adapter |
| `Assets/RatAds/Runtime/Purchases/PurchaseSourceBuilder.cs` | Selects purchase adapter |
| `Assets/RatAds/Runtime/Leaderboards/LeaderboardSourceBuilder.cs` | Selects leaderboard adapter; handles virtual keys |
| `Assets/RatAds/Runtime/Leaderboards/LeaderboardEntry.cs` | Entry data: PlayerId, PlayerName, Score, Rank, IsCurrentPlayer, LastReportedDate |
| `Assets/RatAds/Runtime/Leaderboards/LeaderboardTimeScope.cs` | Enum: AllTime, Today — передаётся в GetEntries/GetPlayerEntry |
| `Assets/RatAds/Runtime/Leaderboards/LeaderboardDefinition.cs` | Per-leaderboard IDs: Id, GpgsId, GameCenterId |
| `Assets/RatAds/Runtime/RatAdsConfig.cs` | Ads config ScriptableObject |
| `Assets/RatAds/Runtime/Purchases/RatPurchasesConfig.cs` | Purchases config ScriptableObject |
| `Assets/RatAds/Runtime/Leaderboards/RatLeaderboardsConfig.cs` | Leaderboards config ScriptableObject |
| `Assets/RatAds/Runtime/Rate/IRateSource.cs` | Rate adapter interface |
| `Assets/RatAds/Runtime/Rate/RatRate.cs` | Main rate/review API |
| `Assets/RatAds/Runtime/Rate/RateSourceBuilder.cs` | Selects rate adapter; handles virtual keys |
| `Assets/RatAds/Runtime/Rate/RatRateConfig.cs` | Rate config ScriptableObject |
| `Assets/RatAds/Runtime/Share/IShareSource.cs` | Share adapter interface |
| `Assets/RatAds/Runtime/Share/ShareData.cs` | Internal share content model: Text, Image (Texture2D), ImageUrl |
| `Assets/RatAds/Runtime/Share/ShareItem.cs` | Public share content unit: Text/Image/ImageUrl/Screenshot factory methods |
| `Assets/RatAds/Runtime/Share/SharingServices.cs` | Public share API — `ShowShareSheet(callback, params ShareItem[])` |
| `Assets/RatAds/Runtime/Share/RatShare.cs` | Internal share facade — `Share(ShareData, callback)` called by SharingServices |
| `Assets/RatAds/Runtime/Share/ShareSourceBuilder.cs` | Selects share adapter; handles virtual/platform keys |
| `Assets/RatAds/Runtime/Share/RatShareConfig.cs` | Share config ScriptableObject |
| `Assets/RatAds/Runtime/Analytics/IAnalyticsSource.cs` | Analytics adapter interface |
| `Assets/RatAds/Runtime/Analytics/RatAnalytics.cs` | Main analytics API — `LogEvent(name, params)` |
| `Assets/RatAds/Runtime/Analytics/RatAnalyticsConfig.cs` | Analytics config ScriptableObject (git-ignored) |
| `Assets/RatAds/Runtime/Analytics/AnalyticsSourceRegistry.cs` | Dictionary-based analytics adapter registry |
| `Assets/RatAds/Runtime/Analytics/AnalyticsSourceBuilder.cs` | Selects analytics adapter; returns null if NoneAdapterKey |
| `Assets/RatAds/Runtime/Analytics/Adapters/AppMetrica/AppMetricaAnalyticsSource.cs` | AppMetrica adapter (`#if RATANALYTICS_APPMETRICA`) |
| `Assets/RatAds/Runtime/RatEnvironment.cs` | Platform environment API — language, audio, pause, visibility, platform ID |
| `Assets/RatAds/Runtime/IPlatformParamsProvider.cs` | Platform provider interface for RatEnvironment |
| `Assets/RatAds/Runtime/Adapters/Playgama/PlaygamaPlatformProvider.cs` | Playgama implementation of IPlatformParamsProvider (Bridge.platform.*) |
| `Assets/RatAds/Runtime/StoreTargetMask.cs` | `[Flags]` enum of store targets — used by `DisableOnStore` |
| `Assets/RatAds/Runtime/DisableOnStore.cs` | MonoBehaviour — disables GameObject on selected stores in `Awake()` |
| `Assets/RatAds/Runtime/DisableIfNotSupported.cs` | MonoBehaviour — disables GameObject if selected system (`RatSystemCheck`) is not supported at runtime |
| `Assets/RatAds/Editor/BuildManager/BuildPreprocessor.cs` | Validates store defines before build; generates/deletes link.xml |
| `Assets/RatAds/Editor/BuildManager/AdapterLinkXmlGenerator.cs` | Generates `Assets/RatAds/Generated/link.xml` to prevent IL2CPP stripping of selected adapter assemblies |
| `Assets/RatAds/Editor/BuildManager/AdapterDefines.cs` | Adapter define→name maps + `GetAdAdapters/GetPurchaseAdapters/GetLeaderboardAdapters/GetRateAdapters/GetSaveAdapters/GetAnalyticsAdapters()` |
| `Assets/RatAds/Editor/BuildManager/StoreAndroidConfigurator.cs` | Dispatcher: `OnStoreChanged(prev, new, ratShareEnabled)` — calls RuStore + RatShare configurators |
| `Assets/RatAds/Editor/BuildManager/RuStoreAndroidConfigurator.cs` | Generates Android manifest for RuStore; `EnsureAndroidLibBuildGradle()` at build time |
| `Assets/RatAds/Editor/BuildManager/RatShareAndroidConfigurator.cs` | Generates `RatShareAndroid.androidlib/` (res/xml resource) for Android FileProvider |
| `Assets/RatAds/Editor/BuildManager/RatShareAndroidManifestInjector.cs` | `IPostGenerateGradleAndroidProject` — injects `<provider>` into generated unityLibrary manifest at Gradle project generation time |
| `Assets/RatAds/Editor/BuildManager/ChecklistRegistry.cs` | Maps defines → checklist providers |
| `Assets/RatAds/Editor/BuildManager/IChecklistProvider.cs` | Checklist provider interface |
| `Assets/RatAds/Editor/BuildManager/StorePresetsManager.cs` | `StorePreset` model (adsAdapter, purchasesAdapter, leaderboardsAdapter, rateAdapter, shareAdapter, savesAdapter, analyticsAdapter) + defaults |
| `Assets/RatAds/Editor/ChecklistUIHelper.cs` | Shared checklist UI rendering |
| `Assets/RatAds/Editor/ScriptingDefinesManager.cs` | Batched scripting define management |
| `Assets/RatAds/Editor/SDKInstallerWindow.cs` | UI Toolkit SDK Installer |
| `Assets/RatAds/Editor/SDKVersionChecker.cs` | On-load SDK detection and define sync |
| `Assets/RatAds/Editor/SDKVersions.json` | Canonical SDK versions and package IDs |
| `Assets/RatAds/Editor/BuildManager/RatAdsBuildManagerUIWindow.cs` | UI Toolkit Build Manager |
| `Assets/RatAds/Editor/BuildManager/StorePresets.json` | Store preset configurations |

## Build Validation Rules

`BuildPreprocessor` enforces before every build:
1. Generates `Assets/RatAds/Generated/link.xml` via `AdapterLinkXmlGenerator.Generate()` — preserves selected adapter assemblies from IL2CPP stripping (file is deleted in `OnPostprocessBuild`)
2. On Android: calls `RuStoreAndroidConfigurator.EnsureAndroidLibBuildGradle()`; if `RatMobileKit/RatShareAndroid.androidlib/` is missing, calls `RatShareAndroidConfigurator.Configure()` (safety net for fresh installs)
3. Exactly one `RATADS_STORE_*` define must be set; if `RATADS_STORE_EDITOR` is active — build fails immediately with a message to select a real store (Editor store is for in-editor use only)
4. Store define must match the current build target (e.g., `RATADS_STORE_APPSTORE` requires iOS)
5. The required SDK defines for the target platform must be present
6. If `RatAdsConfig.PreferredAdsAdapter` is set, warns if that SDK define is not installed (build proceeds with Debug adapter)

### IL2CPP Adapter Stripping

Adapter assemblies (`RatAds.Yandex`, `RatAds.LevelPlay`, etc.) have no direct references from game code — only `[RuntimeInitializeOnLoadMethod]` bootstrap. The IL2CPP managed linker can strip the entire assembly before the bootstrap runs, causing `AdSourceBuilder` to fall back to `DebugAdSource`.

`AdapterLinkXmlGenerator` generates a `link.xml` with `preserve="all"` for each adapter assembly whose define is selected in the configs (`PreferredAdsAdapter`, `PreferredPurchaseAdapter`, `PreferredLeaderboardAdapter`, `PreferredRateAdapter`, `PreferredSavesAdapter`, `PreferredAnalyticsAdapter`). Virtual keys (`RATLEADERBOARDS_SIMULATED`, `RATRATE_MOCK`, `UNITY_IOS_GAMECENTER`, `UNITY_IOS_STOREREVIEW`, `RATSAVES_SIMULATED`, `UNITY_IOS_ICLOUD`) are excluded — they live in core assemblies that are always preserved. The dictionary `AdapterLinkXmlGenerator.AdapterAssemblies` maps SDK define → assembly name; includes `{ "RATSAVES_GPGS", "RatSaves.GPGS" }` and `{ "RATANALYTICS_APPMETRICA", "RatAnalytics.AppMetrica" }`.

Note: RatShare adapters use virtual keys or `RATADS_PLAYGAMA` — `RatShare.Runtime` is `autoReferenced` and not stripped. `RatShare.Playgama` (autoReferenced=false) is preserved via `{ "RATADS_PLAYGAMA", "RatShare.Playgama" }` entry when Playgama share adapter is selected.

`.gitignore` excludes auto-generated directories: `Assets/RatAds/Generated/` (link.xml) and `Assets/Plugins/Android/RatMobileKit/RatShareAndroid.androidlib/`.

## Adding a New Ad Adapter

1. Implement `IAdSource`
2. Self-register in a `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` via `AdSourceRegistry.Register("SDK_DEFINE_KEY", factory, priority)` — key must equal the SDK scripting define
3. Add a new scripting define; create an asmdef gated by it
4. Add the SDK define → display name entry to `AdapterDefines.AdsAdapterNames` and `AdapterDefines.GetAdAdapters()`
5. Add a drawer in `Assets/RatAds/Editor/Drawers/` implementing `ISettingsDrawer`
6. Add platform-specific settings class to `RatAdsConfig`
7. Add the adapter to relevant store presets in `StorePresets.json` / `StorePresetsManager.CreateDefaults()`
8. Add `{ "SDK_DEFINE_KEY", "AssemblyName" }` to `AdapterLinkXmlGenerator.AdapterAssemblies` to prevent IL2CPP stripping

### Ad Adapter Retry Pattern

All ad wrappers (Banner, Interstitial, Reward) use exponential backoff retry via `RetryHelper`. The correct pattern uses `_retryPending` flag to prevent duplicate timers:
- `HandleAdFailedToLoad`: set `_retryPending = true` before calling `RetryHelper.InvokeAfter(delay, RetryLoad)`
- `LoadAd()`: guard with `|| _retryPending` alongside `_isLoading`
- `RetryLoad()`: set `_retryPending = false` first, then check `|| _isLoading` before calling `TryLoadOnce()`

## Adding a New Purchase Adapter

1. Implement `IPurchaseSource` (include `_ownershipCache` keyed by **`ProjectId`** for synchronous `IsPurchased`)
2. Self-register via `PurchaseSourceRegistry.Register(factory, priority)`
3. **`Purchase(projectId, ...)`**: use `config.FindByProjectId(projectId)` → get store-specific ID → call SDK. Report `projectId` in `PurchaseResult.ProductId` and ownership cache.
4. **SDK callbacks**: map store product ID → `projectId` via `config.FindByStoreProductId(storeId)`. Populate `ProductData.ProductId` with `projectId` in `FetchProducts`.
5. Add a new scripting define; create an asmdef gated by it
6. Add a drawer in `Assets/RatAds/Editor/Purchases/Drawers/` implementing `ISettingsDrawer`
7. Add platform-specific settings class to `RatPurchasesConfig`
8. Add `{ "SDK_DEFINE_KEY", "AssemblyName" }` to `AdapterLinkXmlGenerator.AdapterAssemblies` to prevent IL2CPP stripping
9. In `RestorePurchases`: after refreshing `_ownershipCache`, fire `OnPurchaseSuccess?.Invoke(PurchaseResult.FromRestored(id))` for each owned non-consumable. If the platform's restore flow replays purchases through a normal purchase callback (like Unity IAP iOS `ProcessPurchase`), fire `Restored` only for the diff to avoid double-activation.

## Adding a New Leaderboard Adapter

1. Implement `ILeaderboardSource` (`Initialize`, `SubmitScore`, `GetEntries(leaderboardId, count, timeScope, onComplete)`, `GetPlayerEntry(leaderboardId, timeScope, onComplete)`, `IsAuthenticated`, `LocalPlayerName`). Populate `LastReportedDate` в `LeaderboardEntry` из платформенных данных; для адаптеров без поддержки даты — `DateTime.MinValue`.
2. Self-register via `LeaderboardSourceRegistry.Register("SDK_DEFINE_KEY", factory, priority)` inside `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`, gated by `#if`
3. Add a new scripting define (or reuse an existing one like `"RATADS_PLAYGAMA"`); create an asmdef gated by it if needed
4. Add the define → display name to `AdapterDefines.LeaderboardAdapterNames` and update `AdapterDefines.GetLeaderboardAdapters()`
5. Add the adapter to relevant store presets in `StorePresets.json` / `StorePresetsManager.CreateDefaults()`
6. Add `{ "SDK_DEFINE_KEY", "AssemblyName" }` to `AdapterLinkXmlGenerator.AdapterAssemblies` to prevent IL2CPP stripping (skip for virtual keys — they live in core assemblies)

If the adapter key is a virtual key (always available, not a real scripting define), add it to `IsAdapterSdkInstalled()` in `RatAdsBuildManagerUIWindow.cs` to return `true` unconditionally, and handle it explicitly in `LeaderboardSourceBuilder.Build()` before calling the registry.

## Adding a New Rate Adapter

1. Implement `IRateSource` (`Initialize`, `RequestReview`; set `IsSupported = false` for platforms that don't support reviews)
2. Self-register via `RateSourceRegistry.Register("SDK_DEFINE_KEY", factory, priority)` inside `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`, gated by `#if`
3. Add a new scripting define; create an asmdef gated by it if a new SDK package is required
4. Add the define → display name to `AdapterDefines.RateAdapterNames` and update `AdapterDefines.GetRateAdapters()`
5. Add the adapter to relevant store presets in `StorePresets.json` / `StorePresetsManager.CreateDefaults()`
6. Add `{ "SDK_DEFINE_KEY", "AssemblyName" }` to `AdapterLinkXmlGenerator.AdapterAssemblies` to prevent IL2CPP stripping (skip for virtual keys — they live in core assemblies)

If the adapter key is a virtual key (e.g. `"UNITY_IOS_STOREREVIEW"`, `"RATRATE_MOCK"`), add it to `IsAdapterSdkInstalled()` in `RatAdsBuildManagerUIWindow.cs` to return `true` unconditionally, and handle it explicitly in `RateSourceBuilder.Build()` before calling the registry.

## Adding a New Save Adapter

1. Implement `ISaveSource` (`Save(key, json, Action<bool>)`, `Load(key, Action<bool, string>)`, `Delete(key, Action<bool>)`, `HasKey(key, Action<bool>)`)
2. Self-register via `SaveSourceRegistry.Register("SDK_DEFINE_KEY", factory, priority)` inside `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`, gated by `#if`
3. Add a new scripting define (or reuse an existing one like `"RATADS_PLAYGAMA"`); create an asmdef gated by it if needed
4. Add the define → display name to `AdapterDefines.SaveAdapterNames` and update `AdapterDefines.GetSaveAdapters()`
5. Add the adapter to relevant store presets in `StorePresets.json` / `StorePresetsManager.CreateDefaults()`
6. Add `{ "SDK_DEFINE_KEY", "AssemblyName" }` to `AdapterLinkXmlGenerator.AdapterAssemblies` to prevent IL2CPP stripping (skip for virtual keys — they live in core assemblies)

If the adapter key is a virtual key (always available, not a real scripting define), add it to `IsAdapterSdkInstalled()` in `RatAdsBuildManagerUIWindow.cs` to return `true` unconditionally, and handle it explicitly in `SaveSourceBuilder.Build()` before calling the registry.

## Adding a New Share Adapter

1. Implement `IShareSource` (`Initialize`, `Share(ShareData, Action<ShareSheetResult, ShareError>)`; set `IsSupported = false` if the platform doesn't support it)
2. Since all share adapters use virtual keys, handle the new adapter explicitly in `ShareSourceBuilder.Build()` (before registry lookup) gated by `#if`
3. If a real SDK define is needed, create an asmdef with that define as a constraint; otherwise, add to `RatShare.Runtime.asmdef`
4. Add the define → display name to `AdapterDefines.ShareAdapterNames` and `AdapterDefines.GetShareAdapters()`
5. Add the adapter key to `StorePresets.json` / `StorePresetsManager.CreateDefaults()` for relevant stores
6. Add the virtual key to `IsAdapterSdkInstalled()` in `RatAdsBuildManagerUIWindow.cs` to return `true` unconditionally
7. If the adapter requires Android native files (FileProvider, manifest entries, etc.), add `Configure()`/`Cleanup()` logic to `RatShareAndroidConfigurator` and call from `StoreAndroidConfigurator.OnStoreChanged()`

## Adding a New Analytics Adapter

1. Implement `IAnalyticsSource` (`Initialize`, `LogEvent(string, Dictionary<string,object>)`)
2. Self-register via `AnalyticsSourceRegistry.Register("SDK_DEFINE_KEY", factory, priority)` inside `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`, gated by `#if`
3. Add a new scripting define; create an asmdef with `defineConstraints: ["SDK_DEFINE_KEY"]` and `autoReferenced: false`
4. Add the define → display name to `AdapterDefines.AnalyticsAdapterNames` and `AdapterDefines.GetAnalyticsAdapters()`
5. Add the adapter to relevant store presets in `StorePresets.json` / `StorePresetsManager.CreateDefaults()`
6. Add `{ "SDK_DEFINE_KEY", "AssemblyName" }` to `AdapterLinkXmlGenerator.AdapterAssemblies` to prevent IL2CPP stripping
7. Add API key / settings field to `RatAnalyticsConfig` and configure in Build Manager settings drawer

## Adding a New Checklist Provider

1. Implement `IChecklistProvider` (`Title`, `GetItems()`)
2. Register it in `ChecklistRegistry._sdkChecklists` (keyed by define string) or `_storeChecklists`

## Editor Testing

`DebugAdSource` simulates ad behavior in the Editor. The debug canvas (`Resources/DebugAdCanvas.prefab`) shows mock UI panels. Tap 6 times rapidly anywhere at runtime to toggle debug ads on/off.

`DebugLeaderboardSource` provides an in-memory leaderboard mock in the Editor (`IsAuthenticated = true`, `LocalPlayerName = "[DEBUG] You"`, `LastReportedDate = DateTime.Now` for player entry). `SimulatedLeaderboardSource` is the runtime fallback — it generates deterministic bot entries from the leaderboard ID seed, persists them in PlayerPrefs, and stores the player's score submission timestamp (`ratleaderboards_date_{id}`) for `LastReportedDate`.

`DebugRateSource` simulates review requests in the Editor (logs and immediately calls `onComplete(true)`).

`DebugPurchaseSource` shows a UI Toolkit overlay dialog when `Purchase()` is called. The dialog displays the product ID, mock price ($0.99), and product type. "Buy" → `PurchaseStatus.Success`; "Cancel" → `PurchaseStatus.Cancelled`. `AlreadyOwned` for non-consumables already in cache returns immediately without showing the dialog.

Dialog is implemented via `DebugPurchasePanel` MonoBehaviour (built entirely in C# — no UXML/USS). `EnsureUI()` is called lazily on first `Show()`. It looks for an existing `UIDocument` in the scene via `FindObjectOfType<UIDocument>()` to reuse its panel (guarantees correct screen size and font inheritance); if none is found, creates a fallback `UIDocument`+`PanelSettings` (ConstantPixelSize, sortingOrder=1000). The overlay is absolute-positioned with `left/top/right/bottom=0` to cover the parent reliably. All labels and buttons have an explicit font set via `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` to ensure text renders even without a theme. **Important pattern:** `OnConfirm`/`OnCancel` save `_callback` to a local variable before calling `Hide()` (which nulls `_callback`), then invoke the local — otherwise the callback is lost.

`DebugShareSource` simulates share calls in the Editor (logs share data content, immediately calls `onComplete(true)`).

`DebugAnalyticsSource` logs all `LogEvent` calls to the Unity console in the Editor.
