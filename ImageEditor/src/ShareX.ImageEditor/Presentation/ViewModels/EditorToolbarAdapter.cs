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

using Avalonia.Media;
using ShareX.ImageEditor.Core.Abstractions;
using ShareX.ImageEditor.Core.Annotations;
using ShareX.ImageEditor.Presentation.ViewModels;
using System.ComponentModel;
using System.Windows.Input;

namespace ShareX.ImageEditor.Presentation.ViewModels;

/// <summary>
/// Bridges <see cref="MainViewModel"/> to the core-facing toolbar contract.
/// </summary>
public sealed class EditorToolbarAdapter : IAnnotationToolbarAdapter
{
    private readonly MainViewModel _viewModel;

    public event PropertyChangedEventHandler? PropertyChanged;

    public EditorToolbarAdapter(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    public EditorTool ActiveTool
    {
        get => _viewModel.ActiveTool;
        set => _viewModel.ActiveTool = value;
    }

    public string StrokeColor
    {
        get => _viewModel.SelectedColor;
        set => _viewModel.SelectedColor = value;
    }

    public string FillColor
    {
        get => _viewModel.FillColor;
        set => _viewModel.FillColor = value;
    }

    public string TextColor
    {
        get => _viewModel.TextColor;
        set => _viewModel.TextColor = value;
    }

    public int StrokeWidth
    {
        get => _viewModel.StrokeWidth;
        set => _viewModel.StrokeWidth = value;
    }

    public float FontSize
    {
        get => _viewModel.FontSize;
        set => _viewModel.FontSize = value;
    }

    public float EffectStrength
    {
        get => _viewModel.EffectStrength;
        set => _viewModel.EffectStrength = value;
    }

    public float EffectStrengthMaximum => _viewModel.EffectStrengthMaximum;

    public bool ShadowEnabled
    {
        get => _viewModel.ShadowEnabled;
        set => _viewModel.ShadowEnabled = value;
    }

    public bool TextBold
    {
        get => _viewModel.TextBold;
        set => _viewModel.TextBold = value;
    }

    public bool TextItalic
    {
        get => _viewModel.TextItalic;
        set => _viewModel.TextItalic = value;
    }

    public bool TextUnderline
    {
        get => _viewModel.TextUnderline;
        set => _viewModel.TextUnderline = value;
    }

    public bool CanUndo => _viewModel.CanUndo;

    public bool CanRedo => _viewModel.CanRedo;

    public bool HasSelection => _viewModel.HasSelectedAnnotation;

    public bool ShowBorderColor => _viewModel.ShowBorderColor;

    public bool ShowFillColor => _viewModel.ShowFillColor;

    public bool ShowTextColor => _viewModel.ShowTextColor;

    public bool ShowThickness => _viewModel.ShowThickness;

    public bool ShowFontSize => _viewModel.ShowFontSize;

    public bool ShowStrength => _viewModel.ShowStrength;

    public bool ShowTextStyle => _viewModel.ShowTextStyle;

    public bool ShowShadow => _viewModel.ShowShadow;

    public bool ShowToolOptions => _viewModel.ShowToolOptionsSeparator;

    // Compatibility surface for existing toolbar bindings
    public IBrush SelectedColorBrush
    {
        get => _viewModel.SelectedColorBrush;
        set => _viewModel.SelectedColorBrush = value;
    }

    public IBrush FillColorBrush
    {
        get => _viewModel.FillColorBrush;
        set => _viewModel.FillColorBrush = value;
    }

    public IBrush TextColorBrush
    {
        get => _viewModel.TextColorBrush;
        set => _viewModel.TextColorBrush = value;
    }

    public string ActiveToolIcon => _viewModel.ActiveToolIcon;

    public string ActiveToolName => _viewModel.ActiveToolName;

    public bool IsSettingsPanelOpen
    {
        get => _viewModel.IsSettingsPanelOpen;
        set => _viewModel.IsSettingsPanelOpen = value;
    }

    public bool IsEffectsPanelOpen
    {
        get => _viewModel.IsEffectsPanelOpen;
        set => _viewModel.IsEffectsPanelOpen = value;
    }

    public double Zoom
    {
        get => _viewModel.Zoom;
        set => _viewModel.Zoom = value;
    }

    public bool ShowToolOptionsSeparator => ShowToolOptions;

    public ICommand SelectToolCommand => _viewModel.SelectToolCommand;

    public ICommand UndoCommand => _viewModel.UndoCommand;

    public ICommand RedoCommand => _viewModel.RedoCommand;

    public ICommand DeleteSelectedCommand => _viewModel.DeleteSelectedCommand;

    public ICommand ClearAnnotationsCommand => _viewModel.ClearAnnotationsCommand;

    public ICommand ToggleSettingsPanelCommand => _viewModel.ToggleSettingsPanelCommand;

    public ICommand ToggleEffectsPanelCommand => _viewModel.ToggleEffectsPanelCommand;

    public void Undo() => _viewModel.UndoCommand.Execute(null);

    public void Redo() => _viewModel.RedoCommand.Execute(null);

    public void DeleteSelection() => _viewModel.DeleteSelectedCommand.Execute(null);

    public void ClearSelection() => _viewModel.ClearAnnotationsCommand.Execute(null);

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
            return;
        }

        PropertyChanged?.Invoke(this, e);

        switch (e.PropertyName)
        {
            case nameof(MainViewModel.SelectedColor):
                OnPropertyChanged(nameof(StrokeColor));
                OnPropertyChanged(nameof(SelectedColorBrush));
                break;
            case nameof(MainViewModel.FillColor):
                OnPropertyChanged(nameof(FillColor));
                OnPropertyChanged(nameof(FillColorBrush));
                break;
            case nameof(MainViewModel.TextColor):
                OnPropertyChanged(nameof(TextColor));
                OnPropertyChanged(nameof(TextColorBrush));
                break;
            case nameof(MainViewModel.HasSelectedAnnotation):
                OnPropertyChanged(nameof(HasSelection));
                break;
            case nameof(MainViewModel.ShowTextColor):
                OnPropertyChanged(nameof(ShowTextColor));
                break;
            case nameof(MainViewModel.ShowToolOptionsSeparator):
                OnPropertyChanged(nameof(ShowToolOptions));
                break;
            case nameof(MainViewModel.IsEffectsPanelOpen):
                OnPropertyChanged(nameof(IsEffectsPanelOpen));
                break;
            case nameof(MainViewModel.IsSettingsPanelOpen):
                OnPropertyChanged(nameof(IsSettingsPanelOpen));
                break;
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
