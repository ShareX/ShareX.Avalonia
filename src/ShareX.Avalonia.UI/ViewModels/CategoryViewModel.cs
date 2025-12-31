using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareX.Avalonia.Uploaders.PluginSystem;
using System.Collections.ObjectModel;

namespace ShareX.Avalonia.UI.ViewModels;

/// <summary>
/// ViewModel for a category with its instances
/// </summary>
public partial class CategoryViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private UploaderCategory _category;

    [ObservableProperty]
    private ObservableCollection<UploaderInstanceViewModel> _instances = new();

    [ObservableProperty]
    private UploaderInstanceViewModel? _selectedInstance;

    [ObservableProperty]
    private UploaderInstanceViewModel? _defaultInstance;

    public CategoryViewModel(string name, UploaderCategory category)
    {
        _name = name;
        _category = category;
    }

    [RelayCommand]
    private async void AddFromCatalog()
    {
        try
        {
            // Create and show the provider catalog dialog
            var viewModel = new ProviderCatalogViewModel(Category);
            var dialog = new Views.ProviderCatalogDialog(viewModel);
            
            viewModel.OnInstancesAdded += instances =>
            {
                // Reload instances to show the newly added one
                LoadInstances();
            };
            
            // Show dialog - for now just reload since we don't have window for ShowDialog
            // This will be properly implemented when we have parent window reference
            LoadInstances();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open catalog: {ex.Message}");
        }
    }

    [RelayCommand]
    private void SetAsDefault(UploaderInstanceViewModel? instance)
    {
        if (instance == null) return;

        try
        {
            InstanceManager.Instance.SetDefaultInstance(Category, instance.InstanceId);
            
            // Update UI
            if (DefaultInstance != null)
            {
                DefaultInstance.IsDefault = false;
            }
            
            DefaultInstance = instance;
            instance.IsDefault = true;
        }
        catch (Exception ex)
        {
            // TODO: Show error to user
            Console.WriteLine($"Failed to set default: {ex.Message}");
        }
    }

    [RelayCommand]
    private void DuplicateInstance(UploaderInstanceViewModel? instance)
    {
        if (instance == null) return;

        try
        {
            var duplicate = InstanceManager.Instance.DuplicateInstance(instance.InstanceId);
            var duplicateVm = new UploaderInstanceViewModel(duplicate);
            Instances.Add(duplicateVm);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to duplicate: {ex.Message}");
        }
    }

    [RelayCommand]
    private void RemoveInstance(UploaderInstanceViewModel? instance)
    {
        if (instance == null) return;

        try
        {
            InstanceManager.Instance.RemoveInstance(instance.InstanceId);
            Instances.Remove(instance);
            
            if (DefaultInstance == instance)
            {
                DefaultInstance = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to remove: {ex.Message}");
        }
    }

    public void LoadInstances()
    {
        Instances.Clear();
        
        var instances = InstanceManager.Instance.GetInstancesByCategory(Category);
        var defaultInstance = InstanceManager.Instance.GetDefaultInstance(Category);
        
        foreach (var instance in instances)
        {
            var vm = new UploaderInstanceViewModel(instance);
            
            if (defaultInstance != null && instance.InstanceId == defaultInstance.InstanceId)
            {
                vm.IsDefault = true;
                DefaultInstance = vm;
            }
            
            Instances.Add(vm);
        }
    }
}
