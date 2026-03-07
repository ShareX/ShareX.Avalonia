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
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using ShareX.ImageEditor.Core.Annotations;
using ShareX.ImageEditor.Core.Editor;
using ShareX.ImageEditor.Presentation.Controls;
using ShareX.ImageEditor.Presentation.Rendering;
using ShareX.ImageEditor.Hosting;
using ShareX.ImageEditor.Presentation.ViewModels;
using ShareX.ImageEditor.Presentation.Controllers;
using SkiaSharp;
using System.ComponentModel;

namespace ShareX.ImageEditor.Presentation.Views
{
    public partial class EditorView : UserControl
    {
        private readonly EditorZoomController _zoomController;
        private readonly EditorSelectionController _selectionController;
        private readonly EditorInputController _inputController;

        internal EditorCore EditorCore => _editorCore;
        // SIP0018: Hybrid Rendering
        private SKCanvasControl? _canvasControl;
        private readonly EditorCore _editorCore;

        // Sync flags to prevent loop between VM.PreviewImage <-> Core.SourceImage
        private bool _isSyncingFromVM;
        private bool _isSyncingToVM;
        private bool _skipNextCoreImageChanged;

        // SIP-CLIPBOARD: Internal clipboard for shape deep-cloning
        private static Annotation? _clipboardAnnotation;

        public EditorView()
        {
            InitializeComponent();

            _editorCore = new EditorCore();

            _zoomController = new EditorZoomController(this);
            _selectionController = new EditorSelectionController(this);
            _inputController = new EditorInputController(this, _selectionController, _zoomController);

            // Subscribe to selection controller events
            _selectionController.RequestUpdateEffect += OnRequestUpdateEffect;
            _selectionController.SelectionChanged += OnSelectionChanged;

            // SIP0018: Subscribe to Core events
            _editorCore.InvalidateRequested += () => Avalonia.Threading.Dispatcher.UIThread.Post(RenderCore);
            _editorCore.ImageChanged += () => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_canvasControl != null)
                {
                    _canvasControl.Initialize((int)_editorCore.CanvasSize.Width, (int)_editorCore.CanvasSize.Height);
                    RenderCore();
                    if (DataContext is MainViewModel vm)
                    {
                        UpdateViewModelHistoryState(vm);
                        UpdateViewModelMetadata(vm);

                        // Sync Core image back to VM if change originated from Core (Undo/Redo, Core Crop)
                        if (!_isSyncingFromVM && !_isSyncingToVM && _editorCore.SourceImage != null)
                        {
                            // SIP-FIX: Break feedback loop from async ImageChanged events (e.g. Smart Padding)
                            if (_skipNextCoreImageChanged)
                            {
                                _skipNextCoreImageChanged = false;
                                return;
                            }

                            try
                            {
                                _isSyncingToVM = true;
                                vm.UpdatePreviewImageOnly(_editorCore.SourceImage, syncSourceState: true);
                            }
                            finally
                            {
                                _isSyncingToVM = false;
                            }
                        }
                    }
                }
            });
            _editorCore.AnnotationsRestored += () => Avalonia.Threading.Dispatcher.UIThread.Post(OnAnnotationsRestored);
            _editorCore.AnnotationOrderChanged += () => Avalonia.Threading.Dispatcher.UIThread.Post(OnAnnotationOrderChanged);
            _editorCore.HistoryChanged += () => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (DataContext is MainViewModel vm)
                {
                    UpdateViewModelHistoryState(vm);
                    vm.RecalculateNumberCounter(_editorCore.Annotations);

                    // Mark as dirty when history changes (annotations added/interactions/undo/redo)
                    vm.IsDirty = true;
                }
            });

            // Capture wheel events in tunneling phase so ScrollViewer doesn't scroll when using Ctrl+wheel zoom.
            AddHandler(PointerWheelChangedEvent, OnPreviewPointerWheelChanged, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);

            // Enable drag-and-drop for image files
            DragDrop.SetAllowDrop(this, true);
            AddHandler(DragDrop.DropEvent, OnDrop);
            AddHandler(DragDrop.DragOverEvent, OnDragOver);
        }

        private void OnSelectionChanged(bool hasSelection)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.HasSelectedAnnotation = hasSelection;
                var annotation = _selectionController.SelectedShape?.Tag as Annotation;
                vm.SelectedAnnotation = annotation;

                // Sync selection to EditorCore so z-order operations work
                if (annotation != null)
                {
                    _editorCore.Select(annotation);
                }
                else
                {
                    _editorCore.Deselect();
                }

                // Sync VM properties with selected annotation to update UI
                if (vm.SelectedAnnotation != null)
                {
                    // Prevent feedback loop: UI update -> VM Property Changed -> Apply to Annotation (redundant)
                    // But Apply... methods limit damage.

                    // Don't sync stroke properties from ImageAnnotation or effect annotations —
                    // they have StrokeWidth=0 / transparent stroke which would clobber
                    // Options.Thickness and break other tools
                    if (vm.SelectedAnnotation is not ImageAnnotation && vm.SelectedAnnotation is not BaseEffectAnnotation)
                    {
                        vm.SelectedColor = vm.SelectedAnnotation.StrokeColor;
                        vm.StrokeWidth = (int)vm.SelectedAnnotation.StrokeWidth;
                        vm.ShadowEnabled = vm.SelectedAnnotation.ShadowEnabled;
                    }

                    if (vm.SelectedAnnotation is NumberAnnotation num)
                    {
                        vm.FontSize = num.FontSize;
                        vm.FillColor = num.FillColor;
                        if (!string.IsNullOrEmpty(num.TextColor))
                            vm.TextColorValue = Avalonia.Media.Color.Parse(num.TextColor);
                    }
                    else if (vm.SelectedAnnotation is TextAnnotation text)
                    {
                        vm.FontSize = text.FontSize;
                        vm.TextBold = text.IsBold;
                        vm.TextItalic = text.IsItalic;
                        vm.TextUnderline = text.IsUnderline;
                        if (!string.IsNullOrEmpty(text.TextColor))
                            vm.TextColorValue = Avalonia.Media.Color.Parse(text.TextColor);
                    }
                    else if (vm.SelectedAnnotation is SpeechBalloonAnnotation balloon)
                    {
                        vm.FontSize = balloon.FontSize;
                        vm.FillColor = balloon.FillColor;
                        if (!string.IsNullOrEmpty(balloon.TextColor))
                            vm.TextColorValue = Avalonia.Media.Color.Parse(balloon.TextColor);
                    }
                    else if (vm.SelectedAnnotation is RectangleAnnotation rect)
                    {
                        vm.FillColor = rect.FillColor;
                    }
                    else if (vm.SelectedAnnotation is EllipseAnnotation ellipse)
                    {
                        vm.FillColor = ellipse.FillColor;
                    }
                    else if (vm.SelectedAnnotation is BaseEffectAnnotation effect)
                    {
                        vm.EffectStrength = (int)effect.Amount;
                        if (effect is HighlightAnnotation highlight)
                        {
                            vm.FillColor = highlight.FillColor;
                        }
                    }
                }
            }
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            // Check clipboard initially
            _ = CheckClipboardStatus();

            // Listen for window activation to check clipboard (as close as we get to ClipboardChanged)
            if (TopLevel.GetTopLevel(this) is Window window)
            {
                window.Activated += (s, args) => _ = CheckClipboardStatus();
            }

            if (DataContext is MainViewModel vm)
            {
                vm.AttachEditorCore(_editorCore);

                var effectPanel = this.FindControl<EffectBrowserPanel>("EffectBrowserPanel");
                effectPanel?.SetOptions(vm.Options);

                vm.DeleteRequested += (s, args) => PerformDelete();
                vm.UndoRequested += (s, args) => PerformUndo();
                vm.RedoRequested += (s, args) => PerformRedo();
                vm.ClearAnnotationsRequested += (s, args) => ClearAllAnnotations();

                // Subscribe to new context menu events
                vm.CutAnnotationRequested += OnCutRequested;
                vm.CopyAnnotationRequested += OnCopyRequested;
                vm.PasteRequested += OnPasteRequested;
                vm.DuplicateRequested += OnDuplicateRequested;
                vm.ZoomToFitRequested += OnZoomToFitRequested;
                vm.FlattenRequested += OnFlattenRequested;

                // Original code subscribed to vm.PropertyChanged
                vm.PropertyChanged += OnViewModelPropertyChanged;

                // Initialize zoom
                _zoomController.InitLastZoom(vm.Zoom);

                // Wire up View interactions
                vm.DeselectRequested += OnDeselectRequested;

                // Initial load
                if (vm.PreviewImage != null)
                {
                    LoadImageFromViewModel(vm);
                }

                // Reset dirty flag after initial load — loading the image fires HistoryChanged
                // and OnPreviewImageChanged which both set IsDirty=true as a side-effect.
                vm.IsDirty = false;
            }
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);

            if (DataContext is MainViewModel vm)
            {
                vm.PropertyChanged -= OnViewModelPropertyChanged;
                vm.DeselectRequested -= OnDeselectRequested;
                vm.ZoomToFitRequested -= OnZoomToFitRequested;
            }

            _selectionController.RequestUpdateEffect -= OnRequestUpdateEffect;
        }

        private void OnEffectBrowserOverlayPointerPressed(object? sender, global::Avalonia.Input.PointerPressedEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.IsEffectBrowserVisible)
            {
                if (vm.CloseEffectsPanelCommand.CanExecute(null))
                {
                    vm.CloseEffectsPanelCommand.Execute(null);
                }
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is MainViewModel vm)
            {
                if (e.PropertyName == nameof(MainViewModel.SelectedColor))
                {
                    ApplySelectedColor(vm.SelectedColor);
                }
                else if (e.PropertyName == nameof(MainViewModel.StrokeWidth))
                {
                    ApplySelectedStrokeWidth(vm.StrokeWidth);
                }
                else if (e.PropertyName == nameof(MainViewModel.FillColorValue))
                {
                    ApplySelectedFillColor(vm.FillColor);
                }
                else if (e.PropertyName == nameof(MainViewModel.TextColorValue))
                {
                    ApplySelectedTextColor(vm.TextColor);
                }
                else if (e.PropertyName == nameof(MainViewModel.PreviewImage))
                {
                    _zoomController.ResetScrollViewerOffset();
                    // During smart padding, use UpdateSourceImage to preserve history and annotations
                    if (vm.IsSmartPaddingInProgress)
                    {
                        UpdateSourceImageFromViewModel(vm);
                    }
                    else
                    {
                        LoadImageFromViewModel(vm);
                    }
                }
                else if (e.PropertyName == nameof(MainViewModel.Zoom))
                {
                    _zoomController.HandleZoomPropertyChanged(vm);
                }
                else if (e.PropertyName == nameof(MainViewModel.ActiveTool))
                {
                    if (vm.ActiveTool == EditorTool.Crop)
                        _inputController.ActivateCropToFullImage();
                    else
                        _inputController.CancelCrop();
                    _selectionController.ClearSelection();
                    UpdateCursorForTool(); // ISSUE-018 fix: Update cursor feedback for active tool
                }
                else if (e.PropertyName == nameof(MainViewModel.IsEffectBrowserVisible))
                {
                    if (vm.IsEffectBrowserVisible)
                    {
                        var effectPanel = this.FindControl<EffectBrowserPanel>("EffectBrowserPanel");
                        effectPanel?.FocusSearchBox();
                    }
                }
            }
        }

        /// <summary>
        /// ISSUE-018 fix: Updates the canvas cursor based on the active tool
        /// </summary>
        private void UpdateCursorForTool()
        {
            var canvas = this.FindControl<Canvas>("AnnotationCanvas");
            if (canvas == null || DataContext is not MainViewModel vm) return;

            canvas.Cursor = vm.ActiveTool switch
            {
                EditorTool.Select => new Cursor(StandardCursorType.Arrow),
                EditorTool.Crop or EditorTool.CutOut => new Cursor(StandardCursorType.Cross),
                _ => new Cursor(StandardCursorType.Cross) // Drawing tools (Rectangle, Ellipse, Pen, etc.)
            };
        }

        // --- Public/Internal Methods for Controllers ---

        protected override void OnInitialized()
        {
            base.OnInitialized();
            _canvasControl = this.FindControl<SKCanvasControl>("CanvasControl");
        }

        private void LoadImageFromViewModel(MainViewModel vm)
        {
            if (vm.PreviewImage == null || _canvasControl == null) return;
            if (_isSyncingToVM) return; // Ignore updates that we just pushed to VM

            try
            {
                _isSyncingFromVM = true;

                // One-time conversion from Avalonia Bitmap to SKBitmap for the Core
                // In a full refactor, VM would hold SKBitmap source of truth
                using var skBitmap = BitmapConversionHelpers.ToSKBitmap(vm.PreviewImage);
                if (skBitmap != null)
                {
                    // We must copy because ToSKBitmap might return a disposable wrapper or we need ownership
                    // ISSUE-FIX: Use UpdateSourceImage to preserve existing history/annotations
                    // This allows VM-driven updates (Effects, Undo) to not wipe Core state.
                    // New file loads should be preceded by Clear() from the VM/Host.
                    _skipNextCoreImageChanged = true;
                    _editorCore.UpdateSourceImage(skBitmap.Copy());

                    _canvasControl.Initialize(skBitmap.Width, skBitmap.Height);
                    RenderCore();
                }
            }
            finally
            {
                _isSyncingFromVM = false;
            }
        }

        /// <summary>
        /// Updates the source image in EditorCore without clearing history or annotations.
        /// Used during smart padding operations to preserve editing state.
        /// </summary>
        private void UpdateSourceImageFromViewModel(MainViewModel vm)
        {
            if (vm.PreviewImage == null || _canvasControl == null) return;

            using var skBitmap = BitmapConversionHelpers.ToSKBitmap(vm.PreviewImage);
            if (skBitmap != null)
            {
                _skipNextCoreImageChanged = true;
                _editorCore.UpdateSourceImage(skBitmap.Copy());
                _canvasControl.Initialize(skBitmap.Width, skBitmap.Height);
                RenderCore();
            }
        }


        // --- Event Handlers Delegated to Controllers ---

        private void OnPreviewPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            _zoomController.OnPreviewPointerWheelChanged(sender, e);
        }

        private void OnScrollViewerPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            _zoomController.OnScrollViewerPointerPressed(sender, e);
        }

        private void OnScrollViewerPointerMoved(object? sender, PointerEventArgs e)
        {
            _zoomController.OnScrollViewerPointerMoved(sender, e);
        }

        private void OnScrollViewerPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _zoomController.OnScrollViewerPointerReleased(sender, e);
        }

        private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            _inputController.OnCanvasPointerPressed(sender, e);
        }

        private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
        {
            _inputController.OnCanvasPointerMoved(sender, e);
        }

        private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _inputController.OnCanvasPointerReleased(sender, e);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Source is TextBox) return;

            if (DataContext is MainViewModel vm)
            {
                if (e.Key == Key.Delete)
                {
                    if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    {
                        if (vm.ClearAnnotationsCommand.CanExecute(null))
                        {
                            vm.ClearAnnotationsCommand.Execute(null);
                            e.Handled = true;
                        }
                    }
                    else
                    {
                        vm.DeleteSelectedCommand.Execute(null);
                        e.Handled = true;
                    }
                }
                else if (e.KeyModifiers.HasFlag(KeyModifiers.Control | KeyModifiers.Shift))
                {
                    switch (e.Key)
                    {
                        case Key.Z: vm.RedoCommand.Execute(null); e.Handled = true; break;
                        case Key.C: vm.CopyCommand.Execute(null); e.Handled = true; break;
                        case Key.F: vm.FlattenImageCommand.Execute(null); e.Handled = true; break;
                        case Key.S: vm.SaveAsCommand.Execute(null); e.Handled = true; break;
                    }
                }
                else if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    switch (e.Key)
                    {
                        case Key.Z: vm.UndoCommand.Execute(null); e.Handled = true; break;
                        case Key.Y: vm.RedoCommand.Execute(null); e.Handled = true; break;
                        case Key.X: vm.CutAnnotationCommand.Execute(null); e.Handled = true; break;
                        case Key.C: vm.CopyAnnotationCommand.Execute(null); e.Handled = true; break;
                        case Key.V: vm.PasteCommand.Execute(null); e.Handled = true; break;
                        case Key.D: DuplicateSelectedAnnotation(); e.Handled = true; break;
                        case Key.S: vm.SaveCommand.Execute(null); e.Handled = true; break;
                        case Key.P: vm.PinToScreenCommand.Execute(null); e.Handled = true; break;
                        case Key.U: vm.UploadCommand.Execute(null); e.Handled = true; break;
                    }
                }
                else if (e.KeyModifiers == KeyModifiers.None || e.KeyModifiers == KeyModifiers.Shift)
                {
                    double step = e.KeyModifiers == KeyModifiers.Shift ? 10 : 1;

                    if ((e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right) && _selectionController.SelectedShape != null)
                    {
                        double dx = e.Key == Key.Left ? -step : (e.Key == Key.Right ? step : 0);
                        double dy = e.Key == Key.Up ? -step : (e.Key == Key.Down ? step : 0);
                        _selectionController.MoveSelectedShape(dx, dy);
                        e.Handled = true;
                    }
                    else if (e.KeyModifiers == KeyModifiers.None)
                    {
                        // Tool shortcuts
                        switch (e.Key)
                        {
                            case Key.Home: _editorCore.BringToFront(); e.Handled = true; break;
                            case Key.End: _editorCore.SendToBack(); e.Handled = true; break;
                            case Key.PageUp: _editorCore.BringForward(); e.Handled = true; break;
                            case Key.PageDown: _editorCore.SendBackward(); e.Handled = true; break;

                            case Key.V: vm.SelectToolCommand.Execute(EditorTool.Select); e.Handled = true; break;
                            case Key.R: vm.SelectToolCommand.Execute(EditorTool.Rectangle); e.Handled = true; break;
                            case Key.E: vm.SelectToolCommand.Execute(EditorTool.Ellipse); e.Handled = true; break;
                            case Key.L: vm.SelectToolCommand.Execute(EditorTool.Line); e.Handled = true; break;
                            case Key.A: vm.SelectToolCommand.Execute(EditorTool.Arrow); e.Handled = true; break;
                            case Key.F: vm.SelectToolCommand.Execute(EditorTool.Freehand); e.Handled = true; break; // Freehand
                            case Key.T: vm.SelectToolCommand.Execute(EditorTool.Text); e.Handled = true; break;
                            case Key.O: vm.SelectToolCommand.Execute(EditorTool.SpeechBalloon); e.Handled = true; break;
                            case Key.N: vm.SelectToolCommand.Execute(EditorTool.Step); e.Handled = true; break;
                            case Key.W: vm.SelectToolCommand.Execute(EditorTool.SmartEraser); e.Handled = true; break;
                            case Key.S: vm.SelectToolCommand.Execute(EditorTool.Spotlight); e.Handled = true; break;
                            case Key.B: vm.SelectToolCommand.Execute(EditorTool.Blur); e.Handled = true; break;
                            case Key.P: vm.SelectToolCommand.Execute(EditorTool.Pixelate); e.Handled = true; break;
                            case Key.I: vm.SelectToolCommand.Execute(EditorTool.Image); e.Handled = true; break;
                            case Key.H: vm.SelectToolCommand.Execute(EditorTool.Highlight); e.Handled = true; break;
                            case Key.M: vm.SelectToolCommand.Execute(EditorTool.Magnify); e.Handled = true; break;
                            case Key.C: vm.SelectToolCommand.Execute(EditorTool.Crop); e.Handled = true; break;
                            case Key.U: vm.SelectToolCommand.Execute(EditorTool.CutOut); e.Handled = true; break;

                            case Key.Enter:
                                if (_inputController.TryConfirmCrop())
                                {
                                    e.Handled = true;
                                }
                                else if (vm.TaskMode)
                                {
                                    vm.ContinueCommand.Execute(null);
                                    e.Handled = true;
                                }
                                break;
                        }
                    }
                }
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Source is TextBox) return;

            if (DataContext is MainViewModel vm && e.KeyModifiers == KeyModifiers.None)
            {
                switch (e.Key)
                {
                    case Key.Escape:
                        if (_inputController.CancelCrop())
                        {
                            e.Handled = true;
                        }
                        else if (_selectionController.SelectedShape != null)
                        {
                            _selectionController.ClearSelection();
                            e.Handled = true;
                        }
                        else if (vm.TaskMode)
                        {
                            vm.CancelCommand.Execute(null);
                            e.Handled = true;
                        }
                        else
                        {
                            if (TopLevel.GetTopLevel(this) is Avalonia.Controls.Window window)
                            {
                                window.Close();
                                e.Handled = true;
                            }
                        }
                        break;
                }
            }
        }

        // --- Private Helpers (Undo/Redo, Delete, etc that involve view state) ---

        private void PerformUndo()
        {
            if (_editorCore.CanUndo)
            {
                _editorCore.Undo();
                // AnnotationsRestored event will handle UI sync
            }
        }

        private void PerformRedo()
        {
            if (_editorCore.CanRedo)
            {
                _editorCore.Redo();
            }
        }

        private void OnDeselectRequested(object? sender, EventArgs e)
        {
            _inputController.CancelCrop();
            _selectionController.ClearSelection();
        }

        private Color SKColorToAvalonia(SKColor color)
        {
            return Color.FromUInt32((uint)color);
        }

        private Control? CreateControlForAnnotation(Annotation annotation)
        {
            var control = AnnotationVisualFactory.CreateVisualControl(annotation, AnnotationVisualMode.Persisted);
            if (control == null)
            {
                return null;
            }

            AnnotationVisualFactory.UpdateVisualControl(
                control,
                annotation,
                AnnotationVisualMode.Persisted,
                _editorCore.CanvasSize.Width,
                _editorCore.CanvasSize.Height);

            // Effect annotations require bitmap-backed fills from current source image.
            if (annotation is BaseEffectAnnotation)
            {
                OnRequestUpdateEffect(control);
            }

            return control;
        }

        private void PerformDelete()
        {
            var selected = _selectionController.SelectedShape;
            if (selected != null)
            {
                var canvas = this.FindControl<Canvas>("AnnotationCanvas");
                if (canvas != null && canvas.Children.Contains(selected))
                {
                    // Sync with EditorCore - this creates the undo history entry
                    if (selected.Tag is Annotation annotation)
                    {
                        // Select the annotation in core so DeleteSelected knows what to remove
                        _editorCore.Select(annotation);
                        _editorCore.DeleteSelected();
                    }

                    // Dispose annotation resources before removing from view
                    (selected.Tag as IDisposable)?.Dispose();

                    canvas.Children.Remove(selected);

                    _selectionController.ClearSelection();

                    // Update HasAnnotations state
                    UpdateHasAnnotationsState();
                }
            }
        }

        private void ClearAllAnnotations()
        {
            var canvas = this.FindControl<Canvas>("AnnotationCanvas");
            if (canvas != null)
            {
                canvas.Children.Clear();
                _selectionController.ClearSelection();
                _editorCore.ClearAll(resetHistory: false);
                RenderCore();

                // Update HasAnnotations state
                if (DataContext is MainViewModel vm)
                {
                    vm.HasAnnotations = false;
                }
            }
        }


        // --- Crop and Image Insertion ---

        public void PerformCrop()
        {
            var cropOverlay = this.FindControl<global::Avalonia.Controls.Shapes.Rectangle>("CropOverlay");
            if (cropOverlay != null && cropOverlay.IsVisible && DataContext is MainViewModel vm)
            {
                var rect = new SkiaSharp.SKRect(
                    (float)Canvas.GetLeft(cropOverlay),
                    (float)Canvas.GetTop(cropOverlay),
                    (float)(Canvas.GetLeft(cropOverlay) + cropOverlay.Width),
                    (float)(Canvas.GetTop(cropOverlay) + cropOverlay.Height));

                if (rect.Width > 0 && rect.Height > 0)
                {
                    // Canvas coordinates are already in image-pixel space (AnnotationCanvas
                    // is sized to CanvasSize = bitmap.Width/Height). No DPI scaling needed.
                    var cropX = (int)Math.Round(rect.Left);
                    var cropY = (int)Math.Round(rect.Top);
                    var cropW = (int)Math.Round(rect.Width);
                    var cropH = (int)Math.Round(rect.Height);

                    _editorCore.Crop(new SKRect(cropX, cropY, cropX + cropW, cropY + cropH));
                }
                cropOverlay.IsVisible = false;
            }
        }

        // --- Image Paste & Drag-Drop ---

        /// <summary>
        /// Inserts an image annotation from an SKBitmap at an optional drop position.
        /// Adds the annotation to both the Avalonia canvas and EditorCore, then switches to Select tool.
        /// </summary>
        /// <remarks>
        /// XIP0039 Guardrail 6: This method is public so host applications can insert image annotations
        /// directly without resorting to reflection. The previous private access required callers such as
        /// <c>MainWindow.axaml.cs</c> to use <c>BindingFlags.NonPublic</c> reflection.
        /// </remarks>
        public void InsertImageAnnotation(SKBitmap skBitmap, Point? dropPosition = null)
        {
            var canvas = this.FindControl<Canvas>("AnnotationCanvas");
            if (canvas == null || DataContext is not MainViewModel vm)
            {
                return;
            }

            // Calculate position: drop point or center of canvas
            var posX = dropPosition?.X ?? (_editorCore.CanvasSize.Width / 2 - skBitmap.Width / 2);
            var posY = dropPosition?.Y ?? (_editorCore.CanvasSize.Height / 2 - skBitmap.Height / 2);

            var annotation = new ImageAnnotation();
            annotation.SetImage(skBitmap);
            annotation.StartPoint = new SKPoint((float)posX, (float)posY);
            annotation.EndPoint = new SKPoint(
                (float)posX + skBitmap.Width,
                (float)posY + skBitmap.Height);

            var avBitmap = BitmapConversionHelpers.ToAvaloniBitmap(skBitmap);
            var imageControl = new Image
            {
                Source = avBitmap,
                Width = skBitmap.Width,
                Height = skBitmap.Height,
                Tag = annotation
            };
            Canvas.SetLeft(imageControl, posX);
            Canvas.SetTop(imageControl, posY);

            canvas.Children.Add(imageControl);
            _editorCore.AddAnnotation(annotation);
            vm.HasAnnotations = true;
            vm.ActiveTool = EditorTool.Select; // Auto-switch to Select tool
            _selectionController.SetSelectedShape(imageControl);
        }

        /// <summary>
        /// Handles DragOver event to show appropriate drag cursor.
        /// </summary>
        private void OnDragOver(object? sender, DragEventArgs e)
        {
            // Keep DragOver lightweight and non-consuming; resolve concrete files in OnDrop.
            e.DragEffects = e.DataTransfer.Formats.Contains(DataFormat.File)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        /// <summary>
        /// Handles drag-and-drop of image files onto the editor canvas.
        /// </summary>
        private async void OnDrop(object? sender, DragEventArgs e)
        {
            var droppedItems = e.DataTransfer.TryGetFiles()?.ToList() ?? new List<IStorageItem>();

            // Fallback for providers that expose files only through raw items.
            if (droppedItems.Count == 0)
            {
                foreach (var item in e.DataTransfer.Items)
                {
                    if (item.TryGetRaw(DataFormat.File) is IStorageItem storageItem)
                    {
                        droppedItems.Add(storageItem);
                    }
                }
            }

            if (droppedItems.Count > 0)
            {
                // Get drop position relative to the annotation canvas
                var canvas = this.FindControl<Canvas>("AnnotationCanvas");
                Point? dropPos = null;
                if (canvas != null)
                {
                    dropPos = e.GetPosition(canvas);
                }

                foreach (var item in droppedItems)
                {
                    if (item is IStorageFile file)
                    {
                        var ext = System.IO.Path.GetExtension(file.Name)?.ToLowerInvariant();

                        if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif" || ext == ".webp" || ext == ".ico" || ext == ".tiff" || ext == ".tif")
                        {
                            try
                            {
                                using var stream = await file.OpenReadAsync();
                                using var memStream = new System.IO.MemoryStream();
                                await stream.CopyToAsync(memStream);
                                memStream.Position = 0;
                                var skBitmap = SKBitmap.Decode(memStream);
                                if (skBitmap != null)
                                {
                                    // If there's no base image yet (common in embedded MainWindow editor),
                                    // use the dropped file as the main preview image.
                                    if (DataContext is MainViewModel vm && !vm.HasPreviewImage)
                                    {
                                        vm.UpdatePreview(skBitmap, clearAnnotations: true);
                                        return;
                                    }

                                    // Otherwise add it as an image annotation on top of the current canvas.
                                    var centeredPos = dropPos.HasValue
                                        ? new Point(dropPos.Value.X - skBitmap.Width / 2, dropPos.Value.Y - skBitmap.Height / 2)
                                        : (Point?)null;
                                    InsertImageAnnotation(skBitmap, centeredPos);
                                }
                            }
                            catch (Exception ex)
                            {
                                EditorServices.ReportWarning(nameof(EditorView), $"Failed to decode dropped image '{file.Name}'.", ex);
                            }
                        }
                    }
                }
            }
        }

        private void OnZoomToFitRequested(object? sender, EventArgs e)
        {
            _zoomController.ZoomToFit();
        }

        private void OnFlattenRequested(object? sender, EventArgs e)
        {
            var snapshot = GetSnapshot();
            if (snapshot == null) return;

            if (_editorCore.FlattenImage(snapshot))
            {
                // Clear annotation visuals from the UI canvas
                var canvas = this.FindControl<Canvas>("AnnotationCanvas");
                if (canvas != null)
                {
                    canvas.Children.Clear();
                    _selectionController.ClearSelection();
                }

                if (DataContext is MainViewModel vm)
                {
                    vm.HasAnnotations = false;
                }
            }
        }

        public void OpenContextMenu(Control target)
        {
            if (this.Resources["EditorContextMenu"] is ContextMenu menu)
            {
                menu.PlacementTarget = target;
                menu.Open(target);
            }
        }

    }
}
