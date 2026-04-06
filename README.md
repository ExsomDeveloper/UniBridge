# UniBridge

Universal integration package for Unity providing unified APIs for ads, purchases, leaderboards, saves, ratings, sharing, analytics, and authentication across multiple platforms.

**Supported platforms:** Android (Google Play, RuStore), iOS (App Store), WebGL (Playgama)

## Installation

### Unity Package Manager (UPM)

Open **Window > Package Manager**, click **+** > **Add package from git URL...** and enter:

```
https://github.com/ExsomDeveloper/UniBridge.git?path=Assets/UniBridge
```

To install a specific version, append `#v<version>`:

```
https://github.com/ExsomDeveloper/UniBridge.git?path=Assets/UniBridge#v1.3.34
```

### manifest.json

Alternatively, add the following line to your `Packages/manifest.json` under `"dependencies"`:

```json
"com.unibridge.core": "https://github.com/ExsomDeveloper/UniBridge.git?path=Assets/UniBridge"
```

## Requirements

- Unity 2021.3 or newer

## Getting Started

1. Install the package via UPM
2. Open **UniBridge > Build Manager** and select your target store
3. Open **UniBridge > SDK Installer** to install required SDKs for the selected store
4. Configure settings in **UniBridge > Settings**

## Subsystems

| Subsystem | Description |
|-----------|-------------|
| **Ads** | LevelPlay, Yandex Mobile Ads, Playgama, Debug mock |
| **Purchases** | Unity IAP, RuStore Pay, Playgama, Debug mock |
| **Leaderboards** | GPGS, Game Center, Playgama, Simulated, Debug mock |
| **Saves** | GPGS Saved Games, iCloud KV Store, Playgama, Simulated, Local |
| **Rate** | Google Play Review, RuStore Review, App Store Review, Mock |
| **Share** | Android native, iOS native, Playgama, Mock |
| **Analytics** | AppMetrica, Debug mock |
| **Auth** | Game Center, Playgama, Mock |

## License

See [LICENSE](LICENSE) for details.
