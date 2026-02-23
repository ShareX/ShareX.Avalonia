# XerahS - Project Status and Roadmap

## Current Status (Updated 2026-02-23)

Core desktop workflow (capture -> annotate/edit -> save/copy/upload) is implemented and active.

This roadmap has been reconciled against the current codebase, including the `ImageEditor` submodule (`ShareX.ImageEditor`) and current release/build automation.

## Completed Highlights

- [x] Reimagined desktop UI and workflow-based capture pipeline
- [x] Region capture annotation overlay backed by `ShareX.ImageEditor` types
- [x] Annotation toolset and image effects integrated through the editor submodule
- [x] Smart Eraser drawing path and color sampling logic implemented
- [x] Export flow: copy to clipboard, save, save-as, and upload-to-host task routing
- [x] Dynamic uploader/plugin architecture with provider catalog and instance manager
- [x] File-type routing for upload destinations
- [x] System tray integration with recording-aware state
- [x] Multi-platform release packaging workflow (Windows, Linux x64/arm64, macOS x64/arm64)
- [x] App icon assets and packaging scripts for desktop platforms

## ImageEditor Integration Status

- [x] `ShareX.ImageEditor` is referenced by UI/Core/RegionCapture projects
- [x] Theme synchronization between XerahS app theme and editor theme manager
- [x] Legacy image-effect preset namespace compatibility (`ShareX.Editor`/`XerahS.Editor` -> `ShareX.ImageEditor`)
- [x] Region capture overlay compatibility toolbar (`XerahS.RegionCapture.UI.Controls.AnnotationToolbar`) kept locally for upstream parity gaps
- [ ] Full regression parity test coverage for prior in-repo editor behavior (legacy editor tests are currently excluded in `tests/XerahS.Tests/XerahS.Tests.csproj`)

## Roadmap

### Phase 7: Stabilization and Distribution (Current)

- [x] Export and destination integration
- [x] Clipboard copy path through platform services
- [x] Quick save and save-as flows
- [x] Upload-to-host integration with provider selection/fallback pipeline
- [x] Distribution baseline (icons/assets, tray, release workflows)
- [ ] End-to-end verification matrix for annotation tools across desktop platforms
- [ ] End-to-end verification matrix for image effects across desktop platforms
- [ ] Native app interoperability validation (copy/paste in external apps)
- [x] Linux packaging and Linux capture/hotkey orchestration test coverage
- [ ] macOS on-device validation for capture/hotkeys/clipboard/permissions
- [ ] macOS platform completion for remaining stubs/TODOs (ScreenCaptureKit strategy internals, window service gaps, OCR implementation)
- [ ] Restore or replace editor regression scenarios removed during upstream ImageEditor swap
- [ ] Complete remaining after-upload automation gaps (URL shortener and related tasks)

## Known Issues / Notes

- Editor regression suites tied to the legacy in-repo editor API are currently excluded from build (`tests/XerahS.Tests/XerahS.Tests.csproj` removes `Editor/EditorHistoryEffectsTests.cs` and `Editor/EditorRotateAnnotationsTests.cs`).
- macOS platform layer still contains MVP stubs and TODOs in capture/window/OCR paths (for example `src/platform/XerahS.Platform.MacOS/Capture/ScreenCaptureKitStrategy.cs` and `src/platform/XerahS.Platform.MacOS/MacOSWindowService.cs`).
- After-upload URL shortener automation is logged as not implemented in `src/desktop/core/XerahS.Core/Tasks/Processors/UploadJobProcessor.cs`.
