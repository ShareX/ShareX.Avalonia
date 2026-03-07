using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using ShareX.ImageEditor.Core.ImageEffects.Drawings;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class DrawBackgroundImageDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        public DrawBackgroundImageDialog()
        {
            AvaloniaXamlLoader.Load(this);
            AttachedToVisualTree += (s, e) => RequestPreview();
        }

        private DrawBackgroundImageEffect CreateEffect()
        {
            TextBox? pathInput = this.FindControl<TextBox>("ImagePathTextBox");
            CheckBox? centerCheck = this.FindControl<CheckBox>("CenterCheckBox");
            CheckBox? tileCheck = this.FindControl<CheckBox>("TileCheckBox");

            return new DrawBackgroundImageEffect
            {
                ImageFilePath = pathInput?.Text ?? string.Empty,
                Center = centerCheck?.IsChecked ?? true,
                Tile = tileCheck?.IsChecked ?? false
            };
        }

        private void RequestPreview()
        {
            PreviewRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Background image"));
        }

        private void OnSettingChanged(object? sender, RoutedEventArgs e)
        {
            if (IsLoaded) RequestPreview();
        }

        private void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (IsLoaded) RequestPreview();
        }

        private async void OnBrowseClick(object? sender, RoutedEventArgs e)
        {
            TopLevel? topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null)
            {
                return;
            }

            IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select background image",
                AllowMultiple = false,
                FileTypeFilter = [FilePickerFileTypes.ImageAll]
            });

            if (files.Count > 0)
            {
                TextBox? pathInput = this.FindControl<TextBox>("ImagePathTextBox");
                pathInput?.SetCurrentValue(TextBox.TextProperty, files[0].Path.LocalPath);
            }
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            ApplyRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Applied background image"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

