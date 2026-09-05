# Windows portable packages

Use this reference for a local portable build or to verify portable assets during a tagged release. Windows x64 and ARM64 are supported; macOS/Linux archives retain their existing packaging and settings behavior.

## Build locally

From the XerahS repository root on Windows:

```powershell
.\build\windows\package-windows.ps1 -PortableOnly
```

This publishes both architectures with the repository's .NET SDK and Node/npm toolchain, bundles plugins and the VideoEditor WebUI, and writes:

- `dist/XerahS-X.Y.Z-win-x64-portable.zip`
- `dist/XerahS-X.Y.Z-win-arm64-portable.zip`

The version comes from root `Directory.Build.props`. Portable-only packaging needs neither Inno Setup nor WiX. Do not run the release sequence, bump versions, or create tags merely to produce these local files.

For all Windows formats, run `build/windows/package-windows.ps1` without the switch. It creates a portable ZIP from the same self-contained publish payload used for each architecture's EXE/MSI installers. Inno Setup is required; local MSI creation requires the pinned WiX toolchain documented in the parent skill. CI requires all six Windows assets.

For an existing **clean, complete self-contained** publish directory, the archive helper can be run independently:

```powershell
.\build\windows\package-portable.ps1 `
    -PublishDirectory C:\build\xerahs-win-x64 `
    -Version X.Y.Z -Runtime win-x64 -OutputDirectory .\dist
```

Use a numeric version and the architecture actually published. The helper packages existing files; it does not compile them or convert architecture. Do not point it at an installed/user-data directory or a plain framework-dependent build output.

## Layout and portable behavior

- `XerahS.exe` and an empty `portable.txt` are at the ZIP root, with bundled DLLs, native runtime files, plugins, the watch-folder daemon, licenses, and `frontend/dist` retained. PDB symbols are omitted.
- XerahS uses `portable.txt`, whereas classic ShareX uses the extensionless `Portable` marker. Do not copy ShareX's marker filename.
- The Windows runtime defaults to an `XerahS` data folder beside the executable when the marker exists. Explicit personal-folder overrides still take precedence.
- Extract the entire archive into a writable folder and launch `XerahS.exe`; do not launch from inside the ZIP. No installer or separately installed .NET runtime is required.
- Keep `portable.txt` exclusively in the ZIP. The helper does not mutate the publish source, so EXE/MSI installations retain their normal settings behavior.
- The architecture-specific `-win-<arch>-portable.zip` name is part of the updater contract. Keep packaging, updater selection, workflow upload paths, CI validation, and the release skill's expected asset list aligned when changing it.

## Validation and release checks

1. Run `build/windows/test-package-portable.ps1` for archive-helper fixture checks and `python -B -m unittest discover -s build/ci -p 'test_validate_release_assets.py'` for release-validator checks.
2. During the tag workflow, `build/ci/validate_release_assets.py` checks the release matrix, portable contents, and generates SHA-256/size metadata before upload. Do not accept a ZIP filename alone as proof of a portable payload.
3. For a Windows smoke test, extract the ZIP for the host architecture into a fresh writable directory. Start XerahS, change a setting, exit, and reopen. Confirm settings persist under the adjacent `XerahS` folder, plugins load, and the VideoEditor opens. Verify on native ARM64 hardware separately when available; inspecting both ZIPs does not prove both architectures launch.
4. For an upgrade, close XerahS and its helpers first, back up the adjacent `XerahS` data folder, then extract the matching architecture's ZIP over the application folder while retaining that data folder and `portable.txt`. Do not promise automatic in-place installation of portable updates; verify the current updater flow.
5. After a tagged release succeeds, confirm both portable ZIPs and all four Windows installers are attached to the selected GitHub repository. `run-release-sequence.sh` requires both ZIP names during its post-release asset checks. Retain the repository's existing pre-release/latest policy.

Report local artifact paths for a local build. For publication, also report the version, tag, workflow URL, repository, channel, and any smoke-test limitations.
