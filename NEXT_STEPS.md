# ShareX.Avalonia - Next Steps Implementation Plan

## Current Status

**Progress: 40.7% Complete** (109/268 items)
- ✅ ShareX.HelpersLib: ~95% complete (backend utilities)
- ✅ Build Status: 0 errors, 719 platform-specific warnings
- ✅ Git: Committed (b4a25d5) and pushed to master

## Recommended Next Steps

### Phase 2A: Platform Abstraction Layer ✅ COMPLETE

**Goal**: Enable cross-platform support for platform-specific APIs

#### Completed Tasks

1. **Created Platform Projects** ✅
   ```
   ShareX.Avalonia.Platform.Abstractions/
   ├── IScreenService.cs ✅
   ├── IClipboardService.cs ✅
   ├── IWindowService.cs ✅
   ├── IPlatformInfo.cs ✅
   └── PlatformServices.cs ✅ (Service Locator)
   
   ShareX.Avalonia.Platform.Windows/
   ├── WindowsScreenService.cs ✅
   ├── WindowsClipboardService.cs ✅
   ├── WindowsWindowService.cs ✅
   ├── WindowsPlatformInfo.cs ✅
   └── WindowsPlatform.cs ✅ (Initialization Helper)
   ```

2. **Platform Services Implemented** ✅
   - All Windows implementations complete
   - Service locator pattern for easy access
   - Ready for Linux/macOS stub implementations

**Status**: ✅ Complete (2 days)
**Next**: Port CaptureHelpers to use IScreenService, then begin Phase 2B

---

### Phase 2A.1: MVVM Architecture ✅ COMPLETE

**Goal**: Establish clean separation between business logic and UI

#### Completed Tasks

1. **Created New Projects** ✅
   ```
   ShareX.Avalonia.Services.Abstractions/   ← Service interfaces (UI operations)
   ├── IFileDialogService.cs ✅
   ├── IDialogService.cs ✅
   └── INotificationService.cs ✅
   
   ShareX.Avalonia.ViewModels/              ← MVVM ViewModels
   └── ViewModelBase.cs ✅ (ReactiveUI integration)
   
   ShareX.Avalonia.Services/                ← Service implementations (future)
   ```

2. **Architecture Benefits** ✅
   - Separation of concerns (business logic vs UI)
   - Testability (mockable services)
   - Cross-platform ready
   - ReactiveUI MVVM pattern
   - Dependency injection ready

**Status**: ✅ Complete (0.5 days)
**Documentation**: MVVM_ARCHITECTURE.md created
**Next**: Implement service implementations when building UI

---

### Phase 2B: ShareX.UploadersLib Porting (High Priority)

**Goal**: Port uploader infrastructure for core ShareX functionality

#### Tasks

1. **Port Core Uploader Classes**
   - `GenericUploader.cs`
   - `FileUploader.cs`
   - `ImageUploader.cs`
   - `TextUploader.cs`
   - `URLShortener.cs`

2. **Port OAuth Infrastructure**
   - `OAuth2Info.cs`
   - `OAuthInfo.cs`
   - `OAuth2.cs`
   - `OAuth.cs`

3. **Port Popular Uploaders** (Priority Order)
   - Imgur
   - Google Drive
   - Dropbox
   - Amazon S3
   - Custom uploader

**Estimated Effort**: 5-7 days

---

### Phase 2C: Avalonia UI Design (Medium Priority)

**Goal**: Design Avalonia UI components to replace WinForms controls

#### Deferred UI Components

From ShareX.HelpersLib:
- `BlackStyleCheckBox.cs`
- `BlackStyleProgressBar.cs`
- `Canvas.cs`
- `ColorBox.cs`
- `ColorPicker.cs`
- `ColorSlider.cs`
- `CustomVScrollBar.cs`
- `PrintHelper.cs` / `PrintTextHelper.cs`

#### Design Approach

1. **Create Avalonia Styles**
   - Dark theme support
   - Custom control templates
   - Consistent visual language

2. **Implement Custom Controls**
   - Use Avalonia's styling system
   - Leverage SkiaSharp for custom rendering
   - Ensure cross-platform compatibility

3. **Port Print Functionality**
   - Use platform-specific print dialogs
   - Abstract printer selection
   - Support PDF export as fallback

**Estimated Effort**: 7-10 days

---

### Phase 3: Additional Libraries (Lower Priority)

#### ShareX.HistoryLib
- `HistoryItemManager.cs`
- History persistence and search

**Estimated Effort**: 2-3 days

#### ShareX.ImageEffectsLib
- Image effect pipeline
- Filters and transformations
- Watermarking

**Estimated Effort**: 4-5 days

#### ShareX.MediaLib
- FFmpeg integration
- Video encoding
- Image beautification

**Estimated Effort**: 5-7 days

#### ShareX.ScreenCaptureLib
- Region capture
- Annotation tools
- Shape management

**Estimated Effort**: 10-14 days (complex)

---

## Immediate Action Items

### Week 1: Platform Abstraction
1. Create `ShareX.Avalonia.Platform.Abstractions` project
2. Define `IScreenService`, `IClipboardService`, `IWindowService`
3. Implement Windows versions in `ShareX.Avalonia.Platform.Windows`
4. Port `CaptureHelpers.cs` using new abstractions
5. Update `ShareX.Avalonia.Common` to use platform services

### Week 2: Uploader Infrastructure
1. Create `ShareX.Avalonia.Uploaders` project
2. Port core uploader base classes
3. Port OAuth infrastructure
4. Port 3-5 popular uploaders (Imgur, Google Drive, Dropbox)
5. Implement uploader configuration system

### Week 3: UI Foundation
1. Design Avalonia theme and styles
2. Create basic window/dialog infrastructure
3. Implement settings UI
4. Port 2-3 simple custom controls
5. Test cross-platform rendering

---

## Success Criteria

- ✅ Platform abstraction allows compilation on Linux/macOS
- ✅ At least 5 uploaders functional
- ✅ Basic Avalonia UI displays and accepts user input
- ✅ No build errors, reduced platform-specific warnings
- ✅ Progress reaches 60%+ completion

---

## Notes

- **Platform Abstraction** is critical for cross-platform support
- **Uploaders** are core functionality - prioritize early
- **UI Design** can proceed in parallel with backend work
- **Testing** should include Windows, Linux, and macOS where possible
- **Documentation** should be updated as features are ported
