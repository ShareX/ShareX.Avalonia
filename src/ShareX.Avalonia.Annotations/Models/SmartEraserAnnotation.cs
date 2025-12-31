using Avalonia;
using Avalonia.Media;

namespace ShareX.Ava.Annotations.Models;

/// <summary>
/// Smart Eraser annotation - intended to remove content (placeholder)
/// </summary>
public class SmartEraserAnnotation : FreehandAnnotation
{
    public SmartEraserAnnotation()
    {
        ToolType = EditorTool.SmartEraser;
        StrokeColor = "#80FF0000"; // Translucent red to verify area
        StrokeWidth = 10;
    }

    // Inherits Freehand rendering (drawing the path)
    // The "Action" of erasing will be handled by the Editor applying the "Erasure" to the underlying image 
    // and removing this annotation probably? 
    // Or this annotation remains and renders "Inpainted content" (similar to Blur).
    // For now, it stays as a "Mask" that shows what will be erased.
}
