# XIP0043: Remove Windows Forms dependency and harden cross-RID daemon bundling

## Summary

This plan removes the `System.Windows.Forms` dependency from the Windows platform layer and makes daemon packaging deterministic across all six desktop release combinations:

1. `win-x64`
2. `win-arm64`
3. `linux-x64`
4. `linux-arm64`
5. `osx-x64`
6. `osx-arm64`

## Current Findings (Evidence)

1. Windows Forms is explicitly enabled in [src/platform/XerahS.Platform.Windows/XerahS.Platform.Windows.csproj](/c:/Users/liveu/source/repos/ShareX Team/XerahS/src/platform/XerahS.Platform.Windows/XerahS.Platform.Windows.csproj:9) via `<UseWindowsForms>true</UseWindowsForms>`.
2. WinForms APIs are used in [src/platform/XerahS.Platform.Windows/WindowsScreenService.cs](/c:/Users/liveu/source/repos/ShareX Team/XerahS/src/platform/XerahS.Platform.Windows/WindowsScreenService.cs:32) and [src/platform/XerahS.Platform.Windows/Capture/GdiCaptureStrategy.cs](/c:/Users/liveu/source/repos/ShareX Team/XerahS/src/platform/XerahS.Platform.Windows/Capture/GdiCaptureStrategy.cs:49).
3. Windows publish output contains `System.Windows.Forms.*` assemblies and they materially contribute to package size.
4. macOS release archive includes daemon under `XerahS.app/Contents/MacOS/xerahs-watchfolder-daemon`.
5. Linux v0.18.9 release tarballs contain `xerahs-watchfolder-daemon.runtimeconfig.json` but miss the daemon executable.
6. CI logs for v0.18.9 show `PublishWatchFolderDaemon` publishing daemon to daemon project path (`.../XerahS.WatchFolder.Daemon/bin/.../publish/`) instead of app publish dir, confirming pipeline copy gap.

## Goals

1. Eliminate WinForms dependency from app/runtime artifacts.
2. Keep existing monitor/screen behavior stable (bounds, working area, primary monitor, active monitor, DPI scale).
3. Ensure daemon executable is bundled for macOS and Linux (and Windows installer remains correct).
4. Add strict packaging validation in CI so missing daemon/arch mismatches fail build before release.

## Non-Goals

1. No UI/feature changes unrelated to screen/monitor APIs.
2. No daemon protocol/behavior redesign.
3. No changes to updater UX in this XIP (only packaging correctness and asset integrity checks).

## Workstream A: Remove Windows Forms from Windows platform

### A1. Introduce Win32 monitor enumeration service (no WinForms)

Implement `WindowsDisplayEnumeration` (new internal helper) using `user32` APIs:

1. `EnumDisplayMonitors`
2. `GetMonitorInfo` / `MONITORINFOEX`
3. `MonitorFromPoint`
4. `MonitorFromRect`
5. `GetCursorPos`
6. `GetSystemMetrics` (`SM_XVIRTUALSCREEN`, `SM_YVIRTUALSCREEN`, `SM_CXVIRTUALSCREEN`, `SM_CYVIRTUALSCREEN`)
7. Existing DPI helper (`GetDpiForMonitor`) from current native methods

### A2. Replace WinForms usages

Refactor these files to Win32-backed logic:

1. [src/platform/XerahS.Platform.Windows/WindowsScreenService.cs](/c:/Users/liveu/source/repos/ShareX Team/XerahS/src/platform/XerahS.Platform.Windows/WindowsScreenService.cs)
2. [src/platform/XerahS.Platform.Windows/Capture/GdiCaptureStrategy.cs](/c:/Users/liveu/source/repos/ShareX Team/XerahS/src/platform/XerahS.Platform.Windows/Capture/GdiCaptureStrategy.cs)

Expected mapping:

1. `Screen.AllScreens` -> enumerated monitor list from `EnumDisplayMonitors`
2. `Screen.FromPoint` -> `MonitorFromPoint` + lookup
3. `Screen.FromRectangle` -> `MonitorFromRect` + lookup
4. `SystemInformation.VirtualScreen` -> `GetSystemMetrics` virtual screen rectangle
5. `Cursor.Position` -> `GetCursorPos`

### A3. Remove project-level WinForms enablement

In [src/platform/XerahS.Platform.Windows/XerahS.Platform.Windows.csproj](/c:/Users/liveu/source/repos/ShareX Team/XerahS/src/platform/XerahS.Platform.Windows/XerahS.Platform.Windows.csproj), remove:

1. `<UseWindowsForms ...>true</UseWindowsForms>`

Then confirm no remaining `System.Windows.Forms` usage via repo grep.

### A4. Verification for A

1. `dotnet build` must pass.
2. Windows publish artifacts for `win-x64` and `win-arm64` must not contain `System.Windows.Forms.dll`, `System.Windows.Forms.Design.dll`, `System.Windows.Forms.Primitives.dll`.
3. Manual smoke: multi-monitor (including mixed DPI) active screen detection and region capture still work.

## Workstream B: Fix daemon bundling for Linux/macOS/Windows

### B1. Make daemon copy explicit and deterministic

In [src/desktop/app/XerahS.App/XerahS.App.csproj](/c:/Users/liveu/source/repos/ShareX Team/XerahS/src/desktop/app/XerahS.App/XerahS.App.csproj):

1. Keep `PublishWatchFolderDaemon`, but publish daemon to an explicit temp staging dir (e.g. `$(IntermediateOutputPath)watchfolder-daemon-publish/$(RuntimeIdentifier)/`).
2. After daemon publish, copy expected daemon outputs from staging dir into app `$(PublishDir)`.
3. Do not rely on inferred `PublishDir` propagation into nested `MSBuild` calls.

### B2. Add RID-specific required-file assertions

Add a post-publish validation target (same csproj) that fails build if required daemon executable is missing:

1. Windows RIDs: `xerahs-watchfolder-daemon.exe`
2. Linux/macOS RIDs: `xerahs-watchfolder-daemon`

Also validate `runtimeconfig.json` presence.

### B3. Ensure packaging scripts fail fast on missing daemon

Add checks in packaging scripts before archive generation:

1. [build/linux/package-linux.sh](/c:/Users/liveu/source/repos/ShareX Team/XerahS/build/linux/package-linux.sh)
2. [build/macos/package-mac.sh](/c:/Users/liveu/source/repos/ShareX Team/XerahS/build/macos/package-mac.sh)
3. [build/windows/package-windows.ps1](/c:/Users/liveu/source/repos/ShareX Team/XerahS/build/windows/package-windows.ps1)

If expected daemon file is missing, stop with non-zero exit.

### B4. Release artifact content validation step

In [ .github/workflows/release-build-all-platforms.yml ](/c:/Users/liveu/source/repos/ShareX Team/XerahS/.github/workflows/release-build-all-platforms.yml):

1. Add a validation step per job to inspect produced archive and assert daemon presence.
2. Linux: `tar -tzf` contains `xerahs-watchfolder-daemon`.
3. macOS: tar contains `XerahS.app/Contents/MacOS/xerahs-watchfolder-daemon`.
4. Windows: installer payload check already exists via Inno files; add publish-dir precheck for daemon exe.

## Workstream C: Cross-RID architecture/OS integrity checks

Add deterministic checks to prevent wrong asset selection/regressions:

1. Build metadata manifest per artifact (`rid`, `os`, `arch`, filename).
2. CI assertion that each produced artifact filename and RID pair are consistent.
3. Optional: updater unit test/table asserting each client RID maps only to same OS+arch asset.

## Test Matrix (must pass)

1. `dotnet publish` for `win-x64` -> daemon exe present; no WinForms assemblies.
2. `dotnet publish` for `win-arm64` -> daemon exe present; no WinForms assemblies.
3. `dotnet publish` for `linux-x64` -> daemon binary present in publish dir and `.tar.gz`.
4. `dotnet publish` for `linux-arm64` -> daemon binary present in publish dir and `.tar.gz`.
5. `dotnet publish` for `osx-x64` -> daemon binary inside `.app/Contents/MacOS/` and archive.
6. `dotnet publish` for `osx-arm64` -> daemon binary inside `.app/Contents/MacOS/` and archive.

## Rollout Sequence

1. Implement Workstream B first (packaging correctness and CI guards).
2. Ship one patch release confirming Linux/macOS daemon packaging integrity.
3. Implement Workstream A (WinForms removal) behind full publish matrix validation.
4. Run size comparison report pre/post (Windows artifacts and largest-file delta).

## Risks and Mitigations

1. Risk: Win32 monitor enumeration diverges from current WinForms behavior.
   Mitigation: parity tests for monitor bounds/working area/primary flags on multi-monitor setups.
2. Risk: daemon publish path behavior differs across SDK versions.
   Mitigation: explicit staging folder + explicit copy + required-file assertions.
3. Risk: silent archive regressions in future releases.
   Mitigation: archive-content validation in CI before upload.

## Acceptance Criteria

1. No `System.Windows.Forms.*` in Windows publish output.
2. Linux and macOS release archives include daemon executable in expected paths.
3. All six RID combinations validated in CI and failures block release.
4. `dotnet build` remains green with warnings treated as errors.
