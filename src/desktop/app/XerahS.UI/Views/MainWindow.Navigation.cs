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
using Avalonia.Interactivity;
using System;
using System.Collections;
using System.Linq;
using FluentAvalonia.UI.Controls;
using ShareX.ImageEditor.Presentation.Views;
using XerahS.Core;
using XerahS.Core.Hotkeys;
using XerahS.Core.Managers;
using XerahS.UI.Helpers;

namespace XerahS.UI.Views
{
    public partial class MainWindow
    {
        private void OnMenuNavigateClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem)
            {
                return;
            }

            var navTag = menuItem.Tag?.ToString();
            if (string.IsNullOrWhiteSpace(navTag))
            {
                return;
            }

            NavigateTo(navTag);
        }

        private void OnNavSelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
        {
            var navView = sender as NavigationView;
            var contentFrame = this.FindControl<ContentControl>("ContentFrame");
            var selectedItem = navView?.SelectedItem as NavigationViewItem;

            if (contentFrame == null || selectedItem == null)
            {
                return;
            }

            // Skip items handled by OnNavItemInvoked (action items with SelectsOnInvoked=False
            // may still raise SelectionChanged in some FluentAvalonia builds — guard here).
            var tag = selectedItem.Tag?.ToString();
            if (IsActionOnlyNavTag(tag))
            {
                return;
            }

            HandleNavigationTag(tag, contentFrame);
        }

        /// <summary>
        /// Fires on every click/tap of a nav item, even when the item is already selected.
        /// Used for action-only items (Upload, Capture workflows) so that re-clicking them
        /// always re-triggers their associated action (fixes issue #170).
        /// </summary>
        private void OnNavItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
        {
            var invokedItem = e.InvokedItemContainer as NavigationViewItem;
            var tag = invokedItem?.Tag?.ToString();

            if (!IsActionOnlyNavTag(tag))
            {
                return;
            }

            var contentFrame = this.FindControl<ContentControl>("ContentFrame");
            if (contentFrame != null)
            {
                HandleNavigationTag(tag, contentFrame);
            }
        }

        /// <summary>
        /// Returns true for nav tags that map to immediate actions (dialogs, tool windows, workflows)
        /// rather than page navigation. These items use ItemInvoked instead of SelectionChanged.
        /// </summary>
        private static bool IsActionOnlyNavTag(string? tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return false;
            }

            // "Tools" (no underscore) navigates to ToolsView page; "Tools_*" sub-items open dialogs/windows.
            return tag.StartsWith("Capture_", StringComparison.Ordinal)
                || tag.StartsWith("Workflow_", StringComparison.Ordinal)
                || tag.StartsWith("Tools_", StringComparison.Ordinal)
                || tag == "Upload_FileUpload"
                || tag == "Upload_ClipboardUploadWithContentViewer";
        }

        private bool HandleNavigationTag(string? tag, ContentControl contentFrame)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return false;
            }

            // Handle workflow execution by ID
            if (tag.StartsWith("Capture_", StringComparison.Ordinal))
            {
                var workflowId = tag.Replace("Capture_", "", StringComparison.Ordinal);
                if (!string.IsNullOrEmpty(workflowId))
                {
                    WorkflowSettings? workflow = null;

                    // Try to get workflow from WorkflowManager first
                    if (Application.Current is App app && app.WorkflowManager != null)
                    {
                        workflow = app.WorkflowManager.GetWorkflowById(workflowId);
                    }

                    // Fallback to SettingManager
                    if (workflow == null)
                    {
                        workflow = SettingsManager.WorkflowsConfig.Hotkeys.FirstOrDefault(w => w.Id == workflowId);
                    }

                    if (workflow != null)
                    {
                        _ = ExecuteCaptureAsync(workflow.Job, workflow.Id);
                        NavigateToEditor();
                        return true;
                    }
                }

                return false;
            }

            // Handle workflow execution by ID from menu
            if (tag.StartsWith("Workflow_", StringComparison.Ordinal))
            {
                var workflowId = tag.Replace("Workflow_", "", StringComparison.Ordinal);
                if (!string.IsNullOrEmpty(workflowId))
                {
                    var workflow = SettingsManager.WorkflowsConfig?.Hotkeys?.FirstOrDefault(w => w.Id == workflowId);
                    if (workflow != null)
                    {
                        _ = ExecuteCaptureAsync(workflow.Job, workflow.Id);
                        return true;
                    }
                }

                return false;
            }

            if (ToolNavigationHelper.TryHandleToolsTag(tag, this, contentFrame, ExecuteWorkflowFromNavigationAsync))
            {
                return true;
            }

            switch (tag)
            {
                case "Editor":
                    _editorView ??= new EditorView();
                    contentFrame.Content = _editorView;
                    return true;
                case "Recording":
                    contentFrame.Content = new RecordingView();
                    return true;
                case "History":
                    contentFrame.Content = new HistoryView();
                    return true;
                case "Workflows":
                    contentFrame.Content = new WorkflowsView();
                    return true;
                case "Upload_ClipboardUploadWithContentViewer":
                    _ = ExecuteWorkflowFromNavigationAsync(WorkflowType.ClipboardUploadWithContentViewer);
                    return true;
                case "Upload_FileUpload":
                    _ = ExecuteWorkflowFromNavigationAsync(WorkflowType.FileUpload);
                    return true;
                case "Settings":
                    contentFrame.Content = new SettingsView();
                    return true;
                case "Settings_App":
                    contentFrame.Content = new ApplicationSettingsView();
                    return true;
                case "Settings_Dest":
                    contentFrame.Content = new DestinationSettingsView();
                    return true;
                case "Debug":
                    contentFrame.Content = new DebugView();
                    return true;
                case "About":
                    contentFrame.Content = new AboutView();
                    return true;
                default:
                    return false;
            }
        }

        public void NavigateToEditor()
        {
            NavigateTo("Editor");
        }

        public void NavigateToSettings()
        {
            NavigateTo("Settings");
        }

        public void NavigateToHistory()
        {
            NavigateTo("History");
        }

        public void NavigateToAbout()
        {
            NavigateTo("About");
        }

        private void NavigateTo(string navTag)
        {
            bool handled = false;
            var contentFrame = this.FindControl<ContentControl>("ContentFrame");
            var navView = this.FindControl<NavigationView>("NavView");
            if (navView != null)
            {
                var navItem = FindNavigationItemByTag(navView.MenuItems, navTag);
                if (navItem != null)
                {
                    if (!ReferenceEquals(navView.SelectedItem, navItem))
                    {
                        navView.SelectedItem = navItem;
                        handled = true;
                    }
                    else if (contentFrame != null)
                    {
                        handled = HandleNavigationTag(navTag, contentFrame);
                    }
                }
            }

            // Menu-bar actions may not have a corresponding NavigationView item.
            if (!handled && contentFrame != null)
            {
                _ = HandleNavigationTag(navTag, contentFrame);
            }

            // Ensure window is visible and active
            if (!this.IsVisible)
            {
                this.Show();
            }

            if (this.WindowState == Avalonia.Controls.WindowState.Minimized)
            {
                this.WindowState = Avalonia.Controls.WindowState.Normal;
            }

            this.Activate();
            this.Focus();
        }

        private static NavigationViewItem? FindNavigationItemByTag(IEnumerable? menuItems, string navTag)
        {
            if (menuItems == null)
            {
                return null;
            }

            foreach (var item in menuItems)
            {
                if (item is not NavigationViewItem navItem)
                {
                    continue;
                }

                if (string.Equals(navItem.Tag?.ToString(), navTag, StringComparison.Ordinal))
                {
                    return navItem;
                }

                var child = FindNavigationItemByTag(navItem.MenuItems, navTag);
                if (child != null)
                {
                    return child;
                }
            }

            return null;
        }

        private void UpdateNavigationItems(NavigationView navView)
        {
            var captureItem = this.FindControl<NavigationViewItem>("CaptureNavItem");
            if (captureItem == null) return;

            // Use shared helper to update navigation items
            NavigationItemsHelper.UpdateCaptureNavigationItems(captureItem);
        }
    }
}
