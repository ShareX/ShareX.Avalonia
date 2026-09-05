---
name: graphify
description: Query src symbol relationships or cross-project dependency paths when graph navigation helps.
---

# graphify (XerahS)

Canonical graph lives at:

`docs/architecture/graphify-out/`

See [README](../../../docs/architecture/graphify-out/README.md).

Full cross-agent kickoff prompt (copy-paste + skill/tool path index):

`developers/guidelines/GRAPHIFY_AGENT_PROMPT.md`

## When to use

Use graphify when relationship queries help answer:

- Architecture / "how does X connect to Y" questions
- Finding callers, dependents, or cross-project paths
- Orienting in an unfamiliar area of `src/`

Use source inspection when the graph or tool is unavailable or stale. A graph pass is optional; check important graph findings against current source.

## CLI

Repo-local binary (preferred):

```bash
G=.tools/graphify-venv/bin/graphify
GRAPH=docs/architecture/graphify-out/graph.json
```

If the venv is missing:

```bash
python3 -m venv .tools/graphify-venv
.tools/graphify-venv/bin/pip install -U pip graphifyy
```

Commands:

```bash
$G query "<question>" --graph "$GRAPH"
$G path "<A>" "<B>" --graph "$GRAPH"
$G explain "<concept>" --graph "$GRAPH"
$G affected "<symbol>" --graph "$GRAPH"
```

A root symlink `graphify-out` → `docs/architecture/graphify-out` may exist so default CLI paths work.

## Human-readable entry points

1. `docs/architecture/graphify-out/GRAPH_REPORT.md` — hubs / god nodes / suggested questions
2. `docs/architecture/graphify-out/GRAPH_TREE.html` — lighter browser view
3. `docs/architecture/graphify-out/graph.html` — full force graph (large)

## Rebuild

Full rebuild (preferred — scopes to src/, refreshes graph + tree + symlink):

```bash
scripts/update-graphify.sh
```

Incremental update (no `--graph` flag — path comes from the repo-root
`graphify-out/` symlink. NEVER pass `--graph` to `update`; NEVER pass
`.` (whole-repo root) when you only want src/):

```bash
$G update src
```

After very large refactors, prefer the full `scripts/update-graphify.sh`
over an incremental update — the graph's clustering benefits from a clean
rebuild after structural changes.

Help caveat: `graphify <subcommand> --help` is unreliable; use
top-level `graphify --help` only.
