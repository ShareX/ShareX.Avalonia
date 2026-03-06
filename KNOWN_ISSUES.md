# Known Issues

## Windows

### Region Capture
- **DPI Scaling Issue on Region Capture Background:** The dark background behind the region capture tool is not rendering correctly when any monitor connected to the system is set to a DPI scale greater than 100%. The background overlay appears shifted or misaligned in these high-DPI scenarios.

## Linux

### Global Hotkeys
- **Global hotkeys not firing when app is backgrounded (XIP0044):** On Linux (Wayland / XWayland), global hotkeys currently only trigger when XerahS is the active window. When the app is minimised or another window has focus, registered shortcuts (e.g. screenshot/recording) do not fire, making them unusable for normal background usage. See `docs/proposals/xip/XIP0044-linux-global-hotkeys-not-firing-when-app-backgrounded.md` for analysis and planned fixes.

- **Workaround via PrintScreen + folder watch:** On most Linux desktops the **PrintScreen** hardware key still works through the system screenshot tool (often via the XDG portal), even when XerahS is not focused. Configure that tool to save captures into a dedicated folder, and configure XerahS to watch that folder and auto-upload new files as a practical workaround until true global hotkeys are fully fixed.

## macOS

- **No macOS-specific known issues documented yet.**
