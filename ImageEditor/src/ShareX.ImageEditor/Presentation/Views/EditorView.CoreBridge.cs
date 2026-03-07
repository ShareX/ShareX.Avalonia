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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ShareX.ImageEditor.Core.Annotations;
using ShareX.ImageEditor.Presentation.Rendering;
using ShareX.ImageEditor.Hosting;
using ShareX.ImageEditor.Presentation.ViewModels;
using SkiaSharp;

namespace ShareX.ImageEditor.Presentation.Views
{
    public partial class EditorView : UserControl
    {
        private void UpdateViewModelHistoryState(MainViewModel vm)
        {
            vm.UpdateCoreHistoryState(_editorCore.CanUndo, _editorCore.CanRedo);
        }

        private void UpdateViewModelMetadata(MainViewModel vm)
        {
            // Initial sync of metadata if needed
            UpdateViewModelHistoryState(vm);
        }

        private void RenderCore()
        {
            if (_canvasControl == null) return;
            // Hybrid rendering: Render only background + raster effects from Core
            // Vector annotations are handled by Avalonia Canvas
            _canvasControl.Draw(canvas => _editorCore.Render(canvas));
        }

        public SkiaSharp.SKBitmap? GetSource()
        {
            if (_editorCore.SourceImage != null)
            {
                return _editorCore.SourceImage.Copy();
            }

            return null;
        }

        public SkiaSharp.SKBitmap? GetSnapshot()
        {
            if (_editorCore.SourceImage == null) return null;

            var canvasContainer = this.FindControl<Grid>("CanvasContainer");
            var overlayCanvas = this.FindControl<Canvas>("OverlayCanvas");
            if (canvasContainer == null) return _editorCore.GetSnapshot();

            // Hide OverlayCanvas (selection handles, crop overlay) during capture
            bool overlayWasVisible = overlayCanvas?.IsVisible ?? false;
            if (overlayCanvas != null) overlayCanvas.IsVisible = false;

            try
            {
                int width = _editorCore.SourceImage.Width;
                int height = _editorCore.SourceImage.Height;

                // Force layout at native resolution (un-zoomed)
                canvasContainer.Measure(new Size(width, height));
                canvasContainer.Arrange(new Rect(0, 0, width, height));

                // Render Avalonia visual tree to bitmap
                var rtb = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
                rtb.Render(canvasContainer);

                // Convert Avalonia RenderTargetBitmap → SKBitmap
                using var stream = new System.IO.MemoryStream();
                rtb.Save(stream);
                stream.Position = 0;
                var skBitmap = SkiaSharp.SKBitmap.Decode(stream);

                return skBitmap;
            }
            finally
            {
                // Restore OverlayCanvas visibility
                if (overlayCanvas != null) overlayCanvas.IsVisible = overlayWasVisible;

                // Re-trigger layout with current zoom
                canvasContainer.InvalidateMeasure();
                canvasContainer.InvalidateArrange();
            }
        }

        public Task<Bitmap?> RenderSnapshot()
        {
            var skBitmap = GetSnapshot();
            var snapshot = skBitmap != null ? BitmapConversionHelpers.ToAvaloniBitmap(skBitmap) : null;
            return Task.FromResult<Bitmap?>(snapshot);
        }

        // This is called by SelectionController/InputController via event when an effect logic needs update
        // We replicate the UpdateEffectVisual logic here or expose it
        private void OnRequestUpdateEffect(Control shape)
        {
            if (shape == null || shape.Tag is not BaseEffectAnnotation annotation) return;
            if (DataContext is not MainViewModel vm || vm.PreviewImage == null) return;

            // Logic to update effect bitmap
            try
            {
                double left = Canvas.GetLeft(shape);
                double top = Canvas.GetTop(shape);
                // Use explicit Width/Height first, fallback to Bounds, then annotation bounds
                double width = shape.Width;
                double height = shape.Height;
                if (double.IsNaN(width) || width <= 0) width = shape.Bounds.Width;
                if (double.IsNaN(height) || height <= 0) height = shape.Bounds.Height;
                // Final fallback to annotation's own bounds
                if (width <= 0 || height <= 0)
                {
                    var bounds = annotation.GetBounds();
                    width = bounds.Width;
                    height = bounds.Height;
                }
                if (width <= 0 || height <= 0) return;

                // Map to SKPoint
                annotation.StartPoint = new SKPoint((float)left, (float)top);
                annotation.EndPoint = new SKPoint((float)(left + width), (float)(top + height));

                // We don't have the cached bitmap here, create fresh or pass from controller?
                // Original logic cached it. InputController caches it.
                // This handler is for "OnPointerReleased" from SelectionController (dragging an existing effect).
                // SelectionController doesn't have the cached bitmap.
                using var skBitmap = BitmapConversionHelpers.ToSKBitmap(vm.PreviewImage);
                annotation.UpdateEffect(skBitmap);

                if (annotation.EffectBitmap != null && shape is Shape shapeControl)
                {
                    var avaloniaBitmap = BitmapConversionHelpers.ToAvaloniBitmap(annotation.EffectBitmap);
                    shapeControl.Fill = new ImageBrush(avaloniaBitmap)
                    {
                        Stretch = Stretch.Fill,
                        SourceRect = new RelativeRect(0, 0, 1, 1, RelativeUnit.Relative)
                    };
                }
            }
            catch (Exception ex)
            {
                EditorServices.ReportWarning(nameof(EditorView), "Failed to update effect annotation preview.", ex);
            }
        }

        private void OnAnnotationsRestored()
        {
            // Fully rebuild annotation layer from Core state
            // 1. Clear current UI annotations
            var canvas = this.FindControl<Canvas>("AnnotationCanvas");
            if (canvas == null) return;

            // Dispose old annotations before clearing
            foreach (var child in canvas.Children)
            {
                if (child is Control control)
                {
                    (control.Tag as IDisposable)?.Dispose();
                }
            }

            canvas.Children.Clear();
            _selectionController.ClearSelection();

            // 2. Re-create UI for all vector annotations in Core
            foreach (var annotation in _editorCore.Annotations)
            {
                // Only create UI for vector annotations (Hybrid model)
                Control? shape = CreateControlForAnnotation(annotation);
                if (shape != null)
                {
                    canvas.Children.Add(shape);

                    // XIP0039 Guardrail 4: Rehydrate the arrow endpoint cache so that
                    // endpoint drag handles work correctly after undo/redo, paste, and duplicate.
                    // Previously the cache was only populated during the draw flow, causing
                    // handle and hit-test degradation on restored annotations.
                    if (annotation is ArrowAnnotation arrow)
                    {
                        var start = new Point(arrow.StartPoint.X, arrow.StartPoint.Y);
                        var end = new Point(arrow.EndPoint.X, arrow.EndPoint.Y);
                        _selectionController.RegisterArrowEndpoint(shape, start, end);
                    }
                }
            }

            RenderCore();

            // 3. Validate state synchronization (ISSUE-001 mitigation)
            ValidateAnnotationSync();

            // Update HasAnnotations state
            UpdateHasAnnotationsState();
        }

        private void OnAnnotationOrderChanged()
        {
            var canvas = this.FindControl<Canvas>("AnnotationCanvas");
            if (canvas == null) return;

            var children = canvas.Children.OfType<Control>().ToList();
            if (children.Count == 0) return;

            var coreAnnotations = _editorCore.Annotations;
            // Create a lookup for O(1) index access
            var indexLookup = new Dictionary<Annotation, int>();
            for (int i = 0; i < coreAnnotations.Count; i++)
            {
                indexLookup[coreAnnotations[i]] = i;
            }

            children.Sort((a, b) =>
            {
                int indexA = int.MaxValue;
                if (a.Tag is Annotation tagA && indexLookup.TryGetValue(tagA, out var ia))
                {
                    indexA = ia;
                }

                int indexB = int.MaxValue;
                if (b.Tag is Annotation tagB && indexLookup.TryGetValue(tagB, out var ib))
                {
                    indexB = ib;
                }

                return indexA.CompareTo(indexB);
            });

            canvas.Children.Clear();
            canvas.Children.AddRange(children);
        }

        /// <summary>
        /// Updates the ViewModel's HasAnnotations property based on current annotation count.
        /// </summary>
        private void UpdateHasAnnotationsState()
        {
            if (DataContext is MainViewModel vm)
            {
                var canvas = this.FindControl<Canvas>("AnnotationCanvas");
                int coreAnnotationCount = _editorCore.Annotations.Count;
                int canvasChildCount = canvas?.Children.Count ?? 0;
                vm.HasAnnotations = coreAnnotationCount > 0 || canvasChildCount > 0;
            }
        }

        /// <summary>
        /// Sample pixel color from the rendered canvas (including annotations) at the specified canvas coordinates
        /// </summary>
        internal async System.Threading.Tasks.Task<string?> GetPixelColorFromRenderedCanvas(Point canvasPoint)
        {
            if (DataContext is not MainViewModel vm || vm.PreviewImage == null) return null;

            try
            {
                var container = this.FindControl<Grid>("CanvasContainer");
                if (container == null || container.Width <= 0 || container.Height <= 0) return null;

                var rtb = new global::Avalonia.Media.Imaging.RenderTargetBitmap(
                    new PixelSize((int)container.Width, (int)container.Height),
                    new Vector(96, 96));

                rtb.Render(container);

                using var skBitmap = BitmapConversionHelpers.ToSKBitmap(rtb);

                int x = (int)Math.Round(canvasPoint.X);
                int y = (int)Math.Round(canvasPoint.Y);

                if (x < 0 || y < 0 || x >= skBitmap.Width || y >= skBitmap.Height)
                    return null;

                var skColor = skBitmap.GetPixel(x, y);
                return $"#{skColor.Red:X2}{skColor.Green:X2}{skColor.Blue:X2}";
            }
            catch (Exception ex)
            {
                EditorServices.ReportWarning(nameof(EditorView), "Failed to sample pixel color from rendered canvas.", ex);
                return null;
            }
        }
    }
}
