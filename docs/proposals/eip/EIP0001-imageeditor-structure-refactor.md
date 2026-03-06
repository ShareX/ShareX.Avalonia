# EIP0001: ImageEditor Structure Refactor

## Summary
- Treat `ShareX.ImageEditor` as the single source of truth library repo. XerahS and ShareX both consume that upstream as submodules.
- Normalize the XerahS submodule path from `ImageEditor/` to `ShareX.ImageEditor/`.
- Restructure the library around three agent-friendly buckets:
  - `Hosting`
  - `Core`
  - `Presentation`
- Execute the work in distinct phases so path changes, file moves, namespace changes, and host migrations stay reviewable and buildable.

## Motivation
- `Helpers` and `UI/Adapters` currently mix host integration, diagnostics, rendering helpers, converters, and presentation code.
- Important host-facing types such as `ImageEditorOptions` are difficult to locate because folders do not describe ownership or runtime role.
- Namespace layout does not consistently match file layout, which slows code navigation and automated edits.

## Goals
- Make host API, editor core, and Avalonia presentation responsibilities obvious from the folder structure.
- Improve discoverability for both human contributors and coding agents.
- Keep behavior stable while making the structure and namespaces coherent.

## Non-Goals
- No redesign of editor behavior or rendering architecture.
- No long-term compatibility shim layer for old namespaces.
- No attempt to make the legacy `EditorCanvas` part of the active editor path.

## Phase 0: Submodule Identity Normalization
- Rename the XerahS submodule path from `ImageEditor/` to `ShareX.ImageEditor/`.
- Update XerahS references, solution entries, docs, and path-based conditions that depend on the old submodule path.
- Keep ShareX unchanged at the repo-path level because its tracked submodule is already `ShareX.ImageEditor/`.

## Phase 1: Mechanical Structure Move
- Replace ambiguous library folders with explicit responsibility buckets:
  - `Hosting/` for host-facing entry points and service contracts
  - `Core/` for editor engine, history, annotations, abstractions, and image effects
  - `Presentation/` for Avalonia views, controls, dialogs, view models, converters, rendering helpers, and theming
- Move files only in this phase. Keep behavior and namespaces unchanged.
- Remove `EditorViewModel` because it has no live consumers.
- Move `EditorCanvas` under a clearly marked legacy presentation location.
- Add a short structure map to library documentation.

## Phase 2: Namespace Cleanup and Host Migration
- Align namespaces to the new structure:
  - `ShareX.ImageEditor.Hosting`
  - `ShareX.ImageEditor.Hosting.Diagnostics`
  - `ShareX.ImageEditor.Core.*`
  - `ShareX.ImageEditor.Presentation.*`
- Move host-facing API types into `Hosting`:
  - `ImageEditorOptions`
  - `AvaloniaIntegration`
  - `EditorEvents`
  - `EditorServices`
  - `IClipboardService`
- Move Avalonia-only helpers, rendering utilities, adapters, and converters into `Presentation.*`.
- Update all `using` directives and XAML namespace mappings in both host repos.
- Remove `EditorCanvas` entirely if it remains unused after the migration.

## Public API Changes
- The intentional breaking change is namespace cleanup, not behavioral redesign.
- XerahS repo paths change from `ImageEditor/...` to `ShareX.ImageEditor/...`.
- ShareX host paths stay the same at the repo level, but its imports must follow the new namespaces.
- Library changes should be made once in the upstream library and then both superprojects should move to the same library commit.

## Verification
- After Phase 0:
  - Build XerahS projects that reference the renamed submodule path.
- After Phase 1 and Phase 2:
  - Build `ShareX.ImageEditor.sln` with `-m:1`.
  - Build XerahS:
    - `src/desktop/app/XerahS.UI/XerahS.UI.csproj`
    - `src/desktop/app/XerahS.RegionCapture/XerahS.RegionCapture.csproj`
    - `src/desktop/core/XerahS.Core/XerahS.Core.csproj`
  - Build ShareX:
    - `ShareX/ShareX.csproj`
- Smoke scenarios:
  - XerahS editor open, load, and save
  - XerahS crop via `Enter`
  - XerahS add-image-as-annotation flow
  - XerahS region-capture/editor integration
  - ShareX editor launch via `AvaloniaIntegration.ShowEditorDialog`
  - `ImageEditorOptions` round-trip in both hosts

## Assumptions
- Both XerahS and ShareX will be migrated directly to the new namespaces.
- The active `EditorView` pipeline remains the current interaction model.
- Structural alignment between the two checked-out library copies must be preserved at the end of the work.
