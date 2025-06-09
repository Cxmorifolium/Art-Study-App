using artstudio.Models;
using artstudio.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using artstudio.ViewModels;

namespace artstudio.ViewModels
{
    public class PaletteViewModel : INotifyPropertyChanged
    {
        #region Fields

        private readonly IFileSaveService _fileSaveService;
        private readonly Export _exportService;

        // DB
        private readonly PaletteService _paletteService;
        private readonly IToastService _toastService;

        private bool _isFavoritePalette;
        private PaletteModel _paletteModel;

        // Favorites Panel Fields
        private bool _isFavoritesPanelOpen = false;
        private FavTabType _currentFavTab = FavTabType.Swatches;
        private GridLength _favoritesPanelWidth = new GridLength(0);

        #endregion

        #region Enums

        public enum FavTabType
        {
            Swatches,
            Palettes
        }

        #endregion

        #region Properties

        public ObservableCollection<Swatch> Swatches { get; set; }

        public bool IsFavoritePalette
        {
            get => _isFavoritePalette;
            set
            {
                if (_isFavoritePalette != value)
                {
                    _isFavoritePalette = value;
                    OnPropertyChanged();
                }
            }
        }

        // Favorites Panel Properties
        public bool IsFavoritesPanelOpen
        {
            get => _isFavoritesPanelOpen;
            set
            {
                if (_isFavoritesPanelOpen != value)
                {
                    _isFavoritesPanelOpen = value;
                    OnPropertyChanged();
                    UpdatePanelWidth();
                }
            }
        }

        public GridLength FavoritesPanelWidth
        {
            get => _favoritesPanelWidth;
            set => SetProperty(ref _favoritesPanelWidth, value);
        }

        public FavTabType CurrentFavTab
        {
            get => _currentFavTab;
            set
            {
                if (_currentFavTab != value)
                {
                    _currentFavTab = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsSwatchesFavTabVisible));
                    OnPropertyChanged(nameof(IsPalettesFavTabVisible));
                    UpdateFavTabColors();
                }
            }
        }

        // Tab visibility
        public bool IsSwatchesFavTabVisible => CurrentFavTab == FavTabType.Swatches;
        public bool IsPalettesFavTabVisible => CurrentFavTab == FavTabType.Palettes;

        // Tab colors
        public Color SwatchesTabBackgroundColor => CurrentFavTab == FavTabType.Swatches ? Colors.MediumPurple : Colors.Transparent;
        public Color PalettesTabBackgroundColor => CurrentFavTab == FavTabType.Palettes ? Colors.MediumPurple : Colors.Transparent;
        public Color SwatchesTabTextColor => CurrentFavTab == FavTabType.Swatches ? Colors.White : Colors.Black;
        public Color PalettesTabTextColor => CurrentFavTab == FavTabType.Palettes ? Colors.White : Colors.Black;
        public ObservableCollection<FavoriteSwatchItem> FavoriteSwatches { get; set; }
        public ObservableCollection<PaletteCollectionGroup> GroupedFavoritePalettes { get; set; }

        // Empty states - Updated to use GroupedFavoritePalettes
        public bool HasNoFavoriteSwatches => !FavoriteSwatches.Any();
        public bool HasNoFavoritePalettes => !GroupedFavoritePalettes.Any();

        #endregion

        #region Commands

        public ICommand RegenerateCommand { get; }
        public ICommand ExportPaletteCommand { get; }
        public ICommand ToggleFavoritePaletteCommand { get; }
        public ICommand SortColorsCommand { get; private set; }

        // DB related commands
        public ICommand SavePaletteToFavoritesCommand { get; }

        // Favorites Panel Commands
        public ICommand ToggleFavoritesPanelCommand { get; }
        public ICommand CloseFavoritesPanelCommand { get; }
        public ICommand SelectSwatchesFavTabCommand { get; }
        public ICommand SelectPalettesFavTabCommand { get; }
        public ICommand LoadSwatchColorCommand { get; }
        public ICommand LoadFavoritePaletteCommand { get; }
        public ICommand RemoveFavoriteSwatchCommand { get; }
        public ICommand RemoveFavoritePaletteCommand { get; }

        #endregion

        #region Constructor

        public PaletteViewModel(IFileSaveService fileSaveService, Export exportService,
            PaletteService paletteService,
            IToastService toastService)
        {
            _fileSaveService = fileSaveService ?? throw new ArgumentNullException(nameof(fileSaveService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _paletteService = paletteService ?? throw new ArgumentNullException(nameof(paletteService));
            _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
            _paletteModel = new PaletteModel();

            // Initialize swatches default if none
            Swatches = new ObservableCollection<Swatch>
            {
                new Swatch(Colors.LightSalmon),
                new Swatch(Colors.SkyBlue),
                new Swatch(Colors.MediumSeaGreen),
                new Swatch(Colors.Goldenrod),
                new Swatch(Colors.MediumOrchid)
            };

            // Inject services into each swatch
            foreach (var swatch in Swatches)
            {
                swatch.SetServices(_paletteService, _toastService);
            }

            // Initialize favorites collections
            FavoriteSwatches = new ObservableCollection<FavoriteSwatchItem>();
            GroupedFavoritePalettes = new ObservableCollection<PaletteCollectionGroup>();

            // Subscribe to swatch property changes for live updates
            foreach (var swatch in Swatches)
            {
                swatch.PropertyChanged += OnSwatchPropertyChanged;
            }

            // Initialize commands
            RegenerateCommand = new Command(GeneratePalette);
            ToggleFavoritePaletteCommand = new Command(() => IsFavoritePalette = !IsFavoritePalette);
            ExportPaletteCommand = new Command(ExecuteExportPalette);
            SortColorsCommand = new Command<ColorSortingMethod>(SortColors);

            // Database commands
            SavePaletteToFavoritesCommand = new AsyncRelayCommand(SavePaletteToFavoritesAsync);

            // Favorites panel commands
            ToggleFavoritesPanelCommand = new Command(ToggleFavoritesPanel);
            CloseFavoritesPanelCommand = new Command(CloseFavoritesPanel);
            SelectSwatchesFavTabCommand = new Command(() => CurrentFavTab = FavTabType.Swatches);
            SelectPalettesFavTabCommand = new Command(() => CurrentFavTab = FavTabType.Palettes);
            LoadSwatchColorCommand = new Command<string>(LoadSwatchColor);
            LoadFavoritePaletteCommand = new AsyncRelayCommand<FavoritePaletteItem>(LoadFavoritePaletteAsync);
            RemoveFavoriteSwatchCommand = new AsyncRelayCommand<FavoriteSwatchItem>(RemoveFavoriteSwatchAsync);
            RemoveFavoritePaletteCommand = new AsyncRelayCommand<FavoritePaletteItem>(RemoveFavoritePaletteAsync);

            // Prevent null event handlers
            PropertyChanged += (sender, args) => { };

            GeneratePalette();

            // Load favorites data
            _ = LoadFavoritesDataAsync();
            _ = RefreshAllFavoriteStatesAsync();
        }

        private void ExecuteExportPalette()
        {
            _ = ExportPaletteAsync();
        }

        private void OnSwatchPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Swatch.Color) || e.PropertyName == nameof(Swatch.IsFavoriteColor))
            {
                // Trigger refresh of favorites panel if it's open
                if (IsFavoritesPanelOpen)
                {
                    _ = LoadFavoritesDataAsync();
                }
            }
        }

        #endregion

        #region Palette Generation Methods

        private void GeneratePalette()
        {
            var random = new Random();
            var harmonyTypes = Enum.GetValues(typeof(PaletteModel.ColorHarmonyType)).Cast<PaletteModel.ColorHarmonyType>().ToArray();
            var randomHarmony = harmonyTypes[random.Next(harmonyTypes.Length)];

            // Collect current palette colors and locked states
            var existingColors = Swatches.Select(s => s.Color).ToList();
            var lockedFlags = Swatches.Select(s => s.IsLocked).ToArray();

            List<Color> newPalette;

            // Check if we have any locked colors to respect
            if (lockedFlags.Any(locked => locked))
            {
                // Generate new palette respecting locks
                newPalette = _paletteModel.HarmonyPaletteGenerator(
                    randomHarmony,
                    randomFactor: 0.6f, // Increased for more variation
                    existingPalette: existingColors,
                    lockedColors: lockedFlags);

                System.Diagnostics.Debug.WriteLine($"Generated palette with locks using {randomHarmony} harmony");
            }
            else
            {
                // Generate completely new palette
                newPalette = _paletteModel.HarmonyPaletteGenerator(
                    randomHarmony,
                    randomFactor: 0.6f);

                System.Diagnostics.Debug.WriteLine($"Generated new palette using {randomHarmony} harmony");
            }

            // Update swatches based on lock status and deletion state
            for (int i = 0; i < Swatches.Count && i < newPalette.Count; i++)
            {
                var swatch = Swatches[i];

                // Handle deleted swatches - restore them
                if (swatch.IsDeleted)
                {
                    swatch.Color = swatch.PreviousColor;
                    swatch.IsDeleted = false;
                    swatch.IsActive = false;
                }

                // Only update color if swatch is not locked
                if (!swatch.IsLocked)
                {
                    var oldColor = swatch.Color;
                    swatch.Color = newPalette[i];

                    System.Diagnostics.Debug.WriteLine($"Swatch {i}: {(swatch.IsLocked ? "LOCKED" : "Updated")} - " +
                        $"Old: {oldColor.ToHex()}, New: {newPalette[i].ToHex()}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Swatch {i}: LOCKED - Keeping: {swatch.Color.ToHex()}");
                }

                // Ensure services are injected (in case of new swatches)
                swatch.SetServices(_paletteService, _toastService);

                // Notify UI of changes
                swatch.OnPropertyChanged(nameof(Swatch.Color));
                swatch.OnPropertyChanged(nameof(Swatch.ButtonVisible));
                swatch.OnPropertyChanged(nameof(Swatch.DeleteButtonVisible));
            }

            // Apply Coolors-style sorting to unlocked colors only
            ApplyCoolorsStyleSorting();

            // Refresh favorite states for all swatches after color generation
            _ = RefreshAllFavoriteStatesAsync();

            System.Diagnostics.Debug.WriteLine("Palette generation completed");
        }

        private void ApplyCoolorsStyleSorting()
        {
            // Get all active, unlocked swatches
            var unlockedSwatches = Swatches
                .Where(s => !s.IsDeleted && !s.IsLocked)
                .ToList();

            if (unlockedSwatches.Count <= 1) return;

            // Extract colors from unlocked swatches
            var unlockedColors = unlockedSwatches.Select(s => s.Color).ToList();

            // Apply Coolors-style sorting
            var sortedColors = unlockedColors.SortCoolorsStyle();

            // Apply sorted colors back to unlocked swatches
            for (int i = 0; i < unlockedSwatches.Count && i < sortedColors.Count; i++)
            {
                var oldColor = unlockedSwatches[i].Color;
                unlockedSwatches[i].Color = sortedColors[i];
                unlockedSwatches[i].OnPropertyChanged(nameof(Swatch.Color));

                System.Diagnostics.Debug.WriteLine($"Sorted unlocked swatch: {oldColor.ToHex()} → {sortedColors[i].ToHex()}");
            }
        }

        private async Task RefreshAllFavoriteStatesAsync()
        {
            try
            {
                var tasks = Swatches.Select(swatch => swatch.RefreshFavoriteStatusAsync());
                await Task.WhenAll(tasks);

                System.Diagnostics.Debug.WriteLine("Refreshed favorite states for all swatches");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing favorite states: {ex.Message}");
            }
        }

        private void SortColors(ColorSortingMethod method)
        {
            if (Swatches.Count <= 1) return;

            var activeSwatches = Swatches.Where(s => !s.IsDeleted && !s.IsLocked).ToList();
            if (activeSwatches.Count <= 1) return;

            var colors = activeSwatches.Select(s => s.Color).ToList();

            List<Color> sortedColors = method switch
            {
                ColorSortingMethod.Hue => colors.SortByHue(),
                ColorSortingMethod.Brightness => colors.SortByBrightness(),
                ColorSortingMethod.Saturation => colors.SortBySaturation(),
                ColorSortingMethod.CoolorsStyle => colors.SortCoolorsStyle(),
                ColorSortingMethod.SmoothTransitions => colors.SortWithSmoothTransitions(),
                ColorSortingMethod.Professional => colors.SortProfessional(),
                _ => colors
            };

            // Apply sorted colors back to unlocked swatches
            for (int i = 0; i < activeSwatches.Count && i < sortedColors.Count; i++)
            {
                activeSwatches[i].Color = sortedColors[i];
                activeSwatches[i].OnPropertyChanged(nameof(Swatch.Color));
            }
        }

        #endregion

        #region Export Methods

        private async Task ExportPaletteAsync()
        {
            var activeColors = Swatches
                .Where(s => !s.IsDeleted)
                .Select(s => s.Color)
                .ToList();

            if (activeColors.Count == 0)
            {
                var mainPage = Application.Current?.Windows.FirstOrDefault()?.Page;
                if (mainPage != null)
                {
                    await mainPage.DisplayAlert("Export", "No swatches to export.", "OK");
                }
                return;
            }

            var currentPage = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (currentPage == null) return;

            string action = await currentPage.DisplayActionSheet(
                "Choose export method",
                "Cancel",
                null,
                "Share Palette",
                "Save to File"
            );

            if (action == "Share Palette")
            {
                await SharePaletteAsync(activeColors);
            }
            else if (action == "Save to File")
            {
                var imageBytes = await Export.GeneratePaletteImageAsync(activeColors);
                var fileName = $"palette_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                await _fileSaveService.SaveFileAsync(imageBytes, fileName);
            }
        }

        private async Task SharePaletteAsync(List<Color> activeColors)
        {
            var imageBytes = await Export.GeneratePaletteImageAsync(activeColors);
            var fileName = $"palette_{DateTime.Now:yyyyMMdd_HHmmss}.png";

            var tempFilePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllBytesAsync(tempFilePath, imageBytes);

            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "Share Palette",
                File = new ShareFile(tempFilePath)
            });
        }

        #endregion

        #region Database Methods

        private async Task SavePaletteToFavoritesAsync()
        {
            try
            {
                var activeColors = GetActiveColors();
                if (!activeColors.Any())
                {
                    await _toastService.ShowToastAsync("No colors to save!");
                    return;
                }

                var mainPage = Application.Current?.Windows.FirstOrDefault()?.Page;
                if (mainPage != null)
                {
                    // Give user options for saving favorites
                    string action = await mainPage.DisplayActionSheet(
                        "Save Palette to Favorites",
                        "Cancel",
                        null,
                        "Save to Default Favorites",
                        "Save to Collection"
                    );

                    if (action == "Save to Default Favorites")
                    {
                        await SaveToDefaultPaletteFavoritesAsync(activeColors);
                    }
                    else if (action == "Save to Collection")
                    {
                        await SaveToCollectionAsync(activeColors);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SavePaletteToFavoritesAsync: {ex.Message}");
                await _toastService.ShowToastAsync("Error saving to favorites");
            }
        }

        private async Task SaveToDefaultPaletteFavoritesAsync(List<Color> colors)
        {
            try
            {
                string title = $"Palette - {DateTime.Now:MMM dd, HH:mm}";

                var savedCollection = await _paletteService.SavePaletteCollectionAsync(
                    colors,
                    title: title,
                    paletteType: "generated"
                );

                // Mark as favorite
                await _paletteService.TogglePaletteFavoriteAsync(savedCollection.Id);

                await _toastService.ShowToastAsync("Palette saved to favorites! ⭐");
                System.Diagnostics.Debug.WriteLine($"Palette saved to default favorites with title: {title}");

                // Refresh favorites data
                _ = LoadFavoritesDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving to default favorites: {ex.Message}");
                await _toastService.ShowToastAsync("Error saving to favorites");
            }
        }

        private async Task SaveToCollectionAsync(List<Color> colors)
        {
            try
            {
                var mainPage = Application.Current?.Windows.FirstOrDefault()?.Page;
                if (mainPage != null)
                {
                    // Get existing collection names for suggestion
                    var existingCollections = await _paletteService.GetFavoritePalettesAsync();
                    var collectionNames = existingCollections
                        .Select(c => GetCollectionNameFromTitle(c.Title))
                        .Where(name => !string.IsNullOrEmpty(name) && name != "Default")
                        .Distinct()
                        .OrderBy(name => name)
                        .ToList();

                    string promptText = "Enter a collection name:";
                    if (collectionNames.Any())
                    {
                        promptText += $"\n\nExisting collections: {string.Join(", ", collectionNames)}";
                    }

                    // Get collection name from user
                    string collectionName = await mainPage.DisplayPromptAsync(
                        "Collection Name",
                        promptText,
                        "Save",
                        "Cancel",
                        placeholder: "e.g., Sunset, Ocean, Autumn"
                    );

                    if (!string.IsNullOrWhiteSpace(collectionName))
                    {
                        var cleanName = collectionName.Trim();

                        // Create title that indicates this belongs to a collection
                        var existingInCollection = existingCollections
                            .Where(c => GetCollectionNameFromTitle(c.Title) == cleanName)
                            .Count();

                        var title = $"{cleanName} - Palette {existingInCollection + 1}";

                        var savedCollection = await _paletteService.SavePaletteCollectionAsync(
                            colors,
                            title: title,
                            paletteType: "custom"
                        );

                        // Mark as favorite
                        await _paletteService.TogglePaletteFavoriteAsync(savedCollection.Id);

                        await _toastService.ShowToastAsync($"Saved to '{cleanName}' collection ⭐");
                        System.Diagnostics.Debug.WriteLine($"Palette saved to collection: {cleanName} with title: {title}");

                        // Refresh favorites data
                        _ = LoadFavoritesDataAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving to collection: {ex.Message}");
                await _toastService.ShowToastAsync("Error saving to collection");
            }
        }

        private List<Color> GetActiveColors()
        {
            return Swatches
                .Where(s => !s.IsDeleted)
                .Select(s => s.Color)
                .ToList();
        }

        private string GetCollectionNameFromTitle(string? title)
        {
            if (string.IsNullOrEmpty(title))
                return "Default";

            // Handle "Palette - MMM dd, HH:mm" format -> "Default"
            if (title.StartsWith("Palette -"))
                return "Default";

            // Handle "CollectionName - Palette X" format -> "CollectionName"
            var parts = title.Split(" - ");
            if (parts.Length >= 2)
            {
                var collectionName = parts[0].Trim();

                // Don't return empty collection names
                if (!string.IsNullOrWhiteSpace(collectionName))
                    return collectionName;
            }

            return "Default";
        }

        #endregion

        #region Favorites Panel Methods

        private void ToggleFavoritesPanel()
        {
            IsFavoritesPanelOpen = !IsFavoritesPanelOpen;
        }

        private void CloseFavoritesPanel()
        {
            IsFavoritesPanelOpen = false;
        }

        private void UpdatePanelWidth()
        {
            // Set width based on panel state
            FavoritesPanelWidth = IsFavoritesPanelOpen ? new GridLength(300) : new GridLength(0);
        }

        private void UpdateFavTabColors()
        {
            OnPropertyChanged(nameof(SwatchesTabBackgroundColor));
            OnPropertyChanged(nameof(PalettesTabBackgroundColor));
            OnPropertyChanged(nameof(SwatchesTabTextColor));
            OnPropertyChanged(nameof(PalettesTabTextColor));
        }

        private async Task LoadFavoritesDataAsync()
        {
            try
            {
                // Load favorite swatches (unchanged)
                FavoriteSwatches.Clear();
                var favoriteSwatches = await _paletteService.GetFavoriteSwatchesAsync();
                foreach (var swatch in favoriteSwatches)
                {
                    FavoriteSwatches.Add(new FavoriteSwatchItem
                    {
                        HexColor = swatch.HexColor,
                        CollectionName = swatch.Collection,
                        CreatedAt = swatch.CreatedAt
                    });
                }

                // Load and group favorite palettes
                await LoadGroupedFavoritePalettesAsync();

                OnPropertyChanged(nameof(HasNoFavoriteSwatches));
                OnPropertyChanged(nameof(HasNoFavoritePalettes));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading favorites data: {ex.Message}");
            }
        }

        private async Task LoadGroupedFavoritePalettesAsync()
        {
            try
            {
                GroupedFavoritePalettes.Clear();
                var favoritePalettes = await _paletteService.GetFavoritePalettesAsync();

                Debug.WriteLine($"=== Loading {favoritePalettes.Count} favorite palettes ===");

                // Group palettes by collection name
                var grouped = favoritePalettes
                    .GroupBy(p => GetCollectionNameFromTitle(p.Title))
                    .OrderBy(g => g.Key == "Default" ? "ZZZ" : g.Key) // Put Default collection at the end
                    .ToList();

                Debug.WriteLine($"=== Found {grouped.Count} groups ===");

                foreach (var group in grouped)
                {
                    var paletteItems = group.Select(p => new FavoritePaletteItem
                    {
                        Id = p.Id, // Make sure to include the ID for database operations
                        Title = p.Title ?? "Untitled",
                        Colors = p.ColorsList?.Select(c => c.ToHex()).ToList() ?? new List<string>(),
                        CreatedAt = p.CreatedAt
                    }).OrderByDescending(p => p.CreatedAt).ToList(); // Sort by newest first within group

                    Debug.WriteLine($"=== Group '{group.Key}' has {paletteItems.Count} palettes ===");
                    foreach (var item in paletteItems)
                    {
                        Debug.WriteLine($"  - {item.Title} ({item.Colors.Count} colors) - {item.CreatedAt}");
                    }

                    var collectionGroup = new PaletteCollectionGroup(group.Key, paletteItems);
                    GroupedFavoritePalettes.Add(collectionGroup);
                }

                Debug.WriteLine($"=== Final GroupedFavoritePalettes count: {GroupedFavoritePalettes.Count} ===");
                OnPropertyChanged(nameof(GroupedFavoritePalettes));
                OnPropertyChanged(nameof(HasNoFavoritePalettes));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading grouped favorite palettes: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void LoadSwatchColor(string hexColor)
        {
            try
            {
                if (string.IsNullOrEmpty(hexColor)) return;

                // Apply this color to an unlocked swatch in the current palette
                var unlockedSwatch = Swatches.FirstOrDefault(s => !s.IsLocked && !s.IsDeleted);
                if (unlockedSwatch != null)
                {
                    var color = Color.FromArgb(hexColor);
                    unlockedSwatch.Color = color;
                    _ = _toastService.ShowToastAsync($"Applied color {hexColor}");
                }
                else
                {
                    _ = _toastService.ShowToastAsync("No available swatch to apply color to");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading swatch color: {ex.Message}");
                _ = _toastService.ShowToastAsync("Error applying color");
            }
        }

        private async Task LoadFavoritePaletteAsync(FavoritePaletteItem? palette)
        {
            try
            {
                if (palette == null) return;

                System.Diagnostics.Debug.WriteLine($"Loading favorite palette: {palette.Title}");

                // Load the palette colors into current swatches
                for (int i = 0; i < Math.Min(palette.Colors.Count, Swatches.Count); i++)
                {
                    if (!Swatches[i].IsLocked)
                    {
                        var color = Color.FromArgb(palette.Colors[i]);
                        Swatches[i].Color = color;
                        Swatches[i].OnPropertyChanged(nameof(Swatch.Color));
                        System.Diagnostics.Debug.WriteLine($"Applied color {palette.Colors[i]} to swatch {i}");
                    }
                }

                await _toastService.ShowToastAsync($"Loaded palette: {palette.Title}");

                // Close the panel after loading
                CloseFavoritesPanel();

                // Refresh favorite states after loading
                _ = RefreshAllFavoriteStatesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading favorite palette: {ex.Message}");
                await _toastService.ShowToastAsync("Error loading palette");
            }
        }

        private async Task RemoveFavoriteSwatchAsync(FavoriteSwatchItem? swatchItem)
        {
            try
            {
                // Add debug output to confirm method is being called
                System.Diagnostics.Debug.WriteLine($"=== RemoveFavoriteSwatchAsync called for {swatchItem?.HexColor} ===");

                if (swatchItem == null)
                {
                    System.Diagnostics.Debug.WriteLine("SwatchItem is null!");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Attempting to remove swatch: {swatchItem.HexColor}");

                // Remove from database first
                var favoriteSwatches = await _paletteService.GetFavoriteSwatchesAsync();
                var dbSwatch = favoriteSwatches.FirstOrDefault(s => s.HexColor == swatchItem.HexColor);

                if (dbSwatch != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Found swatch in database with ID: {dbSwatch.Id}");

                    await _paletteService.RemoveSwatchFromFavoritesAsync(dbSwatch.Id);
                    System.Diagnostics.Debug.WriteLine("Database removal completed");

                    // Remove from UI immediately for instant feedback
                    var itemToRemove = FavoriteSwatches.FirstOrDefault(s => s.HexColor == swatchItem.HexColor);
                    if (itemToRemove != null)
                    {
                        FavoriteSwatches.Remove(itemToRemove);
                        System.Diagnostics.Debug.WriteLine($"Removed from FavoriteSwatches collection. Count is now: {FavoriteSwatches.Count}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Item not found in FavoriteSwatches collection!");
                    }

                    // Force refresh of UI properties
                    OnPropertyChanged(nameof(FavoriteSwatches));
                    OnPropertyChanged(nameof(HasNoFavoriteSwatches));

                    // Show success message
                    await _toastService.ShowToastAsync("Color removed from favorites ✓");

                    System.Diagnostics.Debug.WriteLine($"=== Successfully removed swatch {swatchItem.HexColor} ===");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Swatch {swatchItem.HexColor} not found in database!");
                    await _toastService.ShowToastAsync("Color not found in favorites");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in RemoveFavoriteSwatchAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                await _toastService.ShowToastAsync("Error removing color");

                // If there's an error, reload to ensure UI is in sync with database
                _ = Task.Run(async () => await LoadFavoritesDataAsync());
            }
        }

        private async Task RemoveFavoritePaletteAsync(FavoritePaletteItem? paletteItem)
        {
            try
            {
                if (paletteItem == null) return;

                var mainPage = Application.Current?.Windows.FirstOrDefault()?.Page;
                if (mainPage != null)
                {
                    bool confirm = await mainPage.DisplayAlert(
                        "Remove Palette",
                        $"Remove '{paletteItem.Title}' from favorites?",
                        "Remove",
                        "Cancel");

                    if (confirm)
                    {
                        // Use the ID if available, otherwise fall back to title/date matching
                        if (paletteItem.Id > 0)
                        {
                            await _paletteService.DeletePaletteCollectionAsync(paletteItem.Id);
                        }
                        else
                        {
                            // Fallback method
                            var favoritePalettes = await _paletteService.GetFavoritePalettesAsync();
                            var dbPalette = favoritePalettes.FirstOrDefault(p =>
                                p.Title == paletteItem.Title && p.CreatedAt == paletteItem.CreatedAt);

                            if (dbPalette != null)
                            {
                                await _paletteService.DeletePaletteCollectionAsync(dbPalette.Id);
                            }
                        }

                        // Fix: Remove from UI groups properly
                        foreach (var group in GroupedFavoritePalettes.ToList())
                        {
                            var itemToRemove = group.Palettes.FirstOrDefault(p =>
                                p.Title == paletteItem.Title && p.CreatedAt == paletteItem.CreatedAt);

                            if (itemToRemove != null)
                            {
                                group.Remove(itemToRemove); // Use the Remove method we added to PaletteCollectionGroup

                                // Remove empty groups
                                if (group.Count == 0)
                                {
                                    GroupedFavoritePalettes.Remove(group);
                                }
                                break;
                            }
                        }

                        OnPropertyChanged(nameof(HasNoFavoritePalettes));
                        await _toastService.ShowToastAsync("Palette removed from favorites ✓");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing favorite palette: {ex.Message}");
                await _toastService.ShowToastAsync("Error removing palette");
            }
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;

            // Ensure propertyName is not null before calling OnPropertyChanged
            if (propertyName != null)
            {
                OnPropertyChanged(propertyName);
            }

            return true;
        }

        #endregion
    }
}