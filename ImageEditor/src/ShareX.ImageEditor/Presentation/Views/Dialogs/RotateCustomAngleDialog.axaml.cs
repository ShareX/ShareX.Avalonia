using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.ImageEditor.Presentation.ViewModels;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs;

public partial class RotateCustomAngleDialog : UserControl
{
    public RotateCustomAngleDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnResetClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.RotateAngleDegrees = 0;
        }
    }

    private void OnRotateLeftClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.RotateAngleDegrees -= 90;
        }
    }

    private void OnRotateRightClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.RotateAngleDegrees += 90;
        }
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.CommitRotateCustomAngleCommand?.Execute(null);
            vm.CloseEffectsPanelCommand?.Execute(null);
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.CancelRotateCustomAngleCommand?.Execute(null);
            vm.CloseEffectsPanelCommand?.Execute(null);
        }
    }
}
