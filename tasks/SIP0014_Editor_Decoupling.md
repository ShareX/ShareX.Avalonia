# SIP0014: Decouple Editor as Shared DLL

## Status
**✅ COMPLETE** (Avalonia Integration) | **⏸️ PENDING** (WinForms Integration)

## Branch
`feature/editor-decoupling` (merged)

---

## Summary

The Editor functionality has been successfully decoupled into a standalone `ShareX.Editor` DLL that can be consumed by both ShareX.Avalonia and ShareX (WinForms).

### What Was Done

| Phase | Status | Description |
|-------|--------|-------------|
| Phase 1 | ✅ | Created `ShareX.Editor` class library with SkiaSharp rendering |
| Phase 2 | ✅ | Moved 20 annotation models from `ShareX.Avalonia.Annotations` |
| Phase 3 | ✅ | Moved 51 ImageEffects files from `ShareX.Avalonia.ImageEffects` |
| Phase 4 | ✅ | Integrated with `ShareX.Avalonia.UI`, removed old projects |
| Phase 5 | ✅ | Verified full solution build and application runs |
| Phase 6 | ⏸️ | WinForms integration (future work) |

---

## Current Project Structure

```
ShareX.Avalonia/
├── ShareX.Editor/                     # Sibling repo
│   └── src/ShareX.Editor/
│       ├── ShareX.Editor.csproj       # net10.0-windows
│       ├── Annotations/               # 20 files (moved from ShareX.Avalonia.Annotations)
│       │   ├── Annotation.cs          # Base class
│       │   ├── EditorTool.cs          # Enum
│       │   ├── ArrowAnnotation.cs
│       │   ├── BlurAnnotation.cs
│       │   ├── RectangleAnnotation.cs
│       │   └── ... (15 more)
│       ├── ImageEffects/              # 51 files (moved from ShareX.Avalonia.ImageEffects)
│       │   ├── ImageEffect.cs         # Base class
│       │   ├── ImageEffectsProcessing.cs
│       │   ├── Adjustments/           # Alpha, Brightness, Contrast, etc.
│       │   ├── Drawings/              # DrawText, DrawImage, etc.
│       │   ├── Filters/               # Blur, Pixelate, Sharpen, etc.
│       │   └── Manipulations/         # Crop, Resize, Rotate, etc.
│       └── Serialization/
│           └── AnnotationSerializer.cs
│
└── src/
    ├── ShareX.Avalonia.UI/            # References ShareX.Editor
    ├── ShareX.Avalonia.Common/        # Referenced by ShareX.Editor
    └── ... (other projects)
```

### Removed Projects
- ~~`src/ShareX.Avalonia.Annotations/`~~ → Moved to `ShareX.Editor/Annotations/`
- ~~`src/ShareX.Avalonia.ImageEffects/`~~ → Moved to `ShareX.Editor/ImageEffects/`

---

## Namespace Changes

| Old Namespace | New Namespace |
|---------------|---------------|
| `ShareX.Ava.Annotations.Models` | `ShareX.Editor.Annotations` |
| `ShareX.Ava.Annotations.Serialization` | `ShareX.Editor.Serialization` |
| `ShareX.Ava.ImageEffects` | `ShareX.Editor.ImageEffects` |
| `ShareX.Ava.ImageEffects.Helpers` | `ShareX.Editor.ImageEffects` |

---

## Dependencies

### ShareX.Editor
```xml
<PackageReference Include="Avalonia" Version="11.3.10" />
<PackageReference Include="SkiaSharp" Version="3.119.1" />
<PackageReference Include="Newtonsoft.Json" Version="..." />
<ProjectReference Include="...\ShareX.Avalonia.Common.csproj" />
```

---

## Phase 6: WinForms Integration (Future Work)

> [!IMPORTANT]
> This section provides detailed instructions for a future developer to integrate `ShareX.Editor` with the original ShareX WinForms application.

### Objective
Enable the "Edit image..." functionality in ShareX (WinForms) to use the new `ShareX.Editor` DLL for annotation editing, providing a unified editing experience across both applications.

### Prerequisites
1. `ShareX.Editor` DLL must be built (either locally or from NuGet)
2. ShareX WinForms project must target `net9.0` or later (currently `net9.0-windows`)

### Step-by-Step Integration

#### Step 1: Add ShareX.Editor Reference

Add the `ShareX.Editor` project reference to the ShareX WinForms project:

```bash
cd ShareX
dotnet add ShareX/ShareX.csproj reference ../ShareX.Editor/src/ShareX.Editor/ShareX.Editor.csproj
```

Or if publishing as NuGet package:
```bash
dotnet add package ShareX.Editor
```

#### Step 2: Add Required Dependencies

ShareX.Editor uses Avalonia's `DrawingContext` for rendering. WinForms will need to:

**Option A: Host Avalonia Control (Recommended)**
```bash
dotnet add package Avalonia.Win32 --version 11.3.10
```

Create a WinForms host control that embeds an Avalonia rendering surface.

**Option B: Use SkiaSharp.Views.WindowsForms**
```bash
dotnet add package SkiaSharp.Views.WindowsForms
```

Render the editor to an `SKControl` and convert the final SKBitmap to GDI+ Bitmap.

#### Step 3: Create Editor Host Form

Create a new form `EditorForm.cs` to host the editor:

```csharp
// ShareX/Forms/EditorForm.cs
using ShareX.Editor.Annotations;
using ShareX.Editor.ImageEffects;
using SkiaSharp;

public partial class EditorForm : Form
{
    private List<Annotation> _annotations = new();
    private SKBitmap _sourceImage;
    private EditorTool _currentTool = EditorTool.Select;

    public EditorForm(string imagePath)
    {
        InitializeComponent();
        LoadImage(imagePath);
    }

    public EditorForm(Bitmap image)
    {
        InitializeComponent();
        LoadImage(image);
    }

    private void LoadImage(string path)
    {
        using var stream = File.OpenRead(path);
        _sourceImage = SKBitmap.Decode(stream);
    }

    private void LoadImage(Bitmap gdiImage)
    {
        // Convert GDI+ Bitmap to SKBitmap
        using var stream = new MemoryStream();
        gdiImage.Save(stream, ImageFormat.Png);
        stream.Position = 0;
        _sourceImage = SKBitmap.Decode(stream);
    }

    public Bitmap GetEditedImage()
    {
        // Apply annotations and return as GDI+ Bitmap
        using var surface = SKSurface.Create(new SKImageInfo(_sourceImage.Width, _sourceImage.Height));
        var canvas = surface.Canvas;
        
        // Draw base image
        canvas.DrawBitmap(_sourceImage, 0, 0);
        
        // Draw annotations (using Avalonia DrawingContext or direct SKCanvas)
        foreach (var annotation in _annotations)
        {
            // Render annotation to canvas
            // Note: May need adapter to convert DrawingContext calls to SKCanvas
        }
        
        // Convert to GDI+ Bitmap
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var ms = new MemoryStream(data.ToArray());
        return new Bitmap(ms);
    }
}
```

#### Step 4: Update TaskHelpers.AnnotateImageFromFile

Modify the existing entry point to use the new editor:

```csharp
// ShareX/TaskHelpers.cs
public static void AnnotateImageFromFile(string filePath, TaskSettings taskSettings = null)
{
    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
    {
        if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

        // NEW: Use ShareX.Editor-based form
        using var editorForm = new EditorForm(filePath);
        if (editorForm.ShowDialog() == DialogResult.OK)
        {
            using var editedImage = editorForm.GetEditedImage();
            // Handle the edited image (save, upload, etc.)
            HandleEditedImage(editedImage, filePath, taskSettings);
        }
    }
    else
    {
        MessageBox.Show("File does not exist:" + Environment.NewLine + filePath, 
            "ShareX", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
```

#### Step 5: Rendering Adapter (Critical)

Since `ShareX.Editor` annotations use Avalonia's `DrawingContext`, you need an adapter to render to `SKCanvas`:

```csharp
// ShareX/Helpers/SkiaDrawingContextAdapter.cs
using Avalonia.Media;
using SkiaSharp;

public class SkiaDrawingContextAdapter : DrawingContext
{
    private readonly SKCanvas _canvas;
    
    public SkiaDrawingContextAdapter(SKCanvas canvas)
    {
        _canvas = canvas;
    }
    
    public override void DrawLine(IPen pen, Point p1, Point p2)
    {
        using var paint = ConvertPen(pen);
        _canvas.DrawLine(
            (float)p1.X, (float)p1.Y,
            (float)p2.X, (float)p2.Y,
            paint
        );
    }
    
    public override void DrawRectangle(IBrush brush, IPen pen, Rect rect, ...)
    {
        var skRect = new SKRect((float)rect.X, (float)rect.Y, 
                                (float)rect.Right, (float)rect.Bottom);
        
        if (brush != null)
        {
            using var fillPaint = ConvertBrush(brush);
            _canvas.DrawRect(skRect, fillPaint);
        }
        
        if (pen != null)
        {
            using var strokePaint = ConvertPen(pen);
            _canvas.DrawRect(skRect, strokePaint);
        }
    }
    
    // ... implement other DrawingContext methods
    
    private SKPaint ConvertPen(IPen pen) { ... }
    private SKPaint ConvertBrush(IBrush brush) { ... }
}
```

#### Step 6: Testing

1. Build ShareX solution
2. Open ShareX WinForms application
3. Go to History → Right-click any image → "Edit image..."
4. Verify the new editor opens
5. Test annotation tools (Rectangle, Arrow, Text, Blur)
6. Save and verify annotations are applied

### Known Considerations

1. **Avalonia Dependency**: `ShareX.Editor` depends on Avalonia for `DrawingContext`. Consider abstracting this if you want to avoid Avalonia runtime in WinForms.

2. **Performance**: Converting between SKBitmap and GDI+ Bitmap on every save is acceptable for editor use cases.

3. **Feature Parity**: The existing `RegionCaptureForm` has more features (screen capture, region selection). The new editor is for **image annotation only**.

---

## Related Documents
- [AGENTS.md](../AGENTS.md) - Code style rules
- `ShareX.ScreenCaptureLib/Shapes/ShapeManager.cs` - Original WinForms implementation reference
