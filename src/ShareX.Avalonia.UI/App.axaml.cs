using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using ShareX.Avalonia.UI.Views;
using ShareX.Avalonia.UI.ViewModels;
using ShareX.Avalonia.Uploaders.PluginSystem;
using ShareX.Avalonia.Uploaders.Plugins.ImgurPlugin;
using ShareX.Avalonia.Uploaders.Plugins.AmazonS3Plugin;

namespace ShareX.Avalonia.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Register built-in providers at startup
        RegisterProviders();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new Views.MainWindow
            {
                DataContext = new ViewModels.MainViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void RegisterProviders()
    {
        // Register Imgur provider
        var imgurProvider = new ImgurProvider();
        ProviderCatalog.RegisterProvider(imgurProvider);

        // Register Amazon S3 provider
        var s3Provider = new AmazonS3Provider();
        ProviderCatalog.RegisterProvider(s3Provider);

        Console.WriteLine("Registered built-in providers: Imgur, Amazon S3");
    }
}
