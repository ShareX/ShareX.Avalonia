# XerahS Watch-Folder Daemon — Refactor Plan
**Branch:** `refactor/watch-folder-daemon`
**Date:** 2026-05-26
**Status:** REVIEW DRAFT — do not implement until approved

---

## 1. Current Architecture Overview

### Source locations

| Component | Path |
|---|---|
| Daemon entry point | `src/desktop/tools/XerahS.WatchFolder.Daemon/Program.cs` |
| CLI argument parsing | `src/desktop/tools/XerahS.WatchFolder.Daemon/DaemonOptions.cs` |
| Headless UI/toast stubs | `src/desktop/tools/XerahS.WatchFolder.Daemon/Services/` |
| Core watch logic | `src/desktop/core/XerahS.Core/Managers/WatchFolderManager.cs` |
| Bootstrap adapter | `src/desktop/app/XerahS.Bootstrap/WatchFolderDaemonControllerAdapter.cs` |
| Platform abstraction | `src/platform/XerahS.Platform.Abstractions/Services/WatchFolderDaemonServiceBase.cs` |
| macOS implementation | `src/platform/XerahS.Platform.MacOS/Services/MacOSWatchFolderDaemonService.cs` |
| Windows implementation | `src/platform/XerahS.Platform.Windows/Services/WindowsWatchFolderDaemonService.cs` |
| Linux implementation | `src/platform/XerahS.Platform.Linux/Services/LinuxWatchFolderDaemonService.cs` |
| CLI command | `src/desktop/cli/XerahS.CLI/Commands/WatchFolderDaemonCommand.cs` |
| Existing upload queue | `src/desktop/core/XerahS.Core/Services/UploadQueueService.cs` |

### File count under refactor
~14 source files, ~2,400 lines of C#. Not a large codebase — the problem is density, not volume.

### What WatchFolderManager.cs does today (one 500-line class)

```
WatchFolderManager
├── StartOrReloadFromCurrentSettings()  ← hot path on daemon start
├── UpdateWatchers()                     ← creates FileSystemWatchers
│   ├── AddWatchers()                    ← one watcher per filter glob
│   ├── ParseFilters()                   ← splits "*.png;*.jpg" into per-filter watchers
│   └── OnFileDetected()                 ← event handler: enqueues processing
├── ProcessFileAsync()                   ← the big one
│   ├── WaitForFileReadyAsync()          ← polling loop waiting for stable size
│   ├── IsFileLocked()                   ← FileStream open attempt
│   ├── ConvertMovToMp4Async()           ← FFmpeg subprocess call
│   ├── DeleteFileWithRetryAsync()       ← retry loop with 5× delay
│   ├── MoveToScreenshotsFolder()        ← File.Move + exists-handling
│   └── TaskManager.StartFileTask()       ← actual upload/workflow dispatch
├── StopWatchers()                       ← dispose all watchers
├── StopAsync() + WaitForInFlightTasksAsync()
└── IDisposable.Dispose()
```

Additionally: `_inFlight` ConcurrentDictionary (debounce/dedup), `_activeProcessingCount` (graceful shutdown coordination), singleton via `Lazy<>`.

### Problems identified

1. **Single monolithic class** — 500 lines doing 6 distinct jobs: watcher lifecycle, file readiness gate, format conversion, screenshots folder move, workflow dispatch, shutdown coordination.
2. **No interface** — `WatchFolderManager` is concrete; cannot mock for testing.
3. **No config watching** — `UpdateWatchers()` called once at startup. If user edits a watch folder config while daemon is running, daemon does not reload.
4. **`_acceptNewFiles` as synchronization** — `volatile bool` + lock is a smell; doesn't handle concurrent reload scenarios cleanly.
5. **`_inFlight` dedup is path-based only** — if the same file appears via different paths (symlink, short-name expansion), deduplication fails.
6. **No cancellation propagation** — `ProcessFileAsync` runs as fire-and-forget `Task.Run`; cancellation token from `StopAsync` is not passed to file processing.
7. **No retry/dispatch abstraction** — `TaskManager.StartFileTask()` is called directly; no upload queue integration for backpressure or retry.
8. **`WatchFolderManager` is a singleton** — hard to test, can't inject per-folder overrides.

---

## 2. Proposed Modular Architecture

```
XerahS.WatchFolder.Daemon
│
├── Program.cs                     ← Bootstrap + infinite wait (unchanged)
├── DaemonOptions.cs               ← CLI parsing (unchanged)
├── Services/
│   ├── IFileWatchService          ← Interface
│   ├── FileWatchService           ← FileSystemWatcher lifecycle + filter routing
│   ├── IFileReadyGate             ← Interface
│   ├── FileReadyGate              ← Poll-until-stable size + not-locked
│   ├── IMovConverter              ← Interface
│   ├── MovConverter               ← FFmpeg MOV→MP4, delete original on success
│   ├── IWorkflowExecutor          ← Interface
│   ├── WorkflowExecutor           ← Clone TaskSettings, set Job=FileUpload, dispatch
│   ├── IDaemonLifecycleController ← Interface
│   └── DaemonLifecycleController  ← Coordinates: watches + queue + graceful shutdown
│
XerahS.Core
├── Managers/
│   ├── IWatchFolderManager        ← Extracted interface (for DI/testing)
│   └── WatchFolderManager         ← Thin adapter: delegates to FileWatchService + WorkflowExecutor
├── Services/
│   └── UploadQueueService         ← Existing; used by WorkflowExecutor for backpressure
│
XerahS.Bootstrap
├── IWatchFolderDaemonController  ← Unchanged
└── WatchFolderDaemonControllerAdapter ← Unchanged (adapts IWatchFolderManager)
```

### Component responsibilities

**FileWatchService**
- Owns `List<FileSystemWatcher>` lifecycle
- Fires `FileDetected` event (not directly onto watchers) with debounce
- Filters: parses `WatchFolderSettings.Filter` → one watcher per glob
- Does NOT process files — only emits `FilePath` events

**FileReadyGate**
- Input: `string fullPath`, `int timeoutMs = 15000`, `int pollMs = 300`
- Output: `Task<bool>` — true if file is stable (same size × 2 polls) and not locked
- Pure: no I/O except the file it is checking

**MovConverter**
- Input: `string sourcePath`, `bool enabled`
- Output: `Task<string?>` — converted path or null
- Deletes source after successful conversion
- FFmpeg path resolved via `PathsManager.GetFFmpegPath()`

**WorkflowExecutor**
- Input: `WatchFolderSettings`, `string filePath`
- Resolves workflow via `SettingsManager.GetWorkflowById()`
- Clones `TaskSettings`, sets `Job = WorkflowType.FileUpload`
- Applies optional screenshots folder move
- Delegates to `TaskManagerService` or `UploadQueueService`

**DaemonLifecycleController**
- Owns the graceful shutdown sequence:
  1. Stop accepting new files (`_acceptNewFiles = false`)
  2. Stop watchers
  3. Wait for in-flight to drain (with timeout)
- Coordinates all other services
- Exposes `StartAsync()`, `StopAsync(TimeSpan)`, `ReloadAsync()`

### Interfaces (key method signatures)

```csharp
public interface IFileWatchService : IDisposable
{
    void Start(IEnumerable<WatchFolderSettings> folders);
    void Stop();
    event EventHandler<FileWatchEventArgs>? FileDetected;
}

public record FileWatchEventArgs(string FullPath, WatchFolderSettings Settings);

public interface IFileReadyGate
{
    Task<bool> WaitAsync(string fullPath, CancellationToken ct = default);
}

public interface IMovConverter
{
    Task<string?> ConvertAsync(string fullPath, CancellationToken ct = default);
}

public interface IWorkflowExecutor
{
    Task ExecuteAsync(WatchFolderSettings settings, string filePath, CancellationToken ct = default);
}

public interface IDaemonLifecycleController
{
    Task StartAsync(CancellationToken ct = default);
    Task<bool> StopAsync(TimeSpan timeout, CancellationToken ct = default);
}
```

---

## 3. Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                     WatchFolderDaemon Process                       │
│                                                                      │
│  ┌──────────────────┐    ┌─────────────────────┐                    │
│  │  Bootstrap       │───▶│  IWatchFolderDaemon  │                    │
│  │  (Program.cs)    │    │  Controller         │                    │
│  └──────────────────┘    └──────────┬──────────┘                    │
│                                     │                               │
│                         ┌───────────▼──────────────┐                │
│                         │  DaemonLifecycleController│               │
│                         └───────────┬──────────────┘                │
│                    ┌───────────────┼───────────────────────┐        │
│                    │               │                       │        │
│         ┌─────────▼─────┐  ┌───────▼───────┐  ┌──────────▼──────┐  │
│         │ FileWatchService│ │WorkflowExec  │  │ ConfigWatcher   │  │
│         │ (FSW lifecycle) │ │              │  │ (future)        │  │
│         └───────┬────────┘ └───────┬───────┘  └────────────────┘  │
│                 │ FileDetected     │ ExecuteAsync                    │
│                 ▼                  ▼                                 │
│  ┌──────────────────┐   ┌────────────────────┐                     │
│  │  FileReadyGate   │   │  UploadQueueService │ (existing)         │
│  │  (poll, stable)  │   │  (backpressure)     │                     │
│  └────┬────────────┘   └──────────┬───────────┘                     │
│       │ ready                    │ enqueue                          │
│       ▼                           ▼                                 │
│  ┌──────────────────┐   ┌────────────────────┐                     │
│  │  MovConverter    │   │  TaskManager       │ (existing)         │
│  │  (FFmpeg)       │   │  (upload pipeline) │                     │
│  └────┬────────────┘   └────────────────────┘                     │
│       │ converted                                                        │
│       └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

**Path:** `FileDetected` → `FileReadyGate.WaitAsync()` → `MovConverter.ConvertAsync()` → `WorkflowExecutor.ExecuteAsync()` → `UploadQueueService.Enqueue()` / `TaskManager.StartFileTask()`

---

## 4. State Machine for the Daemon

```
                    ┌──────────────────────────────────────┐
                    │            UNINITIALIZED              │
                    │  (on bootstrap complete)             │
                    └──────────────┬───────────────────────┘
                                   │ StartAsync()
                                   ▼
                    ┌──────────────────────────────────────┐
         ┌─────────│           INITIALIZING                │
         │         │  • Load settings                     │
         │         │  • Start FileWatchService            │
         │         │  • Subscribe events                  │
         │         └──────────────┬───────────────────────┘
         │                        │ watchers ready
         ▼                        ▼
┌─────────────────────┐   ┌──────────────────────────────┐
│      STOPPING        │   │         RUNNING              │
│ (transitional)      │   │  • Accepts new files         │
│ • Stop watchers     │   │  • Processes file events     │
│ • Wait in-flight    │   │  • Tracks active count       │
└─────────────────────┘   └──────────────┬───────────────┘
         ▲                        │ ReloadAsync()
         │                        │ StopAsync()
         │                        ▼
         │               ┌──────────────────────────────┐
         └───────────────│         STOPPING              │
                         │  • _acceptNewFiles = false   │
                         │  • Stop watchers             │
                         │  • Wait for active→0        │
                         │  • Exit                      │
                         └──────────────────────────────┘
```

**States:**
- `UNINITIALIZED` — initial state before `StartAsync()`
- `INITIALIZING` — loading settings, starting watchers
- `RUNNING` — normal processing state
- `STOPPING` — graceful shutdown; no new files accepted, in-flight completing

**Transitions:**
- `UNINITIALIZED → INITIALIZING`: `StartAsync()` called
- `INITIALIZING → RUNNING`: all watchers started
- `RUNNING → STOPPING`: `StopAsync()` called
- `RUNNING → INITIALIZING`: `ReloadAsync()` (stop then restart)
- `STOPPING → RUNNING`: only via `ReloadAsync()` from a reloading path; otherwise daemon exits

---

## 5. Test Matrix

| Component | What to test | Approach |
|---|---|---|
| `FileWatchService` | Multiple filter globs spawn correct number of watchers | Unit |
| `FileWatchService` | Duplicate path within debounce window is suppressed | Unit |
| `FileWatchService` | Stop() disposes all watchers | Unit |
| `FileWatchService` | Non-existent folder at startup is skipped with log | Unit |
| `FileReadyGate` | Returns false if file deleted during poll | Unit |
| `FileReadyGate` | Returns true when file stable for 2 consecutive polls | Unit |
| `FileReadyGate` | Respects cancellation token mid-poll | Unit |
| `FileReadyGate` | File locked by another process → waits + returns false | Unit |
| `MovConverter` | FFmpeg missing → returns null, no exception | Unit |
| `MovConverter` | Conversion success → source deleted, target returned | Unit |
| `MovConverter` | Conversion failure → neither file exists | Unit |
| `WorkflowExecutor` | Missing workflow → throws ArgumentException | Unit |
| `WorkflowExecutor` | MOV file with convert flag → calls MovConverter | Unit |
| `WorkflowExecutor` | MoveToScreenshots → File.Move called | Unit |
| `WorkflowExecutor` | Job is set to FileUpload on cloned settings | Unit |
| `DaemonLifecycleController` | StartAsync → RUNNING state | Unit |
| `DaemonLifecycleController` | StopAsync with in-flight → waits for drain | Unit |
| `DaemonLifecycleController` | StopAsync timeout expires → returns false | Unit |
| `WatchFolderManager` (adapter) | StartOrReload delegates to FileWatchService | Unit |
| `WatchFolderManager` (adapter) | Stop delegates to lifecycle controller | Unit |

**Coverage target:** All new interfaces + existing `WatchFolderManager` adapter path. Platform service implementations (launchd, systemd, ServiceBase) are tested via integration tests only — mocking the OS-level primitives is not worth the complexity.

---

## 6. Failure Modes and Mitigations

| Failure | Mitigation |
|---|---|
| FileSystemWatcher buffer overflow (many rapid events) | FileWatchService debounces within a 100ms window per path |
| File deleted between Created event and processing | `FileReadyGate` re-checks existence on each poll; returns false → drop |
| FFmpeg not installed | `MovConverter` returns null; logs warning; original file untouched |
| FFmpeg conversion corrupts output | Validate output exists + size > 0; delete both on failure |
| Original MOV delete fails after conversion | Log error, keep both files; do not treat as critical failure |
| Watched folder deleted at runtime | FileSystemWatcher raises Error event → log + remove from active set |
| Settings file corrupted on disk | `ShareXBootstrap` catches; daemon logs error and continues with last valid config |
| In-flight tasks never drain (bug) | `StopAsync` has configurable timeout (default 30s); returns false → process exit |
| Very large file (>4GB) | `FileReadyGate` stable check doesn't care about size; handles correctly |
| Network path goes away | `FileSystemWatcher` errors; removed from active set; log warning |
| Duplicate processing (watcher re-fires after move) | `_inFlight` dedup + `ConvertedPath` tracking from `WatchFolderManager` logic is preserved in `WorkflowExecutor` |
| Watch folder removed while daemon running | `UpdateWatchers()` re-reads on `ReloadAsync()`; removed folder's watchers are disposed |

---

## 7. Implementation Order

### Phase 1: Interface extraction (no behavior change)
1. Extract `IWatchFolderManager` interface from `WatchFolderManager`
2. Register `IWatchFolderManager` in DI container
3. `WatchFolderDaemonControllerAdapter` uses `IWatchFolderManager` (no functional change)
4. Add unit tests for existing `WatchFolderManager` behavior through the interface

### Phase 2: FileReadyGate extraction
1. Create `IFileReadyGate` + `FileReadyGate` from `WaitForFileReadyAsync` logic
2. Create `IMovConverter` + `MovConverter` from `ConvertMovToMp4Async` logic
3. Update `WatchFolderManager` to use both (still same class, injected dependencies)
4. Add unit tests for both new classes

### Phase 3: FileWatchService extraction
1. Create `IFileWatchService` + `FileWatchService` from watcher lifecycle in `WatchFolderManager`
2. Move filter parsing (`ParseFilters`) into `FileWatchService`
3. `FileWatchService` raises `FileDetected` events
4. `WatchFolderManager` subscribes and routes to `ProcessFileAsync`
5. `_inFlight` dedup logic stays in `WatchFolderManager` (orchestrator)

### Phase 4: WorkflowExecutor extraction
1. Create `IWorkflowExecutor` + `WorkflowExecutor`
2. Move `MoveToScreenshotsFolder` and `CloneTaskSettings` logic here
3. `WatchFolderManager` becomes the orchestrator: subscribe to `FileWatchService`, call `FileReadyGate`, then `WorkflowExecutor`

### Phase 5: DaemonLifecycleController
1. Create `IDaemonLifecycleController` + `DaemonLifecycleController`
2. Move `_acceptNewFiles`, `_activeProcessingCount`, `StopAsync`, `WaitForInFlightTasksAsync` here
3. `WatchFolderManager` delegates shutdown coordination to it
4. `IWatchFolderDaemonController` backed by `IDaemonLifecycleController`

### Phase 6: Config watcher (optional/future)
- Detect settings file changes via `FileSystemWatcher` on the config path
- Auto-call `ReloadAsync()` when detected
- **Out of scope for this refactor** — flag as a future enhancement

### Phase 7: Simplify WatchFolderManager
- After Phase 1-5, `WatchFolderManager` is a thin orchestrator ~100 lines
- Rename to `WatchFolderOrchestrator` or keep as thin alias

---

## 8. Scope Check

### In scope ✅
- Extracting `IFileWatchService`, `IFileReadyGate`, `IMovConverter`, `IWorkflowExecutor`, `IDaemonLifecycleController`
- Extracting `IWatchFolderManager` interface
- Refactoring `WatchFolderManager` into coordinated components
- All unit tests for new components
- Preserving existing behavior (no functional change to the watch/process pipeline)
- Platform service layer (launchd/systemd/ServiceBase) — no structural change
- `UploadQueueService` integration — already exists, just wired properly

### Out of scope 🚫
- Config hot-reload (Phase 6 is future enhancement)
- Multi-folder prioritization or ordering
- Rate limiting / backpressure configuration changes
- Plugin system redesign — `XerahS.UploaderPluginSdk` unchanged
- Changes to `ApplicationConfig` schema
- `TaskManager` / capture pipeline changes
- Any UI changes

---

## 9. Open Questions for Review

1. **Should `WatchFolderManager` be renamed?** After refactor it becomes a thin orchestrator. Keep name or rename to `WatchFolderOrchestrator`?
2. **Config watcher** — include as Phase 6 or defer to a separate issue? (Recommend defer — adds non-trivial complexity around "which file to watch" and debounce.)
3. **Should `UploadQueueService` replace `TaskManager.StartFileTask()` direct calls?** `UploadQueueService` handles backpressure; `TaskManager` is fire-and-forget. WorkflowExecutor could enqueue instead of dispatch directly. Tradeoff: introduces async queue semantics vs current synchronous-ish flow.
4. **`_inFlight` dedup scope** — per-process only? Currently yes. If the daemon is ever multi-instance (one per watch folder), dedup needs shared state. Current design assumes single-instance. Flag as assumption.

---

*Plan produced by /autoplan. Ready for CEO review before implementation begins.*