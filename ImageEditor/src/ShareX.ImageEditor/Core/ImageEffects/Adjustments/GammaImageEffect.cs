using SkiaSharp;


namespace ShareX.ImageEditor.Core.ImageEffects.Adjustments;

public class GammaImageEffect : AdjustmentImageEffect
{
    public override string Name => "Gamma";
    public override string IconKey => "IconWaveSquare";
    public float Amount { get; set; } = 1f;

    private float _cachedAmount = float.NaN;
    private byte[]? _cachedTable;

    public override SKBitmap Apply(SKBitmap source)
    {
        if (_cachedTable is null || _cachedAmount != Amount)
        {
            _cachedAmount = Amount;
            _cachedTable = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                float val = i / 255f;
                float corrected = (float)Math.Pow(val, 1.0 / Amount);
                _cachedTable[i] = (byte)(Math.Max(0, Math.Min(1, corrected)) * 255);
            }
        }

        using var filter = SKColorFilter.CreateTable(null, _cachedTable, _cachedTable, _cachedTable);
        return ApplyColorFilter(source, filter);
    }
}

