# Derive /goal from session and repo prompt (reusable)

Use this when an assistant session already contains the discussion, constraints, and intent for a XerahS task, and you want Codex to turn that context into a self-contained `/goal` prompt. Repository policy and workflow still apply; see root `AGENTS.md` and `developers/guidelines/AGENT_WORKFLOW.md`.

---

## Copy-paste prompt

```text
Use the session's stated intent and relevant repository evidence to write a /goal prompt for the most useful next goal.

Inspect history and documentation only where needed to resolve a material uncertainty.

If you are not sure about certain parts, or want to ask me a few questions to clarify certain goals further, don't hesitate.

Output requirements:
- Return only the final prompt text unless clarification is needed first.
- Start the final prompt with `/goal`.
- Make the prompt self-contained enough that Codex can continue in this session and repo nonstop until completion.
- Include concrete goals, constraints, relevant history/docs to inspect, implementation expectations, verification expectations, and completion criteria.
- Preserve XerahS repository rules: stay on the current branch, follow root `AGENTS.md` and its Git wrappers, do not create branches or GitHub issues unless asked, and include verification appropriate to the goal.
```

Paste this into Codex from the session you want to convert. If Codex returns a prompt that does not already start with `/goal`, change the initial part to `/goal` before running it.
