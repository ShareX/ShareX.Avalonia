# XIP0046 — Linux Portal & Hotkey Issues

**Status**: OPEN
**Priority**: High
**Related**: Issues [#63](https://github.com/ShareX/XerahS/issues/63), [#64](https://github.com/ShareX/XerahS/issues/64)

---

## Problem Statement

GitHub issues #63 and #64 document a cluster of related Linux platform problems affecting hotkey registration and XDG Desktop Portal screenshot workflows. This XIP consolidates the distinct issues from both reports with researched fix guidance.

---

## Issues Identified

### Issue A — Print Key Cannot Be Registered as Hotkey (X11/XWayland)

**Source**: [#63](https://github.com/ShareX/XerahS/issues/63)
**Severity**: High
**Status**: Fix pushed (`aa579f0`), awaiting tester confirmation

**Problem**: Avalonia reports the Print Screen key as `Key.Print` (value 28), but `LinuxHotkeyService.SpecialKeyNames` only maps `Key.PrintScreen` (value 30) to the X11 keysym `"Print"`. When a user presses Print Screen, the key lookup fails and registration returns `false`.

**Evidence from logs**:
```
[Hotkey] OnPreviewKeyDown: Key=Print, Mods=None, Mode=Recording
LinuxHotkeyService: Unable to map key Print
```

**Root cause**: Avalonia's `Key` enum has two separate entries — `Key.Print` (28) and `Key.PrintScreen` / `Key.Snapshot` (30). On Linux X11, the physical Print Screen key is reported as `Key.Print`, not `Key.PrintScreen`.

#### Fix Guidance

**File**: `src/XerahS.Platform.Linux/Services/LinuxHotkeyService.cs`

1. **Add `Key.Print` → `"Print"` mapping** to `SpecialKeyNames`:
   ```csharp
   { Key.PrintScreen, "Print" },
   { Key.Print, "Print" },       // Avalonia reports PrintScreen as Key.Print on Linux
   { Key.Snapshot, "Print" },    // alias — defensive mapping
   ```

2. **Also add `Key.Snapshot`** as a defensive alias, since `Key.Snapshot` is defined as value 30 (same as `Key.PrintScreen`) in Avalonia but some keyboard layouts or future Avalonia versions could route through it.

3. **Consider a fallback strategy**: If `SpecialKeyNames` lookup fails, attempt `XStringToKeysym(key.ToString())` as a last-resort conversion before returning `IntPtr.Zero`. This would catch future unmapped keys without requiring code changes.

4. **Verify with `xev`** on the target system that keycode 107 resolves to keysym `Print` (`0xff61`). Some keyboard layouts may map Print Screen to `Sys_Req` when combined with Alt.

> [!IMPORTANT]
> A fix was already pushed in commit `aa579f0`. The fix needs tester confirmation — see test matrix in issue #63 comment.

---

### Issue B — XDG Portal Screenshot UI Varies Across Desktop Environments

**Source**: [#64](https://github.com/ShareX/XerahS/issues/64)
**Severity**: Medium (expected behavior, but UX impact)
**Status**: Open — documentation + mitigation needed

**Problem**: The XDG Portal `Screenshot` method produces different UIs depending on the portal backend (`xdg-desktop-portal-kde`, `-gtk`, `-wlr`). Users report a "weird looking" dialog that doesn't match their desktop theme or expected workflow.

**Root cause**: The XDG Portal specification defines the API contract (`Screenshot(parent_window, options)`), but each backend implements its own UI. The `interactive` option is a **hint**, not a command — the compositor decides what UI to show. This is by design in the Freedesktop specification.

#### Fix Guidance

This is **not a bug in XerahS** but requires mitigation:

1. **Document the behavior** in user-facing docs:
   - Explain that the portal UI is provided by the desktop environment, not XerahS.
   - Provide screenshots of what the portal looks like on KDE, GNOME, and wlroots-based compositors.
   - Link to troubleshooting for users who see mismatched themes (e.g., GTK portal on KDE).

2. **Log which portal backend responds** at startup (already implemented in `ee6d0fa`):
   - Detect running backends via `busctl --user list`.
   - Log portal routing hints from `$XDG_CURRENT_DESKTOP`.
   - Check for `portals.conf` in user/system paths.

3. **Offer `portals.conf` guidance** for users who want to force a specific backend:
   ```ini
   # ~/.config/xdg-desktop-portal/portals.conf
   [preferred]
   default=kde
   org.freedesktop.impl.portal.Screenshot=kde
   ```

4. **Consider a settings option** to let users choose between portal-based capture and CLI tool fallback (e.g., `grim + slurp` on wlroots, `spectacle --region` on KDE).

---

### Issue C — Portal Region Selection Not Available on Some KDE Backends

**Source**: [#64](https://github.com/ShareX/XerahS/issues/64) — comments by Lu9-ST and shindouj
**Severity**: High
**Status**: Open — core UX blocker for region capture on KDE

**Problem**: On KDE Plasma, the portal's `Screenshot` dialog (with `interactive=true`) shows a "Request screenshot" window that only captures the full screen. There is **no region selection** option. The portal window itself sometimes appears in the captured screenshot. Users must then crop in the XerahS editor, which is a poor workflow.

**Root cause**: The `interactive` option in the Portal Screenshot API is a hint — KDE's `xdg-desktop-portal-kde` often delegates to Spectacle's portal integration, which may not expose rectangle selection through the portal dialog. The Freedesktop specification explicitly states: *"Whether the dialog should offer customization before taking a screenshot"* — it does **not** guarantee region selection.

#### Fix Guidance

1. **Use the ScreenCast portal as an alternative for region capture**:
   The `org.freedesktop.portal.ScreenCast` API allows PipeWire-based screen capture with compositor-side source selection. XerahS could:
   - Open a ScreenCast session requesting a single frame.
   - Let the user select a monitor/window via the portal's source picker.
   - Capture the frame from PipeWire and then crop to a user-selected region in the XerahS image editor.

   This approach works on all Wayland compositors with PipeWire support and sidesteps the Screenshot portal's limitations.

2. **Provide CLI tool fallback** for region capture on specific DEs:
   | Desktop Environment | CLI Command | Notes |
   |---|---|---|
   | KDE Plasma | `spectacle --region --nonotify --output <path>` | Native KDE tool with rectangle selection |
   | wlroots (Sway, Hyprland) | `grim -g "$(slurp)" <path>` | Standard wlroots capture pipeline |
   | GNOME | `gnome-screenshot -a -f <path>` | Interactive area selection |

   Detection flow:
   1. Check `$XDG_CURRENT_DESKTOP` for DE.
   2. Check if the CLI tool is available via `which`.
   3. If available, offer it as a capture provider alongside the portal.

3. **Add a "Capture method" preference** in settings:
   - `Auto (Portal)` — current default
   - `CLI Tool` — uses DE-specific CLI tool
   - `Portal + Editor crop` — takes full screenshot via portal, opens editor for cropping

4. **Hide the portal window before capture**: If using the `Screenshot` portal, consider adding a short delay after the portal dialog closes to ensure the portal window is not captured in the screenshot. Alternatively, set `modal=true` to let the compositor handle window stacking.

> [!WARNING]
> KDE's portal Screenshot behavior fixed a self-capture bug in Plasma 6.4.2. Users on older versions will see the portal window in their screenshots. Recommend users update `xdg-desktop-portal-kde` to ≥ 6.4.2.

---

### Issue D — GlobalShortcuts Portal Hotkey Silently Fails to Fire

**Source**: [#64](https://github.com/ShareX/XerahS/issues/64) — Bo0sted's Phase 2 report
**Severity**: High
**Status**: Covered by [XIP0044](file:///c:/Users/liveu/source/repos/ShareX%20Team/XerahS/tasks/XIP0044-linux-global-hotkeys-not-firing-when-app-backgrounded.md)

**Problem**: `WaylandPortalHotkeyService` successfully registers hotkeys via the `GlobalShortcuts` portal (`Response=0`, `BindShortcuts` success), but pressing the bound key produces no `Activated` signal. The hotkey is silently bound but never fires.

> [!IMPORTANT]
> This issue is **the same root cause** identified in XIP0044. The Bo0sted Phase 2 report described
> `BindShortcuts response=0` with hotkeys "successfully registered" but never firing. XIP0044's
> deep-dive found THREE contributing causes — all now fixed:
>
> 1. **App ID mismatch** (`"XerahS"` vs `"xerahs"`) → portal rejected bind silently (Fix 1, commit `4413c031`)
> 2. **`parentWindow=<empty>` startup race** → compositor accepted bind but didn't route events (Fix 5, current branch)
> 3. **CTS `ObjectDisposedException`** in debounce → rebind silently crashed (Fix 3, commit `1cb75370`)
> 4. **Packaging symlink** → `xdg-desktop-portal` couldn't match exe to `.desktop` file (Fix 2, commit `271265ca`)
>
> See XIP0044 for full root cause analysis, code changes, and verification steps.

#### No additional fix needed

All fixes for this issue are tracked in XIP0044. The "compositor key conflict" hypothesis from
this XIP was superseded by the confirmed `parentWindow` startup race in XIP0044.

---

### Issue E — InputCapture Portal Session Creation Fails (Error 2)

**Source**: [#64](https://github.com/ShareX/XerahS/issues/64) — Bo0sted's Phase 2 report
**Severity**: Low (non-fatal, app continues with fallback)
**Status**: Open — cosmetic/logging issue

**Problem**: The `InputCapture` portal interface is present on D-Bus, but `CreateSession` returns error code 2. The `WaylandPortalInputService` logs:
```
WaylandPortalInputService: CreateSession failed (2)
```

**Root cause**: Error code 2 in the XDG Portal spec typically means the portal backend rejected the request. On KDE Plasma, the `InputCapture` portal may:
- Not be fully implemented in the KDE backend
- Require additional Flatpak/Snap sandbox permissions
- Need compositor-specific capabilities not available to native apps

#### Fix Guidance

1. **Graceful degradation (already working)**: The app correctly falls back to the portal hotkey service. No action required for functionality.

2. **Improve logging clarity**:
   ```csharp
   // Instead of:
   Log("WaylandPortalInputService: CreateSession failed (2)");
   // Use:
   Log("WaylandPortalInputService: CreateSession rejected by portal backend (response=2). " +
       "This is expected on KDE Plasma — InputCapture support varies by compositor. " +
       "Falling back to GlobalShortcuts portal.");
   ```

3. **Skip probing on known-unsupported backends**: If the detected portal backend is `xdg-desktop-portal-kde` and the KDE Frameworks version is < 6.x, skip the InputCapture probe entirely to avoid log noise.

4. **Check `portals.conf`** to ensure the `InputCapture` interface isn't being routed to the wrong backend (e.g., GTK backend on KDE).

---

### Issue F — Cancelling Portal Opens Spectacle Unexpectedly

**Source**: [#64](https://github.com/ShareX/XerahS/issues/64) — shindouj comment, Bo0sted confirmation
**Severity**: Medium
**Status**: Fix pushed (`ee6d0fa`), awaiting tester confirmation

**Problem**: When the user cancels the portal Screenshot dialog on KDE, Spectacle opens unexpectedly. This is disruptive and confusing. One user (`Bo0sted`) reports that even removing all Spectacle hotkey bindings in KDE settings does not prevent Spectacle from opening.

**Root cause**: KDE's portal backend internally delegates screenshot requests to Spectacle. When the portal request is cancelled (Response=1), some versions of `xdg-desktop-portal-kde` still signal Spectacle to open. This is a known KDE bug, partially fixed in `xdg-desktop-portal-kde` ≥ 6.4.2.

Additionally, XerahS's previous code would fall back to CLI capture tools after a portal cancel, which could indirectly invoke Spectacle via the system's default screenshot tool chain.

#### Fix Guidance

1. **Respect portal cancel — no fallback (already fixed)**:
   Commit `ee6d0fa` addresses this:
   ```csharp
   if (result.IsCancelled)
   {
       trace.AddStep(stage, provider.ProviderId, CaptureDecisionOutcome.Cancelled);
       trace.Complete(provider.ProviderId, CaptureDecisionOutcome.Cancelled);
       return new LinuxCaptureExecutionResult(result, trace);  // stops chain
   }
   ```
   After a portal cancel, XerahS now stops immediately without trying CLI tools.

2. **Upstream KDE fix**: Recommend users update to `xdg-desktop-portal-kde` ≥ 6.4.2, which fixes the interactive screenshot portal self-capture and cancel handling bugs.

3. **Add user-facing note**: In the XerahS troubleshooting guide, document:
   - If Spectacle opens after cancelling a region capture, update `xdg-desktop-portal-kde`.
   - If Spectacle continues to open after updating, check for system-level Spectacle shortcuts in KDE Settings → Shortcuts → Spectacle and remove/rebind them.

> [!NOTE]
> The fix in `ee6d0fa` prevents XerahS from *causing* the Spectacle launch. However, some KDE systems may still launch Spectacle server-side when the portal Screenshot interface is invoked — this is a KDE bug outside XerahS's control.

---

## Summary of Fix Status

| Issue | Description | Fix Status | Commit |
|-------|------------|------------|--------|
| **A** | Print key hotkey mapping | Fix pushed, needs testing | `aa579f0` |
| **B** | Portal UI varies by DE | Documentation + logging done | `ee6d0fa` |
| **C** | Portal lacks region selection on KDE | **Open** — needs ScreenCast alternative or CLI fallback |  |
| **D** | GlobalShortcuts hotkey doesn't fire | **Open** — needs conflict detection + diagnostics |  |
| **E** | InputCapture CreateSession fails | **Open** — logging improvement needed (non-fatal) |  |
| **F** | Cancel portal opens Spectacle | Fix pushed, needs testing | `ee6d0fa` |

---

## Verification Plan

### For Issues A and F (already fixed)
- Request tester confirmation on issues #63 and #64 per the existing test matrices.
- Close the issues once testers verify on KDE + GNOME + wlroots backends.

### For Issues C and D (open)
- Prototype ScreenCast-based single-frame capture for region selection.
- Add CLI tool fallback providers for `spectacle`, `grim+slurp`, `gnome-screenshot`.
- Test GlobalShortcuts on a clean key binding (e.g., `F9`) to isolate compositor conflicts.
- Add D-Bus conflict detection for `kglobalaccel`.

### For Issue E (open, low priority)
- Improve log message clarity.
- Consider skipping InputCapture probe on KDE backends.
