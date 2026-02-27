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

using System.IO;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using SkiaSharp;
using XerahS.Platform.Abstractions;

namespace XerahS.UI.Services;

/// <summary>
/// Implements <see cref="IClipboardService"/> using Avalonia's built-in <see cref="IClipboard"/> (no Windows Forms).
/// Use this for the desktop Avalonia app; register after MainWindow is created.
/// </summary>
public sealed class AvaloniaClipboardService : IClipboardService
{
    private readonly IClipboard _clipboard;
    private readonly IStorageProvider? _storageProvider;

    public AvaloniaClipboardService(IClipboard clipboard, IStorageProvider? storageProvider = null)
    {
        _clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
        _storageProvider = storageProvider;
    }

    public void Clear()
    {
        RunOnUIThread(() =>
        {
            _clipboard.ClearAsync().GetAwaiter().GetResult();
        });
    }

    public bool ContainsText()
    {
        return !string.IsNullOrEmpty(GetTextAsync().GetAwaiter().GetResult());
    }

    public bool ContainsImage()
    {
        return GetImage() != null;
    }

    public bool ContainsFileDropList()
    {
        var list = GetFileDropList();
        return list != null && list.Length > 0;
    }

    public string? GetText()
    {
        return RunOnUIThread(() => _clipboard.TryGetTextAsync().GetAwaiter().GetResult());
    }

    public void SetText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        RunOnUIThread(() =>
        {
            _clipboard.SetTextAsync(text).GetAwaiter().GetResult();
        });
    }

    public SKBitmap? GetImage()
    {
        return RunOnUIThread(() =>
        {
            var bitmap = _clipboard.TryGetBitmapAsync().GetAwaiter().GetResult();
            if (bitmap == null)
                return null;

            using var stream = new MemoryStream();
            bitmap.Save(stream);
            stream.Position = 0;
            return SKBitmap.Decode(stream);
        });
    }

    public void SetImage(SKBitmap image)
    {
        if (image == null)
            return;

        using var stream = new MemoryStream();
        image.Encode(stream, SKEncodedImageFormat.Png, 100);
        byte[] bytes = stream.ToArray();
        RunOnUIThread(() =>
        {
            SetImageBytesAsync(_clipboard, bytes).GetAwaiter().GetResult();
        });
    }

    /// <summary>
    /// Same Avalonia clipboard pattern as ShareX.ImageEditor.Loader App.axaml.cs (lines 65–81):
    /// bytes → MemoryStream → Bitmap → DataTransfer + DataTransferItem → SetDataAsync.
    /// Using SetDataAsync (and DataTransfer) ensures the same code path works on all OSes (Windows, macOS, Linux).
    /// </summary>
    internal static async Task SetImageBytesAsync(IClipboard clipboard, byte[] bytes)
    {
        if (clipboard == null || bytes == null || bytes.Length == 0)
            return;

        using (var stream = new MemoryStream(bytes))
        {
            var bitmap = new Bitmap(stream);
            var data = new DataTransfer();
            var item = new DataTransferItem();
            item.SetBitmap(bitmap);
            data.Add(item);
            await clipboard.SetDataAsync(data);
        }
    }

    public string[]? GetFileDropList()
    {
        return GetFileDropListAsync().GetAwaiter().GetResult();
    }

    public void SetFileDropList(string[] files)
    {
        if (files == null || files.Length == 0)
            return;
        SetFileDropListAsync(files).GetAwaiter().GetResult();
    }

    public object? GetData(string format)
    {
        return GetDataAsync(format).GetAwaiter().GetResult();
    }

    public void SetData(string format, object data)
    {
        if (string.IsNullOrEmpty(format) || data == null)
            return;
        SetDataAsync(format, data).GetAwaiter().GetResult();
    }

    public bool ContainsData(string format)
    {
        if (string.IsNullOrEmpty(format))
            return false;
        return ContainsDataAsync(format).GetAwaiter().GetResult();
    }

    public async Task<string?> GetTextAsync()
    {
        return await RunOnUIThreadAsync(() => _clipboard.TryGetTextAsync());
    }

    public async Task SetTextAsync(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        await RunOnUIThreadAsync(() => _clipboard.SetTextAsync(text));
    }

    private async Task<string[]?> GetFileDropListAsync()
    {
        return await RunOnUIThreadAsync(async () =>
        {
            var items = await _clipboard.TryGetFilesAsync();
            if (items == null || items.Length == 0)
                return null;

            var paths = new List<string>();
            foreach (var item in items)
            {
                var path = item?.TryGetLocalPath();
                if (!string.IsNullOrEmpty(path))
                    paths.Add(path);
            }

            return paths.Count > 0 ? paths.ToArray() : null;
        });
    }

    private async Task SetFileDropListAsync(string[] files)
    {
        if (_storageProvider == null)
            return;

        await RunOnUIThreadAsync(async () =>
        {
            var items = new List<IStorageItem?>();
            foreach (var path in files)
            {
                if (string.IsNullOrEmpty(path))
                    continue;

                var file = await _storageProvider.TryGetFileFromPathAsync(path);
                if (file != null)
                    items.Add(file);
            }

            if (items.Count > 0)
                await _clipboard.SetFilesAsync(items!);
        });
    }

    private async Task<object?> GetDataAsync(string format)
    {
        if (string.Equals(format, "text/plain", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(format, "Text", StringComparison.OrdinalIgnoreCase))
        {
            return await RunOnUIThreadAsync(() => _clipboard.TryGetTextAsync());
        }

        return null;
    }

    private async Task SetDataAsync(string format, object data)
    {
        if (data is string text)
        {
            await RunOnUIThreadAsync(() => _clipboard.SetTextAsync(text));
            return;
        }

        await RunOnUIThreadAsync(() => _clipboard.SetTextAsync(data.ToString() ?? string.Empty));
    }

    private async Task<bool> ContainsDataAsync(string format)
    {
        if (string.Equals(format, "text/plain", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(format, "Text", StringComparison.OrdinalIgnoreCase))
        {
            return !string.IsNullOrEmpty(await RunOnUIThreadAsync(() => _clipboard.TryGetTextAsync()));
        }

        return false;
    }

    private static void RunOnUIThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        Dispatcher.UIThread.InvokeAsync(action).GetAwaiter().GetResult();
    }

    private static T RunOnUIThread<T>(Func<T> func)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return func();
        }

        return Dispatcher.UIThread.InvokeAsync(func).GetAwaiter().GetResult();
    }

    private static async Task RunOnUIThreadAsync(Func<Task> func)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            await func();
            return;
        }

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                await func();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        await tcs.Task;
    }

    private static async Task<T> RunOnUIThreadAsync<T>(Func<Task<T>> func)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return await func();
        }

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                T value = await func();
                tcs.SetResult(value);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return await tcs.Task;
    }
}
