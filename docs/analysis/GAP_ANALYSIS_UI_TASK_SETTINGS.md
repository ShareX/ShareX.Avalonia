# Task Settings UI Gap Analysis

**Source:** `ShareX\Forms\TaskSettingsForm.cs` (WinForms)
**Target:** `src\ShareX.Avalonia.UI\Views\TaskSettingsPanel.axaml` (Avalonia)

## Executive Summary
The Avalonia implementation of the Task Settings UI is currently incomplete compared to the WinForms version. While basic General, Capture, and File Naming settings are present, significant sections such as Region Capture, Screen Recording, OCR, Watch Folders, and deep customization options (Toast window, Thumbnail, Quality) are missing.

The Avalonia version also adopts a different UX pattern for "Actions" (After Capture/Upload tasks), presenting them as direct checkboxes in the UI, whereas WinForms uses a MenuButton approach on the Task tab.

---

## 1. Task Tab (WinForms)
*Avalonia Status: **Missing / Redesigned***

The dedicated "Task" tab does not exist in the Avalonia implementation. Some functional equivalents are moved to the "Upload" tab, but most settings are missing.

| Control (WinForms) | Avalonia Equivalent | Status | Notes |
| :--- | :--- | :--- | :--- |
| **Screenshots Folder** | None | :x: MISSING | Path textbox, Browse button, Override checkbox. |
| **Custom Uploaders** | None | :x: MISSING | Selection combobox, Override checkbox. |
| **FTP Accounts** | None | :x: MISSING | Selection combobox, Override checkbox. |
| **Description** | None | :x: MISSING | Task description field. |
| **After Capture Tasks** | Upload Tab > "After capture" | :twisted_rightwards_arrows: MOVED | Converted from MenuButton list to checkboxes (`SaveImageToFile`, `CopyImageToClipboard`, etc.). |
| **After Upload Tasks** | Upload Tab > "After upload" | :twisted_rightwards_arrows: MOVED | Converted from MenuButton list to checkboxes (`CopyURLToClipboard`, etc.). |
| **Destinations** | None | :x: MISSING | MenuButton to configure Image/Text/File destinations. |
| **Override Settings** | None | :x: MISSING | Checkboxes for overriding global settings. |

## 2. General Tab (WinForms)
*Avalonia Status: **Partial***

Avalonia implements basic notification toggles but lacks the deep customization of the WinForms version.

### Main & Notifications
| Control (WinForms) | Avalonia Equivalent | Status | Notes |
| :--- | :--- | :--- | :--- |
| **Override General Settings** | None | :x: MISSING | |
| **Play Sound After Capture** | General > Notifications | :white_check_mark: MATCH | |
| **Show Toast Notification** | General > Notifications | :white_check_mark: MATCH | |
| **Custom Sound Paths** | None | :x: MISSING | Controls for custom paths for Capture, Task Completed, Error, Action sounds. |
| **Toast Window Settings** | None | :x: MISSING | Size, Duration, Fade, Placement, Click Actions, Auto-hide, Disable on fullscreen. |
| **Play Sound After Action** | None | :x: MISSING | |
| **Play Sound After Upload** | None | :x: MISSING | |

## 3. Image Tab (WinForms)
*Avalonia Status: **Significant Differences***

Avalonia focuses on Image Effects configuration here, missing standard quality/thumbnail settings.

### Quality & Thumbnail
| Control (WinForms) | Avalonia Equivalent | Status | Notes |
| :--- | :--- | :--- | :--- |
| **JPEG/PNG/GIF Quality** | None | :x: MISSING | Quality sliders, Bit depth, Auto-use JPEG threshold. |
| **Thumbnail Settings** | None | :x: MISSING | Width, Height, Name pattern, "Save thumbnail if smaller". |
| **Image Effects** | Image Tab (Full Editor) | :wrench: MODIFIED | WinForms has simple checkboxes. Avalonia embeds the full Effects Editor (Presets, List, PropertyGrid, Preview) directly into the tab. |

## 4. Capture Tab (WinForms)
*Avalonia Status: **Partial***

Basic "General" capture settings are implemented. Major features (Region Capture options, Screen Recorder, OCR) are completely absent.

### General
| Control (WinForms) | Avalonia Equivalent | Status | Notes |
| :--- | :--- | :--- | :--- |
| **Use Modern Capture** | Capture > General | :white_check_mark: MATCH | (Direct3D11) |
| **Show Cursor** | Capture > General | :white_check_mark: MATCH | |
| **Screenshot Delay** | Capture > General | :white_check_mark: MATCH | |
| **Transparent Capture** | Capture > General | :white_check_mark: MATCH | |
| **Window Shadow** | Capture > General | :white_check_mark: MATCH | |
| **Client Area Only** | Capture > General | :white_check_mark: MATCH | |
| **Auto Hide Icons/Taskbar** | None | :x: MISSING | Desktop icons and Taskbar toggles. |

### Region Capture
| Control (WinForms) | Avalonia Equivalent | Status | Notes |
| :--- | :--- | :--- | :--- |
| **Region Capture Settings** | None | :x: MISSING | Background dim, Fixed size, FPS limit, Crosshair, Magnifier settings, Detect controls/windows, Click actions. |

### Screen Recorder
| Control (WinForms) | Avalonia Equivalent | Status | Notes |
| :--- | :--- | :--- | :--- |
| **Screen Recorder Settings** | None | :x: MISSING | FPS, Duration, Start delay, Codec options (FFmpeg), GIF FPS, Auto-start. |

### OCR
| Control (WinForms) | Avalonia Equivalent | Status | Notes |
| :--- | :--- | :--- | :--- |
| **OCR Settings** | None | :x: MISSING | Auto-copy, Silent mode, Default language. |

## 5. Upload Tab (WinForms)
*Avalonia Status: **Partial***

File naming is mostly implemented. Advanced upload filters and clipboard upload settings are missing.

### File Naming
| Control (WinForms) | Avalonia Equivalent | Status | Notes |
| :--- | :--- | :--- | :--- |
| **Name Pattern** | Upload > File naming | :white_check_mark: MATCH | `NameFormatPattern` |
| **Active Window Pattern** | Upload > File naming | :white_check_mark: MATCH | `NameFormatPatternActiveWindow` |
| **Use Name Pattern for Upload** | Upload > File naming | :white_check_mark: MATCH | |
| **Replace Patterns** | Upload > File naming | :white_check_mark: MATCH | Replace problematic characters. |
| **URL Regex Replace** | Upload > File naming | :white_check_mark: MATCH | Pattern & Replacement fields. |
| **Auto Increment Number** | None | :x: MISSING | |
| **Time Zone Settings** | None | :x: MISSING | |

### Other Sections
| Control (WinForms) | Avalonia Equivalent | Status | Notes |
| :--- | :--- | :--- | :--- |
| **Clipboard Upload** | None | :x: MISSING | Specific settings for "Upload from Clipboard" tool (Share URL, URL contents, etc). |
| **Uploader Filters** | None | :x: MISSING | Filename/Extension based destination routing. |

## 6. Actions Tab
*Status: **Pending***
*   **WinForms**: Fully functional list of custom actions (external programs) with Add/Edit/Duplicate buttons.
*   **Avalonia**: `Actions` tab exists but contains only a placeholder.

## 7. Tools Tab
*Status: **Pending***
*   **WinForms**: Settings for tools like Color Picker (hex format, etc).
*   **Avalonia**: `Tools` tab exists but contains only a placeholder.

## 8. Watch Folders Tab
*Status: **Missing***
*   **WinForms**: Configuration for folder monitoring.
*   **Avalonia**: Tab does not exist.

## 9. Advanced Tab
*Status: **Pending***
*   **WinForms**: PropertyGrid exposing all settings.
*   **Avalonia**: `Advanced` tab exists but contains only a placeholder.
