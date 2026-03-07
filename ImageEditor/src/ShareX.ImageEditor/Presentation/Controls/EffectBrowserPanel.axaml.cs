using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareX.ImageEditor.Hosting;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ShareX.ImageEditor.Presentation.Controls
{
    public sealed class EffectDialogRequestedEventArgs : EventArgs
    {
        public string EffectId { get; }
        public EffectDialogRequestedEventArgs(string effectId) => EffectId = effectId;
    }

    public partial class EffectCategory : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        private readonly bool _showCount;
        private readonly bool _keepVisibleWhenEmpty;
        private readonly string? _headerHint;

        public ObservableCollection<EffectItem> AllEffects { get; } = new();

        [ObservableProperty]
        private ObservableCollection<EffectItem> _visibleEffects = new();

        [ObservableProperty]
        private bool _isVisible = true;

        public EffectCategory(string name, bool showCount = true, bool keepVisibleWhenEmpty = false, string? headerHint = null)
        {
            Name = name;
            _showCount = showCount;
            _keepVisibleWhenEmpty = keepVisibleWhenEmpty;
            _headerHint = headerHint;
        }

        public string HeaderText
        {
            get
            {
                string header = _showCount ? $"{Name} ({AllEffects.Count})" : Name;
                return string.IsNullOrWhiteSpace(_headerHint) ? header : $"{header} - {_headerHint}";
            }
        }

        partial void OnNameChanged(string value)
        {
            OnPropertyChanged(nameof(HeaderText));
        }

        public EffectItem AddEffect(string name, string icon, string description, Action execute, string? effectId = null, bool keepSorted = true)
        {
            var effect = new EffectItem(name, icon, description, execute, effectId);
            Insert(AllEffects, effect, keepSorted);
            Insert(VisibleEffects, effect, keepSorted);
            OnPropertyChanged(nameof(HeaderText));
            return effect;
        }

        public EffectItem AddEffectCopy(EffectItem effect, bool keepSorted = true)
            => AddEffect(effect.Name, effect.Icon, effect.Description, effect.ExecuteAction, effect.EffectId, keepSorted);

        public void ClearEffects()
        {
            AllEffects.Clear();
            VisibleEffects.Clear();
            OnPropertyChanged(nameof(HeaderText));
        }

        public bool RemoveEffectsById(string effectId)
        {
            bool removed = false;

            for (int i = AllEffects.Count - 1; i >= 0; i--)
            {
                if (string.Equals(AllEffects[i].EffectId, effectId, StringComparison.OrdinalIgnoreCase))
                {
                    AllEffects.RemoveAt(i);
                    removed = true;
                }
            }

            for (int i = VisibleEffects.Count - 1; i >= 0; i--)
            {
                if (string.Equals(VisibleEffects[i].EffectId, effectId, StringComparison.OrdinalIgnoreCase))
                {
                    VisibleEffects.RemoveAt(i);
                }
            }

            if (removed)
            {
                OnPropertyChanged(nameof(HeaderText));
            }

            return removed;
        }

        private static void Insert(ObservableCollection<EffectItem> target, EffectItem effect, bool keepSorted)
        {
            if (!keepSorted)
            {
                target.Add(effect);
                return;
            }

            int index = 0;
            while (index < target.Count &&
                   string.Compare(target[index].Name, effect.Name, StringComparison.OrdinalIgnoreCase) <= 0)
            {
                index++;
            }

            target.Insert(index, effect);
        }

        public void Filter(string searchText)
        {
            VisibleEffects.Clear();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                foreach (var effect in AllEffects) VisibleEffects.Add(effect);
            }
            else
            {
                foreach (var effect in AllEffects)
                {
                    if (effect.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    {
                        VisibleEffects.Add(effect);
                    }
                }
            }

            IsVisible = VisibleEffects.Count > 0 || (_keepVisibleWhenEmpty && string.IsNullOrWhiteSpace(searchText));
        }
    }

    public partial class EffectItem : ObservableObject
    {
        [ObservableProperty]
        private string _effectId;

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _icon;

        [ObservableProperty]
        private string _description;

        public Action ExecuteAction { get; }

        public EffectItem(string name, string icon, string description, Action executeAction, string? effectId = null)
        {
            EffectId = string.IsNullOrWhiteSpace(effectId) ? NormalizeEffectId(name) : effectId;
            Name = name;
            Icon = icon;
            Description = description;
            ExecuteAction = executeAction;
        }

        public static string NormalizeEffectId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(value.Length);
            bool lastWasUnderscore = false;

            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(char.ToLowerInvariant(c));
                    lastWasUnderscore = false;
                    continue;
                }

                if (!lastWasUnderscore)
                {
                    sb.Append('_');
                    lastWasUnderscore = true;
                }
            }

            while (sb.Length > 0 && sb[0] == '_')
            {
                sb.Remove(0, 1);
            }

            while (sb.Length > 0 && sb[^1] == '_')
            {
                sb.Length--;
            }

            return sb.ToString();
        }

        [RelayCommand]
        private void Execute()
        {
            ExecuteAction?.Invoke();
        }
    }

    public partial class EffectBrowserPanel : UserControl
    {
        private const string FavoritesHeaderHint = "Right click to favorite";

        private static readonly Dictionary<string, string> FavoriteAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["resize"] = "resize_image",
            ["canvas"] = "resize_canvas",
            ["crop"] = "crop_image",
            ["auto_crop"] = "auto_crop_image",
            ["rotate_90"] = "rotate_90_clockwise",
            ["rotate_90_cc"] = "rotate_90_counter_clockwise"
        };

        public event EventHandler<EffectDialogRequestedEventArgs>? EffectDialogRequested;

        public event EventHandler? InvertRequested;
        public event EventHandler? BlackAndWhiteRequested;
        public event EventHandler? PolaroidRequested;
        public event EventHandler? EdgeDetectRequested;
        public event EventHandler? EmbossRequested;
        public event EventHandler? MeanRemovalRequested;
        public event EventHandler? SmoothRequested;

        public event EventHandler? CropImageRequested;
        public event EventHandler? AutoCropImageRequested;
        public event EventHandler? Rotate90CWRequested;
        public event EventHandler? Rotate90CCWRequested;
        public event EventHandler? Rotate180Requested;
        public event EventHandler? RotateCustomAngleRequested;
        public event EventHandler? FlipHorizontalRequested;
        public event EventHandler? FlipVerticalRequested;

        public ObservableCollection<EffectCategory> Categories { get; } = new();

        private readonly EffectCategory _favoritesCategory = new("Favorites", keepVisibleWhenEmpty: true, headerHint: FavoritesHeaderHint);
        private readonly Dictionary<string, EffectItem> _allEffectsById = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _favoriteEffectIds = new(StringComparer.OrdinalIgnoreCase);
        private ImageEditorOptions? _options;

        public EffectBrowserPanel()
        {
            AvaloniaXamlLoader.Load(this);
            InitializeEffects();
            BuildEffectLookup();
            LoadFavoriteEffects(ImageEditorOptions.DefaultFavoriteEffects, persistToOptions: false);

            var categoriesControl = this.FindControl<ItemsControl>("CategoriesControl");
            if (categoriesControl != null)
            {
                categoriesControl.ItemsSource = Categories;
            }
        }

        public void SetOptions(ImageEditorOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            LoadFavoriteEffects(options.FavoriteEffects, persistToOptions: true);
        }

        private void OnSearchTextChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
        {
            ApplyCurrentFilter();
        }

        public void FocusSearchBox()
        {
            var searchBox = this.FindControl<TextBox>("SearchBox");
            if (searchBox != null)
            {
                // Clear existing search
                searchBox.Text = string.Empty;
                // Focus using Dispatcher to ensure it happens after layout/visibility logic
                System.Threading.Tasks.Task.Delay(50).ContinueWith(_ =>
                {
                    Dispatcher.UIThread.Post(() => searchBox.Focus(), DispatcherPriority.Input);
                });
            }
        }

        private void Raise(EventHandler? handler)
        {
            Dispatcher.UIThread.Post(() => handler?.Invoke(this, EventArgs.Empty));
        }

        private void RaiseDialog(string effectId)
        {
            var args = new EffectDialogRequestedEventArgs(effectId);
            Dispatcher.UIThread.Post(() => EffectDialogRequested?.Invoke(this, args));
        }

        private void OnEffectItemPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not EffectItem effect)
            {
                return;
            }

            var point = e.GetCurrentPoint(button);
            if (!point.Properties.IsRightButtonPressed)
            {
                return;
            }

            if (TryToggleFavorite(effect))
            {
                ApplyCurrentFilter();
                PersistFavoritesToOptions();
            }

            e.Handled = true;
        }

        private void BuildEffectLookup()
        {
            _allEffectsById.Clear();

            foreach (var category in Categories)
            {
                if (ReferenceEquals(category, _favoritesCategory))
                {
                    continue;
                }

                foreach (var effect in category.AllEffects)
                {
                    if (!string.IsNullOrWhiteSpace(effect.EffectId))
                    {
                        _allEffectsById[effect.EffectId] = effect;
                    }
                }
            }
        }

        private void LoadFavoriteEffects(IEnumerable<string>? favoriteEffectIds, bool persistToOptions)
        {
            _favoriteEffectIds.Clear();
            _favoritesCategory.ClearEffects();

            if (favoriteEffectIds != null)
            {
                foreach (string favoriteEffectId in favoriteEffectIds)
                {
                    if (TryResolveEffect(favoriteEffectId, out EffectItem effect))
                    {
                        TryAddFavorite(effect);
                    }
                }
            }

            ApplyCurrentFilter();

            if (persistToOptions)
            {
                PersistFavoritesToOptions();
            }
        }

        private bool TryResolveEffect(string value, out EffectItem effect)
        {
            effect = null!;

            string normalizedId = EffectItem.NormalizeEffectId(value);
            if (string.IsNullOrWhiteSpace(normalizedId))
            {
                return false;
            }

            if (FavoriteAliases.TryGetValue(normalizedId, out string? alias))
            {
                normalizedId = alias;
            }

            if (_allEffectsById.TryGetValue(normalizedId, out EffectItem? resolvedEffect) && resolvedEffect != null)
            {
                effect = resolvedEffect;
                return true;
            }

            return false;
        }

        private bool TryAddFavorite(EffectItem effect)
        {
            if (!_favoriteEffectIds.Add(effect.EffectId))
            {
                return false;
            }

            _favoritesCategory.AddEffectCopy(effect, keepSorted: false);
            return true;
        }

        private bool TryToggleFavorite(EffectItem effect)
        {
            if (_favoriteEffectIds.Contains(effect.EffectId))
            {
                return RemoveFavorite(effect.EffectId);
            }

            return TryAddFavorite(effect);
        }

        private bool RemoveFavorite(string effectId)
        {
            if (!_favoriteEffectIds.Remove(effectId))
            {
                return false;
            }

            _favoritesCategory.RemoveEffectsById(effectId);
            return true;
        }

        private void ApplyCurrentFilter()
        {
            var searchBox = this.FindControl<TextBox>("SearchBox");
            string searchText = searchBox?.Text ?? string.Empty;

            foreach (var category in Categories)
            {
                category.Filter(searchText);
            }
        }

        private void PersistFavoritesToOptions()
        {
            if (_options == null)
            {
                return;
            }

            _options.FavoriteEffects = _favoritesCategory.AllEffects
                .Select(effect => effect.EffectId)
                .Where(effectId => !string.IsNullOrWhiteSpace(effectId))
                .ToList();
        }

        private void InitializeEffects()
        {
            Categories.Add(_favoritesCategory);

            var manip = new EffectCategory("Manipulations");
            manip.AddEffect("3D Box / Extrude...", "\uf1b2", "Applies a 3D box or extrude effect.", () => RaiseDialog("rotate_3d_box"));
            manip.AddEffect("Auto crop image", "\uf0d0", "Automatically crops the image by finding its edges.", () => Raise(AutoCropImageRequested));
            manip.AddEffect("Crop image...", "\uf125", "Crops the image.", () => Raise(CropImageRequested));
            manip.AddEffect("Displacement map...", "\uf03e", "Applies a displacement map.", () => RaiseDialog("displacement_map"));
            manip.AddEffect("Flip...", "\uf021", "Flip the image.", () => RaiseDialog("flip"));
            manip.AddEffect("Flip horizontal", "\uf337", "Flips the image horizontally.", () => Raise(FlipHorizontalRequested));
            manip.AddEffect("Flip vertical", "\uf338", "Flips the image vertically.", () => Raise(FlipVerticalRequested));
            manip.AddEffect("Perspective warp...", "\uf061", "Warps the image perspective.", () => RaiseDialog("perspective_warp"));
            manip.AddEffect("Pinch / bulge...", "\uf00e", "Applies a pinch or bulge effect.", () => RaiseDialog("pinch_bulge"));
            manip.AddEffect("Resize canvas...", "\uf0c8", "Resizes the canvas.", () => RaiseDialog("resize_canvas"));
            manip.AddEffect("Resize image...", "\uf03e", "Resizes the image.", () => RaiseDialog("resize_image"));
            manip.AddEffect("Rotate...", "\uf1de", "Rotates the image by a custom angle.", () => Raise(RotateCustomAngleRequested));
            manip.AddEffect("Rotate 180°", "\uf021", "Rotates the image by 180 degrees.", () => Raise(Rotate180Requested));
            manip.AddEffect("Rotate 3D...", "\uf1b2", "Rotates the image in 3D space.", () => RaiseDialog("rotate_3d"));
            manip.AddEffect("Rotate 90° clockwise", "\uf01e", "Rotates the image 90 degrees clockwise.", () => Raise(Rotate90CWRequested));
            manip.AddEffect("Rotate 90° counter clockwise", "\uf0e2", "Rotates the image 90 degrees counter-clockwise.", () => Raise(Rotate90CCWRequested));
            manip.AddEffect("Rounded Corners...", "\uf0c8", "Rounds the corners of the image.", () => RaiseDialog("rounded_corners"));
            manip.AddEffect("Scale...", "\uf00e", "Scales the image.", () => RaiseDialog("scale"));
            manip.AddEffect("Skew...", "\uf068", "Skews the image.", () => RaiseDialog("skew"));
            manip.AddEffect("Twirl...", "\uf021", "Applies a twirl effect.", () => RaiseDialog("twirl"));
            Categories.Add(manip);

            var adj = new EffectCategory("Adjustments");
            adj.AddEffect("Alpha...", "\uf042", "Adjusts the alpha transparency.", () => RaiseDialog("alpha"));
            adj.AddEffect("Auto contrast...", "\uf042", "Automatically adjusts the contrast.", () => RaiseDialog("auto_contrast"));
            adj.AddEffect("Black & White", "\uf111", "Converts the image to black and white.", () => Raise(BlackAndWhiteRequested));
            adj.AddEffect("Brightness...", "\uf185", "Adjusts image brightness.", () => RaiseDialog("brightness"));
            adj.AddEffect("Color matrix...", "\uf00a", "Applies a color matrix transformation.", () => RaiseDialog("color_matrix"));
            adj.AddEffect("Colorize...", "\uf043", "Colorizes the image.", () => RaiseDialog("colorize"));
            adj.AddEffect("Contrast...", "\uf042", "Adjusts image contrast.", () => RaiseDialog("contrast"));
            adj.AddEffect("Duotone / Gradient map...", "\uf5fd", "Maps grayscale tones to a custom multi-color gradient.", () => RaiseDialog("duotone_gradient_map"));
            adj.AddEffect("Exposure...", "\uf185", "Adjusts the exposure level.", () => RaiseDialog("exposure"));
            adj.AddEffect("Film emulation...", "\uf008", "Applies cinematic analog film looks with grain and fade.", () => RaiseDialog("film_emulation"));
            adj.AddEffect("Gamma...", "\uf1de", "Adjusts the gamma level.", () => RaiseDialog("gamma"));
            adj.AddEffect("Grayscale...", "\uf03e", "Converts the image to grayscale.", () => RaiseDialog("grayscale"));
            adj.AddEffect("Hue...", "\uf043", "Adjusts the hue of the image.", () => RaiseDialog("hue"));
            adj.AddEffect("Invert", "\uf362", "Inverts image colors.", () => Raise(InvertRequested));
            adj.AddEffect("Levels...", "\uf1de", "Adjusts image color levels.", () => RaiseDialog("levels"));
            adj.AddEffect("Polaroid", "\uf03e", "Applies a Polaroid effect.", () => Raise(PolaroidRequested));
            adj.AddEffect("Posterize...", "\uf009", "Reduces the number of colors to create a poster-like effect.", () => RaiseDialog("posterize"));
            adj.AddEffect("Replace Color...", "\uf1fb", "Replaces a specific color.", () => RaiseDialog("replace_color"));
            adj.AddEffect("Saturation...", "\uf591", "Adjusts the color saturation.", () => RaiseDialog("saturation"));
            adj.AddEffect("Selective Color...", "\uf1de", "Adjusts selective color channels.", () => RaiseDialog("selective_color"));
            adj.AddEffect("Sepia...", "\uf0f4", "Applies a sepia tone effect.", () => RaiseDialog("sepia"));
            adj.AddEffect("Shadows / Highlights...", "\uf186", "Adjusts shadows and highlights.", () => RaiseDialog("shadows_highlights"));
            adj.AddEffect("Solarize...", "\uf185", "Applies a solarize effect.", () => RaiseDialog("solarize"));
            adj.AddEffect("Temperature / Tint...", "\uf1de", "Adjusts the color temperature and tint.", () => RaiseDialog("temperature_tint"));
            adj.AddEffect("Threshold...", "\uf042", "Applies a contrast threshold.", () => RaiseDialog("threshold"));
            adj.AddEffect("Vibrance...", "\uf0eb", "Adjusts the color vibrance.", () => RaiseDialog("vibrance"));
            Categories.Add(adj);

            var fil = new EffectCategory("Filters");
            fil.AddEffect("Add noise...", "\uf111", "Adds noise to the image.", () => RaiseDialog("add_noise"));
            fil.AddEffect("Anime speed lines...", "\uf0e7", "Overlays dramatic radial anime motion streaks from a focal point.", () => RaiseDialog("anime_speed_lines"), "anime_speed_lines");
            fil.AddEffect("ASCII art...", "\uf031", "Converts luminance to configurable ASCII glyph shading.", () => RaiseDialog("ascii_art"));
            fil.AddEffect("Block glitch / databending...", "\uf1c9", "Displaces rectangular chunks and misaligns color channels like corrupted files.", () => RaiseDialog("block_glitch"));
            fil.AddEffect("Bloom...", "\uf06e", "Creates a soft glow around bright regions.", () => RaiseDialog("bloom"));
            fil.AddEffect("Blueprint drawing...", "\uf1fc", "Converts image structure into cyan technical lines over blueprint paper.", () => RaiseDialog("blueprint_drawing"), "blueprint_drawing");
            fil.AddEffect("Blur...", "\uf1fc", "Applies a blur effect.", () => RaiseDialog("blur"));
            fil.AddEffect("Cartoon sticker cutout...", "\uf249", "Posterizes colors, inks edges and adds a white sticker-like border glow.", () => RaiseDialog("cartoon_sticker_cutout"), "cartoon_sticker_cutout");
            fil.AddEffect("Claymation texture...", "\uf1b2", "Builds chunky plasticine-like color lumps with tactile relief grain.", () => RaiseDialog("claymation_texture"), "claymation_texture");
            fil.AddEffect("Color depth...", "\uf009", "Changes the color depth of the image.", () => RaiseDialog("color_depth"));
            fil.AddEffect("Convolution matrix...", "\uf00a", "Applies a custom convolution matrix.", () => RaiseDialog("convolution_matrix"));
            fil.AddEffect("CRT...", "\uf26c", "Applies a retro CRT monitor effect.", () => RaiseDialog("crt"));
            fil.AddEffect("Crystal prism...", "\uf5db", "Creates refracted crystal facets with RGB dispersion.", () => RaiseDialog("crystal_prism"));
            fil.AddEffect("Crystalize shards...", "\uf5db", "Breaks the image into faceted crystal shards with edge glints.", () => RaiseDialog("crystalize_shards"), "crystalize_shards");
            fil.AddEffect("Crosshatch...", "\uf550", "Builds shading from layered hatch lines.", () => RaiseDialog("crosshatch"));
            fil.AddEffect("Dithering...", "\uf00a", "Reduces palette with Floyd-Steinberg or Bayer dot diffusion.", () => RaiseDialog("dithering"));
            fil.AddEffect("Edge detect", "\uf061", "Detects visible edges in the image.", () => Raise(EdgeDetectRequested));
            fil.AddEffect("Emboss", "\uf0c8", "Applies an emboss effect.", () => Raise(EmbossRequested));
            fil.AddEffect("Frosted glass + ice edges...", "\uf2dc", "Applies frosted refraction with cool icy edge highlights.", () => RaiseDialog("frosted_glass_ice_edges"), "frosted_glass_ice_edges");
            fil.AddEffect("Gaussian blur...", "\uf1fc", "Applies a Gaussian blur effect.", () => RaiseDialog("gaussian_blur"));
            fil.AddEffect("Glow...", "\uf0eb", "Applies a glowing effect.", () => RaiseDialog("glow"));
            fil.AddEffect("Halftone...", "\uf111", "Creates a comic-style CMYK dot print pattern.", () => RaiseDialog("halftone"));
            fil.AddEffect("Halation...", "\uf185", "Adds warm cinematic glow around intense highlights.", () => RaiseDialog("halation"));
            fil.AddEffect("Hologram scan...", "\uf233", "Adds scanlines, glitch offsets and cyan glow.", () => RaiseDialog("hologram_scan"));
            fil.AddEffect("Holographic foil shimmer...", "\uf06e", "Adds iridescent foil rainbow shimmer with specular sparkle.", () => RaiseDialog("holographic_foil_shimmer"), "holographic_foil_shimmer");
            fil.AddEffect("Ink splatter + drips...", "\uf5aa", "Adds expressive ink blotches with gravity drips and paper stain fade.", () => RaiseDialog("ink_splatter_drips"), "ink_splatter_drips");
            fil.AddEffect("Lens blur (bokeh)...", "\uf06e", "Simulates circular aperture blur with weighted highlight bloom.", () => RaiseDialog("lens_blur"), "lens_blur");
            fil.AddEffect("Liquid glass...", "\uf043", "Applies refractive liquid-like glass distortion and gloss.", () => RaiseDialog("liquid_glass"));
            fil.AddEffect("Matrix digital rain...", "\uf233", "Rebuilds the image with glowing falling terminal glyphs.", () => RaiseDialog("matrix_digital_rain"));
            fil.AddEffect("Mean removal", "\uf1de", "Removes the mean value from colors.", () => Raise(MeanRemovalRequested));
            fil.AddEffect("Median filter...", "\uf1de", "Applies a median filter for noise reduction.", () => RaiseDialog("median_filter"));
            fil.AddEffect("Mosaic polygon...", "\uf5ee", "Pixelates with tessellated hexagon or triangle polygons.", () => RaiseDialog("mosaic_polygon"));
            fil.AddEffect("Motion blur...", "\uf068", "Applies a motion blur effect.", () => RaiseDialog("motion_blur"));
            fil.AddEffect("Spin blur...", "\uf021", "Blurs pixels along an arc around a custom center point.", () => RaiseDialog("spin_blur"), "spin_blur");
            fil.AddEffect("Neon edge glow...", "\uf0e7", "Detects edges and renders a neon glow around them.", () => RaiseDialog("neon_edge_glow"));
            fil.AddEffect("Old camera flash burn...", "\uf0e7", "Simulates overexposed flash, warm frame burn and analog grain.", () => RaiseDialog("old_camera_flash_burn"), "old_camera_flash_burn");
            fil.AddEffect("Oil paint...", "\uf304", "Makes the image look like an oil painting.", () => RaiseDialog("oil_paint"));
            fil.AddEffect("Outline...", "\uf0c8", "Applies an outline effect.", () => RaiseDialog("outline"));
            fil.AddEffect("Pencil sketch...", "\uf040", "Simulates graphite sketch using dodge-blur and edge darkening.", () => RaiseDialog("pencil_sketch"));
            fil.AddEffect("Pixel sorting...", "\uf0dc", "Sorts horizontal or vertical pixel spans by brightness or hue for glitch cascades.", () => RaiseDialog("pixel_sorting"));
            fil.AddEffect("Pixelate...", "\uf1b3", "Pixelates the image.", () => RaiseDialog("pixelate"));
            fil.AddEffect("Pointillism...", "\uf111", "Re-renders the image as overlapping colored dots.", () => RaiseDialog("pointillism"));
            fil.AddEffect("Rainy window...", "\uf73d", "Simulates water streaks, droplets and foggy glass.", () => RaiseDialog("rainy_window"));
            fil.AddEffect("Reflection...", "\uf338", "Adds a reflection to the bottom of the image.", () => RaiseDialog("reflection"));
            fil.AddEffect("RGB split...", "\uf03e", "Splits the red, green, and blue color channels.", () => RaiseDialog("rgb_split"));
            fil.AddEffect("Shadow...", "\uf186", "Adds a drop shadow to the image.", () => RaiseDialog("shadow"));
            fil.AddEffect("Sharpen...", "\uf0eb", "Sharpens the image.", () => RaiseDialog("sharpen"));
            fil.AddEffect("Slice...", "\uf0c4", "Slices the image.", () => RaiseDialog("slice"));
            fil.AddEffect("Sobel edge...", "\uf061", "Applies a Sobel edge detection filter.", () => RaiseDialog("sobel_edge"));
            fil.AddEffect("Snowfall + depth fog...", "\uf2dc", "Overlays procedural snowfall layers with atmospheric distance fog.", () => RaiseDialog("snowfall_depth_fog"), "snowfall_depth_fog");
            fil.AddEffect("Smooth", "\uf1fc", "Applies a smoothing effect.", () => Raise(SmoothRequested));
            fil.AddEffect("Stained glass...", "\uf0db", "Turns the image into stained glass-style tiles.", () => RaiseDialog("stained_glass"));
            fil.AddEffect("Surface blur...", "\uf1fc", "Softens regions while preserving stronger local edges.", () => RaiseDialog("surface_blur"), "surface_blur");
            fil.AddEffect("Thermal vision...", "\uf06d", "Maps image intensity to an infrared heatmap gradient.", () => RaiseDialog("thermal_vision"));
            fil.AddEffect("Tilt-shift (miniature)...", "\uf030", "Adds selective blur and saturation for toy-like miniature scenes.", () => RaiseDialog("tilt_shift"));
            fil.AddEffect("Torn edge...", "\uf12d", "Adds a torn edge border effect.", () => RaiseDialog("torn_edge"));
            fil.AddEffect("Unsharp mask...", "\uf0eb", "Applies an unsharp mask filter.", () => RaiseDialog("unsharp_mask"));
            fil.AddEffect("Vintage print damage...", "\uf56d", "Adds film grain, scratches, dust and faded paper burn.", () => RaiseDialog("vintage_print_damage"));
            fil.AddEffect("Vignette...", "\uf111", "Applies a vignette effect.", () => RaiseDialog("vignette"));
            fil.AddEffect("Watercolor / Kuwahara...", "\uf53f", "Simplifies local color regions for watercolor-like strokes.", () => RaiseDialog("watercolor_kuwahara"));
            fil.AddEffect("Wave edge...", "\uf068", "Adds a wavy edge to the image.", () => RaiseDialog("wave_edge"));
            fil.AddEffect("Zoom blur...", "\uf00e", "Creates radial streak blur toward a chosen center point.", () => RaiseDialog("zoom_blur"), "zoom_blur");
            Categories.Add(fil);

            var drawings = new EffectCategory("Drawings");
            drawings.AddEffect("Background...", "\uf302", "Fills transparent regions using a solid color or gradient background.", () => RaiseDialog("draw_background"));
            drawings.AddEffect("Background image...", "\uf03e", "Draws an image behind the current canvas.", () => RaiseDialog("draw_background_image"));
            drawings.AddEffect("Checkerboard...", "\uf0db", "Draws a checkerboard background behind the image.", () => RaiseDialog("draw_checkerboard"));
            drawings.AddEffect("Border...", "\uf565", "Adds a border to the image.", () => RaiseDialog("border"));
            drawings.AddEffect("Image...", "\uf1c5", "Draws an image overlay with placement, sizing and opacity controls.", () => RaiseDialog("draw_image"));
            drawings.AddEffect("Particles...", "\uf06d", "Draws random particles from an image folder.", () => RaiseDialog("draw_particles"));
            drawings.AddEffect("Text...", "\uf031", "Draws stylized text with gradient, outline and shadow options.", () => RaiseDialog("draw_text"));
            Categories.Add(drawings);
        }
    }
}
