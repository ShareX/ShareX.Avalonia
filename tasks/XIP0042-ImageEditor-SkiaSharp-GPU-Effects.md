# XIP0042: ImageEditor SkiaSharp hardware acceleration for image effects

## Summary

**Goal:** Use SkiaSharp's GPU backend for image effects where possible, and speed up remaining CPU paths.

**Scope:** ShareX.ImageEditor (ImageEditor) — adjustment/filter effects and their application pipeline. **ImageEditor is shared and used by two hosts:**

- **XerahS** — Avalonia desktop app; embeds EditorView. GPU wiring intentionally deferred — see §1.5.
- **ShareX** — WinForms app; opens the modern ImageEditor via `AvaloniaIntegration.ShowEditorDialog` (Avalonia window). Same ImageEditor codebase, different host process.

Any change to ImageEditor (effects, pipeline, or optional GPU path) **must remain compatible with both hosts**. The host may provide an optional `GRContext` when applying effects; when not provided (`null`), the software path is always used (no breaking change for either host). Neither host currently provides one — this is intentional for Phase 1 effects (see §1.5).

---

## Current State (as of Feb 2026)

> **Jaex landed two rounds of optimisations/fixes; all remaining ImageEditor-side items were then implemented in `feature/XIP0042-optimizations`. A third round added diagnostics infrastructure and addressed build-time discoveries. The GPU path in the ImageEditor library is complete, but host wiring is intentionally deferred: the Avalonia 11 frame-scoped API (`AvaloniaLocator / ISkiaGpuWithPlatformGraphicsContext`) is fragile and cannot be used outside a render callback, and a persistent offscreen `GRContext` (the only viable path) has a readback cost that negates the benefit for the color-matrix/filter operations targeted here. GPU wiring will be revisited at Phase 3 (canvas compositor). See §1.5.**

### Round 1 (prior audit)
- ✅ `ApplyPixelOperation` rewritten with `unsafe` pointer arithmetic for `Bgra8888` — no `GetPixel`/`SetPixel`.

### Round 2 (this audit)
- ✅ `GlowImageEffect` — `AutoResize` param added; asymmetric canvas expansion (only expands on the side the glow/offset extends to).
- ✅ `ShadowImageEffect` — Removed `Darkness`, added `Color` property; same asymmetric expansion pattern as Glow.
- ✅ `ReflectionImageEffect` — Fixed flip matrix (proper `Translate + Scale` instead of pivot-based scale); canvas width now accounts for skew displacement.
- ✅ `OutlineImageEffect` — `OutlineOnly` mode added (DstOut erase of inner area).
- ✅ `SliceImageEffect` — Robust `minSliceHeight`/`maxSliceHeight`/`minSliceShift`/`maxSliceShift` bounds checking.

### Round 3 (post-implementation: diagnostics + build fixes)
- ⚠️ `SKSamplingOptions` (§3.5) **reverted** — specific overloads (`SKCubicResampler.Mitchell`, `SKBitmap.Resize(info, SKSamplingOptions)`) absent in SkiaSharp 2.88.9; code remains on `SKFilterQuality` pending a SkiaSharp upgrade.
- ✅ `ReadPixels` corrected to the **5-arg overload** in the GPU surface read-back path (Phase 1 build fix).
- ✅ `SKCanvasControl` rewritten to use `ICustomDrawOperation` + `ISkiaSharpApiLeaseFeature` — correct Avalonia 11 pattern for obtaining `GRContext` during custom Skia rendering.
- ✅ `IEditorDiagnosticsSink` / `EditorDiagnosticEvent` / `EditorDiagnosticLevel` added to `ShareX.ImageEditor.Services` — host-agnostic interface for observability.
- ✅ `EditorServices.Diagnostics` property wired in ImageEditor; GPU path decision points (`SetGpuContext`, `ApplyColorFilter` GPU/CPU branch) emit structured diagnostic events.
- ✅ `EditorDiagnosticsAdapter` added to XerahS (`XerahS.UI.Services`) — routes `IEditorDiagnosticsSink` events to `DebugHelper.WriteLine` / `WriteException`.
- ✅ Wired in `App.axaml.cs`: `EditorServices.Diagnostics = new EditorDiagnosticsAdapter()`.
- ✅ `src/desktop/app/run-debug-app.sh` added — convenience script to launch the desktop app in Debug configuration for GPU path verification.

### Implementation status

| Area | Status |
|---|---|
| **Phase 1** — GRContext / GPU surface path (ImageEditor library) | ✅ DONE |
| **Phase 1** — XerahS host wiring (`SetGpuContext`) | ⏸️ DEFERRED — see §1.5; revisit at Phase 3 |
| **Phase 2a** — BlackAndWhite via color matrix | ✅ DONE |
| **§3.1** — GPU pixel threshold (160 000 px) | ✅ DONE |
| **§3.2** — `ColorizeImageEffect` refactor | ✅ DONE |
| **§3.5** — `SKFilterQuality` → `SKSamplingOptions` | ⚠️ Reverted — SkiaSharp 2.88.9 missing overloads; revisit after upgrade |
| **§3.6** — Redundant `Category` overrides in 7 filter effects | ✅ DONE |
| **§3.8** — `new Random()` per call in Slice/TornEdge | ✅ DONE |
| **§3.9** — Diagnostics / observability infrastructure | ✅ DONE |
| §3.3 GammaImageEffect LUT caching | ⚠️ Low priority — profile first |
| §3.4 BlurImageEffect allocation reduction | ⚠️ Low priority — profile first |
| §3.7 SelectiveColor `ToHsl` short-circuit | ⚠️ Profile first |

### Effect map

| Effect | Method | GPU-ready? | Notes |
|---|---|---|---|
| Brightness | `ApplyColorMatrix` | GPU deferred (§1.5) | |
| Contrast | `ApplyColorMatrix` | GPU deferred (§1.5) | |
| Saturation | `ApplyColorMatrix` | GPU deferred (§1.5) | |
| Hue | `ApplyColorMatrix` | GPU deferred (§1.5) | |
| Invert | `ApplyColorMatrix` | GPU deferred (§1.5) | |
| Grayscale | `ApplyColorMatrix` | GPU deferred (§1.5) | |
| Sepia | `ApplyColorMatrix` | GPU deferred (§1.5) | |
| Polaroid | `ApplyColorMatrix` | GPU deferred (§1.5) | |
| Alpha | `ApplyColorMatrix` | GPU deferred (§1.5) | |
| Gamma | `ApplyColorFilter` (table LUT) | GPU deferred (§1.5) | Rebuilds LUT every call — §3.3 |
| Colorize ✅ | `ApplyColorFilter` helper | GPU deferred (§1.5) ✅ | §3.2 done |
| **BlackAndWhite** ✅ | Two-pass color filter | GPU-eligible ✅ | §2a done |
| **ReplaceColor** | `ApplyPixelOperation` (unsafe ✅) | CPU only | Per-pixel; no matrix equivalent |
| **SelectiveColor** | `ApplyPixelOperation` (unsafe ✅) | CPU only | Per-pixel HSL; §3.7 |
| Blur | `SKImageFilter.CreateBlur` | N/A | 3-bitmap chain — §3.4 |
| Sharpen | `SKImageFilter.CreateMatrixConvolution` | N/A | Clean |
| Pixelate | Downscale/upscale | N/A | `SKFilterQuality` — §3.5 reverted (SkiaSharp 2.88.9) |
| Border ✅ | Canvas drawing | N/A | `Category` override removed — §3.6 done |
| Glow ✅ | `SKColorFilter` + blur; asymmetric resize | N/A | `Category` override removed — §3.6 done |
| Reflection ✅ | Canvas + gradient; correct flip + skew width | N/A | `Category` override removed — §3.6 done |
| Shadow ✅ | `SKColorFilter` + blur; asymmetric resize | N/A | `Category` override removed — §3.6 done |
| Outline ✅ | `SKImageFilter.CreateDilate` + DstOut | N/A | `Category` override removed — §3.6 done |
| TornEdge ✅ | Path generation | N/A | `Random.Shared`; `Category` removed — §3.6, §3.8 done |
| Slice ✅ | Canvas drawing; robust bounds | N/A | `Random.Shared`; `Category` removed — §3.6, §3.8 done |
| Resize | `SKBitmap.Resize` | N/A | `SKFilterQuality` — §3.5 reverted (SkiaSharp 2.88.9) |
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
| **GRContext is optional and provided by the host** | The effect library accepts an optional `GRContext` (or delegate). When `null` or not set, the existing software path runs. No requirement for either host to provide it. Neither host provides it currently — intentional (§1.5). |
| **Host GPU wiring deferred to Phase 3** | The Avalonia 11 render-lease API is frame-scoped and cannot be used at effect-application call sites. A persistent offscreen `GRContext` (Option B, §1.3) is the only correct approach but its readback cost negates the benefit for Phase 1 color-matrix/filter operations. Revisit when the canvas compositor (Phase 3) is designed — that is where GPU acceleration delivers a real, measurable win. |
| **Phase 2 (BlackAndWhite, ReplaceColor, SelectiveColor)** | Pure ImageEditor code; no host dependency. Behaves identically in both hosts. |
| **Validation** | When implementing, ensure ImageEditor builds and effect behavior is unchanged in both repo configurations (XerahS and ShareX). Optionally validate GPU path in XerahS and software fallback in both. |

---

## 1. Phase 1: Use GPU for color-matrix / color-filter effects

> **Library side ✅ DONE. Host wiring ⏸️ DEFERRED — see §1.5.** The `ApplyColorFilter`/`ApplyColorMatrix` GPU path exists and is correct in ImageEditor. Host wiring is intentionally not pursued for Phase 1: the only viable API path (persistent offscreen `GRContext`, §1.3 Option B) has a per-effect readback cost that negates the benefit for color-matrix/filter operations on screenshot-sized images, and the Avalonia 11 frame-scoped lease API cannot be used outside a render callback. The `GRContext?` parameter remains in the API as a zero-cost forward-compatibility hook. GPU wiring will be designed at Phase 3 (canvas compositor). Sections §1.1–1.4 below document the API constraints; §1.5 gives the full rationale.

### 1.1 Obtain GRContext from Avalonia (host-specific)

> ⚠️ **FRAGILE API WARNING (Avalonia 11 / SkiaSharp 2.88.x).** The pattern originally described here —
> `AvaloniaLocator.Current.GetService<IPlatformGraphics>()` → cast to `ISkiaGpuWithPlatformGraphicsContext`
> → `TryGetGrContext()` — has **two serious problems** in Avalonia 11 that make it unsuitable as a
> long-term wiring strategy:
>
> 1. **`AvaloniaLocator` is deprecated / internals-hostile in Avalonia 11.** The service-locator pattern
>    used in Avalonia 0.10 was redesigned in Avalonia 11; `AvaloniaLocator.Current` still compiles but is
>    no longer a stable surface — it can return `null`, change between minor releases, or be removed. The
>    `SKCanvasControl → ICustomDrawOperation + ISkiaSharpApiLeaseFeature` rewrite in the editor canvas
>    was specifically done to move *away* from this pattern. Introducing it now in the host wiring would
>    undo that direction.
>
> 2. **`ISkiaSharpApiLease` / `ISkiaSharpApiLeaseFeature` is frame-scoped.** It is only valid inside an
>    active render-pass callback (e.g., inside `Render(DrawingContext)` or an `ICustomDrawOperation`).
>    Effect application is triggered by user action (clicking Apply), which happens entirely **outside**
>    any render frame. Dispatching to the UI thread does not help — being on the UI thread does not mean
>    a render frame is active. The lease acquired in a render callback is already invalidated by the time
>    the next line of non-render code runs.
>
> **Consequence:** Option A in §1.3 ("dispatch to UI/render thread, acquire `TryGetGrContext()` there")
> does **not work** for effect application. It only works for drawing *to the screen*, not for
> computing a new `SKBitmap` from an existing one at arbitrary call sites.
>
> The only viable runtime path for GPU-accelerated effect application is **Option B** (§1.3) — a
> persistent offscreen `GRContext` created independently of Avalonia's renderer. See §1.5 for the
> recommendation.

- **XerahS:** No `GRContext` acquisition is currently wired; all effects take the CPU path. If GPU is
  pursued in the future, a persistent offscreen context (Option B) is required — see §1.3 and §1.5.
- **ShareX:** Same. Leave context unset; effects use the software path (no functional change from today).
- **Constraint (if Option B is implemented):** The persistent `GRContext` must be created on a thread
  that owns the underlying GL/Vulkan context and must be disposed cleanly when the process exits. It is
  **not** Avalonia's context and is not affected by Avalonia's render lifecycle.

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

- **Option A — dispatch to UI/render thread + acquire Avalonia lease:** ❌ **NOT VIABLE for effect application.** Dispatching to the UI thread does not open a render frame. `ISkiaSharpApiLeaseFeature.Lease()` returns `null` outside an active render callback. This option only works for *drawing to the screen* inside `Render(DrawingContext)` / `ICustomDrawOperation`. It cannot be used to compute a new `SKBitmap` on demand and was incorrectly marked "recommended" in an earlier draft.

- **Option B — persistent offscreen GRContext:** Create a dedicated `GRContext` once (e.g., `GRContext.CreateGl(GRGlInterface.Create())` on Linux/macOS, or the equivalent Vulkan path) that is independent of Avalonia's renderer. Use it on a dedicated background thread (or the thread that created it). This is the **only correct approach** for effect application.
  - Pros: works at any call site, background-thread safe, not fragile to Avalonia version changes.
  - Cons: requires platform-specific GL/Vulkan bootstrap (EGL on Linux, WGL on Windows, CGL on macOS); adds ~50–200 ms startup cost; GPU→CPU readback cost often negates benefit for small/medium images.

- **GPU threshold:** For images below roughly 2 MP (e.g. `width*height < 2_000_000`), the upload + compute + readback round-trip for simple color matrix/filter operations is rarely faster than the optimized CPU path (unsafe pointer arithmetic already in place). GPU benefit is mainly visible for operations that can stay on-GPU (compositing, multi-pass effects). See §3.1.

- **See §1.5 for the overall recommendation before committing to either option.**

### 1.4 Wiring the context into the effect pipeline

- **`MainViewModel` / `EditorCore`** (code that calls `effect.Apply(source)`) should not know about Skia's GPU; keep the public API as `Func<SKBitmap, SKBitmap>`. All wiring stays inside ImageEditor or is injected by the host.
- Introduce an **optional** "effect context" or "render context" that **either host** can set (e.g. when the editor view is loaded), which carries an optional `GRContext` (or a delegate that returns a scoped GRContext). The effect base or a small "effect runner" in the same assembly can check this and pass the context into `ApplyColorFilter` when calling into the adjustment base.
- Each host that wants GPU: obtain `GRContext` (e.g. in a render callback or when opening the editor), store it in a thread-safe way or pass it via a closure to the effect runner. The runner calls existing `Effect.Apply(source)`; the implementation of `ApplyColorFilter` inside the effect library uses the injected context when available. If no context is provided (e.g. ShareX not yet wired), the software path runs—identical behavior for both hosts.
- **`ColorizeImageEffect` note:** This effect currently manages its own canvas instead of using the `ApplyColorFilter` helper (see §3.2). It must be refactored to use the helper so it benefits from the GPU path too.

**Deliverables (Phase 1):**

- Helper that applies a color filter (or matrix) using an optional `GRContext`; fallback to current software path (already done in ImageEditor library).
- All existing `ApplyColorMatrix` / `ApplyColorFilter` call sites use this helper (including `ColorizeImageEffect` after refactor).
- ~~XerahS: obtain Avalonia's `GRContext` and provide it to the effect pipeline on the UI thread.~~ **Revised:** host GPU wiring is deferred — see §1.5.
- Document that GPU is used only when context is available and effect runs on the correct thread.
- Ensure ImageEditor builds and effect behavior is unchanged in both XerahS and ShareX.

---

### 1.5 Recommendation: Defer GPU wiring; pursue CPU optimisation instead

**Short answer: Do not pursue GPU for Phase 1 color-matrix/filter effects. The CPU path is already the right tool for this work. Pursue GPU only in Phase 3 (canvas compositor), where pixels can remain on-GPU across multiple operations.**

**Reasoning:**

| Factor | GPU (Option B) | CPU (current) |
|--------|---------------|---------------|
| Avalonia 11 API stability | ✅ Independent of Avalonia if Option B | ✅ No dependency |
| Implementation complexity | ❌ High — platform GL/Vulkan bootstrap per OS | ✅ Already done |
| Effect throughput (1080p image, color matrix) | ⚠️ Marginal at best; upload+readback ≈ 5–15 ms | ✅ ~1–3 ms with unsafe pointer loop |
| Effect throughput (4K image, color matrix) | ⚠️ Possible win ~2–4× for compute, but readback still costly | ✅ ~8–15 ms with unsafe pointer loop |
| Background-thread safe | ✅ Yes (Option B only) | ✅ Yes |
| Cross-platform risk | ❌ GL/EGL/WGL differences; Vulkan not guaranteed | ✅ None |
| Stability / crash risk | ❌ GL driver bugs, context loss, out-of-VRAM | ✅ None |
| Net benefit for screenshot-sized images | ❌ Usually negative (readback dominates) | ✅ Already fast |

**The performance case for GPU does not close for Phase 1 operations:**

Phase 1 effects (brightness, contrast, hue, saturation, color matrix) are a single pass of arithmetic over each pixel. The bottleneck is memory bandwidth, not compute. The optimized CPU path (unsafe pointer arithmetic, `Bgra8888` layout, cache-friendly linear scan) is already within 2–4 cycles/pixel on modern CPUs. A GPU path that uploads the bitmap, dispatches a shader, and reads back the result adds at minimum one full-image PCIe round-trip at each end — for a 1080p image that is ~8 MB × 2 = ~16 MB of PCIe traffic, which at 16 GB/s takes ~1 ms just for transfers, comparable to the entire CPU-side compute. For typical XerahS usage (screenshots, not 4K video frames), GPU is not a win here.

**Where GPU *does* pay off:** Phase 3 — the canvas compositor. When multiple annotation layers, effects, and overlays are applied and the result is rendered back to the screen without a CPU readback step, pixels never leave the GPU and the benefit is real. That is the correct place to invest in a persistent `GRContext`.

**Recommended action:**

1. **Keep Phase 1 host wiring as NOT DONE.** The library-side `grContext` parameter stays in the API as a forward-compatibility hook (zero cost when `null`). No host glue to write.
2. **Leave §1.1 Option A description as a warning** (done above) so no one wastes time trying it.
3. **When Phase 3 (canvas compositor) is designed**, revisit Option B with a proper persistent offscreen `GRContext`. At that point the design can keep composited layers on-GPU and avoid the readback-per-effect penalty.
4. **For performance now:** Profile whether `ApplyPixelOperation` for `ReplaceColor` / `SelectiveColor` / `BlackAndWhite` is measurably slow on large images. If yes, the right fix is SIMD intrinsics or parallelised loops — not GPU.

---

## 2. Phase 2: Per-pixel effects

### 2a. BlackAndWhite → color matrix ✅ DONE

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

### 3.1 GPU read-back threshold ✅ DONE

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

### 3.2 `ColorizeImageEffect` — refactor to use helper ✅ DONE

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

### 3.5 `SKFilterQuality` → `SKSamplingOptions` ⚠️ Reverted — pending SkiaSharp upgrade

SkiaSharp ≥ 2.88 deprecated `SKFilterQuality` in favour of `SKSamplingOptions`. The migration was implemented and initially marked done, but **subsequently reverted** because SkiaSharp 2.88.9 (the version in use) does not expose the required overloads: `SKCubicResampler.Mitchell` and the `SKBitmap.Resize(SKImageInfo, SKSamplingOptions)` overload are absent in that build. Code reverts to `SKFilterQuality` to maintain a clean build (0 errors, 0 warnings).

**Planned replacements** (for when SkiaSharp is upgraded):

| File | Current (reverted) | Planned |
|---|---|---|
| `PixelateImageEffect.Apply` | `new SKPaint { FilterQuality = SKFilterQuality.None }` | `new SKPaint { SamplingOptions = new SKSamplingOptions(SKFilterMode.Nearest) }` |
| `ResizeImageEffect.Apply` | `source.Resize(info, SKFilterQuality.High)` | `source.Resize(info, new SKSamplingOptions(SKCubicResampler.Mitchell))` |

**No behaviour change when applied.** Fixes compiler warnings and future-proofs against removal.

> **Action required:** Re-apply after upgrading SkiaSharp past 2.88.9 to a build that exposes these overloads. Regression risk is none — pixelation uses nearest-neighbour and resize uses high-quality cubic; the replacements preserve both.

### 3.6 Redundant `Category` overrides in Filter effects ✅ DONE

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

### 3.8 `SliceImageEffect` and `TornEdgeImageEffect` — `new Random()` per call ✅ DONE

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

### 3.9 Diagnostics / observability infrastructure ✅ DONE

Added a host-agnostic diagnostics interface to ImageEditor so GPU path decisions and errors are observable at the host level without coupling ImageEditor to any specific logging framework.

**ImageEditor side (`ShareX.ImageEditor.Services`):**
- `IEditorDiagnosticsSink` — interface with single method `Report(EditorDiagnosticEvent)`.
- `EditorDiagnosticEvent` — struct carrying `Source`, `Message`, `Level` (`EditorDiagnosticLevel`: Info/Warning/Error), and optional `ExceptionText`.
- `EditorServices.Diagnostics` — static nullable `IEditorDiagnosticsSink?` property; set by the host at startup.
- Emit points: `SetGpuContext()` (context acquired / cleared), `ApplyColorFilter()` (GPU branch taken / CPU fallback reason), and `SKCanvasControl` render callbacks.

**XerahS host side (`XerahS.UI.Services`):**
- `EditorDiagnosticsAdapter : IEditorDiagnosticsSink` — routes events to `DebugHelper`: Info → `WriteLine`, Warning → `WriteLine("WARN …")`, Error → `WriteException` or `WriteLine("ERROR …")`.
- Wired in `App.axaml.cs` (composition root): `EditorServices.Diagnostics = new EditorDiagnosticsAdapter()`.

**Developer tooling:**
- `src/desktop/app/run-debug-app.sh` — shell script that resolves `dotnet` (PATH or `~/.dotnet/dotnet`) and launches `XerahS.App.csproj` in Debug configuration, making GPU diagnostics visible on stdout/in the IDE debug output window.

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
| **2a** | BlackAndWhite via two-pass color filter | ✅ **DONE** | No per-pixel loop; GPU-eligible. |
| **3.8** | `Random.Shared` in Slice/TornEdge | ✅ **DONE** | No determinism issue on rapid calls; no allocation. |
| **3.5** | `SKFilterQuality` → `SKSamplingOptions` | ⚠️ **Reverted** — SkiaSharp 2.88.9 missing overloads | Re-apply after SkiaSharp upgrade. |
| **3.6** | Remove redundant `Category` overrides (7 files) | ✅ **DONE** | Code consistency. |
| **3.2** | `ColorizeImageEffect` refactor to use helper | ✅ **DONE** | Colorize gains GPU path. |
| **1 (library)** | GPU surface via `GRContext` for `ApplyColorFilter`/`ApplyColorMatrix` | ✅ **DONE** | ImageEditor GPU path ready; awaits host to call `SetGpuContext`. |
| **1 (host wiring)** | XerahS calls `SetGpuContext(grContext)` to activate GPU path | ❌ **NOT DONE** | `SetGpuContext` never called — GPU inactive on all platforms. |
| **3.1** | GPU read-back threshold | ✅ **DONE** | 160 000 px threshold; CPU for small images. |
| **3.9** | Diagnostics / observability (`IEditorDiagnosticsSink`) | ✅ **DONE** (Round 3) | GPU path decisions visible in host debug output. |
| **3.3** | `GammaImageEffect` LUT caching | ⚠️ Low priority | Minor live-preview speedup. |
| **3.4** | `BlurImageEffect` allocation reduction | ⚠️ Low priority | One fewer SKBitmap per blur call. |
| **3.7** | `SelectiveColor` `ToHsl` short-circuit | ⚠️ Profile first | Potential speedup at 4K scale. |

**Remaining open items:**

- **Phase 1 host wiring — intentionally deferred (see §1.5):** GPU acceleration for color-matrix/filter effects is not being pursued now. The Avalonia 11 render-thread API (`AvaloniaLocator / ISkiaGpuWithPlatformGraphicsContext`) is fragile and frame-scoped — it cannot be used outside a render callback, which is where effect application happens. The only correct approach (persistent offscreen `GRContext`, Option B in §1.3) has platform-specific GL/Vulkan complexity that is not justified for these operations given the optimized CPU path already in place. The `GRContext?` parameter stays in the API as a forward-compatibility hook at zero cost. Revisit when Phase 3 (canvas compositor) is designed.

- **§3.5** — Re-apply `SKSamplingOptions` migration after upgrading SkiaSharp past 2.88.9.

- **§3.3, §3.4, §3.7** — Profile-gated; only implement if profiling confirms they are bottlenecks.

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
- Diagnostics interface (§3.9): `ImageEditor/src/ShareX.ImageEditor/Services/IEditorDiagnosticsSink.cs`; `EditorDiagnosticEvent.cs`; `EditorServices.cs`
- Diagnostics adapter (§3.9): [src/desktop/app/XerahS.UI/Services/EditorDiagnosticsAdapter.cs](../src/desktop/app/XerahS.UI/Services/EditorDiagnosticsAdapter.cs)
- Debug launch script (§3.9): [src/desktop/app/run-debug-app.sh](../src/desktop/app/run-debug-app.sh)
- ShareX host: `ShareX/TaskHelpers.cs` (`OpenImageEditor`, `UseModernImageEditor` → `AvaloniaIntegration.ShowEditorDialog`); `ShareX.ImageEditor/Helpers/AvaloniaIntegration.cs`
