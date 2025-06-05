using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using artstudio.Services;

namespace artstudio.Models
{
    public class Swatch : INotifyPropertyChanged
    {
        private Color _color;
        private bool _isFavoriteColor;
        private bool _isLocked;
        private bool _isDeleted;
        private Color _previousColor;

        // Services for database operations
        private PaletteService? _paletteService;
        private IToastService? _toastService;

        public Color Color
        {
            get => _color;
            set
            {
                if (!_isLocked && _color != value)
                {
                    _color = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HexCode));
                }
            }
        }

        public Color PreviousColor
        {
            get => _previousColor;
            set
            {
                _previousColor = value;
                OnPropertyChanged();
            }
        }

        public string HexCode => Color.ToHex();
        public ImageSource FavoriteColor => ImageSource.FromFile(IsFavoriteColor ? "heart.png" : "unheart.png");
        public ImageSource LockImage => ImageSource.FromFile(IsLocked ? "padlock.png" : "unlock.png");
        public ImageSource DeleteImage => ImageSource.FromFile(IsDeleted ? "undo.png" : "delete.png");

        public bool ButtonVisible => !IsDeleted && IsActive;
        public bool DeleteButtonVisible => IsActive || IsDeleted;

        public bool IsFavoriteColor
        {
            get => _isFavoriteColor;
            set
            {
                if (_isFavoriteColor != value)
                {
                    _isFavoriteColor = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FavoriteColor));
                }
            }
        }

        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                if (_isLocked != value)
                {
                    _isLocked = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LockImage));
                }
            }
        }

        public bool IsDeleted
        {
            get => _isDeleted;
            set
            {
                if (_isDeleted != value)
                {
                    _isDeleted = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DeleteImage));
                    OnPropertyChanged(nameof(ButtonVisible));
                }
            }
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ButtonVisible));
                    OnPropertyChanged(nameof(DeleteButtonVisible));
                }
            }
        }

        public ICommand ToggleFavoriteColorCommand { get; }
        public ICommand ToggleLockCommand { get; }
        public ICommand ToggleDeleteCommand { get; }
        public ICommand ToggleActivateCommand { get; }

        public Swatch(Color color)
        {
            _color = color;
            _previousColor = Colors.Transparent;

            // Use AsyncRelayCommand for the favorite command to handle database operations
            ToggleFavoriteColorCommand = new AsyncRelayCommand(ToggleFavoriteColorAsync);
            ToggleLockCommand = new Command(() => IsLocked = !IsLocked);
            ToggleDeleteCommand = new Command(ToggleDelete);
            ToggleActivateCommand = new Command(() => IsActive = !IsActive);
        }

        // Method to inject services (call this from your ViewModel)
        public void SetServices(PaletteService paletteService, IToastService toastService)
        {
            _paletteService = paletteService;
            _toastService = toastService;
        }

        // Method to check and update favorite status from database
        public async Task RefreshFavoriteStatusAsync()
        {
            if (_paletteService != null)
            {
                try
                {
                    bool isFavorite = await _paletteService.IsSwatchFavoriteAsync(Color);
                    IsFavoriteColor = isFavorite;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error refreshing favorite status: {ex.Message}");
                    // Reset to unfavorited on error
                    IsFavoriteColor = false;
                }
            }
            else
            {
                // Reset to unfavorited if no service available
                IsFavoriteColor = false;
            }
        }

        private async Task ToggleFavoriteColorAsync()
        {
            try
            {
                if (!IsFavoriteColor)
                {
                    // Adding to favorites - show options and save to database
                    if (_paletteService != null && _toastService != null)
                    {
                        var mainPage = Application.Current?.Windows.FirstOrDefault()?.Page;
                        if (mainPage != null)
                        {
                            string action = await mainPage.DisplayActionSheet(
                                "Save Color to Favorites",
                                "Cancel",
                                null,
                                "Save to Default",
                                "Save to Collection"
                            );

                            if (action == "Save to Default")
                            {
                                await _paletteService.SaveSwatchToFavoritesAsync(Color);
                                IsFavoriteColor = true;
                                await _toastService.ShowToastAsync("Color saved to favorites! 💖");
                            }
                            else if (action == "Save to Collection")
                            {
                                string collectionName = await mainPage.DisplayPromptAsync(
                                    "Collection Name",
                                    "Enter collection name:",
                                    "Save",
                                    "Cancel",
                                    placeholder: "e.g., Blues, Reds, Nature"
                                );

                                if (!string.IsNullOrWhiteSpace(collectionName))
                                {
                                    await _paletteService.SaveSwatchToFavoritesAsync(
                                        Color,
                                        collection: collectionName.Trim());
                                    IsFavoriteColor = true;
                                    await _toastService.ShowToastAsync($"Color saved to '{collectionName}' collection! 💖");
                                }
                            }
                        }
                    }
                    else
                    {
                        // Fallback: just toggle visually if services aren't available
                        IsFavoriteColor = true;
                    }
                }
                else
                {
                    // Removing from favorites
                    IsFavoriteColor = false;
                    if (_toastService != null)
                    {
                        await _toastService.ShowToastAsync("Color removed from favorites");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling swatch favorite: {ex.Message}");
                if (_toastService != null)
                {
                    await _toastService.ShowToastAsync("Error saving color");
                }
            }
        }

        private void ToggleDelete()
        {
            if (IsDeleted)
            {
                Color = _previousColor;
                IsDeleted = false;
                IsActive = false;
            }
            else
            {
                _previousColor = Color;
                Color = Colors.Transparent;
                IsDeleted = true;
                IsActive = false;
            }

            OnPropertyChanged(nameof(Color));
            OnPropertyChanged(nameof(ButtonVisible));
            OnPropertyChanged(nameof(DeleteButtonVisible));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string name = null!) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}