using artstudio.Services;
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
        private string _generatedPrompt = string.Empty;
        private int _nounCount = 1;
        private int _settingCount = 1;
        private int _styleCount = 1;
        private int _themeCount = 1;
        private bool _isInitialized = false;
        private string _statusMessage = "Initializing...";

        public PromptGeneratorViewModel()
        {
            DebugLog("=== PromptGeneratorViewModel Constructor Started ===");

            // Initialize collections  
            Nouns = new ObservableCollection<string>();
            Settings = new ObservableCollection<string>();
            Styles = new ObservableCollection<string>();
            Themes = new ObservableCollection<string>();

            // Initialize commands  
            GenerateCommand = new RelayCommand(GeneratePrompt);
            ClearCommand = new RelayCommand(ClearPrompt);
            ExportCommand = new AsyncRelayCommand(ExportToClipboardAsync);
            FavoriteCommand = new RelayCommand(SaveToFavorites);
            HistoryCommand = new RelayCommand(ViewHistory);

            // Initialize the prompt generator asynchronously  
            _ = InitializeGeneratorAsync();

            DebugLog("=== PromptGeneratorViewModel Constructor Completed ===");
        }

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

        public ObservableCollection<string> Nouns { get; }
        public ObservableCollection<string> Settings { get; }
        public ObservableCollection<string> Styles { get; }
        public ObservableCollection<string> Themes { get; }

        public ICommand GenerateCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand FavoriteCommand { get; }
        public ICommand HistoryCommand { get; }

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

            DebugLog("Prompt cleared");
        }

        private async Task ExportToClipboardAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(GeneratedPrompt))
                {
                    await Clipboard.SetTextAsync(GeneratedPrompt);
                    await Toast.Make("Prompt copied to clipboard!").Show();
                    DebugLog("Prompt copied to clipboard");
                }
                else
                {
                    await Toast.Make("No prompt to copy!").Show();
                    DebugLog("Attempted to copy empty prompt");
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Error copying prompt to clipboard: {ex.Message}");
                await Toast.Make("Error copying to clipboard").Show();
            }
        }

        private void SaveToFavorites()
        {
            DebugLog("SaveToFavorites called (not yet implemented)");
            // For future implementation
        }

        private void ViewHistory()
        {
            DebugLog("ViewHistory called (not yet implemented)");
            // For future implementation
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
}