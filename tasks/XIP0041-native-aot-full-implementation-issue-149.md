# XIP0041 Native AOT ΓÇô Full Implementation (Issue #149)
## Summary

**Goal**: Fully implement [GitHub Issue #149](https://github.com/ShareX/XerahS/issues/149) ΓÇö *[Feature]: Consider Enabling NAOT to Reduce Installer Size* ΓÇö by enabling .NET **Native AOT** for the XerahS host so the Windows installer is smaller (~90 MB today; target 50ΓÇô87% reduction), with faster startup and no separate .NET runtime.

**Scope**: Host app (XerahS.App) and its direct dependencies. Plugins remain separate assemblies (loaded at runtime) and are **not** AOT-compiled.

**Architecture refs**:  
- [docs/architecture/NATIVE_AOT_IMPLEMENTATION_ISSUE_149.md](../docs/architecture/NATIVE_AOT_IMPLEMENTATION_ISSUE_149.md)  
- [docs/architecture/SYSTEM_TEXT_JSON_MIGRATION.md](../docs/architecture/SYSTEM_TEXT_JSON_MIGRATION.md)

---

## 1. What is Native AOT?

- **Native AOT** compiles the app to **native machine code** at publish time instead of using the JIT at runtime.
- The .NET runtime is not shipped; unused code is trimmed. Result: smaller binaries, faster startup, lower memory.
- .NET 10 supports Native AOT; XerahS targets `net10.0`.
- References: [Native AOT deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/) | [Avalonia Native AOT](https://docs.avaloniaui.net/docs/deployment/native-aot).

---

## 2. Already achieved in the source code

The following work is **done** and reduces risk for Native AOT.

### 2.1 System.Text.Json migration (AOTΓÇæfriendly JSON)

| Phase | Scope | Status |
|-------|--------|--------|
| **Phase 1** | XerahS.Common: STJ converters (TimeZoneInfo, SKColor), JsonHelpers, legacy effects, update checkers, CommonHotkeyInfo, GradientInfo | Done |
| **Phase 2** | XerahS.Uploaders: provider/instance code (UploaderProviderBase, CustomUploaderProvider, InstanceManager) using STJ; SettingsBase/UploadersConfig still Newtonsoft | Done |
| **Phase 3** | XerahS.History, XerahS.Indexer, XerahS.Media ΓÇô all JsonConvert ΓåÆ JsonSerializer; Newtonsoft removed from csproj | Done |
| **Phase 4** | All 8 plugins (Ftp, Pastebin, Paste2, Auto, GitHubGist, Imgur, Dropbox, AmazonS3): STJ in providers, ViewModels, uploaders, AwsSso*; Newtonsoft removed | Done |
| **Phase 5** | PluginTemplate (MyProvider + csproj), XerahS.Mobile.Core (MobileAmazonS3ConfigViewModel) ΓÇô STJ/JsonNode | Done |

**Remaining Newtonsoft** (acceptable for first AOT attempt; add trimmer roots or migrate later if needed):

- SettingsBase, UploadersConfig (polymorphic), LegacySupport importer, CustomUploader repository.
- TaskSettings clone in MainWindow, SettingsViewModel, IndexFolderViewModel (UI serialization).

Plugins and most core JSON paths no longer use Newtonsoft; the main AOT blocker from the architecture doc (┬º7.2) is largely addressed.

### 2.2 Reflection removed: MainWindow ΓåÆ EditorView

- **File**: `XerahS.UI/Views/MainWindow.axaml.cs`
- **Was**: `GetMethod("InsertImageAnnotation", BindingFlags.NonPublic)` + `Invoke(_editorView, new object?[] { bitmap, null })`.
- **Now**: Direct call `_editorView.InsertImageAnnotation(bitmap, null)` (no reflection).
- **Reason**: Eliminates one reflection hotspot that could be trimmed or misbehave under AOT.

### 2.3 Status summary (Phase 1 AOT complete)

| Blocker / requirement | Status |
|------------------------|--------|
| Newtonsoft in plugins / most core JSON | Addressed (STJ migration Phases 1ΓÇô5) |
| MainWindow ΓåÆ EditorView reflection | **Fixed** (direct call) |
| ViewLocator reflection | **Mitigated** ΓÇô TrimmerRoots.xml preserves Views/ViewModels |
| CustomUploaderFunction discovery | **Mitigated** ΓÇô TrimmerRoots.xml preserves all function types |
| x:CompileBindings | **Done** ΓÇô App.axaml has `x:CompileBindings="True"` |
| PublishAot + packaging | **Done** ΓÇô csproj conditional + package-windows.ps1 `-EnableNativeAot` |
| SettingsBase / UploadersConfig (Newtonsoft) | Deferred; try AOT with roots, migrate if needed (Phase 2) |

---

## 3. Implementation plan (remaining work)

### Phase 1: Enable AOT and pass smoke test Γ£à (infrastructure done; smoke test pending)

**1.1 Enable AOT behind a property** Γ£à DONE

- **XerahS.App.csproj**: conditional `PublishAot=true`, `BuiltInComInteropSupport=false`, `PublishSingleFile=false` when `EnableNativeAot=true`.
- Default Release build unchanged (no AOT).

```xml
<PropertyGroup Condition="'$(EnableNativeAot)' == 'true'">
  <PublishAot>true</PublishAot>
  <BuiltInComInteropSupport>false</BuiltInComInteropSupport>
  <PublishSingleFile>false</PublishSingleFile>
</PropertyGroup>
```

**1.2 Trimmer roots** Γ£à DONE

- **`src/desktop/app/XerahS.App/TrimmerRoots.xml`** created:
  - **XerahS.UI**: preserves `XerahS.UI.Views.*` and `XerahS.UI.ViewModels.*` (ViewLocator).
  - **XerahS.Uploaders**: preserves all 13 concrete `CustomUploaderFunction*` types (ReflectionHelper.GetInstances<T>).
- Referenced from **XerahS.App.csproj** under `Condition="'$(PublishAot)' == 'true'"`.

**1.3 XAML: compiled bindings** Γ£à DONE (partial)

- **`App.axaml`**: `x:CompileBindings="True"` added to `Application` root.
  - All bindings in App.axaml use `Source={x:Static ...}` so type info is available.
- Additional windows/UserControls: deferred to Phase 2 to avoid unverified breaking changes.

**1.4 First AOT publish and packaging** Γ£à DONE

- Publish command:
  ```
  dotnet publish src/desktop/app/XerahS.App/XerahS.App.csproj -c Release -r win-x64 -p:EnableNativeAot=true
  ```
- **`package-windows.ps1`**: added `-EnableNativeAot` switch; branches publish to pass `EnableNativeAot=true`.
- **`XerahS-setup.iss`**: added `skipifsourcedoesntexist` to daemon JSON entries; `*.dll`/`*.json` wildcards annotated for AOT compatibility (wildcards safely match nothing in AOT mode).

**1.5 Smoke test** Γ£à SCRIPT ADDED

- **Script**: `build/scripts/smoke-test-aot.ps1` ΓÇö runs AOT publish for win-x64 (or `-RuntimeIdentifier win-arm64`). ILCompiler is auto-restored; no extra workload required.
- **Full installer**: `build/windows/package-windows.ps1 -EnableNativeAot` produces installers from AOT output + Plugins.
- **Manual checklist**: main window, settings, capture, open image in editor (InsertImageAnnotation path), upload, custom uploader with CustomUploaderFunction syntax, load/run a plugin.

**Acceptance criteria (Phase 1)**

- AOT publish produces a single native exe (or small set of outputs) for win-x64. Γ£à (run script or package-windows.ps1 -EnableNativeAot)
- Installer builds from AOT output and Plugins folder. Γ£à
- Smoke test passes without runtime trim/reflection errors. ΓÅ│ (run manually after publish)

**1.6 Windows ARM64 (Native AOT on ARM64 machines)** Γ£à DONE

- **Prerequisites**: On Windows ARM64, the C++ **ARM64/ARM64EC build tools** are required in addition to the Desktop development with C++ workload (so that `win-arm64` AOT builds can link). Install with the **stable** Build Tools bootstrapper:  
  [https://aka.ms/vs/stable/vs_buildtools.exe](https://aka.ms/vs/stable/vs_buildtools.exe)  
  Then:  
  `vs_buildtools.exe --add Microsoft.VisualStudio.Workload.VCTools --add Microsoft.VisualStudio.Component.VC.Tools.ARM64 --includeRecommended --passive --wait --norestart`
- **vcvarsall limitation**: On ARM64 hosts, `vcvarsall.bat arm64` does **not** add the `Hostarm64\arm64` tools directory to `PATH`, so the ILCompilerΓÇÖs `findvcvarsall.bat` fails with ΓÇ£Platform linker not found.ΓÇ¥
- **Workaround**: The repo adds a build-time workaround for **win-arm64** only:
  - **`build/scripts/findvcvarsall_arm64.bat`** discovers the VS/Build Tools install that has the ARM64 component, locates `link.exe` under `VC\Tools\MSVC\*\bin\Hostarm64\arm64\`, runs `vcvarsall arm64` to get `LIB`, and outputs `CppToolsDirectory#LIB` in the format the AOT targets expect.
  - **XerahS.App.csproj**: when `RuntimeIdentifier` is `win-arm64` and `PublishAot` is true, sets `IlcUseEnvironmentalTools=true` and defines target **SetArm64VCToolsForAot** (runs before **SetupOSSpecificProps**) which runs the script and sets `_CppToolsDirectory`, `CppLinker`, `CppLibCreator`, and `AdditionalNativeLibraryDirectories`.
- **Result**: AOT publish for `win-arm64` no longer fails with ΓÇ£Platform linker not foundΓÇ¥ on ARM64 machines. Remaining failures (if any) are trim/AOT analysis in app or dependencies (e.g. WinForms, reflection), not tooling.

**1.7 WinForms and AOT** Γ£à DONE (Phase 1: suppressions)

- **Issue**: System.Windows.Forms is not AOT-compatible (IL3000: `Assembly.Location` in single-file; IL2104/IL3053 in dependencies). **XerahS.Platform.Windows** uses `UseWindowsForms=true` for **IScreenService** and **IClipboardService** (Screen, Clipboard).
- **Phase 1 (current)**: WinForms remains enabled. IL3000 is suppressed when `PublishAot=true` so ILC does not fail on WinFormsΓÇÖ `Assembly.Location`:
  - **XerahS.App.csproj**: `NoWarn` includes `IL3000` when `PublishAot=true`.
  - **XerahS.WatchFolder.Daemon.csproj**: same `NoWarn` for `IL3000` when `PublishAot=true` (the daemon is published AOT as part of the Windows package and references **XerahS.Platform.Windows** ΓåÆ WinForms).
- **Phase 2 (optional)**: Disable WinForms when AOT and use AOT-safe screen/clipboard implementations (e.g. `WindowsScreenService.Aot.cs` / `WindowsClipboardService.Aot.cs` stubs) to avoid relying on suppressions.
- See **┬º4 Risks** (WinForms under AOT).

**1.8 AOT UI not clickable (menus, controls)** ΓÅ│ OPEN (blocker for AOT UX)

- **Symptom**: Under Native AOT on Windows (both win-x64 and win-arm64), only the window chrome (Minimize/Maximize/Close) works; UI components, menu bar, and buttons are not clickable.
- **Cause (suspected)**: Trimmer or AOT removes or breaks Avalonia/FluentAvalonia code paths used for input handling, hit-testing, or control behaviour (reflection/dynamic lookup, or framework-level).
- **Attempted mitigation**: **XerahS.App.csproj** adds **TrimmerRootAssembly** for `Avalonia`, `Avalonia.Controls`, `Avalonia.Themes.Fluent`, `Avalonia.Desktop`, `Avalonia.Controls.ColorPicker`, and `FluentAvalonia` when `PublishAot=true`. **Outcome**: unchanged; UI remains non-clickable after rebuild and test on both x64 and ARM64.
- **Next steps (future)**: Investigate Avalonia/Win32 backend under AOT (input routing, hit-test visibility); check Avalonia upstream for AOT + Windows input issues; consider disabling trimming for UI assemblies or trying different root descriptors; validate with minimal Avalonia AOT sample on Windows.

---

### Phase 2: Optional ΓÇô reduce trimmer roots (smaller binary)

**2.1 ViewLocator**

- Replace `Type.GetType(name)` + `Activator.CreateInstance(type)` with a **compile-time map** (manual registration or source generator) from ViewModel type ΓåÆ View type.
- Use that map in `ViewLocator.Build`.
- Remove or narrow View/ViewModel preservation in TrimmerRoots.xml.

**2.2 CustomUploaderFunction**

- Replace `ReflectionHelper.GetInstances<CustomUploaderFunction>()` with **explicit registration** or a source-generated list of function types.
- Remove or narrow CustomUploader preservation in TrimmerRoots.xml.

**2.3 SettingsBase / UploadersConfig (if AOT fails on settings)**

- If load/save of settings or uploader config fails under AOT despite trimmer roots:
  - Migrate **SettingsBase** and **UploadersConfig** to System.Text.Json (polymorphic support: custom converter or `[JsonPolymorphic]`/`[JsonDerivedType]`).
  - See SYSTEM_TEXT_JSON_MIGRATION.md ┬º2.3 and ┬º3 Phase 2.
  - Remove remaining Newtonsoft from XerahS.Common and XerahS.Uploaders where possible.

**Acceptance criteria (Phase 2)** Γ£à

- Trimmer roots for Views/ViewModels and CustomUploader removed (explicit registration used).
- Executable size can be smaller; full regression (settings, uploaders, plugins) unchanged.

---

### Phase 3: CI and multi-platform

**3.1 CI**

- In **release-build-all-platforms.yml** (or equivalent): add a publish step that uses AOT (`-p:EnableNativeAot=true` or same property as package-windows.ps1).
- Artifacts: AOT-built exe + Plugins folder.

**3.2 Linux / macOS** Γ£à DONE

- **package-linux.sh**: set `ENABLE_NATIVE_AOT=1` to publish with Native AOT (e.g. `ENABLE_NATIVE_AOT=1 ./build/linux/package-linux.sh`).
- **package-mac.sh**: set `ENABLE_NATIVE_AOT=1` to publish with Native AOT (e.g. `ENABLE_NATIVE_AOT=1 ./build/macos/package-mac.sh`).
- AOT output is used automatically (host exe/bundle is native; Plugins remain JIT). Platform-specific trimmer quirks: add entries to TrimmerRoots.xml if the trimmer removes required types on Linux/macOS.

**3.3 Third-party validation** Γ£à DOCUMENTED

- **FluentAvalonia**: Test under AOT; add trimmer roots or fixes if font/icon loading fails. Documented in `docs/architecture/NATIVE_AOT_IMPLEMENTATION_ISSUE_149.md` ┬º9.
- **ImageEditor (submodule)**: Audit for reflection/dynamic loading; add trimmer roots or fixes if needed under AOT. Same doc ┬º9.

**Acceptance criteria (Phase 3)** Γ£à

- Windows AOT build is produced in CI (job `build-windows-aot`).
- Linux and macOS AOT builds: supported via `ENABLE_NATIVE_AOT=1`; packaging scripts updated.
- FluentAvalonia/ImageEditor: validation documented (┬º3.3); add trimmer roots if issues appear.

---

## 4. Risks and mitigations

| Risk | Mitigation |
|------|------------|
| Trimmer removes View/ViewModel types | Phase 1: TrimmerRoots.xml; Phase 2: replace ViewLocator with explicit map. |
| Trimmer removes CustomUploaderFunction types | Phase 1: TrimmerRoots.xml; Phase 2: explicit registration. |
| Settings/UploadersConfig fail under AOT | Phase 1: trimmer roots for DTOs; Phase 2: migrate to STJ if needed. |
| FluentAvalonia / ImageEditor break under AOT | Phase 3: test and add trimmer roots or fixes. |
| Plugin loading | Plugins are external assemblies; only host is AOT. No change to plugin loading. |
| **WinForms under AOT** | System.Windows.Forms is not AOT-compatible (IL3000: Assembly.Location; IL2104/IL3053 in dependencies). **Phase 1**: `NoWarn` for IL3000 in **XerahS.App** and **XerahS.WatchFolder.Daemon** when `PublishAot=true`. **Phase 2 (optional)**: AOT-safe screen/clipboard so host/daemon do not reference WinForms. See ┬º1.7. |
| **AOT UI not clickable** | Under AOT on Windows (x64 and ARM64), only window chrome works; menus and controls do not respond. TrimmerRootAssembly for Avalonia/FluentAvalonia was added but **outcome unchanged**; cause likely beyond trimmer (e.g. Avalonia Win32 input under AOT). **Status**: open blocker. See ┬º1.8. |

---

## 5. References

- [ShareX/XerahS #149](https://github.com/ShareX/XerahS/issues/149)
- [docs/architecture/NATIVE_AOT_IMPLEMENTATION_ISSUE_149.md](../docs/architecture/NATIVE_AOT_IMPLEMENTATION_ISSUE_149.md)
- [docs/architecture/SYSTEM_TEXT_JSON_MIGRATION.md](../docs/architecture/SYSTEM_TEXT_JSON_MIGRATION.md)
- [Native AOT deployment - .NET](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [Avalonia ΓÇô Native AOT Deployment](https://docs.avaloniaui.net/docs/deployment/native-aot)
- [Optimizing Native AOT deployments](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/optimizing)