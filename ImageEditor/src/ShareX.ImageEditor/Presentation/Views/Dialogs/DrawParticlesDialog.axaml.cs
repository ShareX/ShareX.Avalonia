using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using ShareX.ImageEditor.Core.ImageEffects.Drawings;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class DrawParticlesDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        public DrawParticlesDialog()
        {
            AvaloniaXamlLoader.Load(this);
            AttachedToVisualTree += (s, e) => RequestPreview();
        }

        private int GetInt(string name, int fallback)
        {
            NumericUpDown? control = this.FindControl<NumericUpDown>(name);
            return (int)Math.Round(control?.Value ?? fallback);
        }

        private bool GetBool(string name, bool fallback = false)
        {
            return this.FindControl<CheckBox>(name)?.IsChecked ?? fallback;
        }

        private DrawParticlesEffect CreateEffect()
        {
            TextBox? folderInput = this.FindControl<TextBox>("ImageFolderTextBox");

            return new DrawParticlesEffect
            {
                ImageFolder = folderInput?.Text ?? string.Empty,
                ImageCount = GetInt("ImageCountInput", 1),
                Background = GetBool("BackgroundCheckBox"),
                RandomSize = GetBool("RandomSizeCheckBox"),
                RandomSizeMin = GetInt("RandomSizeMinInput", 64),
                RandomSizeMax = GetInt("RandomSizeMaxInput", 128),
                RandomAngle = GetBool("RandomAngleCheckBox"),
                RandomAngleMin = GetInt("RandomAngleMinInput", 0),
                RandomAngleMax = GetInt("RandomAngleMaxInput", 360),
                RandomOpacity = GetBool("RandomOpacityCheckBox"),
                RandomOpacityMin = GetInt("RandomOpacityMinInput", 0),
                RandomOpacityMax = GetInt("RandomOpacityMaxInput", 100),
                NoOverlap = GetBool("NoOverlapCheckBox"),
                NoOverlapOffset = GetInt("NoOverlapOffsetInput", 0),
                EdgeOverlap = GetBool("EdgeOverlapCheckBox")
            };
        }

        private void RequestPreview()
        {
            PreviewRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Particles"));
        }

        private void OnSettingChanged(object? sender, RoutedEventArgs e)
        {
            if (IsLoaded) RequestPreview();
        }

        private void OnNumericChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            if (IsLoaded) RequestPreview();
        }

        private void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (IsLoaded) RequestPreview();
        }

        private async void OnBrowseFolderClick(object? sender, RoutedEventArgs e)
        {
            TopLevel? topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null)
            {
                return;
            }

            IReadOnlyList<IStorageFolder> folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select particles folder",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                TextBox? folderInput = this.FindControl<TextBox>("ImageFolderTextBox");
                folderInput?.SetCurrentValue(TextBox.TextProperty, folders[0].Path.LocalPath);
            }
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            ApplyRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Applied particles"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

