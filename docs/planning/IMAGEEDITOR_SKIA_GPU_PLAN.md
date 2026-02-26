# Plan: SkiaSharp hardware acceleration for ImageEditor effects

**Goal:** Use SkiaSharp’s GPU backend for image effects where possible, and speed up remaining CPU paths.

**Current state:**
- **Color-matrix / filter effects** (Brightness, Contrast, Hue, Saturation, Invert, Sepia, Grayscale, etc.) use `ApplyColorFilter` → `new SKCanvas(result)` where `result` is an `SKBitmap`. That creates a **software (raster) surface**, so they run on CPU.
- **Per-pixel effects** (BlackAndWhite, ReplaceColor, SelectiveColor) use `ApplyPixelOperation` with `GetPixel`/`SetPixel` in a double loop → always CPU and slow on large images.

**References:** [Avalonia Skia GRContext](https://api-docs.avaloniaui.net/docs/M_Avalonia_Skia_ISkiaGpuWithPlatformGraphicsContext_TryGetGrContext), [SkiaSharp GRContext](https://learn.microsoft.com/en-us/dotnet/api/skiasharp.grcontext).

---

## Phase 1: Use GPU for color-matrix / color-filter effects

**Idea:** When applying effects that use `ApplyColorFilter` / `ApplyColorMatrix`, draw on a **GPU-backed** Skia surface instead of a software one. Avalonia (Skia backend) can provide a `GRContext`; use it to create an offscreen GPU surface, draw with the filter, then read back to `SKBitmap`.

### 1.1 Obtain `GRContext` from Avalonia

- In the **host** (e.g. `EditorView` or wherever effects are triggered), get the current Skia GPU context:
  - From the Avalonia renderer: `IPlatformGraphics` (e.g. via `AvaloniaLocator` or dependency) → cast to `ISkiaGpuWithPlatformGraphicsContext` → `TryGetGrContext()` (returns `IScopedResource<GRContext>?`).
  - Or use `ISkiaSharpApiLease` / `GrContext` if the host has access during rendering.
- **Constraint:** The context is valid only on the **render/UI thread** and only while the lease/scope is held. So effect application that wants GPU must run on that thread (or dispatch to it) and complete within the scope.

### 1.2 Centralized “apply color filter (with optional GPU)” helper

- **Where:** e.g. in `ShareX.ImageEditor.Core.ImageEffects.Adjustments.ImageEffect` or a small shared helper used by all adjustment effects.
- **API:** e.g. `ApplyColorFilter(source, filter, grContext?)`:
  - If `grContext != null` and we can create a GPU render target for `source.Width x source.Height`:
    - Create `SKSurface` with `SKSurface.Create(grContext, budgeted, imageInfo, sampleCount, surfaceProps)` (or the appropriate overload for an offscreen render target).
    - Draw: `canvas.Clear(transparent); canvas.DrawBitmap(source, 0, 0, paintWithFilter);`
    - Read back: `surface.Snapshot().ToRasterImage()` or equivalent to get pixels into an `SKBitmap`.
    - Return that bitmap.
  - Else: **fallback** to current software path (existing `ApplyColorFilter` implementation).
- **Call site:** Change `ApplyColorFilter(source, filter)` and `ApplyColorMatrix(source, matrix)` to call this new helper. The helper needs an optional `GRContext` (or a scoped lease); the **effect pipeline** does not create the context, it receives it from the host.

### 1.3 Threading and context lifetime

- Effects are today invoked from ViewModels / dialogs and may run on background threads (e.g. for preview or apply).
- **Option A:** When GPU is desired, **dispatch** the effect application to the UI/render thread, acquire `TryGetGrContext()` there, run the GPU path, then return the result (or post back). Ensures we use the context on the correct thread.
- **Option B:** Run effects on a background thread and use a **separate** offscreen `GRContext` (e.g. Vulkan) created once per process; no dependency on Avalonia’s context. More code (Vulkan/GL setup) but no UI-thread dependency.
- **Recommendation:** Start with **Option A** (use Avalonia’s context on UI/render thread) so we don’t introduce Vulkan/GL boilerplate. If needed later, add Option B for background effect processing.

### 1.4 Wiring the context into the effect pipeline

- **MainViewModel** / **EditorCore** (or the code that calls `effect.Apply(source)`) should not know about Skia’s GPU; keep the public API as `Func<SKBitmap, SKBitmap>`.
- Introduce an **optional** “effect context” or “render context” that the **host** can set (e.g. when the editor view is loaded), which carries an optional `GRContext` (or a delegate that returns a scoped GRContext). The **effect base** or a small “effect runner” in the same assembly can check this and pass the context into `ApplyColorFilter` when calling into the adjustment base.
- So: host obtains `GRContext` (e.g. in a render callback or when opening the editor), stores it in a thread-safe way or passes it via a closure to the effect runner; the runner calls existing `Effect.Apply(source)` but the **implementation** of `ApplyColorFilter` inside the effect library uses the injected context when available. That may require the helper to be callable with an optional context passed from a static or ambient “current context” that the host sets on the UI thread before applying effects.

**Deliverables for Phase 1:**
- Helper that applies a color filter (or matrix) using an optional `GRContext`; fallback to current software path.
- All existing `ApplyColorMatrix` / `ApplyColorFilter` call sites use this helper.
- Host (XerahS/EditorView) gets Avalonia’s `GRContext` and provides it to the effect pipeline when applying effects (e.g. on UI thread). Document that GPU is used only when context is available and effect runs on the correct thread.

---

## Phase 2: Remove or speed up per-pixel CPU effects

### 2.1 BlackAndWhite → color matrix

- **Current:** `ApplyPixelOperation` with luminance + threshold (e.g. lum &gt; 127 → white, else black).
- **Change:** Express as a **color matrix** (luminance coefficients 0.2126, 0.7152, 0.0722; then threshold in a second pass or approximate with a single matrix that clamps to 0/1). Skia’s `SKColorFilter.CreateColorMatrix` can do the luminance; for a hard threshold you may need two passes (luminance then replace by 0 or 255) or a custom `SKColorFilter` if available.
- **Simplest:** Build a 5×4 matrix that: (1) converts to luminance in alpha or a channel, (2) maps to 0 or 255. Or use two `ApplyColorMatrix` calls: first to luminance grayscale, second a matrix that thresholds (e.g. output = luminance &gt; 0.5 ? 1 : 0). Then **BlackAndWhite** no longer uses `ApplyPixelOperation` and benefits from Phase 1 GPU path.

### 2.2 ReplaceColor and SelectiveColor: keep logic, optimize CPU path

- These are not simple color matrices (color matching, HSL ranges). Keep the algorithm; **replace** the `ApplyPixelOperation` implementation with a **single-pass, unsafe/Span-based** loop over the bitmap’s raw bytes (e.g. `SKBitmap.GetPixels()`, iterate by row, use pointers or `Span<byte>`), and write the result buffer directly. Avoid `GetPixel`/`SetPixel` entirely.
- Add a shared helper, e.g. `ApplyPixelOperationFast(source, (ref byte r, ref byte g, ref byte b, ref byte a) => { ... })` or similar, used only by ReplaceColor and SelectiveColor. This gives a large speedup on large images without GPU.

**Deliverables for Phase 2:**
- BlackAndWhite implemented via color matrix (and thus GPU-accelerated when Phase 1 is active).
- ReplaceColor and SelectiveColor use a fast, single-pass pixel loop (no GetPixel/SetPixel).

---

## Phase 3 (optional): Optimize software fallback and reuse

- **Reuse buffers:** Where effects still use a software `SKCanvas(result)`, consider reusing a single `SKBitmap` and canvas for same-size previews to reduce allocations (with care for thread safety and lifecycle).
- **Read-back cost:** When using GPU, the cost of reading back from GPU to `SKBitmap` can dominate for small images. Consider heuristics (e.g. use GPU only when `width*height > threshold`) or keep software path for thumbnails.
- **Documentation:** In code and in this doc, note that “effects use Skia GPU when a GRContext is provided and the effect runs on the thread that owns the context.”

---

## Summary

| Phase | What | Outcome |
|-------|------|--------|
| **1** | Use Avalonia’s `GRContext` for `ApplyColorFilter` / `ApplyColorMatrix` | All matrix/filter effects (Brightness, Contrast, Hue, Saturation, Invert, Sepia, Grayscale, Polaroid, Alpha, etc.) run on GPU when context is available. |
| **2a** | BlackAndWhite via color matrix | No per-pixel loop; uses same GPU path as above. |
| **2b** | ReplaceColor / SelectiveColor: unsafe Span pixel loop | Same behavior, much faster on CPU; no GPU change. |
| **3** | Software path tuning, optional GPU threshold | Fewer allocations; avoid GPU read-back overhead for tiny images. |

**Dependency:** Phase 1 requires the ImageEditor host (XerahS) to run on Avalonia’s Skia rendering backend and to pass the acquired `GRContext` into the effect pipeline when applying effects on the UI/render thread. If the host uses a different backend (e.g. non-Skia), the fallback is the current software path.
