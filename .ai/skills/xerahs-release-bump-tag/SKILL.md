---
name: xerahs-release-bump-tag
description: "Orchestrate XerahS release flow in strict order: maintenance-chores first, update-changelog second (optional if no CHANGELOG), verify build, bump/commit/push/tag while syncing Chocolatey version metadata, monitor GitHub Actions every 2 minutes, ensure standard release notes content, then optionally set pre-release. On failures, inspect logs, fix root cause, and retry with the next patch release."
---

# XerahS Release Bump Tag

## Overview

Use this skill to run release steps in strict order:
- Step 1: Execute maintenance chores first (`git pull --recurse-submodules` and `git submodule update --init --recursive`)
- Step 2: Run `.ai/skills/update-changelog/SKILL.md` second (optional if no `CHANGELOG.md` exists)
- Step 3: Verify build, then execute bump/commit/push/tag automation
- Step 4: Monitor the tag-triggered release workflow every 2 minutes
- Step 5: If failure occurs, inspect logs, fix issues, and retry with the next patch version
- Step 6: Ensure standard release notes block is present on the GitHub release
- Step 7: If requested, set the successful release as pre-release

Step 3 performs:
- Pre-check: Run `dotnet build src/desktop/XerahS.sln`; do not proceed if build fails.
- Prompts for `x/y/z` bump type (major/minor/patch) unless specified.
- Updates every tracked `Directory.Build.props` file that defines `<Version>`.
- Syncs `build/windows/chocolatey/xerahs.nuspec` `<version>` with the release version.
- Stages all current repo changes.
- Commits with version-prefixed message.
- Pushes current branch and creates/pushes annotated tag `vX.Y.Z`.

Step 4-5 performs:
- Find tag run for `Release Build (All Platforms)`.
- Poll run status every 120 seconds until completion.
- On failure, inspect failing job logs and identify first blocking error.
- Fix root cause in code/workflow/scripts.
- Re-run local pre-check build.
- Retry release using next patch bump, then monitor again.
- Repeat until workflow succeeds.

Step 6 performs:
- Ensures release notes always include:
  - `Change log:`
  - `https://xerahs.com/changelog.html`
  - `### macOS Troubleshooting ("App is damaged")` section with Gatekeeper `xattr -cr` guidance.
- After the release is published, Chocolatey checksums can be synchronized with `build/windows/chocolatey/Sync-ChocolateyPackage.ps1 -Version X.Y.Z`.

## Primary Command

From repository root:

```bash
./.ai/skills/xerahs-release-bump-tag/scripts/run-release-sequence.sh
```

Automated monitor + pre-release (recommended):

```bash
./.ai/skills/xerahs-release-bump-tag/scripts/run-release-sequence.sh --assume-changelog-done --monitor --set-prerelease --bump z --yes
```

Manual monitor (fallback, PowerShell example):

```powershell
gh run list --limit 10 --json databaseId,workflowName,headBranch,status,conclusion,url
Start-Sleep -Seconds 120
gh run view <run-id> --json status,conclusion,jobs,url
```

## Non-Interactive Examples

Patch bump, no prompts:

```bash
./.ai/skills/xerahs-release-bump-tag/scripts/run-release-sequence.sh --assume-changelog-done --bump z --yes
```

Patch bump with built-in 2-minute monitoring:

```bash
./.ai/skills/xerahs-release-bump-tag/scripts/run-release-sequence.sh --assume-changelog-done --monitor --monitor-interval 120 --bump z --yes
```

Minor bump with custom commit token/summary:

```bash
./.ai/skills/xerahs-release-bump-tag/scripts/run-release-sequence.sh --assume-changelog-done --bump y --type CI --summary "Prepare release artifacts" --yes
```

Preview only:

```bash
./.ai/skills/xerahs-release-bump-tag/scripts/run-release-sequence.sh --assume-changelog-done --bump z --dry-run --yes
```

## When bash is unavailable (e.g. Windows PowerShell)

On environments where `bash` is not in PATH, execute the sequence manually:

1. Step 1 - Maintenance
   - `git pull --recurse-submodules`
   - `git submodule update --init --recursive`

2. Step 2 - Changelog
   - Run `.ai/skills/update-changelog/SKILL.md`.
   - Skip if no `CHANGELOG.md` or user confirms skip.

3. Step 3 - Bump, commit, push, tag
   - Run `dotnet build src/desktop/XerahS.sln`; abort if it fails.
   - Read current version from root `Directory.Build.props`.
   - Compute next version: patch `Z+1`, minor `Y+1.0`, major `X+1.0.0`.
   - Ensure tag `v<new-version>` does not exist locally or on `origin`.
   - Update all tracked `Directory.Build.props` files containing `<Version>`.
   - Update `build/windows/chocolatey/xerahs.nuspec` `<version>` to match.
   - `git add -A` -> `git commit -m "[v<new-version>] [CI] Release v<new-version>"` -> `git push origin <current-branch>` -> `git tag -a v<new-version> -m "v<new-version>"` -> `git push origin v<new-version>`.

4. Step 4 - Monitor every 2 minutes
   - Find run: `gh run list --limit 10 --json databaseId,workflowName,headBranch,status,conclusion,url`
   - Poll: `Start-Sleep -Seconds 120`; then `gh run view <run-id> --json status,conclusion,jobs,url`

5. Step 5 - On failure, fix and retry
   - Fetch failed job logs: `gh run view <run-id> --job <job-id> --log`
   - Fix root cause in repository.
   - Re-run `dotnet build src/desktop/XerahS.sln`.
   - Repeat Step 3 with next patch version.

6. Step 6 - Ensure standard release notes content
   - Read current body: `gh release view v<new-version> --json body`
   - Append the standard changelog + macOS troubleshooting block if missing.
   - Write body: `gh release edit v<new-version> --notes-file <file>`

7. Step 7 - Set pre-release (when requested)
   - `gh release edit v<new-version> --prerelease`
   - Verify: `gh release view v<new-version> --json isPrerelease,url,assets`

8. Optional post-release Chocolatey sync
   - `powershell -File build/windows/chocolatey/Sync-ChocolateyPackage.ps1 -Version <new-version> -Pack`
   - Optionally push after review: `powershell -File build/windows/chocolatey/Sync-ChocolateyPackage.ps1 -Version <new-version> -Pack -Push -ApiKey <key>`

Default bump when unspecified: patch (`z`). Default commit type token: `CI`.

## Behavior

1. Require completion of `maintenance-chores` first.
   - Script behavior: executes maintenance commands automatically unless explicitly bypassed with `--skip-maintenance` (or legacy alias `--assume-maintenance-done`).
2. Require completion of `update-changelog` second (skip if no `CHANGELOG.md` or user confirms).
3. Before bump, run `dotnet build src/desktop/XerahS.sln`; abort on failure.
4. Run `scripts/bump-version-commit-tag.sh` (or PowerShell/manual equivalent when bash unavailable).
5. After tag push, monitor the release workflow every 120 seconds until complete.
6. If failed, inspect logs, fix root cause, and retry with next patch version.
7. Continue retry loop until release workflow is successful.
8. Ensure standard release notes content is present on the successful release.
9. If requested, mark successful release as pre-release.

## Guardrails

- Do not skip sequence unless user explicitly requests bypass.
- Do not skip maintenance unless user explicitly requests bypass (`--skip-maintenance`).
- Do not commit/push during maintenance/changelog steps.
- Always verify build before bump/tag.
- Always monitor workflow after tag push; do not stop at tag creation.
- Always inspect logs on failure and fix root cause before retry.
- Always ensure the standard release notes block exists on the successful release.
- Always use a new patch version for retries requiring new commits/tags.
- Abort on detached HEAD.
- Abort if version format is not `X.Y.Z`.
- Abort if matching tag already exists locally or remotely.
- Support `--no-push` and `--no-tag` when partial flow is needed.

## Agent usage (Cursor / Codex)

When executing this skill:
1. Run sequence: maintenance -> changelog -> build verify -> bump/commit/push/tag.
2. Use bash scripts if bash exists; otherwise use PowerShell/manual flow.
3. Default bump is patch (`z`) when unspecified.
4. Monitor tag workflow every 120 seconds until completion.
5. On failure, inspect logs, fix issue, and retry with next patch version.
6. Ensure release notes include changelog link + macOS troubleshooting block.
7. If requested, set the final successful release to pre-release.
8. Report final version, commit hash, branch push status, tag push status, run URL, and pre-release status.

## Notes (lessons learnt)

- Windows/PowerShell: bash may be unavailable; manual fallback must be first-class.
- Build before bump: avoid tagging broken trees.
- Changelog optional: do not block if `CHANGELOG.md` does not exist unless user requires it.
- Version sync: update every tracked `Directory.Build.props` with `<Version>` and sync `build/windows/chocolatey/xerahs.nuspec`.
- Chocolatey asset naming: `build/windows/chocolatey/tools/chocolateyInstall.ps1` resolves `XerahS-<version>-win-x64.exe` or `XerahS-<version>-win-arm64.exe` from `ChocolateyPackageVersion`, so release bumps should not hardcode installer filenames there.
- Chocolatey checksums for community publication are post-release data because GitHub release assets do not exist until after the tag workflow completes. Use `build/windows/chocolatey/Sync-ChocolateyPackage.ps1` after the release is live.
- Release reliability loop: tag push is not the end; monitor, fix, and retry until green.
