using artstudio.Models;
using artstudio.Services;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace artstudio.ViewModels
{
    public class PaletteViewModel : INotifyPropertyChanged
    {
        #region Fields

        private readonly IFileSaveService _fileSaveService;
        private readonly Export _exportService;

        private bool _isFavoritePalette;
        private PaletteModel _paletteModel;

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
        #endregion

        #region Commands

        public ICommand RegenerateCommand { get; }
        public ICommand ExportPaletteCommand { get; }
        public ICommand ToggleFavoritePaletteCommand { get; }

        #endregion

        #region Constructor

        public PaletteViewModel(IFileSaveService fileSaveService, Export exportService)
        {
            _fileSaveService = fileSaveService ?? throw new ArgumentNullException(nameof(fileSaveService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _paletteModel = new PaletteModel();

            // Initialize swatches
            Swatches = new ObservableCollection<Swatch>
            {
                new Swatch(Colors.LightSalmon),
                new Swatch(Colors.SkyBlue),
                new Swatch(Colors.MediumSeaGreen),
                new Swatch(Colors.Goldenrod),
                new Swatch(Colors.MediumOrchid)
            };

            // Initialize commands
            RegenerateCommand = new Command(GeneratePalette);
            ToggleFavoritePaletteCommand = new Command(() => IsFavoritePalette = !IsFavoritePalette);
            ExportPaletteCommand = new Command(async () => await ExportPaletteAsync());

            // Prevent null event handlers
            PropertyChanged += (sender, args) => { };

            GeneratePalette();
        }

        #endregion

        #region Methods

        private void GeneratePalette()
        {        
            var random = new Random();
            var harmonyTypes = Enum.GetValues(typeof(PaletteModel.ColorHarmonyType)).Cast<PaletteModel.ColorHarmonyType>().ToArray();
            var randomHarmony = harmonyTypes[random.Next(harmonyTypes.Length)];
            // Collect current palette colors and locked states
            var existingColors = Swatches.Select(s => s.Color).ToList();
            var lockedFlags = Swatches.Select(s => s.IsLocked).ToArray();

            // Generate new palette using harmony type, with random factor (can be parameterized)
            var newPalette = _paletteModel.HarmonyPaletteGenerator(
                              randomHarmony,
                              randomFactor: 0.1f,
                              existingPalette: existingColors,
                              lockedColors: lockedFlags);

            // Update swatches only if not locked or deleted
            for (int i = 0; i < Swatches.Count; i++)
            {
                if (Swatches[i].IsDeleted)
                {
                    // Restore previous color if deleted
                    Swatches[i].Color = Swatches[i].PreviousColor;
                    Swatches[i].IsDeleted = false;
                    Swatches[i].IsActive = false;
                }

                if (!Swatches[i].IsLocked)
                {
                    Swatches[i].Color = newPalette[i];
                }

                Swatches[i].OnPropertyChanged(nameof(Swatch.Color));
                Swatches[i].OnPropertyChanged(nameof(Swatch.ButtonVisible));
                Swatches[i].OnPropertyChanged(nameof(Swatch.DeleteButtonVisible));
            }

        }

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

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }
}
