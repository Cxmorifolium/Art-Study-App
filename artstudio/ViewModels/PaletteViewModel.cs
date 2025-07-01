using artstudio.Models;
using artstudio.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace artstudio.ViewModels
{
    public class PaletteViewModel : INotifyPropertyChanged
    {
        #region Fields

        private readonly IFileSaveService _fileSaveService;
        private readonly Export _exportService;
        private readonly PaletteService _paletteService;
        private readonly IToastService _toastService;
        private readonly ILogger<PaletteViewModel> _logger;

        private bool _isFavoritePalette;
        private PaletteModel _paletteModel;

        // Favorites Panel Fields
        private bool _isFavoritesPanelOpen = false;
        private FavTabType _currentFavTab = FavTabType.Swatches;
        private GridLength _favoritesPanelWidth = new GridLength(0);

        // Swatch sorting - Use the enum from PaletteService
        private SwatchSortMethod _defaultSwatchSort = SwatchSortMethod.HueGradient;

        #endregion

        #region Enums

        public enum FavTabType
        {
            Swatches,
            Palettes
        }

        #endregion

        #region Properties

        public ObservableCollection<SwatchViewModel> Swatches { get; set; }

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

        // Collections
        public ObservableCollection<FavoriteSwatchItem> FavoriteSwatches { get; set; }
        public ObservableCollection<PaletteCollectionGroup> GroupedFavoritePalettes { get; set; }

        // Empty states
        public bool HasNoFavoriteSwatches => !FavoriteSwatches.Any();
        public bool HasNoFavoritePalettes => !GroupedFavoritePalettes.Any();

        // Swatch sorting property - Use the service enum
        public SwatchSortMethod DefaultSwatchSort
        {
            get => _defaultSwatchSort;
            private set
            {
                _defaultSwatchSort = value;
                OnPropertyChanged();
            }
        }

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
        public ICommand LoadFavoritePaletteCommand { get; }
        public ICommand RemoveFavoriteSwatchCommand { get; }
        public ICommand RemoveFavoritePaletteCommand { get; }

        // Swatch Sorting Commands
        public ICommand ChangeSortToHueCommand { get; }
        public ICommand ChangeSortToDateCommand { get; }
        public ICommand ChangeSortToBrightnessCommand { get; }

        #endregion

        #region Constructor

        public PaletteViewModel(
            IFileSaveService fileSaveService,
            Export exportService,
            PaletteService paletteService,
            IToastService toastService,
            ILoggerFactory loggerFactory)
        {
            _fileSaveService = fileSaveService ?? throw new ArgumentNullException(nameof(fileSaveService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _paletteService = paletteService ?? throw new ArgumentNullException(nameof(paletteService));
            _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
            _logger = loggerFactory?.CreateLogger<PaletteViewModel>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            _paletteModel = new PaletteModel();

            // Initialize swatches default if none
            var swatchModels = new[]
            {
                new SwatchModel(Colors.LightSalmon),
                new SwatchModel(Colors.SkyBlue),
                new SwatchModel(Colors.MediumSeaGreen),
                new SwatchModel(Colors.Goldenrod),
                new SwatchModel(Colors.MediumOrchid)
            };

            Swatches = new ObservableCollection<SwatchViewModel>();
            SwatchViewModel.FavoriteChanged += OnSwatchFavoriteChanged;

            foreach (var model in swatchModels)
            {
                var swatchLogger = loggerFactory.CreateLogger<SwatchViewModel>();
                var swatchViewModel = new SwatchViewModel(model, _paletteService, _toastService, swatchLogger);
                Swatches.Add(swatchViewModel);
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
            LoadFavoritePaletteCommand = new AsyncRelayCommand<FavoritePaletteItem>(LoadFavoritePaletteAsync);
            RemoveFavoriteSwatchCommand = new AsyncRelayCommand<FavoriteSwatchItem>(RemoveFavoriteSwatchAsync);
            RemoveFavoritePaletteCommand = new AsyncRelayCommand<FavoritePaletteItem>(RemoveFavoritePaletteAsync);

            // Swatch sorting commands - Use the correct enum
            ChangeSortToHueCommand = new Command(() => _ = ChangeSortMethodAsync(SwatchSortMethod.HueGradient));
            ChangeSortToDateCommand = new Command(() => _ = ChangeSortMethodAsync(SwatchSortMethod.DateNewest));
            ChangeSortToBrightnessCommand = new Command(() => _ = ChangeSortMethodAsync(SwatchSortMethod.Brightness));

            // Prevent null event handlers
            PropertyChanged += (sender, args) => { };

            GeneratePalette();

            // Load favorites data
            _ = LoadFavoritesDataAsync();
        }

        private void ExecuteExportPalette()
        {
            _ = ExportPaletteAsync();
        }

        private void OnSwatchPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            _logger.LogDebug("Swatch property changed: {PropertyName}", e.PropertyName);
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
                    randomFactor: 0.6f,
                    existingPalette: existingColors,
                    lockedColors: lockedFlags);

                _logger.LogInformation("Generated palette with locks using {HarmonyType} harmony", randomHarmony);
            }
            else
            {
                // Generate completely new palette
                newPalette = _paletteModel.HarmonyPaletteGenerator(
                    randomHarmony,
                    randomFactor: 0.6f);

                _logger.LogInformation("Generated new palette using {HarmonyType} harmony", randomHarmony);
            }

            // Update swatches based on lock status and deletion state
            for (int i = 0; i < Swatches.Count && i < newPalette.Count; i++)
            {
                var swatch = Swatches[i];

                if (!swatch.IsLocked)
                {
                    // Handle deleted swatches - restore them first
                    if (swatch.IsDeleted)
                    {
                        swatch.ToggleDeleteCommand.Execute(null); // This will restore the deleted swatch
                    }

                    // Make sure swatch is inactive (buttons hidden) - only toggle if currently active
                    if (swatch.IsActive)
                    {
                        swatch.ToggleActivateCommand.Execute(null); // This will make IsActive = false
                    }

                    // Update with new color
                    swatch.Color = newPalette[i];

                    _logger.LogDebug("Swatch {Index}: Reset to default state with new color {Color}", i, newPalette[i].ToHex());
                }
                else
                {
                    // Locked swatches keep their current state
                    _logger.LogDebug("Swatch {Index}: LOCKED - Keeping current state and color {Color}", i, swatch.Color.ToHex());
                }
            }

            // Apply Coolors-style sorting to unlocked colors only
            ApplyCoolorsStyleSorting();

            _logger.LogInformation("Palette generation completed");
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

                _logger.LogDebug("Sorted unlocked swatch: {OldColor} → {NewColor}", oldColor.ToHex(), sortedColors[i].ToHex());
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
                _logger.LogError(ex, "Error in SavePaletteToFavoritesAsync");
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
                await LoadFavoritesDataAsync();
                _logger.LogInformation("Palette saved to default favorites with title: {Title}", title);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving to default favorites");
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
                        _logger.LogInformation("Palette saved to collection: {CollectionName} with title: {Title}", cleanName, title);
                        await LoadFavoritesDataAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving to collection");
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

        // Load favorites using the service sorting method
        private async Task LoadFavoritesDataAsync()
        {
            try
            {
                // Load favorite swatches using the service's sorting method
                FavoriteSwatches.Clear();
                var favoriteSwatches = await _paletteService.GetSwatchesSortedAsync(DefaultSwatchSort);

                foreach (var swatch in favoriteSwatches)
                {
                    FavoriteSwatches.Add(new FavoriteSwatchItem
                    {
                        HexColor = swatch.HexColor,
                        CreatedAt = swatch.CreatedAt
                    });
                }

                // Load and group favorite palettes
                await LoadGroupedFavoritePalettesAsync();

                OnPropertyChanged(nameof(HasNoFavoriteSwatches));
                OnPropertyChanged(nameof(HasNoFavoritePalettes));

                _logger.LogInformation("Loaded and sorted {SwatchCount} favorite swatches using {SortMethod}",
                    FavoriteSwatches.Count, DefaultSwatchSort);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading favorites data");
            }
        }

        private async Task LoadGroupedFavoritePalettesAsync()
        {
            try
            {
                GroupedFavoritePalettes.Clear();
                var favoritePalettes = await _paletteService.GetFavoritePalettesAsync();

                _logger.LogDebug("Loading {PaletteCount} favorite palettes", favoritePalettes.Count);

                // Group palettes by collection name
                var grouped = favoritePalettes
                    .GroupBy(p => GetCollectionNameFromTitle(p.Title))
                    .OrderBy(g => g.Key == "Default" ? "ZZZ" : g.Key) // Put Default collection at the end
                    .ToList();

                _logger.LogDebug("Found {GroupCount} groups", grouped.Count);

                foreach (var group in grouped)
                {
                    var paletteItems = group.Select(p => new FavoritePaletteItem
                    {
                        Id = p.Id, // ID for database operations
                        Title = p.Title ?? "Untitled",
                        Colors = p.ColorsList?.Select(c => c.ToHex()).ToList() ?? new List<string>(),
                        CreatedAt = p.CreatedAt
                    }).OrderByDescending(p => p.CreatedAt).ToList(); // Sort by newest first within group

                    _logger.LogDebug("Group '{GroupName}' has {PaletteCount} palettes", group.Key, paletteItems.Count);

                    var collectionGroup = new PaletteCollectionGroup(group.Key, paletteItems);
                    GroupedFavoritePalettes.Add(collectionGroup);
                }

                _logger.LogDebug("Final GroupedFavoritePalettes count: {GroupCount}", GroupedFavoritePalettes.Count);
                OnPropertyChanged(nameof(GroupedFavoritePalettes));
                OnPropertyChanged(nameof(HasNoFavoritePalettes));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading grouped favorite palettes");
            }
        }

        private async Task LoadFavoritePaletteAsync(FavoritePaletteItem? palette)
        {
            try
            {
                if (palette == null) return;

                _logger.LogInformation("Loading favorite palette: {PaletteTitle}", palette.Title);

                // Load the palette colors into current swatches
                for (int i = 0; i < Math.Min(palette.Colors.Count, Swatches.Count); i++)
                {
                    if (!Swatches[i].IsLocked)
                    {
                        var color = Color.FromArgb(palette.Colors[i]);
                        Swatches[i].Color = color;
                        Swatches[i].OnPropertyChanged(nameof(SwatchViewModel.Color));
                        _logger.LogDebug("Applied color {Color} to swatch {Index}", palette.Colors[i], i);
                    }
                }

                await _toastService.ShowToastAsync($"Loaded palette: {palette.Title}");

                // Close the panel after loading
                CloseFavoritesPanel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading favorite palette: {PaletteTitle}", palette?.Title);
                await _toastService.ShowToastAsync("Error loading palette");
            }
        }

        private async Task RemoveFavoriteSwatchAsync(FavoriteSwatchItem? swatchItem)
        {
            try
            {
                if (swatchItem == null) return;

                // Remove from database first
                var favoriteSwatches = await _paletteService.GetFavoriteSwatchesAsync();
                var dbSwatch = favoriteSwatches.FirstOrDefault(s => s.HexColor == swatchItem.HexColor);

                if (dbSwatch != null)
                {
                    await _paletteService.RemoveSwatchFromFavoritesAsync(dbSwatch.Id);

                    FavoriteSwatches.Remove(swatchItem);

                    //Update main palette swatch immediately
                    var matchingSwatch = Swatches.FirstOrDefault(s =>
                        s.Color.ToHex().Equals(swatchItem.HexColor, StringComparison.OrdinalIgnoreCase));

                    if (matchingSwatch != null)
                    {
                        matchingSwatch.IsFavoriteColor = false;
                    }

                    OnPropertyChanged(nameof(HasNoFavoriteSwatches));
                    //await _toastService.ShowToastAsync("Color removed from favorites ✓");

                    _logger.LogInformation("Successfully removed swatch {HexColor}", swatchItem.HexColor);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing favorite swatch");
                await _toastService.ShowToastAsync("Error removing color");
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

                        foreach (var group in GroupedFavoritePalettes.ToList())
                        {
                            var itemToRemove = group.Palettes.FirstOrDefault(p =>
                                p.Title == paletteItem.Title && p.CreatedAt == paletteItem.CreatedAt);

                            if (itemToRemove != null)
                            {
                                group.Remove(itemToRemove);

                                // Remove empty groups
                                if (group.Count == 0)
                                {
                                    GroupedFavoritePalettes.Remove(group);
                                }
                                break;
                            }
                        }

                        OnPropertyChanged(nameof(HasNoFavoritePalettes));
                        //await _toastService.ShowToastAsync("Palette removed from favorites ✓");
                        _logger.LogInformation("Removed favorite palette: {PaletteTitle}", paletteItem.Title);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing favorite palette: {PaletteTitle}", paletteItem?.Title);
                await _toastService.ShowToastAsync("Error removing palette");
            }
        }

        private void OnSwatchFavoriteChanged(string hexColor, bool isFavorited)
        {
            try
            {
                if (isFavorited)
                {
                    // Add to favorites panel immediately
                    var existingItem = FavoriteSwatches.FirstOrDefault(f =>
                        f.HexColor.Equals(hexColor, StringComparison.OrdinalIgnoreCase));

                    if (existingItem == null)
                    {
                        FavoriteSwatches.Add(new FavoriteSwatchItem
                        {
                            HexColor = hexColor,
                            CreatedAt = DateTime.Now
                        });

                        OnPropertyChanged(nameof(HasNoFavoriteSwatches));
                        _logger.LogDebug("Added {HexColor} to favorites panel", hexColor);
                    }
                }
                else
                {
                    // Remove from favorites panel immediately
                    var itemToRemove = FavoriteSwatches.FirstOrDefault(f =>
                        f.HexColor.Equals(hexColor, StringComparison.OrdinalIgnoreCase));

                    if (itemToRemove != null)
                    {
                        FavoriteSwatches.Remove(itemToRemove);
                        OnPropertyChanged(nameof(HasNoFavoriteSwatches));
                        _logger.LogDebug("Removed {HexColor} from favorites panel", hexColor);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling favorite change for {HexColor}", hexColor);
            }
        }

        public void Dispose()
        {
            SwatchViewModel.FavoriteChanged -= OnSwatchFavoriteChanged;
        }
        #endregion

        #region Swatch Sorting Methods

        private async Task ChangeSortMethodAsync(SwatchSortMethod newSort)
        {
            if (DefaultSwatchSort != newSort)
            {
                DefaultSwatchSort = newSort;

                if (IsFavoritesPanelOpen)
                {
                    await LoadFavoritesDataAsync();
                }

                _logger.LogInformation("Changed swatch sorting to: {SortMethod}", newSort);
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