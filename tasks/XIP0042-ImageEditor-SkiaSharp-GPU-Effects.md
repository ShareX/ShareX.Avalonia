# XIP0042: ImageEditor SkiaSharp hardware acceleration for image effects

## Summary

**Goal:** Use SkiaSharp's GPU backend for image effects where possible, and speed up remaining CPU paths.

**Scope:** ShareX.ImageEditor (ImageEditor) — adjustment/filter effects and their application pipeline. **ImageEditor is shared and used by two hosts:**

- **XerahS** — Avalonia desktop app; embeds EditorView and can obtain `GRContext` from the Avalonia Skia renderer.
- **ShareX** — WinForms app; opens the modern ImageEditor via `AvaloniaIntegration.ShowEditorDialog` (Avalonia window). Same ImageEditor codebase, different host process.

Any change to ImageEditor (effects, pipeline, or optional GPU path) **must remain compatible with both hosts**. The host provides an optional `GRContext` when applying effects; when not provided, the software path is always used (no breaking change for either host).

---

## Current State (as of Feb 2026 — second audit)

> **Jaex has landed two rounds of optimisations/fixes. Read this section before implementing anything.**

### Round 1 (prior audit)
- ✅ `ApplyPixelOperation` rewritten with `unsafe` pointer arithmetic for `Bgra8888` — no `GetPixel`/`SetPixel`.

### Round 2 (this audit)
- ✅ `GlowImageEffect` — `AutoResize` param added; asymmetric canvas expansion (only expands on the side the glow/offset extends to).
- ✅ `ShadowImageEffect` — Removed `Darkness`, added `Color` property; same asymmetric expansion pattern as Glow.
- ✅ `ReflectionImageEffect` — Fixed flip matrix (proper `Translate + Scale` instead of pivot-based scale); canvas width now accounts for skew displacement.
- ✅ `OutlineImageEffect` — `OutlineOnly` mode added (DstOut erase of inner area).
- ✅ `SliceImageEffect` — Robust `minSliceHeight`/`maxSliceHeight`/`minSliceShift`/`maxSliceShift` bounds checking.

### What is still pending

| Area | Status |
|---|---|
| **Phase 1** — GRContext / GPU surface path | ❌ NOT STARTED |
| **Phase 2a** — BlackAndWhite via color matrix | ❌ NOT DONE |
| **§3.2** — `ColorizeImageEffect` refactor | ❌ NOT DONE |
| **§3.5** — `SKFilterQuality` deprecation | ❌ NOT DONE |
| **§3.6** — Redundant `Category` overrides in 7 filter effects | ❌ NOT DONE |
| **§3.8** — `new Random()` per call in Slice/TornEdge | ❌ NOT DONE |
| Phase 3.1 (GPU threshold) | ❌ Pending Phase 1 |
| §3.3 GammaImageEffect LUT caching | ⚠️ Low priority |
| §3.4 BlurImageEffect allocation reduction | ⚠️ Low priority |
| §3.7 SelectiveColor `ToHsl` short-circuit | ⚠️ Profile first |

### Effect map

| Effect | Method | GPU-ready? | Notes |
|---|---|---|---|
| Brightness | `ApplyColorMatrix` | After Phase 1 | |
| Contrast | `ApplyColorMatrix` | After Phase 1 | |
| Saturation | `ApplyColorMatrix` | After Phase 1 | |
| Hue | `ApplyColorMatrix` | After Phase 1 | |
| Invert | `ApplyColorMatrix` | After Phase 1 | |
| Grayscale | `ApplyColorMatrix` | After Phase 1 | |
| Sepia | `ApplyColorMatrix` | After Phase 1 | |
| Polaroid | `ApplyColorMatrix` | After Phase 1 | |
| Alpha | `ApplyColorMatrix` | After Phase 1 | |
| Gamma | `ApplyColorFilter` (table LUT) | After Phase 1 | Rebuilds LUT every call — §3.3 |
| Colorize | Custom canvas (bypasses helper) | After §3.2 + Phase 1 | §3.2 |
| **BlackAndWhite** | `ApplyPixelOperation` | ❌ until Phase 2a | §2a |
| **ReplaceColor** | `ApplyPixelOperation` (unsafe ✅) | CPU only | Per-pixel; no matrix equivalent |
| **SelectiveColor** | `ApplyPixelOperation` (unsafe ✅) | CPU only | Per-pixel HSL; §3.7 |
| Blur | `SKImageFilter.CreateBlur` | N/A | 3-bitmap chain — §3.4 |
| Sharpen | `SKImageFilter.CreateMatrixConvolution` | N/A | Clean |
| Pixelate | Downscale/upscale | N/A | Deprecated `SKFilterQuality` — §3.5 |
| Border | Canvas drawing | N/A | Redundant `Category` override — §3.6 |
| Glow ✅ | `SKColorFilter` + blur; asymmetric resize | N/A | Redundant `Category` override — §3.6 |
| Reflection ✅ | Canvas + gradient; correct flip + skew width | N/A | Redundant `Category` override — §3.6 |
| Shadow ✅ | `SKColorFilter` + blur; asymmetric resize | N/A | Redundant `Category` override — §3.6 |
| Outline ✅ | `SKImageFilter.CreateDilate` + DstOut | N/A | Redundant `Category` override — §3.6 |
| TornEdge | Path generation | N/A | `new Random()` per call — §3.6, §3.8 |
| Slice ✅ | Canvas drawing; robust bounds | N/A | `new Random()` per call — §3.6, §3.8 |
| Resize | `SKBitmap.Resize` | N/A | Deprecated `SKFilterQuality` — §3.5 |
| Rotate (orthogonal) | `ExtractSubset` / canvas transform | N/A | Optimised |
| Rotate (custom) | Canvas transform | N/A | Clean |
| Rotate3D | `SKMatrix44` | N/A | Clean |
| Rotate3DBox | `SKMatrix44` + shading | N/A | Clean |
| Flip | Canvas scale | N/A | Clean |
| AutoCrop | Pixel scan | N/A | Clean |
| RoundedCorners | Canvas clip path | N/A | Clean |
| Skew | `SKMatrix.CreateSkew` | N/A | Clean |

---

## 0. Dual-host compatibility (XerahS + ShareX)

ImageEditor lives in a shared codebase consumed by **XerahS** and **ShareX**. Both use the same Avalonia-based EditorWindow/EditorView when the modern editor is enabled (ShareX: `UseModernImageEditor` → `AvaloniaIntegration.ShowEditorDialog`). Compatibility rules:

| Rule | Rationale |
|---|---|
| **No host-specific APIs in ImageEditor core/effects** | ImageEditor must not reference XerahS-only or ShareX-only assemblies. Effect pipeline and `ApplyColorFilter` helper stay in `ShareX.ImageEditor` with no dependency on host. |
| **GRContext is optional and provided by the host** | The effect library accepts an optional `GRContext` (or delegate). When `null` or not set, the existing software path runs. No requirement for either host to provide it. |
| **Host wiring is each host's responsibility** | XerahS may obtain `GRContext` from its main Avalonia renderer and pass it when applying effects. ShareX may do the same from the Avalonia editor window's renderer if feasible; if not wired, ShareX simply does not set a context and effects use the software path. Both get correct, identical effect results. |
| **Phase 2 (BlackAndWhite, ReplaceColor, SelectiveColor)** | Pure ImageEditor code; no host dependency. Behaves identically in both hosts. |
| **Validation** | When implementing, ensure ImageEditor builds and effect behavior is unchanged in both repo configurations (XerahS and ShareX). Optionally validate GPU path in XerahS and software fallback in both. |

---

## 1. Phase 1: Use GPU for color-matrix / color-filter effects ❌ NOT STARTED

### 1.1 Obtain GRContext from Avalonia (host-specific)

- **XerahS:** In the host (e.g. `EditorView` or wherever effects are triggered), get the current Skia GPU context from the Avalonia renderer: `IPlatformGraphics` (e.g. via `AvaloniaLocator` or dependency) → cast to `ISkiaGpuWithPlatformGraphicsContext` → `TryGetGrContext()` (returns `IScopedResource<GRContext>?`). Or use `ISkiaSharpApiLease` / `GrContext` if available during rendering.
- **ShareX:** When the modern editor is shown via `AvaloniaIntegration.ShowEditorDialog`, the editor runs in an Avalonia window. If that window's renderer exposes a Skia backend, the same pattern can be used to obtain `GRContext` when applying effects from within the editor. If not wired, leave context unset; effects use the software path (no functional change from today).
- **Constraint:** The context is valid only on the **render/UI thread** and only while the lease/scope is held. Effect application that uses GPU must run on that thread (or dispatch to it) and complete within the scope.

### 1.2 Centralized "apply color filter (with optional GPU)" helper

- **Where:** `ShareX.ImageEditor/Core/ImageEffects/Adjustments/ImageEffect.cs` — update the existing `ApplyColorFilter` and `ApplyColorMatrix` helpers.
- **Current signature (CPU-only):**
  ```csharp
  protected static SKBitmap ApplyColorFilter(SKBitmap source, SKColorFilter filter)
  ```
- **New signature (optional GPU):**
  ```csharp
  protected static SKBitmap ApplyColorFilter(SKBitmap source, SKColorFilter filter, GRContext? grContext = null)
  ```
- **GPU path (when `grContext != null`):**
  - Create `SKSurface` with `SKSurface.Create(grContext, budgeted: true, imageInfo)` (offscreen GPU render target sized to `source.Width x source.Height`).
  - Draw: `canvas.Clear(transparent); canvas.DrawBitmap(source, 0, 0, paintWithFilter);`
  - Flush: `surface.Canvas.Flush()` then `surface.Snapshot().ToRasterImage()` (or `surface.PeekPixels()` into a new `SKBitmap`) to read back pixels.
  - Return that bitmap and dispose the surface.
- **CPU fallback (when `grContext == null`):** Keep the current `new SKCanvas(result)` implementation unchanged.
- **`ApplyColorMatrix` delegates to `ApplyColorFilter`** (already does); just threads `grContext` through.

### 1.3 Threading and context lifetime

- Effects are today invoked from ViewModels / dialogs and may run on background threads (e.g. for preview or apply).
- **Option A (recommended):** When GPU is desired, **dispatch** the effect application to the UI/render thread, acquire `TryGetGrContext()` there, run the GPU path, then return the result (or post back). Ensures we use the context on the correct thread.
- **Option B:** Run effects on a background thread and use a **separate** offscreen `GRContext` (e.g. Vulkan) created once per process; no dependency on Avalonia's context. More code (Vulkan/GL setup) but no UI-thread dependency.
- **Recommendation:** Start with **Option A** (use Avalonia's context on UI/render thread) so we don't introduce Vulkan/GL boilerplate. If needed later, add Option B for background effect processing.
- **GPU threshold:** For small images (e.g. `width*height < 160_000`, roughly ≤ 400×400 px) the GPU read-back overhead can dominate. Consider applying GPU only above a pixel-count threshold (see §3.1).

### 1.4 Wiring the context into the effect pipeline

- **`MainViewModel` / `EditorCore`** (code that calls `effect.Apply(source)`) should not know about Skia's GPU; keep the public API as `Func<SKBitmap, SKBitmap>`. All wiring stays inside ImageEditor or is injected by the host.
- Introduce an **optional** "effect context" or "render context" that **either host** can set (e.g. when the editor view is loaded), which carries an optional `GRContext` (or a delegate that returns a scoped GRContext). The effect base or a small "effect runner" in the same assembly can check this and pass the context into `ApplyColorFilter` when calling into the adjustment base.
- Each host that wants GPU: obtain `GRContext` (e.g. in a render callback or when opening the editor), store it in a thread-safe way or pass it via a closure to the effect runner. The runner calls existing `Effect.Apply(source)`; the implementation of `ApplyColorFilter` inside the effect library uses the injected context when available. If no context is provided (e.g. ShareX not yet wired), the software path runs—identical behavior for both hosts.
- **`ColorizeImageEffect` note:** This effect currently manages its own canvas instead of using the `ApplyColorFilter` helper (see §3.2). It must be refactored to use the helper so it benefits from the GPU path too.

**Deliverables (Phase 1):**

- Helper that applies a color filter (or matrix) using an optional `GRContext`; fallback to current software path.
- All existing `ApplyColorMatrix` / `ApplyColorFilter` call sites use this helper (including `ColorizeImageEffect` after refactor).
- XerahS: obtain Avalonia's `GRContext` and provide it to the effect pipeline when applying effects (e.g. on UI thread). ShareX: optionally wire `GRContext` from the editor window's Avalonia renderer when feasible; otherwise leave unset (software path).
- Document that GPU is used only when context is available and effect runs on the correct thread.
- Ensure ImageEditor builds and effect behavior is unchanged in both XerahS and ShareX.

---

## 2. Phase 2: Per-pixel effects

### 2a. BlackAndWhite → color matrix ❌ NOT DONE

**Current code** (`BlackAndWhiteImageEffect.Apply`):
```csharp
return ApplyPixelOperation(source, (color) =>
{
    float lum = 0.2126f * color.Red + 0.7152f * color.Green + 0.0722f * color.Blue;
    return lum > 127 ? SKColors.White : SKColors.Black;
});
```

**Problem:** Uses `ApplyPixelOperation` so it cannot use the GPU path even after Phase 1 is implemented. Also note it discards source alpha (replaces every pixel with fully-opaque White or Black — intentional behaviour, not a bug, but document it).

**Proposed change:** Replace with a two-pass color-filter approach:
1. **Pass 1 — luminance grayscale** (same matrix already used by `GrayscaleImageEffect` at strength=100):
   ```csharp
   float[] grayscale = {
       0.2126f, 0.7152f, 0.0722f, 0, 0,
       0.2126f, 0.7152f, 0.0722f, 0, 0,
       0.2126f, 0.7152f, 0.0722f, 0, 0,
       0,       0,       0,       1, 0
   };
   using var step1 = ApplyColorMatrix(source, grayscale);   // thread grContext when Phase 1 done
   ```
2. **Pass 2 — hard threshold** via `SKColorFilter.CreateTable` with a step function:
   ```csharp
   byte[] table = new byte[256];
   for (int i = 0; i < 256; i++) table[i] = i < 128 ? (byte)0 : (byte)255;
   byte[] alphaTable = new byte[256];
   for (int i = 0; i < 256; i++) alphaTable[i] = 255; // force fully opaque (matches old behaviour)
   using var filter = SKColorFilter.CreateTable(alphaTable, table, table, table);
   return ApplyColorFilter(step1, filter);   // thread grContext here too
   ```
   - After Pass 1, all three RGB channels are equal (the luminance value). The table maps 0–127 → 0, 128–255 → 255. Alpha is forced to 255 (matches original behaviour of dropping source alpha).
   - This route goes through `ApplyColorFilter` and is therefore GPU-eligible after Phase 1.

**Deliverables (Phase 2a):**
- `BlackAndWhiteImageEffect.Apply` uses the two-pass color-filter approach.
- No `ApplyPixelOperation` call remains in `BlackAndWhiteImageEffect`.
- Behaviour is pixel-for-pixel identical to the existing implementation (validate with test images including transparent regions).

### 2b. ReplaceColor and SelectiveColor: fast pixel loop ✅ DONE (by Jaex, Round 1)

`Adjustments/ImageEffect.cs::ApplyPixelOperation` uses `unsafe` pointer arithmetic (`SKColor*`) for `Bgra8888` format; falls back to `source.Pixels` managed array for other color types. No `GetPixel`/`SetPixel`. **No further work required.**

---

## 3. Phase 3: Additional optimisations and code health

### 3.1 GPU read-back threshold (after Phase 1) ❌ NOT DONE

GPU read-back (`surface.Snapshot().ToRasterImage()` or `PeekPixels`) has fixed overhead that dominates for small images. After Phase 1 lands, add a heuristic in `ApplyColorFilter`:

```csharp
const int GpuPixelThreshold = 160_000; // ~400×400
if (grContext != null && source.Width * source.Height >= GpuPixelThreshold)
{
    // GPU path
}
else
{
    // CPU path (even if grContext is available)
}
```

Tune the threshold empirically on real hardware. Document the chosen value.

### 3.2 `ColorizeImageEffect` — refactor to use helper ❌ NOT DONE

`ColorizeImageEffect.Apply` currently bypasses the base class helpers and manages its own `SKCanvas` / `SKBitmap`. This means it will **not** benefit from the GPU path after Phase 1 unless refactored.

**Current logic:**
- Full-strength: apply `SKColorFilter.CreateCompose(tint, grayscale)` in a single draw.
- Partial-strength (`strength < 100`): draw original, then draw source again with the composed filter at `paint.Color = (255,255,255, blendAlpha)`.

The partial-strength blending cannot be expressed as a single color filter (it requires two composited draws). The cleanest refactor:
1. `using var colorized = ApplyColorFilter(source, composedFilter);` — GPU-eligible, gets the fully-colorized bitmap.
2. Create a new bitmap at source size; draw `source` first (as baseline), then draw `colorized` with `SKPaint { Color = new SKColor(255,255,255,blendAlpha) }` on top.

This keeps the visual result identical and gives the GPU path to the expensive color-filter step (step 1).

Once refactored, `ColorizeImageEffect` gains the GPU path automatically.

### 3.3 `GammaImageEffect` — optional LUT caching ⚠️ LOW PRIORITY

`GammaImageEffect.Apply` builds a 256-byte lookup table on every call:
```csharp
byte[] table = new byte[256];
for (int i = 0; i < 256; i++) { ... Math.Pow(val, 1.0 / Amount) ... }
```
For live preview (slider drag), this rebuilds on every frame. Cache `(Amount, table)` as a field; only rebuild when `Amount` changes. **Profile before implementing.**

### 3.4 `BlurImageEffect` — intermediate bitmap reduction ⚠️ LOW PRIORITY

Current chain allocates **3 intermediate bitmaps**: `expanded` → `blurred` → `result` (crop). The crop step could be avoided by drawing with a negative translation + clip rect on `blurred`, saving one allocation per call. **Do only if profiling shows allocation pressure.**

### 3.5 `SKFilterQuality` deprecation ❌ NOT DONE

SkiaSharp ≥ 2.88 deprecated `SKFilterQuality` in favour of `SKSamplingOptions`. Two call sites remain:

| File | Current | Replacement |
|---|---|---|
| `PixelateImageEffect.Apply` | `new SKPaint { FilterQuality = SKFilterQuality.None }` | `new SKPaint { SamplingOptions = new SKSamplingOptions(SKFilterMode.Nearest) }` |
| `ResizeImageEffect.Apply` | `source.Resize(info, SKFilterQuality.High)` | `source.Resize(info, new SKSamplingOptions(SKCubicResampler.Mitchell))` |

**No behaviour change.** Fixes compiler warnings and future-proofs against removal.

> **Regression risk:** None. Pixelation relies on nearest-neighbour (no interpolation) and resize uses high-quality cubic; the replacements preserve both.

### 3.6 Redundant `Category` overrides in Filter effects ❌ NOT DONE

`Filters.ImageEffect` (the abstract base in `ShareX.ImageEditor.ImageEffects.Filters`) already sets:
```csharp
public override ImageEffectCategory Category => ImageEffectCategory.Filters;
```

Seven concrete filter effects redundantly re-declare this override when they could simply inherit it:

| File | Action |
|---|---|
| `GlowImageEffect.cs` | Remove `public override ImageEffectCategory Category => ...` |
| `ReflectionImageEffect.cs` | Remove `public override ImageEffectCategory Category => ...` |
| `ShadowImageEffect.cs` | Remove `public override ImageEffectCategory Category => ...` |
| `OutlineImageEffect.cs` | Remove `public override ImageEffectCategory Category => ...` |
| `BorderImageEffect.cs` | Remove `public override ImageEffectCategory Category => ...` |
| `SliceImageEffect.cs` | Remove `public override ImageEffectCategory Category => ...` |
| `TornEdgeImageEffect.cs` | Remove `public override ImageEffectCategory Category => ...` |

**Clean effects** (already inheriting without override): `BlurImageEffect`, `SharpenImageEffect`, `PixelateImageEffect`.

**No functional impact.** Do as cleanup alongside any other edit to these files. All seven concrete classes are in `namespace ShareX.ImageEditor.ImageEffects.Filters` so the base class resolves unambiguously.

### 3.7 `SelectiveColorImageEffect` — per-pixel `ToHsl` cost ⚠️ PROFILE FIRST

Every pixel calls `c.ToHsl()` before range detection, even for pixels that end up unmodified. For large images (e.g. 4K ≈ 8M pixels), this is ~8M HSL decompositions per apply. A micro-optimisation:

- **Whites/Blacks/Neutrals** ranges depend only on `L` and `S`. Approximate lightness from raw RGB without full HSL decomposition: `L ≈ (max(R,G,B) + min(R,G,B)) / 510f * 100`. Skip `c.ToHsl()` for desaturated/near-white/near-black pixels; only call it for chromatic pixels. Adds branch complexity. **Profile first** — the unsafe pointer loop is already fast; `ToHsl` may not be the bottleneck.

### 3.8 `SliceImageEffect` and `TornEdgeImageEffect` — `new Random()` per call ❌ NOT DONE

Both `SliceImageEffect.Apply` and `TornEdgeImageEffect.Apply` create `new Random()` at the start of every call:
```csharp
Random rand = new Random();
```

**Two problems:**
1. **Determinism during rapid calls:** if `Apply` is triggered multiple times within the same OS timer tick (e.g. live-preview slider), both instances may be seeded identically and produce the same sequence — visually identical "random" output on consecutive calls.
2. **Unnecessary allocation:** small but avoidable.

**Fix:** Use `Random.Shared` (.NET 6+, thread-safe, no allocation):
```csharp
var rand = Random.Shared;
```

Drop the local `Random` variable; replace all `rand.Next(...)` calls in the method body with `Random.Shared.Next(...)`.

> **Regression risk:** None. The fix produces statistically equivalent randomness. The visual output of Slice and TornEdge will differ between calls as intended (no determinism loss).

---

## 4. Summary table

| Phase | What | Status | Outcome |
|---|---|---|---|
| **2b** | `ApplyPixelOperation` unsafe pointer loop | ✅ **DONE** (Round 1) | ReplaceColor/SelectiveColor at native speed. |
| **Glow** | AutoResize + asymmetric expansion | ✅ **DONE** (Round 2) | Glow canvas grows only toward the offset. |
| **Shadow** | Color property + asymmetric expansion | ✅ **DONE** (Round 2) | Consistent with Glow. |
| **Reflection** | Fixed flip matrix; skew width expansion | ✅ **DONE** (Round 2) | Correct visual output with skew. |
| **Outline** | OutlineOnly DstOut mode | ✅ **DONE** (Round 2) | Ring-only outline render. |
| **Slice** | Robust bounds | ✅ **DONE** (Round 2) | No crash on edge-case slider values. |
| **2a** | BlackAndWhite via two-pass color filter | ❌ Pending | No per-pixel loop; GPU-eligible after Phase 1. |
| **3.8** | `Random.Shared` in Slice/TornEdge | ❌ Pending (quick fix) | No determinism issue on rapid calls; no allocation. |
| **3.5** | `SKFilterQuality` → `SKSamplingOptions` | ❌ Pending (quick fix) | Removes deprecated-API warnings. |
| **3.6** | Remove redundant `Category` overrides (7 files) | ❌ Pending (quick fix) | Code consistency. |
| **3.2** | `ColorizeImageEffect` refactor to use helper | ❌ Pending (prerequisite for GPU) | Colorize gains GPU path. |
| **1** | GPU surface via `GRContext` for `ApplyColorFilter`/`ApplyColorMatrix` | ❌ Pending | All matrix/filter effects on GPU when context available. |
| **3.1** | GPU read-back threshold | ❌ Pending (part of Phase 1) | Skip GPU for tiny images. |
| **3.3** | `GammaImageEffect` LUT caching | ⚠️ Low priority | Minor live-preview speedup. |
| **3.4** | `BlurImageEffect` allocation reduction | ⚠️ Low priority | One fewer SKBitmap per blur call. |
| **3.7** | `SelectiveColor` `ToHsl` short-circuit | ⚠️ Profile first | Potential speedup at 4K scale. |

**Implementation order recommendation:**
1. **3.8** — two lines changed, no risk, solves determinism bug.
2. **3.5** — trivial, no risk, removes warnings.
3. **3.6** — trivial, no risk, 7 one-line deletions.
4. **2a** — straightforward, no GPU dependency.
5. **3.2** — prerequisite for Phase 1 GPU to cover Colorize.
6. **1 + 3.1** — main GPU work: wire GRContext, size threshold.
7. **3.3, 3.4, 3.7** — only if profiling shows need.

---

## 5. References

- [docs/planning/IMAGEEDITOR_SKIA_GPU_PLAN.md](../docs/planning/IMAGEEDITOR_SKIA_GPU_PLAN.md)
- [Avalonia Skia – TryGetGrContext](https://api-docs.avaloniaui.net/docs/M_Avalonia_Skia_ISkiaGpuWithPlatformGraphicsContext_TryGetGrContext)
- [SkiaSharp GRContext](https://learn.microsoft.com/en-us/dotnet/api/skiasharp.grcontext)
- [SkiaSharp SKSamplingOptions migration](https://github.com/mono/SkiaSharp/wiki/API-Changes-2.88)
- Base helpers: [ImageEditor/src/ShareX.ImageEditor/Core/ImageEffects/Adjustments/ImageEffect.cs](../ImageEditor/src/ShareX.ImageEditor/Core/ImageEffects/Adjustments/ImageEffect.cs)
- BlackAndWhite (§2a): [ImageEditor/src/ShareX.ImageEditor/Core/ImageEffects/Adjustments/BlackAndWhiteImageEffect.cs](../ImageEditor/src/ShareX.ImageEditor/Core/ImageEffects/Adjustments/BlackAndWhiteImageEffect.cs)
- Colorize (§3.2): [ImageEditor/src/ShareX.ImageEditor/Core/ImageEffects/Adjustments/ColorizeImageEffect.cs](../ImageEditor/src/ShareX.ImageEditor/Core/ImageEffects/Adjustments/ColorizeImageEffect.cs)
- Pixelate (§3.5): [ImageEditor/src/ShareX.ImageEditor/Core/ImageEffects/Filters/PixelateImageEffect.cs](../ImageEditor/src/ShareX.ImageEditor/Core/ImageEffects/Filters/PixelateImageEffect.cs)
- Resize (§3.5): [ImageEditor/src/ShareX.ImageEditor/Core/ImageEffects/Manipulations/ResizeImageEffect.cs](../ImageEditor/src/ShareX.ImageEditor/Core/ImageEffects/Manipulations/ResizeImageEffect.cs)
- Slice (§3.8): [ImageEditor/src/ShareX.ImageEditor/Core/ImageEffects/Filters/SliceImageEffect.cs](../ImageEditor/src/ShareX.ImageEditor/Core/ImageEffects/Filters/SliceImageEffect.cs)
- TornEdge (§3.8): [ImageEditor/src/ShareX.ImageEditor/Core/ImageEffects/Filters/TornEdgeImageEffect.cs](../ImageEditor/src/ShareX.ImageEditor/Core/ImageEffects/Filters/TornEdgeImageEffect.cs)
- ShareX host: `ShareX/TaskHelpers.cs` (`OpenImageEditor`, `UseModernImageEditor` → `AvaloniaIntegration.ShowEditorDialog`); `ShareX.ImageEditor/Helpers/AvaloniaIntegration.cs`
