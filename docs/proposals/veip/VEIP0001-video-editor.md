# VEIP0001 — ShareX Video Editor 

> **Description:** A lightweight, cross-platform video editor module (DLL) for post-capture workflows like trimming, cropping, and format conversion. Designed to be consumed by host applications like ShareX and XerahS.

**Status**: DRAFT
**Priority**: High
**Related**: 
**Repository**: [https://github.com/ShareX/ShareX.VideoEditor.git](https://github.com/ShareX/ShareX.VideoEditor.git)

---

## 1. Executive Summary
The Video Editor is a standalone, cross-platform library (`ShareX.VideoEditor`) designed to provide a lightweight, high-performance, and beautifully crafted environment for quick video edits. Much like the existing image editor, it focuses on post-capture workflows such as trimming, cropping, and format conversion. It will operate as an independent DLL module so that its UI and editing capabilities can be invoked seamlessly by host applications like XerahS or ShareX. 

---

## 2. Motivation
Users frequently record screencasts and need a frictionless way to trim out dead time, crop to a specific region, or convert the recording to a different format (like an optimized GIF or WebP) before sharing. Currently, users have to rely on complex, heavy third-party video editors for these trivial tasks. By creating `ShareX.VideoEditor`, we deliver a tailored, extremely user-friendly experience specifically designed for the screencasting workflow.

---

## 3. Scope and Requirements

### 3.1 In Scope
- **Standalone Module**: Must be a fully independent `ShareX.VideoEditor` class library (DLL) project, hosted at `https://github.com/ShareX/ShareX.VideoEditor.git`. Both XerahS and ShareX will integrate it as a Git submodule. Host applications will invoke its Window directly, passing the target video file path.
- **Trimming**: Cut out sections from the start, middle (split), or end of the video.
- **Cropping**: Visually crop the video frame to a specific dimension.
- **Format Conversion**: Convert between MP4, WebM, GIF, and WebP.
- **Annotation & Watermarks**: Incorporate simple text annotations and reuse existing watermark components/configurations provided by the host application (rather than building an isolated overlay system).
- **Optimization**: Adjust framerates or resolution to achieve target file sizes before uploading.
- **Best-in-Class UI/UX**: Premium, intentionally crafted visual aesthetic conforming with the project's frontend design skills.
- **Free Components Only**: All Avalonia controls and third-party libraries used must be 100% free and open-source. No commercial or premium paid components are allowed.

### 3.2 Out of Scope
- Advanced multi-track timeline editing.
- Complex visual effects, audio mixing, or 3D transitions.
- Authoring video from scratch (it is strictly an editor for existing media).
- Downloading or managing FFmpeg binaries (host applications are responsible for providing this path).

---

## 4. Proposed Architecture

### 4.1 UI Framework (Avalonia)
The application will be built using **Avalonia UI** for true cross-platform functionality (Windows, macOS, Linux).
- **Pattern**: Strict MVVM leveraging **ReactiveUI** (`ReactiveObject`, `ReactiveCommand`).
- **Data Binding**: Must use Compiled Bindings (`x:CompileBindings="True"`, `x:DataType="..."`).
- **Styling**: Utilize `FluentAvaloniaTheme` as the visual base.
- **Cost**: Only use free, open-source components. No paid controls. 

### 4.2 Media Engine Pipeline
We will use a hybrid approach to handle media playback and processing:
1. **Playback**: 
   - Primary: Utilize Avalonia's built-in native `MediaPlayer` to leverage OS-native APIs for playback (Media Foundation for Windows, AVFoundation for macOS, GStreamer for Linux).
   - Fallback: Utilize an embedded free fallback like `FFmpegVideoPlayer.Avalonia` or `LibVLCSharp.Avalonia` for broader codec support if native APIs are unavailable.
2. **Processing/Rendering**:
   - Utilize a managed wrapper for FFmpeg (like `FFmpegCore` or `FFmpeg.AutoGen`) or raw process invocation to execute the actual destructive editing, clipping, and format conversion tasks in the background without blocking the UI.
   - Use FFmpeg to asynchronously generate frame thumbnails for the scrubber timeline.
   - **Important**: This DLL expects the host application (e.g., ShareX, XerahS) to locate and supply the path to the FFmpeg executable. The Video Editor will not download, package, or manage FFmpeg itself.

### 4.3 Host Application Integration
- **Entry point**: Host application instantiates a modeless `VideoEditorWindow` from the DLL, passing an options object: e.g., `new VideoEditorOptions { VideoPath = "...", FFmpegPath = "...", Theme = "...", Culture = "...", WatermarkSettings = ... }`.
- **Inheritance**: The Video Editor actively inherits translation localizations, Avalonia theme styling, and watermark logic/settings from the host application’s payload.
- **Completion**: Provides C# events/Callbacks to notify the host application upon successful export. The editor prompts the user via a dialog box to "Save As" (creating a new file instance) or "Overwrite" the input media before triggering the callback.

---

## 5. UI/UX and Aesthetic Requirements

The UI must not feel like a generic desktop window. It must be bold, striking, and meticulously aligned, respecting `.ai\skills\frontend-design\SKILL.md` and `.ai\skills\design-ui-window\SKILL.md`.

### 5.1 Design Direction
- **Theme & Localization**: Must seamlessly ingest and apply the host app's (XerahS/ShareX) current theme and translation locale. UI defaults to Premium Dark Mode if none is provided.
- **Typography**: Utilize distinct, modern typography with a strong visual hierarchy. Pair a characterful display font for headers with a highly legible geometric font for UI labels.
- **Layout System**: Adhere strictly to a grid-based layout with consistent spacing tokens. Avoid arbitrary pixel margins. 
- **Feedback & Motion**: Every interactive control must provide immediate visual feedback (hover/pressed/focused/disabled states). Transitions between views (e.g., from player screen to export progress screen) should feature smooth, descriptive animations.
- **Density**: Purposeful whitespace; do not cramp the UI. The interface should feel spacious but focused.

### 5.2 Window Structure & Controls
- **Modality**: Operates as a *modeless* window, allowing users to interact with XerahS while the editor remains open.
- **Main View**: Split into a prominent video player (center stage) and a tools/timeline section (bottom).
- **Timeline Scrubber**: A custom Avalonia control that displays generated frame thumbnails along a track with draggable trim handles.
- **Context Menus**: Do **not** use the legacy `ContextMenu`. Under FluentAvalonia, you must exclusively use `ContextFlyout` with `MenuFlyout` to avoid rendering bugs.
- **Actions**: A prominent, visually distinct Primary Action button (e.g., "Export" or "Save") overriding standard secondary options.

---

## 6. Implementation Plan

### Phase 1: Application Skeleton
- Initialize a new `ShareX.VideoEditor` Avalonia class library (DLL) project with ReactiveUI.
- Implement the main `VideoEditorWindow` and define the configuration payload class that host applications will pass in (must include `FFmpegPath` and the target media).
- Establish the base standard Grid layouts, spacing resources, and typography tokens.

### Phase 2: Media Playback Integration
- Integrate Avalonia `MediaPlayer` or the cross-platform fallback component into the primary View.
- Bind the Video surface to the ReactiveUI ViewModel to reflect playback state, duration, and current position.
- Build the UI controls for basic scrubbing, Play, Pause, and timeline navigation.

### Phase 3: Timeline & Editing Tools UI
- Implement the custom Timeline scrubber control with interactive trim handles.
- Integrate an FFmpeg thumbnail extractor to asynchronously populate the timeline track with video frames using the provided FFmpeg path.
- Build floating/docked tool panels for Cropping (viewport overlay) and Export Settings (FPS, Resolution scale, Format dropdown).
- Build the Watermark UI overlay system to interpret the host application's payload configurations natively.

### Phase 4: FFmpeg Render Pipeline
- Create a `VideoExportService` that dynamically constructs FFmpeg arguments based on the user's ViewModel state (trim points, crop coordinates, watermark text, output format).
- Execute the FFmpeg process asynchronously using the executable path supplied by the host application, capturing standard output to report granular progress via an Avalonia `ProgressBar` or circular indicator.

### Phase 5: Polish and Integration
- Final UX pass: Validate keyboard navigation, accessible names, focus order, and contrast compliance.
- Refactor repeated styles into a dedicated `.axaml` resource dictionary.
- Update XerahS and ShareX development branches to pass the proper configuration and await the editor window to close to verify post-capture workflows.

---

## 7. Open Questions / Unknowns
- **Avalonia MediaPlayer Stability**: Avalonia's built-in media functionality is actively evolving. We may need to evaluate and explicitly commit to a free fallback like `VlcVideoPlayer.Avalonia` if codec support on certain systems falls short.

---

## 8. Rollout Strategy
1. Release a beta version of the Video Editor DLL bundled with the next XerahS/ShareX snapshot.
2. Integrate as an optional post-capture action in the ShareX/XerahS configurations.
3. Invite power users to test specific stress cases (4K videos, 60fps duration, large file optimizations).
4. Gather feedback primarily on the FFmpeg export performance, cross-platform playback stability, and UI timeline smoothness.
5. Graduate to wide release, standardizing it as the default screencast handler for the ecosystem.
