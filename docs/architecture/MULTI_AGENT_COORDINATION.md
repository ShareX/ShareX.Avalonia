# Multi-agent coordination

Use when multiple agents or sessions work on XerahS.

- One coordinator owns scope, integration order, and final verification.
- Give each worker a bounded task, explicit files or directories, expected verification, and a handoff format. Assign independent write sets; coordinate before two workers edit the same file.
- Keep solution settings, package versions, shared interfaces, and repository policy under coordinator ownership unless explicitly delegated.
- Workers return files changed, results, assumptions, and blockers. The coordinator resolves cross-boundary decisions within the authorized task.
- Escalate to the user only when a decision changes the requested outcome or needs new authority. Routine interface or package changes do not automatically require a pause.
- Preserve other sessions' changes. Use the mandatory wrappers and Git rules in [AGENTS.md](../../AGENTS.md); do not create branches or branch-backed worktrees without the user's explicit request.
- Avoid concurrent builds sharing outputs. Verify the integrated result with checks appropriate to the combined changes.

Useful task boundaries include desktop UI, core services, platform integrations, uploader plugins, native mobile heads, and documentation. Choose boundaries from the actual dependency structure, not a fixed agent-role roster.
