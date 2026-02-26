# XIP0042: ImageEditor SkiaSharp hardware acceleration for image effects

## Summary

**Goal:** Use SkiaSharp’s GPU backend for image effects where possible, and speed up remaining CPU paths.

**Scope:** ShareX.ImageEditor (ImageEditor) — adjustment/filter effects and their application pipeline. **ImageEditor is shared and used by two hosts:**
- **XerahS** — Avalonia desktop app; embeds EditorView and can obtain `GRContext` from the Avalonia Skia renderer.
- **ShareX** — WinForms app; opens the modern ImageEditor via `AvaloniaIntegration.ShowEditorDialog` (Avalonia window). Same ImageEditor codebase, different host process.

Any change to ImageEditor (effects, pipeline, or optional GPU path) **must remain compatible with both hosts**. The host provides an optional `GRContext` when applying effects; when not provided, the software path is always used (no breaking change for either host).

**Current state:**
- **Color-matrix / filter effects** (Brightness, Contrast, Hue, Saturation, Invert, Sepia, Grayscale, Polaroid, Alpha, Gamma, etc.) use `ApplyColorFilter` → `new SKCanvas(result)` where `result` is an `SKBitmap`. That creates a **software (raster) surface**, so they run on CPU.
- **Per-pixel effects** (BlackAndWhite, ReplaceColor, SelectiveColor) use `ApplyPixelOperation` with `GetPixel`/`SetPixel` in a double loop → always CPU and slow on large images.

**References:**
- [docs/planning/IMAGEEDITOR_SKIA_GPU_PLAN.md](../docs/planning/IMAGEEDITOR_SKIA_GPU_PLAN.md)
- [Avalonia Skia TryGetGrContext](https://api-docs.avaloniaui.net/docs/M_Avalonia_Skia_ISkiaGpuWithPlatformGraphicsContext_TryGetGrContext)
- [SkiaSharp GRContext](https://learn.microsoft.com/en-us/dotnet/api/skiasharp.grcontext)

---

## 0. Dual-host compatibility (XerahS + ShareX)

ImageEditor lives in a shared codebase consumed by **XerahS** and **ShareX**. Both use the same Avalonia-based EditorWindow/EditorView when the modern editor is enabled (ShareX: `UseModernImageEditor` → `AvaloniaIntegration.ShowEditorDialog`). Compatibility rules:

| Rule | Rationale |
|------|-----------|
| **No host-specific APIs in ImageEditor core/effects** | ImageEditor must not reference XerahS-only or ShareX-only assemblies. Effect pipeline and `ApplyColorFilter` helper stay in `ShareX.ImageEditor` with no dependency on host. |
| **GRContext is optional and provided by the host** | The effect library accepts an optional `GRContext` (or delegate). When `null` or not set, the existing software path runs. No requirement for either host to provide it. |
| **Host wiring is each host’s responsibility** | XerahS may obtain `GRContext` from its main Avalonia renderer and pass it when applying effects. ShareX may do the same from the Avalonia editor window’s renderer if feasible; if not wired, ShareX simply does not set a context and effects use the software path. Both get correct, identical effect results. |
| **Phase 2 (BlackAndWhite, ReplaceColor, SelectiveColor)** | Pure ImageEditor code; no host dependency. Behaves identically in both hosts. |
| **Validation** | When implementing, ensure ImageEditor builds and effect behavior is unchanged in both repo configurations (XerahS and ShareX). Optionally validate GPU path in XerahS and software fallback in both. |

---

## 1. Phase 1: Use GPU for color-matrix / color-filter effects

### 1.1 Obtain GRContext from Avalonia (host-specific)

- **XerahS:** In the host (e.g. `EditorView` or wherever effects are triggered), get the current Skia GPU context from the Avalonia renderer: `IPlatformGraphics` (e.g. via `AvaloniaLocator` or dependency) → cast to `ISkiaGpuWithPlatformGraphicsContext` → `TryGetGrContext()` (returns `IScopedResource<GRContext>?`). Or use `ISkiaSharpApiLease` / `GrContext` if available during rendering.
- **ShareX:** When the modern editor is shown via `AvaloniaIntegration.ShowEditorDialog`, the editor runs in an Avalonia window. If that window’s renderer exposes a Skia backend, the same pattern can be used to obtain `GRContext` when applying effects from within the editor. If not wired, leave context unset; effects use the software path (no functional change from today).
- **Constraint:** The context is valid only on the **render/UI thread** and only while the lease/scope is held. Effect application that uses GPU must run on that thread (or dispatch to it) and complete within the scope.

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

- **MainViewModel** / **EditorCore** (or the code that calls `effect.Apply(source)`) should not know about Skia’s GPU; keep the public API as `Func<SKBitmap, SKBitmap>`. All wiring stays inside ImageEditor or is injected by the host; no host-specific code in the effect core.
- Introduce an **optional** “effect context” or “render context” that **either host** can set (e.g. when the editor view is loaded), which carries an optional `GRContext` (or a delegate that returns a scoped GRContext). The effect base or a small “effect runner” in the same assembly can check this and pass the context into `ApplyColorFilter` when calling into the adjustment base.
- Each host that wants GPU: obtain `GRContext` (e.g. in a render callback or when opening the editor), store it in a thread-safe way or pass it via a closure to the effect runner. The runner calls existing `Effect.Apply(source)`; the implementation of `ApplyColorFilter` inside the effect library uses the injected context when available. If no context is provided (e.g. ShareX not yet wired), the software path runs—identical behavior for both hosts.

**Deliverables (Phase 1):**
- [ ] Helper that applies a color filter (or matrix) using an optional `GRContext`; fallback to current software path.
- [ ] All existing `ApplyColorMatrix` / `ApplyColorFilter` call sites use this helper.
- [ ] XerahS: obtain Avalonia’s `GRContext` and provide it to the effect pipeline when applying effects (e.g. on UI thread). ShareX: optionally wire `GRContext` from the editor window’s Avalonia renderer when feasible; otherwise leave unset (software path).
- [ ] Document that GPU is used only when context is available and effect runs on the correct thread.
- [ ] Ensure ImageEditor builds and effect behavior is unchanged in both XerahS and ShareX (validate in both repo configurations).

---

## 2. Phase 2: Remove or speed up per-pixel CPU effects

### 2.1 BlackAndWhite → color matrix

- **Current:** `ApplyPixelOperation` with luminance + threshold (e.g. lum > 127 → white, else black).
- **Change:** Express as a **color matrix** (luminance coefficients 0.2126, 0.7152, 0.0722; then threshold in a second pass or approximate with a single matrix that clamps to 0/1). Skia’s `SKColorFilter.CreateColorMatrix` can do the luminance; for a hard threshold use two passes (luminance then replace by 0 or 255) or a custom `SKColorFilter` if available.
- **Simplest:** Build a 5×4 matrix that: (1) converts to luminance in alpha or a channel, (2) maps to 0 or 255. Or use two `ApplyColorMatrix` calls: first to luminance grayscale, second a matrix that thresholds (e.g. output = luminance > 0.5 ? 1 : 0). Then **BlackAndWhite** no longer uses `ApplyPixelOperation` and benefits from Phase 1 GPU path.

### 2.2 ReplaceColor and SelectiveColor: keep logic, optimize CPU path

- These are not simple color matrices (color matching, HSL ranges). Keep the algorithm; **replace** the `ApplyPixelOperation` implementation with a **single-pass, unsafe/Span-based** loop over the bitmap’s raw bytes (e.g. `SKBitmap.GetPixels()`, iterate by row, use pointers or `Span<byte>`), and write the result buffer directly. Avoid `GetPixel`/`SetPixel` entirely.
- Add a shared helper, e.g. `ApplyPixelOperationFast(source, (ref byte r, ref byte g, ref byte b, ref byte a) => { ... })` or similar, used only by ReplaceColor and SelectiveColor. This gives a large speedup on large images without GPU.

**Deliverables (Phase 2):**
- [ ] BlackAndWhite implemented via color matrix (and thus GPU-accelerated when Phase 1 is active).
- [ ] ReplaceColor and SelectiveColor use a fast, single-pass pixel loop (no GetPixel/SetPixel).
- [ ] No host-specific code; validate effect behavior in both XerahS and ShareX.

---

## 3. Phase 3 (optional): Optimize software fallback and reuse

- **Reuse buffers:** Where effects still use a software `SKCanvas(result)`, consider reusing a single `SKBitmap` and canvas for same-size previews to reduce allocations (with care for thread safety and lifecycle).
- **Read-back cost:** When using GPU, the cost of reading back from GPU to `SKBitmap` can dominate for small images. Consider heuristics (e.g. use GPU only when `width*height > threshold`) or keep software path for thumbnails.
- **Documentation:** In code and in the plan doc, note that “effects use Skia GPU when a GRContext is provided and the effect runs on the thread that owns the context.”

---

## 4. Summary table

| Phase | What | Outcome |
|-------|------|--------|
| **1** | Use Avalonia’s `GRContext` for `ApplyColorFilter` / `ApplyColorMatrix` | All matrix/filter effects (Brightness, Contrast, Hue, Saturation, Invert, Sepia, Grayscale, Polaroid, Alpha, etc.) run on GPU when context is available. |
| **2a** | BlackAndWhite via color matrix | No per-pixel loop; uses same GPU path as above. |
| **2b** | ReplaceColor / SelectiveColor: unsafe Span pixel loop | Same behavior, much faster on CPU; no GPU change. |
| **3** | Software path tuning, optional GPU threshold | Fewer allocations; avoid GPU read-back overhead for tiny images. |

**Dependency:** Phase 1 GPU path is used only when a host runs on Avalonia’s Skia rendering backend and passes an acquired `GRContext` into the effect pipeline when applying effects on the UI/render thread. **Both XerahS and ShareX** can optionally do this (ShareX when the modern editor is shown via Avalonia). If a host does not provide a context or uses a non-Skia backend, the fallback is the current software path—no breaking change for either host.

---

## 5. References

- [docs/planning/IMAGEEDITOR_SKIA_GPU_PLAN.md](../docs/planning/IMAGEEDITOR_SKIA_GPU_PLAN.md)
- [Avalonia Skia – TryGetGrContext](https://api-docs.avaloniaui.net/docs/M_Avalonia_Skia_ISkiaGpuWithPlatformGraphicsContext_TryGetGrContext)
- [SkiaSharp GRContext](https://learn.microsoft.com/en-us/dotnet/api/skiasharp.grcontext)
- ImageEditor (shared): `ImageEditor/src/ShareX.ImageEditor/Core/ImageEffects/Adjustments/ImageEffect.cs` (ApplyColorFilter, ApplyColorMatrix, ApplyPixelOperation)
- ShareX host: `ShareX/TaskHelpers.cs` (`OpenImageEditor`, `UseModernImageEditor` → `AvaloniaIntegration.ShowEditorDialog`); `ShareX.ImageEditor/Helpers/AvaloniaIntegration.cs`
