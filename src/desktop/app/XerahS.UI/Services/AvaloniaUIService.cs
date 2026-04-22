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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.Layout;
using Avalonia.Media;
using System.IO;
using XerahS.Bootstrap;
using XerahS.Common;
using XerahS.Core;
using XerahS.Platform.Abstractions;
using XerahS.UI.ViewModels;
using ShareX.ImageEditor.Core.Annotations;
using ShareX.ImageEditor.Hosting;
using ShareX.ImageEditor.Presentation.ViewModels;
using ShareX.ImageEditor.Presentation.Views;
using ShareX.VideoEditor.Hosting;
using SkiaSharp;

namespace XerahS.UI.Services
{
    public class AvaloniaUIService : IUIService
    {
        private IDesktopTaskManager? _taskManager;
        private bool _wasMainWindowVisible;
        private Avalonia.Controls.WindowState _previousWindowState;

        public AvaloniaUIService()
        {
        }

        public AvaloniaUIService(IDesktopTaskManager taskManager)
        {
            _taskManager = taskManager;
        }

        public void Configure(IDesktopTaskManager taskManager)
        {
            _taskManager = taskManager;
        }

        public async Task HideMainWindowAsync()
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var mainWindow = desktop.MainWindow;
                    if (mainWindow != null && mainWindow.IsVisible)
                    {
                        _wasMainWindowVisible = true;
                        _previousWindowState = mainWindow.WindowState;

                        // Minimize the window so it doesn't appear in screenshots
                        mainWindow.WindowState = Avalonia.Controls.WindowState.Minimized;
                        DebugHelper.WriteLine("AvaloniaUIService: Main window minimized before capture");
                    }
                    else
                    {
                        _wasMainWindowVisible = false;
                    }
                }
            });

            // Small delay to ensure window is fully minimized before capture starts
            await Task.Delay(150);
        }

        public async Task RestoreMainWindowAsync()
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_wasMainWindowVisible &&
                    Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var mainWindow = desktop.MainWindow;
                    if (mainWindow != null)
                    {
                        // Restore to previous state
                        mainWindow.WindowState = _previousWindowState;
                        DebugHelper.WriteLine("AvaloniaUIService: Main window restored after capture");
                    }
                }
                _wasMainWindowVisible = false;
            });
        }

        public async Task<SKBitmap?> ShowEditorAsync(SKBitmap image, string? sourceFilePath = null, bool taskMode = false)
        {
            ImageEditorSessionResult? result = await ShowEditorSessionAsync(image, sourceFilePath, taskMode);
            result?.SourceImage?.Dispose();
            return result?.RenderedImage;
        }

        public async Task<ImageEditorSessionResult?> ShowEditorSessionAsync(
            SKBitmap image,
            string? sourceFilePath = null,
            bool taskMode = false,
            IReadOnlyList<Annotation>? annotations = null,
            bool restoredAnnotations = false)
        {
            if (_taskManager == null)
            {
                throw new InvalidOperationException("AvaloniaUIService requires an IDesktopTaskManager before showing the editor.");
            }

            var tcs = new TaskCompletionSource<ImageEditorSessionResult?>();
            var restoredAnnotationSnapshot = annotations?.Select(annotation => annotation.Clone()).ToList();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Create independent Editor Window
                var editorWindow = new Views.EditorWindow();

                // Create independent ViewModel for this editor instance
                var editorOptions = ThemeService.CreateImageEditorOptions(showExitConfirmation: !taskMode);
                var editorViewModel = new MainViewModel(editorOptions);
                editorViewModel.ShowTaskModeButtons = taskMode;
                editorViewModel.TaskMode = taskMode;
                editorViewModel.ApplicationName = AppResources.AppName;

                // Wire up UploadRequested to trigger host app upload workflow
                MainViewModelHelper.WireUploadRequested(editorViewModel, _taskManager, () =>
                {
                    var editorView = editorWindow.FindControl<EditorView>("EditorViewControl");
                    return editorView?.GetSnapshot();
                });

                // Wire up CopyRequested to copy edited image (with annotations) to clipboard
                MainViewModelHelper.WireCopyRequested(editorViewModel, () =>
                {
                    var editorView = editorWindow.FindControl<EditorView>("EditorViewControl");
                    return editorView?.GetSnapshot();
                });

                // Wire up SaveRequested / SaveAsRequested for standalone editor window
                Func<SkiaSharp.SKBitmap?> getSnapshot = () =>
                    editorWindow.FindControl<EditorView>("EditorViewControl")?.GetSnapshot();
                MainViewModelHelper.WireSaveRequested(editorViewModel, getSnapshot, () => editorWindow);
                MainViewModelHelper.WireSaveAsRequested(editorViewModel, getSnapshot, () => editorWindow);
                MainViewModelHelper.WirePinRequested(editorViewModel, getSnapshot);

                // Set DataContext BEFORE initializing preview so bindings update correctly
                editorWindow.DataContext = editorViewModel;

                // Initialize the preview image
                editorViewModel.UpdatePreview(image);
                if (!string.IsNullOrWhiteSpace(sourceFilePath))
                {
                    editorViewModel.ImageFilePath = sourceFilePath;
                    editorViewModel.IsDirty = false;
                }

                if (restoredAnnotationSnapshot?.Count > 0)
                {
                    editorWindow.Opened += (_, _) =>
                    {
                        var editorView = editorWindow.FindControl<EditorView>("EditorViewControl");
                        editorView?.RestoreAnnotations(restoredAnnotationSnapshot, resetHistory: true);
                        editorViewModel.IsDirty = false;
                    };
                }

                // Handle window closing to capture result
                editorWindow.Closing += (s, e) =>
                {
                    if (!editorWindow.IsCloseRequestedByViewModel)
                    {
                        return;
                    }

                    try
                    {
                        var editorView = editorWindow.FindControl<EditorView>("EditorViewControl");

                        bool continueWithoutSave = editorViewModel.TaskResult == MainViewModel.EditorTaskResult.ContinueNoSave
                            || (editorWindow.IsCloseRequestedByViewModel &&
                                editorViewModel.TaskResult == MainViewModel.EditorTaskResult.Cancel);

                        if (taskMode && continueWithoutSave)
                        {
                            tcs.TrySetResult(null);
                        }
                        else if (editorView != null)
                        {
                            var snapshot = editorView.GetSnapshot();
                            if (snapshot == null)
                            {
                                tcs.TrySetResult(null);
                            }
                            else
                            {
                                var source = editorView.GetSource();
                                var annotationSnapshot = editorView.GetAnnotationSnapshot().ToList();
                                tcs.TrySetResult(new ImageEditorSessionResult(snapshot, source, annotationSnapshot));
                            }
                        }
                        else
                        {
                            tcs.TrySetResult(null);
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugHelper.WriteException(ex, "Failed to get editor snapshot");
                        tcs.TrySetResult(null);
                    }
                };

                // Show the window
                editorWindow.Show();
            });

            return await tcs.Task;
        }

        public async Task<string?> ShowVideoEditorAsync(string videoPath, string? ffmpegPath)
        {
            string detectedFfmpegPath = await Dispatcher.UIThread.InvokeAsync(PathsManager.GetFFmpegPath);

            return await Task.Run(async () =>
            {
                try
                {
                    Exception? startupFailure = null;
                    VideoEditorLaunchPolicy launchPolicy = VideoEditorLaunchPolicyResolver.GetCurrentPolicy();
                    if (!launchPolicy.AllowInteractiveLaunch)
                    {
                        await ShowVideoEditorStartupErrorAsync("The video editor is unavailable on this platform/session.");
                        return null;
                    }

                    var ffmpegResolution = VideoEditorFfmpegResolver.Resolve(ffmpegPath, detectedFfmpegPath);
                    LogVideoEditorFfmpegResolution(ffmpegPath, detectedFfmpegPath, ffmpegResolution);

                    string ffprobePath = string.Empty;
                    if (ffmpegResolution.IsAvailable)
                    {
                        try
                        {
                            ffprobePath = await VideoEditorFfprobeResolver.EnsureAvailableAsync(
                                ffmpegResolution.ConfiguredPath,
                                message => DebugHelper.WriteLine($"[VideoEditor] {message}"));

                            if (!string.IsNullOrWhiteSpace(ffprobePath))
                            {
                                DebugHelper.WriteLine($"[VideoEditor] Using FFprobe at: {ffprobePath}");
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugHelper.WriteException(ex, "Failed to resolve FFprobe for video editor");
                        }
                    }

                    var options = new VideoEditorOptions
                    {
                        VideoPath = videoPath,
                        FFmpegPath = ffmpegResolution.ConfiguredPath,
                        FFprobePath = ffprobePath,
                        Theme = ResolveTheme(),
                        EnableLinuxWaylandExplicitSyncMitigation = launchPolicy.EnableLinuxWaylandExplicitSyncMitigation
                    };

                    var events = new VideoEditorEvents
                    {
                        DiagnosticReported = diagnosticEvent =>
                        {
                            string message = $"[VideoEditor:{diagnosticEvent.Source}] {diagnosticEvent.Message}";

                            if (diagnosticEvent.Exception != null)
                            {
                                DebugHelper.WriteException(diagnosticEvent.Exception, message);
                            }
                            else
                            {
                                DebugHelper.WriteLine(message);
                            }

                            if (startupFailure == null &&
                                diagnosticEvent.Source == nameof(VideoEditorHost) &&
                                diagnosticEvent.Exception != null)
                            {
                                startupFailure = diagnosticEvent.Exception;
                            }
                        }
                    };

                    string? result = VideoEditorHost.ShowEditorDialog(options, events);

                    if (startupFailure != null)
                    {
                        await ShowVideoEditorStartupErrorAsync(startupFailure.Message);
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    DebugHelper.WriteException(ex, "Failed to open video editor");
                    await ShowVideoEditorStartupErrorAsync(ex.Message);
                    return null;
                }
            });
        }

        private static void LogVideoEditorFfmpegResolution(
            string? hostPath,
            string? detectedPath,
            (string ConfiguredPath, bool IsAvailable, string Source) resolution)
        {
            string hostCandidate = string.IsNullOrWhiteSpace(hostPath) ? "(empty)" : hostPath;
            string detectedCandidate = string.IsNullOrWhiteSpace(detectedPath) ? "(empty)" : detectedPath;
            string configuredPath = string.IsNullOrWhiteSpace(resolution.ConfiguredPath)
                ? "(not set)"
                : resolution.ConfiguredPath;

            if (resolution.IsAvailable)
            {
                DebugHelper.WriteLine(
                    $"[VideoEditor] Using FFmpeg at: {configuredPath} (source: {resolution.Source}, hostCandidate: {hostCandidate}, detectedCandidate: {detectedCandidate})");
            }
            else
            {
                DebugHelper.WriteLine(
                    $"[VideoEditor] FFmpeg unavailable. Source={resolution.Source}, hostCandidate={hostCandidate}, detectedCandidate={detectedCandidate}, configuredPath={configuredPath}");
            }
        }

        private static string ResolveTheme()
        {
            // Map the XerahS theme setting to the VideoEditorOptions theme string.
            return XerahS.Core.SettingsManager.Settings?.ThemeMode switch
            {
                XerahS.Core.AppThemeMode.Light  => "Light",
                XerahS.Core.AppThemeMode.System => "System",
                _                               => "Dark",
            };
        }

        private static async Task ShowVideoEditorStartupErrorAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var dialog = new Views.SurfaceWindow
                {
                    Title = "Video Editor Unavailable",
                    Width = 680,
                    Height = 280,
                    MinWidth = 560,
                    MinHeight = 220,
                    CanResize = true,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var closeButton = new Button
                {
                    Content = "Close",
                    MinWidth = 100,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                closeButton.Click += (_, _) => dialog.Close();

                dialog.Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 16,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "The video editor could not start.",
                            FontWeight = FontWeight.SemiBold
                        },
                        new ScrollViewer
                        {
                            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                            Content = new TextBlock
                            {
                                Text = message,
                                TextWrapping = TextWrapping.Wrap
                            }
                        },
                        closeButton
                    }
                };

                Window? owner = TryGetDialogOwner();

                if (CanUseDialogOwner(owner))
                {
                    _ = dialog.ShowDialog(owner!);
                }
                else
                {
                    dialog.Show();
                }
            });
        }

        private static Window? TryGetDialogOwner()
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }

            return null;
        }

        private static bool CanUseDialogOwner(Window? owner) =>
            owner != null &&
            owner.IsVisible &&
            owner.WindowState != Avalonia.Controls.WindowState.Minimized &&
            owner.ShowInTaskbar;

        public async Task<(AfterCaptureTasks Capture, AfterUploadTasks Upload, bool Cancel)> ShowAfterCaptureWindowAsync(
            SKBitmap image,
            AfterCaptureTasks afterCapture,
            AfterUploadTasks afterUpload)
        {
            return await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var viewModel = new AfterCaptureViewModel(image, afterCapture, afterUpload);
                var window = new Views.AfterCaptureWindow
                {
                    DataContext = viewModel
                };

                viewModel.RequestClose += () => window.Close();

                Window? owner = null;
                if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    owner = desktop.MainWindow;
                }

                bool canUseOwner = owner != null && owner.IsVisible &&
                                   owner.WindowState != Avalonia.Controls.WindowState.Minimized &&
                                   owner.ShowInTaskbar;

                if (canUseOwner)
                {
                    await window.ShowDialog(owner!);
                }
                else
                {
                    var closedTcs = new TaskCompletionSource<bool>();
                    window.Closed += (_, _) => closedTcs.TrySetResult(true);
                    window.Show();
                    await closedTcs.Task;
                }

                return (viewModel.AfterCaptureTasks, viewModel.AfterUploadTasks, viewModel.Cancelled);
            });
        }

        public async Task ShowAfterUploadWindowAsync(AfterUploadWindowInfo info)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var viewModel = new AfterUploadViewModel(info);
                var window = new Views.AfterUploadWindow
                {
                    DataContext = viewModel
                };

                viewModel.RequestClose += () => window.Close();
                window.Closed += (_, _) => viewModel.Dispose();

                Window? owner = null;
                if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    owner = desktop.MainWindow;
                }

                bool canUseOwner = owner != null && owner.IsVisible &&
                                   owner.WindowState != Avalonia.Controls.WindowState.Minimized &&
                                   owner.ShowInTaskbar;

                if (canUseOwner)
                {
                    window.Show(owner!);
                }
                else
                {
                    window.Show();
                }
            });
        }

        public async Task<SendToPromptResult> ShowSendToPromptAsync(SendToSelection selection)
        {
            return await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var viewModel = new SendToPromptViewModel(selection);
                var window = new Views.SendToPromptWindow
                {
                    DataContext = viewModel
                };

                Window? owner = TryGetDialogOwner();
                if (CanUseDialogOwner(owner))
                {
                    await window.ShowDialog(owner!);
                }
                else
                {
                    var closedTcs = new TaskCompletionSource<bool>();
                    window.Closed += (_, _) => closedTcs.TrySetResult(true);
                    window.Show();
                    await closedTcs.Task;
                }

                return new SendToPromptResult
                {
                    Action = viewModel.SelectedAction
                };
            });
        }

        public async Task ExecuteSendToActionAsync(SendToAction action, SendToSelection selection)
        {
            if (action is SendToAction.Cancel or SendToAction.UploadNow)
            {
                return;
            }

            Window? owner = TryGetDialogOwner();

            switch (action)
            {
                case SendToAction.OpenUploadContent:
                    await UploadContentToolService.ShowSelectionAsync(selection.FilePaths, selection.FolderPaths, owner);
                    break;

                case SendToAction.OpenImageEditor:
                    await OpenSelectedImagesInEditorAsync(selection);
                    break;

                case SendToAction.PinToScreen:
                    await PinSelectedImagesAsync(selection);
                    break;

                case SendToAction.IndexFolders:
                    await OpenSelectedFoldersInIndexFolderAsync(selection, owner);
                    break;
            }
        }

        private async Task OpenSelectedImagesInEditorAsync(SendToSelection selection)
        {
            if (!selection.CanOpenImageEditor)
            {
                return;
            }

            foreach (var filePath in selection.FilePaths ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    continue;
                }

                using var bitmap = SkiaSharp.SKBitmap.Decode(filePath);
                if (bitmap == null)
                {
                    continue;
                }

                await ShowEditorAsync(bitmap, sourceFilePath: filePath);
            }
        }

        private static Task PinSelectedImagesAsync(SendToSelection selection)
        {
            if (!selection.CanPinToScreen)
            {
                return Task.CompletedTask;
            }

            return PinToScreenToolService.PinFilesAsync(selection.FilePaths);
        }

        private async Task OpenSelectedFoldersInIndexFolderAsync(SendToSelection selection, Window? owner)
        {
            if (!selection.CanIndexFolders)
            {
                return;
            }

            foreach (var folderPath in selection.FolderPaths ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                {
                    continue;
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var viewModel = UiViewModelFactoryAccessor.GetRequired().CreateIndexFolderViewModel();
                    viewModel.FolderPath = folderPath;

                    var window = new Views.IndexFolderView
                    {
                        DataContext = viewModel
                    };

                    if (CanUseDialogOwner(owner))
                    {
                        window.Show(owner!);
                    }
                    else
                    {
                        window.Show();
                    }

                    if (viewModel.CanStartIndexing)
                    {
                        _ = viewModel.IndexFolderCommand.ExecuteAsync(null);
                    }
                });
            }
        }

        public async Task ShowOcrWindowAsync(SKBitmap image)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var viewModel = new OcrViewModel(image);

                // Wire the SelectRegion callback so users can re-capture inside the OCR window
                viewModel.SelectRegionRequested = async () =>
                {
                    try
                    {
                        await Task.Delay(300); // Allow window to minimize
                        var captureSettings = SettingsManager.DefaultTaskSettings?.CaptureSettings
                            ?? new TaskSettingsCapture();
                        var captureOptions = new CaptureOptions
                        {
                            UseModernCapture = captureSettings.UseModernCapture,
                            LinuxRegionSelectorPreference = captureSettings.LinuxRegionSelectorPreference,
                            ShowCursor = captureSettings.ShowCursor,
                            CaptureTransparent = captureSettings.CaptureTransparent,
                            CaptureShadow = captureSettings.CaptureShadow,
                            CaptureClientArea = captureSettings.CaptureClientArea
                        };
                        return await PlatformServices.ScreenCapture.CaptureRegionAsync(captureOptions);
                    }
                    catch (Exception ex)
                    {
                        DebugHelper.WriteException(ex, "OCR region capture");
                        return null;
                    }
                };

                var window = new Views.OcrWindow
                {
                    DataContext = viewModel
                };

                Window? owner = null;
                if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    owner = desktop.MainWindow;
                }

                bool canUseOwner = owner != null && owner.IsVisible &&
                                   owner.WindowState != Avalonia.Controls.WindowState.Minimized &&
                                   owner.ShowInTaskbar;

                if (canUseOwner)
                {
                    window.Show(owner!);
                }
                else
                {
                    window.Show();
                }
            });
        }

        public async Task ShowAnalyzerWindowAsync(SKBitmap image)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var viewModel = new ImageAnalyzerViewModel();
                viewModel.SetInputImage(image);

                var window = new Views.ImageAnalyzerWindow();
                window.Initialize(viewModel);

                Window? owner = null;
                if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    owner = desktop.MainWindow;
                }

                bool canUseOwner = owner != null && owner.IsVisible &&
                                   owner.WindowState != Avalonia.Controls.WindowState.Minimized &&
                                   owner.ShowInTaskbar;

                if (canUseOwner)
                {
                    window.Show(owner!);
                }
                else
                {
                    window.Show();
                }
            });
        }
    }
}
