using artstudio.Data;
using artstudio.Data.Models;
using artstudio.Services;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection.Emit;
using System.Windows.Input;
using static System.Net.WebRequestMethods;
using ObservableObject = CommunityToolkit.Mvvm.ComponentModel.ObservableObject;

namespace artstudio.ViewModels
{
    public partial class PromptGeneratorViewModel : ObservableObject
    {
        private PromptGenerator? _generator;
        private readonly WordPromptService _wordPromptService;
        private readonly DatabaseService _databaseService;
        private readonly IToastService _toastService;
        private readonly ILogger<ImagePromptViewModel> _logger;
        private string _generatedPrompt = string.Empty;
        private int _nounCount = 1;
        private int _settingCount = 1;
        private int _styleCount = 1;
        private int _themeCount = 1;
        private bool _isInitialized = false;
        private string _statusMessage = "Initializing...";
        private Dictionary<string, List<string>> _lastGeneratedComponents = [];

        // Favorites flyout properties
        private bool _isFavoritesVisible = false;
        private bool _isLoadingFavorites = false;

        public PromptGeneratorViewModel(WordPromptService wordPromptService, DatabaseService databaseService, IToastService toastService, ILogger<ImagePromptViewModel> logger)
        {
            _logger = logger;
            _logger.LogDebug("PromptGeneratorViewModel Constructor Started!");

            _wordPromptService = wordPromptService;
            _databaseService = databaseService;
            _toastService = toastService;

            // Initialize collections  
            Nouns = [];
            Settings = [];
            Styles = [];
            Themes = [];
            FavoriteCollections = [];
            FavoriteGroups = [];

            // Initialize commands  
            GenerateCommand = new RelayCommand(GeneratePrompt);
            ClearCommand = new RelayCommand(ClearPrompt);
            ExportCommand = new AsyncRelayCommand(ExportToClipboardAsync);
            FavoriteCommand = new AsyncRelayCommand(SaveToFavoritesAsync);
            RefreshFavoritesCommand = new AsyncRelayCommand(LoadFavoritesAsync);
            RemoveFavoriteCommand = new AsyncRelayCommand<WordCollection>(RemoveFavoriteAsync);
            LoadCollectionCommand = new AsyncRelayCommand<WordCollection>(LoadCollectionAsync);

            // Favorites flyout commands
            ToggleFavoritesCommand = new AsyncRelayCommand(ToggleFavoritesAsync);
            CloseFavoritesCommand = new RelayCommand(CloseFavorites);
            LoadFavoriteCommand = new AsyncRelayCommand<WordCollection>(LoadFavoriteAsync);
            RemoveFavoriteFromFlyoutCommand = new AsyncRelayCommand<WordCollection>(RemoveFavoriteFromFlyoutAsync);

            // Initialize the prompt generator asynchronously  
            _ = InitializeGeneratorAsync();

            _logger.LogDebug("PromptGeneratorViewModel Constructor Completed!");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _databaseService?.Dispose();
            }
        }

        #region Properties

        public string GeneratedPrompt
        {
            get => _generatedPrompt;
            set => SetProperty(ref _generatedPrompt, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public int NounCount
        {
            get => _nounCount;
            set => SetProperty(ref _nounCount, value);
        }

        public int SettingCount
        {
            get => _settingCount;
            set => SetProperty(ref _settingCount, value);
        }

        public int StyleCount
        {
            get => _styleCount;
            set => SetProperty(ref _styleCount, value);
        }

        public int ThemeCount
        {
            get => _themeCount;
            set => SetProperty(ref _themeCount, value);
        }

        // Favorites flyout properties
        public bool IsFavoritesVisible
        {
            get => _isFavoritesVisible;
            set => SetProperty(ref _isFavoritesVisible, value);
        }

        public bool IsLoadingFavorites
        {
            get => _isLoadingFavorites;
            set => SetProperty(ref _isLoadingFavorites, value);
        }

        public bool HasNoFavorites => FavoriteGroups.Count == 0 && !IsLoadingFavorites;

        // Collections
        public ObservableCollection<string> Nouns { get; }
        public ObservableCollection<string> Settings { get; }
        public ObservableCollection<string> Styles { get; }
        public ObservableCollection<string> Themes { get; }
        public ObservableCollection<WordCollection> FavoriteCollections { get; }
        public ObservableCollection<PromptCollectionGroup> FavoriteGroups { get; }

        // Commands
        public ICommand GenerateCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand FavoriteCommand { get; }
        public ICommand RefreshFavoritesCommand { get; }
        public ICommand RemoveFavoriteCommand { get; }
        public ICommand LoadCollectionCommand { get; }

        // Favorites flyout commands
        public ICommand ToggleFavoritesCommand { get; }
        public ICommand CloseFavoritesCommand { get; }
        public ICommand LoadFavoriteCommand { get; }
        public ICommand RemoveFavoriteFromFlyoutCommand { get; }

        #endregion

        #region Favorites Flyout Methods

        private async Task ToggleFavoritesAsync()
        {
            try
            {
                if (!IsFavoritesVisible)
                {
                    // Opening favorites - load them
                    await LoadFavoriteGroupsAsync();
                }

                IsFavoritesVisible = !IsFavoritesVisible;
                _logger.LogDebug("Favorites panel toggled: {IsFavoritesVisible}", IsFavoritesVisible);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error toggling favorites");
                await _toastService.ShowToastAsync("Error loading favorites");
            }
        }

        private void CloseFavorites()
        {
            IsFavoritesVisible = false;
            _logger.LogDebug("Favorites panel closed");
        }

        private async Task LoadFavoriteGroupsAsync()
        {
            try
            {
                IsLoadingFavorites = true;
                OnPropertyChanged(nameof(HasNoFavorites));

                // Use the new method that populates words
                var favorites = await _wordPromptService.GetFavoritesWithWordsAsync();

                // Group by collection name (or "Default" if no specific collection)
                var grouped = favorites
                    .GroupBy(f => GetCollectionGroupName(f.Title))
                    .Select(g => new PromptCollectionGroup(g.Key, g))
                    .OrderBy(g => g.CollectionName == "Default" ? "ZZZ" : g.CollectionName) // Put Default at the end
                    .ToList();

                FavoriteGroups.Clear();
                foreach (var group in grouped)
                {
                    FavoriteGroups.Add(group);
                }

                _logger.LogDebug("Loaded {FavoriteCount} favorite prompts in {GroupCount} groups with words populated", favorites.Count, grouped.Count);

                // Log some details about what was loaded
                foreach (var favorite in favorites)
                {
                    _logger.LogDebug("Favorite '{FavoriteTitle}': {WordCount} words", favorite.Title, favorite.WordsList?.Count ?? 0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading favorite groups");
                await _toastService.ShowToastAsync("Error loading favorites");
            }
            finally
            {
                IsLoadingFavorites = false;
                OnPropertyChanged(nameof(HasNoFavorites));
            }
        }

        private async Task LoadFavoriteAsync(WordCollection? collection)
        {
            try
            {
                if (collection == null) return;

                // Load the collection
                await LoadCollectionAsync(collection);

                // Close the favorites panel
                IsFavoritesVisible = false;

                await _toastService.ShowToastAsync($"Loaded: {collection.DisplayTitle} ✓");
                _logger.LogDebug("Loaded favorite collection: {CollectionTitle}", collection.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading favorite");
                await _toastService.ShowToastAsync("Error loading favorite");
            }
        }

        private async Task RemoveFavoriteFromFlyoutAsync(WordCollection? collection)
        {
            try
            {
                if (collection == null) return;

                // Toggle favorite status in database first
                await _wordPromptService.ToggleFavoriteAsync(collection.Id);

                // Remove from UI collections immediately for instant feedback
                var groupName = GetCollectionGroupName(collection.Title);
                var group = FavoriteGroups.FirstOrDefault(g => g.CollectionName == groupName);
                if (group != null)
                {
                    // Remove from both the group's Prompts collection and the group itself
                    group.Prompts.Remove(collection);
                    group.Remove(collection); // Remove from the ObservableCollection base

                    // Remove empty groups
                    if (group.Prompts.Count == 0)
                    {
                        FavoriteGroups.Remove(group);
                    }
                }

                // Also remove from the main FavoriteCollections if it exists there
                var existingItem = FavoriteCollections.FirstOrDefault(f => f.Id == collection.Id);
                if (existingItem != null)
                {
                    FavoriteCollections.Remove(existingItem);
                }

                OnPropertyChanged(nameof(HasNoFavorites));
                await _toastService.ShowToastAsync("Removed from favorites ✓");
                _logger.LogDebug("Removed favorite: {CollectionTitle}", collection.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing favorite");
                await _toastService.ShowToastAsync("Error removing favorite");

                // If there's an error, reload to ensure UI is in sync with database
                _ = Task.Run(async () => await LoadFavoriteGroupsAsync());
            }
        }

        private static string GetCollectionGroupName(string? title)
        {
            if (string.IsNullOrEmpty(title))
                return "Default";

            // Extract collection name from title
            // For titles like "Dark - Prompt 1", "Dark - Prompt 2", extract "Dark"
            // For titles like "Favorite - MMM dd, HH:mm", use "Default"

            if (title.StartsWith("Favorite -"))
                return "Default";

            var parts = title.Split(" - ");
            if (parts.Length >= 2)
                return parts[0].Trim();

            return "Default";
        }

        #endregion

        private async Task InitializeGeneratorAsync()
        {
            try
            {
                _logger.LogDebug("Initializing PromptGenerator...");

                string dataPath = Path.Combine(FileSystem.AppDataDirectory, "prompt_data");
                _logger.LogDebug("Data path: {DataPath}", dataPath);
                _logger.LogDebug("Data path exists: {DataPathExists}", Directory.Exists(dataPath));

                // Wait for the initialization to complete if needed
                int retryCount = 0;
                const int maxRetries = 20;

                while (!Directory.Exists(dataPath) && retryCount < maxRetries)
                {
                    _logger.LogDebug("Waiting for data directory to be created... Retry {RetryAttempt}/{MaxRetries}", retryCount + 1, maxRetries);
                    await Task.Delay(500); // Wait 500ms
                    retryCount++;
                }

                if (Directory.Exists(dataPath))
                {
                    await LogDirectoryContentsAsync(dataPath);

                    // Create the generator
                    _generator = new PromptGenerator(dataPath);

                    await TestGeneratorAsync();

                    _isInitialized = true;
                    StatusMessage = "Ready";
                    _logger.LogDebug("PromptGenerator initialization completed successfully");
                }
                else
                {
                    _logger.LogDebug("Data directory still does not exist after {MaxRetries} retries - creating defaults", maxRetries);

                    // Create the generator- use defaults
                    _generator = new PromptGenerator(dataPath);
                    _isInitialized = true;
                    StatusMessage = "Ready (using defaults)";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing PromptGenerator.");
                StatusMessage = "Error during initialization";

                // Try to create with defaults anyway
                try
                {
                    string dataPath = Path.Combine(FileSystem.AppDataDirectory, "prompt_data");
                    _generator = new PromptGenerator(dataPath);
                    _isInitialized = true;
                    StatusMessage = "Ready (fallback mode)";
                }
                catch (Exception ex2)
                {
                    _logger.LogError(ex2, "Failed to create fallback generator");
                    StatusMessage = "Failed to initialize";
                }
            }
        }

        private async Task LogDirectoryContentsAsync(string dataPath)
        {
            try
            {
                await Task.Run(() =>
                {
                    var dirs = Directory.GetDirectories(dataPath);
                    _logger.LogDebug("Found {DirectoryCount} subdirectories:", dirs.Length);
                    foreach (var dir in dirs)
                    {
                        _logger.LogDebug("  - {DirectoryName}", Path.GetFileName(dir));
                        var files = Directory.GetFiles(dir, "*.json");
                        _logger.LogDebug("    Files: {FileCount}", files.Length);
                        foreach (var file in files)
                        {
                            var fileInfo = new FileInfo(file);
                            _logger.LogDebug("      - {FileName} ({FileSize} bytes)", Path.GetFileName(file), fileInfo.Length);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging directory contents");
            }
        }

        private void GeneratePrompt()
        {
            try
            {
                if (!_isInitialized || _generator == null)
                {
                    _logger.LogDebug("Generator not initialized yet");
                    GeneratedPrompt = "Generator is still initializing, please wait...";
                    return;
                }

                _logger.LogDebug("Generating prompt with counts - Nouns: {NounCount}, Settings: {SettingCount}, Styles: {StyleCount}, Themes: {ThemeCount}", NounCount, SettingCount, StyleCount, ThemeCount);

                var (prompt, components) = _generator.GenerateCustomPrompt(
                    nounCount: NounCount,
                    settingCount: SettingCount,
                    styleCount: StyleCount,
                    themeCount: ThemeCount
                );

                GeneratedPrompt = prompt;
                _lastGeneratedComponents = components;

                // Update the component collections using TryGetValue
                UpdateCollection(Nouns,
                    components.TryGetValue("nouns", out var nouns) ? nouns : []);
                UpdateCollection(Settings,
                    components.TryGetValue("settings", out var settings) ? settings : []);
                UpdateCollection(Styles,
                    components.TryGetValue("styles", out var styles) ? styles : []);
                UpdateCollection(Themes,
                    components.TryGetValue("themes", out var themes) ? themes : []);

                _logger.LogDebug("Prompt generated successfully: {GeneratedPrompt}", prompt);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error generating prompt");
                GeneratedPrompt = "Error generating prompt. Please try again.";
            }
        }

        private async Task TestGeneratorAsync()
        {
            try
            {
                if (_generator == null) return;

                _logger.LogDebug("=== Testing Generator ===");

                await Task.Run(() =>
                {
                    var categories = _generator.GetAvailableCategories();
                    _logger.LogDebug("Available categories: {Categories}", string.Join(", ", categories));

                    foreach (var category in categories)
                    {
                        int count = _generator.GetCategoryItemsCount(category);
                        _logger.LogDebug("Category '{Category}': {Count} items", category, count);

                        if (count > 0)
                        {
                            var sample = _generator.GetRandomItems(category, count: Math.Min(3, count));
                            _logger.LogDebug("  Sample items: {SampleItems}", string.Join(", ", sample));
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing generator");
            }
        }

        private static void UpdateCollection(ObservableCollection<string> collection, List<string> newItems)
        {
            collection.Clear();
            foreach (var item in newItems)
            {
                collection.Add(item);
            }
        }

        private void ClearPrompt()
        {
            GeneratedPrompt = string.Empty;
            Nouns.Clear();
            Settings.Clear();
            Styles.Clear();
            Themes.Clear();
            _lastGeneratedComponents.Clear();

            _logger.LogDebug("Prompt cleared");
        }

        private async Task ExportToClipboardAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(GeneratedPrompt))
                {
                    await Clipboard.SetTextAsync(GeneratedPrompt);
                    await _toastService.ShowToastAsync("Prompt copied to clipboard! ✓");
                    _logger.LogDebug("Prompt copied to clipboard successfully");
                }
                else
                {
                    await _toastService.ShowToastAsync("No prompt to copy!");
                    _logger.LogDebug("Attempted to copy empty prompt");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying prompt to clipboard");
                await _toastService.ShowToastAsync("Error copying to clipboard");
            }
        }

        private async Task SaveToFavoritesAsync()
        {
            try
            {
                if (_lastGeneratedComponents.Count == 0)
                {
                    await _toastService.ShowToastAsync("No prompt to save!");
                    return;
                }

                var mainPage = Application.Current?.Windows.Count > 0 ? Application.Current.Windows[0].Page : null;
                if (mainPage != null)
                {
                    // Give user options for saving favorites
                    string action = await mainPage.DisplayActionSheet(
                        "Save to Favorites",
                        "Cancel",
                        null,
                        "Save to Default Favorites",
                        "Save to Collection"
                    );

                    if (action == "Save to Default Favorites")
                    {
                        await SaveToDefaultFavoritesAsync();
                    }
                    else if (action == "Save to Collection")
                    {
                        await SaveToCollectionAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error in SaveToFavoritesAsync");
                await _toastService.ShowToastAsync("Error saving to favorites");
            }
        }

        private async Task SaveToDefaultFavoritesAsync()
        {
            try
            {
                string title = GeneratedPrompt; // Use the actual prompt as title
                if (string.IsNullOrEmpty(title))
                    title = $"Favorite - {DateTime.Now:MMM dd, HH:mm}";

                var savedCollection = await _wordPromptService.SaveWordCollectionWithCategoriesAsync(
                    _lastGeneratedComponents,
                    title: title,
                    promptType: "generated");

                // Mark as favorite
                await _wordPromptService.ToggleFavoriteAsync(savedCollection.Id);

                await _toastService.ShowToastAsync("Saved to favorites! ⭐");
                _logger.LogDebug("Prompt saved to default favorites with title: {Title}", title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving to default favorites");
                await _toastService.ShowToastAsync("Error saving to favorites");
            }
        }

        private async Task SaveToCollectionAsync()
        {
            try
            {
                var mainPage = Application.Current?.Windows.Count > 0 ? Application.Current.Windows[0].Page : null;
                if (mainPage != null)
                {
                    // Get existing collection names for suggestion
                    var existingCollections = await _wordPromptService.GetFavoritesAsync();
                    var collectionNames = new List<string>();
                    var seenNames = new HashSet<string>();

                    for (int i = 0; i < existingCollections.Count; i++)
                    {
                        var name = GetCollectionNameFromTitle(existingCollections[i].Title);
                        if (!string.IsNullOrEmpty(name) && name != "Default" && seenNames.Add(name))
                        {
                            collectionNames.Add(name);
                        }
                    }
                    collectionNames.Sort();

                    string promptText = "Enter a collection name:";
                    if (collectionNames.Count > 0)
                    {
                        promptText += $"\n\nExisting collections: {string.Join(", ", collectionNames)}";
                    }

                    // Get collection name from user
                    string collectionName = await mainPage.DisplayPromptAsync(
                        "Collection Name",
                        promptText,
                        "Save",
                        "Cancel",
                        placeholder: "e.g., Dark, Fantasy, Sci-Fi"
                    );

                    if (!string.IsNullOrWhiteSpace(collectionName))
                    {
                        var cleanName = collectionName.Trim();

                        // Create title that indicates this belongs to a collection
                        // Format: "CollectionName - Prompt {count}"
                        int existingInCollection = 0;
                        for (int i = 0; i < existingCollections.Count; i++)
                        {
                            if (GetCollectionNameFromTitle(existingCollections[i].Title) == cleanName)
                            {
                                existingInCollection++;
                            }
                        }

                        var title = $"{cleanName} - {GeneratedPrompt}";
                        if (title.Length > 100) // Limit title length
                            title = string.Concat(title.AsSpan(0, 97), "...");

                        var savedCollection = await _wordPromptService.SaveWordCollectionWithCategoriesAsync(
                            _lastGeneratedComponents,
                            title: title,
                            promptType: "custom");

                        // Mark as favorite
                        await _wordPromptService.ToggleFavoriteAsync(savedCollection.Id);

                        await _toastService.ShowToastAsync($"Saved to '{cleanName}' collection ⭐");
                        _logger.LogDebug("Prompt saved to collection: {CollectionName} with title: {Title}", cleanName, title);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving to collection");
                await _toastService.ShowToastAsync("Error saving to collection");
            }
        }

        private static string GetCollectionNameFromTitle(string? title)
        {
            if (string.IsNullOrEmpty(title))
                return "Default";

            // For titles like "Dark - Prompt 1", "Fantasy - Prompt 2", extract the collection name
            if (title.StartsWith("Favorite -"))
                return "Default";

            var parts = title.Split(" - ");
            if (parts.Length >= 2)
                return parts[0].Trim();

            return "Default";
        }

        private async Task LoadFavoritesAsync()
        {
            try
            {
                var favorites = await _wordPromptService.GetFavoritesAsync();

                FavoriteCollections.Clear();
                foreach (var favorite in favorites)
                {
                    FavoriteCollections.Add(favorite);
                }

                _logger.LogDebug("Loaded {FavoriteCount} favorite collections", favorites.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading favorites");
            }
        }

        private async Task RemoveFavoriteAsync(WordCollection? collection)
        {
            try
            {
                if (collection == null) return;

                await _wordPromptService.ToggleFavoriteAsync(collection.Id);
                await LoadFavoritesAsync();

                await _toastService.ShowToastAsync("Removed from favorites ✓");
                _logger.LogDebug("Removed collection {CollectionId} from favorites", collection.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing favorite");
                await _toastService.ShowToastAsync("Error removing from favorites");
            }
        }

        private async Task LoadCollectionAsync(WordCollection? collection)
        {
            try
            {
                if (collection == null) return;

                var categorizedWords = await _wordPromptService.GetWordsByCategoryAsync(collection);

                UpdateCollection(Nouns,
                    categorizedWords.TryGetValue("nouns", out var nouns) ? nouns : []);
                UpdateCollection(Settings,
                    categorizedWords.TryGetValue("settings", out var settings) ? settings : []);
                UpdateCollection(Styles,
                    categorizedWords.TryGetValue("styles", out var styles) ? styles : []);
                UpdateCollection(Themes,
                    categorizedWords.TryGetValue("themes", out var themes) ? themes : []);

                var allWords = new List<string>();
                allWords.AddRange(categorizedWords.Values.SelectMany(words => words));
                GeneratedPrompt = string.Join(", ", allWords);

                _lastGeneratedComponents = categorizedWords;

                await _toastService.ShowToastAsync($"Loaded: {collection.Title ?? "Untitled"} ✓");
                _logger.LogDebug("Loaded collection: {CollectionTitle}", collection.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading collection");
                await _toastService.ShowToastAsync("Error loading collection");
            }
        }
    }

    // Helper classes for grouping
    public partial class PromptCollectionGroup(string collectionName, IEnumerable<WordCollection> prompts)
        : ObservableCollection<WordCollection>(prompts)
    {
        public string CollectionName { get; } = collectionName;
        public ObservableCollection<WordCollection> Prompts { get; } = [];
    }
}