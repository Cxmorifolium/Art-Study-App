using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using artstudio.Models;
using artstudio.Data;

namespace artstudio.ViewModels
{
    public partial class ImageItemViewModel : INotifyPropertyChanged
    {
        private readonly ILogger<ImageItemViewModel>? _logger;
        private readonly ImagePromptViewModel? _parentViewModel;
        private bool _isLocked;
        private bool _isDeleted;
        private bool _isFavorited;

        public UnsplashImage UnsplashImage { get; }

        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                if (_isLocked != value)
                {
                    _isLocked = value;
                    OnPropertyChanged();
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
                    OnPropertyChanged(nameof(DeleteOrUndoIcon));
                }
            }
        }
        public bool IsFavorited
        {
            get => _isFavorited;
            set
            {
                if (_isFavorited != value)
                {
                    _isFavorited = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DeleteOrUndoIcon => IsDeleted ? "undo.png" : "delete.png";
        public string ImageSource => UnsplashImage.urls?.Small ?? "placeholder_image.png";

        public string Description
        {
            get
            {
                string desc = UnsplashImage.Description ?? "Untitled";
                return desc.Length > 40 ? $"{desc[..40]}..." : desc;
            }
        }

        // Add properties for attribution and URL handling
        public string AttributionText => $"By {UnsplashImage.user?.Name ?? "Unknown"}";
        public string UserProfileUrl => UnsplashImage.user?.PortfolioUrl ?? string.Empty;
        public bool HasUserUrl => !string.IsNullOrEmpty(UserProfileUrl);

        // Add properties for image URL handling
        public string ImagePageUrl => !string.IsNullOrEmpty(UnsplashImage.Id)
            ? $"https://unsplash.com/photos/{UnsplashImage.Id}"
            : string.Empty;
        public bool HasImageUrl => !string.IsNullOrEmpty(ImagePageUrl);

        // Commands
        public ICommand OpenUserProfileCommand { get; }
        public ICommand OpenImagePageCommand { get; }
        public ICommand ToggleLockCommand { get; }
        public ICommand DeleteCommand { get; }

        // Wrapper command to delegate to the parent view model's commands since it's not working
        public ICommand ToggleFavoriteCommand { get; }
        public ICommand ParentToggleLockCommand { get; }
        public ICommand ParentDeleteCommand { get; }

        public ImageItemViewModel(UnsplashImage unsplashImage, ImagePromptViewModel? parentViewModel = null, ILogger<ImageItemViewModel>? logger = null)
        {
            UnsplashImage = unsplashImage ?? throw new ArgumentNullException(nameof(unsplashImage));
            _parentViewModel = parentViewModel;
            _logger = logger;

            OpenUserProfileCommand = new AsyncRelayCommand(OpenUserProfileAsync);
            OpenImagePageCommand = new AsyncRelayCommand(OpenImagePageAsync);
            ToggleLockCommand = new RelayCommand(ToggleLock);
            DeleteCommand = new AsyncRelayCommand(DeleteAsync);

            ToggleFavoriteCommand = new AsyncRelayCommand(async () =>
            {
                _logger?.LogDebug("ToggleFavoriteCommand called for image: {ImageId}", UnsplashImage.Id);
                if (_parentViewModel?.SaveToFavoritesCommand?.CanExecute(this) == true)
                {
                    await _parentViewModel.SaveToFavoritesCommand.ExecuteAsync(this);
                }
                else
                {
                    _logger?.LogWarning("SaveToFavoritesCommand not available or cannot execute");
                }
            });

            ParentToggleLockCommand = new RelayCommand(() =>
            {
                _logger?.LogDebug("ParentToggleLockCommand called for image: {ImageId}", UnsplashImage.Id);
                if (_parentViewModel?.ToggleLockCommand?.CanExecute(this) == true)
                {
                    _parentViewModel.ToggleLockCommand.Execute(this);
                }
            });

            ParentDeleteCommand = new AsyncRelayCommand(async () =>
            {
                _logger?.LogDebug("ParentDeleteCommand called for image: {ImageId}", UnsplashImage.Id);
                if (_parentViewModel?.DeleteImageCommand?.CanExecute(this) == true)
                {
                    await _parentViewModel.DeleteImageCommand.ExecuteAsync(this);
                }
            });
        }

        private void ToggleLock()
        {
            if (_parentViewModel?.ToggleLockCommand.CanExecute(this) == true)
            {
                _parentViewModel.ToggleLockCommand.Execute(this);
            }
        }

        private async Task DeleteAsync()
        {
            if (_parentViewModel?.DeleteImageCommand.CanExecute(this) == true)
            {
                await _parentViewModel.DeleteImageCommand.ExecuteAsync(this);
            }
        }

        private async Task OpenUserProfileAsync()
        {
            if (HasUserUrl)
            {
                try
                {
                    var success = await Launcher.OpenAsync(new Uri(UserProfileUrl));
                    if (!success)
                    {
                        _logger?.LogWarning("Failed to open user profile URL: {UserProfileUrl}", UserProfileUrl);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Exception opening user profile URL: {UserProfileUrl}", UserProfileUrl);
                }
            }
        }

        private async Task OpenImagePageAsync()
        {
            if (HasImageUrl)
            {
                try
                {
                    var success = await Launcher.OpenAsync(new Uri(ImagePageUrl));
                    if (!success)
                    {
                        _logger?.LogWarning("Failed to open image page URL: {ImagePageUrl}", ImagePageUrl);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Exception opening image page URL: {ImagePageUrl}", ImagePageUrl);
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}