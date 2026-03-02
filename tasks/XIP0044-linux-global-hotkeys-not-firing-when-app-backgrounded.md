# XIP0044 Linux Global Hotkeys Not Firing When App Is Backgrounded

**Status**: In Progress
**Priority**: High
**Affected platform**: Linux (Wayland / XWayland)
**Related**: XIP0029 (Wayland portal D-Bus errors)

---

## Problem Statement

On Linux, registered hotkeys only trigger when XerahS has focus. When the app is minimised
or another window is active, no hotkey fires. This renders the core screenshot/recording
shortcuts useless for normal day-to-day use.

---

## Root Cause Analysis

### Why the portal path is the only correct solution

XerahS runs as an Avalonia Wayland-native process (under XWayland for display rendering).
The hotkey subsystem has two backends:

| Backend | API | Global? | XWayland limitation |
|---|---|---|---|
| `LinuxHotkeyService` (X11 fallback) | `XGrabKey` / `XNextEvent` | Only for X11 compositor | Events never delivered when a Wayland-native window has focus |
| `WaylandPortalHotkeyService` | XDG GlobalShortcuts portal | Yes — compositor-level | No limitation; compositor delivers via D-Bus signal |

`XGrabKey` registers successfully (kernel sees the key), but the X11 event queue is only
served when an X11-window (or XWayland) surface has keyboard focus. Under a full Wayland
session (GNOME Shell), most windows are Wayland-native, so hotkeys appear "broken" for
the majority of the desktop lifetime.

The **XDG GlobalShortcuts portal** (`org.freedesktop.portal.GlobalShortcuts`) is the
correct solution: the compositor (GNOME Shell ≥ 45 / KDE Plasma ≥ 6) owns the binding and
delivers an `Activated` D-Bus signal regardless of which surface has focus.

### Why the portal fallback is currently being triggered (v0.19.2 installed)

The portal `BindShortcuts` call returns `response=2 (Failed)` immediately without showing
any GNOME permission dialog. The root cause is an **app ID mismatch**:

1. Avalonia sets the Wayland `xdg_toplevel.app_id` from `Application.Name` if set, or
   `Process.GetCurrentProcess().ProcessName` otherwise.
2. Without `Application.Name` set, the process name resolves to `"XerahS"` (capital X).
3. `xdg-desktop-portal` looks for `"XerahS.desktop"` — not found.
4. It also tries matching `/proc/PID/exe` against `Exec=` fields of all `.desktop` files.
   The installed `.desktop` file (`xerahs.desktop`) has `Exec=/usr/bin/xerahs`, but
   `/usr/bin/xerahs` does not exist (packaging bug — binary is at `/usr/lib/xerahs/XerahS`).
5. With no app ID resolved, GNOME rejects `BindShortcuts` with `response=2`.
6. `WaylandPortalHotkeyService` activates X11 fallback → hotkeys broken globally.

### Secondary bug: CTS `ObjectDisposedException` in debounce

Commit `1db0a454` introduced `ScheduleRebind()` which debounces portal rebinds. The
implementation called `old?.Dispose()` synchronously on the previous
`CancellationTokenSource`. Because `Task.Run` queues the lambda asynchronously, call N−1's
lambda may not have started executing yet when call N disposes its CTS. When it eventually
runs, `cts.Token` throws `ObjectDisposedException`.

Observed in log (2026-03-02):
```
WaylandPortalHotkeyService: Failed to rebind shortcuts:
System.ObjectDisposedException: The CancellationTokenSource has been disposed.
  at System.Threading.CancellationTokenSource.get_Token()
  at ...WaylandPortalHotkeyService.<<ScheduleRebind>b__0>d.MoveNext() :line 229
```

---

## Fixes Required

### Fix 1 — Set `Application.Name = "xerahs"` (DONE: commit `4413c031`)

`App.axaml.cs` `Initialize()` now sets `Name = "xerahs"` so Avalonia advertises
`"xerahs"` as the Wayland `xdg_toplevel.app_id`, matching `xerahs.desktop`.

```csharp
// App.axaml.cs
public override void Initialize()
{
    AvaloniaXamlLoader.Load(this);
    // Match the installed xerahs.desktop filename (lowercase) so xdg-desktop-portal
    // can identify the app and grant GlobalShortcuts permission.
    Name = "xerahs";
    Services.ThemeService.Initialize();
}
```

**Verification**: Portal log must show `CreateSession response=0` followed by
`BindShortcuts response=0` and one (single) GNOME permission dialog on first run.

### Fix 2 — Packaging: real symlink + correct StartupWMClass (DONE: commit `271265ca`)

The packaging script was creating `/usr/bin/xerahs` as a shell wrapper script rather than
a real symlink. `xdg-desktop-portal` resolves the `Exec=` path in the `.desktop` file when
doing exe-based app ID detection; a wrapper script is opaque (`/usr/bin/xerahs` ≠
`/usr/lib/xerahs/XerahS`), so matching failed. A real relative symlink
(`../lib/xerahs/XerahS`) resolves to the actual binary and allows matching.

Additionally `StartupWMClass=XerahS` (capital) was changed to `StartupWMClass=xerahs`
(lowercase) to match the WM_CLASS / xdg_toplevel.app_id now advertised after
`Application.Name = "xerahs"`.

A `WriteTarSymlinkEntry` helper was added to the DEB packaging so the data.tar.gz
encodes the symlink correctly rather than copying the binary content.

**Note**: these packaging fixes apply to packages built from the next release onwards.
For existing installations of v0.19.3 and earlier:
```bash
sudo ln -sf /usr/lib/xerahs/XerahS /usr/bin/xerahs
```

### Fix 3 — Debounce CTS ObjectDisposedException (DONE: in-progress branch)

`ScheduleRebind()` must not call `old?.Dispose()` synchronously. Only cancel the old CTS;
let it be collected by GC after the old task exits.

```csharp
// Before (broken):
old?.Cancel();
old?.Dispose();

// After (fixed):
old?.Cancel();
// Do not dispose here — old task lambda may not have started yet and still
// holds a reference via closure; disposing causes ObjectDisposedException on cts.Token.
```

The `finally` block in the task lambda already does a `CompareExchange`-guarded dispose
for the common case (last active CTS). Cancelled-and-replaced CTSes are GC'd after their
(quickly-cancelled) lambda exits. CancellationTokenSource has no finalizer and holds no
unmanaged resources unless `WaitHandle` was ever accessed (it was not), so GC is safe.

### Fix 4 — Complete Portal Strategy: Session Persistence and Configuration (DONE)

Currently `RebindShortcutsAsync` closes and recreates the portal session on every hotkey change.
This is an anti-pattern according to the `xdg-desktop-portal` design philosophy. The XDG GlobalShortcuts
portal is designed so that the application declares its available actions once, and the Desktop
Environment (Compositor) manages user configuration, key recording, and collision resolution.

To fix this and provide a native Wayland experience:

1. **Bind Once at Startup**: The application creates the session and calls `BindShortcuts` exactly
   *once* (for the entire set of available tasks). Recreating the session is avoided unless the actual
   set of hotkey workflows has grown/shrunk, preserving user-defined configurations and skipping spam dialogs.
2. **UI Integration via `ConfigureShortcuts` (Portal v2)**: Instead of XerahS capturing raw keystrokes
   in its own UI to set a hotkey, the "Set Hotkey" button in XerahS invokes the
   `ConfigureShortcuts` D-Bus method. This delegates the UX to the DE (e.g., GNOME Settings or Plasma Wayland),
   which opens a native portal dialog for the user to securely assign triggers directly.
3. **Listen for Triggers**: XerahS uses the `ShortcutsChanged` D-Bus signal to silently learn when the
   user mapping changes natively, so the UI can refresh if needed.

---

## Portal Permission Grant Flow (GNOME)

When the portal responds with `response=0` to `BindShortcuts` for the first time:
- GNOME shows a dialog: "XerahS wants to register global shortcuts. Allow?"
- User clicks **Allow** → bindings are active; no dialog on subsequent app starts
  (permission stored in GNOME's portal permission table keyed by app ID)
- User clicks **Deny** → `response=2` on future calls until permission is reset via
  GNOME Settings → Privacy → File and Application Permissions

---

## Verification Steps

1. Ensure no installed XerahS instance is running: `pkill -f xerahs`
2. Run debug build: `./run-debug-app.sh`
3. Check log for:
   - `WaylandPortalHotkeyService: CreateSession response=0 (Success)`
   - `WaylandPortalHotkeyService: BindShortcuts response=0 (Success)`
   - No `PortalBindFailedException`, no `Activating X11 fallback`
   - No `ObjectDisposedException`
4. One GNOME permission dialog appears (first run only) — click **Allow**
5. Minimise XerahS
6. Press `Ctrl+Shift+F` (or any registered hotkey) from any other app
7. Confirm the capture/action fires correctly

---

## Open Questions

1. **Fallback for older portals**: `ConfigureShortcuts` is part of portal v2 (xdg-desktop-portal ≥ 1.18).
   For older portals, users can still manually assign keys in standard GNOME Settings -> Keyboard,
   but we need a strategy for how the XerahS UI gracefully falls back or shows instructions if the
   `ConfigureShortcuts` call fails.

2. **KDE Plasma support**: KDE Plasma ≥ 6 implements the GlobalShortcuts portal. The aforementioned
   "Bind Once + ConfigureShortcuts" architecture natively aligns with KDE's approach as well. Once
   the refactor is complete, we should verify the flow on Plasma 6 Wayland.

3. **Hotkey registration UI mapping**: The core XerahS hotkey models assume the app knows the key
   combination synchronously. Adopting `ListShortcuts` requires making the UI state asynchronous
   and reactive to the portal's source of truth.

---

## Changelog

| Date | Commit | Description |
|---|---|---|
| 2026-02-28 | `151a94b3` | Fix `BuildPreferredTrigger` GLib format, parent window handle |
| 2026-03-01 | `4413c031` | Set `Application.Name = "xerahs"` to match `xerahs.desktop` app ID |
| 2026-03-02 | `1db0a454` | Debounce `ScheduleRebind()` — reduce 8 portal calls to 1 at startup |
| 2026-03-02 | `1cb75370` | Fix CTS `ObjectDisposedException` in `ScheduleRebind` |
| 2026-03-02 | `271265ca` | Packaging: real symlink, `StartupWMClass=xerahs`, DEB symlink tar entry |
| 2026-03-02 | `271265ca` | Add `app_id` + `parentWindow` diagnostic log to `BindShortcutsAsync` |
| *(future)* | — | Verify `parentWindow` is non-empty (Wayland/XWayland handle) |
| *(future)* | — | Session persistence / `ConfigureShortcuts` for incremental rebinding |
