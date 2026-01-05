#region License Information (GPL v3)

/*
    ShareX.Ava - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2025 ShareX Team

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
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ShareX.Editor;
using ShareX.Editor.Annotations;
using ShareX.Ava.UI.Controls;
using ShareX.Ava.UI.ViewModels;
using ShareX.Ava.UI.Helpers;
using SkiaSharp;
using System.ComponentModel;

namespace ShareX.Ava.UI.Views;

/// <summary>
/// Editor view using EditorCore for annotation rendering.
/// This is a simplified view that delegates all annotation logic to EditorCanvas/EditorCore.
/// </summary>
public partial class EditorView : UserControl
{
    private const double MinZoom = 0.25;
    private const double MaxZoom = 4.0;
    private const double ZoomStep = 0.1;

    private bool _isPanning;
    private Point _panStart;
    private Vector _panOrigin;

    public EditorView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is MainViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            
            // Wire up EditorCanvas with ViewModel
            SyncEditorWithViewModel(vm);
        }
    }


    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (DataContext is MainViewModel vm)
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not MainViewModel vm) return;

        switch (e.PropertyName)
        {
            case nameof(MainViewModel.PreviewImage):
                LoadImageToEditor(vm);
                break;
            case nameof(MainViewModel.ActiveTool):
                if (EditorCanvas != null)
                    EditorCanvas.Editor.ActiveTool = vm.ActiveTool;
                break;
            case nameof(MainViewModel.SelectedColor):
                if (EditorCanvas != null)
                    EditorCanvas.Editor.StrokeColor = vm.SelectedColor;
                break;
            case nameof(MainViewModel.StrokeWidth):
                if (EditorCanvas != null)
                    EditorCanvas.Editor.StrokeWidth = vm.StrokeWidth;
                break;
        }
    }

    private void SyncEditorWithViewModel(MainViewModel vm)
    {
        if (EditorCanvas == null) return;
        
        EditorCanvas.Editor.ActiveTool = vm.ActiveTool;
        EditorCanvas.Editor.StrokeColor = vm.SelectedColor;
        EditorCanvas.Editor.StrokeWidth = vm.StrokeWidth;
        
        EditorCanvas.Editor.StatusTextChanged += text => vm.StatusText = text;
        
        LoadImageToEditor(vm);
    }

    private void LoadImageToEditor(MainViewModel vm)
    {
        if (EditorCanvas == null || vm.PreviewImage == null) return;
        
        var skBitmap = vm.PreviewImage.ToSKBitmap();
        if (skBitmap != null)
        {
            EditorCanvas.LoadImage(skBitmap);
        }
    }

    #region Zoom/Pan

    private void OnPreviewPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            double zoomDelta = e.Delta.Y > 0 ? ZoomStep : -ZoomStep;
            double newZoom = Math.Clamp(vm.Zoom + zoomDelta, MinZoom, MaxZoom);
            vm.Zoom = newZoom;
            e.Handled = true;
        }
    }

    private void OnScrollViewerPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var scrollViewer = this.FindControl<ScrollViewer>("CanvasScrollViewer");
        if (scrollViewer == null) return;

        var props = e.GetCurrentPoint(scrollViewer).Properties;
        if (props.IsMiddleButtonPressed)
        {
            _isPanning = true;
            _panStart = e.GetPosition(scrollViewer);
            _panOrigin = scrollViewer.Offset;
            e.Pointer.Capture(scrollViewer);
            e.Handled = true;
        }
    }

    private void OnScrollViewerPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPanning) return;

        var scrollViewer = this.FindControl<ScrollViewer>("CanvasScrollViewer");
        if (scrollViewer == null) return;

        var currentPoint = e.GetPosition(scrollViewer);
        var delta = _panStart - currentPoint;
        scrollViewer.Offset = _panOrigin + delta;
    }

    private void OnScrollViewerPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
        }
    }

    #endregion

    #region Keyboard

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        // Keyboard shortcuts
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            switch (e.Key)
            {
                case Key.Z:
                    EditorCanvas?.Editor.Undo();
                    e.Handled = true;
                    break;
                case Key.Y:
                    EditorCanvas?.Editor.Redo();
                    e.Handled = true;
                    break;
                case Key.C:
                    CopyToClipboard();
                    e.Handled = true;
                    break;
                case Key.S:
                    ShowSaveAsDialog();
                    e.Handled = true;
                    break;
            }
        }
        else
        {
            switch (e.Key)
            {
                case Key.Delete:
                    EditorCanvas?.Editor.DeleteSelected();
                    e.Handled = true;
                    break;
                case Key.Escape:
                    EditorCanvas?.Editor.Deselect();
                    e.Handled = true;
                    break;
                case Key.R:
                    vm.ActiveTool = EditorTool.Rectangle;
                    e.Handled = true;
                    break;
                case Key.E:
                    vm.ActiveTool = EditorTool.Ellipse;
                    e.Handled = true;
                    break;
                case Key.L:
                    vm.ActiveTool = EditorTool.Line;
                    e.Handled = true;
                    break;
                case Key.A:
                    vm.ActiveTool = EditorTool.Arrow;
                    e.Handled = true;
                    break;
                case Key.T:
                    vm.ActiveTool = EditorTool.Text;
                    e.Handled = true;
                    break;
                case Key.P:
                    vm.ActiveTool = EditorTool.Pen;
                    e.Handled = true;
                    break;
                case Key.V:
                    vm.ActiveTool = EditorTool.Select;
                    e.Handled = true;
                    break;
            }
        }
    }

    #endregion

    #region Copy/Save

    private async void CopyToClipboard()
    {
        var snapshot = EditorCanvas?.GetSnapshot();
        if (snapshot == null) return;

        try
        {
            var avaloniaBitmap = snapshot.ToAvaloniaBitmap();
            if (avaloniaBitmap != null)
            {
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                // TODO: Implement clipboard image copy
            }
        }
        finally
        {
            snapshot.Dispose();
        }
    }

    private async void ShowSaveAsDialog()
    {
        var snapshot = EditorCanvas?.GetSnapshot();
        if (snapshot == null) return;

        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Image",
                SuggestedFileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("PNG Image") { Patterns = new[] { "*.png" } },
                    new FilePickerFileType("JPEG Image") { Patterns = new[] { "*.jpg", "*.jpeg" } }
                }
            });

            if (file != null)
            {
                using var stream = await file.OpenWriteAsync();
                var format = file.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                             file.Name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                    ? SKEncodedImageFormat.Jpeg
                    : SKEncodedImageFormat.Png;
                
                using var image = SKImage.FromBitmap(snapshot);
                using var data = image.Encode(format, 95);
                data.SaveTo(stream);
                
                if (DataContext is MainViewModel vm)
                    vm.StatusText = $"Saved to {file.Name}";
            }
        }
        finally
        {
            snapshot.Dispose();
        }
    }

    #endregion

    private void OnEffectsPanelApplyRequested(object? sender, EventArgs e)
    {
        // Effects panel apply - reload image to EditorCanvas
        if (DataContext is MainViewModel vm)
        {
            LoadImageToEditor(vm);
        }
    }

    #region Public Methods for MainWindow

    /// <summary>
    /// Perform crop operation using the current crop annotation
    /// </summary>
    public void PerformCrop()
    {
        EditorCanvas?.Editor.PerformCrop();
        
        // Reload the cropped image to ViewModel
        if (DataContext is MainViewModel vm && EditorCanvas?.Editor.SourceImage != null)
        {
            vm.PreviewImage = EditorCanvas.Editor.SourceImage.ToAvaloniaBitmap();
        }
    }

    /// <summary>
    /// Clear all annotations
    /// </summary>
    public void ClearAllAnnotations()
    {
        EditorCanvas?.Editor.ClearAll();
    }

    /// <summary>
    /// Undo last action
    /// </summary>
    public void PerformUndo()
    {
        EditorCanvas?.Editor.Undo();
    }

    /// <summary>
    /// Redo last undone action
    /// </summary>
    public void PerformRedo()
    {
        EditorCanvas?.Editor.Redo();
    }

    /// <summary>
    /// Delete selected annotation
    /// </summary>
    public void PerformDelete()
    {
        EditorCanvas?.Editor.DeleteSelected();
    }

    /// <summary>
    /// Get snapshot of the canvas
    /// </summary>
    public SKBitmap? GetSnapshot()
    {
        return EditorCanvas?.GetSnapshot();
    }

    #endregion
}
