using artstudio.Models;
using artstudio.Services;
using artstudio.Data;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace artstudio.ViewModels
{

    public partial class StudyPageViewModel : INotifyPropertyChanged
    {
        #region Fields

        private Timer? _timer;
        private bool _isRunning;
        private bool _isPaused;
        private int _timeLeft;
        private int _totalTime;
        private string _selectedMode = "Quick Sketch";
        private string _selectedQuickTime = "30 sec";
        private string _selectedSessionTime = "30 min";
        private bool _useCustomTime;
        private int _customDuration = 120;

        private readonly PromptGenerator _promptGenerator;
        private readonly Unsplash _unsplash;
        private readonly PaletteModel _paletteModel;
        private readonly ILogger<StudyPageViewModel> _logger;
        private readonly IToastService _toastService;
        private readonly DatabaseService _databaseService;
        private bool _disposed = false;

        public ObservableCollection<string> ModeOptions { get; } = new()
        {
            "Quick Sketch",
            "Session"
        };

        public ObservableCollection<string> QuickTimeOptions { get; } = new()
        {
            "30 sec",
            "1 min",
            "5 min",
            "10 min",
            "30 min"
        };

        public ObservableCollection<string> SessionTimeOptions { get; } = new()
        {
            "30 min",
            "45 min",
            "1 hr",
            "1.5 hr",
            "2 hr",
            "2.5 hr",
            "3 hr",
            "Custom"
        };

        #endregion

        #region Constructor

        public StudyPageViewModel(PromptGenerator promptGenerator, Unsplash unsplash, PaletteModel paletteModel, ILogger<StudyPageViewModel> logger, DatabaseService databaseService, IToastService toastService)
        {
            _promptGenerator = promptGenerator;
            _unsplash = unsplash;
            _paletteModel = paletteModel;
            _databaseService = databaseService;
            _toastService = toastService;
            _logger = logger;

            InitializeCommands(); 
            //DebugPromptGenerator();

        }

        #endregion

        #region Properties

        public string TimeLeftDisplay => $"{_timeLeft / 60}:{(_timeLeft % 60):D2}";

        public string CurrentModeDisplay =>
            _selectedMode == "Quick Sketch" ? $"Quick: {_selectedQuickTime}" :
            _useCustomTime ? $"Session: {_customDuration}m" : $"Session: {_selectedSessionTime}";

        public string PlayPauseButtonText =>
            !_isRunning ? "Start" : (_isPaused ? "Resume" : "Pause");

        public Color PlayPauseButtonColor =>
            !_isRunning ? Color.FromArgb("#4CAF50") : (_isPaused ? Color.FromArgb("#4CAF50") : Color.FromArgb("#FF9800"));

        public bool IsQuickMode => _selectedMode == "Quick Sketch";
        public bool IsSessionMode => _selectedMode == "Session";
        public bool ShowPalette => IsSessionMode && CurrentPalette.Count > 0;
        public bool CanUndo => IsQuickMode && PreviousContent.Count > 0;
        public bool CanRegenerate => IsSessionMode;
        public bool CanSaveSession => IsSessionMode && (CurrentWords.Count > 0 || CurrentPalette.Count > 0 || CurrentImages.Count > 0);

        public string SelectedMode
        {
            get => _selectedMode;
            set
            {
                if (_selectedMode != value)
                {
                    _selectedMode = value;

                    // Reset states when switching modes
                    ResetModeStates();

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsQuickMode));
                    OnPropertyChanged(nameof(IsSessionMode));
                    OnPropertyChanged(nameof(ShowPalette));
                    OnPropertyChanged(nameof(CurrentModeDisplay));
                    OnPropertyChanged(nameof(CanUndo));
                    OnPropertyChanged(nameof(CanRegenerate));
                    UndoCommand.NotifyCanExecuteChanged();
                    RegenerateCommand.NotifyCanExecuteChanged();
                }
            }
        }

        private void ResetModeStates()
        {
            _logger.LogDebug("Switching modes - clearing all states");

            // Stop any running timer
            _timer?.Dispose();
            _isRunning = false;
            _isPaused = false;
            _timeLeft = 0;
            _totalTime = 0;

            // Clear all content
            CurrentPalette.Clear();
            CurrentWords.Clear();
            CurrentImages.Clear();

            // Clear undo history
            PreviousContent.Clear();

            // Notify UI of changes
            OnPropertyChanged(nameof(TimeLeftDisplay));
            OnPropertyChanged(nameof(PlayPauseButtonText));
            OnPropertyChanged(nameof(PlayPauseButtonColor));
            OnPropertyChanged(nameof(ShowPalette));
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanSaveSession)); 
            SaveSessionCommand.NotifyCanExecuteChanged();
        }

        public string SelectedQuickTime
        {
            get => _selectedQuickTime;
            set
            {
                if (_selectedQuickTime != value)
                {
                    _selectedQuickTime = value;

                    // Reset timer if it's currently running and in Quick mode
                    if (_isRunning && IsQuickMode)
                    {
                        ResetTimerOnTimeChange();
                    }

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentModeDisplay));
                }
            }
        }

        public string SelectedSessionTime
        {
            get => _selectedSessionTime;
            set
            {
                if (_selectedSessionTime != value)
                {
                    _selectedSessionTime = value;
                    _useCustomTime = value == "Custom";

                    // Reset timer if it's currently running and in Session mode
                    if (_isRunning && IsSessionMode)
                    {
                        ResetTimerOnTimeChange();
                    }

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(UseCustomTime));
                    OnPropertyChanged(nameof(ShowCustomDuration));
                    OnPropertyChanged(nameof(CurrentModeDisplay));
                }
            }
        }

        public bool UseCustomTime
        {
            get => _useCustomTime;
            set
            {
                _useCustomTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowCustomDuration));
                OnPropertyChanged(nameof(CurrentModeDisplay));
            }
        }

        public bool ShowCustomDuration => _useCustomTime && IsSessionMode;

        public int CustomDuration
        {
            get => _customDuration;
            set
            {
                if (_customDuration != value)
                {
                    _customDuration = value;

                    // Reset timer if it's currently running, in Session mode, and using custom time
                    if (_isRunning && IsSessionMode && _useCustomTime)
                    {
                        ResetTimerOnTimeChange();
                    }

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentModeDisplay));
                }
            }
        }

        private void ResetTimerOnTimeChange()
        {
            _logger.LogDebug("Time selection changed while timer running - resetting timer");

            // Stop the current timer
            _timer?.Dispose();
            _isRunning = false;
            _isPaused = false;
            _timeLeft = 0;
            _totalTime = 0;

            // Update UI to show reset state
            OnPropertyChanged(nameof(TimeLeftDisplay));
            OnPropertyChanged(nameof(PlayPauseButtonText));
            OnPropertyChanged(nameof(PlayPauseButtonColor));

            // Optionally show a toast to inform the user
            _ = Task.Run(async () =>
            {
                try
                {
                    await _toastService.ShowToastAsync("Timer reset due to time change. Press Start to begin with new duration.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to show timer reset toast");
                }
            });
        }

        public ObservableCollection<Color> CurrentPalette { get; private set; } = new();
        public ObservableCollection<string> CurrentWords { get; } = new ObservableCollection<string>();
        public ObservableCollection<ImageItemViewModel> CurrentImages { get; private set; } = new();
        public ObservableCollection<ContentSnapshot> PreviousContent { get; private set; } = new();

        #endregion

        #region Commands

        public RelayCommand PlayPauseCommand { get; private set; } = default!;
        public RelayCommand ResetCommand { get; private set; } = default!;
        public RelayCommand UndoCommand { get; private set; } = default!;
        public RelayCommand RegenerateCommand { get; private set; } = default!;
        public AsyncRelayCommand SaveSessionCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            PlayPauseCommand = new RelayCommand(ExecutePlayPause);
            ResetCommand = new RelayCommand(ExecuteReset);
            UndoCommand = new RelayCommand(ExecuteUndo, () => CanUndo);
            RegenerateCommand = new RelayCommand(ExecuteRegenerate, () => CanRegenerate);
            SaveSessionCommand = new AsyncRelayCommand(ExecuteSaveSessionAsync, () => CanSaveSession);
        }

        #endregion

        #region Command Implementations

        private void ExecutePlayPause()
        {
            if (!_isRunning)
                _ = StartTimerAsync();
            else
                _isPaused = !_isPaused;

            OnPropertyChanged(nameof(PlayPauseButtonText));
            OnPropertyChanged(nameof(PlayPauseButtonColor));
        }

        private void ExecuteReset()
        {
            _timer?.Dispose();
            _isRunning = false;
            _isPaused = false;
            _timeLeft = 0;
            _totalTime = 0;

            OnPropertyChanged(nameof(TimeLeftDisplay));
            OnPropertyChanged(nameof(PlayPauseButtonText));
            OnPropertyChanged(nameof(PlayPauseButtonColor));
        }

        private void ExecuteRegenerate()
        {
            if (IsSessionMode)
            {
                SaveCurrentContent();
                GenerateNewContent();
            }
        }

        private void ExecuteUndo()
        {
            if (PreviousContent.Count > 0)
            {
                var previous = PreviousContent[0];

                if (previous.Palette != null)
                {
                    CurrentPalette.Clear();
                    foreach (var color in previous.Palette) CurrentPalette.Add(color);
                }

                if (previous.Words != null)
                {
                    CurrentWords.Clear();
                    foreach (var word in previous.Words) CurrentWords.Add(word);
                }

                if (previous.Images != null)
                {
                    CurrentImages.Clear();
                    foreach (var image in previous.Images) CurrentImages.Add(image);
                }

                PreviousContent.RemoveAt(0);
                OnPropertyChanged(nameof(CanUndo));
                OnPropertyChanged(nameof(ShowPalette));

                // RelayCommand automatically handles CanExecuteChanged when CanUndo changes
                UndoCommand.NotifyCanExecuteChanged();
            }
        }

        private async Task StartTimerAsync()
        {
            GenerateNewContent();

            _timeLeft = GetTimerDuration();
            _totalTime = _timeLeft;
            _isRunning = true;
            _isPaused = false;

            OnPropertyChanged(nameof(TimeLeftDisplay));
            OnPropertyChanged(nameof(PlayPauseButtonText));
            OnPropertyChanged(nameof(PlayPauseButtonColor));

            _timer = new Timer(TimerCallback, null, 1000, 1000);

            await ExecuteGenerateImagesAsync();
        }

        private void TimerCallback(object? state)
        {
            if (_isPaused) return;

            _timeLeft--;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(TimeLeftDisplay));
            });

            if (_timeLeft <= 0)
            {
                if (IsQuickMode)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        SaveCurrentContent();
                        GenerateNewContent();
                        _timeLeft = GetTimerDuration();
                        _totalTime = _timeLeft;
                    });
                }
                else if (IsSessionMode)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ExecuteReset();
                    });
                }
            }
        }

        private int GetTimerDuration()
        {
            if (IsQuickMode)
            {
                return _selectedQuickTime switch
                {
                    "30 sec" => 30,
                    "1 min" => 60,
                    "5 min" => 300,
                    "10 min" => 600,
                    "30 min" => 1800,
                    _ => 30
                };
            }
            else // Session mode
            {
                if (_useCustomTime)
                {
                    return _customDuration * 60;
                }
                else
                {
                    return _selectedSessionTime switch
                    {
                        "30 min" => 1800,   
                        "45 min" => 2700,    
                        "1 hr" => 3600,      
                        "1.5 hr" => 5400,    
                        "2 hr" => 7200,      
                        "2.5 hr" => 9000,    
                        "3 hr" => 10800,     
                        _ => 1800
                    };
                }
            }
        }

        private void GenerateNewContent()
        {
            if (IsSessionMode)
                GeneratePalette();

            GenerateWords();
            _ = ExecuteGenerateImagesAsync();

            OnPropertyChanged(nameof(CanSaveSession));
            SaveSessionCommand.NotifyCanExecuteChanged();
        }

        private void GeneratePalette()
        {
            CurrentPalette.Clear();
            var random = new Random();
            var harmonyTypes = Enum.GetValues<PaletteModel.ColorHarmonyType>();
            var randomHarmonyType = harmonyTypes[random.Next(harmonyTypes.Length)];

            // Generate completely new palette=
            var generatedColors = _paletteModel.HarmonyPaletteGenerator(
                randomHarmonyType,
                randomFactor: 0.6f);

            var distinctColors = generatedColors.Distinct().Take(5);

            foreach (var color in distinctColors)
            {
                CurrentPalette.Add(color);
            }

            _logger.LogDebug("Generated {ColorCount} distinct colors using {HarmonyType} harmony", CurrentPalette.Count, randomHarmonyType);
            OnPropertyChanged(nameof(ShowPalette));
        }

        // Leaving debug here just in case; problem solved mixmatch directories hence why it was using default
        //private void DebugPromptGenerator()
        //{
        //    try
        //    {
        //        _logger.LogDebug("Debugging PromptGenerator...");

        //        // Check available categories
        //        var categories = _promptGenerator.GetAvailableCategories();
        //        _logger.LogDebug("Available categories: {Categories}", string.Join(", ", categories));

        //        // Check item counts
        //        foreach (var category in categories)
        //        {
        //            var count = _promptGenerator.GetCategoryItemsCount(category);
        //            _logger.LogDebug("Category '{Category}' has {Count} items", category, count);
        //        }

        //        // Test a simple generation
        //        var (testPrompt, testComponents) = _promptGenerator.GenerateDefaultPrompt();
        //        _logger.LogDebug("Test prompt: '{TestPrompt}'", testPrompt);
        //        _logger.LogDebug("Test components count: {ComponentCount}", testComponents.Count);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error in DebugPromptGenerator");
        //    }
        //}

        private void GenerateWords()
        {
            try
            {
                _logger.LogDebug("GenerateWords called");
                CurrentWords.Clear();

                var includeNouns = RandomBool();
                var includeSettings = RandomBool();
                var includeStyles = RandomBool();
                var includeThemes = RandomBool();

                _logger.LogDebug("Include flags - Nouns: {IncludeNouns}, Settings: {IncludeSettings}, Styles: {IncludeStyles}, Themes: {IncludeThemes}", includeNouns, includeSettings, includeStyles, includeThemes);

                // Ensure at least one category is included
                if (!includeNouns && !includeSettings && !includeStyles && !includeThemes)
                {
                    includeNouns = true;
                    _logger.LogDebug("No categories selected, defaulting to nouns");
                }

                var (prompt, components) = _promptGenerator.GeneratePrompt(
                    includeNouns: includeNouns,
                    includeSettings: includeSettings,
                    includeStyles: includeStyles,
                    includeThemes: includeThemes,
                    nounCount: 3,
                    settingMin: 1,
                    settingMax: 2,
                    styleMin: 1,
                    styleMax: 2,
                    themeCount: 1,
                    settingProbability: 0.5,
                    themeProbability: 0.7
                );

                _logger.LogDebug("Generated prompt: '{GeneratedPrompt}'", prompt);
                _logger.LogDebug("Components returned: {ComponentCount}", components.Count);

                int wordCount = 0;
                foreach (var categoryPair in components)
                {
                    _logger.LogDebug("Processing category: {Category} with {ItemCount} items", categoryPair.Key, categoryPair.Value.Count);

                    foreach (var word in categoryPair.Value)
                    {
                        if (!string.IsNullOrWhiteSpace(word))
                        {
                            CurrentWords.Add(word);
                            wordCount++;
                            _logger.LogDebug("Added word: '{Word}'", word);
                        }
                    }
                }

                _logger.LogDebug("Total words added: {WordCount}", wordCount);
                _logger.LogDebug("CurrentWords.Count: {CurrentWordsCount}", CurrentWords.Count);

                // Notify UI of changes
                OnPropertyChanged(nameof(CurrentWords));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GenerateWords");
            }
        }

        private static bool RandomBool()
        {
            return new Random().Next() > 0.5;
        }

        private async Task ExecuteGenerateImagesAsync()
        {
            try
            {
                CurrentImages.Clear();
                var images = await _unsplash.GetRandomImagesAsync(count: 2);
                foreach (var image in images)
                    CurrentImages.Add(new ImageItemViewModel(image));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _logger.LogWarning(ex, "Invalid image count parameter");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "API key issue");
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "Request timeout");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Network error");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Service error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching images");
            }
        }

        private void SaveCurrentContent()
        {
            // Only save content for Quick Sketch mode (for undo functionality)
            if (IsQuickMode && (CurrentPalette.Count > 0 || CurrentWords.Count > 0 || CurrentImages.Count > 0))
            {
                var snapshot = new ContentSnapshot
                {
                    Palette = new List<Color>(CurrentPalette),
                    Words = new List<string>(CurrentWords),
                    Images = new List<ImageItemViewModel>(CurrentImages),
                    Timestamp = DateTime.Now
                };

                PreviousContent.Insert(0, snapshot);
                if (PreviousContent.Count > 5)
                    PreviousContent.RemoveAt(5);

                OnPropertyChanged(nameof(CanUndo));

                // Notify that the UndoCommand's CanExecute state may have changed
                UndoCommand.NotifyCanExecuteChanged();
            }
        }

        private async Task ExecuteSaveSessionAsync()
        {
            try
            {
                _logger.LogDebug("=== STARTING SESSION SAVE ===");

                if (!CanSaveSession)
                {
                    _logger.LogWarning("Cannot save session - CanSaveSession is false");
                    return;
                }

                _logger.LogDebug("CanSaveSession check passed");

                // Prompt user for session name
                var sessionName = await PromptForSessionNameAsync();
                if (sessionName == null)
                {
                    _logger.LogDebug("User cancelled session save");
                    return; // User cancelled
                }

                _logger.LogDebug("User provided session name: '{SessionName}'", sessionName);

                // Cache images locally before saving
                _logger.LogDebug("Caching {ImageCount} images locally...", CurrentImages.Count);
                var cachedImages = await CacheImagesForSessionAsync(CurrentImages.ToList());

                // Get current session info
                var sessionMode = SelectedMode;
                var sessionDuration = IsSessionMode ?
                    (UseCustomTime ? $"{CustomDuration}m" : SelectedSessionTime) :
                    SelectedQuickTime;

                // Create session snapshot with cached images and custom title
                var sessionSnapshot = SessionSnapshot.FromCurrentSession(
                    CurrentWords.ToList(),
                    CurrentPalette.ToList(),
                    cachedImages,
                    sessionMode,
                    sessionDuration,
                    customTitle: sessionName // Pass the user-provided name
                );

                _logger.LogDebug("About to call database save...");
                await _databaseService.SaveSessionSnapshotAsync(sessionSnapshot);
                _logger.LogDebug("Database save completed successfully");

                await _databaseService.DeleteOldSessionSnapshotsAsync(20);
                _logger.LogDebug("Old sessions cleanup completed");

                _logger.LogDebug("About to show success toast...");
                await _toastService.ShowToastAsync($"Session '{sessionName}' saved! Load it in Gallery Creation.");
                _logger.LogDebug("Toast call completed - should be visible now");

                _logger.LogDebug("=== SESSION SAVE COMPLETED ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExecuteSaveSessionAsync");
                _logger.LogDebug("About to show error toast...");
                await _toastService.ShowToastAsync("Failed to save session");
                _logger.LogDebug("Error toast call completed");
            }
        }

        private async Task<string?> PromptForSessionNameAsync()
        {
            try
            {
                var mainPage = Application.Current?.Windows.FirstOrDefault()?.Page;
                if (mainPage == null)
                {
                    _logger.LogWarning("Could not find main page for session name prompt");
                    return GenerateDefaultSessionName(); // Fallback to auto-generated name
                }

                // Generate a suggested name based on current content
                var suggestedName = GenerateDefaultSessionName();

                // Prompt user for session name
                var result = await mainPage.DisplayPromptAsync(
                    "Save Session",
                    "Give your session a memorable name:",
                    "Save",
                    "Cancel",
                    suggestedName,
                    maxLength: 50,
                    keyboard: Keyboard.Text
                );

                if (string.IsNullOrWhiteSpace(result))
                {
                    return null; // User cancelled or entered empty name
                }

                return result.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error prompting for session name");
                return GenerateDefaultSessionName(); // Fallback to auto-generated name
            }
        }

        private string GenerateDefaultSessionName()
        {
            var sessionType = IsSessionMode ? "Session" : "Quick Sketch";
            var duration = IsSessionMode ?
                (UseCustomTime ? $"{CustomDuration}m" : SelectedSessionTime) :
                SelectedQuickTime;

            var timestamp = DateTime.Now.ToString("MMM dd, HH:mm");

            // Try to make it more descriptive based on content
            var description = "";
            if (CurrentWords.Count > 0)
            {
                var firstWord = CurrentWords.FirstOrDefault();
                if (!string.IsNullOrEmpty(firstWord))
                {
                    description = $" - {firstWord}";
                }
            }

            return $"{sessionType} ({duration}){description}";
        }

        #endregion


        #region Image Caching Methods
        private async Task<UnsplashImage> CacheImageLocallyAsync(ImageItemViewModel imageViewModel)
        {
            try
            {
                var originalImage = imageViewModel.UnsplashImage;
                var thumbUrl = originalImage.urls?.Thumb;

                if (string.IsNullOrEmpty(thumbUrl))
                {
                    _logger.LogWarning("No thumbnail URL for image {ImageId}", originalImage.Id);
                    return originalImage; // Return original if no thumb URL
                }

                // Create local file path
                var fileName = $"thumb_{originalImage.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                var localPath = Path.Combine(FileSystem.AppDataDirectory, "SessionImages", fileName);

                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

                // Download and save thumbnail
                using (var httpClient = new HttpClient())
                {
                    var imageBytes = await httpClient.GetByteArrayAsync(thumbUrl);
                    await File.WriteAllBytesAsync(localPath, imageBytes);
                }

                // Create new UnsplashImage with local path
                var cachedImage = new UnsplashImage
                {
                    Id = originalImage.Id,
                    Description = originalImage.Description,
                    user = originalImage.user,
                    urls = new UnsplashImage.Urls
                    {
                        Raw = originalImage.urls?.Raw,
                        Full = originalImage.urls?.Full,
                        Regular = originalImage.urls?.Regular,
                        Small = originalImage.urls?.Small,
                        Thumb = localPath // Use local path instead of remote URL
                    }
                };

                _logger.LogDebug("Cached image thumbnail locally: {LocalPath}", localPath);
                return cachedImage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cache image {ImageId} locally", imageViewModel.UnsplashImage.Id);
                return imageViewModel.UnsplashImage; // Return original on failure
            }
        }

        private async Task<List<UnsplashImage>> CacheImagesForSessionAsync(List<ImageItemViewModel> images)
        {
            var cachedImages = new List<UnsplashImage>();

            foreach (var image in images)
            {
                var cachedImage = await CacheImageLocallyAsync(image);
                cachedImages.Add(cachedImage);
            }

            return cachedImages;
        }

        #endregion
        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _timer?.Dispose();
                _unsplash?.Dispose();
                _disposed = true;
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }

    #region Supporting Models

    public class ContentSnapshot
    {
        public List<Color>? Palette { get; set; }
        public List<string>? Words { get; set; }
        public List<ImageItemViewModel>? Images { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion
}