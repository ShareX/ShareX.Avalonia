using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareX.Avalonia.UI.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace ShareX.Avalonia.UI.ViewModels
{
    /// <summary>
    /// ViewModel for the Effects Panel
    /// </summary>
    public partial class EffectsPanelViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<EffectViewModel> _availableEffects = new();

        [ObservableProperty]
        private ObservableCollection<string> _categories = new();

        [ObservableProperty]
        private string? _selectedCategory;

        [ObservableProperty]
        private EffectViewModel? _selectedEffect;

        public EffectsPanelViewModel()
        {
            LoadEffects();
            LoadCategories();
        }

        /// <summary>
        /// Load all available effects
        /// </summary>
        private void LoadEffects()
        {
            var effects = EffectCatalogService.GetAllEffects();
            AvailableEffects = new ObservableCollection<EffectViewModel>(effects);
        }

        /// <summary>
        /// Load all categories
        /// </summary>
        private void LoadCategories()
        {
            var categories = EffectCatalogService.GetCategories();
            Categories = new ObservableCollection<string>(categories);
            
            // Select first category by default
            if (Categories.Any())
            {
                SelectedCategory = Categories.First();
            }
        }

        /// <summary>
        /// Filter effects by selected category
        /// </summary>
        partial void OnSelectedCategoryChanged(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                LoadEffects();
                return;
            }

            var filtered = EffectCatalogService.GetAllEffects()
                .Where(e => e.Category.Equals(value, System.StringComparison.OrdinalIgnoreCase))
                .ToList();

            AvailableEffects.Clear();
            foreach (var effect in filtered)
            {
                AvailableEffects.Add(effect);
            }
        }

        /// <summary>
        /// Reset selected effect parameters to defaults
        /// </summary>
        [RelayCommand]
        private void ResetEffect()
        {
            SelectedEffect?.ResetParameters();
        }

        /// <summary>
        /// Select a category
        /// </summary>
        [RelayCommand]
        private void SelectCategory(string category)
        {
            SelectedCategory = category;
        }
    }
}
