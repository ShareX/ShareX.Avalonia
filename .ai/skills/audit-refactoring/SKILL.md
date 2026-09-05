---
name: audit-refactoring
description: Audit XerahS refactoring candidates when explicitly requested. Do not trigger on ordinary bug fixes.
---

# Refactoring audit

Inspect the requested area for concrete maintenance costs: mixed UI/startup responsibilities, platform leakage, duplicated workflows, or central uploader switches. File size and class names are clues, not findings.

For each candidate, verify callers, existing abstractions, and platform constraints. Report the source location, observed cost, proposed change, risks, and smallest useful verification. Prioritize evidence-backed improvements; do not manufacture a quota of findings.

An audit produces findings. Implement changes or create GitHub issues only when the user's request includes those actions. For requested issues, submit the verified findings with a title and body file through the configured GitHub tooling.
