# Multi-Agent Development Coordination

## Context

This document defines the coordination rules for parallel development on ShareX.Avalonia using three AI developer agents.

---

## Agent Roles

| Agent | Platform | Role | Primary Scope |
|-------|----------|------|---------------|
| **Antigravity** | Windows (Claude) | Lead Developer | Architecture, platform abstraction, integration, merge decisions |
| **Codex** | macOS (VS Code) | Backend Developer | Core logic, Helpers, Media, History, CLI, Settings |
| **Copilot** | Surface Laptop 7 (VS Code) | UI Developer | ViewModels, Views, Services, UI wiring |

---

## Antigravity Responsibilities

- Own main branch direction and architecture
- Decide task boundaries and assign work
- Review high-level changes before merge
- Prevent overlapping file modifications
- Final conflict resolution

---

## Task Distribution Rules

1. **Assign by project/folder boundaries, not individual files**
2. **Never assign two agents to the same project simultaneously**
3. **Prefer vertical slices over shared utilities**

---

## Recommended Task Split

### Codex (Backend)
- ✅ ShareX.Avalonia.Core (logic only, not Models exposed to UI)
- ✅ ShareX.Avalonia.Common (Helpers, utilities)
- ✅ ShareX.Avalonia.Media (encoding, FFmpeg)
- ✅ ShareX.Avalonia.History (persistence, managers)
- ✅ ShareX.Avalonia.CLI (when created)
- ✅ Settings import/export logic
- ❌ NO Avalonia UI projects or XAML

### Copilot (UI)
- ✅ ShareX.Avalonia.UI/ViewModels/*
- ✅ ShareX.Avalonia.UI/Views/*
- ✅ ShareX.Avalonia.UI/Services/* (UI-layer services)
- ✅ UI wiring and MVVM bindings
- ❌ NO Core logic or backend processing (unless directed)

### Antigravity (Architecture)
- ✅ ShareX.Avalonia.Platform.Abstractions
- ✅ ShareX.Avalonia.Platform.Windows/Linux/macOS
- ✅ Project structure and solution files
- ✅ AGENTS.md, NEXT_STEPS.md, documentation
- ✅ Cross-cutting interfaces and enums
- ✅ Integration and merge work

---

## Git Workflow

### Branch Naming
```
feature/cli              # Codex: CLI implementation
feature/settings-import  # Codex: Settings migration
feature/ui-history       # Copilot: History UI
feature/platform-linux   # Antigravity: Linux platform
```

### Commit Rules
- **Small, focused commits**
- **Message format**:
  ```
  [Project] Brief description

  - Change 1
  - Change 2
  
  Touched: folder/file1.cs, folder/file2.cs
  ```

### Push Frequency
- Push frequently (at least after each logical change)
- Never rebase shared branches
- Pull before starting work

---

## Conflict Avoidance

### Protected Resources (Single Agent Only)
| Resource | Owner |
|----------|-------|
| `AGENTS.md` | Antigravity |
| `NEXT_STEPS.md` | Antigravity |
| `*.sln` files | Antigravity |
| `*.csproj` files | Antigravity (or assigned agent for that project) |
| Shared interfaces | Antigravity approval required |
| Shared enums | Antigravity approval required |

### Escalation Rules
If conflict is likely:
1. Pause one agent
2. Redirect to different task
3. Antigravity resolves ownership

---

## Communication Protocol

### Agent Reports Must Include
1. Files modified (list)
2. New types added (class/interface names)
3. Assumptions made
4. Dependencies introduced

### Report Format
```markdown
## Work Report: [Agent Name]

### Files Modified
- `src/Project/Folder/File.cs`

### New Types
- `ClassName` - purpose

### Assumptions
- Assumed X because Y

### Dependencies
- Added reference to ProjectZ
```

### Antigravity Response
- ✅ Approve and continue
- 🔄 Redirect to different task
- ⏸️ Pause and clarify

---

## Stop Conditions

Agents MUST stop and ask Antigravity if:

1. **Unclear ownership** - Which project should this go in?
2. **Architectural ambiguity** - Is this the right pattern?
3. **Potential conflict** - Another agent might need this file
4. **New shared type needed** - Interface, enum, or model
5. **AGENTS.md violation** - Proposed change breaks rules

**No shortcuts that violate AGENTS.md rules are permitted.**

---

## Current Task Assignments

| Task | Agent | Branch | Status |
|------|-------|--------|--------|
| CLI Implementation | Codex | `feature/cli` | 📋 Planned |
| Settings Import | Codex | `feature/settings-import` | 📋 Planned |
| History UI | Copilot | `feature/ui-history` | 📋 Planned |
| Screen Capture UI | Copilot | `feature/capture-ui` | 📋 Planned |
| Platform Linux | Antigravity | `feature/platform-linux` | 📋 Planned |

---

## Outcome

This structure enables:
- ✅ Parallel development
- ✅ Minimal Git conflicts
- ✅ Centralized architectural control
- ✅ Clear ownership boundaries
- ✅ Efficient code review
