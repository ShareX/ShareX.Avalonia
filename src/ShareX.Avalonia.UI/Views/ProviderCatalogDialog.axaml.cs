using Avalonia;
using Avalonia.Controls;
using ShareX.Avalonia.UI.ViewModels;

namespace ShareX.Avalonia.UI.Views;

public partial class ProviderCatalogDialog : Window
{
    public ProviderCatalogDialog()
    {
        InitializeComponent();
    }

    public ProviderCatalogDialog(ProviderCatalogViewModel viewModel) : this()
    {
        DataContext = viewModel;
        
        // Wire up event handlers
        viewModel.OnInstancesAdded += instances =>
        {
            Close(true); // Return true on success
        };
        
        viewModel.OnCancelled += () =>
        {
            Close(false); // Return false oncancel
        };
    }
}
