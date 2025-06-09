using artstudio.Services;
using artstudio.Data.Models;
using artstudio.Data;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Diagnostics;
using ObservableObject = CommunityToolkit.Mvvm.ComponentModel.ObservableObject;
using System.IO;

namespace artstudio.ViewModels
{
    public class PromptGeneratorViewModel : ObservableObject
    {
        private PromptGenerator? _generator;
        private readonly WordPromptService _wordPromptService;
        private readonly DatabaseService _databaseService;
        private readonly IToastService _toastService;
        private string _generatedPrompt = string.Empty;
        private int _nounCount = 1;
        private int _settingCount = 1;
        private int _styleCount = 1;
        private int _themeCount = 1;
        private bool _isInitialized = false;
        private string _statusMessage = "Initializing...";
        private Dictionary<string, List<string>> _lastGeneratedComponents = new();

        // Favorites flyout properties
        private bool _isFavoritesVisible = false;
        private bool _isLoadingFavorites = false;

        public PromptGeneratorViewModel(WordPromptService wordPromptService, DatabaseService databaseService, IToastService toastService)
        {
            DebugLog("=== PromptGeneratorViewModel Constructor Started ===");

            _wordPromptService = wordPromptService;
            _databaseService = databaseService;
            _toastService = toastService;

            // Initialize collections  
            Nouns = new ObservableCollection<string>();
            Settings = new ObservableCollection<string>();
            Styles = new ObservableCollection<string>();
            Themes = new ObservableCollection<string>();
            FavoriteCollections = new ObservableCollection<WordCollection>();
            FavoriteGroups = new ObservableCollection<PromptCollectionGroup>();

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

            DebugLog("=== PromptGeneratorViewModel Constructor Completed ===");
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

        public bool HasNoFavorites => !FavoriteGroups.Any() && !IsLoadingFavorites;

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
                DebugLog($"Favorites panel toggled: {IsFavoritesVisible}");
            }
            catch (Exception ex)
            {
                DebugLog($"Error toggling favorites: {ex.Message}");
                await _toastService.ShowToastAsync("Error loading favorites");
            }
        }

        private void CloseFavorites()
        {
            IsFavoritesVisible = false;
            DebugLog("Favorites panel closed");
        }

        private async Task LoadFavoriteGroupsAsync()
        {
            try
            {
                IsLoadingFavorites = true;
                OnPropertyChanged(nameof(HasNoFavorites));

                var favorites = await _wordPromptService.GetFavoritesAsync();

                // Group by collection name (or "Default" if no specific collection)
                var grouped = favorites
                    .GroupBy(f => GetCollectionGroupName(f.Title))
                    .Select(g => new PromptCollectionGroup(g.Key, g.ToList()))
                    .OrderBy(g => g.CollectionName == "Default" ? "ZZZ" : g.CollectionName) // Put Default at the end
                    .ToList();

                FavoriteGroups.Clear();
                foreach (var group in grouped)
                {
                    FavoriteGroups.Add(group);
                }

                DebugLog($"Loaded {favorites.Count} favorite prompts in {grouped.Count} groups");
            }
            catch (Exception ex)
            {
                DebugLog($"Error loading favorite groups: {ex.Message}");
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
                DebugLog($"Loaded favorite collection: {collection.Title}");
            }
            catch (Exception ex)
            {
                DebugLog($"Error loading favorite: {ex.Message}");
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
                    if (!group.Prompts.Any())
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
                DebugLog($"Removed favorite: {collection.Title}");
            }
            catch (Exception ex)
            {
                DebugLog($"Error removing favorite: {ex.Message}");
                await _toastService.ShowToastAsync("Error removing favorite");

                // If there's an error, reload to ensure UI is in sync with database
                _ = Task.Run(async () => await LoadFavoriteGroupsAsync());
            }
        }

        private string GetCollectionGroupName(string? title)
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
                DebugLog("Initializing PromptGenerator...");

                string dataPath = Path.Combine(FileSystem.AppDataDirectory, "prompt_data");
                DebugLog($"Data path: {dataPath}");
                DebugLog($"Data path exists: {Directory.Exists(dataPath)}");

                // Wait for the initialization to complete if needed
                int retryCount = 0;
                const int maxRetries = 20;

                while (!Directory.Exists(dataPath) && retryCount < maxRetries)
                {
                    DebugLog($"Waiting for data directory to be created... Retry {retryCount + 1}/{maxRetries}");
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
                    DebugLog("PromptGenerator initialization completed successfully");
                }
                else
                {
                    DebugLog($"Data directory still does not exist after {maxRetries} retries - creating defaults");

                    // Create the generator- use defaults
                    _generator = new PromptGenerator(dataPath);
                    _isInitialized = true;
                    StatusMessage = "Ready (using defaults)";
                }


            }
            catch (Exception ex)
            {
                DebugLog($"Error initializing PromptGenerator: {ex.Message}");
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
                    DebugLog($"Failed to create fallback generator: {ex2.Message}");
                    StatusMessage = "Failed to initialize";
                }
            }
        }

        private static async Task LogDirectoryContentsAsync(string dataPath)
        {
            try
            {
                await Task.Run(() =>
                {
                    var dirs = Directory.GetDirectories(dataPath);
                    DebugLog($"Found {dirs.Length} subdirectories:");

                    foreach (var dir in dirs)
                    {
                        DebugLog($"  - {Path.GetFileName(dir)}");
                        var files = Directory.GetFiles(dir, "*.json");
                        DebugLog($"    Files: {files.Length}");

                        foreach (var file in files)
                        {
                            var fileInfo = new FileInfo(file);
                            DebugLog($"      - {Path.GetFileName(file)} ({fileInfo.Length} bytes)");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                DebugLog($"Error logging directory contents: {ex.Message}");
            }
        }

        private void GeneratePrompt()
        {
            try
            {
                if (!_isInitialized || _generator == null)
                {
                    DebugLog("Generator not initialized yet");
                    GeneratedPrompt = "Generator is still initializing, please wait...";
                    return;
                }

                DebugLog($"Generating prompt with counts - Nouns: {NounCount}, Settings: {SettingCount}, Styles: {StyleCount}, Themes: {ThemeCount}");

                var (prompt, components) = _generator.GenerateCustomPrompt(
                    nounCount: NounCount,
                    settingCount: SettingCount,
                    styleCount: StyleCount,
                    themeCount: ThemeCount
                );

                GeneratedPrompt = prompt;
                _lastGeneratedComponents = components;

                // Update the component collections
                UpdateCollection(Nouns, components.ContainsKey("nouns") ? components["nouns"] : new List<string>());
                UpdateCollection(Settings, components.ContainsKey("settings") ? components["settings"] : new List<string>());
                UpdateCollection(Styles, components.ContainsKey("styles") ? components["styles"] : new List<string>());
                UpdateCollection(Themes, components.ContainsKey("themes") ? components["themes"] : new List<string>());


                DebugLog($"Prompt generated successfully: {prompt}");
            }
            catch (Exception ex)
            {
                DebugLog($"Error generating prompt: {ex.Message}");
                GeneratedPrompt = "Error generating prompt. Please try again.";
            }
        }


        private async Task TestGeneratorAsync()
        {
            try
            {
                if (_generator == null) return;

                DebugLog("=== Testing Generator ===");

                await Task.Run(() =>
                {
                    var categories = _generator.GetAvailableCategories();
                    DebugLog($"Available categories: {string.Join(", ", categories)}");

                    foreach (var category in categories)
                    {
                        int count = _generator.GetCategoryItemsCount(category);
                        DebugLog($"Category '{category}': {count} items");

                        if (count > 0)
                        {
                            var sample = _generator.GetRandomItems(category, count: Math.Min(3, count));
                            DebugLog($"  Sample items: {string.Join(", ", sample)}");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                DebugLog($"Error testing generator: {ex.Message}");
            }
        }

        private void UpdateCollection(ObservableCollection<string> collection, List<string> newItems)
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

            DebugLog("Prompt cleared");
        }

        private async Task ExportToClipboardAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(GeneratedPrompt))
                {
                    await Clipboard.SetTextAsync(GeneratedPrompt);
                    await _toastService.ShowToastAsync("Prompt copied to clipboard! ✓");
                    DebugLog("Prompt copied to clipboard successfully");
                }
                else
                {
                    await _toastService.ShowToastAsync("No prompt to copy!");
                    DebugLog("Attempted to copy empty prompt");
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Error copying prompt to clipboard: {ex.Message}");
                await _toastService.ShowToastAsync("Error copying to clipboard");
            }
        }

        private async Task SaveToFavoritesAsync()
        {
            try
            {
                if (!_lastGeneratedComponents.Any())
                {
                    await _toastService.ShowToastAsync("No prompt to save!");
                    return;
                }

                var mainPage = Application.Current?.Windows.FirstOrDefault()?.Page;
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
                DebugLog($"Error in SaveToFavoritesAsync: {ex.Message}");
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
                DebugLog($"Prompt saved to default favorites with title: {title}");
            }
            catch (Exception ex)
            {
                DebugLog($"Error saving to default favorites: {ex.Message}");
                await _toastService.ShowToastAsync("Error saving to favorites");
            }
        }


        private async Task SaveToCollectionAsync()
        {
            try
            {
                var mainPage = Application.Current?.Windows.FirstOrDefault()?.Page;
                if (mainPage != null)
                {
                    // Get existing collection names for suggestion
                    var existingCollections = await _wordPromptService.GetFavoritesAsync();
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
                        placeholder: "e.g., Dark, Fantasy, Sci-Fi"
                    );

                    if (!string.IsNullOrWhiteSpace(collectionName))
                    {
                        var cleanName = collectionName.Trim();

                        // Create title that indicates this belongs to a collection
                        // Format: "CollectionName - Prompt {count}"
                        var existingInCollection = existingCollections
                            .Where(c => GetCollectionNameFromTitle(c.Title) == cleanName)
                            .Count();

                        var title = $"{cleanName} - {GeneratedPrompt}";
                        if (title.Length > 100) // Limit title length
                            title = title.Substring(0, 97) + "...";

                        var savedCollection = await _wordPromptService.SaveWordCollectionWithCategoriesAsync(
                            _lastGeneratedComponents,
                            title: title,
                            promptType: "custom");

                        // Mark as favorite
                        await _wordPromptService.ToggleFavoriteAsync(savedCollection.Id);

                        await _toastService.ShowToastAsync($"Saved to '{cleanName}' collection ⭐");
                        DebugLog($"Prompt saved to collection: {cleanName} with title: {title}");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Error saving to collection: {ex.Message}");
                await _toastService.ShowToastAsync("Error saving to collection");
            }
        }

        private string GetCollectionNameFromTitle(string? title)
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

                DebugLog($"Loaded {favorites.Count} favorite collections");
            }
            catch (Exception ex)
            {
                DebugLog($"Error loading favorites: {ex.Message}");
            }
        }

        private async Task LoadHistoryAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("PromptHistoryPage");
                DebugLog("Navigated to history page");
            }
            catch (Exception ex)
            {
                DebugLog($"Error navigating to history: {ex.Message}");
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
                DebugLog($"Removed collection {collection.Id} from favorites");
            }
            catch (Exception ex)
            {
                DebugLog($"Error removing favorite: {ex.Message}");
                await _toastService.ShowToastAsync("Error removing from favorites");
            }
        }

        private async Task LoadCollectionAsync(WordCollection? collection)
        {
            try
            {
                if (collection == null) return;

                var categorizedWords = await _wordPromptService.GetWordsByCategoryAsync(collection);

                UpdateCollection(Nouns, categorizedWords.ContainsKey("nouns") ? categorizedWords["nouns"] : new List<string>());
                UpdateCollection(Settings, categorizedWords.ContainsKey("settings") ? categorizedWords["settings"] : new List<string>());
                UpdateCollection(Styles, categorizedWords.ContainsKey("styles") ? categorizedWords["styles"] : new List<string>());
                UpdateCollection(Themes, categorizedWords.ContainsKey("themes") ? categorizedWords["themes"] : new List<string>());

                var allWords = new List<string>();
                allWords.AddRange(categorizedWords.Values.SelectMany(words => words));
                GeneratedPrompt = string.Join(", ", allWords);

                _lastGeneratedComponents = categorizedWords;

                await _toastService.ShowToastAsync($"Loaded: {collection.Title ?? "Untitled"} ✓");
                DebugLog($"Loaded collection: {collection.Title}");
            }
            catch (Exception ex)
            {
                DebugLog($"Error loading collection: {ex.Message}");
                await _toastService.ShowToastAsync("Error loading collection");
            }
        }

        // Simple debug logging method
        private static void DebugLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logMessage = $"[VM {timestamp}] {message}";

            // Output to multiple channels to ensure visibility
            Debug.WriteLine(logMessage);
            System.Console.WriteLine(logMessage);

            // Also try to write to a log file
            try
            {
                var logPath = Path.Combine(FileSystem.AppDataDirectory, "viewmodel_log.txt");
                File.AppendAllText(logPath, logMessage + Environment.NewLine);
            }
            catch
            {
                // Ignore file write errors
            }
        }
    }

    // Helper classes for grouping
    public class PromptCollectionGroup : ObservableCollection<WordCollection>
    {
        public string CollectionName { get; }
        public ObservableCollection<WordCollection> Prompts { get; }

        public PromptCollectionGroup(string collectionName, IEnumerable<WordCollection> prompts) : base(prompts)
        {
            CollectionName = collectionName;
            Prompts = new ObservableCollection<WordCollection>(prompts);
        }
    }
}