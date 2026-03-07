# XIP0047 — Linux Region Capture: DPI and Performance Issues

**Status**: Open (mitigations implemented; some issues may persist)
**Priority**: High
**Affected platform**: Linux (Wayland, mixed-DPI)
**Related**: Commit `58283cb13900be85ede524022c5d5dc46877eebd`, KNOWN_ISSUES.md, XIP0046

---

## Context: Portal vs In-App Overlay (commit 58283cb)

Up to and including commit `58283cb13900be85ede524022c5d5dc46877eebd`, region capture on Linux used the **XDG Portal** (system screenshot dialog) for region selection. After that commit, XerahS uses its **in-app overlay with crosshair** for region selection by default. The overlay path can exhibit:

- **DPI/positioning issues** in mixed-DPI setups (overlay squashed, shifted, or crop misaligned).
- **Performance issues** (slow overlay appearance, sluggish crosshair, delay before the crosshair receives pointer events).

This XIP summarises those issues and all attempts tried to fix or mitigate them.

---

## DPI Issues and Attempts

### Issue 1 — Mixed-DPI overlay squashing (non-Windows)

**Problem**: In mixed-DPI setups, one monitor’s overlay was visually “squashed” because overlay size/position was using physical pixels while the compositor expected logical layout.

**Attempts**:

1. **WaylandMonitorLayoutNormalizer for all non-Windows**  
   Use the normaliser not only on Wayland but for all non-Windows. For X11, convert Avalonia physical `Screen.Bounds` to logical via `PhysicalToLogicalRect` and pass to the normaliser. Wayland/macOS: keep Avalonia bounds as logical.  
   **Files**: `MonitorEnumerationService.cs`, `MonitorInfo.cs`, `WaylandMonitorLayoutNormalizer.cs`  
   **Result**: Overlay uses **logical** bounds (`OverlayBounds`); capture continues to use **physical** bounds. Overlay placement and aspect ratio are correct across mixed-DPI.

2. **Virtual screen bounds for crop (portal vs selection coordinate system)**  
   On Linux, when using the overlay path and no pre-capture, we call `CaptureRectAsync(selection)`. The portal returns a full-screen bitmap (often in **logical** pixels). The selection rect is in **physical** pixels. Using it directly produced invalid crop (e.g. width -159).  
   **Attempts**:
   - **VirtualScreenBoundsForCrop** (`CaptureOptions`): pass a “virtual screen” rectangle so the Linux capture service can map the selection from that space to the bitmap. Initially used union of `PhysicalBounds` from `MonitorEnumerationService` (same as overlay).  
   - **Logical coordinates on Linux**: Portal screenshot is in logical pixels. Use union of **OverlayBounds** (logical) for `VirtualScreenBoundsForCrop`, and convert the **selection** from physical to logical (using monitor at selection centre and that monitor’s scale/OverlayBounds) before calling `CaptureRectAsync`.  
   **Files**: `CaptureOptions.cs`, `ScreenCaptureService.cs`, `LinuxScreenCaptureService.cs`  
   **Result**: Crop mapping matches portal bitmap; captured region aligns with user selection. Windows path unchanged (physical bounds and selection only when not Linux).

### Issue 2 — Overlay window shift on X11 (mixed-DPI, non-primary monitors)

**Problem**: On X11 with mixed-DPI monitors, the overlay for a secondary monitor appeared shifted. Root cause: `Window.Position` on X11 expects **physical pixels**, but the code was using `OverlayBounds.X/Y` (logical coordinates). When the secondary monitor has a different DPI from the primary, `logicalX ≠ physicalX` and the window was placed on the wrong screen.

**Fix**: In `OverlayWindow.axaml.cs` constructor, detect X11 (Linux and not Wayland) via `MonitorEnumerationService.IsWaylandSession()` (changed from `private` to `internal`). Use `PhysicalBounds.X/Y` for `Position` on X11; keep `OverlayBounds.X/Y` on Wayland and all other platforms (where `Window.Position` uses compositor logical coordinates matching `Screen.Bounds`).

**Files**: `OverlayWindow.axaml.cs`, `MonitorEnumerationService.cs`

**Known limitation**: On Wayland, `Window.Position` is documented as physical pixels but the Avalonia Wayland backend uses compositor logical coordinates internally. Using `OverlayBounds.X/Y` (logical) is safe and matches `Screen.Bounds`. Using `PhysicalBounds.X/Y` (physical) could place windows off-screen on Wayland with HiDPI. No code change applied for Wayland.

### Issue 3 — Wrong crop on KDE Plasma (physical-resolution portal bitmap, mixed-DPI)

**Problem**: On KDE Plasma, the XDG portal returns a **physical-resolution** screenshot. The existing crop path assumed a **logical-resolution** bitmap (valid for GNOME) and applied a single linear scale (`bitmap / logicalVirtual`) to map the selection. For mixed-DPI setups this scale is a non-uniform average and produces an incorrect crop.

**Fix**:
- `CaptureOptions.cs`: Added `PhysicalVirtualScreenBoundsForCrop` (physical virtual screen union) and `PhysicalRectForCrop` (selection in physical pixels).
- `ScreenCaptureService.cs` (Linux path): After setting `VirtualScreenBoundsForCrop`, also compute `PhysicalVirtualScreenBoundsForCrop` (union of `monitor.PhysicalBounds`) and set `PhysicalRectForCrop = selection` (the raw physical selection).
- `LinuxScreenCaptureService.CaptureRectAsync`: Before the existing logical path, check whether the portal bitmap dimensions match `PhysicalVirtualScreenBoundsForCrop` (within 2% tolerance). If so, use `PhysicalRectForCrop` directly offset by the physical virtual origin — no scale needed. If not, fall through to the existing logical path (GNOME).

**Files**: `CaptureOptions.cs`, `ScreenCaptureService.cs`, `LinuxScreenCaptureService.cs`

**Known limitation**: The 2% size tolerance heuristic could produce false positives on certain multi-monitor configurations where logical and physical bitmap sizes happen to be close. No per-monitor physical-to-logical mapping is attempted when the size check fails; the existing logical path is used as fallback.

---

## Performance Issues and Attempts

### Issue 1 — Pre-capture delay before overlay

**Problem**: Full-screen portal capture before showing the overlay took ~1.2 s, so the overlay appeared late.

**Attempts**:
- **Fast overlay on Linux**: When `UseTransparentOverlay` is true or on Linux, skip pre-capture and show the overlay immediately. After the user selects a region, use `CaptureRectAsync` (portal full-screen + crop) when there is no pre-capture bitmap.  
- **Files**: `ScreenCaptureService.cs` (`useFastOverlay = options?.UseTransparentOverlay ?? OperatingSystem.IsLinux()`).  
**Result**: Overlay appears much sooner on Linux; capture happens after selection.

### Issue 2 — Sluggish crosshair

**Problem**: Crosshair felt sluggish when moving the pointer (high compositor/CPU load from redraws).

**Attempts**:
- **Throttle crosshair redraws** to ~60 FPS in `RegionCaptureControl.OnPointerMoved` using `Stopwatch` and `CrosshairInvalidateIntervalTicks`; only call `InvalidateVisual()` when the interval has elapsed.  
- **Cache crosshair pens** in `DrawCrosshair` (`_crosshairLinePen`, `_crosshairPen`) to avoid per-frame allocations.  
- **Reset throttle on pointer press** (`_lastCrosshairInvalidateTicks = 0`) so the first frame after click is immediate.  
**Files**: `RegionCaptureControl.cs`  
**Result**: Smoother crosshair with lower CPU/compositor load.

### Issue 3 — Delay before crosshair receives pointer events (~2.4 s)

**Problem**: After “overlay displayed”, the overlay did not receive pointer events for ~2.4 s, so the crosshair did not move until then. User had to wait for the crosshair before drawing the rectangle.

**Root cause**: On Linux/Wayland, the compositor often does not grant focus (and thus pointer events) to the overlay window immediately after `Show()`/`Activate()`/`Focus()` (see also Avalonia issue #8099: “Window.Activate does not focus and bring Window to front” on Linux).

**Attempts**:
1. **Show and focus primary overlay first**  
   In `OverlayManager`, show and activate the **primary** overlay and call `Focus()` on it before showing other overlays, so the compositor has one clear focus target.  
   **File**: `OverlayManager.cs`

2. **Delayed focus retries in OverlayWindow**  
   In `OnOpened`, call `this.Focus()` and `_captureControl.Focus()`, then schedule focus retries at **50 ms, 200 ms, and 500 ms** (via `Task.Delay` and `Dispatcher.UIThread.Post` with `DispatcherPriority.Input`). If the window is closed (`_windowClosed` set in `OnClosed`), skip further retries.  
   **File**: `OverlayWindow.axaml.cs`

3. **Focus both window and control in OnOpened**  
   Ensure both the overlay window and the capture control receive `Focus()` so the control that handles pointer events can get input as soon as the compositor grants it.  
   **File**: `OverlayWindow.axaml.cs`

**Result**: “First pointer moved” milestone can move closer to “overlay displayed”; crosshair becomes responsive sooner. Effectiveness depends on compositor and timing.

### Issue 4 — Bottleneck visibility

**Problem**: Need to see where time is spent (pre-capture, overlay show, first input, etc.) to prioritise fixes.

**Attempts**:
- **Milestone logging**: Add timestamps at key points (region capture started, region capture UI invoked, overlay displayed, overlay control attached to visual tree, first pointer moved, first mouse down, mouse up, selection confirmed, region UI returned, post-overlay delay done, CaptureRectAsync called/returned, bitmap obtained). Use a session start time (`SessionStartUtc` in `RegionCaptureOptions`) so each milestone logs “+X ms”.  
- **DebugHelper.Flush()** after region capture so all lines appear in the Debug Log when the user copies.  
**Files**: `ScreenCaptureService.cs`, `OverlayManager.cs`, `RegionCaptureControl.cs`, `RegionCaptureService.cs`  
**Result**: Logs show where the bottleneck is (e.g. gap between overlay displayed and first pointer moved).

---

## Option: Use XDG Portal Instead of Overlay (UseModernCapture)

To restore the **pre-58283cb** behaviour (portal/system dialog for region capture) and avoid overlay DPI/performance issues:

- **UseModernCapture = true (checkbox checked)** on Linux: delegate region capture to the platform; `_platformImpl.CaptureRegionAsync(options)` is called. On Linux this uses the capture coordinator with `LinuxCaptureKind.Region` (portal or system dialog) and returns the bitmap directly. No in-app overlay.  
- **UseModernCapture = false (checkbox unchecked)** on Linux: use the in-app overlay with crosshair (current default).  
- If the platform call throws, the UI falls back to the overlay path.

**Files**: `ScreenCaptureService.CaptureRegionAsync`, KNOWN_ISSUES.md

---

## Summary Table

| Area            | Issue                                         | Attempt / fix                                                                                          | Status      |
|----------------|-----------------------------------------------|--------------------------------------------------------------------------------------------------------|-------------|
| DPI            | Overlay squashing (mixed-DPI)                 | OverlayBounds (logical) for overlay; normaliser for all non-Windows                                   | Implemented |
| DPI            | Wrong crop — GNOME (logical portal bitmap)    | VirtualScreenBoundsForCrop = OverlayBounds union; selection → logical on Linux                        | Implemented |
| DPI            | Wrong crop — KDE (physical portal bitmap)     | PhysicalVirtualScreenBoundsForCrop + PhysicalRectForCrop; physical-bitmap detection in Linux service  | Implemented |
| DPI            | Overlay window shift on X11 (mixed-DPI)       | Use PhysicalBounds.X/Y for Window.Position on X11; keep OverlayBounds on Wayland                     | Implemented |
| DPI            | Wayland position API ambiguity                | OverlayBounds (logical) used; physical would place window off-screen on HiDPI Wayland                | Known limit |
| DPI            | Vertical monitor stacking Y-bug (normaliser)  | Normaliser uses (logicalY − minLogicalY)×scale; wrong for vertical mixed-DPI stacking                | Known limit |
| Performance    | Pre-capture delay                             | Fast overlay on Linux (skip pre-capture)                                                              | Implemented |
| Performance    | Sluggish crosshair                            | Throttle redraws ~60 FPS; cache pens; reset on press                                                  | Implemented |
| Performance    | ~2.4 s until first pointer move               | Primary overlay first; delayed focus retries (50/200/500 ms)                                          | Implemented |
| Observability  | Bottleneck visibility                         | Milestone logging + flush                                                                              | Implemented |
| User choice    | Prefer portal over overlay                    | UseModernCapture = true → platform (portal); false → overlay                                          | Implemented |

---

## References

- Commit `58283cb13900be85ede524022c5d5dc46877eebd` (switch to in-app overlay for region capture on Linux)
- KNOWN_ISSUES.md — “Region Capture / Screenshot” (Linux)
- XIP0046 — Linux Portal & Hotkey Issues
- Avalonia #8099 — Linux Window.Activate does not focus
