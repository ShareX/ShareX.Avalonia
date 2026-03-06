# XIP0038 Top 3 Refactoring Pain Points (src)
## Summary

Comprehensive audit of `src/` identified three structural pain points with the highest impact on delivery speed, change safety, and long-term maintainability. All three refactor tracks are now complete and documented in this XIP.

---

## Pain Point 1: Workflow Dispatch Fragmentation Across Core + UI + Helpers Γ£à REFACTORED

> **Status:** Completed in commit `be32184`
> **Primary impact:** High change surface for every new workflow or behavior change

### Evidence

- `WorkflowType` branching is distributed across multiple orchestration layers:
  - `src/desktop/core/XerahS.Core/Tasks/WorkerTask.cs` (large central switch at `:250`)
  - `src/desktop/app/XerahS.UI/Services/WorkflowOrchestrator.cs` (tool routing at `:171`)
  - `src/desktop/core/XerahS.Core/Helpers/TaskHelpers.cs` (`WorkflowType` classification at `:58`, `:133`, `:155`)
  - `src/desktop/core/XerahS.Core/Helpers/TaskHelpers.ExecuteJob.cs` (capture decision switch at `:179`)
  - `src/desktop/app/XerahS.UI/Helpers/ToolNavigationHelper.cs` (tag-to-workflow switch at `:47`)
- `WorkflowType.` appears across many desktop files, increasing edit fan-out and drift risk.

### Why it hurts

- Adding or changing one workflow requires touching many files and layers.
- Behavior can silently diverge between hotkey, navigation, and task execution paths.
- Regression risk grows with every new case block.

### Refactor direction

1. Introduce a single workflow registry (`WorkflowDescriptorRegistry`) as source of truth.
2. Introduce handler contracts:
   - `IWorkflowHandler` for execution
   - `IWorkflowUiRoute` (or equivalent metadata) for UI tag mapping
3. Replace multi-file switch chains with registry lookups.
4. Keep `WorkerTask` focused on task lifecycle, cancellation, and result plumbing, not global workflow routing policy.

### What was done

- Added centralized workflow classification in `src/desktop/core/XerahS.Core/WorkflowCatalog.cs`.
- Replaced duplicated tool-workflow branching in `WorkerTask` with `WorkflowCatalog.IsToolWorkflow(...)`.
- Routed UI tool navigation via registry + shared dispatcher:
  - `src/desktop/app/XerahS.UI/Helpers/ToolNavigationRegistry.cs`
  - `src/desktop/app/XerahS.UI/Services/ToolWorkflowDispatcher.cs`
- Updated `WorkflowOrchestrator` to call the shared dispatcher instead of a long `if/else` chain.
- Replaced capture hide decision switch in `TaskHelpers.ExecuteJob` with `WorkflowCatalog.RequiresHideMainWindowForCapture(...)`.

### Scope candidates

- `src/desktop/core/XerahS.Core/Tasks/WorkerTask.cs`
- `src/desktop/app/XerahS.UI/Services/WorkflowOrchestrator.cs`
- `src/desktop/app/XerahS.UI/Helpers/ToolNavigationHelper.cs`
- `src/desktop/core/XerahS.Core/Helpers/TaskHelpers.cs`
- `src/desktop/core/XerahS.Core/Helpers/TaskHelpers.ExecuteJob.cs`

### Acceptance criteria

- New workflow registration requires one descriptor + one handler, not edits in 4-6 switch locations.
- Hotkey-triggered and UI-triggered execution paths resolve through the same registry.
- Existing workflow behavior remains unchanged (no functional regressions).

---

## Pain Point 2: Large UI Code-Behind Classes with Mixed Responsibilities Γ£à REFACTORED

> **Status:** Completed in commit `4375312`
> **Primary impact:** Hard to test and hard to evolve safely

### Evidence

- `src/desktop/app/XerahS.UI/Views/MainWindow.axaml.cs` (~788 lines):
  - navigation routing (`:441`)
  - keyboard shortcut handling (`:545`)
  - workflow/task launching (`:772`)
- `src/desktop/app/XerahS.RegionCapture/UI/OverlayWindow.axaml.cs` (~1068 lines):
  - pointer state machine (`:314`, `:433`, `:466`)
  - annotation render scheduling/rebuild (`:719`, `:745`)
  - inline text editing lifecycle (`:945`, `:1008`, `:1051`)
  - capture finalization (`:1124`, `:1169`, `:1193`)

### Why it hurts

- UI event wiring, domain logic, and rendering orchestration are tightly coupled.
- Low unit-testability due to heavy view ownership and implicit state transitions.
- Small feature additions require high-risk edits in crowded files.

### Refactor direction

1. Split behavior into focused controllers/services without changing view contracts:
   - `MainWindowNavigationController`
   - `MainWindowShortcutController`
   - `OverlayAnnotationSessionController`
   - `OverlayCaptureCompletionService`
2. Keep code-behind as thin composition + event forwarding layer.
3. Add state-holder objects for mutable interaction state (selection/drawing/editing) to reduce field sprawl.

### What was done

- Split `MainWindow` behavior into partial files:
  - `src/desktop/app/XerahS.UI/Views/MainWindow.Input.cs`
  - `src/desktop/app/XerahS.UI/Views/MainWindow.Navigation.cs`
- Split `OverlayWindow` behavior into partial files:
  - `src/desktop/app/XerahS.RegionCapture/UI/OverlayWindow.InlineText.cs`
  - `src/desktop/app/XerahS.RegionCapture/UI/OverlayWindow.Toolbar.cs`
- Kept runtime behavior unchanged while reducing responsibility concentration in:
  - `src/desktop/app/XerahS.UI/Views/MainWindow.axaml.cs`
  - `src/desktop/app/XerahS.RegionCapture/UI/OverlayWindow.axaml.cs`

### Scope candidates

- `src/desktop/app/XerahS.UI/Views/MainWindow.axaml.cs`
- `src/desktop/app/XerahS.RegionCapture/UI/OverlayWindow.axaml.cs`
- related UI service classes in `src/desktop/app/XerahS.UI/Services/`

### Acceptance criteria

- `MainWindow.axaml.cs` and `OverlayWindow.axaml.cs` each reduced significantly in responsibility and size.
- Pointer/annotation behavior can be tested outside a real window instance.
- No XAML contract changes required for this refactor phase.

---

## Pain Point 3: Upload Pipeline Coupling + Duplicated Custom Uploader Execution Paths Γ£à REFACTORED

> **Status:** Completed in commit `a6fc810`
> **Primary impact:** Duplicate protocol logic and inconsistent failure semantics

### Evidence

- `src/desktop/core/XerahS.Core/Tasks/Processors/UploadJobProcessor.cs` (~512 lines) mixes:
  - destination resolution (`:131`)
  - fallback orchestration (`:252`)
  - plugin loading (`:429`)
  - history append (`:453`)
  - after-upload side effects (`:521`)
- Similar custom uploader protocol branches are repeated in:
  - `src/desktop/core/XerahS.Uploaders/ImageUploaders/CustomImageUploader.cs`
  - `src/desktop/core/XerahS.Uploaders/TextUploaders/CustomTextUploader.cs`
  - `src/desktop/core/XerahS.Uploaders/SharingServices/CustomURLSharingService.cs`
  - `src/desktop/core/XerahS.Uploaders/URLShorteners/CustomURLShortener.cs`
- Multiple areas still throw generic `Exception` for protocol-state failures.

### Why it hurts

- Same request-body decision logic is duplicated in multiple uploader types.
- Hard to apply consistent retry/error/history behavior.
- Generic exceptions reduce observability and make failure handling brittle.

### Refactor direction

1. Extract upload pipeline stages:
   - `IUploadDestinationResolver`
   - `IUploadExecutor`
   - `IUploadPostProcessor` (history/clipboard/UI)
2. Create shared `CustomUploaderRequestExecutor` to centralize body-format dispatch (`None`, `MultipartFormData`, `FormURLEncoded`, `JSON/XML`, `Binary`).
3. Replace generic `Exception` with domain-specific exceptions/result codes (for unsupported body, missing config, provider mismatch).
4. Keep compatibility with existing provider/plugin contracts.

### What was done

- Added shared custom uploader body executor:
  - `src/desktop/core/XerahS.Uploaders/CustomUploader/CustomUploaderRequestExecutor.cs`
- Introduced typed unsupported-body exception (`UnsupportedCustomUploaderBodyException`) replacing generic `Exception` for this path.
- Migrated body dispatch logic in:
  - `src/desktop/core/XerahS.Uploaders/ImageUploaders/CustomImageUploader.cs`
  - `src/desktop/core/XerahS.Uploaders/TextUploaders/CustomTextUploader.cs`
  - `src/desktop/core/XerahS.Uploaders/SharingServices/CustomURLSharingService.cs`
  - `src/desktop/core/XerahS.Uploaders/URLShorteners/CustomURLShortener.cs`

### Scope candidates

- `src/desktop/core/XerahS.Core/Tasks/Processors/UploadJobProcessor.cs`
- `src/desktop/core/XerahS.Uploaders/PluginSystem/InstanceManager.cs`
- `src/desktop/core/XerahS.Uploaders/*Uploaders/Custom*.cs`
- `src/desktop/core/XerahS.Uploaders/CustomUploader/`

### Acceptance criteria

- Custom uploader protocol branching exists in one shared executor path.
- Upload post-processing side effects are separated from core upload execution.
- Typed failures are propagated and logged consistently.

---

## Priority Matrix

| Pain Point | Impact on Velocity | Maintenance Risk | Estimated Effort | Priority | Status |
|---|---|---|---|---|---|
| 1. Workflow dispatch fragmentation | High | High | 3-5 days | P0 | Γ£à Done |
| 2. Large UI code-behind classes | High | High | 3-5 days | P1 | Γ£à Done |
| 3. Upload pipeline coupling + duplication | High | Medium-High | 2-4 days | P2 | Γ£à Done |

---

## Execution Order

1. Pain Point 1 completed (`be32184`).
2. Pain Point 2 completed (`4375312`).
3. Pain Point 3 completed (`a6fc810`).

---

## Out of Scope for XIP0038

- Full platform service redesign in `PlatformServices` (already partially addressed in prior XIPs)
- Large-scale folder restructuring under `src/` (covered by separate reorg XIP)
- New product features unrelated to dispatch, UI decomposition, or upload pipeline structure