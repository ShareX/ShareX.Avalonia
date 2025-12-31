using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ShareX.Ava.UI.ViewModels;

namespace ShareX.Ava.UI.Views;

public partial class HotkeyEditView : Window
{
    public HotkeyEditView()
    {
        InitializeComponent();
    }

    private void KeyTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        e.Handled = true;

        // Ignore modifier-only presses for final binding, but we can't easily filter them out here
        // without some logic. Usually we just accept whatever Avalonia gives us.
        // But we probably don't want to bind "Ctrl" as a hotkey.
        
        // Basic filter: If key is a modifier key, don't set it as the main key
        if (IsModifierKey(e.Key))
        {
            return;
        }

        if (DataContext is HotkeyEditViewModel vm)
        {
            vm.SelectedKey = e.Key;
            vm.SelectedModifiers = e.KeyModifiers;
            
            // Force update display manually if needed, but binding should handle it
            // vm.Model.HotkeyInfo.Key = e.Key;
            // vm.Model.HotkeyInfo.Modifiers = e.KeyModifiers;
        }
    }

    private bool IsModifierKey(Key key)
    {
        return key == Key.LeftCtrl || key == Key.RightCtrl ||
               key == Key.LeftAlt || key == Key.RightAlt ||
               key == Key.LeftShift || key == Key.RightShift ||
               key == Key.LWin || key == Key.RWin;
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is HotkeyEditViewModel vm)
        {
            vm.Save();
            Close(true); // Return true for success
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false); // Return false for cancel
    }
}
