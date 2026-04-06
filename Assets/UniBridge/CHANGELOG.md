# Changelog

All notable changes to the UniBridge package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2024-02-04

### Added
- Initial release of UniBridge universal advertisement package
- Support for LevelPlay (IronSource) on Android/iOS
- Support for Playgama Bridge on WebGL
- Automatic platform detection and adapter selection
- Debug adapter for Editor testing
- Interstitial timer system with pause/resume/reset
- COPPA compliance settings (young mode)
- Persistent ad disable functionality
- Custom inspector for UniBridgeConfig
- SDK Updater editor window
- Comprehensive documentation

### Features
- `UniBridge.Initialize()` - Initialize the ad system
- `UniBridge.ShowInterstitial()` - Show interstitial ads
- `UniBridge.ShowReward()` - Show rewarded ads
- `UniBridge.ShowBanner()` / `HideBanner()` - Banner management
- `UniBridge.DisableAds()` - Disable ads permanently
- Interstitial timer with configurable intervals
- Event system for ad lifecycle callbacks

### Supported Platforms
- Android (via LevelPlay)
- iOS (via LevelPlay)
- WebGL (via Playgama)
- Unity Editor (Debug mode)
