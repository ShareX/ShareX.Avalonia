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
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ShareX.Ava.Annotations.Models;
using ShareX.Ava.UI.ViewModels;
using System.ComponentModel;

namespace ShareX.Ava.UI.Views
{
    public partial class EditorView : UserControl
    {
        private const double MinZoom = 0.25;
        private const double MaxZoom = 4.0;
        private const double ZoomStep = 0.1;

        private bool _isPanning;
        private Point _panStart;
        private Vector _panOrigin;

        private Point GetCanvasPosition(PointerEventArgs e, Canvas canvas)
        {
            return e.GetPosition(canvas);
        }

        /// <summary>
        /// Sample pixel color from the rendered canvas (including annotations) at the specified canvas coordinates
        /// </summary>
        private async System.Threading.Tasks.Task<string?> GetPixelColorFromRenderedCanvas(Point canvasPoint)
        {
            if (DataContext is not MainViewModel vm || vm.PreviewImage == null) return null;

            try
            {
                // We need to sample from the RENDERED canvas including all annotations
                var container = this.FindControl<Grid>("CanvasContainer");
                if (container == null || container.Width <= 0 || container.Height <= 0) return null;

                // Render the container (image + annotations) to a bitmap
                var rtb = new global::Avalonia.Media.Imaging.RenderTargetBitmap(
                    new PixelSize((int)container.Width, (int)container.Height), 
                    new Vector(96, 96));
                
                rtb.Render(container);

                // Convert to SKBitmap for pixel access
                using var skBitmap = ShareX.Ava.UI.Helpers.BitmapConversionHelpers.ToSKBitmap(rtb);

                // Convert canvas point to pixel coordinates
                int x = (int)Math.Round(canvasPoint.X);
                int y = (int)Math.Round(canvasPoint.Y);

                System.Diagnostics.Debug.WriteLine($"GetPixelColorFromRenderedCanvas: Canvas point ({canvasPoint.X:F2}, {canvasPoint.Y:F2}) -> Pixel ({x}, {y})");
                System.Diagnostics.Debug.WriteLine($"GetPixelColorFromRenderedCanvas: Rendered size ({skBitmap.Width}, {skBitmap.Height}), Zoom: {vm.Zoom}");

                // Valid ate bounds
                if (x < 0 || y < 0 || x >= skBitmap.Width || y >= skBitmap.Height)
                {
                    System.Diagnostics.Debug.WriteLine($"GetPixelColorFromRenderedCanvas: Out of bounds!");
                    return null;
                }

                // Get pixel color from rendered output
                var skColor = skBitmap.GetPixel(x, y);
                
                // Convert to hex string
                var colorHex = $"#{skColor.Red:X2}{skColor.Green:X2}{skColor.Blue:X2}";
                System.Diagnostics.Debug.WriteLine($"GetPixelColorFromRenderedCanvas: Sampled color {colorHex} at ({x}, {y})");
                
                return colorHex;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetPixelColorFromRenderedCanvas failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sample pixel color from the preview image at the specified canvas coordinates
        /// </summary>
        private string? GetPixelColor(Point canvasPoint)
        {
            if (DataContext is not MainViewModel vm || vm.PreviewImage == null) return null;

            try
            {
                // Canvas point is already in image coordinates (no zoom adjustment needed here
                // because GetCanvasPosition gets position relative to the canvas which is already scaled)
                int x = (int)Math.Round(canvasPoint.X);
                int y = (int)Math.Round(canvasPoint.Y);

                System.Diagnostics.Debug.WriteLine($"GetPixelColor: Canvas point ({canvasPoint.X:F2}, {canvasPoint.Y:F2}) -> Pixel ({x}, {y})");
                System.Diagnostics.Debug.WriteLine($"GetPixelColor: Image size ({vm.PreviewImage.Size.Width}, {vm.PreviewImage.Size.Height}), Zoom: {vm.Zoom}");

                // Validate bounds
                if (x < 0 || y < 0 || x >= vm.PreviewImage.Size.Width || y >= vm.PreviewImage.Size.Height)
                {
                    System.Diagnostics.Debug.WriteLine($"GetPixelColor: Out of bounds!");
                    return null;
                }

                // IMPORTANT: We sample from the BASE image (vm.PreviewImage), not from the rendered canvas
                // This means we get the original pixel color, ignoring any annotations drawn on top.
                // This is the correct behavior for Smart Eraser - it should match the background,
                // not other annotations.

                // Use cached SKBitmap if available, otherwise create one
                // SkiaSharp.SKBitmap? skBitmap = _cachedSkBitmap;
                SkiaSharp.SKBitmap? skBitmap = null;
                bool shouldDispose = false;

                if (skBitmap == null)
                {
                    skBitmap = ShareX.Ava.UI.Helpers.BitmapConversionHelpers.ToSKBitmap(vm.PreviewImage);
                    shouldDispose = true;
                }

                try
                {
                    // Get pixel color
                    var skColor = skBitmap.GetPixel(x, y);
                    
                    // Convert to hex string
                    var colorHex = $"#{skColor.Red:X2}{skColor.Green:X2}{skColor.Blue:X2}";
                    System.Diagnostics.Debug.WriteLine($"GetPixelColor: Sampled color {colorHex} at ({x}, {y})");
                    
                    return colorHex;
                }
                finally
                {
                    if (shouldDispose)
                    {
                        skBitmap?.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetPixelColor failed: {ex.Message}");
                return null;
            }
        }

        private void OnPreviewPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;
            if (!e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;

            var oldZoom = vm.Zoom;
            var direction = e.Delta.Y > 0 ? 1 : -1;
            var newZoom = Math.Clamp(Math.Round((oldZoom + direction * ZoomStep) * 100) / 100, MinZoom, MaxZoom);
            if (Math.Abs(newZoom - oldZoom) < 0.0001) return;

            var scrollViewer = this.FindControl<ScrollViewer>("CanvasScrollViewer");
            if (scrollViewer != null)
            {
                var pointerPosition = e.GetPosition(scrollViewer);
                var offsetBefore = scrollViewer.Offset;
                var logicalPoint = new Vector(
                    (offsetBefore.X + pointerPosition.X) / oldZoom,
                    (offsetBefore.Y + pointerPosition.Y) / oldZoom);

                vm.Zoom = newZoom;

                Dispatcher.UIThread.Post(() =>
                {
                    var targetOffset = new Vector(
                        logicalPoint.X * newZoom - pointerPosition.X,
                        logicalPoint.Y * newZoom - pointerPosition.Y);

                    var maxX = Math.Max(0, scrollViewer.Extent.Width - scrollViewer.Viewport.Width);
                    var maxY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);

                    scrollViewer.Offset = new Vector(
                        Math.Clamp(targetOffset.X, 0, maxX),
                        Math.Clamp(targetOffset.Y, 0, maxY));
                }, DispatcherPriority.Render);
            }
            else
            {
                vm.Zoom = newZoom;
            }

            e.Handled = true;
        }

        private void OnScrollViewerPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not ScrollViewer scrollViewer) return;

            var properties = e.GetCurrentPoint(scrollViewer).Properties;
            if (!properties.IsMiddleButtonPressed) return;

            _isPanning = true;
            _panStart = e.GetPosition(scrollViewer);
            _panOrigin = scrollViewer.Offset;
            scrollViewer.Cursor = new Cursor(StandardCursorType.SizeAll);
            e.Pointer.Capture(scrollViewer);
            e.Handled = true;
        }

        private void OnScrollViewerPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isPanning || sender is not ScrollViewer scrollViewer) return;

            var current = e.GetPosition(scrollViewer);
            var delta = current - _panStart;

            var target = new Vector(
                _panOrigin.X - delta.X,
                _panOrigin.Y - delta.Y);

            var maxX = Math.Max(0, scrollViewer.Extent.Width - scrollViewer.Viewport.Width);
            var maxY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);

            scrollViewer.Offset = new Vector(
                Math.Clamp(target.X, 0, maxX),
                Math.Clamp(target.Y, 0, maxY));

            e.Handled = true;
        }

        private void OnScrollViewerPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (sender is not ScrollViewer scrollViewer) return;

            if (_isPanning)
            {
                _isPanning = false;
                scrollViewer.Cursor = null;
                e.Pointer.Capture(null);
                e.Handled = true;
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Source is TextBox) return;

            if (DataContext is MainViewModel vm)
            {
                 // Global shortcuts like Undo/Redo/Tools
                 // Logic simplified: ViewModel handles commands, we just map keys.
                 // Actually this logic is still fine here.
                 if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
                 {
                     if (e.Key == Key.Z) { vm.UndoCommand.Execute(null); e.Handled=true; return; }
                     else if (e.Key == Key.Y) { vm.RedoCommand.Execute(null); e.Handled=true; return; }
                 }
                 else if (e.KeyModifiers.HasFlag(KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.Z)
                 {
                     vm.RedoCommand.Execute(null); e.Handled=true; return;
                 }
                 
                 if (e.KeyModifiers == KeyModifiers.None)
                 {
                      bool handled = true;
                      switch(e.Key)
                      {
                          case Key.V: vm.SelectToolCommand.Execute(EditorTool.Select); break;
                          case Key.R: vm.SelectToolCommand.Execute(EditorTool.Rectangle); break;
                          case Key.E: vm.SelectToolCommand.Execute(EditorTool.Ellipse); break;
                          case Key.A: vm.SelectToolCommand.Execute(EditorTool.Arrow); break;
                          case Key.L: vm.SelectToolCommand.Execute(EditorTool.Line); break;
                          case Key.P: vm.SelectToolCommand.Execute(EditorTool.Pen); break;
                          case Key.H: vm.SelectToolCommand.Execute(EditorTool.Highlighter); break;
                          case Key.T: vm.SelectToolCommand.Execute(EditorTool.Text); break;
                          case Key.B: vm.SelectToolCommand.Execute(EditorTool.SpeechBalloon); break;
                          case Key.N: vm.SelectToolCommand.Execute(EditorTool.Number); break;
                          case Key.C: vm.SelectToolCommand.Execute(EditorTool.Crop); break;
                          case Key.M: vm.SelectToolCommand.Execute(EditorTool.Magnify); break;
                          case Key.S: vm.SelectToolCommand.Execute(EditorTool.Spotlight); break;
                          case Key.F: vm.ToggleEffectsPanelCommand.Execute(null); break;
                          case Key.Delete: vm.DeleteSelectedCommand.Execute(null); break; 
                          default: handled = false; break;
                      }
                      e.Handled = handled;
                 }
            }
        }

        public EditorView()
        {
            InitializeComponent();
            AddHandler(PointerWheelChangedEvent, OnPreviewPointerWheelChanged, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            if (DataContext is MainViewModel vm)
            {
                vm.SnapshotRequested += GetSnapshot;
                vm.SaveAsRequested += ShowSaveAsDialog;
                vm.CopyRequested += CopyToClipboard;
                vm.ShowErrorDialog += ShowErrorDialog;
            }
        }
        
        // ... Keep File/Result helpers ... 
        
        public async System.Threading.Tasks.Task<global::Avalonia.Media.Imaging.Bitmap?> GetSnapshot()
        {
            var container = this.FindControl<Grid>("CanvasContainer");
            if (container == null) return null;
            try
            {
                var rtb = new global::Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize((int)container.Width, (int)container.Height), new Vector(96, 96));
                rtb.Render(container);
                return rtb;
            }
            catch (Exception) { return null; }
        }

        public async System.Threading.Tasks.Task<string?> ShowSaveAsDialog()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null) return null;
            try
            {
                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save Image",
                    DefaultExtension = "png",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("PNG Image") { Patterns = new[] { "*.png" } },
                        new FilePickerFileType("JPEG Image") { Patterns = new[] { "*.jpg", "*.jpeg" } },
                        new FilePickerFileType("Bitmap Image") { Patterns = new[] { "*.bmp" } }
                    },
                    SuggestedFileName = $"ShareX_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png"
                });
                return file?.Path.AbsolutePath; // Logic might vary
            }
            catch { return null; }
        }
        
        public async System.Threading.Tasks.Task CopyToClipboard(global::Avalonia.Media.Imaging.Bitmap? bitmap = null)
        {
             var snapshot = bitmap ?? await GetSnapshot();
             if (snapshot != null)
             {
                 var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                 if (clipboard != null)
                 {
                     var data = new DataObject();
                     // TODO: Implement proper bitmap copy
                 }
             }
        }

        public async System.Threading.Tasks.Task ShowErrorDialog(string title, string message)
        {
             await Dispatcher.UIThread.InvokeAsync(async () => {
                 var window = new Window
                 {
                     Title = title,
                     Content = new TextBlock { Text = message, Margin = new Thickness(20) },
                     Width = 300,
                     Height = 150,
                     WindowStartupLocation = WindowStartupLocation.CenterOwner
                 };
                 if (VisualRoot is Window root)
                 {
                     await window.ShowDialog(root);
                 }
                 else
                 {
                    window.Show();
                 }
             });
        }

        public void PerformCrop()
        {
             var canvas = this.FindControl<ShareX.Ava.UI.Controls.AnnotationCanvas>("AnnotationCanvas");
             canvas?.PerformCrop();
        }

        private void OnEffectsPanelApplyRequested(object? sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.ApplyEffectCommand.Execute(null);
            }
        }
    }
}
