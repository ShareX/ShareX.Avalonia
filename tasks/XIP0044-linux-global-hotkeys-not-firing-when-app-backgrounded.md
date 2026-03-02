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

### Fix 2 — Packaging bug: create `/usr/bin/xerahs` symlink (PENDING)

The `.desktop` file has `Exec=/usr/bin/xerahs` but the binary is at `/usr/lib/xerahs/XerahS`.
This breaks portal app ID resolution via `Exec=` matching as a fallback.

**Workaround for testing** (manual, as root):
```bash
sudo ln -sf /usr/lib/xerahs/XerahS /usr/bin/xerahs
```

**Permanent fix**: update the RPM/DEB spec to install the symlink, or change the `.desktop`
`Exec=` field to point to `/usr/lib/xerahs/XerahS` directly.

Files to update:
- `build/linux/xerahs.desktop` (or wherever the desktop file is generated)
- RPM `.spec` / Debian `control` packaging scripts

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

### Fix 4 — Portal session persistence across hotkey re-registration (FUTURE)

Currently `RebindShortcutsAsync` closes and recreates the portal session on every call.
This means the GNOME permission dialog may appear again if the session is lost. The correct
approach is to keep the session alive and use `ConfigureShortcuts` (portal v2) or
`BindShortcuts` on the existing session to update the binding set incrementally.

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

1. **Portal v2 `ConfigureShortcuts`**: available in xdg-desktop-portal ≥ 1.18; should be
   preferred over `BindShortcuts` for session lifetime management. Investigate whether
   Tmds.DBus codegen supports the optional `ConfigureShortcuts` method cleanly.

2. **KDE Plasma support**: KDE Plasma ≥ 6.0 also implements the GlobalShortcuts portal.
   Once GNOME is working, verify same flow works on KDE without portal-specific hacks.

3. **Hotkey status feedback to UI**: with the async/debounced portal call, the `Registered`
   status set optimistically by `RegisterHotkey` may not reflect actual portal state. If the
   debounced `RebindShortcutsAsync` fails (e.g. portal rejects), the UI still shows green.
   Consider a callback or event to update `HotkeyInfo.Status` after the async result is known.

---

## Changelog

| Date | Commit | Description |
|---|---|---|
| 2026-02-28 | `151a94b3` | Fix `BuildPreferredTrigger` GLib format, parent window handle |
| 2026-03-01 | `4413c031` | Set `Application.Name = "xerahs"` to match `xerahs.desktop` app ID |
| 2026-03-02 | `1db0a454` | Debounce `ScheduleRebind()` — reduce 8 portal calls to 1 at startup |
| 2026-03-02 | *(pending)* | Fix CTS `ObjectDisposedException` in `ScheduleRebind` |
| *(future)* | — | Fix `/usr/bin/xerahs` packaging symlink |
| *(future)* | — | Session persistence / `ConfigureShortcuts` for incremental rebinding |
