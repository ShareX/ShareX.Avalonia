# XerahS agent instructions

XerahS is the Avalonia implementation of ShareX.

## Repository invariants

- Preserve unrelated working-tree changes. Stay on the current branch; create branches only when explicitly requested.
- Keep `TreatWarningsAsErrors` enabled. Preserve each project's platform conditions: Windows-specific targets use `net10.0-windows10.0.26100.0`; cross-platform targets use `net10.0`.
- Keep `SkiaSharp` and all `SkiaSharp.NativeAssets.*` packages on the same version in root `Directory.Packages.props`. Use central package management; do not restore legacy project-local pins.
- In Windows PowerShell, use separate commands or `if ($?) { ... }` for conditional sequencing; do not use `&&`.

## Git identity

All Git activity must use the operator's own wrapper when available. Never choose another person's identity.

| Agent | Wrapper |
|---|---|
| Aoife | `git-aoife` |
| Mikhail | `git-mikhail` |
| Declan | `git-declan` |
| Vladislava | `git-vladislava` |

Run `git-<person> whoami` before pushing. Only when the matching wrapper is unavailable, verify `git config --get user.name`, `git config --get user.email`, and `git remote -v`, then use configured Git authentication for ShareX/XerahS or the intended fork. Do not edit identity or remotes unless asked.

For commits, pushes, and version changes, use [.ai/skills/git-workflow/SKILL.md](.ai/skills/git-workflow/SKILL.md). XerahS commit prefixes must use the next unreleased app version; shared-library commits omit the app version.

## Scope and completion

- Continue authorized implementation through integration and relevant verification. Resolve routine local decisions; ask only for missing decisions that materially change the requested outcome or authority.
- Issue creation/updates require an explicit request. Publishing, releases, billing, production changes, secrets exposure, and irreversible user-data deletion require authorization covering that action.
- Before pushing code or build configuration, `dotnet build src/desktop/XerahS.sln` must pass with 0 errors, plus relevant tests. Documentation-only changes need diff, link, and instruction-consistency checks, not an application build. For a version-only root `Directory.Build.props` bump, verify the exact version diff and commit prefix.
- Complete the requested behavior, including affected callers and failure paths; report verification evidence and any remaining blocker. Do not stop at a draft when implementation was requested.
- Judge build progress from output and process activity, without a fixed time limit. Stop only confirmed stalled task processes; consult [.ai/skills/build-common/SKILL.md](.ai/skills/build-common/SKILL.md) for lock recovery.
- Load only matching workflows from `.ai/skills/` and the references needed for the task. Keep always-on policy here, compatibility files thin, and durable lessons in `developers/lessons-learnt/`.
