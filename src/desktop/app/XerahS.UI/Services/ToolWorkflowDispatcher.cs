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
using System;
using System.IO;
using System.Threading.Tasks;
using XerahS.Common;
using XerahS.Core;
using XerahS.Platform.Abstractions;

namespace XerahS.UI.Services;

/// <summary>
/// Single dispatcher for tool workflow execution used by hotkeys and navigation.
/// </summary>
internal static class ToolWorkflowDispatcher
{
    public static bool TryDispatch(WorkflowType workflowType, Window? owner, TaskSettings? taskSettings, out Task dispatchTask)
    {
        var effectiveTaskSettings = taskSettings ?? new TaskSettings { Job = workflowType };

        switch (workflowType)
        {
            case WorkflowType.ColorPicker:
            case WorkflowType.ScreenColorPicker:
                dispatchTask = ColorPickerToolService.HandleWorkflowAsync(workflowType, owner);
                return true;

            case WorkflowType.OCR:
                dispatchTask = OcrToolService.HandleWorkflowAsync(workflowType, owner);
                return true;

            case WorkflowType.ScrollingCapture:
                dispatchTask = ScrollingCaptureToolService.HandleWorkflowAsync(workflowType, owner, effectiveTaskSettings);
                return true;

            case WorkflowType.ImageEditor:
                dispatchTask = OpenImageEditorAsync(owner);
                return true;

            case WorkflowType.VideoEditor:
                dispatchTask = OpenVideoEditorAsync(owner);
                return true;

            case WorkflowType.HashCheck:
                dispatchTask = HashCheckToolService.HandleWorkflowAsync(workflowType, owner);
                return true;

            case WorkflowType.PinToScreen:
            case WorkflowType.PinToScreenFromScreen:
            case WorkflowType.PinToScreenFromClipboard:
            case WorkflowType.PinToScreenFromFile:
            case WorkflowType.PinToScreenCloseAll:
                dispatchTask = PinToScreenToolService.HandleWorkflowAsync(workflowType, owner);
                return true;

            case WorkflowType.MonitorTest:
                dispatchTask = MonitorTestToolService.HandleWorkflowAsync(workflowType, owner);
                return true;

            case WorkflowType.Ruler:
                dispatchTask = RulerToolService.HandleWorkflowAsync(workflowType, owner);
                return true;

            case WorkflowType.AutoCapture:
            case WorkflowType.StartAutoCapture:
            case WorkflowType.StopAutoCapture:
                dispatchTask = AutoCaptureToolService.HandleWorkflowAsync(workflowType, owner);
                return true;

            case WorkflowType.ClipboardUploadWithContentViewer:
            case WorkflowType.ClipboardViewer:
                dispatchTask = UploadContentToolService.HandleWorkflowAsync(workflowType, owner);
                return true;

            case WorkflowType.ImageCombiner:
            case WorkflowType.ImageSplitter:
            case WorkflowType.ImageThumbnailer:
            case WorkflowType.VideoConverter:
            case WorkflowType.VideoThumbnailer:
            case WorkflowType.AnalyzeImage:
                dispatchTask = MediaToolsToolService.HandleWorkflowAsync(workflowType, owner);
                return true;

            case WorkflowType.QRCode:
            case WorkflowType.QRCodeDecodeFromScreen:
            case WorkflowType.QRCodeScanRegion:
                dispatchTask = QrCodeToolService.HandleWorkflowAsync(workflowType, owner);
                return true;

            default:
                dispatchTask = Task.CompletedTask;
                return false;
        }
    }

    private static async Task OpenImageEditorAsync(Window? owner)
    {
        try
        {
            var topLevel = owner != null ? TopLevel.GetTopLevel(owner) : null;
            if (topLevel == null)
            {
                return;
            }

            var options = new FilePickerOpenOptions
            {
                Title = "Open Image in Editor",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Image Files")
                    {
                        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.webp", "*.tiff", "*.tif"]
                    },
                    FilePickerFileTypes.All
                ]
            };

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
            if (files.Count < 1)
            {
                return;
            }

            var path = files[0].TryGetLocalPath();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            var skBitmap = SkiaSharp.SKBitmap.Decode(fs);
            if (skBitmap == null)
            {
                return;
            }

            await PlatformServices.UI.ShowEditorAsync(skBitmap);
        }
        catch (Exception ex)
        {
            DebugHelper.WriteException(ex, "Failed to open image in editor");
        }
    }

    private static async Task OpenVideoEditorAsync(Window? owner)
    {
        try
        {
            var topLevel = owner != null ? TopLevel.GetTopLevel(owner) : null;
            if (topLevel == null)
            {
                return;
            }

            var options = new FilePickerOpenOptions
            {
                Title = "Open Video in Editor",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Video Files")
                    {
                        Patterns = ["*.mp4", "*.webm", "*.mov", "*.mkv", "*.avi", "*.m4v", "*.gif"]
                    },
                    FilePickerFileTypes.All
                ]
            };

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
            if (files.Count < 1)
            {
                return;
            }

            var path = files[0].TryGetLocalPath();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            string ffmpegPath = PathsManager.GetFFmpegPath();
            await PlatformServices.UI.ShowVideoEditorAsync(path, string.IsNullOrEmpty(ffmpegPath) ? null : ffmpegPath);
        }
        catch (Exception ex)
        {
            DebugHelper.WriteException(ex, "Failed to open video in editor");
        }
    }
}
