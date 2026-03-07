using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using ShareX.ImageEditor.Core.ImageEffects.Drawings;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    public partial class DrawImageDialog : UserControl, IEffectDialog
    {
        public event EventHandler<EffectEventArgs>? ApplyRequested;
        public event EventHandler<EffectEventArgs>? PreviewRequested;
        public event EventHandler? CancelRequested;

        public DrawImageDialog()
        {
            AvaloniaXamlLoader.Load(this);
            AttachedToVisualTree += (s, e) => RequestPreview();
        }

        private int GetInt(string name, int fallback)
        {
            NumericUpDown? control = this.FindControl<NumericUpDown>(name);
            return (int)Math.Round(control?.Value ?? fallback);
        }

        private DrawingPlacement GetPlacement()
        {
            return this.FindControl<ComboBox>("PlacementComboBox")?.SelectedIndex switch
            {
                1 => DrawingPlacement.TopCenter,
                2 => DrawingPlacement.TopRight,
                3 => DrawingPlacement.MiddleLeft,
                4 => DrawingPlacement.MiddleCenter,
                5 => DrawingPlacement.MiddleRight,
                6 => DrawingPlacement.BottomLeft,
                7 => DrawingPlacement.BottomCenter,
                8 => DrawingPlacement.BottomRight,
                _ => DrawingPlacement.TopLeft
            };
        }

        private DrawingImageSizeMode GetSizeMode()
        {
            return this.FindControl<ComboBox>("SizeModeComboBox")?.SelectedIndex switch
            {
                1 => DrawingImageSizeMode.AbsoluteSize,
                2 => DrawingImageSizeMode.PercentageOfWatermark,
                3 => DrawingImageSizeMode.PercentageOfCanvas,
                _ => DrawingImageSizeMode.DontResize
            };
        }

        private DrawingImageRotateFlipType GetRotateFlip()
        {
            return this.FindControl<ComboBox>("RotateFlipComboBox")?.SelectedIndex switch
            {
                1 => DrawingImageRotateFlipType.Rotate90,
                2 => DrawingImageRotateFlipType.Rotate180,
                3 => DrawingImageRotateFlipType.Rotate270,
                4 => DrawingImageRotateFlipType.FlipX,
                5 => DrawingImageRotateFlipType.Rotate90FlipX,
                6 => DrawingImageRotateFlipType.FlipY,
                7 => DrawingImageRotateFlipType.Rotate90FlipY,
                _ => DrawingImageRotateFlipType.None
            };
        }

        private DrawingInterpolationMode GetInterpolationMode()
        {
            return this.FindControl<ComboBox>("InterpolationComboBox")?.SelectedIndex switch
            {
                1 => DrawingInterpolationMode.Bicubic,
                2 => DrawingInterpolationMode.HighQualityBilinear,
                3 => DrawingInterpolationMode.Bilinear,
                4 => DrawingInterpolationMode.NearestNeighbor,
                _ => DrawingInterpolationMode.HighQualityBicubic
            };
        }

        private DrawingCompositingMode GetCompositingMode()
        {
            return this.FindControl<ComboBox>("CompositingComboBox")?.SelectedIndex == 1
                ? DrawingCompositingMode.SourceCopy
                : DrawingCompositingMode.SourceOver;
        }

        private DrawImageEffect CreateEffect()
        {
            TextBox? pathInput = this.FindControl<TextBox>("ImagePathTextBox");
            CheckBox? tileCheck = this.FindControl<CheckBox>("TileCheckBox");
            CheckBox? autoHideCheck = this.FindControl<CheckBox>("AutoHideCheckBox");
            Slider? opacitySlider = this.FindControl<Slider>("OpacitySlider");

            return new DrawImageEffect
            {
                ImageLocation = pathInput?.Text ?? string.Empty,
                Placement = GetPlacement(),
                Offset = new SkiaSharp.SKPointI(GetInt("OffsetXInput", 0), GetInt("OffsetYInput", 0)),
                SizeMode = GetSizeMode(),
                Size = new SkiaSharp.SKSizeI(GetInt("SizeWidthInput", 0), GetInt("SizeHeightInput", 0)),
                RotateFlip = GetRotateFlip(),
                Tile = tileCheck?.IsChecked ?? false,
                AutoHide = autoHideCheck?.IsChecked ?? false,
                InterpolationMode = GetInterpolationMode(),
                CompositingMode = GetCompositingMode(),
                Opacity = (int)Math.Round(opacitySlider?.Value ?? 100)
            };
        }

        private void RequestPreview()
        {
            PreviewRequested?.Invoke(this, new EffectEventArgs(
                img => CreateEffect().Apply(img),
                "Image"));
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) RequestPreview();
        }

        private void OnNumericChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            if (IsLoaded) RequestPreview();
        }

        private void OnSliderChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            if (IsLoaded) RequestPreview();
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
                Title = "Select image",
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
                "Applied image"));
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

