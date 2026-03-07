using SkiaSharp;


namespace ShareX.ImageEditor.Core.ImageEffects.Adjustments;

public enum SelectiveColorRange
{
    Reds,
    Yellows,
    Greens,
    Cyans,
    Blues,
    Magentas,
    Whites,
    Neutrals,
    Blacks
}

public struct SelectiveColorAdjustment
{
    public float Hue;
    public float Saturation;
    public float Lightness;

    public SelectiveColorAdjustment(float h, float s, float l)
    {
        Hue = h;
        Saturation = s;
        Lightness = l;
    }
}

public class SelectiveColorImageEffect : AdjustmentImageEffect
{
    public override string Name => "Selective Color";
    public override string IconKey => "IconHighlighter";

    public Dictionary<SelectiveColorRange, SelectiveColorAdjustment> Adjustments { get; set; } = new();

    public override SKBitmap Apply(SKBitmap source)
    {
        if (Adjustments == null || Adjustments.Count == 0) return source.Copy();

        var adjArray = new SelectiveColorAdjustment[9];
        bool hasChanges = false;
        foreach (var kvp in Adjustments)
        {
            adjArray[(int)kvp.Key] = kvp.Value;
            if (kvp.Value.Hue != 0 || kvp.Value.Saturation != 0 || kvp.Value.Lightness != 0)
            {
                hasChanges = true;
            }
        }

        if (!hasChanges) return source.Copy();

        return ApplyPixelOperation(source, (c) =>
        {
            c.ToHsl(out float h, out float s, out float l);

            // Determine range preference: Whites/Blacks/Neutrals first
            SelectiveColorRange? range = null;

            // Simplified HSL range detection matching ImageHelpers legacy logic
            if (l > 80) range = SelectiveColorRange.Whites;
            else if (l < 20) range = SelectiveColorRange.Blacks;
            else if (s < 10) range = SelectiveColorRange.Neutrals;
            else
            {
                // Hue based
                float hDeg = h;
                if (hDeg >= 330 || hDeg <= 30) range = SelectiveColorRange.Reds;
                else if (hDeg >= 30 && hDeg < 90) range = SelectiveColorRange.Yellows;
                else if (hDeg >= 90 && hDeg < 150) range = SelectiveColorRange.Greens;
                else if (hDeg >= 150 && hDeg < 210) range = SelectiveColorRange.Cyans;
                else if (hDeg >= 210 && hDeg < 270) range = SelectiveColorRange.Blues;
                else if (hDeg >= 270 && hDeg < 330) range = SelectiveColorRange.Magentas;
            }

            if (range.HasValue)
            {
                ref var adj = ref adjArray[(int)range.Value];
                if (adj.Hue != 0 || adj.Saturation != 0 || adj.Lightness != 0)
                {
                    h = (h + adj.Hue) % 360;
                    if (h < 0) h += 360;

                    s = Math.Clamp(s + adj.Saturation, 0, 100);
                    l = Math.Clamp(l + adj.Lightness, 0, 100);

                    return SKColor.FromHsl(h, s, l, c.Alpha);
                }
            }

            return c;
        });
    }
}

