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
using XerahS.RegionCapture.Models;
using XerahS.RegionCapture;
using XerahS.RegionCapture.UI;
using XerahS.Common;

namespace XerahS.RegionCapture.Services;

/// <summary>
/// Manages the lifecycle and coordination of per-monitor overlay windows.
/// This implements the "Per-Monitor Overlay" pattern (Strategy B) to bypass
/// mixed-DPI scaling artifacts common in single-span windows.
/// </summary>
public sealed class OverlayManager : IDisposable
{
    private readonly List<OverlayWindow> _overlays = [];
    private readonly TaskCompletionSource<RegionSelectionResult?> _completionSource;
    private readonly CoordinateTranslationService _coordinateService;
    private bool _disposed;

    public OverlayManager()
    {
        _completionSource = new TaskCompletionSource<RegionSelectionResult?>();
        _coordinateService = new CoordinateTranslationService();
    }

    /// <summary>
    /// Gets all active overlay windows.
    /// </summary>
    public IReadOnlyList<OverlayWindow> Overlays => _overlays;

    /// <summary>
    /// Gets the coordinate translation service for cross-monitor calculations.
    /// </summary>
    public CoordinateTranslationService CoordinateService => _coordinateService;

    /// <summary>
    /// Creates and shows overlay windows for all monitors.
    /// </summary>
    /// <summary>
    /// Creates and shows overlay windows for all monitors.
    /// </summary>
    public async Task<RegionSelectionResult?> ShowOverlaysAsync(
        Action<PixelRect>? onSelectionChanged = null,
        XerahS.Platform.Abstractions.CursorInfo? initialCursor = null,
        RegionCaptureOptions? options = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var monitors = _coordinateService.Monitors;

        if (monitors.Count == 0)
            return null;

        try
        {
            // Create one overlay per monitor
            foreach (var monitor in monitors)
            {
                var overlay = new OverlayWindow(monitor, _completionSource, onSelectionChanged, initialCursor, options);
                _overlays.Add(overlay);
            }

            // Determine primary overlay first so we can show and focus it before others (helps Linux/Wayland grant focus sooner)
            int primaryIndex = -1;
            for (int i = 0; i < monitors.Count; i++)
            {
                if (monitors[i].IsPrimary)
                {
                    primaryIndex = i;
                    break;
                }
            }

            var primaryOverlay = primaryIndex >= 0 && primaryIndex < _overlays.Count
                ? _overlays[primaryIndex]
                : null;

            // Show primary overlay first and focus it immediately so compositor has one clear focus target (reduces pointer-event delay on Wayland)
            if (primaryOverlay != null)
            {
                primaryOverlay.Show();
                primaryOverlay.Activate();
                primaryOverlay.Focus();
#if WINDOWS
                if (primaryOverlay.TryGetPlatformHandle()?.Handle is { } primaryHandle)
                {
                    Platform.Windows.NativeWindowService.ExcludeHandle(primaryHandle);
                }
#endif
            }

            // Show remaining overlays
            foreach (var overlay in _overlays)
            {
                if (overlay == primaryOverlay)
                    continue;
                overlay.Show();
                overlay.Activate();
#if WINDOWS
                if (overlay.TryGetPlatformHandle()?.Handle is { } handle)
                {
                    Platform.Windows.NativeWindowService.ExcludeHandle(handle);
                }
#endif
            }

            if (options?.SessionStartUtc is { } start)
            {
                double elapsedMs = (DateTime.UtcNow - start).TotalMilliseconds;
                DebugHelper.WriteLine($"[RegionCapture] Milestone: overlay displayed (+{elapsedMs:F0} ms)");
            }

            // Wait for result
            return await _completionSource.Task;
        }
        finally
        {
            CloseAllOverlays();
        }
    }

    private void CloseAllOverlays()
    {
        foreach (var overlay in _overlays)
        {
#if WINDOWS
            if (overlay.TryGetPlatformHandle()?.Handle is { } handle)
            {
                Platform.Windows.NativeWindowService.RemoveExcludedHandle(handle);
            }
#endif

            try
            {
                overlay.Close();
            }
            catch
            {
                // Ignore close errors
            }
        }

        _overlays.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        CloseAllOverlays();
        _completionSource.TrySetCanceled();
    }
}
