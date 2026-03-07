# ShareX.ImageEditor

ShareX.ImageEditor is the editor library used by ShareX and XerahS. It combines a framework-agnostic editing engine with an Avalonia presentation layer and a small host-facing integration surface.

## Structure

- `src/ShareX.ImageEditor/Hosting`
  Host entry points and configuration for embedding the editor in an app.
  Examples: `AvaloniaIntegration`, `ImageEditorOptions`, `EditorServices`, diagnostics, clipboard contracts.
- `src/ShareX.ImageEditor/Core`
  Framework-agnostic editor logic.
  Examples: annotations, editor state, history, image effects, abstractions.
- `src/ShareX.ImageEditor/Presentation`
  Avalonia-only UI, rendering, controls, dialogs, themes, and view models.
- `src/ShareX.ImageEditor.Loader`
  Development host for running the editor standalone.

## Navigation Guide

- Start in `Hosting` when you need the public host API or options surface.
- Start in `Core` when you need editor behavior, history, annotations, or effects.
- Start in `Presentation` when you need Avalonia views, input, rendering, or theme work.

## Notes

- Phase 1 of the structure refactor keeps namespaces stable and moves files mechanically.
- Phase 2 aligns namespaces with the `Hosting`, `Core`, and `Presentation` layout.
