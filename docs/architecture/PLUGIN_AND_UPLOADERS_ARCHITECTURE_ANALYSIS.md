# Plugin & XerahS.Uploaders Architecture Analysis

**Goal:** Plugins should be as **self-contained** as possible; **governance and overall interface** are provided by `src/desktop/core/XerahS.Uploaders`.

**Legacy import and mobile support:** XerahS supports importing legacy ShareX settings (including Amazon S3) and uses the same config shape for **mobile** (e.g. `UploadersConfig`, `AmazonS3Settings`). The code necessary for legacy import and mobile—including types such as `AmazonS3Settings` and `AmazonS3StorageClass` when used for import, sync, and mobile UI—**can stay in** `src/desktop/core/XerahS.Uploaders`. This document does not require moving those types into plugins for legacy or mobile compatibility.

**Is legacy/mobile support in the way of a truly plugin-based implementation?**  
**No.** Legacy import and mobile support live entirely in core (importer, `UploadersConfig`, sync, legacy DTOs). The **runtime** path is already plugin-based: the host uses `ProviderId` + `SettingsJson` and calls `provider.CreateInstance(settingsJson)`; plugins use their own config models. The only coupling that prevents a plugin from being **fully** self-contained is the **plugin’s choice** to use a core type in its **runtime** config (e.g. Amazon S3 plugin uses core’s `AmazonS3StorageClass` in `S3ConfigModel`). That can be removed without touching legacy/mobile: the plugin defines its own enum for runtime; core keeps its types for import and mobile. So legacy/mobile support and a truly plugin-based design coexist: core keeps these types for import and mobile; plugins can own all runtime types and remain self-contained.

---

## 1. Current State Summary

### 1.1 Two Parallel Config Models

| Aspect | Instance-based (plugin-friendly) | Legacy / polymorphic (core-owned) |
|--------|----------------------------------|-----------------------------------|
| **Storage** | `uploader-instances.json` (InstanceManager) | `UploadersConfig.json` (SettingsManager) |
| **Key** | `ProviderId` (e.g. `"amazons3"`, `"imgur"`) | `UploaderType` enum (Imgur, Dropbox, FTP, AmazonS3, Custom) |
| **Config shape** | Opaque `SettingsJson` per instance; plugin owns schema | Strongly-typed: `ImgurConfig`, `DropboxConfig`, `S3Config`, `FtpConfig` in core |
| **Used by** | Desktop UI, workflows, ProviderCatalog, CreateInstance(settingsJson) | ShareX config import, Save path (SyncPolymorphic*), mobile, CustomUploaders/FTP lists |

Runtime upload flow is already **plugin-centric**: the host uses `ProviderId` + `SettingsJson` and calls `provider.CreateInstance(settingsJson)`. Plugins use their own config models (e.g. `S3ConfigModel`, `ImgurConfigModel`).

The **legacy/polymorphic side** (UploadersConfig and its types) remains so that:
- ShareX `UploadersConfig.json` can be imported (same shape as ShareX).
- **Mobile** and some desktop UI read/write `UploadersConfig` (e.g. `AmazonS3Settings`, `CustomUploadersList`, `FTPAccountList`).
- `UploadersConfig` is saved with both legacy flat properties and `ServiceSettings` (Dictionary<UploaderType, IUploaderConfig>).

### 1.2 What Core (XerahS.Uploaders) Contains Today

| Category | Contents | Plugin coupling |
|----------|-----------|-----------------|
| **Interfaces & contracts** | `IUploaderProvider`, `IUploaderConfigViewModel`, `IUploaderExplorer`, `IProviderContextAware`, `ISecretStore`; `UploaderCategory`, `FileTypeScope`, `MediaItem`, `ExplorerPage`, `ExplorerQuery` | ✅ Appropriate: plugins implement these |
| **Abstractions** | `IUploaderConfig` (Id, Name, **UploaderType**), base `Uploader`, `UploaderProviderBase` | ⚠️ `IUploaderConfig` ties core to UploaderType |
| **Plugin system** | `ProviderCatalog`, `PluginDiscovery`, `PluginLoadContext`, `PluginManifest`, `PluginConfigurationVerifier`, `PluginPackager` | ✅ Governance |
| **Instance management** | `UploaderInstance` (ProviderId, SettingsJson), `InstanceManager`, `InstanceConfiguration` | ✅ Provider-agnostic |
| **Concrete config types** | `ImgurConfig`, `DropboxConfig`, `S3Config`, `FtpConfig`, `CustomUploaderConfig`; `AmazonS3Settings`, `AmazonS3StorageClass` | ⚠️ Used for legacy import, mobile, and sync; acceptable to keep in core |
| **UploadersConfig** | Legacy flat properties (Imgur*, Dropbox*, AmazonS3Settings, FTP*, etc.) + `ServiceSettings` Dictionary<UploaderType, IUploaderConfig> + sync methods | ⚠️ Needed for legacy import and mobile; acceptable in core |
| **ShareX compat** | `UploadersLib` (Stubs.cs): `ShareX.UploadersLib.*` namespaces (ImgurAlbumData, OAuth2Info, PastebinSettings, etc.); `UploadersConfigImporter` | ✅ Legacy import; can stay in core |
| **Custom uploader** | `CustomUploaderProvider`, `CustomUploaderItem`, `.sxcu` parsing, custom uploader functions | ✅ Could stay; custom is a first-class “plugin” in core |
| **OAuth / shared** | `OAuth2Info`, `OAuthInfo` (in core) | ⚠️ Shared by multiple plugins; could stay as shared contract or move to a small “Auth” contract assembly |
| **Base uploaders** | `FileUploader`, `TextUploader`, `GenericUploader` (used by plugins) | ✅ Appropriate |
| **Helpers** | `UploaderErrorManager`, `ResponseInfo`, `EscapeHelper`, etc. | ✅ Fine |

### 1.3 What Plugins Depend On (Core)

- **All plugins:** `XerahS.Uploaders` (and usually `XerahS.Common`) via ProjectReference.
- **Used from core:**  
  - `IUploaderProvider`, `IUploaderConfigViewModel`, `UploaderProviderBase`, `Uploader` (base class).  
  - `PluginSystem` types: `MediaItem`, `UploaderCategory`, `FileTypeScope`, `ProviderCatalog`, `ISecretStore` / `IProviderContextAware`, etc.  
  - **AmazonS3.Plugin only:** `XerahS.Uploaders.FileUploaders.AmazonS3StorageClass` (and thus core’s `AmazonS3Settings` indirectly via S3Config in core).
- **No plugin** references `ImgurConfig`, `DropboxConfig`, or `S3Config` in their *runtime* path; they use their own `*ConfigModel` and `SettingsJson`. Core’s config types are used for **UploadersConfig** and **import/sync**, not for plugin execution.

### 1.4 Where Core Violates “Plugins Self-Contained”

1. **Core defines plugin-specific types**  
   `ImgurConfig`, `DropboxConfig`, `S3Config`, `FtpConfig`, `AmazonS3Settings`, `AmazonS3StorageClass` live in core. For **legacy ShareX import and mobile support**, keeping these in core is acceptable. Plugins (e.g. Amazon S3) may reference core’s types (e.g. `AmazonS3StorageClass`) for consistency; plugins can still own their runtime config model (`S3ConfigModel`).

2. **UploaderType enum is a closed list in core**  
   Every plugin type is hard-coded (Imgur, Dropbox, FTP, AmazonS3, Custom). Adding a new plugin requires changing core. Governance should be “provider identified by string ProviderId,” not by an enum that core owns.

3. **UploadersConfig sync is tightly coupled**  
   `SyncPolymorphicSettingsFromLegacy` / `SyncLegacySettingsFromPolymorphic` explicitly construct and read `ImgurConfig`, `DropboxConfig`, `S3Config`, `FtpConfig`. So core must know every such type.

4. **InstanceManager has provider-specific logic**  
   `MigrateSecretsIfNeeded` (and similar) hard-code `providerId == "amazons3"`, `"imgur"`, `"gist"`. That logic could be moved behind an interface implemented by plugins (e.g. “migrate secrets for this instance”).

5. **ShareX.UploadersLib in core**  
   A large set of stubs and DTOs (Imgur, Pastebin, Flickr, etc.) live in core for ShareX compatibility and for the polymorphic config shape. That keeps core large and couples it to every legacy uploader.

6. **Mobile and import use core’s concrete types**  
   Mobile reads/writes `UploadersConfig` (e.g. `AmazonS3Settings`, `CustomUploadersList`); the importer copies `AmazonS3Settings`, etc. Both legacy import and mobile depend on core holding those types—which is acceptable for legacy and mobile support.

---

## 2. Target: Core as Governance + Interface Only

**Principle:** Core defines **how** plugins behave (contracts, discovery, instances, secrets, UI hooks) and **where** config is stored (e.g. by ProviderId + opaque JSON). It does **not** define **what** each plugin’s config looks like.

### 2.1 What Should Stay in Core

- **Plugin system:** Discovery, loading, `ProviderCatalog`, manifest, verification, packager.
- **Instance model:** `UploaderInstance` (ProviderId, SettingsJson, Category, FileTypeScope, etc.), `InstanceManager`, `InstanceConfiguration`.
- **Contracts:** `IUploaderProvider`, `IUploaderConfigViewModel`, `IUploaderExplorer`, `IProviderContextAware`, `ISecretStore`; `UploaderCategory`, `FileTypeScope`, `MediaItem`, `ExplorerPage`/`ExplorerQuery`.
- **Base class:** `Uploader` (and base uploaders like `FileUploader`/`TextUploader` if plugins inherit them).
- **Custom uploader:** Custom uploader provider and .sxcu handling (as the one “built-in” plugin model).
- **Shared governance types:** e.g. a minimal `IUploaderConfig` that has `Id`, `Name`, and **ProviderId** (or no enum); or drop `IUploaderConfig` from the instance path and keep it only for optional legacy compat.
- **Optional:** Small shared auth contract (e.g. `OAuth2Info`) in core or in a tiny `XerahS.Uploaders.Contracts` used by core and plugins.
- **Legacy import and mobile support:** Code required to import legacy ShareX settings and to support mobile (e.g. `UploadersConfigImporter`, `UploadersConfig`, types like `AmazonS3Settings`, `AmazonS3StorageClass`, `S3Config` and their use in sync/import and mobile) **stays in** `src/desktop/core/XerahS.Uploaders`. Plugins can remain self-contained for *runtime* config (their own `*ConfigModel` and `SettingsJson`); core may keep these DTOs and sync for legacy import and mobile.

### 2.2 What May Move Out of Core (Optional, for stronger plugin containment)

- **Plugin-specific runtime config types and enums**  
  For *runtime* use only, plugins can own their config (e.g. plugin defines its own storage class enum). **Legacy import and mobile** continue to use types in core (e.g. `AmazonS3Settings`, `S3Config`); those do not need to move.

- **UploaderType enum**  
  Prefer **ProviderId string** as the only identifier in the plugin model. If legacy or mobile still need a “type” for display or import, that can be a plugin-registered string or a small compat layer that maps ProviderId ↔ legacy type.

- **UploadersConfig polymorphic shape**  
  Option A: **Slim UploadersConfig** – only holds what is truly global: e.g. CustomUploaders list, FTP accounts, and optionally a **per-ProviderId legacy blob** (e.g. `Dictionary<string, string>` or `Dictionary<string, JObject>`). No `ServiceSettings` with concrete types.  
  Option B: Keep a polymorphic store for backward compatibility, but **values are opaque** (e.g. string or JObject). Core never deserializes them into `ImgurConfig`/`S3Config`; **plugins** optionally participate in “import from legacy blob” or “export to legacy blob” via a small interface.

- **ShareX.UploadersLib**  
  Can remain in core for legacy import. If desired, it could be moved to a compat-only assembly later; it is not required to leave core for legacy support.

- **Provider-specific logic in InstanceManager**  
  Replace hard-coded `if (providerId == "amazons3")` with an interface (e.g. `ISecretMigrator` or `IInstanceMigrator`) that plugins implement. Core calls “migrate this instance” on the provider; the provider does provider-specific work.

### 2.3 Governance and Interface “Provided by Core”

- **Discovery and loading:** Core decides where to scan, how to load plugin DLLs, and how to register `IUploaderProvider`.
- **Instance lifecycle:** Core owns `UploaderInstance`, add/update/remove, default instance per category, and persistence to `uploader-instances.json`.
- **Config flow:** Core passes `SettingsJson` into the plugin; plugin deserializes to its own type. Core never interprets plugin config.
- **Secrets:** Core provides `ISecretStore` (and possibly migration hooks); plugins use it with their own `ProviderId` and key scheme.
- **UI contract:** Core defines `IUploaderConfigViewModel` (LoadFromJson / ToJson / Validate) and how config views are hosted; plugins supply the implementation and optional view.
- **Categories and file types:** Core defines `UploaderCategory` and the file-type routing model; plugins declare which categories they support and which file types they handle.

---

## 3. Recommended Direction (Rethought Architecture)

1. **Treat ProviderId as the single source of truth**  
   No `UploaderType` in the plugin contract (or reserve it only for a legacy/compat layer). All runtime behavior keyed by `ProviderId`.

2. **Config: instance-based only in the plugin world**  
   Persisted config for plugins = `UploaderInstance` (ProviderId + SettingsJson). No core-owned `ImgurConfig`/`S3Config` in the runtime path. Plugins own their config schema and enums (e.g. move `AmazonS3StorageClass` and S3 settings into Amazon S3 plugin).

3. **UploadersConfig: slim or opaque**  
   - **Slim:** UploadersConfig holds only non-plugin data (CustomUploaders, FTP, etc.) and optionally a dictionary of “legacy blob per ProviderId” (string or JObject).  
   - **Sync/import:** Either (a) plugins implement “import from legacy blob” / “export to legacy blob” so core doesn’t need to know the blob shape, or (b) a dedicated “ShareX import” module that knows the legacy shape and writes into instance JSON + legacy blobs without core defining every uploader type.

4. **Legacy import and mobile support stay in core**  
   Types and logic needed to import legacy ShareX settings and to support mobile (including Amazon S3) remain in `XerahS.Uploaders`. Plugins can own their *runtime* config model and enums; core may keep `AmazonS3Settings`, `AmazonS3StorageClass`, `S3Config`, and the importer/sync code for legacy import and mobile.

5. **Extensibility for secrets and migration**  
   Define a small interface (e.g. “migrate secrets for this instance”) implemented by plugins so `InstanceManager` (or similar) doesn’t hard-code provider ids.

6. **Mobile and import**  
   - Mobile: Prefer reading/writing config via instance JSON and ProviderId, or via a small adapter that uses plugin-provided “default instance” or “legacy blob” if needed.  
   - Import: Confined to an importer that either uses plugin adapters or a separate compat assembly that knows ShareX’s UploadersConfig shape and produces instance JSON + legacy blobs.

---

## 4. Phased Refactor (High Level)

- **Phase 1 – Low risk (optional)**  
  - Keep `AmazonS3Settings`, `AmazonS3StorageClass`, and `S3Config` in core for legacy import, mobile, and sync; no requirement to move them to the plugin.  
  - Optionally introduce an `IInstanceSecretMigrator` (or similar) and move `InstanceManager`’s provider-specific branches behind it for one or two providers as a pilot.

- **Phase 2 – UploadersConfig**  
  - Replace `ServiceSettings` values with an opaque type (e.g. JObject or string) keyed by ProviderId (or keep UploaderType only as a compat key).  
  - Remove core’s dependency on `ImgurConfig`, `DropboxConfig`, `S3Config`, `FtpConfig` in sync logic by having plugins (or a compat module) perform the sync from/to legacy blobs.

- **Phase 3 – UploaderType and legacy (optional)**  
  - Use ProviderId everywhere in the plugin API; treat UploaderType as legacy/compat only.  
  - ShareX.UploadersLib and the importer can remain in core for legacy import and mobile support.

This gives a clear path to **self-contained plugins** with **governance and overall interface provided by** `src/desktop/core/XerahS.Uploaders`.
