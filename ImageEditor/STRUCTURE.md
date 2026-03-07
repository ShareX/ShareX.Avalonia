# ShareX.ImageEditor — Structure Map

This document is the authoritative reference for navigating and extending the library.
It is written for coding agents and human contributors equally.

---

## Namespace-to-Path Contract

**Every type's file path is deterministically computable from its namespace.**

```
namespace ShareX.ImageEditor.X.Y.Z
→ src/ShareX.ImageEditor/X/Y/Z/TypeName.cs
```

Examples:
- `ShareX.ImageEditor.Hosting.ImageEditorOptions` → `Hosting/ImageEditorOptions.cs`
- `ShareX.ImageEditor.Core.ImageEffects.Filters.BlurImageEffect` → `Core/ImageEffects/Filters/BlurImageEffect.cs`
- `ShareX.ImageEditor.Presentation.Views.Dialogs.BlurDialog` → `Presentation/Views/Dialogs/BlurDialog.axaml.cs`
- `ShareX.ImageEditor.Presentation.Controllers.EditorInputController` → `Presentation/Controllers/EditorInputController.cs`

**Rule for AXAML views:** append `.axaml.cs` instead of `.cs`.
**Rule for Styles/ResourceDictionary files:** no `.cs` at all (e.g. `EffectSlider.axaml`, `ShareXTheme.axaml`).

Skip grep for type lookups. Compute the path directly.

---

## Top-Level Buckets

```
src/ShareX.ImageEditor/
├── Hosting/          Host-facing API and service contracts
├── Core/             Platform-agnostic editor engine
└── Presentation/     Avalonia UI — views, controls, rendering, theming
```

---

## Hosting/

Entry points and contracts consumed by host applications (XerahS, ShareX).

| File | Role |
|------|------|
| `AvaloniaIntegration.cs` | Static launcher — `ShowEditorDialog(...)` |
| `ImageEditorOptions.cs` | Options passed from host to editor |
| `EditorServices.cs` | Service wiring for DI |
| `IClipboardService.cs` | Clipboard abstraction for host to implement |
| `EditorHostExample.cs` | Reference integration example |
| `Diagnostics/EditorDiagnostics.cs` | Logging and tracing hooks |

**To integrate the editor into a new host:** read `AvaloniaIntegration.cs` and `ImageEditorOptions.cs` only.

---

## Core/

Platform-agnostic. No Avalonia references. Safe to unit-test without UI.

### Annotations

All annotation types are enumerated in `Core/Annotations/Base/Annotation.cs` via
`[JsonDerivedType]` attributes — that file is the authoritative annotation inventory.

Sub-folders group annotations by category (mirroring `AnnotationCategory`):

```
Core/Annotations/
├── Base/             Annotation.cs (base + full type inventory)
│                     IPointBasedAnnotation.cs
├── Effects/          BaseEffectAnnotation.cs + Blur, Highlight, Magnify, Pixelate, Spotlight
├── Shapes/           Arrow, Crop, CutOut, Ellipse, Freehand, Image, Line, Rectangle, SmartEraser
└── Text/             Number, SpeechBalloon, Text
```

`AnnotationCategory.cs` in `Core/Annotations/` enumerates the three categories.
Each concrete `Annotation` subclass implements `Category` to return its category.

**To add a new annotation type:**
1. Create the class file in the appropriate sub-folder.
2. Add `[JsonDerivedType]` to `Annotation.cs`.
3. Add a visual partial in `Presentation/Rendering/AnnotationVisuals/`.
4. Register in `Presentation/Rendering/AnnotationVisuals/AnnotationVisualFactory.cs`.

### ImageEffects

All effect categories are enumerated in `Core/ImageEffects/ImageEffectCategory.cs`.

```
Core/ImageEffects/
├── ImageEffect.cs            Abstract root base
├── ImageEffectCategory.cs    Category enum (Manipulations, Adjustments, Filters, Drawings)
├── Adjustments/              AdjustmentImageEffect.cs + ~26 concrete effects
├── Filters/                  FilterImageEffect.cs + ~65 concrete effects
├── Drawings/                 ~6 drawing effects (extend root ImageEffect)
├── Manipulations/            ~13 manipulation effects (extend root ImageEffect)
└── Helpers/                  ConvolutionHelper, ImageHelpers, ProceduralEffectHelper, TypeExtensions
```

**Base class disambiguation:** `AdjustmentImageEffect` and `FilterImageEffect` are the
per-category abstract bases. `ImageEffect.cs` (root) is the single true base class.
Glob `**/ImageEffect.cs` returns exactly one result.

**To add a new image effect:**
1. Create the class in the appropriate sub-folder, extending the category base.
2. Add a dialog in `Presentation/Views/Dialogs/`.
3. Register the dialog factory in `Presentation/Views/Dialogs/EffectDialogRegistry.cs`.
4. Add a menu item in the effects menu calling `RaiseDialog("your_id")`.

### Editor / History / Abstractions

```
Core/Editor/          EditorCore.cs, EditorTool.cs
Core/History/         EditorHistory.cs, EditorMemento.cs
Core/Abstractions/    IAnnotationToolbarAdapter.cs
```

---

## Presentation/

All Avalonia UI code. Depends on Core; never referenced by Core.

### Registry / Inventory Files

These files are the authoritative source for what exists in their domain.
Read them first when discovering what's registered, not the individual type files.

| File | Inventories |
|------|-------------|
| `Presentation/Rendering/AnnotationVisuals/AnnotationVisualFactory.cs` | All annotation → visual mappings (31 entries) |
| `Presentation/Views/Dialogs/EffectDialogRegistry.cs` | All effect → dialog factory mappings (107 entries) |

### Folder Map

```
Presentation/
├── Controllers/      EditorInputController, EditorSelectionController, EditorZoomController
├── Controls/         Custom Avalonia controls (EffectSlider, ColorPickerDropdown, etc.)
│                     EffectSlider.axaml  ← Styles file, no .cs codebehind
├── Converters/       Value converters for XAML bindings
├── Rendering/        SkiaSharp drawing helpers + annotation visual partials
│   └── AnnotationVisuals/  One .Visual.cs per annotation type
├── Theming/          ThemeManager.cs
│                     ShareXTheme.axaml  ← ResourceDictionary, no .cs codebehind
├── ViewModels/       MainViewModel (split into 4 partials), ConfirmationDialogViewModel
└── Views/
    ├── Dialogs/      One .axaml + .axaml.cs per effect dialog (~107 dialogs)
    │                 EffectDialogRegistry.cs  ← dialog inventory
    │                 IEffectDialog.cs
    ├── EditorView.axaml.cs           Main editor surface
    ├── EditorView.ClipboardHandler.cs  ← clipboard partial
    ├── EditorView.CoreBridge.cs        ← Core event wiring partial
    ├── EditorView.EffectsHost.cs       ← effects menu partial
    ├── EditorView.ToolbarHandlers.cs   ← toolbar partial
    ├── EditorWindow.axaml.cs           Window shell
    └── ConfirmationDialogView.axaml.cs
```

### Partial Class Conventions

`EditorView` and `MainViewModel` are split into named partials.
Glob `EditorView.*.cs` or `MainViewModel.*.cs` to find all partials for a class.

| Partial suffix | Responsibility |
|----------------|----------------|
| `ClipboardHandler` | Cut/copy/paste from host clipboard |
| `CoreBridge` | EditorCore event subscriptions and callbacks |
| `EffectsHost` | Image effects menu and dialog orchestration |
| `ToolbarHandlers` | Annotation toolbar button handlers |
| `CanvasState` | Canvas pan/zoom/selection state |
| `EffectPreview` | Live effect preview pipeline |
| `ImageState` | Source bitmap management |
| `ToolOptions` | Per-tool property bindings |

---

## Assets/

Font Awesome 7 Free and any other embedded resources.
Referenced via `avares://ShareX.ImageEditor/Assets#...` in AXAML.
