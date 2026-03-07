using SkiaSharp;

namespace ShareX.ImageEditor.Core.ImageEffects.Filters;

public class TornEdgeImageEffect : FilterImageEffect
{
    public int Depth { get; set; }
    public int Range { get; set; }
    public bool Top { get; set; }
    public bool Right { get; set; }
    public bool Bottom { get; set; }
    public bool Left { get; set; }
    public bool Curved { get; set; }

    public override string Name => "Torn edge";

    public TornEdgeImageEffect(int depth, int range, bool top, bool right, bool bottom, bool left, bool curved)
    {
        Depth = depth;
        Range = range;
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
        Curved = curved;
    }

    public override SKBitmap Apply(SKBitmap source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (Depth < 1 || Range < 1) return source.Copy();
        if (!Top && !Right && !Bottom && !Left) return source.Copy();

        int horizontalTornCount = source.Width / Range;
        int verticalTornCount = source.Height / Range;

        if (horizontalTornCount < 2 && verticalTornCount < 2)
        {
            return source.Copy();
        }

        List<SKPoint> points = new List<SKPoint>();
        var rand = Random.Shared;

        // Top edge
        if (Top && horizontalTornCount > 1)
        {
            int startX = (Left && verticalTornCount > 1) ? Depth : 0;
            int endX = (Right && verticalTornCount > 1) ? source.Width - Depth : source.Width;
            for (int x = startX; x < endX; x += Range)
            {
                int y = rand.Next(0, Depth + 1);
                points.Add(new SKPoint(x, y));
            }
        }
        else
        {
            points.Add(new SKPoint(0, 0));
            points.Add(new SKPoint(source.Width, 0));
        }

        // Right edge
        if (Right && verticalTornCount > 1)
        {
            int startY = (Top && horizontalTornCount > 1) ? Depth : 0;
            int endY = (Bottom && horizontalTornCount > 1) ? source.Height - Depth : source.Height;
            for (int y = startY; y < endY; y += Range)
            {
                int x = rand.Next(0, Depth + 1);
                points.Add(new SKPoint(source.Width - Depth + x, y));
            }
        }
        else
        {
            points.Add(new SKPoint(source.Width, 0));
            points.Add(new SKPoint(source.Width, source.Height));
        }

        // Bottom edge (reverse direction)
        if (Bottom && horizontalTornCount > 1)
        {
            int startX = (Right && verticalTornCount > 1) ? source.Width - Depth : source.Width;
            int endX = (Left && verticalTornCount > 1) ? Depth : 0;
            for (int x = startX; x >= endX; x -= Range)
            {
                int y = rand.Next(0, Depth + 1);
                points.Add(new SKPoint(x, source.Height - Depth + y));
            }
        }
        else
        {
            points.Add(new SKPoint(source.Width, source.Height));
            points.Add(new SKPoint(0, source.Height));
        }

        // Left edge (reverse direction)
        if (Left && verticalTornCount > 1)
        {
            int startY = (Bottom && horizontalTornCount > 1) ? source.Height - Depth : source.Height;
            int endY = (Top && horizontalTornCount > 1) ? Depth : 0;
            for (int y = startY; y >= endY; y -= Range)
            {
                int x = rand.Next(0, Depth + 1);
                points.Add(new SKPoint(x, y));
            }
        }
        else
        {
            points.Add(new SKPoint(0, source.Height));
            points.Add(new SKPoint(0, 0));
        }

        // Remove duplicates and ensure clean polygon
        var distinctPoints = new List<SKPoint>();
        if (points.Count > 0)
        {
            distinctPoints.Add(points[0]);
            for (int i = 1; i < points.Count; i++)
            {
                if (points[i] != points[i - 1])
                    distinctPoints.Add(points[i]);
            }
            // If the last point is same as first, remove it to avoid double-closure
            if (distinctPoints.Count > 1 && distinctPoints[^1] == distinctPoints[0])
            {
                distinctPoints.RemoveAt(distinctPoints.Count - 1);
            }
        }
        var pts = distinctPoints.ToArray();

        SKBitmap result = new SKBitmap(source.Width, source.Height);
        using SKCanvas canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        // Create shader from source bitmap
        using SKShader shader = SKShader.CreateBitmap(source, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);
        using SKPaint paint = new SKPaint
        {
            Shader = shader,
            IsAntialias = true
        };

        using SKPath path = new SKPath();
        if (pts.Length > 2)
        {
            if (Curved)
            {
                // Create curved path using quad bezier approximation on closed loop
                // To avoid the "first and last connected with line" issue, we ensure full cycle
                var lastPt = pts[^1];
                var firstPt = pts[0];
                var currentMid = new SKPoint((lastPt.X + firstPt.X) / 2, (lastPt.Y + firstPt.Y) / 2);

                path.MoveTo(currentMid);

                for (int i = 0; i < pts.Length; i++)
                {
                    // Use modulo to wrap around to 0
                    var pCurrent = pts[i];
                    var pNext = pts[(i + 1) % pts.Length];

                    var nextMid = new SKPoint((pCurrent.X + pNext.X) / 2, (pCurrent.Y + pNext.Y) / 2);

                    path.QuadTo(pCurrent, nextMid);

                    currentMid = nextMid;
                }
                path.Close();
            }
            else
            {
                path.AddPoly(pts, true);
            }

            canvas.DrawPath(path, paint);
        }

        return result;
    }
}

