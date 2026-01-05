using Avalonia;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ShareX.Ava.UI.ViewModels;
using EditorViewControl = ShareX.Editor.Views.EditorView;
using ShareX.Editor.ViewModels;
using ShareX.Editor.Annotations;
using FluentAvalonia.UI.Controls;
using System.ComponentModel;

namespace ShareX.Ava.UI.Views
{
    public partial class MainWindow : Window
    {
        private EditorViewControl? _editorView;
        private readonly EditorViewModel _editorViewModel = new();
        private INotifyPropertyChanged? _dataContextNotifier;
        
        public MainWindow()
        {
            InitializeComponent();
            KeyDown += OnKeyDown;
            
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
        }

        private void OnWindowOpened(object? sender, EventArgs e)
        {
            // Maximize window and center it on screen
            this.WindowState = WindowState.Maximized;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        private void OnNavSelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
        {
            var navView = sender as NavigationView;
            var contentFrame = this.FindControl<ContentControl>("ContentFrame");
            var selectedItem = navView?.SelectedItem as NavigationViewItem;
            
            if (contentFrame != null && selectedItem != null && DataContext is MainViewModel vm)
            {
                var tag = selectedItem.Tag?.ToString();
                
                switch (tag)
                {
                    case "Capture_Fullscreen":
                        vm.CaptureFullscreenCommand.Execute(null);
                        // Navigate back to Editor to see the captured image
                        NavigateToEditor();
                        break;
                    case "Capture_Region":
                        vm.CaptureRegionCommand.Execute(null);
                        NavigateToEditor();
                        break;
                    case "Capture_Window":
                        vm.CaptureWindowCommand.Execute(null);
                        NavigateToEditor();
                        break;
                    case "Editor":
                        if (_editorView == null)
                        {
                            _editorView = new EditorViewControl
                            {
                                DataContext = _editorViewModel
                            };
                        }
                        contentFrame.Content = _editorView;
                        break;
                    case "History":
                        contentFrame.Content = new HistoryView();
                        break;
                    case "Workflows":
                        contentFrame.Content = new WorkflowsView();
                        break;
                    case "Settings":
                        contentFrame.Content = new SettingsView();
                        break;
                    case "Settings_App":
                        contentFrame.Content = new ApplicationSettingsView();
                        break;
                    case "Settings_Task":
                        contentFrame.Content = new TaskSettingsView();
                        break;

                    case "Settings_Dest":
                        contentFrame.Content = new DestinationSettingsView();
                        break;
                    case "Debug":
                        contentFrame.Content = new DebugView();
                        break;
                }
            }
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            var editorVm = _editorView?.DataContext as EditorViewModel;
            if (editorVm == null) return;
            
            if (e.Source is TextBox) return;

            if (e.KeyModifiers == KeyModifiers.None && e.Key == Key.Enter && editorVm.ActiveTool == EditorTool.Crop)
            {
                if (_editorView != null && _editorView.IsVisible)
                {
                    _editorView.PerformCrop();
                    e.Handled = true;
                    return;
                }
            }

            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                switch (e.Key)
                {
                    case Key.Z:
                        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                            editorVm.RedoCommand.Execute(null);
                        else
                            editorVm.UndoCommand.Execute(null);
                        e.Handled = true;
                        return;
                    case Key.Y:
                        editorVm.RedoCommand.Execute(null);
                        e.Handled = true;
                        return;
                    case Key.C:
                        editorVm.CopyCommand.Execute(null);
                        e.Handled = true;
                        return;
                    case Key.S:
                        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                            editorVm.SaveAsCommand.Execute(null);
                        else
                            editorVm.QuickSaveCommand.Execute(null);
                        e.Handled = true;
                        return;
                }
            }

            if (e.KeyModifiers == KeyModifiers.None)
            {
                switch (e.Key)
                {
                    case Key.V:
                        editorVm.SelectToolCommand.Execute(EditorTool.Select);
                        e.Handled = true;
                        break;
                    case Key.R:
                        editorVm.SelectToolCommand.Execute(EditorTool.Rectangle);
                        e.Handled = true;
                        break;
                    case Key.E:
                        editorVm.SelectToolCommand.Execute(EditorTool.Ellipse);
                        e.Handled = true;
                        break;
                    case Key.A:
                        editorVm.SelectToolCommand.Execute(EditorTool.Arrow);
                        e.Handled = true;
                        break;
                    case Key.L:
                        editorVm.SelectToolCommand.Execute(EditorTool.Line);
                        e.Handled = true;
                        break;
                    case Key.T:
                        editorVm.SelectToolCommand.Execute(EditorTool.Text);
                        e.Handled = true;
                        break;
                    case Key.N:
                        editorVm.SelectToolCommand.Execute(EditorTool.Number);
                        e.Handled = true;
                        break;
                    case Key.S:
                        editorVm.SelectToolCommand.Execute(EditorTool.Spotlight);
                        e.Handled = true;
                        break;
                    case Key.C:
                        editorVm.SelectToolCommand.Execute(EditorTool.Crop);
                        e.Handled = true;
                        break;
                    case Key.Delete:
                    case Key.Back:
                        editorVm.DeleteSelectedCommand.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.Escape:
                        editorVm.SelectToolCommand.Execute(EditorTool.Select);
                        e.Handled = true;
                        break;
                }
            }
        }


        private void OnBackdropPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.CloseModalCommand.Execute(null);
            }
        }


        
        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (_dataContextNotifier != null)
            {
                _dataContextNotifier.PropertyChanged -= OnMainViewModelPropertyChanged;
            }

            _dataContextNotifier = DataContext as INotifyPropertyChanged;
            if (_dataContextNotifier != null)
            {
                _dataContextNotifier.PropertyChanged += OnMainViewModelPropertyChanged;
            }
        }

        private void OnMainViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not MainViewModel vm) return;

            if (e.PropertyName == nameof(MainViewModel.PreviewImage))
            {
                _editorViewModel.PreviewImage = vm.PreviewImage;
            }
            else if (e.PropertyName == nameof(MainViewModel.ActiveTool))
            {
                _editorViewModel.ActiveTool = vm.ActiveTool;
            }
            else if (e.PropertyName == nameof(MainViewModel.SelectedColor))
            {
                _editorViewModel.SelectedColor = vm.SelectedColor;
            }
            else if (e.PropertyName == nameof(MainViewModel.StrokeWidth))
            {
                _editorViewModel.StrokeWidth = vm.StrokeWidth;
            }
        }
            public void NavigateToEditor()
        {
            var navView = this.FindControl<NavigationView>("NavView");
            if (navView != null)
            {
                // Navigate to Editor (Tag="Editor")
                foreach (var item in navView.MenuItems)
                {
                    if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == "Editor")
                    {
                        navView.SelectedItem = navItem;
                        break;
                    }
                }
            }
            
            // Ensure window is visible and active
            if (!this.IsVisible)
            {
                this.Show();
            }
            
            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Maximized;
            }
            
            this.Activate();
            this.Focus();
        }
    }
}
