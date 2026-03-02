# Changelog

All notable changes to XerahS will be documented in this file.

The format follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html):
- **MAJOR** (x): Breaking changes (0 while unreleased)
- **MINOR** (y): New features and enhancements
- **PATCH** (z): Bug fixes and patches


## Unreleased

## v0.19.1

### Fixes
- **Linux Recording**: Harden GStreamer recording by correcting region crop, removing conflicting `video/x-raw` caps before `glupload`, adding GL-to-CPU fallback, and making fatal errors selectable in RecordingView (01527ef5, ef55b9e7, 78523202, eba1e9d0, ba13d971)
- **Linux Recording**: Clean up portal session on fatal errors to prevent unobserved exceptions (d69bd5a1)
- **Core**: Fix tray stop button behavior and hotkey recording stop flow (36410a85)
- **PluginLoadContext**: Fix stale shared dependency name/order checks (fff53962)
- **Updates/Logging**: Fix reflection-disabled GitHub update JSON handling and normalize error log naming to `yyyyMMdd` (f2ed43cf)

### Refactor
- **Core**: Centralize log and app path handling with `PathsManager` and expand path audit coverage for plugins/screenshots/tools/troubleshooting paths (ad12770f, bcb0423e)

### Build
- **Release Automation**: Run maintenance chores during release bump-tag flow (df7976f4)
- **Developer Tooling**: Add `run-debug-app.sh` helper script (7d4fe9ec)

### Documentation
- **XIP0042**: Update the ImageEditor SkiaSharp hardware acceleration task document (3605dfa7)

## v0.19.0

### Fixes
- **Core**: Correct DMDO_90/DMDO_270 → ModeRotation mapping in DXGI capture (b484d197)

### Documentation
- **Core**: Fix XIP0042 markdown rendering (939f92c5)
- **Core**: Normalize mojibake symbols in XIP0042 task doc (28c39130)
- **Core**: Replicate XIP0042 formatting from ba3713b3 (5b418f5d)

### Changed
- **Core**: [Docs] Shorten XIP0043 title and backup filename; sync XIP0038/XIP0040 slugs (8ebe0ae8)
- **Core**: [Docs] XIP sync: GitHub source of truth, single-folder backup, merge script (5994bb13)
- **Core**: [XIP0042] Second audit — update task after Jaex's Round 2 fixes (4c06d5cf)
- **Core**: [XIP0042] Sync task doc from feature/XIP0042-optimizations (latest implementation status) (b9da24b8)
- **Core**: [XIP0042] Update GPU effects task with current codebase audit (7c70e94a)
- **Core**: Move XIP0043 task to complete folder (2b9a95ed)
- **Core**: Update ImageEditor (009d2201, 12c0380f, 600a1fdd, 8236ce9c, 9c2f85c4)

## v0.18.11

### Fixes
- **Core**: Avoid SIGPIPE in archive validation checks (93287f30)

## v0.18.10

### Fixes
- **Core**: Correct flipped monitor orientation in DXGI capture (106a497d)
- **Core**: Fail fast for Linux publish and validate package payload (78f93344)
- **Core**: Harden daemon bundling across desktop RIDs (d3052258)
- **Core**: Marshal Avalonia clipboard access to UI thread (6d24889e)
- **Core**: Remove WinForms dependency from Windows platform (0ced3438)
- **Update Changelog Script**: ensure entries array has Count for single-category (22b5cbb3)

### Build
- **Core**: Add changelog update automation script (18d58b73)
- **Core**: Validate release assets and RID metadata (571e383c)

### Performance
- **Core**: Skip app-driven plugin build in solution builds (57fb31f6)
- **Core**: Update ImageEditor submodule for TFM simplification (619dddda)

### Changed
- **Core**: Create XIP0043-Remove-WinForms-and-Harden-CrossRID-Daemon-Bundling.md (63895920)
- **Core**: Update CHANGELOG.md (43b0cbdb)
- **Core**: Update ImageEditor (6fc22242)

## v0.18.9

### Features
- **Mobile**: Android and iOS MVP with Share Extension and MAUI; adaptive theming, upload queue/picker/history, active destination selector, desktop-compatible upload filename pattern, broad share-intent support; Amazon S3 and Custom Uploader config UI; Swift/Kotlin native shells and share extension `(8746372, 03698c6, 493d147, 4b79ddb, a7cfb22, 1e5f9eb, 30bbe98, 68d97d9, 52d6ad2, 0b42d73, ccfa4ea, 357188f, c0af5d6, dbb6633, 7292102, 78a488e, 08604ee5, 21c40429, 5876b44b, 1e61b8bf)`
- **Media Explorer**: Provider file browsing with S3 and Imgur, navigation, search, filtering, and CDN thumbnail optimization `(9deedf9, e374160)`
- **Watch Folder**: Daemon with lifecycle hooks, runtime policy, settings controls, and tests `(79c1292, 2b94600, 4265528, 992c41b)`
- **Indexer**: Async streaming with progress and cancellation; open in own window; file extension filters; dark theme with light-mode toggle `(8b2fe88, 8b20b3b, e3445f5b, cc58316, d24cdcf)`
- **ImageEditor**: Integrate submodule; File Open choice dialog; annotation options persistence; app/editor theme sync `(0db2c71, 1a41df5, 7e82df3, 0d42719, 71fa3e1)`
- **Workflows**: UploadContentWindow; AutoCapture, Pin to Screen, Ruler, MonitorTest, HashCheck; 6 media tools (ImageCombiner, ImageSplitter, ImageThumbnailer, VideoConverter, VideoThumbnailer, AnalyzeImage); OCR and ScrollingCapture end-to-end `(298457a, a45d02f, 1e0d3f2, 5647b4d, 8ea941e, 56a1ea3, 8e3164ac, 3a779ef1, ed56345c, 1eff3202)`
- **Upload**: Auto destination uploader; cross-platform secrets store with diagnostics; proxy config UI `(f3abe81, c2b8105, f626f09, 473cbb88)`
- **Amazon S3**: AWS SSO auth, region selection, CNAME, public bucket policy; redesign config to mimic Custom Uploaders `(9e2623be, 6880866, 6bacd05e)`
- **Plugins**: Dropbox, Paste2, GitHub Gist, FTP/FTPS/SFTP, Pastebin; XIP0040 plugin architecture; DestinationsPluginSdk `(e04a8953, 3ec377db, 83669aec, 848d3064, c5c49513, 1c92e2c2)`
- **UI**: Copy Errors to HistoryView, AfterUploadWindow, Toast `(5c08812)`
- **Linux Capture**: DBus fallbacks, KDE permissions, decision trace orchestration, portal waterfall `(290b3e0, dc02dbd, c744059)`
- **Packaging**: Scoop, WinGet, Chocolatey support; generate-winget.ps1 enhancements `(1ce955e0, aaa833f6, 552ef730, 124095e7)`
- **Misc**: Imgur album selection and GIFV; Dropbox OAuth overhaul `(70a34373, d4993fd0)`

### Fixes
- **ImageEditor**: XAML startup crash, highlight/crop/submodule fixes, context menu, DPI and crop handles `(258bb09, f987eaa, 73dff63, 0eca71e, fcddf02, d9ab54a, db3bcaa, 584de4e, bd44498, 80eb42f, a1ac173, 592a2f1, 2cbc692, f85c57f, bb862c4, c5618de)`
- **Scrolling Capture**: Auto-scroll, workflow settings, hotkeys, scroll position detection `(1fa45f2, 971219c, 8ac2c8b)`
- **Media Explorer**: Harden listing, normalize URLs, error handling, copyable footer `(9bab13e, e1a5d59, 6b2b8d6, f4e796b)`
- **Mobile**: iOS App Group for S3 config in Share Extension; unify share payload and TimeZoneInfo `(42a1033, 0aad5c1, a835153)`
- **Upload**: MainViewModel parameterless copy/upload; multi-uploader fallback, clipboard routing `(06a2232, 72079e6, c06f17f, 6527590)`
- **Capture/Region**: Annotation layer rendering, crop offset, AfterCapture refresh, workflow integration `(f3e3908, b3034be, af35c74, 4048f00, c5efeab, 4500b8a)`
- **Workflows**: Allow OCR and scrolling workflows from tray `(4e07852)`
- **Linux**: Portal timeout, Wayland/slurp/portal fixes, GStreamer clamp, D-Bus and plugins path resolution `(501af7bb, 4de4a5b1, 4735dcb1, 89a61dd4, d2590b9d, 5e12cbed)`
- **After Capture**: ShowAfterCaptureWindow persistence `(9a04c9d, a3a581d, a8262d4)`
- **Misc**: FAQ XerahS/ShareX Linux ref; update checker pre-releases; backup machine-specific; S3 setup reorder; macOS icon in Windows build; File Open dialog crash `(699634f, ed68066, c618542, 3196b02, ba40fbb, 5cbf5dd)`

### Refactor
- **Core**: Split large ViewModels, WatchFolder daemon base service, ScreenRecordingManager startup; WindowState naming; GeneralHelpers split `(86286af, 315549a, 1160519, 506072e, 78214dd)`
- **Upload**: Polymorphic uploader config pilot `(7f2815d)`
- **Workflows**: App workflow orchestration services `(4ee8ab9)`
- **Linux Capture**: Modular providers, parallel lanes, coordinator, contracts `(733a49d, 5dd9931, 0a81693, 3569c0a)`

### Build
- **CI/Release**: All-platform release workflow, Linux by arch, release title, bump/tag automation `(2fbe5ee, bd8d0d3, aeccb68, 55f25d3)`
- **Android**: Mobile build infrastructure `(3952287)`
- **Linux**: Plugin packaging, RPM strip, display diagnostics, desktop-file-utils `(817d83a, 0723b45, 1c79a94, 2f6e3112)`
- **ImageEditor**: Submodule checkout, recovery hook, pre-push `(3098824, 899e8f1)`
- **Misc**: Version/changelog bumps, central package management, plugin DLL deduplication, cross-compilation macOS, GPL headers Swift/Kotlin `(81db32e, a2bf5a61, 19b3a84c, 519423d9, 55f25d30, cbcd5bb3)`

### Documentation
- **Consolidate**: Developer docs to developers/; plugins to developers/plugins and .xsdp; changelog consolidation; mobile README simplification `(1f17491, b78882f, 41702bd, 21927b4, ad719c9, c9ebe39, 72f2e55, c043844)`
- **Planning**: Roadmap, XIP0033 complete, task docs `(caeaae1, e3f37e3, 04cf9cf, 168b2ea)`
- **Misc**: Feasibility report JS/CSS; sync-submodules; build/Linux/mobile docs; XIP0040/0039; update-changelog skill in maintenance-chores `(8fc7446, 47d833c, ce35146, e9ed21a, 8e97f89, ccff1c4, a05200f, 14be1df, 717be27, 76df673, 5ade43b)`

### Testing
- **Linux Capture**: Waterfall and lane matrix tests `(7f49769)`

### Performance
- **RegionCapture**: Reduce annotation rebuild pressure `(3bf82243)`


## v0.17.4

### Features
- **Indexer**: Modernize HTML output flow and default to dark theme with light-mode toggle `(cc58316, d24cdcf)`

### Build
- **CI**: Split Linux release builds by runner architecture and set release title metadata `(aeccb68)`
- **Automation**: Add release bump/tag workflow skill for standardized release prep `(55f25d3)`


## v0.16.3

### Features
- **Mobile**: Add active upload destination selector and in-app destination label on Android and iOS `(0b42d73, ccfa4ea)`
- **Mobile**: Use desktop-compatible upload filename pattern on Android and iOS `(357188f, c0af5d6)`
- **Mobile**: Add broad share-intent support for arbitrary file types on Android and iOS `(dbb6633, 7292102)`
- **Media Explorer**: Implement provider file browsing with S3 and Imgur support, including navigation, search, filtering, and CDN thumbnail optimization `(9deedf9, e374160)`
- **Watch Folder**: Add watch-folder daemon with lifecycle hooks, runtime policy controls, and tests `(79c1292, 2b94600, 4265528, 992c41b)`
- **Mobile**: Add adaptive theming infrastructure with native styling polish `(4b79ddb, a7cfb22, 1e5f9eb, 30bbe98)`
- **Mobile**: Add upload queue, picker, and history screens `(68d97d9, 52d6ad2)`
- **UI**: Add Copy Errors to UI (HistoryView, AfterUploadWindow, Toast) `(5c08812)`
- **ImageEditor**: Add app/editor theme synchronization with platform-aware styling `(0d42719, 71fa3e1)`

### Fixes
- **iOS**: Use App Group settings so Share Extension can read Amazon S3 configuration `(42a1033)`
- **ImageEditor**: Fix precompiled Avalonia XAML startup crash (`XamlLoadException`) in editor app initialization `(258bb09, f987eaa)`
- **ImageEditor**: Improve highlight rendering/fill behavior, Smart Eraser, text defaults, and canvas zoom performance `(73dff63, 0eca71e, fcddf02, d9ab54a, db3bcaa, 584de4e, bd44498)`
- **ImageEditor**: Restore crop UX and precision with full-image/L-shape fixes, visible handles, and DPI-aware hit zones `(80eb42f, a1ac173, 592a2f1, 2cbc692, f85c57f)`
- **Scrolling Capture**: Improve auto-scroll behavior and workflow settings integration `(1fa45f2, 971219c, 8ac2c8b)`
- **Workflows**: Allow OCR and scrolling workflows from tray `(4e07852)`
- **Media Explorer**: Harden listing, normalize URLs, and improve error handling `(9bab13e, e1a5d59, 6b2b8d6, f4e796b)`
- **Mobile**: Unify iOS share payload handling and TimeZoneInfo serialization `(0aad5c1, a835153)`
- **Upload**: Align MainViewModel helper with parameterless copy/upload events `(06a2232)`
- **ImageEditor**: Update submodule with context menu fixes `(bb862c4, c5618de)`
- **Capture**: Optimize annotation layer rendering and resource management `(f3e3908, b3034be, af35c74, 4048f00)`
- **Documentation**: Update FAQ to correctly reference XerahS instead of ShareX in Linux screen capture section `(699634f)`
- **Infrastructure**: Integrate update-changelog skill into maintenance-chores workflow `(5ade43b)`

### Refactor
- **Core**: Split large ViewModels, extract WatchFolder daemon base service, and consolidate ScreenRecordingManager startup flow `(86286af, 315549a, 1160519)`
- **Core**: Remove WindowState naming collisions `(506072e)`
- **Core**: Split GeneralHelpers into utility classes `(78214dd)`
- **Upload**: Add polymorphic uploader config pilot `(7f2815d)`
- **Workflows**: Extract app workflow orchestration services `(4ee8ab9)`

### Build
- **Infrastructure**: Add all-platform release workflow and repository sync helper script `(2fbe5ee, bd8d0d3)`
- **Android**: Add Android mobile build infrastructure `(3952287)`
- **Linux**: Harden plugin packaging, RPM strip behavior, and display diagnostics `(817d83a, 0723b45, 1c79a94)`
- **Hooks**: Add cross-platform ImageEditor recovery and auto-push on pre-push `(3098824, 899e8f1)`

### Documentation
- **Maintenance**: Simplify mobile README and move refactor/hardening notes into documentation archives `(ad719c9, c9ebe39, 72f2e55, c043844)`
- **Planning**: Update task planning docs and move completed XIP0033 `(caeaae1, e3f37e3, 04cf9cf, 168b2ea)`
- **Plugins**: Consolidate plugin documentation into 'developers/plugins' and standardize on .xsdp extension `(b78882f, 41702bd, 21927b4)`
- **Developer**: Consolidate developer documentation into 'developers' root folder `(1f17491)`
- **Architecture**: Add feasibility report for JS/CSS migration `(8fc7446, 47d833c, ce35146, e9ed21a, 8e97f89, ccff1c4)`
- **Submodules**: Add sync-submodules workflow and update ImageEditor to latest develop `(a05200f, a0e3054, 14be1df)`
- **Tasks**: Add refactoring audit skill and native UI theming task `(ff8ea0e)`


## v0.15.5

### Features
- **Linux Capture**: Add DBus fallbacks, KDE desktop permissions, and decision trace orchestration `(290b3e0, dc02dbd)`

### Fixes
- **Linux Capture**: Enforce portal-only sandbox policy, unify waterfall, and improve logging `(2de4ac6, c744059, a381faa)`
- **Builds**: Fix cross-platform build configuration and add linux-arm64 support `(ad8611c, 519423d)`

### Refactor
- **Linux Capture**: Modularize providers with parallel lanes, coordinator, and contracts `(733a49d, 5dd9931, 0a81693, 3569c0a)`

### Testing
- **Linux Capture**: Add Linux capture waterfall and lane matrix tests `(7f49769)`

### Documentation
- **Build System**: Rename developer README and add Linux guide `(717be27)`
- **Roadmap**: Finalize Linux phase roadmap and release gate `(76df673)`

## v0.15.0

### Features
- **Mobile**: Add Android and iOS MVP with Share Extension support, .NET MAUI project `(8746372, 03698c6, 493d147)`
- **Mobile**: Add Custom Uploader and Amazon S3 configuration UI `(#124, #125, @Hexeption; 78a488e)`
- **Indexer**: Implement async streaming indexer with progress and cancellation `(8b2fe88)`

### Fixes
- **Image Editor**: Share annotation preview visuals with ImageEditor to ensure consistency `(cc074ad)`
### Fixes
- **Annotations**: Optimize rendering, remove draw-start dot artifact, and improve responsiveness `(d1afa2f, faa84e7, 891eed0)`
- **Workflow**: Complete WorkflowType end-to-end wiring `(47ead0b)`
- **UX**: Hide SilentRun window on first open instead of minimizing `(7567223)`
- **Updates**: Gracefully handle repositories with only pre-releases `(ed68066)`
- **After Capture**: Persist "Show after capture window" behavior across repeated runs `(9a04c9d, a3a581d, a8262d4)`
- **Upload**: Add multi-uploader auto destination fallback and wire mobile Amazon S3 and plugin integration to InstanceManager `(72079e6, c06f17f, a576e78, 44c316b, 02087fb)`
- **Watch Folder**: Convert MOV captures to MP4 `(27f6fec)`
- **Settings**: Make backup and secrets filenames machine-specific `(c618542, 55a32d0)`
- **Amazon S3**: Reorder and renumber setup steps `(3196b02)`
- **iOS**: Improve local signing setup and share extension flow `(30f6822)`

### Build
- **Plugins**: Centralize plugin copy target and pass host TFM `(6bfa2e1)`
- **Dependencies**: Bump Avalonia packages to 11.3.12 `(27ce502)`
- **ImageEditor**: Update submodule for theme-aware view, net9 compatibility, and track develop branch `(5e8eee0, e03ec12, 71601ee, a17d91e, 493d147)`

### Documentation
- **Audits**: Organize audit files and update UI control inventory snapshots `(e3d2a9c, aadfea4)`
- **Tasks**: Mark XIP0030 complete and move to completed tasks `(25a83a1)`

## v0.14.0

### Features
- **Monitor Test**: Implement MonitorTest workflow with diagnostic and pattern testing modes `(56a1ea3, 1dc10f8)`
- **Tools**: Add Ruler workflow with full RegionCapture integration `(5647b4d, 8ea9419)`
- **Indexer**: Make Index Folder open in its own window `(8b20b3b)`
- **Editor**: Integrate upstream ShareX.ImageEditor submodule with File Open choice dialog `(0db2c71, 1a41df5)`
- **Region Capture**: Add annotation options persistence `(7e82df3)`

### Fixes
- **Logging**: Fix duplicate date in log filename on date rotation `(69cb3c2)`
- **Region Capture**: Improve annotation toolbar integration and reduce rebuild pressure `(4500b8a, 3bf8224)`
- **Indexer**: Enable Open in Browser button and remove WebView in favor of system browser `(4582529, 16945a0)`
- **Navigation**: Enable menu navigation and update editor data transfer APIs `(49772bf)`
- **Editor**: Sync ImageEditor fixes, persist annotation options, refactor platform abstractions, enable Zoom to Fit `(3ee199a, 2cc8fa7, 554099c, 79eb2be, e5ffef7)`
- **ImageEditor**: Update submodule with unified undo-redo, smart padding crop sync, clipboard fixes, z-order fixes, and dispose bug fixes `(240649d, b3125b8, 0ee0ad7, 4eb30bf, 0c2b53e, 1131223, 751eb7c)`
- **Packaging**: Restore macOS icon in Windows package build `(ba40fbb)`
- **Upload**: Delay upload progress title update until actual upload starts `(9d4894b)`
- **macOS**: Harden mac packaging and cross-platform editor wiring `(6e1d569)`
- **Dialogs**: Prevent File Open dialog crash and add global exception logging `(5cbf5dd)`

### Build
- **Cross-Compilation**: Add macOS from Windows support and build system documentation `(a2bf5a6, 19b3a84)`
- **Infrastructure**: Fix version parsing in Windows package script `(5069a01)`

## v0.13.0

### Fixes
- **Menu Bar**: Fix hash checker routing and dynamic workflows menu `(8068e6f)`
- **Upload**: Improve Upload Content workflow handling, window UX, and text upload routing `(62a1cda, 4fd8182)`

## v0.12.0

### Fixes
- **Tools**: Add media tools to navigation bar and fix DataTemplate issues `(485a438)`
- **Proxy**: Fix custom uploader loading and add configuration UI `(#77, @Hexeption)`
- **Linux**: Add dark mode support, theme settings, and Wayland Hyprland screenshot support `(#62, @unicxrn; #61, @unicxrn)`
- **macOS**: Add native application menu `(#60, @Hexeption)`
- **Custom Uploaders**: Fix compatibility improvements and version compatibility `(#74, @Hexeption; #71, @emmsixx)`
- **Security**: Fix DPAPI platform warning `(#73, @Hexeption)`

### Refactor
- **Editor**: Rename namespace from ShareX.Editor to XerahS.Editor and update all references `(25135d0, d0d1266, 1dfeb3b)`

### Build
- **Plugins**: Improve plugin copy target to only include plugin assemblies `(a9b5c63)`
- **Configuration**: Update build files, packaging configuration, issue templates, and .gitignore `(09222cc, 5c03c33, b107da9, 789ec93)`

## v0.11.0

### Features
- **Upload**: Implement UploadContentWindow and remove superseded upload WorkflowTypes `(298457a)`

## v0.10.0

### Features
- **Workflows**: Implement AutoCapture workflows `(a45d02f)`

## v0.9.0

### Features
- **Workflows**: Implement Pin to Screen workflows `(1e0d3f2)`
- **Amazon S3**: Enhance SSO with region selection `(6880866)`

### Fixes
- **Upload**: Improve upload error surfacing and history actions `(760a6ef)`
- **Workflows**: Preserve workflow order and exclude None `(6c08b22)`
- **Custom Uploaders**: Fix compatibility check for XerahS versions `(422710a)`

### Build
- **Plugins**: Restore plugin DLL deduplication with retry logic `(81db32e)`

### Core
- **Rendering**: Remove RectangleLight; modern Skia rendering deprecated it `(12d3ae5)`

## v0.8.0

### Features
- **Security**: Add cross-platform secrets store with diagnostics `(c2b8105, f626f09)`
- **Upload**: Add auto destination uploader `(f3abe81)`
- **Custom Uploaders**: Implement full support including editor UI and integration `(5962870, 8020d73)`
- **Task Settings**: Redesign Task Settings UX with dedicated Image/Video tabs `(43436af)`
- **Tray Icon**: Add recording-aware tray icon with pause/abort controls `(7d22818)`
- **Image Formats**: Add AVIF and WebP image format support `(3b89381)`
- **Linux/Wayland**: Fix screen capture on Wayland by integrating XDG Portal API `(4cc5a9f)`

### Fixes
- **Capture**: Allow clipboard payloads in capture phase `(a2e336f)`
- **Upload**: Add clipboard upload auto routing `(6527590)`
- **Region Capture**: Correct crop offset, refresh AfterCapture UI, and fix coordinate mapping for Windows `(c5efeab, #29)`
- **Linux**: Fix active window capture hierarchy, coordinates, hotkey initialization, and Region Capture `(2957c89, 007f261, 73dd95d, e8a9cc8)`
- **UX**: Hide main window when capture triggered from tray/navbar `(45264fb)`
- **UI**: Fix update dialog layout `(7868256)`

### Refactor
- **Editor**: Update XerahS.Editor.csproj references and docs `(1dfeb3b, 90b9fe0)`


## v0.7.0 - Annotation Overlays & Packaging

### Features & Improvements
- **Annotations**: Enable Annotation Toolbar in Region Capture Overlay and refactor `(05dcaf3, #53)`
- **Region Capture**: Add support for transparent background capture (RectangleTransparent) `(9ee7277)`
- **macOS**: Native single-file app bundle packaging (`.app`) `(c2b882c)`
- **Packaging**: Automated multi-arch Windows release builds `(49a7ec6)`
- **Plugins**: Support for user-installed plugins and packaging `(e787536)`
- **Window Capture**: Add support via monitor cropping fallback `(d73daf5)`
- **Media Library**: Basic implementation `(#49)`

### Bug Fixes
- **Annotation Layer**: Fix coordinate system for multi-monitor/high DPI and compositing `(5d69425, 61bd0c9, 3875298)`
- **Exceptions**: Global exception handling implementation `(ad6d443)`
- **Screen**: Fix frozen screen issue `(#51)`
- **Cursor**: Fix system cursor issues `(#46)`

## v0.6.0 - UI Redesign & Auto-Update

### Features & Improvements
- **UI Redesign**: Comprehensive visual overhaul of all views using Grid layout and consistent styling `(34f4cbf, d390fa7)`
- **Auto-Update**: Implement auto-update system with Avalonia UI `(54b9546)`
- **After Upload**: Add "After Upload" results window `(18a3ab7)`
- **Property Grid**: Add ApplicationConfig property grid `(c4d20bf)`
- **CLI**: Add `verify-recording` command for automated screen recording validation `(732e173)`
- **Editor**: Unify editor undo history across different toolsets `(24ad021)`
- **Architecture**: Move Windows-specific P/Invoke types to dedicated Platform.Windows project `(90da89a)`
- **FFmpeg**: Improve FFmpeg download/config UX with progress hooks and better path resolution `(1646cbb, 7677ceb, b4fdcbf)`
- **Documentation**: Replace ShareX.Avalonia references with XerahS `(#44)`
- **Workflow**: Update cursor handling `(#43)`

### Bug Fixes
- **Recording**: Improve GIF recording quality, add clipboard support, pause, and stroke-based abort `(1baecc0, 4148e49, c3d04a7)`
- **After Upload**: Fix window theming and errors `(9b752c0, 6dfe81e)`
- **Rendering**: Fix speech balloon tail geometry rendering `(784594e)`
- **Region Capture**: Fix system cursor appearing in screenshots and hotkey issues `(85a4e2f, #38, #39)`

## v0.5.0 - Core Capture & Editor Improvements

### Features & Improvements
- **Capture**: Add single instance enforcement for the application `(aacb23b)`
- **Region Capture**: Enhance crosshair visibility, add magnifier pixel sampling, and hide system cursor when ghost cursor active `(a838ae1, 56aa4de, d338b32)`
- **Editor**: Wire ImageEffectsViewModel to unified undo/redo stack `(81a3815)`
- **UX**: Set default file picker location to Desktop for easier access `(f5083e3)`

### Bug Fixes
- Fix 11+ HIGH/MEDIUM priority issues including null safety and resource management `(9188a22, 1f9a74f)`
- Set RegionCaptureControl cursor to None to prevent double cursor visibility `(fe35424)`

## v0.4.0 - Image Effects & Tools

### Features & Improvements
- **Image Effects**: Refactor preset management and improve effects UI `(154a6c9, 5d9dbd7, ee47e3d)`
- **Tools**: Add QR code generator/decoder and Color Picker tools with standard color name mapping `(66bd61b, bdb22f8, 0b50328)`
- **Watch Folders**: Implement Watch Folder system with per-folder workflow assignments `(49e838d, 63124f6, 951e034)`
- **Indexer**: Add Index Folder preview and modernize HTML output using WebView `(63ca369, 3f3a751, e57932e)`
- **macOS**: Add native ScreenCaptureKit video recording support `(fd75640)`

### Bug Fixes
- **Capture**: Fix cursor tracking and visibility during GDI capture `(f6973f6, e0a056b, 265a96a)`
- **Capture**: Fix NullReferenceException in DXGI capture by preventing premature disposal of D3D11 device context `(df9bd33)`


## v0.3.0 - Modern Capture Architecture

### Features & Improvements
- **Modern Capture**: Implement DXGI-based high-performance screen capture for Windows `(1440efc, 25f544d)`
- **Screen Recording**: Unified recording pipeline with Windows Media Foundation and FFmpeg support `(9224b62, 7a6e47b, 8fc451c)`
- **Workflow System**: Major overhaul of hotkeys into full Workflow system with GUID persistence `(faebe87, 09f1e35)`
- **Toast Notifications**: New custom Avalonia-based notification system with advanced settings `(6229154, f1d9b88)`
- **Linux**: Initial support for Wayland via XDG Desktop Portal and native X11 capture `(3573ad1, f7a103c, b92fb89, 7ccd5d9)`
- **Settings**: Add weekly backup system for application settings `(0a8e15f)`
- **UX**: Add tray icon support with customizable click actions `(035e8b4, 4ddfb59)`

### Bug Fixes
- **Modern Capture**: Fix multi-monitor blank capture issues `(52ae45e)`
- **Region Capture**: Fix DPI handling, coordinate mapping, and offsets/scaling on multi-monitor setups `(e4817b1, 954dee3, e47e81b)`
- **Code Quality**: Massive code audit fixing 500+ license headers and 160+ nullability issues `(dca9217, dd90761)`
- **Windows**: Standardize Windows TFM and fix CsWinRT interop issues `(2f44742, 4e88d23)`



## v0.2.0 - macOS Support & Plugin System

### Features & Improvements
- **macOS**: Initial platform support including ScreenCaptureKit, SharpHook hotkeys, and app bundling `(acba9d5, ca05d4b, 6fbf63e)`
- **Plugins**: Implement dynamic plugin system with packaging (`.sxap`), CLI tools, and `.sxadp` file association `(f81c656, a2adbf3, e787536, df9bbd1)`
- **History**: Switch history storage from XML to SQLite with automatic backups `(22b6cf5, 0f20d76)`
- **Editor**: Integrate ShareX.Editor as core component with SkiaSharp rendering `(57bfe32, 90b5871)`

## v0.1.0 - Initial Feature Set

### Core Features
- **UI**: Reimagined interface with two-toolbar system and modern dark theme `(c0bad1e, 231e4df)`
- **Capture**: Region, Fullscreen, and Window capture modes `(4839944)`
- **Annotations**: Object-based editor with Rectangle, Ellipse, Arrow, Line, Text, Number, Crop tools, and full Undo/Redo support `(bd1153c, 9b6cfe0, 9ecd720, cb7b54a)`
- **Hotkeys**: Global hotkey system with Win32 registration `(80cd222)`
- **Image Effects**: Initial implementation of 40+ effects including Resize, Shadows, and Gradients `(0840cef, 6777d86)`
- **History**: Basic task history tracking `(9c1c2f8)`

---

*This changelog follows Semantic Versioning while the project remains in pre-release (0.x.x).*
