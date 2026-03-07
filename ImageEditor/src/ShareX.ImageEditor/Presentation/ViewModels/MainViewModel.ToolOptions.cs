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
using CommunityToolkit.Mvvm.ComponentModel;
using ShareX.ImageEditor.Core.Annotations;

namespace ShareX.ImageEditor.Presentation.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _selectedColor = "#EF4444";

        // Add a brush version for the dropdown control
        public IBrush SelectedColorBrush
        {
            get => new SolidColorBrush(Color.Parse(SelectedColor));
            set
            {
                if (value is SolidColorBrush solidBrush)
                {
                    SelectedColor = $"#{solidBrush.Color.A:X2}{solidBrush.Color.R:X2}{solidBrush.Color.G:X2}{solidBrush.Color.B:X2}";
                }
            }
        }

        // Color value for Avalonia ColorPicker binding
        public Color SelectedColorValue
        {
            get => Color.Parse(SelectedColor);
            set => SelectedColor = $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}";
        }

        partial void OnSelectedColorChanged(string value)
        {
            OnPropertyChanged(nameof(SelectedColorBrush));
            OnPropertyChanged(nameof(SelectedColorValue));
            UpdateOptionsFromSelectedColor();
        }

        private void UpdateOptionsFromSelectedColor()
        {
            var color = SelectedColorValue;
            switch (ActiveTool)
            {
                case EditorTool.Select:
                    if (SelectedAnnotation != null)
                    {
                        // TODO: Update SelectedAnnotation color if needed
                    }
                    else
                    {
                        // Fallback to update generic options if no annotation is selected but tool is active?
                        // Or just update default options.
                        UpdateDefaultOptionsColor(color);
                    }
                    break;
                default:
                    UpdateDefaultOptionsColor(color);
                    break;
            }
        }

        private void UpdateDefaultOptionsColor(Color color)
        {
            switch (ActiveTool)
            {
                case EditorTool.Step:
                    Options.StepBorderColor = color;
                    break;
                case EditorTool.SpeechBalloon:
                    Options.SpeechBalloonBorderColor = color;
                    break;
                case EditorTool.Text:
                    Options.TextBorderColor = color;
                    break;
                default:
                    Options.BorderColor = color;
                    break;
            }
        }

        [ObservableProperty]
        private int _strokeWidth = 4;

        partial void OnStrokeWidthChanged(int value)
        {
            if (ActiveTool == EditorTool.Step)
            {
                Options.StepThickness = value;
            }
            else if (ActiveTool == EditorTool.SpeechBalloon)
            {
                Options.SpeechBalloonThickness = value;
            }
            else if (ActiveTool == EditorTool.Text)
            {
                Options.TextThickness = value;
            }
            else if (ActiveTool == EditorTool.SmartEraser)
            {
                Options.SmartEraserThickness = value;
            }
            else if (ActiveTool == EditorTool.Select && SelectedAnnotation != null)
            {
                if (SelectedAnnotation is NumberAnnotation) Options.StepThickness = value;
                else if (SelectedAnnotation is SpeechBalloonAnnotation) Options.SpeechBalloonThickness = value;
                else if (SelectedAnnotation is TextAnnotation) Options.TextThickness = value;
                else if (SelectedAnnotation is SmartEraserAnnotation) Options.SmartEraserThickness = value;
                else Options.Thickness = value;
            }
            else
            {
                Options.Thickness = value;
            }
        }

        // Tool-specific options
        [ObservableProperty]
        private string _fillColor = "#00000000"; // Transparent by default

        // Add a brush version for the fill color dropdown control
        public IBrush FillColorBrush
        {
            get => new SolidColorBrush(Color.Parse(FillColor));
            set
            {
                if (value is SolidColorBrush solidBrush)
                {
                    FillColor = $"#{solidBrush.Color.A:X2}{solidBrush.Color.R:X2}{solidBrush.Color.G:X2}{solidBrush.Color.B:X2}";
                }
            }
        }

        // Color value for Avalonia ColorPicker binding
        public Color FillColorValue
        {
            get => Color.Parse(FillColor);
            set => FillColor = $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}";
        }

        partial void OnFillColorChanged(string value)
        {
            OnPropertyChanged(nameof(FillColorBrush));
            OnPropertyChanged(nameof(FillColorValue));
            UpdateOptionsFromFillColor();
        }

        private void UpdateOptionsFromFillColor()
        {
            var color = FillColorValue;
            switch (ActiveTool)
            {
                case EditorTool.Step:
                    Options.StepFillColor = color;
                    break;
                case EditorTool.SpeechBalloon:
                    Options.SpeechBalloonFillColor = color;
                    break;
                case EditorTool.Highlight:
                    Options.HighlightFillColor = color;
                    break;
                default:
                    Options.FillColor = color;
                    break;
            }
        }

        [ObservableProperty]
        private string _textColor = "#FF000000"; // Black by default

        public IBrush TextColorBrush
        {
            get => new SolidColorBrush(Color.Parse(TextColor));
            set
            {
                if (value is SolidColorBrush solidBrush)
                {
                    TextColor = $"#{solidBrush.Color.A:X2}{solidBrush.Color.R:X2}{solidBrush.Color.G:X2}{solidBrush.Color.B:X2}";
                }
            }
        }

        public Color TextColorValue
        {
            get => Color.Parse(TextColor);
            set => TextColor = $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}";
        }

        partial void OnTextColorChanged(string value)
        {
            OnPropertyChanged(nameof(TextColorBrush));
            OnPropertyChanged(nameof(TextColorValue));
            UpdateOptionsFromTextColor();
        }

        private void UpdateOptionsFromTextColor()
        {
            var color = TextColorValue;

            // Determine which option to update based on active tool or selected annotation
            if (ActiveTool == EditorTool.Step)
            {
                Options.StepTextColor = color;
            }
            else if (ActiveTool == EditorTool.SpeechBalloon)
            {
                Options.SpeechBalloonTextColor = color;
            }
            else if (ActiveTool == EditorTool.Text)
            {
                Options.TextTextColor = color;
            }
            else if (ActiveTool == EditorTool.Select && SelectedAnnotation != null)
            {
                if (SelectedAnnotation is NumberAnnotation)
                {
                    Options.StepTextColor = color;
                }
                else if (SelectedAnnotation is SpeechBalloonAnnotation)
                {
                    Options.SpeechBalloonTextColor = color;
                }
                else if (SelectedAnnotation is TextAnnotation)
                {
                    Options.TextTextColor = color;
                }
            }
        }

        [ObservableProperty]
        private float _fontSize = 30;

        partial void OnFontSizeChanged(float value)
        {
            bool isStep = ActiveTool == EditorTool.Step;
            bool isSpeechBalloon = ActiveTool == EditorTool.SpeechBalloon;

            if (ActiveTool == EditorTool.Select && SelectedAnnotation != null)
            {
                if (SelectedAnnotation is NumberAnnotation) isStep = true;
                if (SelectedAnnotation is SpeechBalloonAnnotation) isSpeechBalloon = true;
            }

            if (isStep)
            {
                Options.StepFontSize = value;
            }
            else if (isSpeechBalloon)
            {
                Options.SpeechBalloonFontSize = value;
            }
            else
            {
                Options.TextFontSize = value;
            }
        }

        private bool _isLoadingOptions;

        [ObservableProperty]
        private float _effectStrength = 1;

        private const float MinEffectStrength = 1;
        private const float MaxBlurStrength = 200;
        private const float MaxPixelateStrength = 200;
        private const float MaxMagnifyStrength = 10;
        private const float MaxSpotlightStrength = 100;

        public static float GetMaxEffectStrength(EditorTool tool) => tool switch
        {
            EditorTool.Blur => MaxBlurStrength,
            EditorTool.Pixelate => MaxPixelateStrength,
            EditorTool.Magnify => MaxMagnifyStrength,
            EditorTool.Spotlight => MaxSpotlightStrength,
            _ => 30
        };

        private EditorTool GetEffectiveStrengthTool()
        {
            if (ActiveTool == EditorTool.Select && _selectedAnnotation != null)
            {
                return _selectedAnnotation.ToolType;
            }

            return ActiveTool;
        }

        public float EffectStrengthMaximum => GetMaxEffectStrength(GetEffectiveStrengthTool());

        partial void OnEffectStrengthChanged(float value)
        {
            var clamped = Math.Clamp(value, MinEffectStrength, EffectStrengthMaximum);
            if (Math.Abs(clamped - value) > float.Epsilon)
            {
                EffectStrength = clamped;
                return;
            }

            // Don't write back to Options while we're loading values for a tool switch,
            // otherwise the clamped stale value overwrites the correct stored option.
            if (_isLoadingOptions)
            {
                return;
            }

            switch (GetEffectiveStrengthTool())
            {
                case EditorTool.Blur:
                    Options.BlurStrength = value;
                    break;
                case EditorTool.Pixelate:
                    Options.PixelateStrength = value;
                    break;
                case EditorTool.Magnify:
                    Options.MagnifierStrength = value;
                    break;
                case EditorTool.Spotlight:
                    Options.SpotlightStrength = value;
                    break;
            }
        }

        [ObservableProperty]
        private bool _shadowEnabled = true;

        partial void OnShadowEnabledChanged(bool value)
        {
            Options.Shadow = value;
        }

        [ObservableProperty]
        private bool _textBold = true;

        partial void OnTextBoldChanged(bool value)
        {
            Options.TextBold = value;
        }

        [ObservableProperty]
        private bool _textItalic;

        partial void OnTextItalicChanged(bool value)
        {
            Options.TextItalic = value;
        }

        [ObservableProperty]
        private bool _textUnderline;

        partial void OnTextUnderlineChanged(bool value)
        {
            Options.TextUnderline = value;
        }

        // Visibility computed properties based on ActiveTool
        public bool ShowBorderColor => ActiveTool switch
        {
            EditorTool.Select => _selectedAnnotation != null && _selectedAnnotation.ToolType switch
            {
                EditorTool.Rectangle or EditorTool.Ellipse or EditorTool.Line or EditorTool.Arrow or EditorTool.Freehand or EditorTool.SpeechBalloon or EditorTool.Text or EditorTool.Step => true,
                _ => false
            },
            EditorTool.Rectangle or EditorTool.Ellipse or EditorTool.Line or EditorTool.Arrow or EditorTool.Freehand or EditorTool.SpeechBalloon or EditorTool.Text or EditorTool.Step => true,
            _ => false
        };

        public bool ShowFillColor => ActiveTool switch
        {
            EditorTool.Rectangle or EditorTool.Ellipse or EditorTool.SpeechBalloon or EditorTool.Step or EditorTool.Highlight => true,
            EditorTool.Select => _selectedAnnotation != null && _selectedAnnotation.ToolType switch
            {
                EditorTool.Rectangle or EditorTool.Ellipse or EditorTool.SpeechBalloon or EditorTool.Step or EditorTool.Highlight => true,
                _ => false
            },
            _ => false
        };

        public bool ShowTextColor => ActiveTool switch
        {
            EditorTool.Text or EditorTool.SpeechBalloon or EditorTool.Step => true,
            EditorTool.Select => SelectedAnnotation?.ToolType switch
            {
                EditorTool.Text or EditorTool.SpeechBalloon or EditorTool.Step => true,
                _ => false
            },
            _ => false
        };

        public bool ShowThickness => ActiveTool switch
        {
            EditorTool.Rectangle or EditorTool.Ellipse or EditorTool.Line or EditorTool.Arrow
                or EditorTool.Freehand or EditorTool.SpeechBalloon or EditorTool.Step or EditorTool.SmartEraser or EditorTool.Text => true,
            EditorTool.Select => _selectedAnnotation != null && _selectedAnnotation.ToolType switch
            {
                EditorTool.Rectangle or EditorTool.Ellipse or EditorTool.Line or EditorTool.Arrow
                    or EditorTool.Freehand or EditorTool.SpeechBalloon or EditorTool.Step or EditorTool.SmartEraser or EditorTool.Text => true,
                _ => false
            },
            _ => false
        };

        public bool ShowFontSize => ActiveTool switch
        {
            EditorTool.Text or EditorTool.Step or EditorTool.SpeechBalloon => true,
            EditorTool.Select => _selectedAnnotation != null && _selectedAnnotation.ToolType switch
            {
                EditorTool.Text or EditorTool.Step or EditorTool.SpeechBalloon => true,
                _ => false
            },
            _ => false
        };

        public bool ShowStrength => ActiveTool switch
        {
            EditorTool.Blur or EditorTool.Pixelate or EditorTool.Magnify or EditorTool.Spotlight => true,
            EditorTool.Select => _selectedAnnotation != null && _selectedAnnotation.ToolType switch
            {
                EditorTool.Blur or EditorTool.Pixelate or EditorTool.Magnify or EditorTool.Spotlight => true,
                _ => false
            },
            _ => false
        };

        public bool ShowShadow => ActiveTool switch
        {
            EditorTool.Rectangle or EditorTool.Ellipse or EditorTool.Line or EditorTool.Arrow
                or EditorTool.Freehand or EditorTool.Text or EditorTool.SpeechBalloon or EditorTool.Step => true,
            EditorTool.Select => _selectedAnnotation != null && _selectedAnnotation.ToolType switch
            {
                EditorTool.Rectangle or EditorTool.Ellipse or EditorTool.Line or EditorTool.Arrow
                    or EditorTool.Freehand or EditorTool.Text or EditorTool.SpeechBalloon or EditorTool.Step => true,
                _ => false
            },
            _ => false
        };

        public bool ShowTextStyle => ActiveTool switch
        {
            EditorTool.Text => true,
            EditorTool.Select => _selectedAnnotation != null && _selectedAnnotation.ToolType switch
            {
                EditorTool.Text => true,
                _ => false
            },
            _ => false
        };

        /// <summary>
        /// Returns the Font Awesome icon codepoint for the active tool (or selected shape's tool in Select mode).
        /// </summary>
        public string ActiveToolIcon
        {
            get
            {
                var tool = ActiveTool;
                if (tool == EditorTool.Select && _selectedAnnotation != null)
                {
                    tool = _selectedAnnotation.ToolType;
                }

                return tool switch
                {
                    EditorTool.Select => "\uf25a",
                    EditorTool.Rectangle => "\uf0c8",
                    EditorTool.Ellipse => "\uf111",
                    EditorTool.Line => "\uf068",
                    EditorTool.Arrow => "\uf061",
                    EditorTool.Freehand => "\uf304",
                    EditorTool.Text => "\uf031",
                    EditorTool.SpeechBalloon => "\uf4ad",
                    EditorTool.Step => "\uf0cb",
                    EditorTool.Blur => "\uf043",
                    EditorTool.Pixelate => "\uf009",
                    EditorTool.Magnify => "\uf00e",
                    EditorTool.Spotlight => "\uf0eb",
                    EditorTool.SmartEraser => "\uf12d",
                    EditorTool.Highlight => "\uf591",
                    EditorTool.Crop => "\uf125",
                    EditorTool.CutOut => "\uf0c4",
                    _ => "\uf25a"
                };
            }
        }

        /// <summary>
        /// Returns the display name of the active tool (or selected shape's tool in Select mode).
        /// </summary>
        public string ActiveToolName
        {
            get
            {
                var tool = ActiveTool;
                if (tool == EditorTool.Select && _selectedAnnotation != null)
                {
                    tool = _selectedAnnotation.ToolType;
                }

                return tool switch
                {
                    EditorTool.Select => "Select",
                    EditorTool.Rectangle => "Rectangle",
                    EditorTool.Ellipse => "Ellipse",
                    EditorTool.Line => "Line",
                    EditorTool.Arrow => "Arrow",
                    EditorTool.Freehand => "Freehand",
                    EditorTool.Text => "Text",
                    EditorTool.SpeechBalloon => "Speech Balloon",
                    EditorTool.Step => "Step",
                    EditorTool.Blur => "Blur",
                    EditorTool.Pixelate => "Pixelate",
                    EditorTool.Magnify => "Magnify",
                    EditorTool.Spotlight => "Spotlight",
                    EditorTool.SmartEraser => "Smart Eraser",
                    EditorTool.Highlight => "Highlight",
                    EditorTool.Crop => "Crop",
                    EditorTool.CutOut => "Cut Out",
                    _ => "Select"
                };
            }
        }

        // Track selected annotation for Select tool visibility logic
        private Annotation? _selectedAnnotation;
        public Annotation? SelectedAnnotation
        {
            get => _selectedAnnotation;
            set
            {
                if (SetProperty(ref _selectedAnnotation, value))
                {
                    UpdateToolOptionsVisibility();
                }
            }
        }

        private void UpdateToolOptionsVisibility()
        {
            OnPropertyChanged(nameof(ShowBorderColor));
            OnPropertyChanged(nameof(ShowFillColor));
            OnPropertyChanged(nameof(ShowTextColor));
            OnPropertyChanged(nameof(ShowThickness));
            OnPropertyChanged(nameof(ShowFontSize));
            OnPropertyChanged(nameof(ShowStrength));
            OnPropertyChanged(nameof(ShowTextStyle));
            OnPropertyChanged(nameof(ShowShadow));
            OnPropertyChanged(nameof(ActiveToolIcon));
            OnPropertyChanged(nameof(ActiveToolName));
            OnPropertyChanged(nameof(EffectStrengthMaximum));
            OnPropertyChanged(nameof(ShowToolOptionsSeparator));
        }

        public bool ShowToolOptionsSeparator => ShowBorderColor || ShowFillColor || ShowTextColor || ShowThickness || ShowFontSize || ShowStrength || ShowTextStyle || ShowShadow;

        [ObservableProperty]
        private EditorTool _activeTool = EditorTool.Rectangle;

        partial void OnActiveToolChanged(EditorTool value)
        {
            _isLoadingOptions = true;
            try
            {
                // Update the maximum FIRST so the UI control (slider/numeric)
                // accepts the new tool's value range before we set the value.
                // Without this, the control clamps the new value to the OLD maximum
                // via its two-way binding (e.g. Spotlight default 15 clamped to
                // Magnify's old max of 10).
                OnPropertyChanged(nameof(EffectStrengthMaximum));

                LoadOptionsForTool(value);
                UpdateToolOptionsVisibility();
            }
            finally
            {
                _isLoadingOptions = false;
            }
        }

        private void LoadOptionsForTool(EditorTool tool)
        {
            // Prevent property change callbacks from overwriting options while loading
            // We can just set fields directly or use a flag, but setting properties is safer for UI updates.
            // However, setting properties triggers On...Changed which calls UpdateOptionsFrom...
            // Use a flag to suppress updates back to Options? 
            // Actually, if we set the property to the value from Options, updating Options back to the same value is harmless.

            switch (tool)
            {
                case EditorTool.Rectangle:
                case EditorTool.Ellipse:
                case EditorTool.Line:
                case EditorTool.Arrow:
                case EditorTool.Freehand:
                    SelectedColorValue = Options.BorderColor;
                    FillColorValue = Options.FillColor;
                    StrokeWidth = Options.Thickness;
                    ShadowEnabled = Options.Shadow;
                    FontSize = Options.TextFontSize;
                    break;
                case EditorTool.Text:
                    SelectedColorValue = Options.TextBorderColor;
                    TextColorValue = Options.TextTextColor;
                    StrokeWidth = Options.TextThickness;
                    ShadowEnabled = Options.Shadow;
                    FontSize = Options.TextFontSize;
                    TextBold = Options.TextBold;
                    TextItalic = Options.TextItalic;
                    TextUnderline = Options.TextUnderline;
                    break;
                case EditorTool.SpeechBalloon:
                    SelectedColorValue = Options.SpeechBalloonBorderColor;
                    FillColorValue = Options.SpeechBalloonFillColor;
                    TextColorValue = Options.SpeechBalloonTextColor;
                    StrokeWidth = Options.SpeechBalloonThickness;
                    ShadowEnabled = Options.Shadow;
                    FontSize = Options.SpeechBalloonFontSize;
                    TextBold = Options.TextBold;
                    TextItalic = Options.TextItalic;
                    TextUnderline = Options.TextUnderline;
                    break;
                case EditorTool.Step:
                    SelectedColorValue = Options.StepBorderColor;
                    FillColorValue = Options.StepFillColor;
                    TextColorValue = Options.StepTextColor;
                    StrokeWidth = Options.StepThickness;
                    ShadowEnabled = Options.Shadow;
                    FontSize = Options.StepFontSize;
                    TextBold = Options.TextBold;
                    TextItalic = Options.TextItalic;
                    TextUnderline = Options.TextUnderline;
                    break;
                case EditorTool.Highlight:
                    FillColorValue = Options.HighlightFillColor;
                    break;
                case EditorTool.SmartEraser:
                    StrokeWidth = Options.SmartEraserThickness;
                    break;
                case EditorTool.Blur:
                    EffectStrength = Options.BlurStrength;
                    break;
                case EditorTool.Pixelate:
                    EffectStrength = Options.PixelateStrength;
                    break;
                case EditorTool.Magnify:
                    EffectStrength = Options.MagnifierStrength;
                    break;
                case EditorTool.Spotlight:
                    EffectStrength = Options.SpotlightStrength;
                    break;
            }
        }

    }
}
