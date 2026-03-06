# FFmpeg in XerahS

XerahS uses FFmpeg as a recording backend across all supported platforms. The role FFmpeg plays differs per OS — on some platforms it is the primary recorder, on others it is a fallback behind a native API.

---

## Windows

### Installation

XerahS can download FFmpeg automatically via **Settings > FFmpeg > Download FFmpeg**. It fetches the latest build from the [ShareX/FFmpeg](https://github.com/ShareX/FFmpeg) GitHub release and extracts it to the app's `Tools` folder. You can also install FFmpeg manually and point XerahS to it via **Settings > FFmpeg > Override path**.

### Capture devices

On Windows, XerahS uses the following FFmpeg input devices:

| Device | Flag | Notes |
|---|---|---|
| GDI grab | `-f gdigrab` | Default fallback. Works on all Windows versions. Lower performance. |
| Desktop Duplication API | `-f ddagrab` | Hardware-accelerated. Requires Windows 8+. |
| DirectShow (screen-capture-recorder) | `-f dshow -i video="screen-capture-recorder"` | Third-party virtual device. |
| DirectShow (virtual-audio-capturer) | `-f dshow -i audio="virtual-audio-capturer"` | Third-party virtual audio device. |

The primary recording path on Windows uses **Windows.Graphics.Capture** (native API). FFmpeg with `gdigrab` is the fallback for systems that do not support it.

### Audio

System audio is captured via DirectShow (`-f dshow`) using **Stereo Mix**. This requires Stereo Mix to be enabled in Windows Sound settings (right-click the speaker icon > Sounds > Recording tab > Show Disabled Devices).

Microphone capture also uses DirectShow with the selected device ID.

### Example command (Windows fallback)

```
ffmpeg -f gdigrab -framerate 30 -draw_mouse 1 -i desktop -f dshow -i audio="Stereo Mix" -map 0:v -map 1:a -c:v libx264 -preset ultrafast -b:v 5000k -c:a aac -b:a 192k -pix_fmt yuv420p -y output.mp4
```

For a region:

```
ffmpeg -f gdigrab -framerate 30 -draw_mouse 1 -offset_x 100 -offset_y 50 -video_size 1280x720 -i desktop -c:v libx264 -preset ultrafast -b:v 5000k -pix_fmt yuv420p -y output.mp4
```

---

## macOS

### Installation

FFmpeg is not bundled. Install it via Homebrew:

```sh
brew install ffmpeg
```

XerahS searches `PATH` and common locations automatically. You can also set a custom path in **Settings > FFmpeg > Override path**.

### Recording backends

XerahS prefers the native **ScreenCaptureKit** (AVAssetWriter) backend on macOS 12.3+. FFmpeg is used as a fallback when ScreenCaptureKit is unavailable.

| Backend | Condition |
|---|---|
| Native ScreenCaptureKit (primary) | macOS 12.3+ |
| FFmpeg `avfoundation` (fallback) | Older macOS or when native backend fails |

### Capture device

The `avfoundation` input device is used:

```
-f avfoundation -framerate 30 -i "1"
```

The input index `"1"` refers to the main display. Audio uses `-f avfoundation -i ":0"` for the default audio device.

Region capture is implemented as a crop filter applied post-capture, since `avfoundation` does not natively support offset/size arguments:

```
-vf "crop=1280:720:100:50"
```

### Example command (macOS fallback)

```sh
ffmpeg -f avfoundation -framerate 30 -capture_cursor 1 -i "1" -f avfoundation -i ":0" -map 0:v -map 1:a -c:v libx264 -preset ultrafast -b:v 5000k -c:a aac -b:a 192k -pix_fmt yuv420p -y output.mp4
```

---

## Linux

### Installation

FFmpeg must be installed with **PipeWire input support** compiled in. Most modern distro packages include it by default. Check what you already have:

```sh
ffmpeg -devices 2>&1 | grep pipewire
```

If a `pipewire` line appears in the output, you are good. If not, use one of the following:

**Fedora / RHEL (RPM Fusion)**

RPM Fusion's build includes PipeWire. Enable it first if you haven't:

```sh
sudo dnf install https://mirrors.rpmfusion.org/free/fedora/rpmfusion-free-release-$(rpm -E %fedora).noarch.rpm
sudo dnf install ffmpeg
```

**Ubuntu / Debian**

Ubuntu 22.04+ and Debian 12+ include PipeWire in their default FFmpeg package. If yours doesn't, use a PPA with full codec support:

```sh
sudo add-apt-repository ppa:savoury1/ffmpeg4
sudo apt update && sudo apt install ffmpeg
```

**Arch Linux**

The official `ffmpeg` package in the Arch repos includes PipeWire:

```sh
sudo pacman -S ffmpeg
```

**NixOS**

Use `ffmpeg-full` which enables all inputs including PipeWire:

```nix
environment.systemPackages = [ pkgs.ffmpeg-full ];
```

**Static builds**

Static builds (e.g. from johnvansickle.com) do **not** include PipeWire. PipeWire requires runtime linking to system libraries and cannot be statically bundled. You must use a dynamically linked package from your distro.

GStreamer with PipeWire plugins is used as a fallback if FFmpeg lacks PipeWire support:

```sh
# GStreamer PipeWire plugins (fallback)
sudo apt install gstreamer1.0-pipewire          # Debian/Ubuntu
sudo dnf install gstreamer1-plugin-pipewire     # Fedora
sudo pacman -S gst-plugin-pipewire              # Arch
```

### How screen recording works on Linux

XerahS uses the **XDG ScreenCast portal** (`org.freedesktop.portal.ScreenCast`) to obtain a PipeWire stream from the compositor. This works on all Wayland compositors that support the portal (GNOME, KDE Plasma, wlroots-based, etc.).

The flow is fully automatic:

1. XerahS opens a D-Bus session with the portal.
2. The compositor displays its own native source picker — the user selects a monitor or window.
3. The portal returns a PipeWire node ID for the selected source.
4. XerahS passes that node ID directly to FFmpeg as `-i <node_id>`.

**The user never needs to know or configure a PipeWire node ID.** It is resolved automatically per recording session.

### Recording backend priority (Wayland)

| Priority | Backend | Condition |
|---|---|---|
| 1 | `wf-recorder` | wlroots compositor detected and wf-recorder is installed |
| 2 | XDG Portal + FFmpeg (`pipewire`) | FFmpeg has PipeWire input support |
| 3 | XDG Portal + GStreamer (`pipewiresrc`) | GStreamer PipeWire plugins installed |

On X11, the fallback is `x11grab`.

### Verify your setup

Run the built-in diagnostic from the app: **Help > Diagnostics** (Linux). It reports:

- Whether FFmpeg has PipeWire input
- Whether GStreamer has PipeWire plugins
- The recommended backend for your session type
- Any missing dependencies with install suggestions

### Audio on Linux

System audio is captured from the PulseAudio monitor source of the default output device. XerahS resolves this automatically using `pactl`. Microphone capture uses the selected device ID from settings.

Both are passed to FFmpeg as a second input:

```
-f pulse -i alsa_output.pci-0000_00_1f.3.analog-stereo.monitor
```

### Example command (Wayland, FFmpeg + PipeWire)

```sh
ffmpeg \
  -f pipewire -framerate 30 -i <node_id> \
  -f pulse -i alsa_output.pci-0000_00_1f.3.analog-stereo.monitor \
  -map 0:v -map 1:a \
  -c:v libx264 -preset ultrafast -b:v 5000k \
  -c:a aac -b:a 192k \
  -pix_fmt yuv420p \
  -y output.mp4
```

With region crop:

```sh
ffmpeg \
  -f pipewire -framerate 30 -i <node_id> \
  -vf "crop=1280:720:0:0" \
  -c:v libx264 -preset ultrafast -b:v 5000k \
  -pix_fmt yuv420p \
  -y output.mp4
```

`<node_id>` is the integer PipeWire node provided by the portal — XerahS fills this in automatically.

### Example command (X11 fallback)

```sh
ffmpeg -f x11grab -framerate 30 -draw_mouse 1 -i :0.0 -c:v libx264 -preset ultrafast -b:v 5000k -pix_fmt yuv420p -y output.mp4
```

For a specific region on X11:

```sh
ffmpeg -f x11grab -framerate 30 -draw_mouse 1 -video_size 1280x720 -i :0.0+100,50 -c:v libx264 -preset ultrafast -b:v 5000k -pix_fmt yuv420p -y output.mp4
```

---

## Supported codecs (all platforms)

| Codec | FFmpeg encoder | Notes |
|---|---|---|
| H.264 | `libx264` | Default. Best compatibility. |
| H.265 / HEVC | `libx265` | Better compression, slower encode. |
| VP9 | `libvpx-vp9` | Open, good for WebM. |
| AV1 | `libaom-av1` | Best compression, CPU-intensive. |

All codecs use `-pix_fmt yuv420p` for broad player compatibility.

---

## Custom FFmpeg path

If XerahS cannot find FFmpeg automatically, set a custom path in **Settings > FFmpeg > Override CLI path**. The app checks (in order):

1. Explicitly configured path
2. `Options.CLIPath` if override is enabled
3. `PATH` environment variable and common install locations (`Tools/`, `Program Files/FFmpeg/bin/`, etc.)
