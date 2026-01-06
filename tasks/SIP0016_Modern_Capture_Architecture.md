# SIP0016: Modern Cross-Platform Capture Architecture

## Goal
Upgrade the `ShareX.Avalonia` screen capture subsystem to utilize modern, high-performance, and OS-native APIs. The current implementations (GDI+ for Windows, CLI tools for macOS) are performance bottlenecks and lack support for modern display servers (like Wayland) or hardware acceleration. This proposal outlines a staged approach to implement robust capture providers for Windows, Linux, and macOS.

> [!IMPORTANT]
> **Branching Strategy**:
> Before starting any work on this SIP, verify you are on the latest `master` branch and create a new feature branch named `feature/SIP0016-modern-capture`. Do not commit directly to `master` or existing branches.

## Implementation Plan

The implementation will be executed in three distinct stages, prioritizing the Windows platform followed by Linux and macOS.

### Stage 1: Windows - Direct3D11 & WinRT Integration
**Objective**: Replace the legacy GDI+ (`System.Drawing`) capture method with a hardware-accelerated solution using Direct3D11 and Windows Runtime (WinRT) APIs.

**Technical Requirements**:
*   **Direct3D11 Device Management**: Implement management of D3D11 devices and contexts to handle GPU resources efficiently.
*   **Windows.Graphics.Capture API**: Utilize the `Windows.Graphics.Capture` namespace (introducted in Windows 10 build 1803) for high-performance frame capture.
*   **Interop Layer**: Establish interop between .NET and native WinRT/COM interfaces (e.g., `IDirect3D11Device`, `IGraphicsCaptureItem`).
*   **Benefits**:
    *   Zero-copy capture where possible (GPU memory).
    *   Ability to capture exclusive fullscreen games and hardware-accelerated windows.
    *   Cursor capture compositing handled by the OS.
    *   "Yellow Border" privacy indicator support (optional/configurable).

### Stage 2: Linux - XDG Portals & Wayland Support
**Objective**: Implement a secure and compliant capture provider for Linux, specifically targeting Wayland compositors where X11 calls are restricted.

**Technical Requirements**:
*   **DBus Communication**: Implement a DBus client to communicate with session services.
*   **XDG Desktop Portals**:
    *   Use `org.freedesktop.portal.Screenshot` or `org.freedesktop.portal.ScreenCast` for universal capture support across distributions (GNOME, KDE, etc.).
*   **KDE Specifics**:
    *   Investigate `org.kde.KWin.ScreenShot2` for privileged, silent capture where appropriate/configured.
*   **Fallbacks**: Maintain or refine X11 fallback for legacy sessions.

### Stage 3: macOS - ScreenCaptureKit
**Objective**: Replace the slow and limited `screencapture` CLI subprocess calls with the native `ScreenCaptureKit` framework (available macOS 12.3+).

**Technical Requirements**:
*   **ScreenCaptureKit Framework**:
    *   Use `SCStream` for efficient frame delivery.
    *   Use `SCShareableContent` to enumerate windows and displays efficiently.
*   **Swift/Obj-C Interop**:
    *   Create a thin native library or use direct P/Invoke bindings to interface with the Swift/Objective-C APIs from C#.
*   **Performance**:
    *   Enable high-framerate capture suitable for video recording.
    *   Reduce CPU usage compared to spawning detached processes.

## Architectural Changes
*   Define a robust `ICaptureProvider` interface in `ShareX.Avalonia.Platform.Abstractions`.
*   Each stage will implement a concrete provider:
    *   `WindowsSecureCaptureProvider`
    *   `LinuxWaylandCaptureProvider`
    *   `MacOSNativeCaptureProvider`
*   The system will auto-detect the OS and version to select the best available provider, falling back to legacy methods (GDI+/CLI) only when modern APIs are unavailable (e.g., older OS versions).
