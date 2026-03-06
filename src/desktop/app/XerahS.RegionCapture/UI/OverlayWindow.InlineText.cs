#region License Information (GPL v3)

/*
    XerahS - The Avalonia UI implementation of ShareX
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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using ShareX.ImageEditor.Core.Annotations;
using SkiaSharp;

namespace XerahS.RegionCapture.UI;

public partial class OverlayWindow
{
    // Inline text editing state
    private TextBox? _inlineTextBox;
    private Annotation? _editingAnnotation;

    /// <summary>
    /// Handles EditorCore's EditAnnotationRequested event for Text and SpeechBalloon tools.
    /// Shows an inline TextBox at the annotation position.
    /// </summary>
    private void OnEditAnnotationRequested(Annotation annotation)
    {
        if (_annotationCanvas == null) return;
        if (annotation is not TextAnnotation && annotation is not SpeechBalloonAnnotation) return;

        _editingAnnotation = annotation;

        var bounds = annotation.GetBounds();
        float fontSize = annotation is TextAnnotation t ? t.FontSize :
                         annotation is SpeechBalloonAnnotation s ? s.FontSize : 16;

        _inlineTextBox = new TextBox
        {
            Width = Math.Max(200, bounds.Width),
            Height = Math.Max(40, bounds.Height),
            FontSize = fontSize,
            Foreground = new SolidColorBrush(Color.Parse(annotation.StrokeColor)),
            Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
            BorderBrush = new SolidColorBrush(Colors.DodgerBlue),
            BorderThickness = new Thickness(2),
            AcceptsReturn = false,
            TextWrapping = TextWrapping.Wrap,
            Padding = new Thickness(4),
            Watermark = "Type text here..."
        };

        Canvas.SetLeft(_inlineTextBox, bounds.Left);
        Canvas.SetTop(_inlineTextBox, bounds.Top);

        _inlineTextBox.KeyDown += OnInlineTextBoxKeyDown;
        _inlineTextBox.LostFocus += OnInlineTextBoxLostFocus;

        _annotationCanvas.Children.Add(_inlineTextBox);

        // Rebuild canvas first to show underlying annotations, then focus the text box
        RebuildAnnotationCanvas();

        Dispatcher.UIThread.Post(() => _inlineTextBox?.Focus(), DispatcherPriority.Input);
    }

    private void OnInlineTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CommitInlineText();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CancelInlineText();
            e.Handled = true;
        }
    }

    private void OnInlineTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        // Don't auto-commit if already cleaned up
        if (_inlineTextBox != null && _editingAnnotation != null)
        {
            CommitInlineText();
        }
    }

    private void CommitInlineText()
    {
        if (_inlineTextBox == null || _editingAnnotation == null) return;

        var text = _inlineTextBox.Text ?? "";

        // Persist final bounds from the inline editor so finalized text remains visible.
        var left = Canvas.GetLeft(_inlineTextBox);
        if (double.IsNaN(left))
        {
            left = _editingAnnotation.StartPoint.X;
        }

        var top = Canvas.GetTop(_inlineTextBox);
        if (double.IsNaN(top))
        {
            top = _editingAnnotation.StartPoint.Y;
        }

        var width = _inlineTextBox.Bounds.Width > 0 ? _inlineTextBox.Bounds.Width : _inlineTextBox.Width;
        if (double.IsNaN(width) || width <= 0)
        {
            width = 10;
        }

        var height = _inlineTextBox.Bounds.Height > 0 ? _inlineTextBox.Bounds.Height : _inlineTextBox.Height;
        if (double.IsNaN(height) || height <= 0)
        {
            height = 10;
        }

        _editingAnnotation.StartPoint = new SKPoint((float)left, (float)top);
        _editingAnnotation.EndPoint = new SKPoint((float)(left + width), (float)(top + height));

        if (_editingAnnotation is TextAnnotation textAnn)
            textAnn.Text = text;
        else if (_editingAnnotation is SpeechBalloonAnnotation balloonAnn)
            balloonAnn.Text = text;

        CleanupInlineTextBox();
        RebuildAnnotationCanvas();
    }

    private void CancelInlineText()
    {
        if (_editingAnnotation != null)
        {
            // Remove the annotation since user cancelled text input
            _viewModel.EditorCore.RemoveAnnotation(_editingAnnotation);
            _viewModel.HasAnnotations = _viewModel.EditorCore.Annotations.Count > 0;
        }

        CleanupInlineTextBox();
        RebuildAnnotationCanvas();
    }

    private void CleanupInlineTextBox()
    {
        if (_inlineTextBox != null)
        {
            _inlineTextBox.KeyDown -= OnInlineTextBoxKeyDown;
            _inlineTextBox.LostFocus -= OnInlineTextBoxLostFocus;
            _annotationCanvas?.Children.Remove(_inlineTextBox);
            _inlineTextBox = null;
        }
        _editingAnnotation = null;
    }
}
