---
name: mobile-android-ios-parity
description: Compare or implement native Android parity with src/mobile/ios. Excludes experimental MAUI/Avalonia apps.
metadata:
  keywords:
    - mobile
    - android
    - ios
    - parity
    - kotlin
    - swift
    - compose
    - emulator
    - adb
    - share intent
    - upload
---

# Mobile Android/iOS Parity

Use this skill when the goal is for `src/mobile/ios` and `src/mobile/android` to represent the same user-facing act while remaining native implementations under the hood.

Feature parity means: same user outcome, same data semantics, same error handling quality, and equivalent workflows. It does not mean copying Swift code into Android, copying Compose UI into iOS, or forcing identical platform chrome.

## Non-negotiables

- Keep Android native: Kotlin, Gradle, Android APIs, Jetpack Compose, Android lifecycle, intents, storage, permissions, and platform services.
- Keep iOS native: Swift, SwiftUI/UIKit, extensions, app groups, iOS storage/security APIs.
- Do not replace `src/mobile/android` with Swift-on-Android for app UI or platform integration.
- Use Swift/iOS code as the behavioral specification for Android, not as code to mechanically port.
- Preserve file formats and persisted semantics where users expect cross-platform compatibility: app config JSON, queue/history records, `.sxcu`, `.xsdc`, upload result fields, destination IDs, generated URLs, and error text where practical.
- Update docs and durable lessons when the implementation reveals repo-specific mobile behavior.

## First pass

1. Read `AGENTS.md` and the nearest mobile docs before editing.
2. Read:
   - `src/mobile/ios/README.md`
   - `src/mobile/android/README.md`
   - Relevant iOS files under `src/mobile/ios/XerahSMobile` and `src/mobile/ios/ShareExtension`
   - Relevant Android files under `src/mobile/android/app`, `core`, and `feature`
3. Create a parity matrix before coding:

```markdown
| Behavior | iOS source | Android source | Status | Android native plan |
| --- | --- | --- | --- | --- |
| Share one file from Photos/Files | ... | ... | missing/partial/done | ... |
```

Keep the matrix focused on observable behavior. Do not spend time documenting unrelated implementation details.

## Parity areas

Use these areas as the default checklist. Add or remove rows only when the current task clearly scopes the work differently.

- App launch, loading state, and navigation defaults.
- Share extension / Android share intent receiving for one item and multiple items.
- Accepted file types: images, videos, audio, PDFs, text, arbitrary files, URLs, `.sxcu`, and `.xsdc`.
- Copying shared content into app-owned storage without losing useful file names or extensions.
- Upload queue processing, progress presentation, cancellation/error behavior, and retry-safe persistence.
- Upload destinations: Amazon S3 and custom uploaders.
- S3 options: region, custom endpoint, path-style behavior, custom domain result URL, signed payload, public ACL.
- Custom uploader execution: methods, headers, parameters, body types, argument expansion, result URL extraction, thumbnail/deletion/error expressions.
- HEIC/HEIF to PNG conversion using native platform image APIs.
- Settings screens and validation.
- History list, search, copy, delete, clear, and persisted history schema.
- Clipboard behavior for uploaded links and copied errors.
- Import/export or open-in-place behavior for supported config files.
- About/app metadata and store-facing documentation when user-facing behavior changes.

## Implementation rules

- Prefer existing Android module boundaries: `app`, `core:common`, `core:domain`, `core:data`, and `feature:*`.
- Keep pure domain models in `core:domain`; keep Android framework code out of domain models.
- Keep repository/storage/upload code in `core:data` unless the existing structure clearly says otherwise.
- Keep Compose UI in feature modules and `app` navigation.
- Use Android equivalents, not iOS abstractions:
  - Share extension -> `ACTION_SEND`, `ACTION_SEND_MULTIPLE`, `ACTION_VIEW`, content URI handling.
  - App Group container -> app-specific files/cache and Android-scoped storage.
  - Keychain -> Android Keystore or encrypted preferences when secrets need native protection.
  - UIKit/SwiftUI views -> Compose screens and Material components.
  - `UIImage`/iOS image decoding -> Android `ImageDecoder`, `Bitmap`, or platform media APIs.
- When iOS and Android already have separate implementations for the same concept, align behavior through tests and shared file/data contracts rather than introducing a cross-platform abstraction prematurely.
- Keep UI idiomatic for Android. Equivalent workflow is required; pixel-identical UI is not.

## Build workflow

For Android build/deploy work, use `.ai/skills/build-android/SKILL.md`.

Default Kotlin app commands from `src/mobile/android`:

```bash
./gradlew assembleDebug --console=plain
./gradlew test --console=plain
```

If working on root/shared code or pushing changes, follow `AGENTS.md` build integrity rules. Do not disable warnings-as-errors.

## Commit workflow

Commit each completed parity slice before starting the next unrelated feature slice when the repo is in a committable state.

1. Define the smallest user-facing slice: examples include `.sxcu` import, secure secret persistence, About screen parity, or share-intent intake.
2. Implement only the files needed for that slice.
3. Run the narrowest meaningful verification for the slice. For Android app code this is normally `./gradlew assembleDebug --console=plain` plus focused emulator QA when the flow is user-visible.
4. Stage only that slice's files. Do not mix unrelated parity features in the same commit.
5. Use the repo commit prefix rules from `AGENTS.md`: read root `Directory.Build.props` `<Version>`, compare with the latest XerahS tag, bump first if needed, and commit as `[vX.Y.Z] [Type] Concise description`.
6. Continue with the next slice from a clean working tree, except for intentionally deferred docs or skill updates.

Good slice boundaries from this parity work:

- `.sxcu`/`.xsdc` import and Android intent routing.
- Android Keystore protection for persisted secrets.
- Settings/About navigation and metadata/link parity.
- Share-intent filename/MIME robustness.
- Skill/docs updates that capture lessons learned.

## Test Android Apps QA

Use the `Test Android Apps: android-emulator-qa` workflow for emulator verification after each meaningful parity slice.

Minimum QA loop:

1. Build and install the Android app.
2. Launch with adb.
3. Drive the exact parity flow using adb input and UI-tree-derived coordinates.
4. Capture:
   - screenshot of the final state
   - compact UI tree summary when useful
   - logcat for crashes or upload failures
5. Compare against the iOS behavior contract or iOS screenshots/manual observations.
6. Record any behavior still missing in the parity matrix.

Do not claim parity from code inspection alone when the behavior can be exercised in an emulator.

## Lessons learned

- Check Java and Android SDK paths before the first Gradle run. This repo may need `JAVA_HOME=/usr/local/opt/openjdk@17/libexec/openjdk.jdk/Contents/Home` and `ANDROID_HOME=/usr/local/share/android-commandlinetools` on the current macOS workstation.
- If Gradle install fails with `Can't find service: package`, wait for the emulator to finish booting and package manager to come up, then retry install with adb.
- Avoid validating share/open flows with shell-only `file://` intents. They do not model Android content URI grants from Files, Photos, or the share sheet. Prefer a real `content://` source, a provider-backed test fixture, or manual Files/share-sheet QA.
- Avoid local cleartext HTTP for deep-link import QA unless the app explicitly allows it for debug. Use a real HTTPS URL for `xerahs://import-sxcu?url=...` verification, or document that only routing was exercised.
- When an iOS feature uses Keychain, Android parity should normally use Android Keystore or encrypted preferences; do not persist sensitive config JSON fields in plain text just because the cross-platform file format is JSON.
- Update both mobile READMEs when the user-facing feature list changes, even if only one platform's implementation changed.

## Flow-specific QA prompts

Use these as starting points for emulator validation:

- Share receive: send one content URI and multiple content URIs into `com.getsharex.xerahs.mobile`; verify Upload opens with the same item count and usable display names.
- Settings: configure S3/custom uploader, rotate away/back or restart, and verify values persist.
- Upload: run success and failure paths; verify links/errors are copyable and history records are created only when appropriate.
- History: add multiple entries, search, copy one URL, delete one entry, clear all.
- Config import: open/share `.sxcu` and `.xsdc`; verify imported destinations appear and validation/error messages match the iOS contract.
- HEIC: share a HEIC/HEIF file on API levels that support decoding; verify PNG conversion result and fallback behavior.

## Acceptance criteria

A parity slice is done only when:

- The Android implementation uses native Android/Kotlin mechanisms.
- The user-visible behavior matches the iOS contract for the scoped feature.
- Existing Android behavior is not regressed.
- Gradle build/test for affected Android modules passes, or any blocker is documented with exact command output.
- Emulator QA evidence exists for the critical path, unless no emulator/device is available and that limitation is stated.
- Documentation, README, store submission text, or lessons are updated when the user-facing feature set changed.

## Reporting

In the final response for parity work, include:

- The scoped iOS behavior used as the contract.
- Android files changed.
- Build/test commands run.
- Emulator QA performed with device/API level when available.
- Remaining parity gaps.
