#region License Information (GPL v3)

/*
    ShareX.Ava - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2025 ShareX Team

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

using System.Runtime.InteropServices;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Vortice.Direct3D11;
using Vortice.DXGI;
using XerahS.ScreenCapture.Recording;

namespace XerahS.Platform.Windows.Recording;

/// <summary>
/// Windows.Graphics.Capture implementation for modern screen recording
/// Requires Windows 10 version 1803 (build 17134) or later
/// </summary>
public class WindowsGraphicsCaptureSource : ICaptureSource
{
    private GraphicsCaptureItem? _captureItem;
    private Direct3D11CaptureFramePool? _framePool;
    private GraphicsCaptureSession? _session;
    private ID3D11Device? _d3dDevice;
    private IDirect3DDevice? _device;
    private readonly object _lock = new();
    private bool _isCapturing;
    private bool _disposed;

    /// <summary>
    /// Check if Windows.Graphics.Capture is supported on this system
    /// </summary>
    public static bool IsSupported
    {
        get
        {
            try
            {
                // Check Windows version >= 10.0.17134 (1803)
                var version = Environment.OSVersion.Version;
                if (version.Major < 10) return false;
                if (version.Build < 17134) return false;

                // Try to create a test capture item to verify API availability
                return GraphicsCaptureSession.IsSupported();
            }
            catch
            {
                return false;
            }
        }
    }

    public event EventHandler<FrameArrivedEventArgs>? FrameArrived;

    /// <summary>
    /// Initialize capture for a specific window
    /// </summary>
    public void InitializeForWindow(IntPtr hwnd)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(WindowsGraphicsCaptureSource));

        try
        {
            // Create Direct3D device
            _d3dDevice = CreateD3DDevice();
            _device = CreateDirect3DDeviceFromD3D11Device(_d3dDevice);

            // Create capture item from window handle
            _captureItem = CaptureHelper.CreateItemForWindow(hwnd);
            if (_captureItem == null)
            {
                throw new InvalidOperationException("Failed to create capture item for window");
            }

            // Create frame pool
            _framePool = Direct3D11CaptureFramePool.Create(
                _device,
                Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                2, // Number of buffers
                _captureItem.Size);

            _framePool.FrameArrived += OnFrameArrived;
        }
        catch (Exception ex)
        {
            Dispose();
            throw new PlatformNotSupportedException(
                "Failed to initialize Windows.Graphics.Capture. This may be due to Windows version < 1803 or missing permissions.",
                ex);
        }
    }

    /// <summary>
    /// Initialize capture for the primary monitor
    /// </summary>
    public void InitializeForPrimaryMonitor()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(WindowsGraphicsCaptureSource));

        try
        {
            _d3dDevice = CreateD3DDevice();
            _device = CreateDirect3DDeviceFromD3D11Device(_d3dDevice);

            _captureItem = CaptureHelper.CreateItemForMonitor(GetPrimaryMonitorHandle());
            if (_captureItem == null)
            {
                throw new InvalidOperationException("Failed to create capture item for monitor");
            }

            _framePool = Direct3D11CaptureFramePool.Create(
                _device,
                Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                2,
                _captureItem.Size);

            _framePool.FrameArrived += OnFrameArrived;
        }
        catch (Exception ex)
        {
            Dispose();
            throw new PlatformNotSupportedException(
                "Failed to initialize Windows.Graphics.Capture for monitor.",
                ex);
        }
    }

    public Task StartCaptureAsync()
    {
        lock (_lock)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(WindowsGraphicsCaptureSource));
            if (_isCapturing) return Task.CompletedTask;
            if (_captureItem == null || _framePool == null)
            {
                throw new InvalidOperationException("Capture source not initialized. Call InitializeForWindow or InitializeForPrimaryMonitor first.");
            }

            _session = _framePool.CreateCaptureSession(_captureItem);
            _session.IsCursorCaptureEnabled = true; // Capture cursor by default
            _session.StartCapture();
            _isCapturing = true;
        }

        return Task.CompletedTask;
    }

    public Task StopCaptureAsync()
    {
        lock (_lock)
        {
            if (!_isCapturing) return Task.CompletedTask;

            _session?.Dispose();
            _session = null;
            _isCapturing = false;
        }

        return Task.CompletedTask;
    }

    private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        if (_disposed || !_isCapturing) return;

        try
        {
            using var frame = sender.TryGetNextFrame();
            if (frame == null) return;

            // Get Direct3D surface
            var surface = frame.Surface;
            if (surface == null) return;

            // Convert to FrameData
            var frameData = ConvertSurfaceToFrameData(surface, frame.SystemRelativeTime);

            // Raise event on capture thread
            // Note: Encoder must handle thread marshaling if needed
            FrameArrived?.Invoke(this, new FrameArrivedEventArgs(frameData));
        }
        catch (Exception ex)
        {
            // Log error but don't crash capture thread
            System.Diagnostics.Debug.WriteLine($"Error processing captured frame: {ex.Message}");
        }
    }

    private FrameData ConvertSurfaceToFrameData(IDirect3DSurface surface, TimeSpan systemRelativeTime)
    {
        // Get the underlying Direct3D11 texture
        var access = surface as Windows.Graphics.DirectX.Direct3D11.IDirect3DDxgiInterfaceAccess;
        if (access == null)
        {
            throw new InvalidOperationException("Failed to get Direct3D interface access");
        }

        var dxgiResource = access.GetInterface(typeof(IDXGISurface).GUID);
        var dxgiSurface = Marshal.GetObjectForIUnknown(dxgiResource) as IDXGISurface;

        if (dxgiSurface == null)
        {
            throw new InvalidOperationException("Failed to get DXGI surface");
        }

        try
        {
            var desc = dxgiSurface.Description;

            // Map surface for CPU access
            var mapped = dxgiSurface.Map(MapFlags.Read);

            return new FrameData
            {
                DataPtr = mapped.DataPointer,
                Stride = mapped.Pitch,
                Width = (int)desc.Width,
                Height = (int)desc.Height,
                Timestamp = (long)(systemRelativeTime.TotalMilliseconds * 10000), // Convert to 100ns units
                Format = PixelFormat.Bgra32 // WGC always provides BGRA32
            };
        }
        finally
        {
            dxgiSurface.Unmap();
            Marshal.Release(dxgiResource);
        }
    }

    private static ID3D11Device CreateD3DDevice()
    {
        var result = D3D11.D3D11CreateDevice(
            null,
            Vortice.Direct3D.DriverType.Hardware,
            DeviceCreationFlags.BgraSupport,
            null,
            out var device,
            out _,
            out _);

        if (result.Failure || device == null)
        {
            throw new InvalidOperationException($"Failed to create Direct3D11 device: {result}");
        }

        return device;
    }

    private static IDirect3DDevice CreateDirect3DDeviceFromD3D11Device(ID3D11Device d3dDevice)
    {
        // Use Windows.Graphics.Capture interop to create IDirect3DDevice
        var dxgiDevice = d3dDevice.QueryInterface<IDXGIDevice>();
        var inspectable = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice);
        return (IDirect3DDevice)inspectable;
    }

    [DllImport("d3d11.dll", EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern uint CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

    private static object CreateDirect3D11DeviceFromDXGIDevice(IDXGIDevice dxgiDevice)
    {
        var hr = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out var pUnknown);
        if (hr != 0)
        {
            throw new COMException("Failed to create Direct3D11 device from DXGI device", (int)hr);
        }

        return Marshal.GetObjectForIUnknown(pUnknown);
    }

    private static IntPtr GetPrimaryMonitorHandle()
    {
        return MonitorFromPoint(new POINT { X = 0, Y = 0 }, MONITOR_DEFAULTTOPRIMARY);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    private const uint MONITOR_DEFAULTTOPRIMARY = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    public void Dispose()
    {
        if (_disposed) return;

        lock (_lock)
        {
            _disposed = true;
            _isCapturing = false;

            if (_framePool != null)
            {
                _framePool.FrameArrived -= OnFrameArrived;
                _framePool.Dispose();
                _framePool = null;
            }

            _session?.Dispose();
            _session = null;
            _captureItem = null;
            _device = null;
            _d3dDevice?.Dispose();
            _d3dDevice = null;
        }

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Helper class for creating GraphicsCaptureItem from HWND/HMONITOR
/// </summary>
internal static class CaptureHelper
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    public static GraphicsCaptureItem? CreateItemForWindow(IntPtr hwnd)
    {
        try
        {
            return GraphicsCaptureItem.TryCreateFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
        }
        catch
        {
            return null;
        }
    }

    public static GraphicsCaptureItem? CreateItemForMonitor(IntPtr hmonitor)
    {
        try
        {
            return GraphicsCaptureItem.TryCreateFromDisplayId(Win32Interop.GetDisplayIdFromMonitor(hmonitor));
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Win32 interop for getting WindowId and DisplayId
/// </summary>
internal static class Win32Interop
{
    public static Windows.UI.WindowId GetWindowIdFromWindow(IntPtr hwnd)
    {
        return Windows.UI.WindowId.CreateFromWin32(hwnd.ToInt64());
    }

    public static Windows.Graphics.DisplayId GetDisplayIdFromMonitor(IntPtr hmonitor)
    {
        return Windows.Graphics.DisplayId.CreateFromInt64(hmonitor.ToInt64());
    }
}
