using CommunityToolkit.Mvvm.ComponentModel;
using ShareX.Avalonia.Uploaders.PluginSystem;

namespace ShareX.Avalonia.UI.ViewModels;

/// <summary>
/// ViewModel for a single uploader instance in the list
/// </summary>
public partial class UploaderInstanceViewModel : ViewModelBase
{
    [ObservableProperty]
    private Guid _instanceId;

    [ObservableProperty]
    private string _providerId = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private UploaderCategory _category;

    [ObservableProperty]
    private bool _isDefault;

    [ObservableProperty]
    private bool _isAvailable;

    [ObservableProperty]
    private string _settingsJson = "{}";

    /// <summary>
    /// The actual instance model
    /// </summary>
    public UploaderInstance Instance { get; }

    public UploaderInstanceViewModel(UploaderInstance instance)
    {
        Instance = instance;
        _instanceId = instance.InstanceId;
        _providerId = instance.ProviderId;
        _displayName = instance.DisplayName;
        _category = instance.Category;
        _settingsJson = instance.SettingsJson;
        _isAvailable = instance.IsAvailable;
    }

    public void UpdateFromInstance(UploaderInstance instance)
    {
        DisplayName = instance.DisplayName;
        SettingsJson = instance.SettingsJson;
        IsAvailable = instance.IsAvailable;
    }
}
