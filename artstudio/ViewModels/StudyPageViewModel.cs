using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using artstudio.Models;
using artstudio.Services;

namespace artstudio.ViewModels
{

    public class StudyPageViewModel : INotifyPropertyChanged
    {
        #region Fields

        private Timer? _timer;
        private bool _isRunning;
        private bool _isPaused;
        private int _timeLeft;
        private string _selectedMode = "Quick Sketch";
        private string _selectedQuickTime = "30 sec";
        private bool _useCustomSession;
        private bool _showWipAlert;
        private int _countdownAlert;
        private int _customDuration = 120;
        private int _customPauseInterval = 10;

        private readonly PromptGenerator _promptGenerator;
        private readonly Unsplash _unsplash;

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

        #endregion

        #region Constructor

        public StudyPageViewModel(PromptGenerator promptGenerator, Unsplash unsplash)
        {
            _promptGenerator = promptGenerator;
            _unsplash = unsplash;

            InitializeCommands();
            InitializeData();
        }

        #endregion

        #region Properties

        public string TimeLeftDisplay => $"{_timeLeft / 60}:{(_timeLeft % 60):D2}";

        public string CurrentModeDisplay =>
            _selectedMode == "Quick Sketch" ? $"Quick: {_selectedQuickTime}" :
            _useCustomSession ? $"Custom: {_customDuration}m" : "Session";

        public string PlayPauseButtonText =>
            !_isRunning ? "Start" : (_isPaused ? "Resume" : "Pause");

        public Color PlayPauseButtonColor =>
            !_isRunning ? Color.FromArgb("#4CAF50") : (_isPaused ? Color.FromArgb("#4CAF50") : Color.FromArgb("#FF9800"));

        public bool IsQuickMode => _selectedMode == "Quick Sketch";
        public bool IsSessionMode => _selectedMode == "Session";
        public bool ShowPalette => IsSessionMode && CurrentPalette.Count > 0;
        public bool CanUndo => PreviousContent.Count > 0;

        public string SelectedMode
        {
            get => _selectedMode;
            set
            {
                _selectedMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsQuickMode));
                OnPropertyChanged(nameof(IsSessionMode));
                OnPropertyChanged(nameof(ShowPalette));
                OnPropertyChanged(nameof(CurrentModeDisplay));
            }
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

        public bool UseCustomSession
        {
            get => _useCustomSession;
            set
            {
                _useCustomSession = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowCustomSettings));
                OnPropertyChanged(nameof(CurrentModeDisplay));
            }
        }

        public bool ShowCustomSettings => _useCustomSession && IsSessionMode;

        public bool ShowWipAlert
        {
            get => _showWipAlert;
            set
            {
                _showWipAlert = value;
                OnPropertyChanged();
            }
        }

        public int CountdownAlert
        {
            get => _countdownAlert;
            set
            {
                _countdownAlert = value;
                OnPropertyChanged();
            }
        }

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

        public int CustomPauseInterval
        {
            get => _customPauseInterval;
            set
            {
                _customPauseInterval = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> CurrentPalette { get; private set; } = new();
        public ObservableCollection<string> CurrentWords { get; private set; } = new();
        public ObservableCollection<ImageItem> CurrentImages { get; private set; } = new();
        public ObservableCollection<ContentSnapshot> PreviousContent { get; private set; } = new();

        #endregion

        #region Commands

        public ICommand PlayPauseCommand { get; private set; } = default!;
        public ICommand ResetCommand { get; private set; } = default!;
        public ICommand ToggleSettingsCommand { get; private set; } = default!;
        public ICommand UndoCommand { get; private set; } = default!;

        private void InitializeCommands()
        {
            PlayPauseCommand = new Command(ExecutePlayPause);
            ResetCommand = new Command(ExecuteReset);
            UndoCommand = new Command(ExecuteUndo, () => CanUndo);
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
            ShowWipAlert = false;

            OnPropertyChanged(nameof(TimeLeftDisplay));
            OnPropertyChanged(nameof(PlayPauseButtonText));
            OnPropertyChanged(nameof(PlayPauseButtonColor));
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
            }
        }

        private async Task StartTimerAsync()
        {
            SaveCurrentContent();
            GenerateNewContent();

            _timeLeft = GetTimerDuration();
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

            // Ensure UI updates happen on the main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(TimeLeftDisplay));
            });

            if (_timeLeft <= 0)
            {
                if (IsSessionMode && _timeLeft <= -30)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ShowWipAlert = true;
                        _ = StartCountdownAsync().ConfigureAwait(false);
                    });
                }
                else if (IsQuickMode)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        GenerateNewContent();
                        _timeLeft = GetTimerDuration();
                    });
                }
            }
        }

        private int GetTimerDuration()
        {
            return IsQuickMode
                ? _selectedQuickTime switch
                {
                    "30 sec" => 30,
                    "1 min" => 60,
                    "5 min" => 300,
                    "10 min" => 600,
                    "30 min" => 1800,
                    _ => 30
                }
                : _useCustomSession ? _customDuration * 60 : 30;
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

            var paletteModel = new PaletteModel();
            var random = new Random();
            var harmonyTypes = Enum.GetValues<PaletteModel.ColorHarmonyType>();
            var randomHarmonyType = harmonyTypes[random.Next(harmonyTypes.Length)];

            var generatedColors = paletteModel.HarmonyPaletteGenerator(randomHarmonyType, 0.15f);

            foreach (var color in generatedColors)
            {
                paletteModel.ColorToHsl(color, out float h, out float s, out float l);
                var hslString = $"hsl({h:F0}, {s * 100:F0}%, {l * 100:F0}%)";
                CurrentPalette.Add(hslString);
            }

            OnPropertyChanged(nameof(ShowPalette));
        }

        private void GenerateWords()
        {
            CurrentWords.Clear();

            var includeNouns = RandomBool();
            var includeSettings = RandomBool();
            var includeStyles = RandomBool();
            var includeThemes = RandomBool();

            if (!includeNouns && !includeSettings && !includeStyles && !includeThemes)
                includeNouns = true;

            var (prompt, components) = _promptGenerator.GeneratePrompt(
                includeNouns, includeSettings, includeStyles, includeThemes,
                nounCount: 3,
                settingMin: 1, settingMax: 2,
                styleMin: 1, styleMax: 2,
                themeCount: 1,
                settingProbability: 0.5f,
                themeProbability: 0.7f
            );

            foreach (var list in components.Values)
                foreach (var word in list)
                    if (!string.IsNullOrWhiteSpace(word))
                        CurrentWords.Add(word);
        }

        private bool RandomBool() => new Random().Next(2) == 0;

        private async Task ExecuteGenerateImagesAsync()
        {
            try
            {
                CurrentImages.Clear();
                var images = await _unsplash.GetRandomImagesAsync(count: 2);
                foreach (var image in images)
                    CurrentImages.Add(new ImageItem(image));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching images: {ex.Message}");
            }
        }

        private void SaveCurrentContent()
        {
            if (CurrentPalette.Count > 0 || CurrentWords.Count > 0 || CurrentImages.Count > 0)
            {
                var snapshot = new ContentSnapshot
                {
                    Palette = new List<string>(CurrentPalette),
                    Words = new List<string>(CurrentWords),
                    Images = new List<ImageItem>(CurrentImages),
                    Timestamp = DateTime.Now
                };

                PreviousContent.Insert(0, snapshot);
                if (PreviousContent.Count > 5)
                    PreviousContent.RemoveAt(5);

                OnPropertyChanged(nameof(CanUndo));
            }
        }

        private void InitializeData()
        {
            CurrentPalette = new ObservableCollection<string>();
            CurrentWords = new ObservableCollection<string>();
            CurrentImages = new ObservableCollection<ImageItem>();
            PreviousContent = new ObservableCollection<ContentSnapshot>();
        }

        private async Task StartCountdownAsync()
        {
            for (int i = 5; i > 0; i--)
            {
                CountdownAlert = i;
                await Task.Delay(1000);
            }

            ShowWipAlert = false;
            GenerateNewContent();
            _timeLeft = GetTimerDuration();
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
        public List<string>? Palette { get; set; }
        public List<string>? Words { get; set; }
        public List<ImageItem>? Images { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion
}