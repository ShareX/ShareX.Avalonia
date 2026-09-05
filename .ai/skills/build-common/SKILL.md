---
name: build-common
description: Recover XerahS build stalls, file locks, or shared-output races. Use with platform build workflows when needed.
---

# Build recovery

Keep the build and dependency invariants in [AGENTS.md](../../../AGENTS.md). Platform skills own packaging commands and artifact validation.

Do not run concurrent builds that share this checkout's outputs. Prefer single-node MSBuild (`-m:1`) when locks or output races occur. Elapsed time alone is not a reason to terminate a build.

## Diagnose and recover

1. Inspect the build output and process activity. Lock errors commonly name a DLL, APK, compiler, or lock-holder PID.
2. Confirm that the lock holder belongs to this task and checkout. Cancel the affected build first; stop a remaining task-owned process by its verified PID only if necessary. Avoid blanket `dotnet`, compiler, or packaging-process termination across the machine.
3. Clean the affected project if needed. Remove only verified task output directories when ordinary clean fails; preserve source and other sessions' outputs.
4. Retry with single-node MSBuild and monitor progress. For shared ImageEditor/plugin output races, build the affected dependency first, then retry the platform build.

For normal code/config verification, use the desktop solution build from `AGENTS.md`. For packaging, also inspect the expected platform artifacts.

Platform workflows:
- [Android build and deployment](../build-android/SKILL.md)
- [Windows packaging](../build-windows-exe/SKILL.md)
- [Linux packaging](../build-linux-binary/SKILL.md)
