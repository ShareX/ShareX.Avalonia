# XIP0004 Annotation Editor - Canvas & Decoupling

XIP0004: Annotation Editor - Canvas & Decoupling

**Status**: Complete ?  
**Priority**: HIGH - Core ShareX Feature  
**Assignees**: Copilot (Canvas), Codex (Decoupling)  
**Branches**: `feature/annotation-canvas`, `feature/editor-decoupling`  
**Area**: Image Editor / Annotations  
**Original Documents**: XIP0004a + XIP0004b merged into this issue

---

## Overview

XIP-0004 implements a fully decoupled, reusable image annotation editor for XerahS. This includes both the annotation canvas with drawing tools and the architectural decoupling into a standalone DLL.

---

## Part A: Annotation Canvas (XIP0004a)

### Objective
Implement the core image annotation tools for the `AnnotationCanvas` control - the heart of ShareX's image editor.

### Architectural Vision
> **EditorView must be designed as a reusable component**, not coupled to any specific window or context.

#### Use Cases
1. **New Screenshot ? Editor**: When a screenshot is captured, it immediately opens in `EditorView`
2. **History ? Editor**: User right-clicks a historical screenshot and selects "Edit Image"

#### Design Requirements
- `EditorView.axaml` and `EditorViewModel` are **self-contained** and **portable**
- Support **multiple simultaneous instances** (edit multiple images at once)
- `EditorViewModel` accepts image via constructor: `EditorViewModel(Image image, string? filePath = null)`
- **No coupling** to main window or specific parent containers
- Works for fresh screenshots and historical images

### Implementation

#### ? Shape Architecture (COMPLETE)
Jaex implemented:
- Base `Annotation` class with `Render()` and `HitTest()` methods
- 5 core tools: `RectangleAnnotation`, `EllipseAnnotation`, `ArrowAnnotation`, `TextAnnotation`, `HighlightAnnotation`
- Additional tools: `LineAnnotation`, `FreehandAnnotation`, `BlurAnnotation`, `PixelateAnnotation`, `NumberAnnotation`, etc.
- `EditorTool` enum for tool selection
- Color management and rendering helpers

**Location**: `src/XerahS.Annotations/Models/`

#### UI Canvas Control
Created `ShareX.Editor` project (replaced `AnnotationCanvas.axaml` requirement):
- **Rendering**: `DrawingContext` in `OnRender()` to draw all annotations
- **Mouse Interaction**: Handle mouse down/move/up for drawing
- **Tool Selection**: Rectangle, Ellipse, Arrow, Text, Highlighter
- **Color/Width Picker**: UI for stroke color and width

#### Interaction Features
- **Selection**: Click to select annotations, show selection handles
- **Resize/Move**: Drag handles to resize, drag body to move
- **Delete**: Delete key to remove selected annotation
- **Undo/Redo**: Command stack for undo/redo operations

---

## Part B: Editor Decoupling (XIP0004b)

### Objective
Decouple the Editor functionality into a standalone `ShareX.Editor` DLL that can be consumed by both XerahS and potentially other projects.

### Scope
1. **Extract Editor Logic**: Move all editor-related code to `ShareX.Editor` project
2. **Remove Dependencies**: Ensure `ShareX.Editor` does not depend on XerahS-specific infrastructure
3. **Refactor References**: Update `XerahS` to reference the new DLL

### Deliverables
- ? `ShareX.Editor` project created
- ? Code moved and refactored
- ? `XerahS` builds successfully with new reference

---

## Final Architecture

```
???????????????????????????????????????????????????????????
?                    Applications                         ?
?  ???????????????  ???????????????  ???????????????????  ?
?  ?   XerahS    ?  ?  Other App  ?  ?   ShareX.Legacy ?  ?
?  ???????????????  ???????????????  ???????????????????  ?
???????????????????????????????????????????????????????????
          ?                ?                  ?
          ?????????????????????????????????????
                             ?
              ???????????????????????????????
              ?       ShareX.Editor         ?
              ?  ???????????????????????    ?
              ?  ?  EditorView.axaml   ?    ?
              ?  ?  EditorViewModel    ?    ?
              ?  ?  Annotation Tools   ?    ?
              ?  ?  Undo/Redo System   ?    ?
              ?  ???????????????????????    ?
              ???????????????????????????????
                             ?
              ???????????????????????????????
              ?     XerahS.Annotations      ?
              ?    (Shape Definitions)      ?
              ???????????????????????????????
```

---

## Key Design Principles

1. **Reusability**: `EditorView` is standalone and portable
2. **Multi-Instance**: Multiple editors can run simultaneously
3. **MVVM**: Business logic in ViewModel, canvas handles rendering
4. **Performance**: `InvalidateVisual()` used judiciously
5. **No Tight Coupling**: No dependencies on MainWindow

---

## Files Created/Modified

### New Projects
- `src/ShareX.Editor/ShareX.Editor.csproj` - Standalone editor DLL

### Core Files
- `EditorView.axaml` / `EditorView.axaml.cs` - Main editor UI
- `EditorViewModel.cs` - Editor logic and state
- `AnnotationCanvasViewModel.cs` - Canvas-specific ViewModel

### Annotations (XerahS.Annotations)
- `Annotation.cs` - Base annotation class
- `RectangleAnnotation.cs`, `EllipseAnnotation.cs`, etc. - Shape implementations
- `EditorTool.cs` - Tool enumeration

---

## Original Documents Merged

| Document | Focus | Status |
|----------|-------|--------|
| XIP0004a_Annotation_Canvas.md | Canvas implementation | Complete |
| XIP0004b_Editor_Decoupling.md | DLL decoupling | Complete |

---

## Conclusion

**Status: ? COMPLETE**

The annotation editor is fully implemented as a reusable, decoupled component:
- All annotation tools functional
- Editor works for new screenshots and history images
- `ShareX.Editor` DLL is standalone and reusable
- Multiple editor instances supported
- Integration with XerahS complete

**Result**: `ShareX.Editor` DLL created and decoupled, satisfying the requirement for a reusable editor component.


---

## Legacy content from `XIP0004-annotation-editor-canvas-decoupling.md`

# XIP0004 Annotation Editor - Canvas & Decoupling

XIP0004: Annotation Editor - Canvas & Decoupling

**Status**: Complete ?  
**Priority**: HIGH - Core ShareX Feature  
**Assignees**: Copilot (Canvas), Codex (Decoupling)  
**Branches**: `feature/annotation-canvas`, `feature/editor-decoupling`  
**Area**: Image Editor / Annotations  
**Original Documents**: XIP0004a + XIP0004b merged into this issue

---

## Overview

XIP-0004 implements a fully decoupled, reusable image annotation editor for XerahS. This includes both the annotation canvas with drawing tools and the architectural decoupling into a standalone DLL.

---

## Part A: Annotation Canvas (XIP0004a)

### Objective
Implement the core image annotation tools for the `AnnotationCanvas` control - the heart of ShareX's image editor.

### Architectural Vision
> **EditorView must be designed as a reusable component**, not coupled to any specific window or context.

#### Use Cases
1. **New Screenshot ? Editor**: When a screenshot is captured, it immediately opens in `EditorView`
2. **History ? Editor**: User right-clicks a historical screenshot and selects "Edit Image"

#### Design Requirements
- `EditorView.axaml` and `EditorViewModel` are **self-contained** and **portable**
- Support **multiple simultaneous instances** (edit multiple images at once)
- `EditorViewModel` accepts image via constructor: `EditorViewModel(Image image, string? filePath = null)`
- **No coupling** to main window or specific parent containers
- Works for fresh screenshots and historical images

### Implementation

#### ? Shape Architecture (COMPLETE)
Jaex implemented:
- Base `Annotation` class with `Render()` and `HitTest()` methods
- 5 core tools: `RectangleAnnotation`, `EllipseAnnotation`, `ArrowAnnotation`, `TextAnnotation`, `HighlightAnnotation`
- Additional tools: `LineAnnotation`, `FreehandAnnotation`, `BlurAnnotation`, `PixelateAnnotation`, `NumberAnnotation`, etc.
- `EditorTool` enum for tool selection
- Color management and rendering helpers

**Location**: `src/XerahS.Annotations/Models/`

#### UI Canvas Control
Created `ShareX.Editor` project (replaced `AnnotationCanvas.axaml` requirement):
- **Rendering**: `DrawingContext` in `OnRender()` to draw all annotations
- **Mouse Interaction**: Handle mouse down/move/up for drawing
- **Tool Selection**: Rectangle, Ellipse, Arrow, Text, Highlighter
- **Color/Width Picker**: UI for stroke color and width

#### Interaction Features
- **Selection**: Click to select annotations, show selection handles
- **Resize/Move**: Drag handles to resize, drag body to move
- **Delete**: Delete key to remove selected annotation
- **Undo/Redo**: Command stack for undo/redo operations

---

## Part B: Editor Decoupling (XIP0004b)

### Objective
Decouple the Editor functionality into a standalone `ShareX.Editor` DLL that can be consumed by both XerahS and potentially other projects.

### Scope
1. **Extract Editor Logic**: Move all editor-related code to `ShareX.Editor` project
2. **Remove Dependencies**: Ensure `ShareX.Editor` does not depend on XerahS-specific infrastructure
3. **Refactor References**: Update `XerahS` to reference the new DLL

### Deliverables
- ? `ShareX.Editor` project created
- ? Code moved and refactored
- ? `XerahS` builds successfully with new reference

---

## Final Architecture

```
???????????????????????????????????????????????????????????
?                    Applications                         ?
?  ???????????????  ???????????????  ???????????????????  ?
?  ?   XerahS    ?  ?  Other App  ?  ?   ShareX.Legacy ?  ?
?  ???????????????  ???????????????  ???????????????????  ?
???????????????????????????????????????????????????????????
          ?                ?                  ?
          ?????????????????????????????????????
                             ?
              ???????????????????????????????
              ?       ShareX.Editor         ?
              ?  ???????????????????????    ?
              ?  ?  EditorView.axaml   ?    ?
              ?  ?  EditorViewModel    ?    ?
              ?  ?  Annotation Tools   ?    ?
              ?  ?  Undo/Redo System   ?    ?
              ?  ???????????????????????    ?
              ???????????????????????????????
                             ?
              ???????????????????????????????
              ?     XerahS.Annotations      ?
              ?    (Shape Definitions)      ?
              ???????????????????????????????
```

---

## Key Design Principles

1. **Reusability**: `EditorView` is standalone and portable
2. **Multi-Instance**: Multiple editors can run simultaneously
3. **MVVM**: Business logic in ViewModel, canvas handles rendering
4. **Performance**: `InvalidateVisual()` used judiciously
5. **No Tight Coupling**: No dependencies on MainWindow

---

## Files Created/Modified

### New Projects
- `src/ShareX.Editor/ShareX.Editor.csproj` - Standalone editor DLL

### Core Files
- `EditorView.axaml` / `EditorView.axaml.cs` - Main editor UI
- `EditorViewModel.cs` - Editor logic and state
- `AnnotationCanvasViewModel.cs` - Canvas-specific ViewModel

### Annotations (XerahS.Annotations)
- `Annotation.cs` - Base annotation class
- `RectangleAnnotation.cs`, `EllipseAnnotation.cs`, etc. - Shape implementations
- `EditorTool.cs` - Tool enumeration

---

## Original Documents Merged

| Document | Focus | Status |
|----------|-------|--------|
| XIP0004a_Annotation_Canvas.md | Canvas implementation | Complete |
| XIP0004b_Editor_Decoupling.md | DLL decoupling | Complete |

---

## Conclusion

**Status: ? COMPLETE**

The annotation editor is fully implemented as a reusable, decoupled component:
- All annotation tools functional
- Editor works for new screenshots and history images
- `ShareX.Editor` DLL is standalone and reusable
- Multiple editor instances supported
- Integration with XerahS complete

**Result**: `ShareX.Editor` DLL created and decoupled, satisfying the requirement for a reusable editor component.

---

## Legacy content from `XIP0004a_Annotation_Canvas.md`

# CP01: Annotation Canvas (Phase 2)

## Priority
**HIGH** - Core ShareX Feature

## Assignee
**Copilot** (Surface Laptop 7, VS2026 IDE)

## Branch
`feature/annotation-canvas`

## Status
Complete - Verified on 2026-01-08

## Assessment
100% Complete. The goal was achieved by creating `ShareX.Editor` instead of `AnnotationCanvas.axaml`.

## Instructions
**CRITICAL**: You must START by creating (or checking out if it exists) the branch `feature/annotation-canvas`. Do not work on `main`.

## Objective
Implement the core image annotation tools for the `AnnotationCanvas` control. This is the heart of ShareX's image editor.
Ref: `ShareX.ScreenCaptureLib/Shapes`

## Architectural Vision

> [!IMPORTANT]
> **EditorView must be designed as a reusable component**, not coupled to any specific window or context.

### Use Cases
1. **New Screenshot ΓåÆ Editor**: When a screenshot is captured, it immediately opens in `EditorView`
2. **History ΓåÆ Editor**: When user right-clicks a historical screenshot and selects "Edit Image", a new instance of `EditorView` opens with that image

### Design Requirements
- `EditorView.axaml` and `EditorViewModel` must be **self-contained** and **portable**
- Support **multiple simultaneous instances** (user can edit multiple images at once in separate windows/tabs)
- The `EditorViewModel` should accept an image via constructor (e.g., `EditorViewModel(Image image, string? filePath = null)`)
- **Do not couple** the editor to the main window or any specific parent container
- The view should work equally well for:
  - Fresh screenshots (new image, no file path)
  - Historical images (existing image, known file path)

## Scope

### 1. Γ£à Shape Architecture (COMPLETE)
Jaex has implemented:
- Base `Annotation` class with `Render()` and `HitTest()` methods
- All 5 core tools: `RectangleAnnotation`, `EllipseAnnotation`, `ArrowAnnotation`, `TextAnnotation`, `HighlightAnnotation`
- Additional tools: `LineAnnotation`, `FreehandAnnotation`, `BlurAnnotation`, `PixelateAnnotation`, `NumberAnnotation`, etc.
- `EditorTool` enum for tool selection
- Color management and rendering helpers

**Location**: `src/XerahS.Annotations/Models/`

### 2. UI Canvas Control (NEW FOCUS)
Create the Avalonia UI control to host and interact with annotations:
- **Create `ShareX.Editor` project** (Replaces `AnnotationCanvas.axaml` requirement)
- **Rendering**: Use `DrawingContext` in `OnRender()` to draw all annotations
- **Mouse Interaction**: Handle mouse down/move/up for drawing new annotations
- **Tool Selection**: Wire up tool switching (Rectangle, Ellipse, Arrow, Text, Highlighter)
- **Color/Width Picker**: UI elements for stroke color and width

### 3. Interaction Features
- **Selection**: Click to select annotations, show selection handles
- **Resize/Move**: Drag handles to resize, drag body to move
- **Delete**: Delete key to remove selected annotation
- **Undo/Redo**: Command stack for undo/redo operations

### 4. ViewModel Integration
- Create `AnnotationCanvasViewModel` or integrate into existing editor ViewModel
- Expose `ObservableCollection<Annotation>` for data binding
- Command bindings for tool selection, undo/redo, delete

## Guidelines
- **Reuse Models**: Use the existing `XerahS.Annotations` models, don't recreate them
- **Performance**: Use `InvalidateVisual()` only when needed to minimize redraws
- **MVVM**: Keep business logic in ViewModel, canvas handles rendering and input
- **Portability**: Design `EditorView` as a standalone, reusable component that can be instantiated in different contexts (new screenshots, history editing)
- **No Tight Coupling**: Avoid dependencies on MainWindow or specific parent containers

## Deliverables
- Functional `ShareX.Editor` project (Verified)
- Integration with existing UI
- Basic undo/redo support


---

## Legacy content from `XIP0004b_Editor_Decoupling.md`

# SIP0014: Editor Decoupling

## Priority
**HIGH** - Architecture Refactoring

## Assignee
**Codex**

## Branch
`feature/editor-decoupling` (merged)

## Status
Complete - Verified on 2026-01-08

## Assessment
100% Complete. `ShareX.Editor` DLL is created and decoupled. This satisfies the requirement for a reusable editor component.

## Objective
Decouple the Editor functionality into a standalone `ShareX.Editor` DLL that can be consumed by both XerahS and potentially other projects.

## Scope
1.  **Extract Editor Logic**: Move all editor-related code (annotations, tools, canvas) to a new project `ShareX.Editor`.
2.  **Remove Dependencies**: Ensure `ShareX.Editor` does not depend on `XerahS` specific infrastructure where possible.
3.  **Refactor References**: Update `XerahS` to reference the new DLL.

## Deliverables
- Γ£à `ShareX.Editor` project created
- Γ£à Code moved and refactored
- Γ£à `XerahS` builds successfully with new reference