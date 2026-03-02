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
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using SkiaSharp;
using XerahS.Core;
using XerahS.UI.ViewModels;
using XerahS.Core.Hotkeys;
using Avalonia; // For Application.Current
using XerahS.Core.Tasks;
using XerahS.Core.Managers;
using ShareX.ImageEditor.Annotations;
using ShareX.ImageEditor.ViewModels;
using ShareX.ImageEditor.Views;
using XerahS.UI.Helpers;
using XerahS.UI.Views.Dialogs;

namespace XerahS.UI.Views
{
    public partial class MainWindow : Window
    {
        private EditorView? _editorView = null;
        private bool _isOpenImageInProgress;

        /// <summary>
        /// Collection of user-configured workflows for menu binding.
        /// </summary>
        public ObservableCollection<WorkflowSettings> UserWorkflows { get; } = new ObservableCollection<WorkflowSettings>();

        public MainWindow()
        {
            InitializeComponent();
            KeyDown += OnKeyDown;

            // Set initial theme and subscribe to changes
            RequestedThemeVariant = ShareX.ImageEditor.Helpers.ThemeManager.GetCurrentTheme();
            ShareX.ImageEditor.Helpers.ThemeManager.ThemeChanged += (s, theme) => RequestedThemeVariant = theme;

            // Initial Navigation
            var navView = this.FindControl<NavigationView>("NavView");
            if (navView != null)
            {
                // Force selection of first item
                if (navView.MenuItems[0] is NavigationViewItem item)
                {
                    navView.SelectedItem = item;
                    OnNavSelectionChanged(navView, new NavigationViewSelectionChangedEventArgs());
                }
            }

            LoadUserWorkflows();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
        }

        private void OnExitClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }

        private async void OnOpenImageClick(object? sender, RoutedEventArgs e)
        {
            await OpenImageFromFileAsync();
        }

        private async Task OpenImageFromFileAsync()
        {
            if (_isOpenImageInProgress)
            {
                return;
            }

            _isOpenImageInProgress = true;

            try
            {
                if (DataContext is not MainViewModel vm)
                {
                    return;
                }

                string? path = await PickImagePathAsync();
                if (string.IsNullOrWhiteSpace(path))
                {
                    return;
                }

                if (vm.PreviewImage == null)
                {
                    ReplaceImageFromPath(vm, path);
                    return;
                }

                OpenImageChoice choice = await ShowOpenImageChoiceDialogAsync();
                switch (choice)
                {
                    case OpenImageChoice.ReplaceImage:
                        ReplaceImageFromPath(vm, path);
                        break;
                    case OpenImageChoice.AddAsShape:
                        await AddImageAsShapeFromPathAsync(path);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                XerahS.Common.DebugHelper.WriteException(ex, "File > Open failed");
            }
            finally
            {
                _isOpenImageInProgress = false;
            }
        }

        private async Task<string?> PickImagePathAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null)
            {
                return null;
            }

            var options = new FilePickerOpenOptions
            {
                Title = "Open Image",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Image Files")
                    {
                        Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.webp", "*.tiff", "*.tif" }
                    },
                    FilePickerFileTypes.All
                }
            };

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
            if (files.Count < 1)
            {
                return null;
            }

            string? path = files[0].TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            return path;
        }

        private async Task<OpenImageChoice> ShowOpenImageChoiceDialogAsync()
        {
            try
            {
                var dialog = new OpenImageChoiceDialog();
                return await dialog.ShowDialog<OpenImageChoice>(this);
            }
            catch (Exception ex)
            {
                XerahS.Common.DebugHelper.WriteException(ex, "Failed to show open image choice dialog");
                return OpenImageChoice.Cancel;
            }
        }

        private void ReplaceImageFromPath(MainViewModel vm, string path)
        {
            SKBitmap? bitmap = null;

            try
            {
                bitmap = SKBitmap.Decode(path);
                if (bitmap == null || bitmap.Handle == IntPtr.Zero)
                {
                    bitmap?.Dispose();
                    return;
                }

                NavigateToEditor();
                vm.ClearCommand.Execute(null);

                // Ownership of bitmap is transferred to ViewModel.
                vm.UpdatePreview(bitmap, clearAnnotations: true);
                bitmap = null;
            }
            catch (Exception ex)
            {
                XerahS.Common.DebugHelper.WriteException(ex, "Failed to load selected image");
                bitmap?.Dispose();
            }
        }

        private async Task AddImageAsShapeFromPathAsync(string path)
        {
            try
            {
                NavigateToEditor();

                if (_editorView == null)
                {
                    return;
                }

                // XIP0039 Guardrail 6: Call the now-public InsertImageAnnotation directly
                // instead of using reflection (BindingFlags.NonPublic).
                var bitmap = SKBitmap.Decode(path);
                if (bitmap == null || bitmap.Handle == IntPtr.Zero)
                {
                    bitmap?.Dispose();
                    return;
                }

                try
                {
                    _editorView.InsertImageAnnotation(bitmap, dropPosition: null);
                    bitmap = null; // Ownership transferred to inserted image annotation.
                }
                finally
                {
                    bitmap?.Dispose();
                }
            }
            catch (Exception ex)
            {
                XerahS.Common.DebugHelper.WriteException(ex, "Failed to add selected image as annotation");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Loads user-configured workflows from SettingsManager into UserWorkflows collection.
        /// </summary>
        private void LoadUserWorkflows()
        {
            UserWorkflows.Clear();
            var workflows = SettingsManager.WorkflowsConfig?.Hotkeys;
            if (workflows != null)
            {
                foreach (var workflow in workflows)
                {
                    if (workflow.Job != WorkflowType.None)
                    {
                        UserWorkflows.Add(workflow);
                    }
                }
            }

            UpdateWorkflowMenuItems();
        }

        private void OnWorkflowMenuItemClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is WorkflowSettings workflow)
            {
                _ = ExecuteCaptureAsync(workflow.Job, workflow.Id);
            }
        }

        private void UpdateWorkflowMenuItems()
        {
            var runWorkflowsMenuItem = this.FindControl<MenuItem>("RunWorkflowsMenuItem");
            if (runWorkflowsMenuItem == null)
            {
                return;
            }

            var workflowMenuItems = new List<MenuItem>();

            foreach (var workflow in UserWorkflows)
            {
                var workflowMenuItem = new MenuItem
                {
                    Header = GetWorkflowDisplayName(workflow),
                    DataContext = workflow
                };
                workflowMenuItem.Click += OnWorkflowMenuItemClick;
                workflowMenuItems.Add(workflowMenuItem);
            }

            if (workflowMenuItems.Count == 0)
            {
                workflowMenuItems.Add(new MenuItem
                {
                    Header = "No workflows configured",
                    IsEnabled = false
                });
            }

            runWorkflowsMenuItem.ItemsSource = workflowMenuItems;
        }

        private static string GetWorkflowDisplayName(WorkflowSettings workflow)
        {
            if (!string.IsNullOrWhiteSpace(workflow.TaskSettings?.Description))
            {
                return workflow.TaskSettings.Description;
            }

            return XerahS.Common.EnumExtensions.GetDescription(workflow.Job);
        }

        private void OnWindowOpened(object? sender, EventArgs e)
        {
            // Provide the native window handle to platform services so the Wayland GlobalShortcuts
            // portal can display a transient permissions dialog (GNOME returns response=2 without it).
            // On X11/XWayland the descriptor is "XID"; on native Wayland it is "wl_surface"
            // (xdg-foreign export not yet implemented, so that path still passes empty string).
            var platformHandle = TryGetPlatformHandle();
            XerahS.Common.DebugHelper.WriteLine(
                $"MainWindow: OnWindowOpened — platform handle descriptor={platformHandle?.HandleDescriptor ?? "<null>"}, handle={platformHandle?.Handle}");

            if (platformHandle != null)
            {
                XerahS.Platform.Abstractions.PlatformServices.NativeWindowHandleProvider = () =>
                    platformHandle.HandleDescriptor == "XID"
                        ? $"x11:{platformHandle.Handle:x}"
                        : null;
            }

            // Notify the hotkey service that the window is ready and the native window handle is
            // now available via NativeWindowHandleProvider. If the portal BindShortcuts call at
            // startup ran before this point (e.g. the 100ms debounce fired while the window was
            // still initialising — in debug builds startup can take 40+ seconds) and received
            // parentWindow="" which caused a response=2 failure, this triggers a portal retry so
            // hotkeys work globally without needing an app restart.
            try
            {
                XerahS.Platform.Abstractions.PlatformServices.Hotkey.NotifyWindowReady();
            }
            catch (Exception ex)
            {
                XerahS.Common.DebugHelper.WriteException(ex, "MainWindow: NotifyWindowReady failed");
            }

            // Only maximize if we are NOT in silent run mode
            if (!SettingsManager.Settings.SilentRun)
            {
                // Maximize window and center it on screen
                this.WindowState = Avalonia.Controls.WindowState.Maximized;
            }

            // Update navigation items after settings are loaded
            var navView = this.FindControl<NavigationView>("NavView");
            if (navView != null)
            {
                UpdateNavigationItems(navView);
            }

            LoadUserWorkflows();

            if (Application.Current is App app && app.WorkflowManager != null)
            {
                app.WorkflowManager.WorkflowsChanged += (s, args) =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        LoadUserWorkflows();

                        if (navView != null)
                        {
                            UpdateNavigationItems(navView);
                        }
                    });
                };
            }
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            // If SilentRun ("Start minimized to tray") is enabled and we are not explicitly
            // exiting via Tray → Exit, hide the window to tray instead of closing the app.
            // This works on all platforms (Windows, Linux, macOS); no OS-specific logic.
            bool silentRun = SettingsManager.Settings.SilentRun;

            if (silentRun && !App.IsExiting)
            {
                e.Cancel = true;
                // Ensure tray icon is visible so user can restore or exit (handles edge case
                // where config had SilentRun true but ShowTray false, e.g. from another machine).
                if (!SettingsManager.Settings.ShowTray)
                {
                    SettingsManager.Settings.ShowTray = true;
                    TrayIconHelper.Instance.RefreshFromSettings();
                }
                this.Hide();
                this.ShowInTaskbar = false;
                return;
            }

            base.OnClosing(e);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async Task ExecuteCaptureAsync(WorkflowType jobType, string? workflowId = null, AfterCaptureTasks afterCapture = AfterCaptureTasks.SaveImageToFile, SkiaSharp.SKBitmap? image = null)
        {
            TaskSettings settings;

            // Find an existing workflow - prefer by ID if provided, otherwise by job type
            WorkflowSettings? workflow = null;

            if (!string.IsNullOrEmpty(workflowId))
            {
                // Try to find by ID first
                if (Application.Current is App app && app.WorkflowManager != null)
                {
                    workflow = app.WorkflowManager.GetWorkflowById(workflowId);
                }

                if (workflow == null)
                {
                    workflow = SettingsManager.WorkflowsConfig.Hotkeys.FirstOrDefault(x => x.Id == workflowId);
                }
            }

            // Fallback to job type if no ID provided or not found
            if (workflow == null)
            {
                workflow = SettingsManager.WorkflowsConfig.Hotkeys.FirstOrDefault(x => x.Job == jobType);
            }

            if (workflow != null && workflow.TaskSettings != null)
            {
                // Clone workflow settings to avoid modifying the original instance during execution
                var jsonSettings = new Newtonsoft.Json.JsonSerializerSettings
                {
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                    ObjectCreationHandling = Newtonsoft.Json.ObjectCreationHandling.Replace
                };
                var effectCount = workflow.TaskSettings?.ImageSettings?.ImageEffectsPreset?.Effects?.Count ?? 0;
                var presetName = workflow.TaskSettings?.ImageSettings?.ImageEffectsPreset?.Name ?? "(null)";
                Console.WriteLine($"[MainWindow] Clone workflow settings. Preset='{presetName}', Effects={effectCount}");
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(workflow.TaskSettings, jsonSettings);
                settings = Newtonsoft.Json.JsonConvert.DeserializeObject<TaskSettings>(json, jsonSettings)!;

                // Store the workflow ID in the task settings for troubleshooting
                settings.WorkflowId = workflow.Id;

                // Note: We deliberately ignore the 'afterCapture' parameter if a workflow is found,
                // as the workflow's configured tasks should take precedence.
                // We only use 'afterCapture' as a fallback when creating a temporary task setting.
            }
            else
            {
                // No workflow found, create brand new default settings (no globals)
                settings = new TaskSettings();
                settings.Job = jobType;
                // Apply the requested after capture actions since we have no user pref
                settings.AfterCaptureJob = afterCapture;
            }

            // Ensure Job is correct (if workflow had different job, we technically picked it by job, but safe to set)
            settings.Job = jobType;

            // Subscribe to task completion to update Editor preview
            void HandleTaskCompleted(object? s, WorkerTask task)
            {
                TaskManager.Instance.TaskCompleted -= HandleTaskCompleted;

                if (task.Info?.Metadata?.Image != null && DataContext is MainViewModel vm)
                {
                    vm.UpdatePreview(task.Info.Metadata.Image);
                }
            }

            TaskManager.Instance.TaskCompleted += HandleTaskCompleted;

            // Hide main window before capture to avoid capturing the app itself
            // This only applies to navbar-triggered captures, not hotkeys
            try
            {
                await Platform.Abstractions.PlatformServices.UI.HideMainWindowAsync();
            }
            catch
            {
                // Ignore errors - window hiding is not critical
            }

            try
            {
                await TaskManager.Instance.StartTask(settings, image);
            }
            finally
            {
                // Restore main window after capture
                try
                {
                    await Platform.Abstractions.PlatformServices.UI.RestoreMainWindowAsync();
                }
                catch
                {
                    // Ignore errors
                }
            }
        }

        private static Task ExecuteWorkflowFromNavigationAsync(WorkflowType jobType)
        {
            var workflow = SettingsManager.GetFirstWorkflow(jobType);

            // Upload Content nav fallback:
            // if no workflow is configured for ClipboardUploadWithContentViewer,
            // use FileUpload workflow when available.
            if (workflow == null && jobType == WorkflowType.ClipboardUploadWithContentViewer)
            {
                workflow = SettingsManager.GetFirstWorkflow(WorkflowType.FileUpload);
            }

            if (workflow != null)
            {
                return XerahS.Core.Helpers.TaskHelpers.ExecuteWorkflow(workflow, workflow.Id);
            }

            return XerahS.Core.Helpers.TaskHelpers.ExecuteJob(jobType, new TaskSettings { Job = jobType });
        }

    }
}

