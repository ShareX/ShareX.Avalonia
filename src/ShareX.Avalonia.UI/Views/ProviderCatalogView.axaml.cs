using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShareX.Ava.UI.ViewModels;

namespace ShareX.Ava.UI.Views;

public partial class ProviderCatalogView : UserControl
{
    public ProviderCatalogView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnProviderTapped(object? sender, RoutedEventArgs e)
    {
        Common.DebugHelper.WriteLine($"[ProviderCatalogView] Tapped event fired. Sender: {sender?.GetType().Name}");
        
        if (sender is Border border)
        {
            Common.DebugHelper.WriteLine($"[ProviderCatalogView] Sender is Border. Tag: {border.Tag?.GetType().Name ?? "null"}");
            
            if (border.Tag is ProviderViewModel provider)
            {
                Common.DebugHelper.WriteLine($"[ProviderCatalogView] Executing SelectCommand for: {provider.Name}");
                provider.SelectCommand.Execute(null);
            }
            else
            {
                Common.DebugHelper.WriteLine($"[ProviderCatalogView] ERROR: Border.Tag is NOT ProviderViewModel. It is: {border.Tag}");
            }
        }
        else
        {
            Common.DebugHelper.WriteLine("[ProviderCatalogView] ERROR: Sender is not Border");
        }
    }
}
