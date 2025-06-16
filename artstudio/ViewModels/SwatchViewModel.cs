using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using artstudio.Services;
using artstudio.Models;

namespace artstudio.ViewModels
{
    public class SwatchViewModel : INotifyPropertyChanged
    {
        private readonly SwatchModel _model;
        private readonly PaletteService _paletteService;
        private readonly IToastService _toastService;
        private bool _isFavoriteColor;

        public SwatchViewModel(SwatchModel model, PaletteService paletteService, IToastService toastService)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _paletteService = paletteService ?? throw new ArgumentNullException(nameof(paletteService));
            _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

            // Initialize commands
            ToggleFavoriteColorCommand = new AsyncRelayCommand(ToggleFavoriteColorAsync);
            ToggleLockCommand = new Command(ToggleLock);
            ToggleDeleteCommand = new Command(ToggleDelete);
            ToggleActivateCommand = new Command(ToggleActivate);
        }

        #region Model Property Wrappers

        public Color Color
        {
            get => _model.Color;
            set
            {
                if (_model.CanUpdateColor() && _model.Color != value)
                {
                    _model.Color = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HexCode));
                }
            }
        }

        public Color PreviousColor
        {
            get => _model.PreviousColor;
            set
            {
                _model.PreviousColor = value;
                OnPropertyChanged();
            }
        }

        public bool IsLocked
        {
            get => _model.IsLocked;
            private set
            {
                if (_model.IsLocked != value)
                {
                    _model.IsLocked = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LockImage));
                }
            }
        }

        public bool IsDeleted
        {
            get => _model.IsDeleted;
            private set
            {
                if (_model.IsDeleted != value)
                {
                    _model.IsDeleted = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DeleteImage));
                    OnPropertyChanged(nameof(ButtonVisible));
                    OnPropertyChanged(nameof(DeleteButtonVisible));
                }
            }
        }

        public bool IsActive
        {
            get => _model.IsActive;
            private set
            {
                if (_model.IsActive != value)
                {
                    _model.IsActive = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ButtonVisible));
                    OnPropertyChanged(nameof(DeleteButtonVisible));
                }
            }
        }

        public string HexCode => _model.HexCode;

        #endregion

        #region UI-Specific Properties

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

        // View binding properties
        public ImageSource FavoriteColor => ImageSource.FromFile(IsFavoriteColor ? "heart.png" : "unheart.png");
        public ImageSource LockImage => ImageSource.FromFile(IsLocked ? "padlock.png" : "unlock.png");
        public ImageSource DeleteImage => ImageSource.FromFile(IsDeleted ? "undo.png" : "delete.png");

        public bool ButtonVisible => !IsDeleted && IsActive;
        public bool DeleteButtonVisible => IsActive || IsDeleted;

        #endregion

        #region Commands

        public ICommand ToggleFavoriteColorCommand { get; }
        public ICommand ToggleLockCommand { get; }
        public ICommand ToggleDeleteCommand { get; }
        public ICommand ToggleActivateCommand { get; }

        #endregion

        #region Command Implementations

        public async Task ToggleFavoriteColorAsync()
        {
            try
            {
                var hexColor = Color.ToHex();

                if (!IsFavoriteColor)
                {
                    // Try to add to favorites
                    bool wasAdded = await _paletteService.AddSwatchToFavoritesAsync(hexColor);

                    if (wasAdded)
                    {
                        IsFavoriteColor = true;
                        await _toastService.ShowToastAsync($"Added {hexColor} to favorites! ⭐");
                        System.Diagnostics.Debug.WriteLine($"Successfully added {hexColor} to favorites");
                    }
                    else
                    {
                        // Color was already favorited
                        IsFavoriteColor = true; // Update UI to reflect it's actually favorited
                        await _toastService.ShowToastAsync($"{hexColor} is already in favorites!");
                        System.Diagnostics.Debug.WriteLine($"Color {hexColor} was already favorited");
                    }
                }
                else
                {
                    // Remove from favorites
                    var favoriteSwatches = await _paletteService.GetFavoriteSwatchesAsync();
                    var existingSwatch = favoriteSwatches.FirstOrDefault(s =>
                        s.HexColor.Equals(hexColor, StringComparison.OrdinalIgnoreCase));

                    if (existingSwatch != null)
                    {
                        await _paletteService.RemoveSwatchFromFavoritesAsync(existingSwatch.Id);
                        IsFavoriteColor = false;
                        await _toastService.ShowToastAsync($"Removed {hexColor} from favorites");
                        System.Diagnostics.Debug.WriteLine($"Removed {hexColor} from favorites");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling favorite color: {ex.Message}");
                await _toastService.ShowToastAsync("Error updating favorites");
            }
        }

        private void ToggleLock()
        {
            _model.ToggleLock();
            IsLocked = _model.IsLocked; // Trigger property change notifications
        }

        private void ToggleDelete()
        {
            _model.Delete();

            // Update all affected properties
            IsDeleted = _model.IsDeleted;
            IsActive = _model.IsActive;
            OnPropertyChanged(nameof(Color));
        }

        private void ToggleActivate()
        {
            IsActive = !IsActive;
        }

        #endregion

        #region Public Methods

        // Method to check and update favorite status from database
        public async Task RefreshFavoriteStatusAsync()
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

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }
}