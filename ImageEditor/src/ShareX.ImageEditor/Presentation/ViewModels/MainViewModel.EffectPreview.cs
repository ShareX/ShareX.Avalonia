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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareX.ImageEditor.Core.ImageEffects.Adjustments;
using ShareX.ImageEditor.Core.ImageEffects.Filters;
using ShareX.ImageEditor.Core.ImageEffects.Manipulations;
using ShareX.ImageEditor.Hosting;
using ShareX.ImageEditor.Presentation.Rendering;

namespace ShareX.ImageEditor.Presentation.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        // --- Edit Menu Commands ---

        [RelayCommand]
        private void Rotate90Clockwise()
        {
            _editorCore?.Rotate90Clockwise();
        }

        [RelayCommand]
        private void Rotate90CounterClockwise()
        {
            _editorCore?.Rotate90CounterClockwise();
        }

        [RelayCommand]
        private void Rotate180()
        {
            _editorCore?.Rotate180();
        }

        [RelayCommand]
        private void FlipHorizontal()
        {
            _editorCore?.FlipHorizontal();
        }

        [RelayCommand]
        private void FlipVertical()
        {
            _editorCore?.FlipVertical();
        }

        [RelayCommand]
        private void AutoCropImage()
        {
            _editorCore?.AutoCrop(10);
        }

        /// <summary>
        /// Resize the image to new dimensions with specified quality.
        /// </summary>
        public void ResizeImage(int newWidth, int newHeight, SkiaSharp.SKFilterQuality quality = SkiaSharp.SKFilterQuality.High)
        {
            if (newWidth <= 0 || newHeight <= 0)
            {
                return;
            }

            _editorCore?.ResizeImage(newWidth, newHeight, quality);
        }

        /// <summary>
        /// Resize the canvas by adding padding around the image.
        /// </summary>
        public void ResizeCanvas(int top, int right, int bottom, int left, SkiaSharp.SKColor backgroundColor)
        {
            _editorCore?.ResizeCanvas(top, right, bottom, left, backgroundColor);
        }

        // --- Effects Menu Commands ---

        [RelayCommand]
        private void InvertColors()
        {
            ApplyOneShotEffect(img => new InvertImageEffect().Apply(img), "Inverted colors");
        }

        [RelayCommand]
        private void BlackAndWhite()
        {
            ApplyOneShotEffect(img => new BlackAndWhiteImageEffect().Apply(img), "Applied Black & White filter");
        }

        [RelayCommand]
        private void Sepia()
        {
            ApplyOneShotEffect(img => new SepiaImageEffect().Apply(img), "Applied Sepia filter");
        }

        [RelayCommand]
        private void Polaroid()
        {
            ApplyOneShotEffect(img => new PolaroidImageEffect().Apply(img), "Applied Polaroid filter");
        }

        [RelayCommand]
        private void EdgeDetect()
        {
            ApplyOneShotEffect(img => new EdgeDetectImageEffect().Apply(img), "Applied Edge detect filter");
        }

        [RelayCommand]
        private void Emboss()
        {
            ApplyOneShotEffect(img => new EmbossImageEffect().Apply(img), "Applied Emboss filter");
        }

        [RelayCommand]
        private void MeanRemoval()
        {
            ApplyOneShotEffect(img => new MeanRemovalImageEffect().Apply(img), "Applied Mean removal filter");
        }

        [RelayCommand]
        private void Smooth()
        {
            ApplyOneShotEffect(img => new SmoothImageEffect().Apply(img), "Applied Smooth filter");
        }

        private void ApplyOneShotEffect(Func<SkiaSharp.SKBitmap, SkiaSharp.SKBitmap> effect, string statusMessage)
        {
            if (_editorCore == null)
            {
                return;
            }

            _editorCore.ApplyImageEffect(effect);
        }

        // --- Effect Live Preview Logic ---

        private SkiaSharp.SKBitmap? _preEffectImage;
        private SkiaSharp.SKBitmap? _latestEffectPreviewImage;

        /// <summary>
        /// Called when an effect dialog opens to store the state before previewing.
        /// </summary>
        public void StartEffectPreview()
        {
            SkiaSharp.SKBitmap? source = GetBestAvailableSourceBitmap();
            if (source == null)
            {
                return;
            }

            SkiaSharp.SKBitmap? copy = SafeCopyBitmap(source, "StartEffectPreview");
            if (copy == null)
            {
                return;
            }

            if (_latestEffectPreviewImage != null && !ReferenceEquals(_latestEffectPreviewImage, _preEffectImage))
            {
                _latestEffectPreviewImage.Dispose();
            }
            _latestEffectPreviewImage = null;

            // ISSUE-024 fix: Dispose previous bitmap before reassignment
            _preEffectImage?.Dispose();
            _preEffectImage = copy;

            _isPreviewingEffect = true;
            OnPropertyChanged(nameof(AreBackgroundEffectsActive));
            UpdateCanvasProperties();
            ApplySmartPaddingCrop();
        }

        /// <summary>
        /// Updates the displayed preview without committing changes to the source image or undo stack.
        /// </summary>
        public void UpdatePreviewImageOnly(SkiaSharp.SKBitmap preview, bool syncSourceState = false)
        {
            if (!IsBitmapAlive(preview))
            {
                return;
            }

            try
            {
                _isSyncingFromCore = true;

                // SIP-FIX: Calculate dimensions string BEFORE setting PreviewImage.
                // Setting PreviewImage can trigger bindings that might dispose the source 
                // via EditorCore updates if not handled carefully.
                string dimStr = $"{preview.Width} x {preview.Height}";

                PreviewImage = BitmapConversionHelpers.ToAvaloniBitmap(preview);
                ImageDimensions = dimStr;

                if (syncSourceState)
                {
                    SkiaSharp.SKBitmap? sourceCopy = SafeCopyBitmap(preview, "UpdatePreviewImageOnly.SyncCurrent");
                    if (sourceCopy != null)
                    {
                        _currentSourceImage?.Dispose();
                        _currentSourceImage = sourceCopy;

                        if (!_isApplyingSmartPadding)
                        {
                            _originalSourceImage?.Dispose();
                            _originalSourceImage = SafeCopyBitmap(sourceCopy, "UpdatePreviewImageOnly.SyncOriginal");
                        }
                    }
                }
            }
            finally
            {
                _isSyncingFromCore = false;
            }
        }

        /// <summary>
        /// ISSUE-028 fix: Common logic for committing effects and cleaning up preview state.
        /// </summary>
        private void CommitEffectAndCleanup(SkiaSharp.SKBitmap result, string statusMessage)
        {
            SkiaSharp.SKBitmap? preEffectImage = _preEffectImage;
            SkiaSharp.SKBitmap? latestPreviewImage = _latestEffectPreviewImage;
            _latestEffectPreviewImage = null;

            if (latestPreviewImage != null &&
                !ReferenceEquals(latestPreviewImage, result) &&
                !ReferenceEquals(latestPreviewImage, preEffectImage))
            {
                latestPreviewImage.Dispose();
            }

            bool applied = false;
            bool resultTransferred = false;

            if (_editorCore != null)
            {
                // SIP-FIX: Ensure EditorCore has the original clean image before applying the effect.
                // This ensures the memento captures the pre-effect state, not the preview/intermediate state.
                if (preEffectImage != null)
                {
                    var cleanState = preEffectImage.Copy();
                    if (cleanState != null)
                    {
                        _editorCore.UpdateSourceImage(cleanState);
                    }
                }

                applied = _editorCore.ApplyImageOperation(_ => result, clearAnnotations: false);
                resultTransferred = applied;

                // SIP-FIX: Ensure ViewModel state (_currentSourceImage) matches Core state after apply.
                if (applied)
                {
                    var syncCopy = SafeCopyBitmap(result, "CommitEffect.Sync");
                    if (syncCopy != null)
                    {
                        _currentSourceImage?.Dispose();
                        _currentSourceImage = syncCopy;

                        if (!_isApplyingSmartPadding)
                        {
                            _originalSourceImage?.Dispose();
                            _originalSourceImage = SafeCopyBitmap(syncCopy, "CommitEffect.SyncOriginal");
                        }
                    }
                }

                if (!applied)
                {
                    if (!ReferenceEquals(result, preEffectImage))
                    {
                        result.Dispose();
                    }
                }
            }
            else
            {
                UpdatePreview(result, clearAnnotations: false);
                applied = true;
                resultTransferred = true;
            }

            if (!(resultTransferred && ReferenceEquals(preEffectImage, result)))
            {
                preEffectImage?.Dispose();
            }
            _preEffectImage = null;

            _isPreviewingEffect = false;
            OnPropertyChanged(nameof(AreBackgroundEffectsActive));
            UpdateCanvasProperties();
            ApplySmartPaddingCrop();
        }

        /// <summary>
        /// Commits the effect to the undo stack and updates the source image.
        /// </summary>
        public void ApplyEffect(SkiaSharp.SKBitmap result, string statusMessage)
        {
            if (_preEffectImage == null) return; // Should have been started
            CommitEffectAndCleanup(result, statusMessage);
        }

        /// <summary>
        /// Cancels the preview and restores the original image view.
        /// </summary>
        public void CancelEffectPreview()
        {
            // SIP-FIX: Prioritize _preEffectImage (clean state) for cancellation.
            // GetBestAvailableSourceBitmap() might return the dirty/preview state from EditorCore.
            SkiaSharp.SKBitmap? source = _preEffectImage ?? GetBestAvailableSourceBitmap();

            if (source != null)
            {
                UpdatePreviewImageOnly(source, syncSourceState: true);
            }

            if (_latestEffectPreviewImage != null && !ReferenceEquals(_latestEffectPreviewImage, _preEffectImage))
            {
                _latestEffectPreviewImage.Dispose();
            }
            _latestEffectPreviewImage = null;

            _preEffectImage?.Dispose();
            _preEffectImage = null;

            // Restore Background Effects
            _isPreviewingEffect = false;
            OnPropertyChanged(nameof(AreBackgroundEffectsActive));
            UpdateCanvasProperties();
            ApplySmartPaddingCrop();
        }

        /// <summary>
        /// Applies the effect function to the pre-effect image and updates the preview.
        /// </summary>
        public void PreviewEffect(Func<SkiaSharp.SKBitmap, SkiaSharp.SKBitmap> effect)
        {
            if (_preEffectImage == null || effect == null) return;

            SkiaSharp.SKBitmap? result = null;

            try
            {
                result = effect(_preEffectImage);
                if (result == null)
                {
                    return;
                }

                if (!IsBitmapAlive(result))
                {
                    result.Dispose();
                    return;
                }

                // Normalize to a distinct owned instance if operation returns the source itself.
                if (ReferenceEquals(result, _preEffectImage))
                {
                    SkiaSharp.SKBitmap? copied = SafeCopyBitmap(result, "PreviewEffect.ReferenceEqual");
                    if (copied == null)
                    {
                        return;
                    }

                    result = copied;
                }

                UpdatePreviewImageOnly(result, syncSourceState: false);

                if (_latestEffectPreviewImage != null && !ReferenceEquals(_latestEffectPreviewImage, _preEffectImage))
                {
                    _latestEffectPreviewImage.Dispose();
                }

                _latestEffectPreviewImage = result;
                result = null;
            }
            catch (Exception ex)
            {
                result?.Dispose();
                EditorServices.ReportError(nameof(MainViewModel), "Failed to render live effect preview.", ex);
            }
        }

        /// <summary>
        /// Applies the effect to the source image and commits to undo stack.
        /// </summary>
        public void ApplyEffect(Func<SkiaSharp.SKBitmap, SkiaSharp.SKBitmap> effect, string statusMessage)
        {
            if (_preEffectImage == null) return;

            if (IsBitmapAlive(_latestEffectPreviewImage))
            {
                SkiaSharp.SKBitmap previewResult = _latestEffectPreviewImage!;
                _latestEffectPreviewImage = null;
                CommitEffectAndCleanup(previewResult, statusMessage);
                return;
            }

            _latestEffectPreviewImage?.Dispose();
            _latestEffectPreviewImage = null;

            SkiaSharp.SKBitmap? result = effect(_preEffectImage);
            if (!IsBitmapAlive(result))
            {
                result?.Dispose();
                return;
            }

            CommitEffectAndCleanup(result!, statusMessage);
        }

        // --- Rotate Custom Angle Feature ---

        [ObservableProperty]
        private double _rotateAngleDegrees;

        [ObservableProperty]
        private bool _isRotateCustomAngleDialogOpen;

        [ObservableProperty]
        private bool _rotateAutoResize = true;

        private SkiaSharp.SKBitmap? _rotateCustomAngleOriginalBitmap;

        [RelayCommand]
        public void OpenRotateCustomAngleDialog()
        {
            SkiaSharp.SKBitmap? source = GetBestAvailableSourceBitmap();
            if (PreviewImage == null || source == null)
            {
                return;
            }

            // Snapshot the CURRENT state (including any previous edits)
            var current = source;
            if (current != null)
            {
                // ISSUE-024 fix: Dispose previous bitmap before reassignment
                _rotateCustomAngleOriginalBitmap?.Dispose();
                var copy = SafeCopyBitmap(current, "OpenRotateCustomAngleDialog");
                if (copy == null)
                {
                    return;
                }
                _rotateCustomAngleOriginalBitmap = copy;
                RotateAngleDegrees = 0;
                IsRotateCustomAngleDialogOpen = true;
            }
        }

        partial void OnRotateAngleDegreesChanged(double value)
        {
            RotateCustomAngleLiveApply();
        }

        partial void OnRotateAutoResizeChanged(bool value)
        {
            RotateCustomAngleLiveApply();
        }

        private void RotateCustomAngleLiveApply()
        {
            if (!IsRotateCustomAngleDialogOpen || _rotateCustomAngleOriginalBitmap == null) return;

            float angle = (float)Math.Clamp(RotateAngleDegrees, -360, 360);
            var effect = RotateImageEffect.Custom(angle, RotateAutoResize);

            var result = effect.Apply(_rotateCustomAngleOriginalBitmap);

            UpdatePreviewImageOnly(result, syncSourceState: false);
            result.Dispose();
        }

        [RelayCommand]
        public void CommitRotateCustomAngle()
        {
            if (_editorCore == null || _rotateCustomAngleOriginalBitmap == null)
            {
                return;
            }

            float angle = (float)Math.Clamp(RotateAngleDegrees, -360, 360);

            // Restore the original (pre-preview) bitmap to EditorCore before applying,
            // so the rotation is only applied once (preview had already rotated the display).
            var cleanState = _rotateCustomAngleOriginalBitmap.Copy();
            if (cleanState != null)
            {
                _editorCore.UpdateSourceImage(cleanState);
            }

            _editorCore.RotateCustomAngle(angle, RotateAutoResize);

            IsRotateCustomAngleDialogOpen = false;
            IsModalOpen = false;
            ModalContent = null;

            _rotateCustomAngleOriginalBitmap?.Dispose();
            _rotateCustomAngleOriginalBitmap = null;
        }

        [RelayCommand]
        public void CancelRotateCustomAngle()
        {
            if (_rotateCustomAngleOriginalBitmap != null)
            {
                // Restore the original (pre-rotation) bitmap to both display and EditorCore
                UpdatePreviewImageOnly(_rotateCustomAngleOriginalBitmap, syncSourceState: true);

                var cleanState = _rotateCustomAngleOriginalBitmap.Copy();
                if (cleanState != null)
                {
                    _editorCore?.UpdateSourceImage(cleanState);
                }

                _rotateCustomAngleOriginalBitmap.Dispose();
                _rotateCustomAngleOriginalBitmap = null;
            }

            IsRotateCustomAngleDialogOpen = false;
            IsModalOpen = false;
            ModalContent = null;
        }
    }
}
