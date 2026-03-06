# XIP0039 ImageEditor Refactor Priorities
## Summary

Audit scope: `ImageEditor/src/ShareX.ImageEditor`.

Top three refactor pain points were selected by impact on regression risk, change cost, and code ownership clarity.

---

## Pain Point 1: Monolithic UI orchestration classes (P0)

### Evidence

- `ImageEditor/src/ShareX.ImageEditor/UI/ViewModels/MainViewModel.cs` is ~2,156 lines and mixes:
  - command orchestration (`ImageEditor/src/ShareX.ImageEditor/UI/ViewModels/MainViewModel.cs:1528`)
  - image ownership/lifecycle (`ImageEditor/src/ShareX.ImageEditor/UI/ViewModels/MainViewModel.cs:1693`)
  - effect preview/commit state machine (`ImageEditor/src/ShareX.ImageEditor/UI/ViewModels/MainViewModel.cs:1920`)
  - smart padding recursion control (`ImageEditor/src/ShareX.ImageEditor/UI/ViewModels/MainViewModel.cs:1081`)
- `ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs` is ~2,107 lines and mixes:
  - core event wiring and sync flags (`ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:57`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:80`)
  - UI rebuild/history sync (`ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:645`)
  - export/snapshot logic (`ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:821`)
  - effect dialog dispatch (`ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:1520`)

### Why it hurts

- Any non-trivial editor change touches high-fan-in files with unrelated responsibilities.
- Unit testing is constrained because state transitions are deeply coupled to Avalonia view lifecycle and callbacks.
- Fixes to one feature risk side effects in unrelated features.

### Refactor direction

1. Split `MainViewModel` into feature-oriented collaborators:
   - `ImageStateCoordinator`
   - `EffectPreviewSession`
   - `CanvasPresentationState`
2. Split `EditorView` into composable handlers:
   - `EditorCoreBridge`
   - `EditorClipboardHandler`
   - `EditorEffectsPanelHost`
3. Keep `EditorView.axaml.cs` as composition/wiring only.

### Acceptance criteria

- `MainViewModel.cs` and `EditorView.axaml.cs` each drop below ~900 lines.
- Effect flow, clipboard flow, and core sync each have isolated testable classes.
- No change in user-visible behavior for undo/redo, crop/cutout, and effects.

---

## Pain Point 2: Split-brain interaction architecture (P0)

### Evidence

- `EditorCore` contains a full pointer/input annotation pipeline (`ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:529`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:641`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:736`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:1293`).
- UI controllers also implement a second pointer/input pipeline with direct visual and annotation mutation:
  - `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:55`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:249`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:738`
  - `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorSelectionController.cs:105`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorSelectionController.cs:347`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorSelectionController.cs:1188`
- `EditorView` routes actual pointer input to the controller pipeline via `AnnotationCanvas`/`OverlayCanvas` (`ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml:483`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml:493`), not to `EditorCore.OnPointer*`.
- `EditorView` carries loop-prevention and reconciliation flags (`ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:57`) plus a sync-check stub (`ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:1592`) indicating persistent dual-state pressure.
- A separate `EditorCanvas` path still forwards pointer events directly to `EditorCore` (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controls/EditorCanvas.cs:117`) but is not integrated with the active `EditorView` host.

### Why it hurts

- Two overlapping interaction engines increase drift risk (selection, hit-test, resize, history behavior).
- Debugging state mismatch is expensive because both UI controls and core annotations can be "authoritative" depending on code path.
- Regression fixes become local patches instead of architectural fixes, especially around effect bounds and text editing.

### Refactor direction

1. Preserve the currently active `EditorView` interaction pipeline and make it explicit as the only supported host path:
   - `EditorView` pointer events -> `EditorInputController`/`EditorSelectionController` -> `AnnotationVisualFactory` visuals -> `EditorCore.AddAnnotation`/history.
2. Keep `EditorCore` as the state/history engine for annotations and image mutations, and keep Avalonia controls as the annotation rendering engine.
3. Decommission dormant/parallel paths that are not used by `EditorView` (notably direct `EditorCore.OnPointer*` host flow via `EditorCanvas`) after parity validation.
4. Add focused parity tests/snapshots for geometry, z-order, hit-testing, and export before any structural move in this area.

### Acceptance criteria

- `EditorView` host no longer has parallel interaction paths; one documented path is used for create/select/drag/resize.
- `EditorCanvas` direct-pointer host path is either removed or clearly marked non-production.
- Undo/redo restore still rebuilds UI from `EditorCore.Annotations` (`ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:645`) with no rendering regressions.
- Export output (`EditorView.GetSnapshot`) remains pixel-parity for all annotation families.

---

## Current Active Annotation Pipeline (Trace)

### Host-level source of truth (current behavior)

1. Runtime interaction truth: Avalonia controls on `AnnotationCanvas`/`OverlayCanvas` plus their attached `Annotation` model (`Tag` / control annotation property).
2. Persistence/history truth: `EditorCore._annotations` and `EditorHistory` mementos (`ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:1110`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:1158`).
3. Final annotation rendering truth: Avalonia visual tree; `EditorCore.Render` draws only source bitmap (`ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:1201`), and export composes the visual tree (`ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:821`).

### Per-annotation flow

| Annotation/tool | Create path | Update path | Commit/replay path | Current active SoT |
|---|---|---|---|---|
| Rectangle, Ellipse | `EditorInputController` tool switch (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:251`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:255`) | Pointer resize + selection resize/move (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:581`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorSelectionController.cs:509`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorSelectionController.cs:668`) | Add to core on release (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:740`), restore via `OnAnnotationsRestored` (`ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:645`) | Annotation instance shared between control `Tag` and `EditorCore._annotations`; visual comes from control |
| Line, Arrow | Create in tool switch (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:259`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:264`) | Live endpoints + `_shapeEndpoints` helper + annotation sync (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:595`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorSelectionController.cs:384`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorSelectionController.cs:585`) | Commit on release (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:740`) | `Annotation.StartPoint/EndPoint` are canonical; `_shapeEndpoints` is transient UI cache |
| Freehand, SmartEraser | Create path mode in switch (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:344`) | Points list append and geometry regen (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:491`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorSelectionController.cs:629`) | Commit on release (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:740`) | Annotation points list (`FreehandAnnotation.Points` / `SmartEraserAnnotation.Points`) |
| Text | Starts as temporary `TextBox` with `TextAnnotation` in `Tag` (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:1544`) | Existing text edits through `ShowTextEditor` and bounds observer (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorSelectionController.cs:1618`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorSelectionController.cs:1729`) | First commit on lost focus adds to core and replaces with persisted control (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:1635`) | Before first commit: temporary UI textbox; after commit: `TextAnnotation` model + `OutlinedTextControl` |
| SpeechBalloon | Create in switch (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:306`) | Resize/move/tail drag in selection controller (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorSelectionController.cs:407`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorSelectionController.cs:456`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorSelectionController.cs:605`) | Commit on release (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:740`) | `SpeechBalloonAnnotation` bound to `SpeechBalloonControl` (`Annotation` property + `Tag`) |
| Step (Number) | Create in switch (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:321`) | Grid visual + annotation sync on selection operations (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorSelectionController.cs:509`) | Commit on release (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:740`) | `NumberAnnotation` model is canonical for value/style/radius |
| Blur, Pixelate, Magnify, Highlight | Create in switch (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:290`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:294`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:298`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:302`) | Effect bitmap refresh during draw and move/resize (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:773`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:874`) | Commit on release (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:740`) | `BaseEffectAnnotation` bounds + generated `EffectBitmap`; control fill is derived |
| Spotlight | Create in switch (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:273`) | Update spotlight annotation bounds and control invalidation (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:610`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorSelectionController.cs:483`) | Commit on release (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:740`) | `SpotlightAnnotation` model + `SpotlightControl` canvas-sized visual |
| Image annotation | Created via image tool/paste/drop (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:1506`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:1617`) | Selection/move flows like other controls (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorSelectionController.cs:668`) | Added directly to core when inserted (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:1534`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:1648`) | `ImageAnnotation` (including bitmap payload) |
| Crop, CutOut | Transient overlay controls in input flow (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:205`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:216`) | Overlay-only until confirmation | Execute destructive image ops in core (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:825`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:1324`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:1425`) | `EditorCore.SourceImage` + adjusted core annotation list after operation |

### Plan constraint for Pain Point 2

Refactor work must preserve the active pipeline above and remove only dormant parallel paths. The plan must not migrate annotation rendering away from Avalonia visuals in this phase.

---

## No-Blind-Refactor Safety Scan (2026-02-23)

This additional scan covers eight risk surfaces that can silently break annotation behavior even if the refactor compiles.

### 1) Coordinate-space consistency (logical pixels vs DPI scaling)

Status: `RED`

Evidence:
- Effect creation path applies `RenderScaling` before writing `BaseEffectAnnotation` bounds (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:787`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:793`).
- Existing-effect update path writes bounds without `RenderScaling` (`ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:899`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:900`).
- Crop paths explicitly assume logical image-pixel coordinates with no scaling (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:1458`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:1146`).
- CutOut path applies `RenderScaling` (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:1480`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:1498`).

Risk:
- Same shape/effect can land at different model coordinates depending on which code path updated it.
- High-DPI behavior can diverge between Crop, CutOut, and effect annotations.

Guardrail to add:
- Define one coordinate contract: annotation model coordinates are always logical image pixels.
- Centralize pointer-to-annotation conversion and remove path-specific scaling math.

### 2) Destructive-operation transform completeness (Crop/CutOut)

Status: `RED`

Evidence:
- Crop adjusts generic `StartPoint/EndPoint` plus `Freehand`, effect bitmap, and spotlight canvas size (`ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:1381`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:1408`).
- CutOut adjustment logic handles generic coordinates, `Freehand`, and effects (`ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:1594`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:1691`).
- `SpeechBalloonAnnotation` has independent `TailPoint` geometry (`ImageEditor/src/ShareX.ImageEditor/Core/Annotations/Text/SpeechBalloonAnnotation.cs:13`) but Crop/CutOut adjusters do not update it.

Risk:
- Balloon tail and other type-specific geometry can drift after canvas mutations.
- Partial correctness by annotation type makes refactors brittle.

Guardrail to add:
- Introduce per-type geometry adjusters for destructive operations (`Crop`/`CutOut`) with explicit coverage of tail points, spotlight canvas size, and point-based annotations.

### 3) History selection restoration integrity

Status: `RED`

Evidence:
- `Annotation.Clone()` generates a new `Id` (`ImageEditor/src/ShareX.ImageEditor/Core/Annotations/Base/Annotation.cs:138`).
- History snapshots are built from clones (`ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:1151`).
- Restore logic tries to restore by saved `SelectedAnnotationId` (`ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:1178`).
- UI restore path clears selection unconditionally (`ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:662`).

Risk:
- Selection restoration is not reliable across undo/redo.
- Future refactors can mistakenly assume selection memento behavior is trustworthy.

Guardrail to add:
- Preserve IDs in memento clones (or store/restored-by-index) and reapply UI selection from core on `OnAnnotationsRestored`.

### 4) Arrow endpoint cache lifecycle (`_shapeEndpoints`)

Status: `RED`

Evidence:
- Arrow editing handles rely on `_shapeEndpoints` cache (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorSelectionController.cs:42`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorSelectionController.cs:706`).
- Cache is populated during draw flow via `RegisterArrowEndpoint` (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:268`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controllers/EditorInputController.cs:608`).
- Rebuild/paste/duplicate paths create controls through factory but do not repopulate arrow endpoint cache (`ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:665`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:1838`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:1997`).

Risk:
- Arrow endpoint handles and hit/hover behavior can degrade after restore/paste/duplicate.

Guardrail to add:
- Remove mutable endpoint cache as authoritative geometry and derive endpoints from `ArrowAnnotation` model, or rehydrate/clear cache deterministically on every lifecycle event.

### 5) Export/snapshot parity contract

Status: `AMBER`

Evidence:
- Core render path draws source image only (`ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:1201`).
- Full snapshot/export path depends on Avalonia visual tree render (`ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:821`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:843`).
- Overlay canvas is hidden for capture (`ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:829`).
- Fallback to `_editorCore.GetSnapshot()` drops annotations when `CanvasContainer` is unavailable (`ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:827`).

Risk:
- Different export behavior by host/control state.
- Refactor can accidentally route to source-only snapshot path.

Guardrail to add:
- Enforce one export contract: all user-visible exports must come from visual-tree composite path; fallback path must be explicit and test-guarded.

### 6) Host integration contract stability

Status: `AMBER`

Evidence:
- Desktop host invokes private `EditorView.InsertImageAnnotation` via reflection (`src/desktop/app/XerahS.UI/Views/MainWindow.axaml.cs:244`, `src/desktop/app/XerahS.UI/Views/MainWindow.axaml.cs:253`).
- Desktop host directly calls `EditorView.PerformCrop()` (`src/desktop/app/XerahS.UI/Views/MainWindow.Input.cs:47`), coupling host behavior to view internals (`ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:1133`).

Risk:
- Refactoring private/internal view methods can break host runtime behavior without compile-time safety.

Guardrail to add:
- Replace reflection/internal coupling with explicit public host API surface (`IEditorHostBridge` or equivalent).

### 7) Test coverage and disabled regression suites

Status: `RED`

Evidence:
- High-value history/rotate tests are excluded from compile (`tests/XerahS.Tests/XerahS.Tests.csproj:37`, `tests/XerahS.Tests/XerahS.Tests.csproj:38`).
- Excluded rotate tests target removed API names (example usage: `tests/XerahS.Tests/Editor/EditorRotateAnnotationsTests.cs:74`), while current core exposes `Rotate90Clockwise` (`ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:295`).
- ImageEditor audit baseline reports no test project execution (`ImageEditor/docs/audits/refactor-baseline.md:22`, `ImageEditor/docs/audits/refactor-baseline.md:29`).

Risk:
- Refactor safety is largely manual; regressions in transform/history/export can ship undetected.

Guardrail to add:
- Add active tests for the current `EditorView` pipeline and core history semantics; port disabled regression scenarios to current APIs.

### 8) Dormant parallel host path (`EditorCanvas`)

Status: `AMBER`

Evidence:
- `EditorCanvas` still implements direct pointer forwarding to core (`ImageEditor/src/ShareX.ImageEditor/UI/Views/Controls/EditorCanvas.cs:117`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/Controls/EditorCanvas.cs:153`).
- Active host routes pointer events through `EditorView` canvases/controllers (`ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml:483`, `ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml:493`).

Risk:
- Refactor can accidentally preserve two active interaction contracts.

Guardrail to add:
- Mark `EditorCanvas` as non-production/deprecated in this phase, or remove it after parity validation.

### Updated execution sequence (to avoid blind refactor)

1. Freeze coordinate contract and add conversion helpers shared by input/effect/crop/cutout paths.
2. Build mutation-adjustment table for all annotation families (including speech-balloon tail and spotlight canvas size).
3. Fix history selection restore semantics before splitting classes.
4. Replace arrow endpoint cache authority with model-derived geometry (or deterministic rehydration).
5. Introduce export parity tests (visual-tree snapshot as source of truth).
6. Add explicit host bridge API and remove reflection dependency.
7. Re-enable/port disabled regression tests to current APIs.
8. Decommission dormant `EditorCanvas` direct-pointer host path.

### Refactor gate checklist

- [ ] High-DPI (100/150/200%) geometry parity verified for rectangle, ellipse, line, arrow, freehand, text, speech balloon, spotlight, blur/pixelate/magnify/highlight.
- [ ] Crop/CutOut mutation tests cover all annotation families and assert geometry + export output.
- [ ] Undo/redo restores selected annotation deterministically.
- [ ] Arrow endpoint edit handles work after undo/redo, paste, duplicate, and reload.
- [ ] Export output parity snapshots exist for all annotation families.
- [ ] Host integration compiles/runs without reflection access to private view members.
- [ ] Dormant direct-core pointer path is removed or formally isolated from production flow.

---

## Cross-Repo Public Interface Impact (XerahS + ShareX)

Consumer roots covered in this scan:
- `C:\Users\liveu\source\repos\ShareX Team\XerahS`
- `C:\Users\liveu\source\repos\ShareX Team\ShareX`

### API Surface: `EditorCore` (public)

External consumer: `XerahS` region capture host.

Call sites:
- `src/desktop/app/XerahS.RegionCapture/ViewModels/RegionCaptureAnnotationViewModel.cs:47`
- `src/desktop/app/XerahS.RegionCapture/ViewModels/RegionCaptureAnnotationViewModel.cs:48`
- `src/desktop/app/XerahS.RegionCapture/ViewModels/RegionCaptureAnnotationViewModel.cs:49`
- `src/desktop/app/XerahS.RegionCapture/ViewModels/RegionCaptureAnnotationViewModel.cs:50`
- `src/desktop/app/XerahS.RegionCapture/ViewModels/RegionCaptureAnnotationViewModel.cs:135`
- `src/desktop/app/XerahS.RegionCapture/ViewModels/RegionCaptureAnnotationViewModel.cs:246`
- `src/desktop/app/XerahS.RegionCapture/ViewModels/RegionCaptureAnnotationViewModel.cs:295`
- `src/desktop/app/XerahS.RegionCapture/ViewModels/RegionCaptureAnnotationViewModel.cs:334`
- `src/desktop/app/XerahS.RegionCapture/ViewModels/RegionCaptureAnnotationViewModel.cs:336`
- `src/desktop/app/XerahS.RegionCapture/ViewModels/RegionCaptureAnnotationViewModel.cs:343`
- `src/desktop/app/XerahS.RegionCapture/ViewModels/RegionCaptureAnnotationViewModel.cs:345`
- `src/desktop/app/XerahS.RegionCapture/ViewModels/RegionCaptureAnnotationViewModel.cs:390`
- `src/desktop/app/XerahS.RegionCapture/ViewModels/RegionCaptureAnnotationViewModel.cs:398`
- `src/desktop/app/XerahS.RegionCapture/ViewModels/RegionCaptureAnnotationViewModel.cs:427`
- `src/desktop/app/XerahS.RegionCapture/UI/OverlayWindow.axaml.cs:331`
- `src/desktop/app/XerahS.RegionCapture/UI/OverlayWindow.axaml.cs:459`
- `src/desktop/app/XerahS.RegionCapture/UI/OverlayWindow.axaml.cs:476`
- `src/desktop/app/XerahS.RegionCapture/UI/OverlayWindow.axaml.cs:545`
- `src/desktop/app/XerahS.RegionCapture/UI/OverlayWindow.InlineText.cs:159`

Public members currently depended on:
- `EditorCore` ctor (`ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:161`)
- events: `InvalidateRequested`, `ImageChanged`, `EditAnnotationRequested`, `AnnotationsRestored`, `HistoryChanged` (`ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:63`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:70`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:71`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:76`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:81`)
- properties: `SourceImage`, `ActiveTool`, `StrokeColor`, `StrokeWidth`, `Annotations`, `SelectedAnnotation`, `CanUndo`, `CanRedo` (`ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:90`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:95`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:100`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:105`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:147`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:152`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:1127`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:1128`)
- methods: `LoadImage(SKBitmap)`, `OnPointerPressed`, `OnPointerMoved`, `OnPointerReleased`, `GetSnapshot`, `Undo`, `Redo`, `DeleteSelected`, `RemoveAnnotation`, `ClearAll` (`ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:169`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:529`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:641`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:736`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:1218`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:1130`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:1136`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:985`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:1068`, `ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorCore.cs:462`)

Compatibility decision:
- `MUST STAY SOURCE-COMPATIBLE` in XIP0039. Do not rename/remove these public members in this phase.

### API Surface: `EditorView` and host coupling

External consumer: `XerahS` desktop host.

Call sites:
- `src/desktop/app/XerahS.UI/Services/AvaloniaUIService.cs:120`
- `src/desktop/app/XerahS.UI/Services/AvaloniaUIService.cs:123`
- `src/desktop/app/XerahS.UI/Views/MainWindow.Navigation.cs:136`
- `src/desktop/app/XerahS.UI/Views/MainWindow.Input.cs:47`
- `src/desktop/app/XerahS.UI/Views/MainWindow.axaml.cs:244`

Public methods currently depended on:
- `EditorView.GetSnapshot()` (`ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:821`)
- `EditorView.PerformCrop()` (`ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:1133`)

Non-public but externally consumed (reflection):
- `EditorView.InsertImageAnnotation(SKBitmap, Point?)` via `BindingFlags.NonPublic` (`src/desktop/app/XerahS.UI/Views/MainWindow.axaml.cs:244`)

Compatibility decision:
- `GetSnapshot` and `PerformCrop` must remain callable through XIP0039.
- Reflection dependency is a hidden external contract and is the highest API fragility in XerahS.
- XIP0039 must introduce a formal public host API for image insertion and then migrate XerahS off reflection.

### API Surface: `MainViewModel` host-facing members

External consumer: `XerahS` application shell and services.

Call sites:
- `src/desktop/app/XerahS.UI/App.axaml.cs:72`
- `src/desktop/app/XerahS.UI/App.axaml.cs:73`
- `src/desktop/app/XerahS.UI/Services/AvaloniaUIService.cs:99`
- `src/desktop/app/XerahS.UI/Services/AvaloniaUIService.cs:100`
- `src/desktop/app/XerahS.UI/Services/AvaloniaUIService.cs:101`
- `src/desktop/app/XerahS.UI/Services/AvaloniaUIService.cs:113`
- `src/desktop/app/XerahS.UI/Views/MainWindow.axaml.cs:223`
- `src/desktop/app/XerahS.UI/Views/MainWindow.axaml.cs:478`
- `src/desktop/app/XerahS.UI/Services/WorkflowOrchestrator.cs:267`
- `src/desktop/app/XerahS.UI/Services/MainViewModelHelper.cs:42`
- `src/desktop/app/XerahS.UI/Services/MainViewModelHelper.cs:53`

Public members currently depended on:
- ctor `MainViewModel(EditorOptions? options = null)` (`ImageEditor/src/ShareX.ImageEditor/UI/ViewModels/MainViewModel.cs:968`)
- `UpdatePreview(SKBitmap image, bool clearAnnotations = true)` (`ImageEditor/src/ShareX.ImageEditor/UI/ViewModels/MainViewModel.cs:1693`)
- `PreviewImage` (`ImageEditor/src/ShareX.ImageEditor/UI/ViewModels/MainViewModel.cs:190`)
- events: `CopyRequested`, `UploadRequested` (`ImageEditor/src/ShareX.ImageEditor/UI/ViewModels/MainViewModel.cs:150`, `ImageEditor/src/ShareX.ImageEditor/UI/ViewModels/MainViewModel.cs:182`)
- host-configured UI members: `ShowCaptureToolbar`, `ApplicationName` (`ImageEditor/src/ShareX.ImageEditor/UI/ViewModels/MainViewModel.cs:73`, `ImageEditor/src/ShareX.ImageEditor/UI/ViewModels/MainViewModel.cs:962`)

Compatibility decision:
- `MUST STAY SOURCE-COMPATIBLE` for host wiring in XIP0039.
- Internal decomposition of `MainViewModel` is allowed only behind the existing public/events surface.

### API Surface: `AvaloniaIntegration` + `EditorEvents` (ShareX repo)

External consumer: `ShareX` WinForms host.

Call site:
- `..\ShareX\ShareX\TaskHelpers.cs:1342`

Public members currently depended on:
- `EditorEvents` delegate properties (`ImageEditor/src/ShareX.ImageEditor/AvaloniaIntegration.cs:55`)
- `AvaloniaIntegration.ShowEditorDialog(Stream, EditorEvents?, bool)` (`ImageEditor/src/ShareX.ImageEditor/AvaloniaIntegration.cs:119`)

Compatibility decision:
- `MUST STAY SOURCE/BINARY-COMPATIBLE` in XIP0039 for `C:\Users\liveu\source\repos\ShareX Team\ShareX`.

### API Surface: supporting shared helpers

External consumers: mostly `XerahS`.

Call sites:
- `src/desktop/app/XerahS.UI/App.axaml.cs:144`
- `src/desktop/app/XerahS.UI/Views/MainWindow.axaml.cs:71`
- `src/desktop/app/XerahS.UI/Views/MainWindow.axaml.cs:72`
- `src/desktop/app/XerahS.UI/Views/EditorWindow.axaml.cs:37`
- `src/desktop/app/XerahS.UI/Views/EditorWindow.axaml.cs:38`
- `src/desktop/app/XerahS.UI/Services/ThemeService.cs:55`
- `src/desktop/app/XerahS.UI/Services/MainViewModelHelper.cs:72`
- `src/desktop/app/XerahS.UI/Services/MainViewModelHelper.cs:133`
- `src/desktop/app/XerahS.RegionCapture/UI/OverlayWindow.axaml.cs:656`
- `src/desktop/app/XerahS.RegionCapture/UI/OverlayWindow.axaml.cs:674`

Public members currently depended on:
- `EditorServices.Clipboard` and `IClipboardService` (`ImageEditor/src/ShareX.ImageEditor/UI/Adapters/IClipboardService.cs:33`, `ImageEditor/src/ShareX.ImageEditor/UI/Adapters/IClipboardService.cs:55`)
- `ThemeManager.GetCurrentTheme`, `ThemeManager.SetTheme`, `ThemeManager.ThemeChanged`, `ThemeManager.ShareXDark`, `ThemeManager.ShareXLight` (`ImageEditor/src/ShareX.ImageEditor/Helpers/ThemeManager.cs:9`, `ImageEditor/src/ShareX.ImageEditor/Helpers/ThemeManager.cs:10`, `ImageEditor/src/ShareX.ImageEditor/Helpers/ThemeManager.cs:12`, `ImageEditor/src/ShareX.ImageEditor/Helpers/ThemeManager.cs:14`, `ImageEditor/src/ShareX.ImageEditor/Helpers/ThemeManager.cs:36`)
- `BitmapConversionHelpers.ToSKBitmap`, `BitmapConversionHelpers.ToAvaloniBitmap` (`ImageEditor/src/ShareX.ImageEditor/UI/Adapters/BitmapConversionHelpers.cs:18`, `ImageEditor/src/ShareX.ImageEditor/UI/Adapters/BitmapConversionHelpers.cs:81`)
- `AnnotationVisualFactory.CreateVisualControl`, `AnnotationVisualFactory.UpdateVisualControl` (`ImageEditor/src/ShareX.ImageEditor/UI/Adapters/AnnotationVisuals/AnnotationVisualFactory.cs:53`, `ImageEditor/src/ShareX.ImageEditor/UI/Adapters/AnnotationVisuals/AnnotationVisualFactory.cs:78`)
- `EditorTool` enum (`ImageEditor/src/ShareX.ImageEditor/Core/Editor/EditorTool.cs:31`)

Compatibility decision:
- Keep these APIs and namespaces stable for XIP0039, or provide compatibility wrappers if classes are moved.

### Interface impact policy for XIP0039

1. No breaking signature changes for externally consumed public APIs above.
2. If internals are moved/split, keep type names and namespaces (or add forwarding shims) until both host repos migrate.
3. Replace reflection-based `InsertImageAnnotation` usage with a formal public host API in a compatibility-first way.

---

## Pain Point 3: Effect dialog duplication and non-scalable effect wiring (P1)

### Evidence

- Effect dialogs repeat near-identical event patterns:
  - `ImageEditor/src/ShareX.ImageEditor/UI/Views/Dialogs/IEffectDialog.cs:9`
  - `ImageEditor/src/ShareX.ImageEditor/UI/Views/Dialogs/BrightnessDialog.axaml.cs:11`
  - `ImageEditor/src/ShareX.ImageEditor/UI/Views/Dialogs/ContrastDialog.axaml.cs:10`
  - similar pattern repeated across >20 dialog files.
- `EditorView` uses many one-off dispatch methods for effects (`ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:1520`) and a generic-but-manual runtime hookup (`ImageEditor/src/ShareX.ImageEditor/UI/Views/EditorView.axaml.cs:1547`).
- Dialogs repeatedly use direct control lookups (`FindControl`) and local preview wiring, creating repetitive boilerplate and inconsistency risk.

### Why it hurts

- Adding or changing one effect requires touching both per-dialog code and central view dispatch.
- Consistency bugs (preview timing, cancel behavior, status text, validation) are likely across dialogs.
- Maintenance cost scales linearly with number of effects.

### Refactor direction

1. Introduce effect metadata descriptors (id, display name, parameter schema, factory).
2. Replace one-off dialog handlers with registry-driven effect panel routing.
3. Add shared `EffectDialogBase<TParams>` or generated parameter editor controls for common slider/color patterns.

### Acceptance criteria

- New effect integration requires descriptor + effect implementation, without new `EditorView` handler methods.
- Common preview/apply/cancel behavior is centralized.
- Dialog boilerplate reduced significantly across existing effects.

---

## Priority Order

1. P0: Monolithic UI orchestration classes
2. P0: Split-brain interaction architecture
3. P1: Effect dialog duplication

## Out of Scope for This XIP

- New image effect algorithms
- Visual redesign/theming changes
- Cross-repo integration changes outside `ImageEditor`