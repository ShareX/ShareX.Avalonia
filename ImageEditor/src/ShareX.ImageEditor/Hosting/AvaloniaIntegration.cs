#region License Information (GPL v3)

/*
    ShareX.ImageEditor - The UI-agnostic Editor library for ShareX
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
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using ShareX.ImageEditor.Hosting.Diagnostics;
using ShareX.ImageEditor.Presentation.ViewModels;
using ShareX.ImageEditor.Presentation.Views;

namespace ShareX.ImageEditor.Hosting
{
    public class AvaloniaApp : Application
    {
        public override void Initialize()
        {
            Styles.Add(new FluentTheme());
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // No main window here, we manage windows manually
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }

    public class EditorEvents
    {
        public Action<byte[]>? CopyImageRequested { get; set; }
        public Func<byte[], string?, string?>? SaveImageRequested { get; set; }
        public Func<byte[], string?, string?>? SaveImageAsRequested { get; set; }
        public Action<byte[]>? PinImageRequested { get; set; }
        public Action<byte[]>? UploadImageRequested { get; set; }
        public Action<EditorDiagnosticEvent>? DiagnosticReported { get; set; }
        public string? ImageFilePath { get; set; }
    }

    public static class AvaloniaIntegration
    {
        private static bool initialized = false;

        private static readonly object _initLock = new object();

        public static void Initialize()
        {
            if (!initialized)
            {
                lock (_initLock)
                {
                    if (!initialized)
                    {
                        if (Application.Current == null)
                        {
                            AppBuilder.Configure<AvaloniaApp>()
                                .UsePlatformDetect()
                                .WithInterFont()
                                .LogToTrace()
                                .SetupWithoutStarting();
                        }

                        initialized = true;
                    }
                }
            }
        }

        public static void ShowEditor(string filePath)
        {
            Initialize();
            EditorWindow window = new EditorWindow();

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                window.LoadImage(filePath);
            }

            window.Show();
        }

        public static void ShowEditor(Stream imageStream)
        {
            Initialize();
            EditorWindow window = new EditorWindow();

            if (imageStream != null)
            {
                window.LoadImage(imageStream);
            }

            window.Show();
        }

        public static byte[]? ShowEditorDialog(Stream imageStream, ImageEditorOptions options, EditorEvents? events = null, bool taskMode = false, string? imageFilePath = null)
        {
            byte[]? result = null;
            IEditorDiagnosticsSink? previousDiagnosticsSink = null;
            bool restoreScopedDiagnostics = false;

            if (events?.DiagnosticReported != null)
            {
                var diagnosticHandler = events.DiagnosticReported;
                previousDiagnosticsSink = EditorServices.Diagnostics;
                restoreScopedDiagnostics = true;

                EditorServices.Diagnostics = new DelegateEditorDiagnosticsSink(diagnosticEvent =>
                {
                    try
                    {
                        diagnosticHandler(diagnosticEvent);
                    }
                    catch
                    {
                        // Host diagnostics callback failures must not break the editor.
                    }

                    if (previousDiagnosticsSink != null)
                    {
                        try
                        {
                            previousDiagnosticsSink.Report(diagnosticEvent);
                        }
                        catch
                        {
                            // Ignore downstream sink failures.
                        }
                    }
                });
            }

            Initialize();
            EditorWindow window = new EditorWindow(options);

            if (imageStream != null)
            {
                window.LoadImage(imageStream);
            }

            // Set file path from events or parameter
            string? filePath = imageFilePath ?? events?.ImageFilePath;
            if (!string.IsNullOrEmpty(filePath) && window.DataContext is MainViewModel vmForPath)
            {
                vmForPath.ImageFilePath = filePath;
            }

            if (taskMode && window.DataContext is MainViewModel vm)
            {
                vm.TaskMode = true;
            }

            SetupEvents(window, events, () =>
            {
                if (window.DataContext is MainViewModel vm)
                {
                    if (taskMode)
                    {
                        switch (vm.TaskResult)
                        {
                            case MainViewModel.EditorTaskResult.Continue:
                                result = window.GetResultBytes();
                                break;
                            case MainViewModel.EditorTaskResult.ContinueNoSave:
                                result = window.GetSourceBytes();
                                break;
                        }
                    }
                }
            });

            window.Show();

            DispatcherFrame frame = new DispatcherFrame();
            window.Closed += (s, e) =>
            {
                frame.Continue = false;

                if (restoreScopedDiagnostics)
                {
                    EditorServices.Diagnostics = previousDiagnosticsSink;
                }
            };
            Dispatcher.UIThread.PushFrame(frame);

            return result;
        }

        private static void SetupEvents(EditorWindow window, EditorEvents? events, Action onResult)
        {
            if (events == null) return;

            MainViewModel? vm = window.DataContext as MainViewModel;
            if (vm == null) return;

            if (events.CopyImageRequested != null)
            {
                vm.CopyRequested += () =>
                {
                    byte[]? bytes = window.GetResultBytes();
                    if (bytes != null)
                    {
                        InvokeHostCallback(bytes, events.CopyImageRequested, nameof(EditorEvents.CopyImageRequested));
                    }
                };
            }

            if (events.SaveImageRequested != null)
            {
                vm.SaveRequested += () =>
                {
                    byte[]? bytes = window.GetResultBytes();
                    if (bytes != null)
                    {
                        string? savedPath = InvokeHostSaveCallback(bytes, vm.ImageFilePath, events.SaveImageRequested, nameof(EditorEvents.SaveImageRequested));
                        if (!string.IsNullOrEmpty(savedPath))
                        {
                            vm.ImageFilePath = savedPath;
                        }
                    }
                };
            }

            if (events.SaveImageAsRequested != null)
            {
                vm.SaveAsRequested += () =>
                {
                    byte[]? bytes = window.GetResultBytes();
                    if (bytes != null)
                    {
                        string? savedPath = InvokeHostSaveCallback(bytes, vm.ImageFilePath, events.SaveImageAsRequested, nameof(EditorEvents.SaveImageAsRequested));
                        if (!string.IsNullOrEmpty(savedPath))
                        {
                            vm.ImageFilePath = savedPath;
                        }
                    }
                };
            }

            if (events.PinImageRequested != null)
            {
                vm.PinRequested += () =>
                {
                    byte[]? bytes = window.GetResultBytes();
                    if (bytes != null)
                    {
                        InvokeHostCallback(bytes, events.PinImageRequested, nameof(EditorEvents.PinImageRequested));
                    }
                };
            }

            if (events.UploadImageRequested != null)
            {
                vm.UploadRequested += () =>
                {
                    byte[]? bytes = window.GetResultBytes();
                    if (bytes != null)
                    {
                        InvokeHostCallback(bytes, events.UploadImageRequested, nameof(EditorEvents.UploadImageRequested));
                    }
                };
            }

            window.Closed += (s, e) =>
            {
                try
                {
                    onResult();
                }
                catch (Exception ex)
                {
                    EditorServices.ReportError(nameof(AvaloniaIntegration), "Failed to process editor dialog result.", ex);
                }
            };
        }

        private static void InvokeHostCallback(byte[] bytes, Action<byte[]> callback, string callbackName)
        {
            try
            {
                callback(bytes);
            }
            catch (Exception ex)
            {
                EditorServices.ReportError(nameof(AvaloniaIntegration), $"Host callback '{callbackName}' failed.", ex);
            }
        }

        private static string? InvokeHostSaveCallback(byte[] bytes, string? filePath, Func<byte[], string?, string?> callback, string callbackName)
        {
            try
            {
                return callback(bytes, filePath);
            }
            catch (Exception ex)
            {
                EditorServices.ReportError(nameof(AvaloniaIntegration), $"Host callback '{callbackName}' failed.", ex);
                return null;
            }
        }
    }
}
