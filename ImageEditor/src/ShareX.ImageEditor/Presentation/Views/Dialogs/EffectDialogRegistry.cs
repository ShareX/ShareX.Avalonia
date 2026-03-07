#region License Information (GPL v3)

/*
    ShareX.ImageEditor - The UI-agnostic Editor library for ShareX
    Copyright (c) 2007-2026 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using Avalonia.Controls;

namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    /// <summary>
    /// XIP0039 Pain Point 3: Centralizes effect dialog factories so new effects can be
    /// registered without adding handler methods to <c>EditorView</c>.
    /// <para>
    /// To add a new dialog-based effect:
    /// <list type="number">
    ///   <item>Implement <see cref="IEffectDialog"/> on a <see cref="UserControl"/>.</item>
    ///   <item>Add a factory entry in <see cref="EffectDialogRegistry._factories"/> below.</item>
    ///   <item>Add a menu item in <c>EffectsMenuDropdown</c> that calls <c>RaiseDialog("your_id")</c>.</item>
    /// </list>
    /// No new handler method in <c>EditorView.EffectsHost.cs</c> is required.
    /// </para>
    /// </summary>
    public static class EffectDialogRegistry
    {
        private static readonly Dictionary<string, Func<UserControl>> _factories =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // --- Adjustments ---
                ["auto_contrast"] = () => new AutoContrastDialog(),
                ["brightness"] = () => new BrightnessDialog(),
                ["contrast"] = () => new ContrastDialog(),
                ["hue"] = () => new HueDialog(),
                ["saturation"] = () => new SaturationDialog(),
                ["gamma"] = () => new GammaDialog(),
                ["alpha"] = () => new AlphaDialog(),
                ["color_matrix"] = () => new ColorMatrixDialog(),
                ["colorize"] = () => new ColorizeDialog(),
                ["selective_color"] = () => new SelectiveColorDialog(),
                ["replace_color"] = () => new ReplaceColorDialog(),
                ["grayscale"] = () => new GrayscaleDialog(),
                ["posterize"] = () => new PosterizeDialog(),
                ["sepia"] = () => new SepiaDialog(),
                ["solarize"] = () => new SolarizeDialog(),
                ["exposure"] = () => new ExposureDialog(),
                ["film_emulation"] = () => new FilmEmulationDialog(),
                ["levels"] = () => new LevelsDialog(),
                ["shadows_highlights"] = () => new ShadowsHighlightsDialog(),
                ["temperature_tint"] = () => new TemperatureTintDialog(),
                ["threshold"] = () => new ThresholdDialog(),
                ["vibrance"] = () => new VibranceDialog(),
                ["duotone_gradient_map"] = () => new DuotoneGradientMapDialog(),

                // --- Filters ---
                ["add_noise"] = () => new AddNoiseDialog(),
                ["anime_speed_lines"] = () => new AnimeSpeedLinesDialog(),
                ["ascii_art"] = () => new ASCIIArtDialog(),
                ["block_glitch"] = () => new BlockGlitchDialog(),
                ["blueprint_drawing"] = () => new BlueprintDrawingDialog(),
                ["border"] = () => new BorderDialog(),
                ["cartoon_sticker_cutout"] = () => new CartoonStickerCutoutDialog(),
                ["claymation_texture"] = () => new ClaymationTextureDialog(),
                ["outline"] = () => new OutlineDialog(),
                ["shadow"] = () => new ShadowDialog(),
                ["glow"] = () => new GlowDialog(),
                ["reflection"] = () => new ReflectionDialog(),
                ["crystalize_shards"] = () => new CrystalizeShardsDialog(),
                ["crosshatch"] = () => new CrosshatchDialog(),
                ["oil_paint"] = () => new OilPaintDialog(),
                ["halftone"] = () => new HalftoneDialog(),
                ["sobel_edge"] = () => new SobelEdgeDialog(),
                ["torn_edge"] = () => new TornEdgeDialog(),
                ["wave_edge"] = () => new WaveEdgeDialog(),
                ["slice"] = () => new SliceDialog(),
                ["color_depth"] = () => new ColorDepthDialog(),
                ["convolution_matrix"] = () => new ConvolutionMatrixDialog(),
                ["crt"] = () => new CRTDialog(),
                ["gaussian_blur"] = () => new GaussianBlurDialog(),
                ["bloom"] = () => new BloomDialog(),
                ["dithering"] = () => new DitheringDialog(),
                ["lens_blur"] = () => new LensBlurDialog(),
                ["median_filter"] = () => new MedianFilterDialog(),
                ["matrix_digital_rain"] = () => new MatrixDigitalRainDialog(),
                ["motion_blur"] = () => new MotionBlurDialog(),
                ["spin_blur"] = () => new SpinBlurDialog(),
                ["surface_blur"] = () => new SurfaceBlurDialog(),
                ["halation"] = () => new HalationDialog(),
                ["pixel_sorting"] = () => new PixelSortingDialog(),
                ["rgb_split"] = () => new RGBSplitDialog(),
                ["crystal_prism"] = () => new CrystalPrismDialog(),
                ["hologram_scan"] = () => new HologramScanDialog(),
                ["holographic_foil_shimmer"] = () => new HolographicFoilShimmerDialog(),
                ["ink_splatter_drips"] = () => new InkSplatterDripsDialog(),
                ["liquid_glass"] = () => new LiquidGlassDialog(),
                ["neon_edge_glow"] = () => new NeonEdgeGlowDialog(),
                ["old_camera_flash_burn"] = () => new OldCameraFlashBurnDialog(),
                ["rainy_window"] = () => new RainyWindowDialog(),
                ["frosted_glass_ice_edges"] = () => new FrostedGlassIceEdgesDialog(),
                ["snowfall_depth_fog"] = () => new SnowfallDepthFogDialog(),
                ["stained_glass"] = () => new StainedGlassDialog(),
                ["unsharp_mask"] = () => new UnsharpMaskDialog(),
                ["vintage_print_damage"] = () => new VintagePrintDamageDialog(),
                ["vignette"] = () => new VignetteDialog(),
                ["mosaic_polygon"] = () => new MosaicPolygonDialog(),
                ["pencil_sketch"] = () => new PencilSketchDialog(),
                ["pointillism"] = () => new PointillismDialog(),
                ["thermal_vision"] = () => new ThermalVisionDialog(),
                ["tilt_shift"] = () => new TiltShiftDialog(),
                ["watercolor_kuwahara"] = () => new WatercolorKuwaharaDialog(),
                ["zoom_blur"] = () => new ZoomBlurDialog(),

                // --- Drawings ---
                ["draw_background"] = () => new DrawBackgroundDialog(),
                ["draw_background_image"] = () => new DrawBackgroundImageDialog(),
                ["draw_checkerboard"] = () => new DrawCheckerboardDialog(),
                ["draw_image"] = () => new DrawImageDialog(),
                ["draw_particles"] = () => new DrawParticlesDialog(),
                ["draw_text"] = () => new DrawTextDialog(),

                // --- Transforms ---
                ["rounded_corners"] = () => new RoundedCornersDialog(),
                ["skew"] = () => new SkewDialog(),
                ["rotate_3d"] = () => new Rotate3DDialog(),
                ["rotate_3d_box"] = () => new Rotate3DBoxDialog(),
                ["flip"] = () => new FlipDialog(),
                ["scale"] = () => new ScaleDialog(),
                ["displacement_map"] = () => new DisplacementMapDialog(),
                ["perspective_warp"] = () => new PerspectiveWarpDialog(),
                ["pinch_bulge"] = () => new PinchBulgeDialog(),
                ["twirl"] = () => new TwirlDialog(),
                ["resize_image"] = () => new ResizeImageDialog(),
                ["resize_canvas"] = () => new ResizeCanvasDialog(),

                // --- Quality ---
                ["blur"] = () => new BlurDialog(),
                ["pixelate"] = () => new PixelateDialog(),
                ["sharpen"] = () => new SharpenDialog(),
            };

        /// <summary>
        /// Tries to create a new effect dialog instance for the given <paramref name="effectId"/>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> and a fresh dialog <see cref="UserControl"/> if the ID is registered;
        /// otherwise <see langword="false"/> and <see langword="null"/>.
        /// </returns>
        public static bool TryCreate(string effectId, out UserControl? dialog)
        {
            if (_factories.TryGetValue(effectId, out var factory))
            {
                dialog = factory();
                return true;
            }

            dialog = null;
            return false;
        }

        /// <summary>Returns all registered effect IDs (case-insensitive).</summary>
        public static IReadOnlyCollection<string> RegisteredIds => _factories.Keys;
    }
}
