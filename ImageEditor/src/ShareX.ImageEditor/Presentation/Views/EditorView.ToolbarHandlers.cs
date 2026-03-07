#region License Information (GPL v3)

/*
    ShareX.ImageEditor - The UI-agnostic Editor library for ShareX
    Copyright (c) 2007-2026 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using ShareX.ImageEditor.Core.Annotations;
using ShareX.ImageEditor.Presentation.Controls;
using ShareX.ImageEditor.Presentation.ViewModels;

namespace ShareX.ImageEditor.Presentation.Views
{
    public partial class EditorView : UserControl
    {
        private void OnColorChanged(object? sender, IBrush color)
        {
            if (DataContext is MainViewModel vm && color is SolidColorBrush solidBrush)
            {
                var hexColor = $"#{solidBrush.Color.A:X2}{solidBrush.Color.R:X2}{solidBrush.Color.G:X2}{solidBrush.Color.B:X2}";
                vm.SetColorCommand.Execute(hexColor);
            }
        }

        private void OnFillColorChanged(object? sender, IBrush color)
        {
            if (DataContext is MainViewModel vm && color is SolidColorBrush solidBrush)
            {
                var hexColor = $"#{solidBrush.Color.A:X2}{solidBrush.Color.R:X2}{solidBrush.Color.G:X2}{solidBrush.Color.B:X2}";
                vm.FillColor = hexColor;
            }
        }

        private void OnTextColorChanged(object? sender, IBrush color)
        {
            if (DataContext is MainViewModel vm && color is SolidColorBrush solidBrush)
            {
                var hexColor = $"#{solidBrush.Color.A:X2}{solidBrush.Color.R:X2}{solidBrush.Color.G:X2}{solidBrush.Color.B:X2}";
                vm.TextColor = hexColor;
            }
        }

        private void OnFontSizeChanged(object? sender, float fontSize)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.FontSize = fontSize;

                // ... (rest of logic) ...

                // Apply to selected annotation if any
                var selected = _selectionController.SelectedShape;
                if (selected?.Tag is TextAnnotation textAnn)
                {
                    textAnn.FontSize = fontSize;
                    if (selected is OutlinedTextControl outlinedText)
                    {
                        outlinedText.InvalidateMeasure();
                        outlinedText.InvalidateVisual();
                    }
                }
                else if (selected?.Tag is NumberAnnotation numAnn)
                {
                    numAnn.FontSize = fontSize;

                    // Update the visual - resize grid and update text
                    if (selected is Grid grid)
                    {
                        var radius = Math.Max(12, fontSize * 0.7f);
                        grid.Width = radius * 2;
                        grid.Height = radius * 2;

                        foreach (var child in grid.Children)
                        {
                            if (child is TextBlock textBlock)
                            {
                                textBlock.FontSize = fontSize * 0.6; // Match CreateVisual scaling
                            }
                        }
                    }
                }
                else if (selected?.Tag is SpeechBalloonAnnotation balloonAnn)
                {
                    balloonAnn.FontSize = fontSize;
                    if (selected is SpeechBalloonControl balloonControl)
                    {
                        balloonControl.InvalidateVisual();
                    }
                    _selectionController.UpdateActiveTextEditorProperties();
                }
            }
        }

        private void OnStrengthChanged(object? sender, float strength)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.EffectStrength = strength;

                // Apply to selected annotation if any
                var selected = _selectionController.SelectedShape;
                if (selected?.Tag is BaseEffectAnnotation effectAnn)
                {
                    effectAnn.Amount = strength;
                    // Regenerate effect
                    OnRequestUpdateEffect(selected);
                }
                else if (selected?.Tag is SpotlightAnnotation spotlightAnn)
                {
                    // Map EffectStrength (0-100) to DarkenOpacity (0-255)
                    spotlightAnn.DarkenOpacity = (byte)Math.Clamp(strength / MainViewModel.GetMaxEffectStrength(EditorTool.Spotlight) * 255, 0, 255);

                    if (selected is SpotlightControl spotlightControl)
                    {
                        spotlightControl.InvalidateVisual();
                    }
                }
            }
        }

        private void OnShadowButtonClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                // Toggle state
                vm.ShadowEnabled = !vm.ShadowEnabled;
                var isEnabled = vm.ShadowEnabled;

                // Apply to selected annotation if any
                var selected = _selectionController.SelectedShape;
                if (selected?.Tag is Annotation annotation)
                {
                    annotation.ShadowEnabled = isEnabled;

                    // Update the UI control's Effect property
                    if (selected is Control control)
                    {
                        if (isEnabled)
                        {
                            control.Effect = new Avalonia.Media.DropShadowEffect
                            {
                                OffsetX = 3,
                                OffsetY = 3,
                                BlurRadius = 4,
                                Color = Avalonia.Media.Color.FromArgb(128, 0, 0, 0)
                            };
                        }
                        else
                        {
                            control.Effect = null;
                        }
                    }
                }
            }
        }
        private void OnBoldButtonClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.TextBold = !vm.TextBold;

                var selected = _selectionController.SelectedShape;
                if (selected?.Tag is TextAnnotation textAnn)
                {
                    textAnn.IsBold = vm.TextBold;
                    if (selected is OutlinedTextControl otc)
                    {
                        otc.InvalidateMeasure();
                        otc.InvalidateVisual();
                    }
                }
            }
        }

        private void OnItalicButtonClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.TextItalic = !vm.TextItalic;

                var selected = _selectionController.SelectedShape;
                if (selected?.Tag is TextAnnotation textAnn)
                {
                    textAnn.IsItalic = vm.TextItalic;
                    if (selected is OutlinedTextControl otc)
                    {
                        otc.InvalidateMeasure();
                        otc.InvalidateVisual();
                    }
                }
            }
        }

        private void OnUnderlineButtonClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.TextUnderline = !vm.TextUnderline;

                var selected = _selectionController.SelectedShape;
                if (selected?.Tag is TextAnnotation textAnn)
                {
                    textAnn.IsUnderline = vm.TextUnderline;
                    if (selected is OutlinedTextControl otc)
                    {
                        otc.InvalidateMeasure();
                        otc.InvalidateVisual();
                    }
                }
            }
        }

        private void OnWidthChanged(object? sender, int width)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SetStrokeWidthCommand.Execute(width);
            }
        }

        private void OnZoomChanged(object? sender, double zoom)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.Zoom = zoom;
            }
        }

        private void OnZoomPickerZoomToFitRequested(object? sender, EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.ZoomToFitCommand.Execute(null);
            }
        }

        private void OnSidebarScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            // TODO: Restore sidebar scrollbar overlay logic
        }

        private void OnToolbarScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            // TODO: Restore toolbar scrollbar overlay logic
        }

        // --- Restored from ref\3babd33_EditorView.axaml.cs lines 767-829 ---

        private void ApplySelectedColor(string colorHex)
        {
            var selected = _selectionController.SelectedShape;
            if (selected == null) return;

            // Ensure the annotation model property is updated so changes persist and effects render correctly
            if (selected.Tag is Annotation annotation)
            {
                annotation.StrokeColor = colorHex;
            }

            var brush = new SolidColorBrush(Color.Parse(colorHex));

            switch (selected)
            {
                case Shape shape:
                    shape.Stroke = brush;

                    if (shape is global::Avalonia.Controls.Shapes.Path path)
                    {
                        path.Fill = brush;
                    }
                    else if (selected is SpeechBalloonControl balloon)
                    {
                        balloon.InvalidateVisual();
                    }
                    break;
                case OutlinedTextControl textBox:
                    textBox.InvalidateVisual();
                    break;
                case Grid grid:
                    foreach (var child in grid.Children)
                    {
                        if (child is Ellipse ellipse)
                        {
                            ellipse.Stroke = brush;
                        }
                    }
                    break;
                case SpeechBalloonControl balloonControl:
                    balloonControl.InvalidateVisual();
                    break;
            }

            // ISSUE-LIVE-UPDATE: Update active text editor if present
            _selectionController.UpdateActiveTextEditorProperties();
        }

        private void ApplySelectedFillColor(string colorHex)
        {
            var selected = _selectionController.SelectedShape;
            if (selected == null) return;

            // Ensure the annotation model property is updated so changes persist and effects render correctly
            if (selected.Tag is Annotation annotation)
            {
                annotation.FillColor = colorHex;
            }

            var brush = new SolidColorBrush(Color.Parse(colorHex));

            switch (selected)
            {
                case Shape shape:
                    if (shape.Tag is HighlightAnnotation)
                    {
                        OnRequestUpdateEffect(shape);
                        break;
                    }

                    if (shape is global::Avalonia.Controls.Shapes.Path path)
                    {
                        path.Fill = brush;
                    }
                    else if (shape is global::Avalonia.Controls.Shapes.Rectangle || shape is global::Avalonia.Controls.Shapes.Ellipse)
                    {
                        shape.Fill = brush;
                    }
                    break;
                case OutlinedTextControl textBox:
                    textBox.InvalidateVisual();
                    break;
                case SpeechBalloonControl balloonControl:
                    balloonControl.InvalidateVisual();
                    break;
                case Grid grid:
                    foreach (var child in grid.Children)
                    {
                        if (child is global::Avalonia.Controls.Shapes.Ellipse ellipse)
                        {
                            ellipse.Fill = colorHex == "#00000000" ? Brushes.Transparent : brush;
                        }
                    }
                    break;
            }

            // ISSUE-LIVE-UPDATE: Update active text editor if present
            _selectionController.UpdateActiveTextEditorProperties();
        }

        private void ApplySelectedTextColor(string colorHex)
        {
            var selected = _selectionController.SelectedShape;
            if (selected == null) return;

            if (selected.Tag is TextAnnotation textAnnotation)
            {
                textAnnotation.TextColor = colorHex;
            }
            else if (selected.Tag is SpeechBalloonAnnotation balloon)
            {
                balloon.TextColor = colorHex;
            }
            else if (selected.Tag is NumberAnnotation number)
            {
                number.TextColor = colorHex;
            }

            // Update UI
            if (selected is OutlinedTextControl outText)
            {
                outText.InvalidateVisual();
            }
            else if (selected is SpeechBalloonControl balloonControl)
            {
                balloonControl.InvalidateVisual();
            }
            else if (selected is Grid grid)
            {
                // NumberAnnotation (Step) uses a Grid with a TextBlock
                foreach (var child in grid.Children)
                {
                    if (child is TextBlock textBlock)
                    {
                        textBlock.Foreground = new SolidColorBrush(Avalonia.Media.Color.Parse(colorHex));
                    }
                }
            }

            // ISSUE-LIVE-UPDATE: Update active text editor if present
            _selectionController.UpdateActiveTextEditorProperties();
        }

        private void ApplySelectedStrokeWidth(int width)
        {
            var selected = _selectionController.SelectedShape;
            if (selected == null) return;

            if (selected.Tag is Annotation annotation)
            {
                annotation.StrokeWidth = width;
            }

            switch (selected)
            {
                case Shape shape:
                    shape.StrokeThickness = width;
                    break;
                case OutlinedTextControl textBox:
                    textBox.InvalidateMeasure();
                    textBox.InvalidateVisual();
                    break;
                case Grid grid:
                    foreach (var child in grid.Children)
                    {
                        if (child is Ellipse ellipse)
                        {
                            ellipse.StrokeThickness = Math.Max(1, width);
                        }
                    }
                    break;
                case SpeechBalloonControl balloon:
                    if (balloon.Annotation != null)
                    {
                        balloon.Annotation.StrokeWidth = width;
                        balloon.InvalidateVisual();
                    }
                    break;
            }
        }

        private static Color ApplyHighlightAlpha(Color baseColor)
        {
            return Color.FromArgb(0x55, baseColor.R, baseColor.G, baseColor.B);
        }

    }
}
