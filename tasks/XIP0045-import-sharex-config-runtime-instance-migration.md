# XIP0045 Import ShareX Config — Runtime Instance Migration & UX Gaps

**Status**: IN PROGRESS
**Priority**: High
**Related**: XIP0012, issue #171

---

## Problem Statement

`Import ShareX Config` imports legacy `UploadersConfig.json`, but active uploads use plugin
instances from `uploader-instances.json` (`ProviderId` + `SettingsJson`). There are two
distinct sub-problems:

1. **Custom Uploaders** — the `.sxcu` export pipeline is complete, but users still must
   manually "Add from Catalog" to create a usable instance. The files are on disk and
   providers are registered, but no `UploaderInstance` is created automatically.

2. **Built-in Uploaders** — settings are imported into `SettingsManager.UploadersConfig`
   (backward-compat layer) but never connected to runtime instances. Import reports success
   while destinations still use stale or empty `SettingsJson`.

---

## Code Audit (2026-03-03)

### What Is Fully Working

| Feature | File | Status |
|---------|------|--------|
| Detect / browse `UploadersConfig.json` | `UploadersConfigImporter.FindShareXUploadersConfig()` | ✅ |
| Deserialise and dispatch to per-category importers | `UploadersConfigImporter.ImportFromFile()` | ✅ |
| Extract `CustomUploaderItem` list from import result | `ImportResult.ImportedCustomUploaders` | ✅ |
| Export each item to `{PluginsFolder}/{name}.sxcu` | `ExportImportedCustomUploaders()` in `DestinationSettingsViewModel` | ✅ |
| Duplicate detection via `JToken.DeepEquals` | `IsEquivalentCustomUploaderFile()` | ✅ |
| Register new providers in `ProviderCatalog` | `ProviderCatalog.LoadCustomUploaders()` | ✅ |
| Refresh all destination-category UI lists | `category.LoadInstances()` per category | ✅ |
| Formatted import-summary dialog | `BuildImportSummary()` | ✅ |

### Confirmed Bugs / Gaps

#### Bug 1 — No auto-instance creation for custom uploaders (HIGH)

**File**: `DestinationSettingsViewModel.cs` — `ImportShareXConfig()`, lines 197–205

After exporting `.sxcu` files and registering providers, the import flow stops.
Users are told via the summary dialog:

> *"Next step: use 'Add from Catalog' to create destination instances."*

But this manual step is unnecessary. `InstanceManager.AddInstance()` is the exact API
needed, and it already exists:

```csharp
// InstanceManager.cs line 152
public void AddInstance(UploaderInstance instance)
```

Each exported custom uploader should produce one `UploaderInstance` per supported category,
skipping categories where an instance with the same `ProviderId` already exists.

**Fix location**: after `ProviderCatalog.LoadCustomUploaders()` succeeds:
```csharp
// For each successfully exported provider:
var provider = ProviderCatalog.GetProvider(providerId);
foreach (var category in provider.SupportedCategories)
{
    bool alreadyExists = InstanceManager.Instance
        .GetInstancesByCategory(category)
        .Any(i => i.ProviderId == providerId);

    if (!alreadyExists)
    {
        var instance = new UploaderInstance
        {
            ProviderId  = providerId,
            DisplayName = provider.Name,
            Category    = category,
            SettingsJson = provider.GetDefaultSettings(category)
        };
        InstanceManager.Instance.AddInstance(instance);
    }
}
```

Update `BuildImportSummary` and `CustomUploaderExportResult` to include
`InstancesCreatedCount` and `InstancesSkippedCount`.

#### Bug 2 — Catalog reload gated only on `ExportedCount`, skips fresh-session reload (MEDIUM)

**File**: `DestinationSettingsViewModel.cs` — `ImportShareXConfig()`, lines 197–205

```csharp
// Current
if (customUploaderExport.ExportedCount > 0)
{
    ProviderCatalog.LoadCustomUploaders(customUploaderExport.PluginsPath);
    ...
}
```

If all custom uploaders were **duplicates** (all skipped, `ExportedCount == 0`), the catalog
is not reloaded. In a fresh session where `LoadCustomUploaders` has not yet been called,
existing `.sxcu` files are not registered, so their providers don't appear in the UI.

**Fix**: change condition to `ExportedCount > 0 || SkippedCount > 0`:

```csharp
if (customUploaderExport.ExportedCount > 0 || customUploaderExport.SkippedCount > 0)
{
    ProviderCatalog.LoadCustomUploaders(customUploaderExport.PluginsPath);
    ...
}
```

#### Bug 3 — "Next step: use Add from Catalog" shown even when ExportedCount == 0 (LOW)

**File**: `DestinationSettingsViewModel.cs` — `BuildImportSummary()`, line 464

The "Next step" message is appended whenever `TotalImportedCustomUploaders > 0`, even if
everything was skipped (nothing new was written). Once Bug 1 is fixed (auto-instance
creation), this message becomes redundant and should be replaced with instance-count lines.
Until then it should only appear when `ExportedCount > 0`.

#### Bug 4 — `AddCustomUploader` path lacks equivalency check (LOW)

**File**: `DestinationSettingsViewModel.cs` — `AddCustomUploader()`, lines 282–287

```csharp
// Current — no content equality check
int counter = 1;
while (File.Exists(filePath))
{
    filePath = Path.Combine(pluginsPath, $"{safeName}_{counter++}.sxcu");
}
```

This always creates a new file even if an identical one already exists. The import path
uses `ResolveCustomUploaderFilePath()` which calls `IsEquivalentCustomUploaderFile()`.
`AddCustomUploader` should do the same to stay consistent.

#### Gap — Built-in uploaders not instance-aware (HIGH, main XIP0045 scope)

**File**: `UploadersConfigImporter.cs` — all category importers (lines 106–318)

Settings for Imgur, FTP, Dropbox, Pastebin, etc. are written to
`SettingsManager.UploadersConfig` only. They never reach `InstanceManager` or
`uploader-instances.json`. Users who had these configured in ShareX see no change in
active upload behaviour after import.

This requires a provider-mapping design (see TODO items below).

---

## TODO Action Items

### Custom Uploader Stream (near-term)

- [ ] **[Bug 1 fix]** After `ProviderCatalog.LoadCustomUploaders()` succeeds, iterate
  `result.ExportedFilePaths` (new field), resolve each to its `ProviderId` via
  `CustomUploaderProvider.GenerateProviderId`, and create `UploaderInstance` via
  `InstanceManager.Instance.AddInstance()` for each supported category, skipping existing
  instances with the same `ProviderId` + `Category`.

- [ ] **[Bug 2 fix]** Change reload gate condition from `ExportedCount > 0` to
  `ExportedCount > 0 || SkippedCount > 0` in `ImportShareXConfig()`.

- [ ] **[Bug 3 fix]** Remove or conditionalize "Next step: use Add from Catalog" message in
  `BuildImportSummary` once auto-instance creation is implemented.

- [ ] **[Bug 4 fix]** Use `ResolveCustomUploaderFilePath()` in `AddCustomUploader()` instead
  of the manual counter loop, for consistent duplicate detection.

- [ ] Add `ExportedFilePaths: List<string>` to `CustomUploaderExportResult` so caller can
  resolve `ProviderId` values without re-scanning the folder.

- [ ] Update `BuildImportSummary` to report instances created vs skipped:
  ```
  Custom uploader export:
  - Imported from config:     5
  - Created .sxcu files:      3
  - Skipped duplicates:       2
  - Failed exports:           0
  - Destination instances created: 4   ← new
  - Destination instances skipped: 2   ← new (already existed)
  - Plugins folder: C:\...\Plugins
  ```

### Built-in Uploader Stream (XIP0045 core, medium-term)

- [ ] Design provider-ID mapping table: legacy `UploadersConfig` property paths →
  XerahS `ProviderId` + `SettingsJson` shape. Start with the most-used providers:
  Imgur, FTP (all entries), Dropbox, Google Drive, Pastebin.

- [ ] Implement `BuiltinInstanceMigrator` in
  `XerahS.Uploaders/LegacySupport/BuiltinInstanceMigrator.cs`:
  - Input: `UploadersConfig` (post-import snapshot)
  - Output: list of `UploaderInstance` to add/update
  - Rules: idempotent (re-importing same config must not duplicate instances);
    update existing instance if `ProviderId` + `Category` match; preserve `InstanceId`
    when updating so workflow references are not broken.

- [ ] Call `BuiltinInstanceMigrator` from `ImportShareXConfig()` after the custom-uploader
  block; add results to the summary dialog.

- [ ] Include `InstancesUpdated`, `InstancesCreated`, and `ProvidersSkipped` sections in
  the summary for built-in providers.

- [ ] Add explicit per-provider warning when import was compatibility-only (settings saved
  to `UploadersConfig` but no runtime instance could be created, e.g. OAuth providers
  that require re-authorisation).

### Tests

- [ ] Unit test: import config with 3 custom uploaders → 3 `.sxcu` files + 3 instances.
- [ ] Unit test: re-import same config → 0 new files, 0 new instances (idempotent).
- [ ] Unit test: import config where identical `.sxcu` already exists → skipped, existing
  instance preserved.
- [ ] Unit test: built-in importer (Imgur) creates instance when none exists.
- [ ] Unit test: built-in importer (Imgur) updates instance when one already exists with
  same `ProviderId`.

---

## Implementation Notes

### Resolving `ProviderId` from an exported file path

```csharp
// Given filePath = "{PluginsFolder}/My_Uploader.sxcu"
string slug = Path.GetFileNameWithoutExtension(filePath); // "My_Uploader"
string providerId = CustomUploaderProvider.SlugifyForProviderId(slug); // "custom_my_uploader"
```

`CustomUploaderProvider.GenerateProviderId(item, filePath)` already does this; expose
`SlugifyForProviderId(string)` as `internal static` for use in the importer.

### `UploaderInstance` constructor pattern (from existing usage)

```csharp
var instance = new UploaderInstance
{
    ProviderId   = providerId,
    DisplayName  = provider.Name,
    Category     = category,
    SettingsJson = provider.GetDefaultSettings(category) // "{}" for custom uploaders
};
InstanceManager.Instance.AddInstance(instance); // assigns InstanceId + timestamps
```

### Idempotency check (for built-in providers)

```csharp
bool exists = InstanceManager.Instance
    .GetInstancesByCategory(category)
    .Any(i => i.ProviderId == providerId);
if (exists)
    InstanceManager.Instance.UpdateInstance(updatedInstance);
else
    InstanceManager.Instance.AddInstance(newInstance);
```

---

## Initial Target Files

- `src/desktop/app/XerahS.UI/ViewModels/DestinationSettingsViewModel.cs`
- `src/desktop/core/XerahS.Uploaders/LegacySupport/UploadersConfigImporter.cs`
- `src/desktop/core/XerahS.Uploaders/PluginSystem/InstanceManager.cs`
- `src/desktop/core/XerahS.Uploaders/CustomUploader/CustomUploaderProvider.cs`
- `src/desktop/core/XerahS.Uploaders/CustomUploader/CustomUploaderRepository.cs`
- *(new)* `src/desktop/core/XerahS.Uploaders/LegacySupport/BuiltinInstanceMigrator.cs`

---

## Acceptance Criteria

1. Importing `UploadersConfig.json` with custom uploaders automatically creates usable
   destination instances — no "Add from Catalog" step required.
2. Re-importing the same file does not create duplicate `.sxcu` files or instances.
3. Importing in a fresh session where `.sxcu` files already exist registers providers
   and creates missing instances correctly.
4. Built-in uploaders with importable settings produce runtime instances (or a
   per-provider warning explaining why they cannot).
5. Import completion dialog accurately reports: files created, files skipped, instances
   created, instances skipped, and per-provider warnings.
6. `dotnet build src/desktop/XerahS.sln -m:1` passes with 0 errors (close app first).

---

## Verification Commands

```powershell
dotnet build src/desktop/core/XerahS.Uploaders/XerahS.Uploaders.csproj -m:1
dotnet build src/desktop/app/XerahS.UI/XerahS.UI.csproj -m:1
dotnet build src/desktop/XerahS.sln -m:1
```
