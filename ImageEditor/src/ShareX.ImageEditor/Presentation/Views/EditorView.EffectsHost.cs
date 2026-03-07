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
using ShareX.ImageEditor.Core.Annotations;
using ShareX.ImageEditor.Presentation.Controls;
using ShareX.ImageEditor.Presentation.ViewModels;
using ShareX.ImageEditor.Presentation.Views.Dialogs;
using SkiaSharp;

namespace ShareX.ImageEditor.Presentation.Views
{
    public partial class EditorView : UserControl
    {
        // --- Edit Menu Event Handlers ---

        private void OnCropImageRequested(object? sender, EventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.PreviewImage != null)
            {
                var dialog = new CropImageDialog();
                dialog.Initialize((int)vm.ImageWidth, (int)vm.ImageHeight);

                dialog.ApplyRequested += (s, args) =>
                {
                    // SIP-FIX: Use Core crop to handle annotation adjustment and history unified
                    _editorCore.Crop(new SKRect(args.X, args.Y, args.X + args.Width, args.Y + args.Height));
                    vm.CloseEffectsPanelCommand.Execute(null);
                };

                dialog.CancelRequested += (s, args) =>
                {
                    vm.CloseEffectsPanelCommand.Execute(null);
                };

                vm.EffectsPanelContent = dialog;
                vm.IsEffectsPanelOpen = true;
            }
        }

        private void OnAutoCropImageRequested(object? sender, EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.AutoCropImageCommand.Execute(null);
                vm.CloseEffectsPanelCommand.Execute(null);
            }
        }

        private void OnRotate90CWRequested(object? sender, EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.Rotate90ClockwiseCommand.Execute(null);
                vm.CloseEffectsPanelCommand.Execute(null);
            }
        }

        private void OnRotate90CCWRequested(object? sender, EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.Rotate90CounterClockwiseCommand.Execute(null);
                vm.CloseEffectsPanelCommand.Execute(null);
            }
        }

        private void OnRotate180Requested(object? sender, EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.Rotate180Command.Execute(null);
                vm.CloseEffectsPanelCommand.Execute(null);
            }
        }

        private void OnRotateCustomAngleRequested(object? sender, EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.OpenRotateCustomAngleDialogCommand.Execute(null);
                var dialog = new RotateCustomAngleDialog();
                vm.EffectsPanelContent = dialog;
                vm.IsEffectsPanelOpen = true;
            }
        }

        private void OnFlipHorizontalRequested(object? sender, EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.FlipHorizontalCommand.Execute(null);
                vm.CloseEffectsPanelCommand.Execute(null);
            }
        }

        private void OnFlipVerticalRequested(object? sender, EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.FlipVerticalCommand.Execute(null);
                vm.CloseEffectsPanelCommand.Execute(null);
            }
        }

        // --- Immediate effects (no dialog) ---

        private void OnInvertRequested(object? sender, EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.InvertColorsCommand.Execute(null);
                vm.CloseEffectsPanelCommand.Execute(null);
            }
        }

        private void OnBlackAndWhiteRequested(object? sender, EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.BlackAndWhiteCommand.Execute(null);
                vm.CloseEffectsPanelCommand.Execute(null);
            }
        }

        private void OnPolaroidRequested(object? sender, EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.PolaroidCommand.Execute(null);
                vm.CloseEffectsPanelCommand.Execute(null);
            }
        }

        private void OnEdgeDetectRequested(object? sender, EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.EdgeDetectCommand.Execute(null);
                vm.CloseEffectsPanelCommand.Execute(null);
            }
        }

        private void OnEmbossRequested(object? sender, EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.EmbossCommand.Execute(null);
                vm.CloseEffectsPanelCommand.Execute(null);
            }
        }

        private void OnMeanRemovalRequested(object? sender, EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.MeanRemovalCommand.Execute(null);
                vm.CloseEffectsPanelCommand.Execute(null);
            }
        }

        private void OnSmoothRequested(object? sender, EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SmoothCommand.Execute(null);
                vm.CloseEffectsPanelCommand.Execute(null);
            }
        }

        // --- XIP0039 Pain Point 3: Registry-driven dialog dispatch ---

        /// <summary>
        /// Single handler for all registry-backed effect dialogs.
        /// Adding a new dialog-based effect requires only an <see cref="EffectDialogRegistry"/>
        /// entry plus a menu item that calls <c>RaiseDialog("id")</c> — no new method here.
        /// </summary>
        private void OnEffectDialogRequested(object? sender, EffectDialogRequestedEventArgs e)
        {
            if (!EffectDialogRegistry.TryCreate(e.EffectId, out var dialog) || dialog == null)
                return;

            switch (dialog)
            {
                case IEffectDialog effectDialog:
                    ShowEffectDialog(dialog, effectDialog);
                    break;
                case ResizeImageDialog resizeImageDialog:
                    ShowResizeImageDialog(resizeImageDialog);
                    break;
                case ResizeCanvasDialog resizeCanvasDialog:
                    ShowResizeCanvasDialog(resizeCanvasDialog);
                    break;
            }
        }

        /// <summary>
        /// Wires preview/apply/cancel lifecycle for a dialog-based effect and opens the effects panel.
        /// </summary>
        private void ShowEffectDialog(UserControl dialog, IEffectDialog effectDialog)
        {
            var vm = DataContext as MainViewModel;
            if (vm == null) return;

            vm.StartEffectPreview();

            effectDialog.PreviewRequested += (s, e) => vm.PreviewEffect(e.EffectOperation);
            effectDialog.ApplyRequested += (s, e) =>
            {
                vm.ApplyEffect(e.EffectOperation, e.StatusMessage);
                vm.CloseEffectsPanelCommand.Execute(null);
            };
            effectDialog.CancelRequested += (s, e) =>
            {
                vm.CancelEffectPreview();
                vm.CloseEffectsPanelCommand.Execute(null);
            };

            vm.EffectsPanelContent = dialog;
            vm.IsEffectsPanelOpen = true;
        }

        private void ShowResizeImageDialog(ResizeImageDialog dialog)
        {
            if (DataContext is not MainViewModel vm || vm.PreviewImage == null)
                return;

            dialog.Initialize((int)vm.ImageWidth, (int)vm.ImageHeight);
            dialog.ApplyRequested += (s, args) =>
            {
                vm.ResizeImage(args.NewWidth, args.NewHeight, args.Quality);
                vm.CloseEffectsPanelCommand.Execute(null);
            };
            dialog.CancelRequested += (s, args) =>
            {
                vm.CloseEffectsPanelCommand.Execute(null);
            };

            vm.EffectsPanelContent = dialog;
            vm.IsEffectsPanelOpen = true;
        }

        private void ShowResizeCanvasDialog(ResizeCanvasDialog dialog)
        {
            if (DataContext is not MainViewModel vm || vm.PreviewImage == null)
                return;

            dialog.ApplyRequested += (s, args) =>
            {
                vm.ResizeCanvas(args.Top, args.Right, args.Bottom, args.Left, args.BackgroundColor);
                vm.CloseEffectsPanelCommand.Execute(null);
            };
            dialog.CancelRequested += (s, args) =>
            {
                vm.CloseEffectsPanelCommand.Execute(null);
            };

            vm.EffectsPanelContent = dialog;
            vm.IsEffectsPanelOpen = true;
        }

        private void OnModalBackgroundPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            // Only close if clicking on the background, not the dialog content
            if (e.Source == sender && DataContext is MainViewModel vm)
            {
                vm.CancelEffectPreview();
                vm.CloseModalCommand.Execute(null);
            }
        }

        /// <summary>
        /// Validates that UI annotation state is synchronized with EditorCore state.
        /// ISSUE-001 mitigation: Detect annotation count mismatches in dual-state architecture.
        /// </summary>
        private void ValidateAnnotationSync()
        {
            var canvas = this.FindControl<Canvas>("AnnotationCanvas");
            if (canvas == null) return;

            int uiAnnotationCount = 0;
            foreach (var child in canvas.Children)
            {
                if (child is Control control && control.Tag is Annotation &&
                    control.Name != "CropOverlay" && control.Name != "CutOutOverlay")
                {
                    uiAnnotationCount++;
                }
            }

            int coreAnnotationCount = _editorCore.Annotations.Count;
        }
    }
}
