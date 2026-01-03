using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareX.Ava.Core;
using ShareX.Ava.Core.Hotkeys;
using ShareX.Ava.UI.Services;

namespace ShareX.Ava.UI.ViewModels;

public partial class WorkflowsViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<WorkflowItemViewModel> _workflows;

    [ObservableProperty]
    private WorkflowItemViewModel? _selectedWorkflow;

    [ObservableProperty]
    private bool _isWizardOpen;

    [ObservableProperty]
    private WorkflowWizardViewModel? _wizardViewModel;

    public WorkflowsViewModel()
    {
        _workflows = new ObservableCollection<WorkflowItemViewModel>(
            SettingManager.HotkeysConfig.Hotkeys.Select(h => new WorkflowItemViewModel(h))
        );
    }

    [RelayCommand]
    private void AddWorkflow()
    {
        WizardViewModel = new WorkflowWizardViewModel();
        IsWizardOpen = true;
    }

    [RelayCommand]
    private void CompleteWizard()
    {
        if (WizardViewModel != null)
        {
            var newSettings = WizardViewModel.ConstructHotkeySettings();
            SettingManager.HotkeysConfig.Hotkeys.Add(newSettings);
            
            var vm = new WorkflowItemViewModel(newSettings);
            Workflows.Add(vm);
            SelectedWorkflow = vm;
            Save();
        }
        
        CloseWizard();
    }

    [RelayCommand]
    private void CloseWizard()
    {
        IsWizardOpen = false;
        WizardViewModel = null;
    }

    [RelayCommand]
    private void RemoveWorkflow()
    {
        if (SelectedWorkflow != null)
        {
            SettingManager.HotkeysConfig.Hotkeys.Remove(SelectedWorkflow.Model);
            Workflows.Remove(SelectedWorkflow);
            Save();
        }
    }

    private void Save()
    {
        SettingManager.SaveHotkeysConfig();
    }
}
