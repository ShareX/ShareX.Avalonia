using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ShareX.ImageEditor.Presentation.Controls
{
    public partial class StrengthSlider : UserControl
    {
        public static readonly StyledProperty<float> SelectedStrengthProperty =
            AvaloniaProperty.Register<StrengthSlider, float>(
                nameof(SelectedStrength),
                defaultValue: 10);

        public static readonly StyledProperty<float> MinimumProperty =
            AvaloniaProperty.Register<StrengthSlider, float>(
                nameof(Minimum),
                defaultValue: 1);

        public static readonly StyledProperty<float> MaximumProperty =
            AvaloniaProperty.Register<StrengthSlider, float>(
                nameof(Maximum),
                defaultValue: 30);

        public float SelectedStrength
        {
            get => GetValue(SelectedStrengthProperty);
            set => SetValue(SelectedStrengthProperty, value);
        }

        public float Minimum
        {
            get => GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public float Maximum
        {
            get => GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public event EventHandler<float>? StrengthChanged;

        static StrengthSlider()
        {
            SelectedStrengthProperty.Changed.AddClassHandler<StrengthSlider>((s, e) =>
            {
                s.StrengthChanged?.Invoke(s, s.SelectedStrength);
            });
        }

        public StrengthSlider()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
