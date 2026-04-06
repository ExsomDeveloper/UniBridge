# UniBridge — Руководство по разработке

Справочник для разработчиков пакета. Описывает архитектуру, паттерны и пошаговые инструкции по добавлению новых систем и адаптеров.

---

## Содержание

1. [Архитектура: три кита](#1-архитектура-три-кита)
2. [Структура директорий](#2-структура-директорий)
3. [Scripting Defines и виртуальные ключи](#3-scripting-defines-и-виртуальные-ключи)
4. [Assembly Definitions (asmdef)](#4-assembly-definitions-asmdef)
5. [Timing регистрации адаптеров](#5-timing-регистрации-адаптеров)
6. [IL2CPP Linker (link.xml)](#6-il2cpp-linker-linkxml)
7. [Build Manager и Store Presets](#7-build-manager-и-store-presets)
8. [Android конфигурация](#8-android-конфигурация)
9. [Checklist система](#9-checklist-система)
10. [Settings Drawers](#10-settings-drawers)
11. [Демо сцена](#11-демо-сцена)
12. [Опциональные системы (паттерн UNIBRIDGESHARE_ENABLED)](#12-опциональные-системы-паттерн-unibridgeshare_enabled)
13. [Retry-паттерн для рекламных оберток](#13-retry-паттерн-для-рекламных-оберток)
14. [Чеклисты добавления новой сущности](#14-чеклисты-добавления-новой-сущности)
15. [Quick Reference: карта ключевых файлов](#15-quick-reference-карта-ключевых-файлов)
16. [Подводные камни](#16-подводные-камни)

---

## 1. Архитектура: три кита

Каждая подсистема строится по одним и тем же трём паттернам.

### Паттерн 1: Статический фасад (Static Facade)

Каждая система (`UniBridge`, `UniBridgePurchases`, `UniBridgeLeaderboards`, `UniBridgeSaves`, `UniBridgeRate`, `UniBridgeShare`) — статический класс (или MonoBehaviour-синглтон для `UniBridge`).

Шаблон всегда одинаков:

| Член | Роль |
|------|------|
| `IsInitialized` | флаг готовности |
| `IsSupported` | поддерживается ли на платформе (у систем с заглушкой) |
| `AdapterName` | имя активного адаптера для отладки |
| `OnInitSuccess` / `OnInitFailed` | события |
| `AutoInitialize()` | `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`, гвард `if (IsInitialized) return;` |
| `Initialize()` | ручная инициализация |
| конфиг | `Resources.Load<Rat*Config>(nameof(Rat*Config))` |

### Паттерн 2: Реестр + Строитель (Registry + Builder)

Поток данных:

```
Адаптер → [RuntimeInitializeOnLoadMethod] → Registry.Register(define, factory, priority)
Facade  → Builder.Build(config)           → Registry.Create(config) → IXxxSource
```

**Реестр** (`AdSourceRegistry`, `PurchaseSourceRegistry`, `LeaderboardSourceRegistry`, `RateSourceRegistry`, `ShareSourceRegistry`) — `Dictionary<string, (Factory, Priority)>`, ключ = строка SDK define, например `"UNIBRIDGE_YANDEX"`.

**Строитель** — в `#if UNITY_EDITOR` всегда возвращает Debug/Mock. В рантайме: спрашивает реестр, при неудаче — Debug/Mock/Simulated.

### Паттерн 3: Config ScriptableObject

Каждая система имеет `Rat*Config : ScriptableObject` в `Assets/UniBridge/Resources/`, загружаемый через `Resources.Load`. Хранит `AutoInitialize`, `Preferred*Adapter` и SDK-специфичные настройки. Используется как редактором, так и рантаймом.

---

## 2. Структура директорий

```
Assets/UniBridge/
  Runtime/
    UniBridge.cs               ← фасад рекламы (MonoBehaviour)
    IAdSource.cs            ← интерфейс рекламного адаптера
    AdSourceRegistry.cs     ← реестр рекламных адаптеров
    AdSourceBuilder.cs      ← выбор адаптера
    UniBridgeConfig.cs         ← конфиг рекламы
    Adapters/
      Debug/                ← DebugAdSource (редактор)
      LevelPlay/            ← LevelPlayAdapter
      Yandex/               ← YandexAdapter
      Playgama/             ← PlaygamaAdapter + PlaygamaPurchaseSource + ...
    Purchases/              ← IPurchaseSource, реестр, строитель, фасад, адаптеры
    Leaderboards/           ← ILeaderboardSource, реестр, строитель, фасад, адаптеры
    Rate/                   ← IRateSource, реестр, строитель, фасад, адаптеры
    Share/                  ← IShareSource, реестр, строитель, фасад, адаптеры
                              (только при UNIBRIDGESHARE_ENABLED)
  Editor/
    BuildManager/
      AdapterDefines.cs               ← define→name, списки адаптеров по платформе
      AdapterLinkXmlGenerator.cs      ← генерация link.xml против IL2CPP stripping
      BuildPreprocessor.cs            ← валидация перед сборкой
      ChecklistRegistry.cs            ← реестр чеклистов
      IChecklistProvider.cs           ← интерфейс чеклиста
      UniBridgeBuildManagerUIWindow.cs   ← UI Build Manager
      UniBridgeShareAndroidConfigurator.cs  ← генерация UniBridgeShareAndroid.androidlib/
      UniBridgeShareAndroidManifestInjector.cs ← инъекция <provider> в manifest (post-Gradle)
      RuStoreAndroidConfigurator.cs   ← генерация RuStoreUnityPay/AndroidManifest.xml
      StoreAndroidConfigurator.cs     ← единая точка диспетчеризации Android-конфигурации
      StorePresets.json               ← источник истины для пресетов сторов
      StorePresetsManager.cs          ← загрузка/сохранение пресетов
      StorePlatformDefines.cs         ← константы store defines + helper методы
      Checklists/                     ← реализации IChecklistProvider
    Drawers/                ← ISettingsDrawer для рекламных SDK
    Purchases/Drawers/      ← ISettingsDrawer для purchase SDK
    Rate/                   ← настройки оценок в редакторе
    Share/                  ← настройки шаринга в редакторе
    SDKInstallerWindow.cs   ← UI SDK Installer
    SDKVersionChecker.cs    ← авто-детектирование установленных SDK при загрузке редактора
    SDKVersions.json        ← метаданные SDK (версия, packageId, define)
    ScriptingDefinesManager.cs ← батчированное управление scripting defines
    ChecklistUIHelper.cs    ← общий UI-рендер чеклистов
  Resources/
    UniBridgeConfig.asset
    UniBridgePurchasesConfig.asset
    UniBridgeLeaderboardsConfig.asset
    UniBridgeRateConfig.asset
    UniBridgeShareConfig.asset        ← авто-ген при первой сборке/выборе, git-игнорируется
    DebugAdCanvas.prefab        ← mock UI для DebugAdSource
  Plugins/
    Android/
      UniBridgeSharePlugin.java       ← hand-written нативный Java-плагин шаринга
    iOS/
      UniBridgeShareiOS.mm            ← hand-written Obj-C плагин шаринга

Assets/Plugins/Android/UniBridgeMobileKit/
  RuStoreUnityPay/              ← авто-ген, коммитится в git
  UniBridgeShareAndroid.androidlib/   ← авто-ген, git-игнорируется
  Generated/                    ← авто-ген link.xml, git-игнорируется

Assets/Demo/
  DemoScene.unity
  DemoMainUI.cs / .uxml / .uss   ← полный демо-контроллер (все системы)
  DemoUniBridgeUI.cs / .uxml / .uss ← упрощённый демо только для рекламы
```

---

## 3. Scripting Defines и виртуальные ключи

### Реальные defines (существуют в PlayerSettings)

| Define | Система | Как устанавливается |
|--------|---------|-------------------|
| `UNIBRIDGE_LEVELPLAY` | Ads | SDKVersionChecker (авто) |
| `UNIBRIDGE_PLAYGAMA` | Ads / Purchases / Leaderboards / Saves / Share | SDKVersionChecker (авто) |
| `UNIBRIDGE_YANDEX` | Ads | SDK Installer (вручную) |
| `UNIBRIDGEPURCHASES_IAP` | Purchases | SDKVersionChecker (авто) |
| `UNIBRIDGEPURCHASES_RUSTORE` | Purchases | SDK Installer (вручную) |
| `UNIBRIDGELEADERBOARDS_GPGS` | Leaderboards | SDKVersionChecker (авто) |
| `UNIBRIDGERATE_GOOGLEPLAY` | Rate | SDK Installer (вручную) |
| `UNIBRIDGERATE_RUSTORE` | Rate | SDK Installer (вручную) |
| `UNIBRIDGE_STORE_*` | Все | Build Manager |
| `UNIBRIDGESHARE_ENABLED` | Share | Build Manager (тогл опциональной системы) |

### Виртуальные ключи (строки-константы, не реальные defines)

Используются как значение `Preferred*Adapter` в конфиге. Builder обрабатывает их явно **до** обращения к реестру:

| Ключ | Что возвращает Builder |
|------|----------------------|
| `"UNIBRIDGELEADERBOARDS_SIMULATED"` | `SimulatedLeaderboardSource` |
| `"UNITY_IOS_GAMECENTER"` | `GameCenterLeaderboardSource` |
| `"UNITY_IOS_STOREREVIEW"` | `AppStoreReviewSource` |
| `"UNIBRIDGERATE_MOCK"` | `MockRateSource` (IsSupported=false) |
| `"UNIBRIDGESHARE_ANDROID"` | `AndroidShareSource` |
| `"UNIBRIDGESHARE_IOS"` | `iOSShareSource` |
| `"UNIBRIDGESHARE_MOCK"` | `MockShareSource` (IsSupported=false) |
| `"UNIBRIDGE_NONE"` | `null` (система намеренно отключена) |

> **⚠️ Правило:** Каждый виртуальный ключ обязан возвращать `true` в методе `IsAdapterSdkInstalled()` в `UniBridgeBuildManagerUIWindow.cs`. Иначе Build Manager покажет адаптер как "(не установлен)".

### ScriptingDefinesManager

Для изменения defines из Editor-кода используй только `ScriptingDefinesManager`, не `PlayerSettings` напрямую:

```csharp
ScriptingDefinesManager.AddDefine("MY_DEFINE");    // батчируется → один recompile
ScriptingDefinesManager.RemoveDefine("MY_DEFINE");
ScriptingDefinesManager.Flush();                   // применить немедленно (синхронно)
```

Батчирование работает через `EditorApplication.delayCall` — несколько вызовов `Add/Remove` в одном тике редактора приводят к одной рекомпиляции.

---

## 4. Assembly Definitions (asmdef)

### Соглашение об именовании

```
Rat{Система}.{Runtime | Editor | НазваниеАдаптера}
```

Примеры: `UniBridge.Runtime`, `UniBridge.Editor`, `UniBridge.Yandex`, `UniBridge.Rate.GooglePlay`, `UniBridge.Share.Runtime`, `UniBridge.Share.Playgama`

### Шаблон asmdef для нового адаптера

```json
{
    "name": "UniBridge.NewSdk",
    "rootNamespace": "UniBridge",
    "references": ["UniBridge.Runtime", "NewSdkUnityAssemblyName"],
    "autoReferenced": true,
    "defineConstraints": ["UNIBRIDGE_NEWSDK"]
}
```

### Правила

| Ситуация | Правило |
|----------|---------|
| Все asmdef в пакете | `autoReferenced: true` |
| Адаптер с реальным define | `defineConstraints: ["UNIBRIDGE_NEWSDK"]` |
| Полностью опциональная система | `defineConstraints: ["UNIBRIDGESHARE_ENABLED"]` |
| Адаптер опциональной системы + SDK | `defineConstraints: ["UNIBRIDGE_PLAYGAMA", "UNIBRIDGESHARE_ENABLED"]` |
| Editor asmdef | без `defineConstraints` (компилируются в любом режиме) |

### ⚠️ Подводный камень: UniBridge.Leaderboards.Runtime

Несмотря на `autoReferenced: true`, `UniBridge.Leaderboards.Runtime` **не виден** в editor-сборках автоматически. Он явно прописан в `references` у `UniBridge.Editor.asmdef`. При добавлении нового модуля, который нужен в Editor, — аналогично добавляй явную ссылку.

---

## 5. Timing регистрации адаптеров

Порядок `RuntimeInitializeLoadType` критичен:

| Уровень | Используется для |
|---------|-----------------|
| `SubsystemRegistration` | **Рекламные адаптеры** — LevelPlay, Yandex, Playgama |
| `BeforeSceneLoad` | Purchases, Leaderboards, Saves, Rate, Share; также `UniBridge.AutoInitialize` |

**Почему реклама — `SubsystemRegistration`:** `UniBridge.AutoInitialize` запускается на `BeforeSceneLoad` и уже читает `AdSourceRegistry`. Если рекламный адаптер регистрируется на том же уровне (`BeforeSceneLoad`) — порядок выполнения не гарантирован, и реестр может оказаться пустым в момент чтения.

Все остальные системы (`UniBridgePurchases.AutoInitialize` и т.д.) тоже запускаются на `BeforeSceneLoad`, но их адаптеры регистрируются раньше (из адаптерных сборок). Порядок внутри одного уровня среди разных сборок не строго гарантирован, однако на практике работает корректно.

---

## 6. IL2CPP Linker (link.xml)

### Проблема

Адаптерные сборки (например `UniBridge.Yandex`) не имеют прямых ссылок из игрового кода — только `[RuntimeInitializeOnLoadMethod]`. IL2CPP managed linker может полностью удалить такую сборку до выполнения bootstrap. Результат: пустой реестр → Runtime откатывается на `DebugAdSource` в релизной сборке.

### Решение

`AdapterLinkXmlGenerator.Generate()` вызывается в `BuildPreprocessor.OnPreprocessBuild()`. Генерирует `Assets/UniBridge/Generated/link.xml`:

```xml
<!-- Auto-generated by UniBridge BuildPreprocessor. Do not edit manually. -->
<linker>
  <assembly fullname="UniBridge.Yandex" preserve="all" />
  <assembly fullname="UniBridge.Purchases.UnityIAP" preserve="all" />
  <!-- ... -->
</linker>
```

Файл удаляется в `OnPostprocessBuild`. Папка `Generated/` находится в `.gitignore`.

### Что добавлять в `AdapterAssemblies`

Файл `Editor/BuildManager/AdapterLinkXmlGenerator.cs`, словарь `AdapterAssemblies`:

```csharp
{ "UNIBRIDGE_LEVELPLAY",     "UniBridge.LevelPlay" },
{ "UNIBRIDGE_YANDEX",        "UniBridge.Yandex" },
{ "UNIBRIDGE_PLAYGAMA",      "UniBridge.Playgama" },
{ "UNIBRIDGEPURCHASES_IAP",     "UniBridge.Purchases.UnityIAP" },
{ "UNIBRIDGEPURCHASES_RUSTORE", "UniBridge.Purchases.RuStore" },
{ "UNIBRIDGELEADERBOARDS_GPGS", "UniBridge.Leaderboards.GPGS" },
{ "UNIBRIDGERATE_GOOGLEPLAY",   "UniBridge.Rate.GooglePlay" },
{ "UNIBRIDGERATE_RUSTORE",      "UniBridge.Rate.RuStore" },
// ← НОВЫЙ АДАПТЕР ЗДЕСЬ
```

### Что НЕ добавлять

- **Виртуальные ключи** (`UNIBRIDGELEADERBOARDS_SIMULATED`, `UNIBRIDGERATE_MOCK`, `UNITY_IOS_*`) — их типы живут в основных сборках (`UniBridge.Leaderboards.Runtime`, `UniBridge.Rate.Runtime`), которые всегда сохраняются
- **Share-адаптеры** — все они в `UniBridge.Share.Runtime` и `UniBridge.Share.Playgama`, оба `autoReferenced: true` и не стрипаются

---

## 7. Build Manager и Store Presets

### Файл StorePresets.json

Источник истины для 5 пресетов (Editor, Google Play, RuStore, App Store, Playgama):

```json
{
    "displayName": "Google Play",
    "define": "UNIBRIDGE_STORE_GOOGLEPLAY",
    "buildTarget": "Android",
    "adsAdapter": "UNIBRIDGE_YANDEX",
    "purchasesAdapter": "UNIBRIDGEPURCHASES_IAP",
    "leaderboardsAdapter": "UNIBRIDGELEADERBOARDS_GPGS",
    "rateAdapter": "UNIBRIDGERATE_GOOGLEPLAY",
    "shareAdapter": "UNIBRIDGESHARE_ANDROID",
    "sdkDefines": ["UNIBRIDGE_YANDEX", "UNIBRIDGEPURCHASES_IAP"]
}
```

`sdkDefines` — список SDK для валидации в `BuildPreprocessor` перед сборкой. Package ID (строки, содержащие `.`) пропускаются — их нельзя проверить через scripting defines.

### Поток при нажатии "Выбрать" в Build Manager

1. `StorePlatformDefines.SetStoreDefine(newDefine)` — обновляет PlayerSettings, убирает предыдущий store define
2. Запись `Preferred*Adapter` в Config ScriptableObjects
3. `StoreAndroidConfigurator.OnStoreChanged(prev, new, ratShareEnabled)` → делегирует:
   - `RuStoreAndroidConfigurator.Configure()` или `Cleanup()`
   - `UniBridgeShareAndroidConfigurator.Configure()` или `Cleanup()`

### Валидация перед сборкой (BuildPreprocessor)

Последовательность в `OnPreprocessBuild`:

1. Генерирует `link.xml` → `AdapterLinkXmlGenerator.Generate()`
2. Ровно один `UNIBRIDGE_STORE_*` define; если `UNIBRIDGE_STORE_EDITOR` — сборка падает с сообщением выбрать реальный стор
3. Store define соответствует build target (например `UNIBRIDGE_STORE_APPSTORE` требует iOS)
4. Все `sdkDefines` пресета присутствуют в PlayerSettings
5. На Android: `RuStoreAndroidConfigurator.EnsureAndroidLibBuildGradle()`; safety-net для UniBridgeShare файлов при свежей установке
6. Post-build (`OnPostprocessBuild`): удаляет `link.xml` и папку `Generated/`

---

## 8. Android конфигурация

### Принцип

Все Android-файлы, создаваемые UniBridge, живут в `Assets/Plugins/Android/UniBridgeMobileKit/`. Этот префиксный путь исключает конфликты с файлами SDK.

Единая точка диспетчеризации: `StoreAndroidConfigurator.OnStoreChanged(previousDefine, newDefine, ratShareEnabled)`.

### Маркеры авто-генерированных файлов

Каждый генерируемый файл содержит маркер:
- XML-файлы: `<!-- UNIBRIDGE_RUSTORE_GENERATED -->`, `<!-- UNIBRIDGE_UNIBRIDGESHARE_GENERATED -->`
- Gradle/Java: `// UNIBRIDGE_UNIBRIDGESHARE_GENERATED`

Перед перезаписью и перед удалением **всегда проверяй маркер**:

```csharp
if (File.Exists(path) && !File.ReadAllText(path).Contains(ScanMarker))
{
    Debug.LogWarning("[UniBridge] Пропуск — файл не создан UniBridge.");
    return;
}
```

### Почему НЕ `Assets/Plugins/Android/AndroidManifest.xml`

Этот файл конфликтует с:
- **Yandex SDK** — их `AndroidManifestPostprocessor` крашится с `NullReferenceException`, если в манифесте нет `UnityPlayerActivity`
- **RuStoreAndroidConfigurator** — трактует этот путь как legacy-локацию

Для инъекций в AndroidManifest использовать `IPostGenerateGradleAndroidProject` (пример: `UniBridgeShareAndroidManifestInjector`, `callbackOrder=100`). Такой хук запускается после генерации полного Gradle-проекта и вставляет изменения напрямую в `unityLibrary/src/main/AndroidManifest.xml`.

### Создание нового конфигуратора Android

1. Создать класс в `Assets/UniBridge/Editor/BuildManager/` с методами `Configure()` и `Cleanup()`
2. Файлы создавать в `Assets/Plugins/Android/UniBridgeMobileKit/NewFeature/`
3. Добавлять маркер в каждый генерируемый файл
4. Вызывать из `StoreAndroidConfigurator.OnStoreChanged()`
5. Добавить safety-net вызов в `BuildPreprocessor.OnPreprocessBuild()` для свежих установок

### Паттерн EnsureAndroidLibBuildGradle

Если SDK требует `.androidlib`-директорию — обязательно создавать в ней `build.gradle`. Без него Unity Gradle-система игнорирует директорию. Вызывать из `BuildPreprocessor`:

```csharp
if (report.summary.platform == BuildTarget.Android)
    RuStoreAndroidConfigurator.EnsureAndroidLibBuildGradle();
```

---

## 9. Checklist система

### Интерфейс

```csharp
public interface IChecklistProvider
{
    string Title { get; }
    ChecklistItem[] GetItems();
}

// ChecklistItem поля:
//   string Label     — отображаемое описание шага
//   bool   Ok        — true → зелёная галочка; false → красный крест
//   string Hint      — что делать, если Ok == false
//   bool   IsOptional — не блокирует сборку, отображается отдельно
```

### Регистрация в ChecklistRegistry.cs

```csharp
// Привязан к SDK — отображается когда SDK установлен для выбранного стора:
_sdkChecklists["UNIBRIDGE_NEWSDK"] = new NewSdkChecklist();

// Привязан к стору — отображается при выборе стора, независимо от SDK:
_storeChecklists[StorePlatformDefines.STORE_GOOGLEPLAY] = new SomeChecklist();
```

Чеклист, возвращающий пустой массив — не отображается в UI.

### Создание нового чеклиста

Файл: `Assets/UniBridge/Editor/BuildManager/Checklists/NewSdkChecklist.cs`

```csharp
public class NewSdkChecklist : IChecklistProvider
{
    public string Title => "New SDK";

    public ChecklistItem[] GetItems()
    {
        bool configured = !string.IsNullOrEmpty(GetAdUnitId());
        return new[]
        {
            new ChecklistItem("Ad Unit ID заполнен", configured, "Укажи Ad Unit ID в UniBridge > Settings"),
        };
    }
}
```

---

## 10. Settings Drawers

Интерфейс `ISettingsDrawer` с методом `DrawInspector()`. Живёт в `Assets/UniBridge/Editor/Drawers/` (для рекламных SDK) или `Assets/UniBridge/Editor/Purchases/Drawers/` (для purchase SDK).

```csharp
#if UNIBRIDGE_NEWSDK
public class NewSdkSettingsDrawer : EmptySettingsDrawer
{
    private readonly UniBridgeConfig _config;
    public NewSdkSettingsDrawer(UniBridgeConfig config) { _config = config; }

    protected override void OnDrawInspector()
    {
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField("New SDK Settings", EditorStyles.boldLabel);
        _config.NewSdkSettings.AdUnitId =
            EditorGUILayout.TextField("Ad Unit ID", _config.NewSdkSettings.AdUnitId);
        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(_config);
    }
}
#endif
```

После создания drawer — добавить его инстанциирование и вызов `DrawInspector()` в нужной вкладке окна настроек.

---

## 11. Демо сцена

`Assets/Demo/DemoScene.unity` — тестовая сцена с полным UI для всех систем.

**`DemoMainUI.cs`** — главный контроллер (UI Toolkit). Вкладки: Ads, Purchases, Leaderboards, Rate, Share.
**`DemoUniBridgeUI.cs`** — упрощённый контроллер только для рекламы.

Вкладка Share защищена `#if UNIBRIDGESHARE_ENABLED`. При добавлении новой опциональной системы — аналогично оборачивать весь код работы с ней.

При добавлении новой системы в демо:
1. Добавить вкладку в `DemoMainUI.uxml`
2. Добавить `#if NEW_SYSTEM_ENABLED` вокруг полей и обработчиков в `DemoMainUI.cs`
3. Подписаться на события новой системы в `RegisterEventHandlers()`

---

## 12. Опциональные системы (паттерн UNIBRIDGESHARE_ENABLED)

Система, которая должна компилироваться только при установленном define:

1. **Все asmdef системы**: `"defineConstraints": ["MY_SYSTEM_ENABLED"]`
2. **Editor-код** использующий типы системы: обёртка в `#if MY_SYSTEM_ENABLED`
3. **Build Manager**: тогл → `ScriptingDefinesManager.AddDefine/RemoveDefine("MY_SYSTEM_ENABLED")`
4. **Запись конфига** через `SerializedObject` + `System.Type.GetType("TypeName, AssemblyName")`:

```csharp
// НЕ делай: var config = Resources.Load<MySystemConfig>(...) — тип недоступен без define
// Делай так:
var configType = Type.GetType("MyNamespace.MySystemConfig, MySystem.Runtime");
if (configType == null) return; // ещё не скомпилировано
var config = ScriptableObject.CreateInstance(configType);
var so = new SerializedObject(config);
so.FindProperty("PreferredAdapter").stringValue = adapter;
so.ApplyModifiedProperties();
```

5. **После recompile** — используй `SessionState` + `[DidReloadScripts]` для отложенной записи:

```csharp
// До recompile: сохранить нужные данные
SessionState.SetString("MySystem_PendingAdapter", adapter);
ScriptingDefinesManager.AddDefine("MY_SYSTEM_ENABLED");

// После recompile:
[DidReloadScripts]
private static void OnScriptsReloaded()
{
    var pending = SessionState.GetString("MySystem_PendingAdapter", null);
    if (pending == null) return;
    SessionState.EraseString("MySystem_PendingAdapter");
    ApplyConfig(pending);
}
```

6. **Авто-генерированные файлы** добавить в `.gitignore`

---

## 13. Retry-паттерн для рекламных оберток

Правильный паттерн с `_retryPending` для предотвращения дублирования таймеров:

```csharp
private bool _isLoading;
private bool _retryPending;

public void LoadAd()
{
    // _retryPending в гварде — не вызывать Load пока запланирован retry
    if (_isLoaded || _isLoading || _retryPending) return;
    TryLoadOnce();
}

private void HandleAdFailedToLoad()
{
    _isLoading = false;
    _retryPending = true;                          // ОБЯЗАТЕЛЬНО ДО InvokeAfter
    RetryHelper.InvokeAfter(delay, RetryLoad);
}

private void RetryLoad()
{
    _retryPending = false;                         // ОБЯЗАТЕЛЬНО ПЕРВЫМ
    if (_isLoading) return;
    TryLoadOnce();
}
```

**Почему именно такой порядок:**
- `_retryPending = true` ДО `InvokeAfter` — исключает race condition, если коллбэк выполнится синхронно
- `_retryPending = false` ПЕРВЫМ в `RetryLoad` — чтобы `LoadAd()` мог быть вызван снаружи без блокировки после завершения retry

---

## 14. Чеклисты добавления новой сущности

### A. Новый рекламный адаптер

- [ ] Реализовать `IAdSource`
- [ ] Регистрация: `AdSourceRegistry.Register("UNIBRIDGE_NEWSDK", factory, priority)` на **`SubsystemRegistration`**; `#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR`
- [ ] Создать `UniBridge.NewSdk.asmdef` с `defineConstraints: ["UNIBRIDGE_NEWSDK"]`
- [ ] `AdapterDefines.AdsAdapterNames` + `GetAdAdapters(buildTarget)`
- [ ] `AdapterLinkXmlGenerator.AdapterAssemblies`: `{ "UNIBRIDGE_NEWSDK", "UniBridge.NewSdk" }`
- [ ] `StorePresets.json` + `StorePresetsManager.CreateDefaults()` — обновить нужные пресеты
- [ ] `NewSdkSettingsDrawer : EmptySettingsDrawer` в `Editor/Drawers/`
- [ ] Настройки `NewSdkSettings` в `UniBridgeConfig`
- [ ] `SDKVersions.json` — запись SDK
- [ ] `SDKVersionChecker.cs` — детектирование пакета → `AddDefine/RemoveDefine`

### B. Новый адаптер покупок

- [ ] Реализовать `IPurchaseSource` с `_ownershipCache` (синхронный `IsPurchased()`)
- [ ] `PurchaseSourceRegistry.Register(factory, priority)` на **`BeforeSceneLoad`**
- [ ] Создать asmdef
- [ ] `AdapterDefines.PurchaseAdapterNames` + `GetPurchaseAdapters(storeDefine)`
- [ ] `AdapterLinkXmlGenerator.AdapterAssemblies`
- [ ] `StorePresets.json` + `StorePresetsManager`
- [ ] Drawer в `Editor/Purchases/Drawers/`; настройки в `UniBridgePurchasesConfig`
- [ ] В `RestorePurchases`: `OnPurchaseSuccess?.Invoke(PurchaseResult.FromRestored(id))` для каждого owned non-consumable; для iOS UnityIAP — только diff (платформа переигрывает через обычный callback)

### C. Новый адаптер лидербордов

- [ ] Реализовать `ILeaderboardSource` (`Initialize`, `SubmitScore`, `GetEntries`, `GetPlayerEntry`)
- [ ] `LeaderboardSourceRegistry.Register("SDK_DEFINE", factory, priority)` на **`BeforeSceneLoad`**
- [ ] Если виртуальный ключ: обработать в `LeaderboardSourceBuilder.Build()` до реестра + `IsAdapterSdkInstalled()` → `true`
- [ ] `AdapterDefines.LeaderboardAdapterNames` + `GetLeaderboardAdapters(storeDefine)`
- [ ] `AdapterLinkXmlGenerator.AdapterAssemblies` (если не виртуальный)
- [ ] `StorePresets.json` + `StorePresetsManager`

### D. Новый адаптер оценок

- [ ] Реализовать `IRateSource`; `IsSupported = false` для платформ без поддержки
- [ ] `RateSourceRegistry.Register("SDK_DEFINE", factory, priority)` на **`BeforeSceneLoad`**
- [ ] Если виртуальный ключ: обработать в `RateSourceBuilder.Build()` до реестра + `IsAdapterSdkInstalled()` → `true`
- [ ] `AdapterDefines.RateAdapterNames` + `GetRateAdapters(storeDefine)`
- [ ] `AdapterLinkXmlGenerator.AdapterAssemblies` (если не виртуальный)
- [ ] `StorePresets.json` + `StorePresetsManager`

### E. Новый адаптер шаринга

- [ ] Реализовать `IShareSource`; `IsSupported = false` если платформа не поддерживает
- [ ] Share не использует реестр для платформенных адаптеров — добавить ветку в `ShareSourceBuilder.Build()` через `#if`
- [ ] `AdapterDefines.ShareAdapterNames` + `GetShareAdapters(storeDefine)`
- [ ] `IsAdapterSdkInstalled()` → `true`
- [ ] `StorePresets.json` + `StorePresetsManager`
- [ ] Android-специфика → `UniBridgeShareAndroidConfigurator`; инъекция в manifest → `IPostGenerateGradleAndroidProject`

### F. Новый стор

- [ ] Константа в `StorePlatformDefines`: `public const string STORE_MYSTORE = "UNIBRIDGE_STORE_MYSTORE";`
- [ ] Пресет в `StorePresets.json` + `StorePresetsManager.CreateDefaults()`
- [ ] Маппинг BuildTarget в `GetExpectedBuildTarget(define)`
- [ ] Ветки в `GetAdAdapters/GetPurchaseAdapters/GetLeaderboardAdapters/GetRateAdapters/GetShareAdapters` в `AdapterDefines`
- [ ] Если Android: ветка в `StoreAndroidConfigurator.IsAndroidStore()`

### G. Новый SDK в SDK Installer

- [ ] Запись в `SDKVersions.json`:
  ```json
  "newSdk": {
      "version": "1.0.0",
      "packageId": "com.newsdk.unity",
      "define": "UNIBRIDGE_NEWSDK",
      "displayName": "New SDK",
      "checkMode": "define"
  }
  ```
  Поля `gitUrl`, `registryUrl`, `registryScopes` — при необходимости.
  `checkMode: "define"` — проверять по scripting define; `"package"` — по manifest.json (для SDK без define).

- [ ] Детектирование в `SDKVersionChecker.OnListComplete()` → `ScriptingDefinesManager.AddDefine/RemoveDefine`

### H. Новый чеклист

- [ ] Создать `NewChecklist : IChecklistProvider` в `Editor/BuildManager/Checklists/`
- [ ] Зарегистрировать в `ChecklistRegistry._sdkChecklists` или `_storeChecklists`

### I. Полностью новая система (новый Rat*)

- [ ] `INewSource`: `IsInitialized`, `IsSupported`, `Initialize(config, ok, fail)`, специфичные методы
- [ ] `NewSourceRegistry`: Dictionary + `Register(key, factory, priority)` + `Create(config)`
- [ ] `NewSourceBuilder`: `#if UNITY_EDITOR` → Debug; рантайм → реестр → fallback
- [ ] `UniBridgeNew.cs`: статический фасад, `AutoInitialize` на `BeforeSceneLoad`, гвард `if (IsInitialized) return;`
- [ ] `UniBridgeNewConfig : ScriptableObject`: `AutoInitialize`, `PreferredNewAdapter`, SDK-настройки
- [ ] Адаптеры: Debug (редактор), Mock (заглушка, IsSupported=false), платформенные
- [ ] asmdef: `UniBridgeNew.Runtime`, `UniBridgeNew.Editor`, адаптерные
- [ ] Редактор: вкладка в Settings, drawers, `AdapterDefines.NewAdapterNames`, `GetNewAdapters()`, запись `PreferredNewAdapter` из `OnSelectClicked` в Build Manager
- [ ] `AdapterLinkXmlGenerator.AdapterAssemblies` для каждого не-виртуального адаптера
- [ ] Демо-сцена: вкладка в `DemoMainUI.cs`, защита `#if` если опциональная система

---

## 15. Quick Reference: карта ключевых файлов

| Задача | Файл |
|--------|------|
| Добавить define→name, список адаптеров по платформе | `Editor/BuildManager/AdapterDefines.cs` |
| Добавить защиту от IL2CPP stripping | `Editor/BuildManager/AdapterLinkXmlGenerator.cs` |
| Обновить store presets (код) | `Editor/BuildManager/StorePresetsManager.cs` |
| Обновить store presets (данные) | `Editor/BuildManager/StorePresets.json` |
| Добавить виртуальный ключ как "всегда установлен" | `Editor/BuildManager/UniBridgeBuildManagerUIWindow.cs` → `IsAdapterSdkInstalled()` |
| Обработать виртуальный ключ в рантайме | `Runtime/*/XxxSourceBuilder.cs` |
| Добавить Android-конфигуратор | `Editor/BuildManager/StoreAndroidConfigurator.cs` (диспетчер) |
| Инъекция в AndroidManifest при Gradle-сборке | `Editor/BuildManager/UniBridgeShareAndroidManifestInjector.cs` (образец) |
| Зарегистрировать чеклист | `Editor/BuildManager/ChecklistRegistry.cs` |
| Добавить новый SDK в Installer | `Editor/SDKVersions.json` + `Editor/SDKVersionChecker.cs` |
| Добавить drawer настроек рекламы | `Editor/Drawers/` |
| Добавить drawer настроек покупок | `Editor/Purchases/Drawers/` |
| Интерфейс конкретной системы | `Runtime/I*Source.cs` |
| Реестр адаптеров | `Runtime/*/XxxSourceRegistry.cs` |

---

## 16. Подводные камни

1. **`UniBridge.Leaderboards.Runtime` невидим в Editor** — несмотря на `autoReferenced: true`, для editor-сборок нужна явная запись в `references` у `UniBridge.Editor.asmdef`

2. **Виртуальный ключ не добавлен в `IsAdapterSdkInstalled()`** — Build Manager показывает адаптер как "(не установлен)", хотя он всегда доступен

3. **Адаптер не в `AdapterLinkXmlGenerator.AdapterAssemblies`** — в IL2CPP-релизной сборке сборка вырезается, Runtime откатывается на Debug-адаптер

4. **Рекламный адаптер регистрируется на `BeforeSceneLoad`** — порядок не гарантирован, `AdSourceRegistry` может быть пуст в момент `UniBridge.AutoInitialize`

5. **Неверный порядок `_retryPending`** — `_retryPending = true` должен быть ДО вызова `InvokeAfter`; `_retryPending = false` — ПЕРВЫМ в `RetryLoad()`

6. **Запись в `Assets/Plugins/Android/AndroidManifest.xml`** — конфликтует с Yandex SDK и RuStoreAndroidConfigurator; использовать `IPostGenerateGradleAndroidProject` вместо этого

7. **Авто-ген файлы без маркера** — при `Cleanup` можно случайно удалить файл пользователя; всегда проверяй маркер перед перезаписью и удалением

8. **Прямая ссылка на тип опциональной системы в Editor-коде** — тип недоступен до компиляции нужного define; использовать `System.Type.GetType("TypeName, AssemblyName")` + `SerializedObject`

9. **Забыт `"UNIBRIDGE_NONE"` в `GetAdAdapters()`** — нельзя отключить систему через Build Manager

10. **`.androidlib` без `build.gradle`** — Unity не обрабатывает директорию как Gradle-субпроект; всегда создавать `build.gradle` и вызывать `EnsureAndroidLibBuildGradle()` из BuildPreprocessor
