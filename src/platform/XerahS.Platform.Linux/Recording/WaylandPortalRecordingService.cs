#region License Information (GPL v3)

/*
    XerahS - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2026 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Tmds.DBus;
using XerahS.Common;
using XerahS.Media;
using XerahS.Platform.Linux.Capture;
using XerahS.Platform.Linux.Services;
using XerahS.RegionCapture.ScreenRecording;

namespace XerahS.Platform.Linux.Recording;

/// <summary>
/// Wayland screen recording via XDG ScreenCast portal + FFmpeg pipewire input.
/// Falls back to FFmpegRecordingService if portal negotiation fails.
/// </summary>
public sealed class WaylandPortalRecordingService : IRecordingService
{
    private const string PortalBusName = "org.freedesktop.portal.Desktop";
    private static readonly ObjectPath PortalObjectPath = new("/org/freedesktop/portal/desktop");

    private FFmpegCLIManager? _ffmpeg;
    private Process? _gstreamerProcess;
    private int? _gstreamerPid;
    private Task? _ffmpegTask;
    private RecordingOptions? _currentOptions;
    private RecordingStatus _status = RecordingStatus.Idle;
    private readonly Stopwatch _stopwatch = new();
    private readonly object _lock = new();
    private bool _disposed;
    private bool _stopRequested;
    private Timer? _durationTimer;

    private Connection? _connection;
    private IScreenCastPortal? _portal;
    private IPortalSession? _sessionProxy;
    private ObjectPath? _sessionHandle;
    private uint _pipewireNodeId;
    private int _pipewireSourceWidth;
    private int _pipewireSourceHeight;

    public event EventHandler<RecordingErrorEventArgs>? ErrorOccurred;
    public event EventHandler<RecordingStatusEventArgs>? StatusChanged;

    public Task StartRecordingAsync(RecordingOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        lock (_lock)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(WaylandPortalRecordingService));
            if (_status != RecordingStatus.Idle)
            {
                throw new InvalidOperationException("Recording already in progress");
            }

            _currentOptions = options;
            UpdateStatus(RecordingStatus.Initializing);
        }

        try
        {
            EnsureWayland();

            // Prefer wf-recorder only on wlroots compositors and scenarios it supports well.
            if (CanUseWfRecorder(options))
            {
                DebugHelper.WriteLine("[WaylandPortalRecording] Using wf-recorder (handles portal internally)");
                return StartWithWfRecorder(options);
            }

            // Fall back to portal + GStreamer/FFmpeg approach
            InitializePortalSession(options).GetAwaiter().GetResult();

            var (executable, args, useGStreamer) = BuildRecordingCommand(options, _pipewireNodeId, _pipewireSourceWidth, _pipewireSourceHeight);
            DebugHelper.WriteLine($"[WaylandPortalRecording] Using {(useGStreamer ? "GStreamer" : "FFmpeg")}");
            DebugHelper.WriteLine($"[WaylandPortalRecording] Command: {executable} {args}");

            if (useGStreamer)
            {
                // Build CPU-only fallback pipeline in case the GL path fails.
                // Two known failure modes on GNOME/Wayland:
                //   (a) GL path with video/x-raw filter → not-negotiated when pipewiresrc only offers DMABuf
                //   (b) GL path without filter → glupload "unhandled format" on some systems
                // The fallback (no GL) uses videoconvert which handles both raw and DMABuf universally.
                string? fallbackGstArgs = null;
                bool hasGlElements = HasGStreamerElement("gldownload") && HasGStreamerElement("glupload");
                if (hasGlElements)
                {
                    // options.OutputPath was resolved (and possibly extension-adjusted) by BuildRecordingCommand above.
                    var (fallbackPipeline, _) = BuildGStreamerPipeline(options, _pipewireNodeId,
                        options.OutputPath ?? string.Empty, _pipewireSourceWidth, _pipewireSourceHeight, useGl: false);
                    fallbackGstArgs = "-e " + fallbackPipeline;
                    DebugHelper.WriteLine("[WaylandPortalRecording] CPU fallback pipeline ready (will use if GL path fails)");
                }

                var capturedExecutable = executable;
                var capturedPrimaryArgs = args;
                var capturedFallbackArgs = fallbackGstArgs;
                _ffmpegTask = Task.Run(() => RunGStreamerWithFallback(capturedExecutable, capturedPrimaryArgs, capturedFallbackArgs));
            }
            else
            {
                // Use FFmpeg via FFmpegCLIManager
                string ffmpegPath = PathsManager.GetFFmpegPath();
                if (string.IsNullOrEmpty(ffmpegPath) || !File.Exists(ffmpegPath))
                {
                    throw new FileNotFoundException("FFmpeg not found for Wayland portal recording.", ffmpegPath);
                }

                _ffmpeg = new FFmpegCLIManager(ffmpegPath)
                {
                    ShowError = true,
                    TrackEncodeProgress = true
                };

                _ffmpegTask = Task.Run(() =>
                {
                    try
                    {
                        lock (_lock)
                        {
                            _stopwatch.Restart();
                            UpdateStatus(RecordingStatus.Recording);
                        }

                        bool success = _ffmpeg.Run(args);
                        if (!success && !_ffmpeg.StopRequested)
                        {
                            HandleFatalError(new Exception($"FFmpeg process failed.\nOutput: {_ffmpeg.Output}"), true);
                        }
                    }
                    catch (Exception ex)
                    {
                        HandleFatalError(ex, true);
                    }
                });
            }

            return Task.CompletedTask;
        }
        catch (DBusException ex)
        {
            CleanupPortalSession();
            throw new PlatformNotSupportedException("Wayland ScreenCast portal unavailable.", ex);
        }
        catch (Exception ex)
        {
            CleanupPortalSession();
            HandleFatalError(ex, true);
            throw;
        }
    }

    /// <summary>
    /// Tries the primary GStreamer pipeline; if it fails and a fallback is available, retries with the fallback.
    /// Only calls HandleFatalError when all candidates are exhausted.
    /// </summary>
    private void RunGStreamerWithFallback(string executable, string primaryArgs, string? fallbackArgs)
    {
        bool primaryFailed = RunGStreamerProcess(executable, primaryArgs, out string primaryOutput);

        if (!primaryFailed || _stopRequested)
            return;

        if (fallbackArgs != null)
        {
            DebugHelper.WriteLine("[WaylandPortalRecording] Primary (GL) pipeline failed; retrying with CPU fallback pipeline...");
            bool fallbackFailed = RunGStreamerProcess(executable, fallbackArgs, out string fallbackOutput);
            if (fallbackFailed && !_stopRequested)
            {
                HandleFatalError(new Exception(
                    $"GStreamer: both GL and CPU pipelines failed.\nGL output: {primaryOutput}\nCPU output: {fallbackOutput}"), true);
            }
        }
        else
        {
            HandleFatalError(new Exception($"GStreamer process failed.\nOutput: {primaryOutput}"), true);
        }
    }

    /// <summary>
    /// Runs a single GStreamer process and waits for it to exit.
    /// Returns true if the process failed and the caller should consider a retry.
    /// </summary>
    private bool RunGStreamerProcess(string executable, string args, out string stderrOutput)
    {
        stderrOutput = string.Empty;
        try
        {
            lock (_lock)
            {
                _stopwatch.Restart();
                UpdateStatus(RecordingStatus.Recording);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(startInfo);
            if (process == null)
            {
                stderrOutput = "Failed to start GStreamer process";
                return true;
            }

            _gstreamerProcess = process;
            try { _gstreamerPid = process.Id; } catch { }

            stderrOutput = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                DebugHelper.WriteLine($"[WaylandPortalRecording] GStreamer stderr:\n{stderrOutput}");
                return !_stopRequested; // failed → caller should retry (unless user stopped)
            }

            return false; // exited cleanly
        }
        catch (Exception ex)
        {
            HandleFatalError(ex, true);
            stderrOutput = ex.Message;
            return false; // unexpected exception: don't retry
        }
    }

    /// <summary>
    /// Start recording using wf-recorder (handles portal integration internally).
    /// This is the preferred method on wlroots-based compositors (Hyprland, Sway).
    /// </summary>
    private Task StartWithWfRecorder(RecordingOptions options)
    {
        var settings = options.Settings ?? new ScreenRecordingSettings();
        string outputPath = options.OutputPath ?? GetDefaultOutputPath();

        var args = new List<string>();

        // Add geometry for region capture (wf-recorder uses "x,y WxH" format)
        if (options.Mode == CaptureMode.Region && options.Region.Width > 0 && options.Region.Height > 0)
        {
            args.Add($"-g \"{options.Region.X},{options.Region.Y} {options.Region.Width}x{options.Region.Height}\"");
        }

        // Codec selection - wf-recorder prefers VAAPI for hardware encoding
        string codec = settings.Codec switch
        {
            VideoCodec.H264 => "libx264",  // Could use h264_vaapi if available
            VideoCodec.HEVC => "libx265",  // Could use hevc_vaapi if available
            VideoCodec.VP9 => "libvpx-vp9",
            VideoCodec.AV1 => "libaom-av1",
            _ => "libx264"
        };
        args.Add($"-c {codec}");

        // Encoder parameters
        args.Add($"-p crf=23");  // Quality setting
        args.Add($"-r {settings.FPS}");  // Frame rate

        // Audio capture
        if (settings.CaptureSystemAudio)
        {
            string monitorSource = PulseAudioHelper.GetDefaultMonitorSource();
            args.Add($"-a{monitorSource}");
            DebugHelper.WriteLine($"[WaylandPortalRecording] wf-recorder audio: system audio via {monitorSource}");
        }
        else if (settings.CaptureMicrophone)
        {
            string micDevice = !string.IsNullOrEmpty(settings.MicrophoneDeviceId)
                ? settings.MicrophoneDeviceId
                : "default";
            args.Add($"-a{micDevice}");
            DebugHelper.WriteLine($"[WaylandPortalRecording] wf-recorder audio: microphone via {micDevice}");
        }

        // Output file
        args.Add($"-f \"{outputPath}\"");

        string argsString = string.Join(" ", args);
        DebugHelper.WriteLine($"[WaylandPortalRecording] wf-recorder command: wf-recorder {argsString}");

        _ffmpegTask = Task.Run(() =>
        {
            try
            {
                lock (_lock)
                {
                    _stopwatch.Restart();
                    UpdateStatus(RecordingStatus.Recording);
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "wf-recorder",
                    Arguments = argsString,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var process = Process.Start(startInfo);
                if (process == null)
                {
                    HandleFatalError(new Exception("Failed to start wf-recorder process"), true);
                    return;
                }

                _gstreamerProcess = process;  // Reuse this field for the process reference
                try { _gstreamerPid = process.Id; } catch { }
                string output = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0 && !_stopRequested)
                {
                    HandleFatalError(new Exception($"wf-recorder failed.\nOutput: {output}"), true);
                }
            }
            catch (Exception ex)
            {
                HandleFatalError(ex, true);
            }
        });

        return Task.CompletedTask;
    }

    public async Task StopRecordingAsync()
    {
        FFmpegCLIManager? ffmpeg;
        Process? gstreamer;
        int? gstreamerPid;
        Task? ffmpegTask;

        lock (_lock)
        {
            if (_status != RecordingStatus.Recording)
            {
                return;
            }

            _stopRequested = true;
            UpdateStatus(RecordingStatus.Finalizing);
            _stopwatch.Stop();

            ffmpeg = _ffmpeg;
            gstreamer = _gstreamerProcess;
            gstreamerPid = _gstreamerPid;
            ffmpegTask = _ffmpegTask;
        }

        try
        {
            // Stop FFmpeg
            if (ffmpeg != null)
            {
                ffmpeg.StopRequested = true;
                ffmpeg.WriteInput("q");
            }

            // Stop GStreamer by sending EOS (End of Stream) via SIGINT
            if (gstreamerPid.HasValue)
            {
                try
                {
                    DebugHelper.WriteLine($"[WaylandPortalRecording] Sending SIGINT to GStreamer (PID {gstreamerPid.Value}) for graceful shutdown...");
                    // Send SIGINT to GStreamer for graceful shutdown (triggers EOS with -e flag)
                    var killProcess = Process.Start("kill", $"-2 {gstreamerPid.Value}");
                    killProcess?.WaitForExit(1000);

                    // Wait for GStreamer to finish writing and exit
                    if (gstreamer != null && !gstreamer.WaitForExit(10000))
                    {
                        DebugHelper.WriteLine("[WaylandPortalRecording] GStreamer did not exit in time, force killing...");
                        try { Process.Start("kill", $"-9 {gstreamerPid.Value}")?.WaitForExit(1000); } catch { }
                    }
                    else if (gstreamer != null)
                    {
                        DebugHelper.WriteLine($"[WaylandPortalRecording] GStreamer exited with code {gstreamer.ExitCode}");
                    }
                }
                catch (Exception ex)
                {
                    DebugHelper.WriteLine($"[WaylandPortalRecording] Error stopping GStreamer: {ex.Message}");
                    // If SIGINT fails, try force kill
                    try { Process.Start("kill", $"-9 {gstreamerPid.Value}")?.WaitForExit(1000); } catch { }
                }
            }
            else if (gstreamer != null)
            {
                // Fallback: no stored PID, try Kill() directly
                try { gstreamer.Kill(); } catch { }
            }

            if (ffmpegTask != null)
            {
                await Task.WhenAny(ffmpegTask, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            HandleFatalError(ex, false);
        }
        finally
        {
            CleanupPortalSession();

            lock (_lock)
            {
                _ffmpeg = null;
                _gstreamerProcess = null;
                _gstreamerPid = null;
                _currentOptions = null;
                _stopRequested = false;
                UpdateStatus(RecordingStatus.Idle);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        lock (_lock)
        {
            _disposed = true;
            _durationTimer?.Dispose();
            _durationTimer = null;
        }

        try
        {
            if (_status == RecordingStatus.Recording)
            {
                StopRecordingAsync().Wait();
            }
        }
        catch
        {
            // Best effort cleanup
        }

        CleanupPortalSession();
    }

    private static void EnsureWayland()
    {
        if (!LinuxScreenCaptureService.IsWayland)
        {
            throw new PlatformNotSupportedException("Wayland ScreenCast portal requires a Wayland session.");
        }
    }

    private async Task InitializePortalSession(RecordingOptions options)
    {
        _connection = new Connection(Address.Session);
        await _connection.ConnectAsync().ConfigureAwait(false);
        _portal = _connection.CreateProxy<IScreenCastPortal>(PortalBusName, PortalObjectPath);

        var createOptions = new Dictionary<string, object>
        {
            ["session_handle_token"] = $"xerahs_sc_{Guid.NewGuid():N}"
        };

        var createPath = await _portal.CreateSessionAsync(createOptions).ConfigureAwait(false);
        var createRequest = _connection.CreateProxy<IPortalRequest>(PortalBusName, createPath);
        var (createResponse, createResults) = await createRequest.WaitForResponseAsync().ConfigureAwait(false);
        if (createResponse != 0 ||
            !createResults.TryGetResult("session_handle", out string? sessionHandlePath) ||
            string.IsNullOrWhiteSpace(sessionHandlePath))
        {
            throw new PlatformNotSupportedException($"ScreenCast CreateSession failed ({createResponse}).");
        }

        _sessionHandle = new ObjectPath(sessionHandlePath);
        _sessionProxy = _connection.CreateProxy<IPortalSession>(PortalBusName, _sessionHandle.Value);

        var selectOptions = new Dictionary<string, object>
        {
            ["types"] = GetSourceTypes(options.Mode),
            ["multiple"] = false,
            ["cursor_mode"] = (uint)((options.Settings?.ShowCursor ?? true) ? 1 : 0),
            // persist_mode: 2 = persist the permission until explicitly revoked
            // This reduces portal dialogs for subsequent recordings
            ["persist_mode"] = (uint)2
        };

        var selectPath = await _portal.SelectSourcesAsync(_sessionHandle.Value, selectOptions).ConfigureAwait(false);
        var selectRequest = _connection.CreateProxy<IPortalRequest>(PortalBusName, selectPath);
        var (selectResponse, _) = await selectRequest.WaitForResponseAsync().ConfigureAwait(false);
        if (selectResponse != 0)
        {
            throw new PlatformNotSupportedException($"ScreenCast SelectSources failed ({selectResponse}).");
        }

        var startPath = await _portal.StartAsync(_sessionHandle.Value, string.Empty, new Dictionary<string, object>()).ConfigureAwait(false);
        var startRequest = _connection.CreateProxy<IPortalRequest>(PortalBusName, startPath);
        var (startResponse, startResults) = await startRequest.WaitForResponseAsync().ConfigureAwait(false);
        if (startResponse != 0)
        {
            throw new PlatformNotSupportedException($"ScreenCast Start failed ({startResponse}).");
        }

        if (!TryGetPipeWireNodeId(startResults, out _pipewireNodeId))
        {
            throw new PlatformNotSupportedException("ScreenCast response did not include PipeWire stream node.");
        }

        TryGetPipeWireSourceSize(startResults, out _pipewireSourceWidth, out _pipewireSourceHeight);
        if (_pipewireSourceWidth > 0)
            DebugHelper.WriteLine($"[WaylandPortalRecording] PipeWire source size: {_pipewireSourceWidth}x{_pipewireSourceHeight}");
        else
            DebugHelper.WriteLine("[WaylandPortalRecording] PipeWire source size: not reported by portal");
    }

    private void CleanupPortalSession()
    {
        try
        {
            _sessionProxy?.CloseAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // Best effort cleanup
        }
        finally
        {
            _sessionProxy = null;
            _sessionHandle = null;
            _portal = null;
            _connection?.Dispose();
            _connection = null;
        }
    }

    private static uint GetSourceTypes(CaptureMode mode)
    {
        return mode == CaptureMode.Window ? 2u : 1u;
    }

    private static bool TryGetPipeWireNodeId(IDictionary<string, object> results, out uint nodeId)
    {
        nodeId = 0;
        if (!results.TryGetValue("streams", out var streamsRaw) || streamsRaw == null)
        {
            return false;
        }

        var streams = UnwrapVariant(streamsRaw) as Array;
        if (streams == null || streams.Length == 0)
        {
            return false;
        }

        foreach (var entry in streams)
        {
            if (entry == null) continue;
            var unwrapped = UnwrapVariant(entry);

            if (unwrapped is ValueTuple<uint, IDictionary<string, object>> tuple)
            {
                nodeId = tuple.Item1;
                return true;
            }

            if (unwrapped is object[] parts && parts.Length > 0)
            {
                var idCandidate = UnwrapVariant(parts[0]);
                if (idCandidate is uint id)
                {
                    nodeId = id;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Extracts the source stream size (width × height in screen pixels) from the portal Start response.
    /// The XDG ScreenCast portal includes a "size" property per stream: a{sv} with key "size" → (int32, int32).
    /// </summary>
    private static bool TryGetPipeWireSourceSize(IDictionary<string, object> results, out int width, out int height)
    {
        width = 0;
        height = 0;

        if (!results.TryGetValue("streams", out var streamsRaw) || streamsRaw == null)
            return false;

        var streams = UnwrapVariant(streamsRaw) as Array;
        if (streams == null || streams.Length == 0)
            return false;

        foreach (var entry in streams)
        {
            if (entry == null) continue;
            var unwrapped = UnwrapVariant(entry);

            IDictionary<string, object>? props = null;
            if (unwrapped is ValueTuple<uint, IDictionary<string, object>> tuple)
                props = tuple.Item2;
            else if (unwrapped is object[] parts && parts.Length > 1)
                props = UnwrapVariant(parts[1]) as IDictionary<string, object>;

            if (props == null || !props.TryGetValue("size", out var sizeRaw))
                continue;

            var size = UnwrapVariant(sizeRaw);
            if (size is ValueTuple<int, int> sizeTuple)
            {
                width = sizeTuple.Item1;
                height = sizeTuple.Item2;
                return width > 0 && height > 0;
            }
            if (size is object[] sizeArr && sizeArr.Length >= 2)
            {
                width = Convert.ToInt32(UnwrapVariant(sizeArr[0]));
                height = Convert.ToInt32(UnwrapVariant(sizeArr[1]));
                return width > 0 && height > 0;
            }
        }

        return false;
    }

    private static object UnwrapVariant(object value)
    {
        var current = value;
        while (current != null)
        {
            var type = current.GetType();
            var typeName = type.FullName;
            if (typeName != "Tmds.DBus.Protocol.Variant" &&
                typeName != "Tmds.DBus.Protocol.VariantValue" &&
                typeName != "Tmds.DBus.Variant")
            {
                break;
            }

            var valueProp = type.GetProperty("Value");
            var unwrapped = valueProp?.GetValue(current);
            if (unwrapped == null)
            {
                break;
            }

            current = unwrapped;
        }

        return current ?? value;
    }

    private static bool HasFFmpegPipewireSupport()
    {
        try
        {
            string ffmpegPath = PathsManager.GetFFmpegPath();
            if (string.IsNullOrEmpty(ffmpegPath) || !File.Exists(ffmpegPath))
            {
                ffmpegPath = "ffmpeg";
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = "-devices",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return false;

            string output = process.StandardError.ReadToEnd();
            process.WaitForExit(5000);

            return output.Contains("pipewire", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool HasGStreamerPipewireSupport()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "gst-inspect-1.0",
                Arguments = "pipewiresrc",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return false;

            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool HasWfRecorder()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "which",
                Arguments = "wf-recorder",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return false;

            process.WaitForExit(3000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool CanUseWfRecorder(RecordingOptions options)
    {
        if (!HasWfRecorder())
        {
            return false;
        }

        // Keep window-mode behavior stable: current wf-recorder path doesn't implement window selection.
        if (options.Mode == CaptureMode.Window)
        {
            DebugHelper.WriteLine("[WaylandPortalRecording] Window mode requested; using portal path instead of wf-recorder.");
            return false;
        }

        if (!IsWlrootsCompositor())
        {
            DebugHelper.WriteLine("[WaylandPortalRecording] Non-wlroots compositor detected; preferring portal path over wf-recorder.");
            return false;
        }

        return true;
    }

    private static bool IsWlrootsCompositor()
    {
        // Strong signals for wlroots-based compositors.
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HYPRLAND_INSTANCE_SIGNATURE")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SWAYSOCK")))
        {
            return true;
        }

        string currentDesktop = (Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? string.Empty).ToUpperInvariant();
        string desktopSession = (Environment.GetEnvironmentVariable("DESKTOP_SESSION") ?? string.Empty).ToUpperInvariant();

        // Conservative allowlist for known wlroots families.
        return currentDesktop.Contains("HYPRLAND") ||
               currentDesktop.Contains("SWAY") ||
               currentDesktop.Contains("WLROOTS") ||
               desktopSession.Contains("HYPRLAND") ||
               desktopSession.Contains("SWAY");
    }

    private static (string executable, string arguments, bool useGStreamer) BuildRecordingCommand(RecordingOptions options, uint pipeWireNodeId, int sourceWidth = 0, int sourceHeight = 0)
    {
        var settings = options.Settings ?? new ScreenRecordingSettings();
        string outputPath = options.OutputPath ?? GetDefaultOutputPath();

        // Check if FFmpeg has pipewire support
        if (HasFFmpegPipewireSupport())
        {
            return ("ffmpeg", BuildFFmpegArguments(options, pipeWireNodeId, outputPath), false);
        }

        // Fall back to GStreamer if available
        if (HasGStreamerPipewireSupport())
        {
            DebugHelper.WriteLine("[WaylandPortalRecording] FFmpeg lacks pipewire support, using GStreamer");
            // -e flag: send EOS on SIGINT for proper file finalization
            var (pipeline, actualOutputPath) = BuildGStreamerPipeline(options, pipeWireNodeId, outputPath, sourceWidth, sourceHeight);

            // Update options with the actual output path (may differ if muxer changed, e.g. .mp4 -> .mkv)
            if (!string.Equals(outputPath, actualOutputPath, StringComparison.Ordinal))
            {
                options.OutputPath = actualOutputPath;
                DebugHelper.WriteLine($"[WaylandPortalRecording] Output path changed to: {actualOutputPath}");
            }

            return ("gst-launch-1.0", "-e " + pipeline, true);
        }

        // Last resort: try FFmpeg anyway (will likely fail)
        DebugHelper.WriteLine("[WaylandPortalRecording] WARNING: Neither FFmpeg pipewire nor GStreamer available");
        return ("ffmpeg", BuildFFmpegArguments(options, pipeWireNodeId, outputPath), false);
    }

    private static string BuildFFmpegArguments(RecordingOptions options, uint pipeWireNodeId, string outputPath)
    {
        var settings = options.Settings ?? new ScreenRecordingSettings();
        bool hasAudio = settings.CaptureSystemAudio || settings.CaptureMicrophone;

        // Video input (input 0)
        var args = new List<string>
        {
            "-f pipewire",
            "-framerate " + settings.FPS.ToString(CultureInfo.InvariantCulture),
            $"-i {pipeWireNodeId}"
        };

        // Audio input (input 1) — must come right after video input, before codec/output args
        if (hasAudio)
        {
            args.Add("-f pulse");

            if (settings.CaptureSystemAudio)
            {
                string monitorSource = PulseAudioHelper.GetDefaultMonitorSource();
                args.Add($"-i {monitorSource}");
            }
            else
            {
                args.Add(!string.IsNullOrEmpty(settings.MicrophoneDeviceId)
                    ? $"-i {settings.MicrophoneDeviceId}"
                    : "-i default");
            }
        }

        // Stream mapping — required when there are multiple inputs
        if (hasAudio)
        {
            args.Add("-map 0:v");
            args.Add("-map 1:a");
        }

        if (options.Mode == CaptureMode.Region && options.Region.Width > 0 && options.Region.Height > 0)
        {
            args.Add($"-vf \"crop={options.Region.Width}:{options.Region.Height}:{options.Region.X}:{options.Region.Y}\"");
        }

        switch (settings.Codec)
        {
            case VideoCodec.H264:
                args.Add("-c:v libx264");
                args.Add("-preset ultrafast");
                args.Add($"-b:v {settings.BitrateKbps}k");
                break;
            case VideoCodec.HEVC:
                args.Add("-c:v libx265");
                args.Add("-preset ultrafast");
                args.Add($"-b:v {settings.BitrateKbps}k");
                break;
            case VideoCodec.VP9:
                args.Add("-c:v libvpx-vp9");
                args.Add($"-b:v {settings.BitrateKbps}k");
                break;
            case VideoCodec.AV1:
                args.Add("-c:v libaom-av1");
                args.Add($"-b:v {settings.BitrateKbps}k");
                break;
        }

        if (hasAudio)
        {
            args.Add("-c:a aac");
            args.Add("-b:a 192k");
        }

        args.Add("-pix_fmt yuv420p");
        args.Add("-y");
        args.Add($"\"{outputPath}\"");

        return string.Join(" ", args);
    }

    // Cached GStreamer element availability
    private static readonly ConcurrentDictionary<string, bool> _gstElementCache = new();

    private static bool HasGStreamerElement(string elementName)
    {
        return _gstElementCache.GetOrAdd(elementName, name =>
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "gst-inspect-1.0",
                    Arguments = name,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null) return false;

                process.WaitForExit(3000);
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        });
    }

    private static (string pipeline, string actualOutputPath) BuildGStreamerPipeline(RecordingOptions options, uint pipeWireNodeId, string outputPath, int sourceWidth = 0, int sourceHeight = 0, bool useGl = true)
    {
        var settings = options.Settings ?? new ScreenRecordingSettings();
        bool hasAudio = settings.CaptureSystemAudio || settings.CaptureMicrophone;

        // Build pipeline with queue for buffering large frames.
        var pipeline = new List<string>();
        pipeline.Add($"pipewiresrc path={pipeWireNodeId} do-timestamp=true");

        if (useGl && HasGStreamerElement("gldownload") && HasGStreamerElement("glupload"))
        {
            // GPU path: let glupload negotiate directly with pipewiresrc.
            // glupload is designed to handle DMA-BUF from PipeWire natively.
            // DO NOT insert a video/x-raw caps filter here: pipewiresrc on Wayland often
            // only offers DMA-BUF (video/x-raw(memory:DMABuf)), and the plain video/x-raw
            // filter rejects that, causing "streaming stopped, reason not-negotiated (-4)".
            // NOTE: on some systems glupload itself fails with "unhandled format" — if that
            // happens the caller retries with useGl=false (the CPU path below).
            pipeline.AddRange(new[] { "!", "glupload", "!", "glcolorconvert", "!", "gldownload" });
        }
        // CPU path: no GL, no caps filter.
        // videoconvert below handles all pipewiresrc output formats (raw memory and DMA-BUF).

        // queue to decouple and handle buffering
        pipeline.AddRange(new[] { "!", "queue max-size-buffers=3 leaky=downstream" });
        // videoconvert handles any format conversion needed
        pipeline.AddRange(new[] { "!", "videoconvert" });

        // Add crop filter for region capture.
        if (options.Mode == CaptureMode.Region && options.Region.Width > 0 && options.Region.Height > 0)
        {
            // GStreamer videocrop removes pixels from each edge (left, top, right, bottom).
            // We MUST specify all four edges; omitting right/bottom leaves them at 0, meaning the
            // full screen width/height minus only the left/top crop is passed to videoscale, which
            // then squashes a wider-than-intended frame into the target dimensions.
            int cropLeft = Math.Max(0, options.Region.X);
            int cropTop = Math.Max(0, options.Region.Y);

            string cropElement;
            if (sourceWidth > 0 && sourceHeight > 0)
            {
                // Source size known from portal stream properties — compute exact right/bottom crops.
                int cropRight = Math.Max(0, sourceWidth - cropLeft - options.Region.Width);
                int cropBottom = Math.Max(0, sourceHeight - cropTop - options.Region.Height);
                cropElement = $"videocrop left={cropLeft} top={cropTop} right={cropRight} bottom={cropBottom}";
                DebugHelper.WriteLine($"[WaylandPortalRecording] videocrop: left={cropLeft} top={cropTop} right={cropRight} bottom={cropBottom} (source={sourceWidth}x{sourceHeight})");
            }
            else
            {
                // Source size unknown; crop left/top only. videoscale will still scale to the target
                // dimensions, but may squash if the source is wider than the region.
                cropElement = $"videocrop left={cropLeft} top={cropTop}";
                DebugHelper.WriteLine($"[WaylandPortalRecording] videocrop: left={cropLeft} top={cropTop} (source size unknown, right/bottom not cropped)");
            }

            pipeline.Add("!");
            pipeline.Add(cropElement);
            pipeline.Add("!");
            pipeline.Add("videoconvert");
            pipeline.Add("!");
            pipeline.Add("videoscale");
            pipeline.Add("!");
            pipeline.Add($"video/x-raw,width={options.Region.Width},height={options.Region.Height}");
            // Final videoconvert to ensure encoder-compatible format
            pipeline.Add("!");
            pipeline.Add("videoconvert");
        }

        // Get encoder and muxer based on requested codec and available elements
        var (encoderElement, muxerElement, fileExtension) = GetEncoderAndMuxer(settings.Codec, settings.BitrateKbps);

        pipeline.Add("!");
        pipeline.Add(encoderElement);

        // Adjust output path extension if needed
        string finalOutputPath = outputPath;
        if (!string.IsNullOrEmpty(fileExtension))
        {
            string dir = Path.GetDirectoryName(outputPath) ?? "";
            string baseName = Path.GetFileNameWithoutExtension(outputPath);
            finalOutputPath = Path.Combine(dir, baseName + fileExtension);
        }

        if (hasAudio)
        {
            // Audio branch: pulsesrc -> queue -> audioconvert -> encoder -> mux.
            string audioDevice;
            if (settings.CaptureSystemAudio)
            {
                audioDevice = PulseAudioHelper.GetDefaultMonitorSource();
            }
            else
            {
                audioDevice = !string.IsNullOrEmpty(settings.MicrophoneDeviceId)
                    ? settings.MicrophoneDeviceId
                    : "default";
            }

            var (audioEncoder, actualMuxer, actualExtension) = GetCompatibleAudioEncoder(muxerElement, fileExtension);

            // Update output path if the muxer/extension changed (e.g. mp4 -> mkv for Opus)
            if (actualExtension != fileExtension)
            {
                string dir = Path.GetDirectoryName(finalOutputPath) ?? "";
                string baseName = Path.GetFileNameWithoutExtension(finalOutputPath);
                finalOutputPath = Path.Combine(dir, baseName + actualExtension);
            }

            // Use a named muxer so both video and audio branches can connect
            pipeline.Add("!");
            pipeline.Add($"{actualMuxer} name=mux");
            pipeline.Add("!");
            pipeline.Add($"filesink location=\"{finalOutputPath}\"");

            pipeline.Add($"pulsesrc device=\"{audioDevice}\"");
            pipeline.Add("!");
            pipeline.Add("queue");
            pipeline.Add("!");
            pipeline.Add("audioconvert");
            pipeline.Add("!");
            pipeline.Add(audioEncoder);
            pipeline.Add("!");
            pipeline.Add("mux.");

            DebugHelper.WriteLine($"[WaylandPortalRecording] GStreamer audio: device={audioDevice}, encoder={audioEncoder}, muxer={actualMuxer}");
        }
        else
        {
            // Simple pipeline without named muxer
            pipeline.Add("!");
            pipeline.Add(muxerElement);
            pipeline.Add("!");
            pipeline.Add($"filesink location=\"{finalOutputPath}\"");
        }

        DebugHelper.WriteLine($"[WaylandPortalRecording] GStreamer encoder: {encoderElement.Split(' ')[0]}, muxer: {muxerElement}, output: {finalOutputPath}");

        return (string.Join(" ", pipeline), finalOutputPath);
    }

    /// <summary>
    /// Returns a compatible (audioEncoder, muxer, extension) tuple.
    /// MP4 requires AAC; if only Opus is available, falls back to Matroska container.
    /// </summary>
    private static (string audioEncoder, string muxer, string extension) GetCompatibleAudioEncoder(string requestedMuxer, string requestedExtension)
    {
        bool isMp4 = requestedMuxer.Contains("mp4", StringComparison.OrdinalIgnoreCase);

        // AAC encoders work in both MP4 and MKV
        if (HasGStreamerElement("avenc_aac"))
            return ("avenc_aac", requestedMuxer, requestedExtension);
        if (HasGStreamerElement("voaacenc"))
            return ("voaacenc", requestedMuxer, requestedExtension);
        if (HasGStreamerElement("fdkaacenc"))
            return ("fdkaacenc", requestedMuxer, requestedExtension);

        // Opus is available but incompatible with MP4 — switch to MKV
        if (HasGStreamerElement("opusenc"))
        {
            if (isMp4)
            {
                DebugHelper.WriteLine("[WaylandPortalRecording] No AAC encoder available; switching from mp4mux to matroskamux for Opus compatibility");
                return ("opusenc", "matroskamux", ".mkv");
            }

            return ("opusenc", requestedMuxer, requestedExtension);
        }

        // Vorbis works in WebM/MKV/OGG
        if (HasGStreamerElement("vorbisenc"))
        {
            if (isMp4)
            {
                DebugHelper.WriteLine("[WaylandPortalRecording] No AAC/Opus encoder available; switching from mp4mux to matroskamux for Vorbis");
                return ("vorbisenc", "matroskamux", ".mkv");
            }

            return ("vorbisenc", requestedMuxer, requestedExtension);
        }

        // Last resort: raw audio in MKV (will work but produce large files)
        DebugHelper.WriteLine("[WaylandPortalRecording] No audio encoder found, using raw audio in MKV");
        return ("identity", "matroskamux", ".mkv");
    }

    private static (string encoder, string muxer, string extension) GetEncoderAndMuxer(VideoCodec codec, int bitrateKbps)
    {
        // Try to find an available encoder for the requested codec
        // Preference: Hardware (NVIDIA) > Software > Fallback to VP9

        switch (codec)
        {
            case VideoCodec.H264:
                // Try NVIDIA hardware encoder first
                if (HasGStreamerElement("nvh264enc"))
                {
                    DebugHelper.WriteLine("[WaylandPortalRecording] Using NVIDIA H.264 hardware encoder");
                    return ($"nvh264enc bitrate={bitrateKbps} preset=low-latency ! h264parse ! video/x-h264,profile=main", "mp4mux", ".mp4");
                }
                // Try software x264enc
                if (HasGStreamerElement("x264enc"))
                {
                    return ($"x264enc tune=zerolatency bitrate={bitrateKbps} speed-preset=ultrafast ! video/x-h264,profile=main", "mp4mux", ".mp4");
                }
                // Fallback to VP9
                DebugHelper.WriteLine("[WaylandPortalRecording] H.264 encoder not available, falling back to VP9");
                return GetVP9Encoder(bitrateKbps);

            case VideoCodec.HEVC:
                // Try NVIDIA hardware encoder first
                if (HasGStreamerElement("nvh265enc"))
                {
                    DebugHelper.WriteLine("[WaylandPortalRecording] Using NVIDIA H.265 hardware encoder");
                    return ($"nvh265enc bitrate={bitrateKbps} preset=low-latency ! h265parse", "mp4mux", ".mp4");
                }
                // Try software x265enc
                if (HasGStreamerElement("x265enc"))
                {
                    return ($"x265enc tune=zerolatency bitrate={bitrateKbps} speed-preset=ultrafast", "mp4mux", ".mp4");
                }
                // Fallback to VP9
                DebugHelper.WriteLine("[WaylandPortalRecording] H.265 encoder not available, falling back to VP9");
                return GetVP9Encoder(bitrateKbps);

            case VideoCodec.VP9:
                return GetVP9Encoder(bitrateKbps);

            case VideoCodec.AV1:
                if (HasGStreamerElement("av1enc"))
                {
                    return ($"av1enc target-bitrate={bitrateKbps * 1000}", "webmmux", ".webm");
                }
                // Fallback to VP9
                DebugHelper.WriteLine("[WaylandPortalRecording] AV1 encoder not available, falling back to VP9");
                return GetVP9Encoder(bitrateKbps);

            default:
                // Default: try H.264 path
                return GetEncoderAndMuxer(VideoCodec.H264, bitrateKbps);
        }
    }

    private static (string encoder, string muxer, string extension) GetVP9Encoder(int bitrateKbps)
    {
        if (HasGStreamerElement("vp9enc"))
        {
            // Keep VP9 options conservative for compatibility across plugin versions.
            // Some distributions reject deadline=realtime and fail pipeline creation.
            string encoderArgs = $"vp9enc target-bitrate={bitrateKbps * 1000} cpu-used=8";
            DebugHelper.WriteLine("[WaylandPortalRecording] VP9 compatibility profile selected (deadline omitted)");
            DebugHelper.WriteLine($"[WaylandPortalRecording] VP9 encoder args: {encoderArgs}");
            return (encoderArgs, "webmmux", ".webm");
        }

        // Last resort: Theora (almost always available but lower quality)
        if (HasGStreamerElement("theoraenc"))
        {
            DebugHelper.WriteLine("[WaylandPortalRecording] Using Theora encoder as last resort");
            return ($"theoraenc bitrate={bitrateKbps}", "oggmux", ".ogv");
        }

        throw new InvalidOperationException("No suitable video encoder found. Please install gst-plugins-good (for VP9) or gst-plugins-ugly (for H.264).");
    }

    private static string GetDefaultOutputPath()
    {
        string screencastsFolder = PathsManager.ScreencastsFolder;
        Directory.CreateDirectory(screencastsFolder);
        string fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.mp4";
        return Path.Combine(screencastsFolder, fileName);
    }

    private void UpdateStatus(RecordingStatus newStatus)
    {
        lock (_lock)
        {
            if (_status == newStatus) return;
            _status = newStatus;
            var duration = _stopwatch.Elapsed;

            // Start/stop duration timer based on recording status
            if (newStatus == RecordingStatus.Recording)
            {
                // Fire duration updates every 100ms for smooth timer display
                _durationTimer = new Timer(_ =>
                {
                    lock (_lock)
                    {
                        if (_status == RecordingStatus.Recording)
                        {
                            StatusChanged?.Invoke(this, new RecordingStatusEventArgs(RecordingStatus.Recording, _stopwatch.Elapsed));
                        }
                    }
                }, null, 100, 100);
            }
            else
            {
                _durationTimer?.Dispose();
                _durationTimer = null;
            }

            StatusChanged?.Invoke(this, new RecordingStatusEventArgs(newStatus, duration));
        }
    }

    private void HandleFatalError(Exception ex, bool isFatal)
    {
        lock (_lock)
        {
            if (_status != RecordingStatus.Error)
            {
                UpdateStatus(RecordingStatus.Error);
            }
        }

        ErrorOccurred?.Invoke(this, new RecordingErrorEventArgs(ex, isFatal));

        if (isFatal)
        {
            try
            {
                _ffmpeg?.Close();
            }
            catch
            {
                // Ignore cleanup errors
            }

            _ffmpeg = null;

            // Clean up the portal session immediately so the DBus Connection is not
            // left open until GC. An open Connection whose finalizer fires a close
            // message on a dead portal session produces an unobserved task exception
            // (org.freedesktop.DBus.Error.ServiceUnknown).
            CleanupPortalSession();
        }
    }
}

[DBusInterface("org.freedesktop.portal.ScreenCast")]
public interface IScreenCastPortal : IDBusObject
{
    Task<ObjectPath> CreateSessionAsync(IDictionary<string, object> options);
    Task<ObjectPath> SelectSourcesAsync(ObjectPath sessionHandle, IDictionary<string, object> options);
    Task<ObjectPath> StartAsync(ObjectPath sessionHandle, string parentWindow, IDictionary<string, object> options);
}
