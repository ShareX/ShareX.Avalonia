# XIP0045 Import ShareX Config Runtime Instance Migration

**Status**: TODO  
**Priority**: High  
**Related**: XIP0012, issue #171

---

## Problem Statement

`Import ShareX Config` currently imports legacy data into `SettingsManager.UploadersConfig`, but active uploads use plugin instances from `uploader-instances.json` (`ProviderId` + `SettingsJson`).

Result: import can report success while existing destinations still use old or empty runtime settings.

---

## Findings Snapshot

1. Custom uploader import path now exports `.sxcu` files to `Plugins` and reloads providers.
2. Built-in uploader settings import is still compatibility-only and not instance-aware.
3. UX currently does not state which runtime instances were updated because no such migration exists yet.

---

## TODO Action Items

1. Design provider mapping from legacy `UploadersConfig` fields to instance `SettingsJson`.
2. Implement an importer service that migrates imported settings into runtime instances.
3. For each provider with imported settings, update an existing instance or create one if missing.
4. Preserve existing instance IDs when updating existing instances to avoid workflow breakage.
5. Keep migration idempotent and non-destructive; do not overwrite unrelated provider settings.
6. Add import summary sections for runtime instance updates:
   - instances updated
   - instances created
   - providers skipped (with reason)
7. Add explicit warning section when import was compatibility-only for any provider.
8. Add unit/integration tests for:
   - update existing instance
   - create missing instance
   - no duplicate instances on repeated import
   - no migration when source settings are empty
9. Validate workflow editor behavior when legacy override fields are present after migration.
10. Document final behavior in `docs/` and update any existing import-related architecture/task docs.

---

## Initial Target Files

- `src/desktop/app/XerahS.UI/ViewModels/DestinationSettingsViewModel.cs`
- `src/desktop/core/XerahS.Uploaders/LegacySupport/UploadersConfigImporter.cs`
- `src/desktop/core/XerahS.Uploaders/PluginSystem/InstanceManager.cs`
- `src/desktop/core/XerahS.Core/Tasks/Processors/UploadJobProcessor.cs`

---

## Acceptance Criteria

1. Importing `UploadersConfig.json` updates runtime behavior for supported providers without manual reconfiguration.
2. Re-importing the same file does not create duplicate runtime instances.
3. Import completion dialog accurately reports runtime changes and skipped providers.
4. `dotnet build src/desktop/XerahS.sln -m:1` succeeds with 0 errors (run with app process closed to avoid file locks).

---

## Verification Commands

```powershell
dotnet build src/desktop/core/XerahS.Uploaders/XerahS.Uploaders.csproj -m:1
dotnet build src/desktop/app/XerahS.UI/XerahS.UI.csproj -m:1
dotnet build src/desktop/XerahS.sln -m:1
```

