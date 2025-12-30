using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.Avalonia.UI.ViewModels;

namespace ShareX.Avalonia.UI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            KeyDown += OnKeyDown;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;
            
            // Skip if typing in a text input
            if (e.Source is TextBox) return;

            // Handle Ctrl key combinations
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                switch (e.Key)
                {
                    case Key.Z:
                        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                            vm.RedoCommand.Execute(null);
                        else
                            vm.UndoCommand.Execute(null);
                        e.Handled = true;
                        return;
                    case Key.Y:
                        vm.RedoCommand.Execute(null);
                        e.Handled = true;
                        return;
                    case Key.C:
                        vm.CopyCommand.Execute(null);
                        e.Handled = true;
                        return;
                    case Key.S:
                        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                            vm.SaveAsCommand.Execute(null);
                        else
                            vm.QuickSaveCommand.Execute(null);
                        e.Handled = true;
                        return;
                }
            }

            // Tool selection shortcuts (single keys without modifiers)
            if (e.KeyModifiers == KeyModifiers.None)
            {
                switch (e.Key)
                {
                    case Key.V:
                        vm.SelectToolCommand.Execute(EditorTool.Select);
                        e.Handled = true;
                        break;
                    case Key.R:
                        vm.SelectToolCommand.Execute(EditorTool.Rectangle);
                        e.Handled = true;
                        break;
                    case Key.E:
                        vm.SelectToolCommand.Execute(EditorTool.Ellipse);
                        e.Handled = true;
                        break;
                    case Key.A:
                        vm.SelectToolCommand.Execute(EditorTool.Arrow);
                        e.Handled = true;
                        break;
                    case Key.L:
                        vm.SelectToolCommand.Execute(EditorTool.Line);
                        e.Handled = true;
                        break;
                    case Key.T:
                        vm.SelectToolCommand.Execute(EditorTool.Text);
                        e.Handled = true;
                        break;
                    case Key.N:
                        vm.SelectToolCommand.Execute(EditorTool.Number);
                        e.Handled = true;
                        break;
                    case Key.S:
                        vm.SelectToolCommand.Execute(EditorTool.Spotlight);
                        e.Handled = true;
                        break;
                    case Key.C:
                        vm.SelectToolCommand.Execute(EditorTool.Crop);
                        e.Handled = true;
                        break;
                    case Key.Delete:
                    case Key.Back:
                        vm.DeleteSelectedCommand.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.Escape:
                        vm.SelectToolCommand.Execute(EditorTool.Select);
                        e.Handled = true;
                        break;
                }
            }
        }

        private void OnMinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnMaximizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized 
                ? WindowState.Normal 
                : WindowState.Maximized;
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
