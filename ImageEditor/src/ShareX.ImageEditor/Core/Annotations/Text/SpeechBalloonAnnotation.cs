using SkiaSharp;

namespace ShareX.ImageEditor.Core.Annotations;

/// <summary>
/// Speech Balloon annotation with tail
/// </summary>
public partial class SpeechBalloonAnnotation : Annotation
{
    public override AnnotationCategory Category => AnnotationCategory.Text;
    /// <summary>
    /// Tail point (absolute position)
    /// </summary>
    public SKPoint TailPoint { get; set; }

    /// <summary>
    /// Optional text content inside the balloon
    /// </summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// Font size for the balloon text
    /// </summary>
    public float FontSize { get; set; } = 20;

    /// <summary>
    /// Text body color
    /// </summary>
    public string TextColor { get; set; } = "#FF000000";


    /// <summary>
    /// Background color (hex) - defaults to white for speech balloon
    /// </summary>
    public SpeechBalloonAnnotation()
    {
        ToolType = EditorTool.SpeechBalloon;
        StrokeWidth = 2;
        StrokeColor = "#FF000000";
        FillColor = "#FFFFFFFF"; // Default to white
    }



    public override SKRect GetBounds()
    {
        return new SKRect(
            Math.Min(StartPoint.X, EndPoint.X),
            Math.Min(StartPoint.Y, EndPoint.Y),
            Math.Max(StartPoint.X, EndPoint.X),
            Math.Max(StartPoint.Y, EndPoint.Y));
    }

    public override bool HitTest(SKPoint point, float tolerance = 5)
    {
        var bounds = GetBounds();
        // Include tail in hit area by expanding to cover the tail point
        bounds = SKRect.Union(bounds, new SKRect(
            TailPoint.X - tolerance,
            TailPoint.Y - tolerance,
            TailPoint.X + tolerance,
            TailPoint.Y + tolerance));
        var inflated = SKRect.Inflate(bounds, tolerance, tolerance);
        return inflated.Contains(point);
    }
}
