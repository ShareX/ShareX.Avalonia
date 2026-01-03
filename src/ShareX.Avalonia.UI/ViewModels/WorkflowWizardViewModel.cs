using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using ShareX.Ava.Core;
using ShareX.Ava.Core.Hotkeys;
using ShareX.Ava.Common;
using ShareX.Ava.Uploaders;
using ShareX.UploadersLib;

namespace ShareX.Ava.UI.ViewModels;

public enum WizardDestinationType
{
    Standard,
    CustomUploader,
    FTP,
    FileDestination,
    None
}

public class DestinationItem
{
    public string Name { get; set; }
    public WizardDestinationType Type { get; set; }
    public object Value { get; set; } // Enum value or Index
    public int CustomIndex { get; set; } = -1;
    public string Group { get; set; }

    public override string ToString() => Name;
}

public partial class WorkflowWizardViewModel : ObservableObject
{
    // Step 1: Job Selection
    public ObservableCollection<JobCategoryViewModel> JobCategories { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AvailableDestinations))]
    [NotifyPropertyChangedFor(nameof(ReviewJobName))]
    private HotkeyItemViewModel? _selectedJob;

    // Step 2: Destination
    public ObservableCollection<DestinationItem> AvailableDestinations { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReviewDestinationName))]
    private DestinationItem? _selectedDestination;

    // Step 3: Tasks
    [ObservableProperty] public bool taskSaveToFile;
    [ObservableProperty] public bool taskCopyImage;
    [ObservableProperty] public bool taskUpload;
    [ObservableProperty] public bool taskCopyUrl;
    
    // Step 4: General
    [ObservableProperty] private string _workflowName;
    [ObservableProperty] private string _hotkeyString = "None";
    
    // Summary properties
    public string ReviewJobName => SelectedJob?.Description ?? "None";
    public string ReviewDestinationName => SelectedDestination?.Name ?? "None";

    public WorkflowWizardViewModel()
    {
        LoadJobCategories();
        // Default selection
        SelectedJob = JobCategories.FirstOrDefault()?.Jobs.FirstOrDefault();
        WorkflowName = "My New Workflow";
            
        // Default tasks
        TaskSaveToFile = true;
        TaskCopyImage = true;
    }

    private void LoadJobCategories()
    {
        // Group HotkeyTypes by their Category attribute
        var allTypes = Enum.GetValues(typeof(HotkeyType)).Cast<HotkeyType>()
            .Where(t => t != HotkeyType.None);

        var grouped = allTypes.GroupBy(GetHotkeyCategory)
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .OrderBy(g => GetCategoryOrder(g.Key));

        foreach (var group in grouped)
        {
            var category = new JobCategoryViewModel(GetCategoryDisplayName(group.Key), group);
            JobCategories.Add(category);
        }
    }

    private string GetHotkeyCategory(HotkeyType type)
    {
        var field = type.GetType().GetField(type.ToString());
        if (field != null)
        {
            var attrs = (CategoryAttribute[])field.GetCustomAttributes(typeof(CategoryAttribute), false);
            if (attrs.Length > 0)
            {
                return attrs[0].Category;
            }
        }
        return string.Empty;
    }

    private string GetCategoryDisplayName(string category)
    {
        // Map internal category names to display names
        return category switch
        {
            EnumExtensions.HotkeyType_Category_Upload => "Upload",
            EnumExtensions.HotkeyType_Category_ScreenCapture => "Screen Capture",
            EnumExtensions.HotkeyType_Category_ScreenRecord => "Screen Record",
            EnumExtensions.HotkeyType_Category_Tools => "Tools",
            EnumExtensions.HotkeyType_Category_Other => "Other",
            _ => category
        };
    }

    private int GetCategoryOrder(string category)
    {
        // Define display order
        return category switch
        {
            EnumExtensions.HotkeyType_Category_ScreenCapture => 0,
            EnumExtensions.HotkeyType_Category_ScreenRecord => 1,
            EnumExtensions.HotkeyType_Category_Upload => 2,
            EnumExtensions.HotkeyType_Category_Tools => 3,
            EnumExtensions.HotkeyType_Category_Other => 4,
            _ => 99
        };
    }

    partial void OnSelectedJobChanged(HotkeyItemViewModel? value)
    {
        UpdateDestinations();
        
        // Auto-set reasonable defaults based on job category
        if (value != null)
        {
            string category = GetHotkeyCategory(value.Model.Job);
            
            if (category == EnumExtensions.HotkeyType_Category_Upload)
            {
                TaskUpload = true;
                TaskCopyUrl = true;
            }
            else if (category == EnumExtensions.HotkeyType_Category_ScreenCapture || 
                     category == EnumExtensions.HotkeyType_Category_ScreenRecord)
            {
                TaskSaveToFile = true;
                TaskUpload = true;
                TaskCopyUrl = true;
            }
        }
    }

    private void UpdateDestinations()
    {
        AvailableDestinations.Clear();

        if (SelectedJob == null) return;

        string category = GetHotkeyCategory(SelectedJob.Model.Job);
        
        // Determine which destination types to show based on category
        bool showImageUploaders = false;
        bool showTextUploaders = false;
        bool showFileUploaders = false;

        switch (category)
        {
            case EnumExtensions.HotkeyType_Category_ScreenCapture:
            case EnumExtensions.HotkeyType_Category_ScreenRecord:
                // Screen captures can go to Image or File uploaders
                showImageUploaders = true;
                showFileUploaders = true;
                break;
                
            case EnumExtensions.HotkeyType_Category_Upload:
                // Check specific upload type
                var job = SelectedJob.Model.Job;
                if (job == HotkeyType.UploadText)
                {
                    showTextUploaders = true;
                    showFileUploaders = true;
                }
                else if (job == HotkeyType.FileUpload || job == HotkeyType.FolderUpload)
                {
                    showFileUploaders = true;
                }
                else // ClipboardUpload, DragDropUpload, etc - could be image or file
                {
                    showImageUploaders = true;
                    showFileUploaders = true;
                }
                break;
                
            case EnumExtensions.HotkeyType_Category_Tools:
                // Tools generally don't upload, but offer option
                showImageUploaders = true;
                showFileUploaders = true;
                break;
        }

        // Add "None" / "Local Only"
        AvailableDestinations.Add(new DestinationItem 
        { 
            Name = "Local Storage (No Upload)", 
            Type = WizardDestinationType.None, 
            Group = "General" 
        });

        if (showImageUploaders)
        {
            // Standard Image Uploaders
            foreach (ImageDestination dest in Enum.GetValues(typeof(ImageDestination)))
            {
                if (dest == ImageDestination.CustomImageUploader) continue;
                if (dest == ImageDestination.FileUploader) continue;
                
                AvailableDestinations.Add(new DestinationItem
                {
                    Name = EnumExtensions.GetDescription(dest),
                    Type = WizardDestinationType.Standard,
                    Value = dest,
                    Group = "Image Uploaders"
                });
            }

            // Custom Image Uploaders
            var customUploaders = SettingManager.UploadersConfig.CustomUploadersList;
            for (int i = 0; i < customUploaders.Count; i++)
            {
                var custom = customUploaders[i];
                if (custom.DestinationType.HasFlag(CustomUploaderDestinationType.ImageUploader))
                {
                     AvailableDestinations.Add(new DestinationItem
                    {
                        Name = custom.Name,
                        Type = WizardDestinationType.CustomUploader,
                        CustomIndex = i,
                        Group = "Custom Image Uploaders"
                    });
                }
            }
        }
        
        if (showTextUploaders)
        {
             // Standard Text Uploaders
            foreach (TextDestination dest in Enum.GetValues(typeof(TextDestination)))
            {
                 if (dest == TextDestination.CustomTextUploader) continue;
                 if (dest == TextDestination.FileUploader) continue;

                AvailableDestinations.Add(new DestinationItem
                {
                    Name = EnumExtensions.GetDescription(dest),
                    Type = WizardDestinationType.Standard,
                    Value = dest,
                    Group = "Text Uploaders"
                });
            }
            
            // Custom Text Uploaders
            var customUploaders = SettingManager.UploadersConfig.CustomUploadersList;
            for (int i = 0; i < customUploaders.Count; i++)
            {
                var custom = customUploaders[i];
                if (custom.DestinationType.HasFlag(CustomUploaderDestinationType.TextUploader))
                {
                     AvailableDestinations.Add(new DestinationItem
                    {
                        Name = custom.Name,
                        Type = WizardDestinationType.CustomUploader,
                        CustomIndex = i,
                        Group = "Custom Text Uploaders"
                    });
                }
            }
        }

        if (showFileUploaders)
        {
            // Standard File Uploaders
            foreach (FileDestination dest in Enum.GetValues(typeof(FileDestination)))
            {
                if (dest == FileDestination.CustomFileUploader) continue;
                if (dest == FileDestination.SharedFolder) continue; // Handled separately
                if (dest == FileDestination.Email) continue; // Handled separately
                
                AvailableDestinations.Add(new DestinationItem
                {
                    Name = EnumExtensions.GetDescription(dest),
                    Type = WizardDestinationType.Standard,
                    Value = dest,
                    Group = "File Uploaders"
                });
            }
            
            // Custom File Uploaders
            var customUploaders = SettingManager.UploadersConfig.CustomUploadersList;
            for (int i = 0; i < customUploaders.Count; i++)
            {
                var custom = customUploaders[i];
                if (custom.DestinationType.HasFlag(CustomUploaderDestinationType.FileUploader))
                {
                     AvailableDestinations.Add(new DestinationItem
                    {
                        Name = custom.Name,
                        Type = WizardDestinationType.CustomUploader,
                        CustomIndex = i,
                        Group = "Custom File Uploaders"
                    });
                }
            }
        }

        // FTP accounts (Available for all upload types generally)
        if (showImageUploaders || showFileUploaders)
        {
            var ftps = SettingManager.UploadersConfig.FTPAccountList;
            for (int i = 0; i < ftps.Count; i++)
            {
                AvailableDestinations.Add(new DestinationItem
                {
                    Name = $"FTP: {ftps[i].Name}",
                    Type = WizardDestinationType.FTP,
                    CustomIndex = i,
                    Group = "FTP"
                });
            }
        }
        
        // Select first valid destination (prefer Imgur for images, first for others)
        if (showImageUploaders)
        {
            SelectedDestination = AvailableDestinations.FirstOrDefault(d => d.Group == "Image Uploaders" && d.Name.Contains("Imgur")) 
                                  ?? AvailableDestinations.FirstOrDefault(d => d.Group == "Image Uploaders")
                                  ?? AvailableDestinations.FirstOrDefault();
        }
        else
        {
            SelectedDestination = AvailableDestinations.Skip(1).FirstOrDefault() ?? AvailableDestinations.FirstOrDefault();
        }
    }

    public HotkeySettings ConstructHotkeySettings()
    {
        var settings = new HotkeySettings();
        settings.TaskSettings.Description = WorkflowName;
        settings.Job = SelectedJob?.Model.Job ?? HotkeyType.None;
        
        // Apply Destination Logic
        if (SelectedDestination != null && SelectedDestination.Type != WizardDestinationType.None)
        {
             if (SelectedDestination.Type == WizardDestinationType.Standard)
             {
                 if (SelectedDestination.Value is ImageDestination imgDest)
                 {
                     settings.TaskSettings.ImageDestination = imgDest;
                 }
                 else if (SelectedDestination.Value is TextDestination txtDest)
                 {
                     settings.TaskSettings.TextDestination = txtDest;
                 }
                 else if (SelectedDestination.Value is FileDestination fileDest)
                 {
                     settings.TaskSettings.FileDestination = fileDest;
                 }
             }
             else if (SelectedDestination.Type == WizardDestinationType.CustomUploader)
             {
                 settings.TaskSettings.OverrideCustomUploader = true;
                 settings.TaskSettings.CustomUploaderIndex = SelectedDestination.CustomIndex;
                 
                 // Determine which destination type based on group
                 if (SelectedDestination.Group.Contains("Image"))
                 {
                     settings.TaskSettings.ImageDestination = ImageDestination.CustomImageUploader;
                 }
                 else if (SelectedDestination.Group.Contains("Text"))
                 {
                     settings.TaskSettings.TextDestination = TextDestination.CustomTextUploader;
                 }
                 else if (SelectedDestination.Group.Contains("File"))
                 {
                     settings.TaskSettings.FileDestination = FileDestination.CustomFileUploader;
                 }
             }
             else if (SelectedDestination.Type == WizardDestinationType.FTP)
             {
                 settings.TaskSettings.OverrideFTP = true;
                 settings.TaskSettings.FTPIndex = SelectedDestination.CustomIndex;
                 settings.TaskSettings.ImageDestination = ImageDestination.FileUploader;
                 settings.TaskSettings.FileDestination = FileDestination.FTP;
             }
        }

        // Apply Tasks
        AfterCaptureTasks captureTasks = 0;
        if (TaskSaveToFile) captureTasks |= AfterCaptureTasks.SaveImageToFile;
        if (TaskCopyImage) captureTasks |= AfterCaptureTasks.CopyImageToClipboard;
        if (TaskUpload) captureTasks |= AfterCaptureTasks.UploadImageToHost;
        
        settings.TaskSettings.AfterCaptureJob = captureTasks;
        settings.TaskSettings.UseDefaultAfterCaptureJob = false;

        AfterUploadTasks uploadTasks = 0;
        if (TaskCopyUrl) uploadTasks |= AfterUploadTasks.CopyURLToClipboard;
        
        settings.TaskSettings.AfterUploadJob = uploadTasks;
        settings.TaskSettings.UseDefaultAfterUploadJob = false;
        
        settings.TaskSettings.UseDefaultDestinations = false;

        return settings;
    }
}

public class JobCategoryViewModel
{
    public string Name { get; }
    public ObservableCollection<HotkeyItemViewModel> Jobs { get; }

    public JobCategoryViewModel(string name, IEnumerable<HotkeyType> jobs)
    {
        Name = name;
        Jobs = new ObservableCollection<HotkeyItemViewModel>(
            jobs.Select(j => new HotkeyItemViewModel(new HotkeySettings(j, Key.None)))
        );
    }
}
