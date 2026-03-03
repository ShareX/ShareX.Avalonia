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
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ShareX.ImageEditor.Helpers;
using ShareX.ImageEditor.ViewModels;
using SkiaSharp;
using System.IO;
using XerahS.Common;
using XerahS.Core;
using XerahS.Uploaders.PluginSystem;

namespace XerahS.UI.Services;

/// <summary>
/// Helper class for wiring up MainViewModel events to host application infrastructure.
/// </summary>
public static class MainViewModelHelper
{
    /// <summary>
    /// Wires up the UploadRequested event to the XerahS upload pipeline.
    /// </summary>
    public static void WireUploadRequested(MainViewModel viewModel)
    {
        viewModel.UploadRequested += () =>
        {
            _ = HandleUploadRequestedAsync(viewModel);
        };
    }

    /// <summary>
    /// Wires up the CopyRequested event to copy the image to the system clipboard.
    /// When getEditedSnapshot is provided, uses the rendered image (with annotations) instead of the base preview.
    /// </summary>
    public static void WireCopyRequested(MainViewModel viewModel, Func<SkiaSharp.SKBitmap?>? getEditedSnapshot = null)
    {
        viewModel.CopyRequested += () =>
        {
            HandleCopyRequested(viewModel, getEditedSnapshot);
        };
    }

    /// <summary>
    /// Wires up the SaveRequested event to overwrite the last saved path, or falls back to SaveAs.
    /// </summary>
    public static void WireSaveRequested(MainViewModel viewModel, Func<SKBitmap?>? getEditedSnapshot = null, Func<Window?>? getWindow = null)
    {
        viewModel.SaveRequested += () =>
        {
            _ = HandleSaveRequestedAsync(viewModel, getEditedSnapshot, getWindow);
        };
    }

    /// <summary>
    /// Wires up the SaveAsRequested event to show a save file dialog and save to the chosen path.
    /// </summary>
    public static void WireSaveAsRequested(MainViewModel viewModel, Func<SKBitmap?>? getEditedSnapshot = null, Func<Window?>? getWindow = null)
    {
        viewModel.SaveAsRequested += () =>
        {
            _ = HandleSaveAsRequestedAsync(viewModel, getEditedSnapshot, getWindow);
        };
    }

    private static async Task HandleSaveRequestedAsync(MainViewModel viewModel, Func<SKBitmap?>? getEditedSnapshot, Func<Window?>? getWindow)
    {
        DebugHelper.WriteLine("MainViewModelHelper: SaveRequested received");
        try
        {
            if (!string.IsNullOrEmpty(viewModel.LastSavedPath))
            {
                SaveToPath(viewModel, getEditedSnapshot, viewModel.LastSavedPath);
            }
            else
            {
                await HandleSaveAsRequestedAsync(viewModel, getEditedSnapshot, getWindow);
            }
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine($"Editor save failed: {ex.Message}");
            DebugHelper.WriteException(ex);
        }
    }

    private static async Task HandleSaveAsRequestedAsync(MainViewModel viewModel, Func<SKBitmap?>? getEditedSnapshot, Func<Window?>? getWindow)
    {
        DebugHelper.WriteLine("MainViewModelHelper: SaveAsRequested received");
        try
        {
            var window = getWindow?.Invoke();
            var topLevel = window != null ? TopLevel.GetTopLevel(window) : null;
            if (topLevel?.StorageProvider == null)
            {
                DebugHelper.WriteLine("MainViewModelHelper: SaveAs — no storage provider available.");
                return;
            }

            string suggestedName = string.IsNullOrEmpty(viewModel.LastSavedPath)
                ? "image.png"
                : Path.GetFileName(viewModel.LastSavedPath);

            var options = new FilePickerSaveOptions
            {
                Title = "Save Image As",
                SuggestedFileName = suggestedName,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("PNG Image") { Patterns = new[] { "*.png" } },
                    new FilePickerFileType("JPEG Image") { Patterns = new[] { "*.jpg", "*.jpeg" } },
                },
                DefaultExtension = "png"
            };

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(options);
            var path = file?.TryGetLocalPath();
            if (string.IsNullOrEmpty(path))
            {
                DebugHelper.WriteLine("MainViewModelHelper: SaveAs cancelled or path unavailable.");
                return;
            }

            SaveToPath(viewModel, getEditedSnapshot, path);
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine($"Editor save-as failed: {ex.Message}");
            DebugHelper.WriteException(ex);
        }
    }

    private static void SaveToPath(MainViewModel viewModel, Func<SKBitmap?>? getEditedSnapshot, string path)
    {
        SKBitmap? bitmap = getEditedSnapshot?.Invoke();
        if (bitmap == null && viewModel.PreviewImage != null)
            bitmap = BitmapConversionHelpers.ToSKBitmap(viewModel.PreviewImage);

        if (bitmap == null)
        {
            DebugHelper.WriteLine("MainViewModelHelper: SaveToPath — no image to save.");
            return;
        }

        using (bitmap)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            var format = ext is ".jpg" or ".jpeg" ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png;
            int quality = format == SKEncodedImageFormat.Jpeg ? 95 : 100;
            using var data = bitmap.Encode(format, quality);
            using var stream = File.OpenWrite(path);
            data.SaveTo(stream);
        }

        viewModel.LastSavedPath = path;
        viewModel.IsDirty = false;
        DebugHelper.WriteLine($"MainViewModelHelper: Image saved to '{path}'");
    }

    private static async Task HandleUploadRequestedAsync(MainViewModel viewModel)
    {
        DebugHelper.WriteLine("MainViewModelHelper: UploadRequested received");

        try
        {
            if (viewModel.PreviewImage == null)
            {
                DebugHelper.WriteLine("MainViewModelHelper: UploadRequested ignored because PreviewImage is null.");
                return;
            }

            // Convert Avalonia Bitmap to SKBitmap for upload pipeline.
            using var skBitmap = ShareX.ImageEditor.Helpers.BitmapConversionHelpers.ToSKBitmap(viewModel.PreviewImage);
            DebugHelper.WriteLine($"MainViewModelHelper: Converted bitmap {skBitmap.Width}x{skBitmap.Height}");

            // Get the default image uploader instance, or first available.
            string? destinationInstanceId = null;
            var defaultInstance = InstanceManager.Instance.GetDefaultInstance(UploaderCategory.Image);
            if (defaultInstance != null)
            {
                destinationInstanceId = defaultInstance.InstanceId;
                DebugHelper.WriteLine($"MainViewModelHelper: Using default instance: {defaultInstance.DisplayName} ({destinationInstanceId})");
            }
            else
            {
                // Fall back to first available image uploader.
                var imageInstances = InstanceManager.Instance.GetInstancesByCategory(UploaderCategory.Image);
                var firstInstance = imageInstances.FirstOrDefault();
                if (firstInstance != null)
                {
                    destinationInstanceId = firstInstance.InstanceId;
                    DebugHelper.WriteLine($"MainViewModelHelper: Using first instance: {firstInstance.DisplayName} ({destinationInstanceId})");
                }
                else
                {
                    DebugHelper.WriteLine("MainViewModelHelper: No image uploader instances available.");
                }
            }

            var taskSettings = new TaskSettings
            {
                Job = WorkflowType.None,
                AfterCaptureJob = AfterCaptureTasks.UploadImageToHost,
                AfterUploadJob = AfterUploadTasks.CopyURLToClipboard,
                DestinationInstanceId = destinationInstanceId
            };

            DebugHelper.WriteLine($"MainViewModelHelper: TaskSettings created - Job={taskSettings.Job}, AfterCapture={taskSettings.AfterCaptureJob}, AfterUpload={taskSettings.AfterUploadJob}, DestId={taskSettings.DestinationInstanceId}");

            DebugHelper.WriteLine("MainViewModelHelper: Calling TaskManager.StartTask...");
            await Core.Managers.TaskManager.Instance.StartTask(taskSettings, skBitmap.Copy());
            DebugHelper.WriteLine("MainViewModelHelper: TaskManager.StartTask completed");
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine($"Editor upload failed: {ex.Message}");
            DebugHelper.WriteException(ex);
        }
    }

    private static void HandleCopyRequested(MainViewModel viewModel, Func<SkiaSharp.SKBitmap?>? getEditedSnapshot = null)
    {
        DebugHelper.WriteLine("MainViewModelHelper: CopyRequested received");

        try
        {
            // Prefer edited snapshot (with annotations) over base preview image
            SkiaSharp.SKBitmap? imageToCopy = null;
            if (getEditedSnapshot != null)
            {
                imageToCopy = getEditedSnapshot();
                if (imageToCopy != null)
                    DebugHelper.WriteLine($"MainViewModelHelper: Using edited snapshot {imageToCopy.Width}x{imageToCopy.Height} for clipboard");
            }

            if (imageToCopy == null && viewModel.PreviewImage != null)
            {
                imageToCopy = ShareX.ImageEditor.Helpers.BitmapConversionHelpers.ToSKBitmap(viewModel.PreviewImage);
                if (imageToCopy != null)
                    DebugHelper.WriteLine($"MainViewModelHelper: Using preview image {imageToCopy.Width}x{imageToCopy.Height} for clipboard");
            }

            if (imageToCopy == null)
            {
                DebugHelper.WriteLine("MainViewModelHelper: CopyRequested ignored because no image available.");
                return;
            }

            // Use the platform clipboard service (set up via EditorClipboardAdapter).
            if (Platform.Abstractions.PlatformServices.IsInitialized)
            {
                Platform.Abstractions.PlatformServices.Clipboard.SetImage(imageToCopy.Copy());
                DebugHelper.WriteLine("MainViewModelHelper: Image copied to clipboard");
            }
            else
            {
                DebugHelper.WriteLine("MainViewModelHelper: Platform clipboard not initialized");
            }
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine($"Editor copy to clipboard failed: {ex.Message}");
            DebugHelper.WriteException(ex);
        }
    }
}
