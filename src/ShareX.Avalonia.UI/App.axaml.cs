using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using ShareX.Avalonia.UI.Views;
using ShareX.Avalonia.UI.ViewModels;

namespace ShareX.Avalonia.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new Views.MainWindow
            {
                DataContext = new ViewModels.MainViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
