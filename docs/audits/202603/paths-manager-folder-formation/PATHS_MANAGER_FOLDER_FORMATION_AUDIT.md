# PathsManager Folder Path Formation Audit

**Date:** 2026-03-01  
**Scope:** Entire XerahS codebase (desktop, platform, CLI, tools)  
**Goal:** Identify places that build folder paths manually instead of using `PathsManager` (or `SettingsManager` where it delegates to PathsManager), so all app-related folder paths are formed in one place.

---

## PathsManager API (canonical)

Defined in `src/desktop/core/XerahS.Common/PathsManager.cs`:

| Member | Purpose |
|--------|--------|
| `PersonalFolder` | Base app folder (e.g. MyDocuments/XerahS) |
| `ScreenshotsFolder` | PersonalFolder/Screenshots |
| `ScreencastsFolder` | PersonalFolder/Screencasts |
| `FrameDumpsFolder` | ScreencastsFolder/FrameDumps |
| `LogsFolderBase` | PersonalFolder/Logs |
| `GetLogsFolderForMonth(DateTime?)` | LogsFolderBase/yyyy-MM |
| `GetMainLogFilePath()` | Main log file path for today |
| `GetErrorLogFilePath()` | Error log file path for today |
| `SettingsFolder` | PersonalFolder/Settings |
| `HistoryFolder` | PersonalFolder/History |
| `BackupFolder` | SettingsFolder/Backup |
| `HistoryBackupFolder` | HistoryFolder/Backup |
| `ToolsFolder` | PersonalFolder/Tools |
| `ToolsArchitectureFolder` | ToolsFolder/win-arm64, macos64, etc. |
| `PluginsFolder` | App BaseDirectory/Plugins (DEBUG) or PersonalFolder/Plugins |
| `GetPluginDirectories()` | Returns app Plugins + user Plugins paths |

`SettingsManager` (Core) exposes `PersonalFolder` and `SettingsFolder` as delegates to PathsManager; using those is equivalent to PathsManager for folder formation.

---

## Findings

### 1. **Should use PathsManager / AppResources (recommended fix)**

| File | Line(s) | Current | Recommendation |
|------|---------|--------|-----------------|
| **PluginInstallerViewModel.cs** | 128 | `Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins")` | Use same pattern as PathsManager: `Path.Combine(AppContext.BaseDirectory, AppResources.PluginsFolderName)` or add `PathsManager.GetAppBundledPluginsDirectory()` if desired. Ensures "Plugins" name is not hardcoded. |
| **SettingsViewModel.cs** | 329 | `Path.Combine(Environment.GetFolderPath(MyPictures), "ShareX")` (ResetToDefaults) | Use `PathsManager.ScreenshotsFolder` for default, or at least `AppResources.AppName` instead of hardcoded "ShareX". |
| **PathsManager.cs** (GetFFmpegPath) | 215–216 | `Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "ffmpeg.exe")` | Prefer `Path.Combine(PathsManager.ToolsFolder, ...)` for user Tools, or keep BaseDirectory for app-bundled tools; consider a single helper e.g. `GetAppToolsDirectory()` for BaseDirectory/Tools. |

### 2. **Custom subpaths under PersonalFolder (consider PathsManager extension)**

| File | Line(s) | Current | Recommendation |
|------|---------|--------|-----------------|
| **TroubleshootingHelper.cs** | 178 | `Path.Combine(SettingsManager.PersonalFolder, "Troubleshooting")` | Optional: add `PathsManager.TroubleshootingFolder` (e.g. `PersonalFolder/Troubleshooting`) and use it here so the name is centralised. |
| **VerifyRegionCaptureCommand.cs** | 141 | `Path.Combine(PathsManager.PersonalFolder, "CaptureTroubleshooting", "RegionVerify")` | Optional: add e.g. `PathsManager.CaptureTroubleshootingFolder` and subfolders if these become reused. |
| **VerifyRecordingCommand.cs** | 139 | `Path.Combine(PathsManager.PersonalFolder, "CaptureTroubleshooting", "RecordingVerify")` | Same as above. |

### 3. **Intentional / legacy / system paths (no change or document only)**

| File | Line(s) | Current | Note |
|------|---------|--------|------|
| **UploadersConfigImporter.cs** | 42, 49 | `Path.Combine(MyDocuments, "ShareX")`, `DefaultShareXConfigPath + "UploadersConfig.json"` | **Legacy ShareX** config location; not XerahS app path. Intentionally different (migration from ShareX). Leave as-is or document. |
| **UploadersConfigImporter.cs** | 56 | `Path.Combine(BaseDirectory, "UploadersConfig.json")` | Portable/config-next-to-exe; not a PathsManager folder. OK. |
| **PortalScreenshotFallback.cs** | 135 | `Path.Combine(picturesDir, "Screenshots")` | User’s system Pictures/Screenshots (Linux portal); not app folder. OK. |
| **LinuxStartupService.cs** | 46 | `Path.Combine(autostartFolder, AppResources.AppName + ".desktop")` | XDG autostart path; uses AppResources.AppName. OK. |
| **UpdateService.cs** | 279 | `Path.Combine(BaseDirectory, "portable.txt")` | Portable marker file; not a folder. OK. |
| **NameParser.cs** | 357 | `Path.Combine(BaseDirectory, "Resources", fileName)` | App resources; not PathsManager scope. OK. |
| **FileHelpers.cs** | 297 | `Path.Combine(BaseDirectory, path)` | Generic resolution; path is argument. OK. |
| **FFmpegRecordingService.cs** | 91 | String format listing checked paths | Error message only; not folder creation. OK. |
| **MacOSScreenCaptureKitService.cs** | 66 | `Path.Combine(BaseDirectory, libraryFileName)` | Native library load path. OK. |
| **WatchFolderDaemonServiceBase.cs** | 90 | `processDirectory = AppContext.BaseDirectory` | Daemon process working directory. OK. |

### 4. **Already using PathsManager or SettingsManager correctly**

- **DebugViewModel.cs** – `PathsManager.LogsFolderBase`, `SettingsManager.SettingsFolder`
- **WatchFolder.Daemon Program.cs** – `PathsManager.GetLogsFolderForMonth()`
- **Logger.cs** – `PathsManager.GetLogsFolderForMonth()` for rotation
- **ShareXBootstrap.cs**, **Program.cs** – `PathsManager.GetMainLogFilePath()`, `PathsManager.GetLogsFolderForMonth()`
- **InstanceManager.cs** – `PathsManager.SettingsFolder`
- **UploadQueueService.cs** – `Path.Combine(SettingsManager.SettingsFolder, QueueFileName)` (SettingsFolder is PathsManager-backed)
- **WaylandPortalRecordingService.cs** – `PathsManager.ScreencastsFolder`
- **CaptureJobProcessor.cs** – `PathsManager.PluginsFolder`
- **UploaderInstanceViewModel.cs** – `Path.Combine(PathsManager.PluginsFolder, ProviderId)`
- **PathsManager.GetPluginDirectories()** – uses `AppContext.BaseDirectory` + `AppResources.PluginsFolderName` and `PluginsFolder` (canonical)

---

## Summary

| Category | Count |
|----------|--------|
| Should use PathsManager/AppResources | 3 |
| Optional PathsManager extension | 3 |
| Intentional/legacy/system (no change) | 10+ |
| Already correct | 10+ |

**Recommended next steps**

1. **PluginInstallerViewModel.cs:** Replace hardcoded `"Plugins"` with `AppResources.PluginsFolderName` (and `AppContext.BaseDirectory`) so it matches PathsManager/GetPluginDirectories.
2. **SettingsViewModel.cs (ResetToDefaults):** Use `PathsManager.ScreenshotsFolder` or at least `AppResources.AppName` instead of `"ShareX"`.
3. **PathsManager GetFFmpegPath:** Optionally introduce an app-tools path helper and use it for BaseDirectory/Tools so "Tools" is not duplicated.
4. Optionally add `TroubleshootingFolder` (and optionally CaptureTroubleshooting subfolders) to PathsManager and switch TroubleshootingHelper and CLI verify commands to use them.

---

*Audit produced by scanning for `Path.Combine`, `Environment.GetFolderPath`, `PersonalFolder`, `SettingsFolder`, `Logs`, `Plugins`, `Tools`, `Screenshots`, and related symbols across the repo.*
