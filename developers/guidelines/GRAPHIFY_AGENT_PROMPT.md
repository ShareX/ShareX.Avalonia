# Graphify task prompt

Use for questions about src dependencies, callers, or symbol relationships.

```text
Answer the requested relationship question using current source. If the checked-in graph helps, use .ai/skills/graphify/SKILL.md and query only the relevant symbols. Confirm important findings in source; graph nodes may be stale.

Do not load the full JSON/HTML graph into context. If the graph or CLI is unavailable, continue with targeted source search. Rebuild only when the task requires updated graph artifacts.

The graph covers src/; inspect .axaml and submodule code directly when those are relevant.
```

The [graphify skill](../../.ai/skills/graphify/SKILL.md) owns CLI commands and artifact paths. Root [AGENTS.md](../../AGENTS.md) owns repository policy.
