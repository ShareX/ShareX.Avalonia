---
name: draft-blog-post
description: Create or consolidate a requested XerahS daily development blog draft in docs/blog.
---

## Scope

Treat the daily blog as a single consolidated record across these three repositories:

- `https://github.com/ShareX/XerahS.git`
- `https://github.com/ShareX/ShareX.ImageEditor.git`
- `https://github.com/McoreD/XerahS.Editor.git`

The XerahS repo is the **publishing destination** for all blog files and docs commits.

## Source of truth

You must base every blog entry only on real git history. Do not guess, infer unsupported changes, or fabricate technical details.

- Use commit history from **all three** repositories as the only source of truth for each day.
- Use `git log --date=iso-strict` or pre-dumped TSV logs when available.
- For every UTC+8 day with activity in any of the three repositories, collect all commits whose **author date** falls on that UTC+8 day.
- You must go all the way back to the earliest commit in each repository and cover every active UTC+8 day from those first commits up to today.

## File creation rules

Create blog posts in the XerahS repo under:

`docs/blog/YYYY/YYYY-MM/blog-YYYYMMDD.md`

Rules:

- Use **exactly one** markdown file per UTC+8 day.
- Treat each file as a **complete daily dev blog post**, not a minimal changelog.
- If multiple repositories have activity on the same day, merge them into **one** single daily post.
- If at least one of the three repositories has activity on a day, that day **must** have a fully populated blog file.

## Workflow

1. Resolve the target UTC+8 date.
   - Default to today unless the user supplies a specific date.
   - Always write to `docs/blog/YYYY/YYYY-MM/blog-YYYYMMDD.md`.

2. Ensure the daily draft exists with the helper script:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .ai/skills/draft-blog-post/scripts/upsert-blog-draft.ps1
```

Use an explicit date when needed:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .ai/skills/draft-blog-post/scripts/upsert-blog-draft.ps1 -Date 2026-03-13
```

Append a verified bullet to an existing section without creating a second post for the same day:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .ai/skills/draft-blog-post/scripts/upsert-blog-draft.ps1 -Date 2026-03-13 -Section Fixes -Bullet "Refreshed ShareX.VideoEditor npm bootstrap after the latest submodule update."
```

3. Gather context before drafting.
   - For the target UTC+8 day, check **all three** repositories for commits whose author date falls on that day (e.g. `git log --date=iso-strict --since=<day-start-UTC> --until=<day-end-UTC>`).
   - Inspect repo or submodule history with `git log --oneline -n 20`; open specific commits with `git show --stat <commit>`.
   - If the day's work is still in progress, inspect the current diff with `git diff -- <paths>` or `git -C <submodule> diff -- <paths>`.
   - Use only verified details from the repository state. Do not invent shipped behavior.

4. Consolidate the day into one markdown post.
   - Keep one file per UTC+8 day.
   - Update the existing file instead of creating multiple same-day posts.
   - Replace placeholder bullets when you have verified content.
   - Merge related fixes or build notes into concise bullets instead of repeating commit titles verbatim.

5. Populate all required sections (see **Content requirements** below). Keep the post in draft form unless the user asks for a polished publication pass.

6. Before finishing, remove untouched placeholders if the draft is meant to be reviewed by humans.

## Content requirements for each active day

Every processed day must include all of these sections:

- `## Summary`
- `## Features`
- `## Fixes`
- `## Build and Tooling`
- `## Commits Reviewed`
- `## Notes`

### Section expectations

- **Summary**: Write 2 to 3 sentences explaining what happened that day across the relevant repositories.
- **Features**: Bullets summarising new user-visible or contributor-visible capabilities.
- **Fixes**: Bullets covering bug fixes, stability work, correctness improvements, and regressions addressed.
- **Build and Tooling**: Bullets for CI, packaging, build changes, refactors, developer tooling, and documentation infrastructure.
- **Commits Reviewed**: List **real** short hashes and **real** commit subjects from the commits used for that day.
- **Notes**: Bullets for risks, technical debt, follow-ups, incomplete work, or partially shipped changes.

## Writing rules

- Write in **blog-style prose** using full sentences and a clear narrative. Prioritise clarity and completeness over extreme brevity. File size is not a concern.
- Do **not** mirror commits one-to-one into bullets. Group related commits into clear thematic summaries (e.g. "Linux portal fixes", "ImageEditor GPU refactor", "Task Settings UX redesign").
- Maintain traceability by listing the real commits in `## Commits Reviewed`.
- When useful, explicitly attribute work by repository inside bullets or prose, for example: `XerahS: ...`, `ShareX.ImageEditor: ...`, `XerahS.Editor: ...`.

## Quality rules

- Replace **all** `TBD` placeholders with real content once a day is processed.
- No processed day may remain a skeleton.
- Do not leave any targeted month (e.g. February 2026) with placeholder-only files.
- Do **not** invent commits, features, fixes, or technical changes.
- If a day has no commits across any of the three repositories, state "No development activity today." or "Took a break today." in the Summary and mark other sections as "None" or "No changes."

## Commit rules

For each day with blog content, create **exactly one** git commit in the XerahS repo that stages **only** that day's `blog-YYYYMMDD.md` file.

Commit message format:

`[vX.Y.Z] [Docs] Add YYYY-MM-DD <short description> blog draft.`

Rules:

- One docs commit per blog day.
- Stage only that single day's markdown file in that commit.
- Order commits chronologically from the earliest day to the latest day.
- Do **not** push to remote unless explicitly asked.

## Execution rules

- Once the user has explicitly said to go ahead, continue without repeated re-confirmation.
- Keep going automatically through remaining days of the targeted range (e.g. remaining January 2026, all February 2026, later active months up to the present, and earlier active months back to the earliest commits).
- Continue until every UTC+8 day with activity across any of the three repositories is represented by a fully populated `blog-YYYYMMDD.md` file and exactly one corresponding docs commit in the XerahS repo.

## Parallelisation rules

When possible, use multiple subagents in parallel to speed up the work.

- Split parallel work into **disjoint** time ranges (e.g. by week or month) so there is no overlap.
- Use subagents to: analyse different time ranges; pre-compute daily summaries from git logs; prepare grounded day-by-day inputs for blog drafting.
- Then use those outputs to perform the concrete markdown edits and single-file docs commits in the XerahS repo.

## Behaviour rules

- When the user says progress is not visible, **immediately do the concrete work** required: create or update markdown files and commit them, instead of only describing intentions.
- Prefer execution over more planning once the task is already clear.

## Final standard: when a day is complete

A day is only considered complete when:

- all three repositories have been checked for that UTC+8 day;
- one combined daily blog file exists in the XerahS repo;
- all required sections are fully populated with real, commit-grounded content;
- no placeholders (TBD, placeholders from script) remain;
- the day's file has exactly one matching docs commit in chronological order.

## Scheduling (Cursor Automations, cron, or Task Scheduler)

**Cursor Automations** can run this full skill on a daily schedule (cloud agent with cron or preset). See [SCHEDULING.md](SCHEDULING.md) for setup at [cursor.com/automations](https://cursor.com/automations).

To only ensure today's draft file exists (no content or commit), use the daily wrapper:

```powershell
.ai/skills/draft-blog-post/scripts/run-daily-draft.ps1
```

Use `-IncludePreviousDay` when you want to cover both the current UTC+8 day and the previous UTC+8 day, for example in a scheduled workflow that runs at `16:00 UTC`.

That script switches to the repo root and runs the upsert for the current UTC+8 day, and optionally the previous UTC+8 day. For content population and commit-grounded blog prose, run this skill in Cursor or use Cursor Automations.

## Helper Script

Script path:

```powershell
.ai/skills/draft-blog-post/scripts/upsert-blog-draft.ps1
```

Behavior:
- Creates `docs/blog/YYYY/YYYY-MM/` if it does not exist.
- Creates `blog-YYYYMMDD.md` with the standard daily draft template if it does not exist.
- Prints the resolved file path.
- When `-Section` and `-Bullet` are provided, appends a deduplicated bullet to that section and removes the section placeholder.

Supported append sections:
- `Features`
- `Fixes`
- `Build and Tooling`
- `Commits Reviewed`
- `Notes`
