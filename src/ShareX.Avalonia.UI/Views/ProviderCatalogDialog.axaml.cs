using Avalonia;
using Avalonia.Controls;
using ShareX.Ava.UI.ViewModels;

namespace ShareX.Ava.UI.Views;

public partial class ProviderCatalogDialog : UserControl
{
    public ProviderCatalogDialog()
    {
        InitializeComponent();
    }

    public ProviderCatalogDialog(ProviderCatalogViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
