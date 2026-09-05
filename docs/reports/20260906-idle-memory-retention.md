# Idle memory retention investigation

Investigated on Windows ARM64, 2026-09-05/06, against `develop` after `cbfd2df6`.

## Finding

XerahS already limits in-memory task history to 100 entries and paginates history thumbnails (50 items, decoded to width 180). Those controls did not prevent completed capture tasks from retaining full-resolution images.

`TaskManager` retains `WorkerTask` objects for history. Before this fix, completing a task left `Info.Metadata.Image` pointing to its `SKBitmap`, and `WorkerTask.Dispose` only disposed the cancellation source. Skia pixels are native allocations; they are not the managed large object heap (LOH). One 3840x2160 32-bit image occupies 33,177,600 bytes (31.64 MiB). Ten retained captures account for approximately 316 MiB of native pixel storage, in addition to the editor's current image and other process memory.

Other confirmed ownership problems:

- `AvaloniaClipboardService.ContainsImage` decoded an entire image just to answer a Boolean and abandoned the returned `SKBitmap`.
- Synchronous and asynchronous clipboard reads did not dispose the intermediate Avalonia bitmap after converting it to an independent Skia image.
- The editor copy action created an extra Skia copy for the non-owning clipboard setter and never disposed that copy.
- Upload progress handlers attached to historical tasks retained upload queue items. The upload queue also passed its preview/retry bitmap directly into a worker that should own its input.

## Changes

Completed tasks now release their native image after completion/status callbacks return. Callbacks can still copy pixels for a preview. Lightweight success information survives image disposal. Queued or active tasks are not disposed by history pruning, and disposal requested during processing is deferred until borrowed pixels are no longer in use.

Clipboard availability checks inspect formats. Clipboard conversions and copy snapshots have explicit disposal scopes. Upload tasks receive a separate bitmap, and their queue progress handler is detached on both success and failure.

No forced garbage collection, LOH compaction, or working-set trimming was added. Such changes would not release native images that are still intentionally reachable through task history.

## Measurements and limits

A 12-second read-only `System.Runtime` counter sample was collected from the already-running installed Windows app (version 0.29.1, commit `96e7c9b7`), not a controlled before/after instance of this checkout:

| Metric | Observed value |
| --- | ---: |
| Process private bytes, before counter attachment | 198.9 MiB |
| Process working set, before counter attachment | 31.2 MiB |
| Managed GC committed bytes, last collection | 75.6 MiB |
| LOH size, last collection | 28.6 MiB |
| LOH fragmentation, last collection | 0.23 MiB |

These metrics are not interchangeable. Process private memory includes native allocations and runtime overhead. Working set depends on residency. GC metrics describe the last collection and do not establish all currently live allocations. This sample does not reproduce the reported 400 MB or prove that LOH fragmentation caused it.

The bundled startup sample is 920x430 (about 1.51 MiB per uncompressed copy); it cannot alone explain 400 MB. The current editor image, undo state, graphics resources, loaded assemblies, and clipboard-owned outbound data can legitimately remain while idle. This change removes the confirmed abandoned/retained image paths; it does not promise a universal idle RAM target.

## Verification

Regression coverage exercises full `WorkerTask.StartAsync` completion with four retained 4K tasks (132,710,400 bytes of input pixels), valid preview copies in completion callbacks, and zero remaining native image handles after completion. It also covers throwing callbacks, queued/in-flight disposal, history pruning while other tasks run, upload queue image ownership and event detachment, and clipboard format-only checks and bitmap disposal.

For a user-specific follow-up, record OS, app version, Release/Debug build, the exact memory metric, and whether idle follows launch, capture, editing, or clipboard operations. Compare a fresh process with repeated same-size captures using the same workflow. Collect `System.Runtime` counters alongside private bytes and working set; use a managed heap analysis only if the counters point to live managed/LOH growth, and investigate native image owners when private memory grows without matching managed growth.
