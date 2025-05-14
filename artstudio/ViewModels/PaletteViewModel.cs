using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using artstudio.Models;
using artstudio.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
//using artstudio.Data;
using SQLite;

namespace artstudio.ViewModels
{
    public class PaletteViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Swatch> Swatches { get; set; }

        // Commands
        public ICommand RegenerateCommand { get; }
        public ICommand ExportPaletteCommand { get; }
        public ICommand ToggleFavoritePaletteCommand { get; }


        // Services
        private readonly IFileSaveService _fileSaveService;
        private readonly Export _exportService;


        public PaletteViewModel(IFileSaveService fileSaveService, Export exportService)
        {
            _fileSaveService = fileSaveService ?? throw new ArgumentNullException(nameof(fileSaveService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));

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

            // Initialize PropertyChanged event to avoid nullability issues
            PropertyChanged += (sender, args) => { };
        }

        private void GeneratePalette()
        {
            var random = new Random();

            foreach (var swatch in Swatches)
            {
                if (swatch.IsDeleted)
                {
                    swatch.Color = swatch.PreviousColor;
                    swatch.IsDeleted = false;
                    swatch.IsActive = false;
                }

                if (!swatch.IsLocked)
                {
                    swatch.Color = Color.FromRgb(
                        random.Next(256),
                        random.Next(256),
                        random.Next(256));
                }

                swatch.OnPropertyChanged(nameof(swatch.Color));
                swatch.OnPropertyChanged(nameof(swatch.ButtonVisible));
                swatch.OnPropertyChanged(nameof(swatch.DeleteButtonVisible));
            }
        }

        // Favorite palette state --> Edit this once favorite has a place to store
        private bool _isFavoritePalette;
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

        // Export palette method
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
            if (currentPage == null)
            {
                return;
            }

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

        // Share palette method
        private async Task SharePaletteAsync(List<Color> activeColors)
        {
            var imageBytes = await Export.GeneratePaletteImageAsync(activeColors);
            var fileName = $"palette_{DateTime.Now:yyyyMMdd_HHmmss}.png";

            var tempFilePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllBytesAsync(tempFilePath, imageBytes);

            // Using MAUI's Share API
            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "Share Palette",
                File = new ShareFile(tempFilePath)
            });
        }

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string name = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }
}