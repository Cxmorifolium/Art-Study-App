﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.Input;
using artstudio.Models;
using artstudio.Services;

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
        private bool _disposed = false;
        private readonly PaletteModel _paletteModel;

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
            "3 hr"
        };

        #endregion

        #region Constructor

        public StudyPageViewModel(PromptGenerator promptGenerator, Unsplash unsplash, PaletteModel paletteModel)
        {
            _promptGenerator = promptGenerator;
            _unsplash = unsplash;
            _paletteModel = paletteModel;

            InitializeCommands();
            DebugPromptGenerator();
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
            Debug.WriteLine($"[StudyPage] Switching modes - clearing all states");

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
        }

        public string SelectedQuickTime
        {
            get => _selectedQuickTime;
            set
            {
                _selectedQuickTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentModeDisplay));
            }
        }

        public string SelectedSessionTime
        {
            get => _selectedSessionTime;
            set
            {
                _selectedSessionTime = value;
                _useCustomTime = value == "Custom";
                OnPropertyChanged();
                OnPropertyChanged(nameof(UseCustomTime));
                OnPropertyChanged(nameof(ShowCustomDuration));
                OnPropertyChanged(nameof(CurrentModeDisplay));
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
                _customDuration = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentModeDisplay));
            }
        }

        public ObservableCollection<Color> CurrentPalette { get; private set; } = new();
        public ObservableCollection<string> CurrentWords { get; } = new ObservableCollection<string>();
        public ObservableCollection<ImageItem> CurrentImages { get; private set; } = new();
        public ObservableCollection<ContentSnapshot> PreviousContent { get; private set; } = new();

        #endregion

        #region Commands

        public RelayCommand PlayPauseCommand { get; private set; } = default!;
        public RelayCommand ResetCommand { get; private set; } = default!;
        public RelayCommand UndoCommand { get; private set; } = default!;
        public RelayCommand RegenerateCommand { get; private set; } = default!;

        private void InitializeCommands()
        {
            PlayPauseCommand = new RelayCommand(ExecutePlayPause);
            ResetCommand = new RelayCommand(ExecuteReset);
            UndoCommand = new RelayCommand(ExecuteUndo, () => CanUndo);
            RegenerateCommand = new RelayCommand(ExecuteRegenerate, () => CanRegenerate);
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

            Debug.WriteLine($"Generated {CurrentPalette.Count} distinct colors using {randomHarmonyType} harmony");
            OnPropertyChanged(nameof(ShowPalette));
        }

        // Leaving debug here just in case; problem solved mixmatch directories hence why it was using default
        private void DebugPromptGenerator()
        {
            try
            {
                Debug.WriteLine("[StudyPage] Debugging PromptGenerator...");

                // Check available categories
                var categories = _promptGenerator.GetAvailableCategories();
                Debug.WriteLine($"[StudyPage] Available categories: {string.Join(", ", categories)}");

                // Check item counts
                foreach (var category in categories)
                {
                    var count = _promptGenerator.GetCategoryItemsCount(category);
                    Debug.WriteLine($"[StudyPage] Category '{category}' has {count} items");
                }

                // Test a simple generation
                var (testPrompt, testComponents) = _promptGenerator.GenerateDefaultPrompt();
                Debug.WriteLine($"[StudyPage] Test prompt: '{testPrompt}'");
                Debug.WriteLine($"[StudyPage] Test components count: {testComponents.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StudyPage] Error in DebugPromptGenerator: {ex.Message}");
            }
        }

        private void GenerateWords()
        {
            try
            {
                Debug.WriteLine("[StudyPage] GenerateWords called");
                CurrentWords.Clear();

                var includeNouns = RandomBool();
                var includeSettings = RandomBool();
                var includeStyles = RandomBool();
                var includeThemes = RandomBool();

                Debug.WriteLine($"[StudyPage] Include flags - Nouns: {includeNouns}, Settings: {includeSettings}, Styles: {includeStyles}, Themes: {includeThemes}");

                // Ensure at least one category is included
                if (!includeNouns && !includeSettings && !includeStyles && !includeThemes)
                {
                    includeNouns = true;
                    Debug.WriteLine("[StudyPage] No categories selected, defaulting to nouns");
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

                Debug.WriteLine($"[StudyPage] Generated prompt: '{prompt}'");
                Debug.WriteLine($"[StudyPage] Components returned: {components.Count}");

                int wordCount = 0;
                foreach (var categoryPair in components)
                {
                    Debug.WriteLine($"[StudyPage] Processing category: {categoryPair.Key} with {categoryPair.Value.Count} items");

                    foreach (var word in categoryPair.Value)
                    {
                        if (!string.IsNullOrWhiteSpace(word))
                        {
                            CurrentWords.Add(word);
                            wordCount++;
                            Debug.WriteLine($"[StudyPage] Added word: '{word}'");
                        }
                    }
                }

                Debug.WriteLine($"[StudyPage] Total words added: {wordCount}");
                Debug.WriteLine($"[StudyPage] CurrentWords.Count: {CurrentWords.Count}");

                // Notify UI of changes
                OnPropertyChanged(nameof(CurrentWords));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StudyPage] Error in GenerateWords: {ex.Message}");
                Debug.WriteLine($"[StudyPage] Stack trace: {ex.StackTrace}");
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
                    CurrentImages.Add(new ImageItem(image));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Debug.WriteLine($"Invalid image count parameter: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"API key issue: {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine($"Request timeout: {ex.Message}");
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"Network error: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"Service error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error fetching images: {ex.Message}");
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
                    Images = new List<ImageItem>(CurrentImages),
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
        public List<ImageItem>? Images { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion
}