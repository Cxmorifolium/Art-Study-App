using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using artstudio.Services;
using artstudio.Models;

namespace artstudio.ViewModels
{
    public class SwatchViewModel : INotifyPropertyChanged
    {
        private readonly SwatchModel _model;
        private readonly PaletteService _paletteService;
        private readonly IToastService _toastService;
        private readonly ILogger<SwatchViewModel> _logger;
        private bool _isFavoriteColor;

        public static event Action<string, bool>? FavoriteChanged;

        public SwatchViewModel(
            SwatchModel model,
            PaletteService paletteService,
            IToastService toastService,
            ILogger<SwatchViewModel> logger)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _paletteService = paletteService ?? throw new ArgumentNullException(nameof(paletteService));
            _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
                    _logger.LogDebug("Color changed to {HexColor}", value.ToHex());
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
                        // Direct state update 
                        IsFavoriteColor = true;
                        await _toastService.ShowToastAsync($"Added {hexColor} to favorites! ⭐");
                        _logger.LogInformation("Successfully added {HexColor} to favorites", hexColor);

                        // Fire event for immediate UI update
                        FavoriteChanged?.Invoke(hexColor, true);
                    }
                    else
                    {
                        // Color was already favorited
                        IsFavoriteColor = true;
                        await _toastService.ShowToastAsync($"{hexColor} is already in favorites!");
                        _logger.LogInformation("Color {HexColor} was already favorited", hexColor);
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

                        // ✅ Direct state update - no refresh calls
                        IsFavoriteColor = false;
                        await _toastService.ShowToastAsync($"Removed {hexColor} from favorites");
                        _logger.LogInformation("Removed {HexColor} from favorites", hexColor);

                        // ✅ Fire event for immediate UI update
                        FavoriteChanged?.Invoke(hexColor, false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling favorite color");
                await _toastService.ShowToastAsync("Error updating favorites");
            }
        }

        private void ToggleLock()
        {
            try
            {
                _model.ToggleLock();

                OnPropertyChanged(nameof(IsLocked));
                OnPropertyChanged(nameof(LockImage));
                _logger.LogInformation("Lock toggled. IsLocked: {IsLocked}", _model.IsLocked);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling lock state");
            }
        }

        private void ToggleDelete()
        {
            try
            {
                _model.Delete();

                // CRITICAL: Update ALL affected properties manually
                IsDeleted = _model.IsDeleted;
                IsActive = _model.IsActive;
                OnPropertyChanged(nameof(Color));

                // Make sure ButtonVisible gets updated
                OnPropertyChanged(nameof(ButtonVisible));
                OnPropertyChanged(nameof(DeleteButtonVisible));
                OnPropertyChanged(nameof(DeleteImage));

                _logger.LogInformation("Delete toggled. IsDeleted: {IsDeleted}, IsActive: {IsActive}", IsDeleted, IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling delete state");
            }
        }

        private void ToggleActivate()
        {
            bool wasActive = IsActive;
            IsActive = !IsActive;

            // When swatch becomes active, check if it's favorited
            if (!wasActive && IsActive)
            {
                _ = CheckFavoriteStatusOnActivationAsync();
            }

            _logger.LogInformation("Swatch activated/deactivated. IsActive: {IsActive}", IsActive);
        }

        private static readonly Dictionary<string, bool> _favoriteCache = new();

        public static void ClearFavoriteCache(string hexColor)
        {
            _favoriteCache.Remove(hexColor);
        }

        private async Task CheckFavoriteStatusOnActivationAsync()
        {
            try
            {
                var hexColor = Color.ToHex();

                // Check cache first
                if (_favoriteCache.TryGetValue(hexColor, out bool cachedResult))
                {
                    IsFavoriteColor = cachedResult;
                    _logger.LogDebug("Used cached favorite status: {HexColor} = {IsFavorite}", hexColor, cachedResult);
                    return;
                }

                // Hit database if not cached
                bool isFavorite = await _paletteService.IsSwatchFavoriteAsync(Color);
                IsFavoriteColor = isFavorite;
                _favoriteCache[hexColor] = isFavorite; // Cache the result

                _logger.LogDebug("Checked favorite status: {HexColor} = {IsFavorite}", hexColor, isFavorite);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking favorite status on activation");
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