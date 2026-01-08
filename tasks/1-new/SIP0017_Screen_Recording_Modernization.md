# SIP0017 Implementation Plan

## Current Implementation Status by SIP Stage

### Stage 1: MVP Recording (Silent) — 🟢 100% Complete

| Component | Status | Notes |
|-----------|--------|-------|
| `IRecordingService` interface | ✅ Complete | Full interface with Start/Stop/Events |
| `ICaptureSource` interface | ✅ Complete | Includes StopCaptureAsync |
| `IVideoEncoder` interface | ✅ Complete | Initialize/WriteFrame/Finalize |
| `IAudioCapture` interface | ✅ Complete | Prepared for Stage 6 |
| `RecordingOptions` | ✅ Complete | All fields documented |
| `ScreenRecordingSettings` | ✅ Complete | FPS/Bitrate/Codec/Audio flags |
| `FrameData`, `VideoFormat` | ✅ Complete | Proper structs with init |
| All EventArgs classes | ✅ Complete | Constructors included |
| Enums (CaptureMode, RecordingStatus, VideoCodec, PixelFormat) | ✅ Complete | All documented |
| `WindowsGraphicsCaptureSource` | ✅ Complete | WGC via Vortice.Direct3D11 |
| `MediaFoundationEncoder` | ✅ Complete | IMFSinkWriter with BGRA input |
| `ScreenRecorderService` | ✅ Complete | Orchestration with factory pattern |
| Factory registration in `WindowsPlatform.InitializeRecording()` | ✅ Complete | Called in Program.cs |
| **UI Integration (StartRecordingCommand)** | ✅ Complete | Implemented in `RecordingViewModel` |
| **RecordingToolbarView** | ✅ Complete | Implemented as floating overlay |

### Stage 2: Window & Region Parity — 🟡 ~40% Complete

| Component | Status | Notes |
|-----------|--------|-------|
| `InitializeForWindow(IntPtr)` | ✅ Complete | Uses WGC CreateItemForWindow |
| `InitializeForPrimaryMonitor()` | ✅ Complete | Uses WGC CreateItemForMonitor |
| Region cropping logic | ❌ Not Started | Currently falls back to fullscreen |
| Cursor overlay (software) | ❌ Not Started | WGC cursor enabled by default |
| GraphicsCapturePicker integration | ❌ Not Started | Current code takes direct HWND |

### Stage 3: Advanced Native Encoding — 🟡 ~30% Complete

| Component | Status | Notes |
|-----------|--------|-------|
| MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS | ✅ Complete | Enabled in encoder |
| Bitrate/FPS controls in Settings | ✅ Complete | ScreenRecordingSettings has fields |
| UI controls for Bitrate/FPS | ❌ Not Started | No settings UI for recording |
| Hardware encoder detection/display | ❌ Not Started | MF auto-detects but no UI indicator |

### Stage 4: FFmpeg Fallback & Auto-Switch — 🟡 ~20% Complete

| Component | Status | Notes |
|-----------|--------|-------|
| `FFmpegOptions` model | ✅ Complete | Full codec/source options |
| `FFmpegCaptureDevice` | ✅ Complete | GDIGrab, DDAGrab, etc. |
| `FFmpegRecordingService` | ❌ Not Started | Mentioned in code but not implemented |
| Auto-switch logic on exception | ✅ Partial | ScreenRecorderService catches PlatformNotSupported/COMException |
| `FallbackServiceFactory` registration | ❌ Not Started | Commented out in WindowsPlatform.cs |

### Stage 5: Migration & Presets — 🔴 Not Started

| Component | Status | Notes |
|-----------|--------|-------|
| ShareX config import logic | ❌ Not Started | |
| Modern vs Legacy toggle in UI | ❌ Not Started | |

### Stage 6: Audio Support — 🔴 Not Started

| Component | Status | Notes |
|-----------|--------|-------|
| `WasapiLoopbackCapture` | ❌ Not Started | |
| `WasapiMicrophoneCapture` | ❌ Not Started | |
| Audio mixing in encoder | ❌ Not Started | |

### Stage 7: macOS & Linux Implementation — 🔴 Not Started

| Component | Status | Notes |
|-----------|--------|-------|
| Linux XDGPortalCaptureSource | ❌ Not Started | |
| macOS ScreenCaptureKit recording | ❌ Not Started | |

---

## Alignment Assessment with SIP0017

### ✅ Aligned

1. **Interface-based architecture**: All core interfaces defined in `ShareX.Avalonia.ScreenCapture.ScreenRecording`.
2. **Platform abstraction**: Windows implementations in `ShareX.Avalonia.Platform.Windows.Recording`.
3. **Factory pattern**: `CaptureSourceFactory` and `EncoderFactory` in ScreenRecorderService.
4. **Modern native APIs**: Windows.Graphics.Capture + Media Foundation as primary path.
5. **FFmpeg as fallback only**: FFmpegRecordingService defined but not primary.
6. **Exception-based fallback triggers**: PlatformNotSupportedException, COMException caught.

### ⚠️ Minor Deviations

1. **No DI container**: Uses static factory functions instead of `IServiceCollection`. Acceptable for current complexity.
2. **Dynamic dispatch for initialization**: `ScreenRecorderService.InitializeCaptureSource` uses `dynamic` to call platform-specific methods. Works but not type-safe.

---

## Resolved Gaps from SIP Review

| Gap ID | Resolution |
|--------|------------|
| #1 Missing enum definitions | ✅ All enums in `RecordingEnums.cs` |
| #2 PlatformManager undefined | ✅ Using static factory pattern instead (CaptureSourceFactory/EncoderFactory) |
| #3 IntPtr for window handle | ✅ Documented as cross-platform approach |
| #4 Config storage precedence | ⚠️ Model exists but not integrated into SettingManager |
| #5 Output file naming | ✅ Default pattern in `GetOutputPath()` |
| #6 CancellationToken support | ⚠️ Deferred (documented in interface comments) |

---

## Remaining Implementation Work

### Priority 1: Complete Stage 1 UI Integration

**Files to create/modify:**

1. **[NEW]** `src/ShareX.Avalonia.UI/ViewModels/RecordingViewModel.cs`
   - Manages recording state
   - Exposes `StartRecordingCommand`, `StopRecordingCommand`
   - Binds to `ScreenRecorderService`

2. **[MODIFY]** `src/ShareX.Avalonia.UI/ViewModels/MainViewModel.cs`
   - Add recording commands or reference to RecordingViewModel

3. **[NEW]** `src/ShareX.Avalonia.UI/Views/RecordingToolbarView.axaml`
   - Floating toolbar with Start/Stop button
   - Timer display during recording
   - Status indicator

### Priority 2: Configuration Persistence

**Files to modify:**

1. **[MODIFY]** `src/ShareX.Avalonia.Core/Settings/TaskSettings.cs`
   - ✅ Add `ScreenRecordingSettings` property

2. **[MODIFY]** `src/ShareX.Avalonia.Core/SettingManager.cs`
   - ✅ Ensure ScreenRecordingSettings serializes with WorkflowsConfig.json

### Priority 3: Stage 4 FFmpeg Fallback

**Files to create:**

1. **[NEW]** `src/ShareX.Avalonia.ScreenCapture/ScreenRecording/FFmpegRecordingService.cs`
   - Implements `IRecordingService`
   - Uses `FFmpegCLIManager` pattern
   - Wraps existing `FFmpegOptions`

2. **[MODIFY]** `src/ShareX.Avalonia.Platform.Windows/WindowsPlatform.cs`
   - Uncomment and complete `FallbackServiceFactory` registration

---

## Verification Plan

### Automated Build
```bash
dotnet build ShareX.Avalonia.sln
```

### Manual Testing (Stage 1 MVP)

1. **Start Recording Test**
   - Launch application
   - Click Start Recording button
   - Verify status changes to "Recording"
   - Wait 5 seconds
   - Click Stop Recording
   - Verify .mp4 file created in Documents/ShareX/Screenshots/yyyy-MM/

2. **Fallback Test (Stage 4)**
   - Rename `mfplat.dll` temporarily
   - Start recording
   - Verify fallback message in logs
   - Verify FFmpeg process started

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| WGC not available on older Windows | Medium | FFmpegRecordingService fallback |
| Media Foundation codec missing | Medium | Check IsAvailable before attempting |
| Frame rate mismatch between capture and encode | Low | Use timestamp from WGC, not fixed interval |
| Memory pressure from frame copies | Medium | Consider zero-copy GPU path in Stage 3 |

---

## Next Steps

1. Implement `RecordingViewModel` with commands
2. Integrate recording controls into MainWindow
3. Verify end-to-end recording works
4. Implement FFmpegRecordingService for fallback
5. Add settings persistence
