---
name: run-maintenance
description: Prepare repository and submodule state for requested maintenance or release work. Do not trigger for routine edits.
---

# Maintenance Prep Skill

This skill prepares the XerahS workspace for release, changelog, or routine repository maintenance. It is not the source of truth for commit, push, tag, or version-prefix rules.

Canonical references:
- Git/version/commit/push rules: [git-workflow](../git-workflow/SKILL.md)
- Changelog generation and grouping rules: [update-changelog](../update-changelog/SKILL.md)
- Full release bump/tag automation: [publish-release](../publish-release/SKILL.md)
- General repository policy: [AGENTS.md](../../../AGENTS.md)

## Scope

Use this skill to:
- Sync the main repository and submodules.
- Inspect sibling repositories when the maintenance task explicitly touches them.
- Identify whether a version bump or changelog update is needed.
- Run the relevant pre-commit build after changes are made.
- Leave final staging, commit, push, and tag decisions to `git-workflow` or `publish-release`.

Do not use this skill to invent commit message formats, submodule version prefixes, release tags, or changelog grouping rules.

## Workflow

### 1. Inspect Status Before Sync

Run from the XerahS repository root before any pull or submodule update:

```powershell
git status --short
git submodule foreach --recursive "git status --short"
```

For optional sibling repositories involved in the task:

```powershell
git -C ../xerahs.github.io status --short
git -C ../ShareX status --short
```

If the main repo, submodules, or in-scope sibling repositories have local changes, do not pull over them. Commit, stash, or explicitly pause for the owner to decide. Submodule changes belong to that submodule repository.

### 2. Sync Clean Repository State

Run from the XerahS repository root only after the relevant repositories are clean:

```powershell
git pull --recurse-submodules
git submodule update --init --recursive
```

`git submodule update` can detach submodule HEADs because it checks out the exact commit recorded by the parent repository. Mandatory follow-up: reattach `ShareX.ImageEditor` to its tracked `develop` branch and fast-forward it before release or changelog work continues:

```powershell
git -C ShareX.ImageEditor fetch origin --prune
git -C ShareX.ImageEditor checkout develop
git -C ShareX.ImageEditor pull --ff-only origin develop
git submodule status ShareX.ImageEditor
git -C ShareX.ImageEditor status --short --branch
```

Abort and resolve manually if `ShareX.ImageEditor` cannot fast-forward, has local changes, or remains detached. If the fast-forward changes the submodule commit recorded by XerahS, commit and push the submodule first, then commit the updated XerahS gitlink in the parent repository.

If the task also touches optional sibling repositories, sync only those clean repositories:

```powershell
git -C ../xerahs.github.io pull origin main
git -C ../ShareX pull origin develop
```

Do not pull optional sibling repositories just because they exist.

If a submodule has its own changes, treat it as a separate repository. Shared-library submodule commits, such as `ShareX.ImageEditor`, must follow `git-workflow` and omit the XerahS app version prefix.

### 3. Check Version Needs

Read the current XerahS app version from the root `Directory.Build.props`:

```powershell
$version = (Select-String -Path Directory.Build.props -Pattern '<Version>(.+?)</Version>').Matches[0].Groups[1].Value
git tag --sort=-v:refname | Select-Object -First 1
```

Only bump versions when the task is explicitly about versioning or release preparation. If a bump is needed, use [git-workflow](../git-workflow/SKILL.md) for version source-of-truth and prefix rules, or [publish-release](../publish-release/SKILL.md) for the full release bump/tag sequence.

SkiaSharp remains centrally managed in `Directory.Packages.props`; do not reintroduce project-local legacy pins.

### 4. Update Changelog When Needed

For changelog work, use [update-changelog](../update-changelog/SKILL.md). Prefer the helper script for draft generation:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .ai/skills/update-changelog/scripts/update-changelog.ps1
```

Manually review generated changelog text for accurate grouping, contributor attribution, and wording before release.

### 5. Build Gate

For code/config changes, run the required build before pushing:

```powershell
dotnet build src/desktop/XerahS.sln
```

Do not stop a build solely because a fixed amount of time elapsed. If it genuinely stalls, clear stale build processes/locks, prefer `-m:1`, then retry.

Documentation-only skill edits do not require a full solution build unless they affect build or release scripts that need local execution.

### 6. Commit And Push Handoff

Use [git-workflow](../git-workflow/SKILL.md) for final staging, commit message, version prefix, and push behavior.

Summary:
- XerahS app commits use `[vX.Y.Z] [Type] description` only after verifying the root version is ahead of the latest tag.
- Shared library or submodule-only commits omit `[vX.Y.Z]` and use `[Type] description`.
- Submodule commits are pushed before the parent repository when the parent records an updated submodule pointer.
- Do not tag releases from this skill; use [publish-release](../publish-release/SKILL.md).

## Checklist

- [ ] Main repo and relevant submodule/sibling statuses inspected.
- [ ] Local changes handled before pulling or updating submodules.
- [ ] Main repository synced with `git pull --recurse-submodules`.
- [ ] Submodules initialized and updated.
- [ ] `ShareX.ImageEditor` reattached to `develop`, fast-forwarded from `origin/develop`, and verified not detached.
- [ ] Relevant sibling repositories synced only if they are in scope.
- [ ] Version bump need evaluated against `Directory.Build.props` and latest tag.
- [ ] Changelog update delegated to `update-changelog` when needed.
- [ ] Required build run for code/config changes.
- [ ] Commit/push/tag rules delegated to `git-workflow` or `publish-release`.
