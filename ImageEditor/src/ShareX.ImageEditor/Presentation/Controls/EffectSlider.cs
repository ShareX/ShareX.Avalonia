using Avalonia;
using Avalonia.Controls;
using System.Globalization;

namespace ShareX.ImageEditor.Presentation.Controls
{
    public class EffectSlider : Slider
    {
        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<EffectSlider, string>(
                nameof(Label),
                defaultValue: string.Empty);

        public static readonly StyledProperty<string> ValueStringFormatProperty =
            AvaloniaProperty.Register<EffectSlider, string>(
                nameof(ValueStringFormat),
                defaultValue: "{}{0:0.##}");

        public static readonly DirectProperty<EffectSlider, string> ValueTextProperty =
            AvaloniaProperty.RegisterDirect<EffectSlider, string>(
                nameof(ValueText),
                o => o.ValueText);

        private string _valueText = "0";

        public string Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public string ValueStringFormat
        {
            get => GetValue(ValueStringFormatProperty);
            set => SetValue(ValueStringFormatProperty, value);
        }

        public string ValueText
        {
            get => _valueText;
            private set => SetAndRaise(ValueTextProperty, ref _valueText, value);
        }

        static EffectSlider()
        {
            ValueProperty.Changed.AddClassHandler<EffectSlider>((s, e) => s.UpdateValueText());
            ValueStringFormatProperty.Changed.AddClassHandler<EffectSlider>((s, e) => s.UpdateValueText());
        }

        public EffectSlider()
        {
            UpdateValueText();
        }

        private void UpdateValueText()
        {
            string format = NormalizeFormat(ValueStringFormat);

            try
            {
                ValueText = string.Format(CultureInfo.CurrentCulture, format, Value);
            }
            catch (FormatException)
            {
                ValueText = Value.ToString("0.##", CultureInfo.CurrentCulture);
            }
        }

        private static string NormalizeFormat(string? format)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                return "{0:0.##}";
            }

            if (format.StartsWith("{}", StringComparison.Ordinal))
            {
                return format[2..];
            }

            return format;
        }
    }
}
