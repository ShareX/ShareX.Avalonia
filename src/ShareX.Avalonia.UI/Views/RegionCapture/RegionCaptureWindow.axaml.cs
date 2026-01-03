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

using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ShareX.Ava.Platform.Abstractions;
using DrawingRectangle = System.Drawing.Rectangle;
using PathShape = Avalonia.Controls.Shapes.Path;
using Point = Avalonia.Point;
using RectangleShape = Avalonia.Controls.Shapes.Rectangle;

namespace ShareX.Ava.UI.Views.RegionCapture
{
    public partial class RegionCaptureWindow : Window
    {
        private Point _startPoint;
        private bool _isSelecting;
        private double _renderScale = 1.0;
        private double _logicalSurfaceWidth;
        private double _logicalSurfaceHeight;
        private DrawingRectangle _virtualScreenBounds = DrawingRectangle.Empty;

        private readonly TaskCompletionSource<DrawingRectangle> _tcs;
        
        public RegionCaptureWindow()
        {
            InitializeComponent();
            _tcs = new TaskCompletionSource<DrawingRectangle>();
            
            KeyDown += (_, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    _tcs.TrySetResult(DrawingRectangle.Empty);
                    Close();
                }
            };
        }

        public Task<DrawingRectangle> GetResultAsync() => _tcs.Task;

        public async Task SetBackgroundScreenshot()
        {
            if (PlatformServices.IsInitialized)
            {
                var screenshot = await PlatformServices.ScreenCapture.CaptureFullScreenAsync();
                if (screenshot != null)
                {
                    using var memoryStream = new System.IO.MemoryStream();
                    screenshot.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                    memoryStream.Position = 0;
                    
                    var avaloniaBitmap = new Bitmap(memoryStream);
                    
                    var backgroundImage = this.FindControl<Image>("BackgroundImage");
                    if (backgroundImage != null)
                    {
                        backgroundImage.Source = avaloniaBitmap;
                    }
                    
                    screenshot.Dispose();
                }
            }
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            
            CanResize = false;

            _virtualScreenBounds = ResolveVirtualScreenBounds();
            _renderScale = GetRenderScale();

            ApplySurfaceSizing();

            ShareX.Ava.Common.DebugHelper.WriteLine($"RegionCapture: Virtual screen: X={_virtualScreenBounds.X}, Y={_virtualScreenBounds.Y}, W={_virtualScreenBounds.Width}, H={_virtualScreenBounds.Height}");
            ShareX.Ava.Common.DebugHelper.WriteLine($"RegionCapture: Window logical size: {Width}x{Height}");
            ShareX.Ava.Common.DebugHelper.WriteLine($"RegionCapture: RenderScaling: {_renderScale}");

            InitializeFullScreenDarkening();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private double GetRenderScale() => VisualRoot?.RenderScaling ?? RenderScaling;

        private DrawingRectangle ResolveVirtualScreenBounds()
        {
            if (PlatformServices.IsInitialized)
            {
                return PlatformServices.Screen.GetVirtualScreenBounds();
            }

            if (Screens.ScreenCount > 0)
            {
                var minX = int.MaxValue;
                var minY = int.MaxValue;
                var maxX = int.MinValue;
                var maxY = int.MinValue;

                foreach (var screen in Screens.All)
                {
                    minX = Math.Min(minX, screen.Bounds.X);
                    minY = Math.Min(minY, screen.Bounds.Y);
                    maxX = Math.Max(maxX, screen.Bounds.Right);
                    maxY = Math.Max(maxY, screen.Bounds.Bottom);
                }

                return new DrawingRectangle(minX, minY, maxX - minX, maxY - minY);
            }

            return DrawingRectangle.Empty;
        }

        private void ApplySurfaceSizing()
        {
            if (_virtualScreenBounds.Width <= 0 || _virtualScreenBounds.Height <= 0)
            {
                var fallbackWidth = 1920;
                var fallbackHeight = 1080;

                if (Screens.ScreenCount > 0)
                {
                    var primary = Screens.Primary ?? Screens.All[0];
                    fallbackWidth = primary.Bounds.Width;
                    fallbackHeight = primary.Bounds.Height;
                }

                _virtualScreenBounds = new DrawingRectangle(0, 0, fallbackWidth, fallbackHeight);
            }

            Position = new PixelPoint(_virtualScreenBounds.X, _virtualScreenBounds.Y);

            _logicalSurfaceWidth = _virtualScreenBounds.Width / _renderScale;
            _logicalSurfaceHeight = _virtualScreenBounds.Height / _renderScale;

            Width = _logicalSurfaceWidth;
            Height = _logicalSurfaceHeight;

            var canvas = this.FindControl<Canvas>("SelectionCanvas");
            var backgroundImage = this.FindControl<Image>("BackgroundImage");

            if (canvas != null)
            {
                canvas.Width = _logicalSurfaceWidth;
                canvas.Height = _logicalSurfaceHeight;
            }

            if (backgroundImage != null)
            {
                backgroundImage.Width = _logicalSurfaceWidth;
                backgroundImage.Height = _logicalSurfaceHeight;
            }
        }

        private void InitializeFullScreenDarkening()
        {
            var overlay = this.FindControl<PathShape>("DarkeningOverlay");
            if (overlay == null) return;

            var darkeningGeometry = new PathGeometry();
            var fullScreenFigure = new PathFigure 
            { 
                StartPoint = new Point(0, 0), 
                IsClosed = true 
            };
            
            fullScreenFigure.Segments.Add(new LineSegment { Point = new Point(_logicalSurfaceWidth, 0) });
            fullScreenFigure.Segments.Add(new LineSegment { Point = new Point(_logicalSurfaceWidth, _logicalSurfaceHeight) });
            fullScreenFigure.Segments.Add(new LineSegment { Point = new Point(0, _logicalSurfaceHeight) });
            darkeningGeometry.Figures.Add(fullScreenFigure);

            overlay.Data = darkeningGeometry;
            overlay.IsVisible = true;
        }

        private void UpdateDarkeningOverlay(double logicalSelX, double logicalSelY, double logicalSelWidth, double logicalSelHeight)
        {
            var overlay = this.FindControl<PathShape>("DarkeningOverlay");
            if (overlay == null) return;

            var darkeningGeometry = new PathGeometry { FillRule = FillRule.EvenOdd };

            var outerFigure = new PathFigure 
            { 
                StartPoint = new Point(0, 0), 
                IsClosed = true 
            };
            outerFigure.Segments.Add(new LineSegment { Point = new Point(_logicalSurfaceWidth, 0) });
            outerFigure.Segments.Add(new LineSegment { Point = new Point(_logicalSurfaceWidth, _logicalSurfaceHeight) });
            outerFigure.Segments.Add(new LineSegment { Point = new Point(0, _logicalSurfaceHeight) });
            darkeningGeometry.Figures.Add(outerFigure);

            var innerFigure = new PathFigure 
            { 
                StartPoint = new Point(logicalSelX, logicalSelY), 
                IsClosed = true 
            };
            innerFigure.Segments.Add(new LineSegment { Point = new Point(logicalSelX + logicalSelWidth, logicalSelY) });
            innerFigure.Segments.Add(new LineSegment { Point = new Point(logicalSelX + logicalSelWidth, logicalSelY + logicalSelHeight) });
            innerFigure.Segments.Add(new LineSegment { Point = new Point(logicalSelX, logicalSelY + logicalSelHeight) });
            darkeningGeometry.Figures.Add(innerFigure);

            overlay.Data = darkeningGeometry;
        }

        private void CancelSelection()
        {
            var border = this.FindControl<RectangleShape>("SelectionBorder");
            if (border != null)
                border.IsVisible = false;

            var borderInner = this.FindControl<RectangleShape>("SelectionBorderInner");
            if (borderInner != null)
                borderInner.IsVisible = false;

            var infoText = this.FindControl<TextBlock>("InfoText");
            if (infoText != null)
                infoText.IsVisible = false;

            InitializeFullScreenDarkening();
            _isSelecting = false;
        }

        private void OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            var point = e.GetPosition(this);
            
            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                if (_isSelecting)
                {
                    CancelSelection();
                    e.Handled = true;
                }
                else
                {
                    _tcs.TrySetResult(DrawingRectangle.Empty);
                    Close();
                    e.Handled = true;
                }
                return;
            }
            
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _startPoint = ClampToSurface(point);
                _isSelecting = true;
                
                var border = this.FindControl<RectangleShape>("SelectionBorder");
                if (border != null)
                {
                    border.IsVisible = true;
                    Canvas.SetLeft(border, _startPoint.X);
                    Canvas.SetTop(border, _startPoint.Y);
                    border.Width = 0;
                    border.Height = 0;
                }

                var borderInner = this.FindControl<RectangleShape>("SelectionBorderInner");
                if (borderInner != null)
                {
                    borderInner.IsVisible = true;
                    Canvas.SetLeft(borderInner, _startPoint.X);
                    Canvas.SetTop(borderInner, _startPoint.Y);
                    borderInner.Width = 0;
                    borderInner.Height = 0;
                }

                UpdateDarkeningOverlay(_startPoint.X, _startPoint.Y, 0, 0);
            }
        }

        private void OnPointerMoved(object sender, PointerEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed && _isSelecting)
            {
                CancelSelection();
                e.Handled = true;
                return;
            }

            if (!_isSelecting) return;

            var currentPoint = ClampToSurface(e.GetPosition(this));
            var border = this.FindControl<RectangleShape>("SelectionBorder");
            var borderInner = this.FindControl<RectangleShape>("SelectionBorderInner");
            var infoText = this.FindControl<TextBlock>("InfoText");

            if (border != null)
            {
                var x = Math.Min(_startPoint.X, currentPoint.X);
                var y = Math.Min(_startPoint.Y, currentPoint.Y);
                var width = Math.Abs(_startPoint.X - currentPoint.X);
                var height = Math.Abs(_startPoint.Y - currentPoint.Y);

                Canvas.SetLeft(border, x);
                Canvas.SetTop(border, y);
                border.Width = width;
                border.Height = height;

                if (borderInner != null)
                {
                    Canvas.SetLeft(borderInner, x);
                    Canvas.SetTop(borderInner, y);
                    borderInner.Width = width;
                    borderInner.Height = height;
                }

                UpdateDarkeningOverlay(x, y, width, height);
                
                if (infoText != null)
                {
                    infoText.IsVisible = true;

                    var physicalRect = LogicalRectToPhysical(x, y, width, height);
                    infoText.Text = $"X: {physicalRect.X} Y: {physicalRect.Y} W: {physicalRect.Width} H: {physicalRect.Height}";
                    
                    Canvas.SetLeft(infoText, x);
                    
                    double labelHeight = 30;
                    double topPadding = 5;
                    double labelY = y - labelHeight - topPadding;
                    
                    if (labelY < 5)
                        labelY = 5;
                    
                    Canvas.SetTop(infoText, labelY);
                }
            }
        }

        private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (_isSelecting)
            {
                _isSelecting = false;
                var currentPoint = ClampToSurface(e.GetPosition(this));
                
                var x = Math.Min(_startPoint.X, currentPoint.X);
                var y = Math.Min(_startPoint.Y, currentPoint.Y);
                var width = Math.Abs(_startPoint.X - currentPoint.X);
                var height = Math.Abs(_startPoint.Y - currentPoint.Y);

                var resultRect = LogicalRectToPhysical(x, y, width, height);

                ShareX.Ava.Common.DebugHelper.WriteLine($"RegionCapture: Selection result: ({resultRect.X}, {resultRect.Y}, {resultRect.Width}x{resultRect.Height})");
                
                _tcs.TrySetResult(resultRect);
                Close();
            }
        }

        private Point ClampToSurface(Point logicalPoint)
        {
            var clampedX = Math.Clamp(logicalPoint.X, 0, _logicalSurfaceWidth);
            var clampedY = Math.Clamp(logicalPoint.Y, 0, _logicalSurfaceHeight);
            return new Point(clampedX, clampedY);
        }

        private DrawingRectangle LogicalRectToPhysical(double logicalX, double logicalY, double logicalWidth, double logicalHeight)
        {
            int px = _virtualScreenBounds.X + (int)Math.Round(logicalX * _renderScale);
            int py = _virtualScreenBounds.Y + (int)Math.Round(logicalY * _renderScale);
            int pw = Math.Max(1, (int)Math.Round(logicalWidth * _renderScale));
            int ph = Math.Max(1, (int)Math.Round(logicalHeight * _renderScale));

            return new DrawingRectangle(px, py, pw, ph);
        }
    }
}
