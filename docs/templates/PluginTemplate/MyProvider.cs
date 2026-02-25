#region License Information (GPL v3)
// Your plugin license header
#endregion

using Newtonsoft.Json;
using XerahS.Uploaders;
using XerahS.Uploaders.PluginSystem;

namespace MyPlugin.Provider;

/// <summary>
/// Example provider: implements IUploaderProvider using UploaderProviderBase from XerahS.Uploaders.
/// Replace with your upload logic (real uploader class that extends Uploader or GenericUploader).
/// </summary>
public class MyProvider : UploaderProviderBase
{
    public override string ProviderId => "myplugin";
    public override string Name => "My Plugin";
    public override string Description => "Example destination plugin";
    public override Version Version => new Version(1, 0, 0);
    public override UploaderCategory[] SupportedCategories => new[] { UploaderCategory.Image };
    public override Type ConfigModelType => typeof(MyConfigModel);

    public override Uploader CreateInstance(string settingsJson)
    {
        var config = JsonConvert.DeserializeObject<MyConfigModel>(settingsJson)
            ?? throw new InvalidOperationException("Invalid settings");
        // TODO: Create a class that extends Uploader or GenericUploader, then return it here, e.g.:
        // return new MyUploader(config);
        throw new NotImplementedException("Replace with: return new YourUploaderClass(config);");
    }

    public override Dictionary<UploaderCategory, string[]> GetSupportedFileTypes()
    {
        return new Dictionary<UploaderCategory, string[]>
        {
            { UploaderCategory.Image, new[] { "png", "jpg", "jpeg", "gif", "webp", "bmp" } }
        };
    }

    public override object? CreateConfigView() => null; // Use default property grid
    public override IUploaderConfigViewModel? CreateConfigViewModel() => null;
}
