# IEIP0001: ImageEditor Structure Refactor

## Status
- Status: Implemented on March 6, 2026. Follow-up cleanup phases added March 6, 2026.
- Outcome: The planned structural refactor was completed across `ShareX.ImageEditor`, XerahS, and the ShareX host integration.
- Remaining gap from the original verification plan: manual UI smoke scenarios were not re-run as part of this proposal closeout.

## Implementation Outcome
- Phase 0 was completed in XerahS by renaming the submodule path from `ImageEditor/` to `ShareX.ImageEditor/` and updating repo references, solution paths, and docs.
- Phase 1 was completed in `ShareX.ImageEditor` as a sequence of folder-specific mechanical commits rather than one large move commit. This exceeded the original reviewability goal.
- Phase 2 was completed by aligning namespaces to `Hosting`, `Core`, and `Presentation`, then migrating both XerahS and ShareX host code to those namespaces.
- `EditorOptions` was already in the process of being renamed upstream and the migration was completed in host code as `ImageEditorOptions`.
- `Public` was not introduced. `Hosting` was chosen intentionally because `Public` reads like a C# access modifier rather than a host-facing responsibility boundary.

## Current State
- The library root now exposes explicit responsibility buckets under `ShareX.ImageEditor/src/ShareX.ImageEditor/`:
  - `Hosting`
  - `Core`
  - `Presentation`
- Host-facing entry points now live in `Hosting`, including:
  - `ImageEditorOptions`
  - `AvaloniaIntegration`
  - `EditorServices`
  - `IClipboardService`
- XerahS now consumes the new namespaces directly, for example:
  - `ShareX.ImageEditor.Hosting`
  - `ShareX.ImageEditor.Core.*`
  - `ShareX.ImageEditor.Presentation.*`
- `EditorViewModel` is no longer present in the active codebase.
- `EditorCanvas` was not preserved as an active editor surface in the final structure.
- The library documentation now includes a structure map describing where host API, editor core, and Avalonia presentation code live.

## Plan Variance Notes
- The original plan described two major implementation phases, but Phase 1 was intentionally split into per-folder commits:
  - `Hosting`
  - `Presentation/Controls`
  - `Presentation/Converters`
  - `Presentation/Legacy`
  - `Presentation/Rendering`
  - `Presentation/Theming`
  - `Presentation/ViewModels`
  - `Presentation/Views`
- This was a process improvement, not a scope change.
- ShareX repo-path changes were not needed because ShareX was already using the `ShareX.ImageEditor` submodule path; only its namespace imports/usages needed migration.

---

## Cleanup Phases (March 6, 2026)

Post-implementation audit identified three remaining friction points for agentic coding. Addressed as separate mechanical commits.

### Phase A: Delete Empty Artifact Directories

**Problem:** Four empty directories were left as migration artifacts under `Presentation/`:
```
Presentation/Controls/Controls/
Presentation/Converters/Converters/
Presentation/Views/Controls/
Presentation/Views/Views/Controls/
```
These appear in directory listings and Glob results, adding noise and suggesting false sub-structure.

**Action:** Delete all four directories. No file or namespace changes.

**Verification:** `find Presentation -empty -type d` returns nothing.

---

### Phase B: Disambiguate `ImageEffect.cs` Base Class File Names

**Problem:** Three files share the name `ImageEffect.cs` in the same directory tree:
```
Core/ImageEffects/ImageEffect.cs              → abstract root base
Core/ImageEffects/Adjustments/ImageEffect.cs  → abstract adjustments base
Core/ImageEffects/Filters/ImageEffect.cs      → abstract filters base
```
When an agent greps for `ImageEffect` or does a Glob for `**/ImageEffect.cs`, it gets three candidates and must read all three to determine which is relevant. This multiplies agent context consumption on every effect-related task.

**Action:**
- Rename `Adjustments/ImageEffect.cs` → `AdjustmentImageEffect.cs`, class renamed to `AdjustmentImageEffect`
- Rename `Filters/ImageEffect.cs` → `FilterImageEffect.cs`, class renamed to `FilterImageEffect`
- Update all `~26` concrete adjustment effect classes: `: ImageEffect` → `: AdjustmentImageEffect`
- Update all `~65` concrete filter effect classes: `: ImageEffect` → `: FilterImageEffect`
- Root `Core/ImageEffects/ImageEffect.cs` stays unchanged as the unambiguous true base

**Namespace impact:** None. Class renaming within the same namespace only.

**Verification:** `find Core/ImageEffects -name "ImageEffect.cs"` returns exactly one result (the root base).

---

### Phase C: Relocate Input Controllers Out of `Views/`

**Problem:** `Presentation/Views/Controllers/` contains behavioral input controllers:
```
EditorInputController.cs
EditorSelectionController.cs
EditorZoomController.cs
```
These are not views. Nesting them under `Views/` misrepresents their responsibility and means Glob patterns like `Presentation/Views/**` pull in non-view files.

**Action:**
- Move the three files from `Presentation/Views/Controllers/` → `Presentation/Controllers/`
- Update namespace from `ShareX.ImageEditor.Presentation.Views.Controllers` → `ShareX.ImageEditor.Presentation.Controllers`
- Update `using` directives in any consumer files (primarily `EditorView.*.cs`)
- Remove the now-empty `Presentation/Views/Controllers/` directory

**Verification:** `Presentation/Views/` contains only view and dialog files; controllers are at `Presentation/Controllers/`.

---

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

## Verification Outcome
- Completed:
  - `ShareX.ImageEditor.sln` build verification in both checked-out library copies
  - XerahS builds for `XerahS.Core`, `XerahS.RegionCapture`, `XerahS.UI`, and `XerahS.Tests`
  - Full XerahS solution build via `src/desktop/XerahS.sln -m:1`
  - ShareX host build for `ShareX/ShareX.csproj`
- Not completed:
  - Manual smoke execution of the UI scenarios listed above was not documented during implementation
- Result:
  - The structural and namespace migration completed with successful compile-time verification across the library and both host applications

## Assumptions
- Both XerahS and ShareX will be migrated directly to the new namespaces.
- The active `EditorView` pipeline remains the current interaction model.
- Structural alignment between the two checked-out library copies must be preserved at the end of the work.
