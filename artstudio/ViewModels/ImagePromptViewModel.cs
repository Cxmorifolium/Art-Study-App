using artstudio.Models;
using artstudio.Services;
using artstudio.Data;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace artstudio.ViewModels
{
    public partial class ImagePromptViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly Unsplash _unsplashService;
        private readonly DatabaseService _databaseService;
        private readonly IToastService _toastService;
        private const int DefaultImageCount = 3;
        private const int AdditionalImages = 1;
        private bool _isLoading;
        private bool _disposed = false;
        private readonly ILogger<ImagePromptViewModel> _logger;

        // Favorites flyout properties
        private bool _isFavoritesFlyoutVisible = false;
        private bool _isLoadingFavorites = false;

        private readonly Stack<(ImageItemViewModel item, int index)> _undoStack = new();

        public ObservableCollection<ImageItemViewModel> Images { get; } = new();
        public ObservableCollection<FavoriteImageItem> FavoriteImages { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                    CommandsCanExecuteChanged();
                }
            }
        }

        public bool IsFavoritesFlyoutVisible
        {
            get => _isFavoritesFlyoutVisible;
            set
            {
                if (SetProperty(ref _isFavoritesFlyoutVisible, value))
                {
                    _logger.LogDebug("IsFavoritesFlyoutVisible changed to: {Value}", value);
                }
            }
        }

        public bool IsLoadingFavorites
        {
            get => _isLoadingFavorites;
            set => SetProperty(ref _isLoadingFavorites, value);
        }

        public bool HasNoFavorites => FavoriteImages.Count == 0 && !IsLoadingFavorites;

        public bool CanUndo => _undoStack.Count > 0;

        #region Commands

        public IRelayCommand LoadInitialImagesCommand { get; }
        public IRelayCommand AddImagesCommand { get; }
        public IRelayCommand RegenerateImagesCommand { get; }
        public IRelayCommand<ImageItemViewModel> ToggleLockCommand { get; }
        public IAsyncRelayCommand<ImageItemViewModel> DeleteImageCommand { get; }
        public IRelayCommand UndoDeleteCommand { get; }

        // Favorites commands
        public IAsyncRelayCommand ToggleFavoritesFlyoutCommand { get; }
        public IRelayCommand CloseFavoritesFlyoutCommand { get; }
        public IAsyncRelayCommand<ImageItemViewModel> SaveToFavoritesCommand { get; }
        public IAsyncRelayCommand<FavoriteImageItem> LoadFromFavoritesCommand { get; }
        public IAsyncRelayCommand<FavoriteImageItem> RemoveFromFavoritesCommand { get; }

        #endregion

        public ImagePromptViewModel(ILogger<ImagePromptViewModel> logger, DatabaseService databaseService, IToastService toastService)
        {
            _logger = logger;
            _databaseService = databaseService;
            _toastService = toastService;
            _unsplashService = new Unsplash();

            LoadInitialImagesCommand = new RelayCommand(ExecuteLoadInitialImages, () => !IsLoading);
            AddImagesCommand = new RelayCommand(ExecuteAddImages, () => !IsLoading);
            RegenerateImagesCommand = new RelayCommand(ExecuteRegenerateImages, () => !IsLoading);
            ToggleLockCommand = new RelayCommand<ImageItemViewModel>(ToggleLock);
            DeleteImageCommand = new AsyncRelayCommand<ImageItemViewModel>(DeleteImageAsync);
            UndoDeleteCommand = new RelayCommand(UndoDelete, () => CanUndo);

            // Initialize favorites commands with better debugging
            ToggleFavoritesFlyoutCommand = new AsyncRelayCommand(ExecuteToggleFavoritesFlyoutAsync);
            CloseFavoritesFlyoutCommand = new RelayCommand(ExecuteCloseFavoritesFlyout);
            SaveToFavoritesCommand = new AsyncRelayCommand<ImageItemViewModel>(ExecuteSaveToFavoritesAsync);
            LoadFromFavoritesCommand = new AsyncRelayCommand<FavoriteImageItem>(ExecuteLoadFromFavoritesAsync);
            RemoveFromFavoritesCommand = new AsyncRelayCommand<FavoriteImageItem>(ExecuteRemoveFromFavoritesAsync);

            // Load initial images on main thread
            MainThread.BeginInvokeOnMainThread(ExecuteLoadInitialImages);


        }

        #region Favorites Command Implementations

        private async Task ExecuteToggleFavoritesFlyoutAsync()
        {
            try
            {
                if (!IsFavoritesFlyoutVisible)
                {
                    await LoadFavoriteImagesAsync();
                }
                IsFavoritesFlyoutVisible = !IsFavoritesFlyoutVisible;
                _logger.LogDebug("Favorites flyout toggled: {IsVisible}", IsFavoritesFlyoutVisible);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling favorites flyout");
                await _toastService.ShowToastAsync("Error loading favorite images");
            }
        }

        private void ExecuteCloseFavoritesFlyout()
        {
            try
            {
                _logger.LogDebug("Close favorites flyout command executed");
                IsFavoritesFlyoutVisible = false;
                _logger.LogDebug("IsFavoritesFlyoutVisible set to false");

                // Force property change notification
                OnPropertyChanged(nameof(IsFavoritesFlyoutVisible));
                _logger.LogDebug("Property change notification sent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing favorites flyout");
            }
        }

        // Updated save to favorites
        private async Task ExecuteSaveToFavoritesAsync(ImageItemViewModel? imageItem)
        {
            try
            {
                if (imageItem?.UnsplashImage == null || string.IsNullOrEmpty(imageItem.UnsplashImage.Id))
                {
                    _logger.LogWarning("Cannot save to favorites: invalid image item");
                    return;
                }

                var image = imageItem.UnsplashImage;
                _logger.LogDebug("Toggling favorite status for image: {ImageId}", image.Id);

                // Check if already favorited in the database
                var existingFavorite = await _databaseService.GetFavoriteByUnsplashIdAsync(image.Id!);

                if (existingFavorite != null)
                {
                    _logger.LogDebug("Removing from favorites - Found existing favorite with ID: {FavoriteId}", existingFavorite.Id);

                    // FIXED: Use the ID from the existing favorite record
                    if (existingFavorite.Id > 0)
                    {
                        var deleteResult = await _databaseService.DeleteFavoriteImageAsync(existingFavorite.Id);
                        _logger.LogDebug("Database delete result: {Result}", deleteResult);
                    }
                    else
                    {
                        // Fallback: delete by UnsplashId if ID is invalid
                        _logger.LogWarning("Existing favorite has invalid ID, using UnsplashId delete");
                        await _databaseService.DeleteFavoriteImageByUnsplashIdAsync(image.Id!);
                    }

                    // Update UI on main thread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        // Remove from in-memory collection
                        var favoriteToRemove = FavoriteImages.FirstOrDefault(f => f.UnsplashId == image.Id);
                        if (favoriteToRemove != null)
                        {
                            FavoriteImages.Remove(favoriteToRemove);
                            _logger.LogDebug("Removed from FavoriteImages collection");
                        }

                        // Update the image item's favorite status
                        imageItem.IsFavorited = false;
                        _logger.LogDebug("Set imageItem.IsFavorited = false");

                        // Notify property changes
                        OnPropertyChanged(nameof(HasNoFavorites));
                        OnPropertyChanged(nameof(FavoriteImages));
                    });

                    await _toastService.ShowToastAsync("Removed from favorites");
                    _logger.LogDebug("Successfully removed image from favorites: {ImageId}", image.Id);
                }
                else
                {
                    // Add to favorites
                    _logger.LogDebug("Adding to favorites: {ImageId}", image.Id);

                    var cachedImage = await CacheImageLocallyAsync(image);
                    var favoriteImage = new FavoriteImageItem
                    {
                        UnsplashId = image.Id ?? Guid.NewGuid().ToString(),
                        Title = image.Description ?? "Untitled Image",
                        Description = image.Description,
                        UserName = image.user?.Name,
                        UserPortfolioUrl = image.user?.PortfolioUrl,
                        LocalImagePath = cachedImage.urls?.Thumb,
                        OriginalUrl = image.urls?.Regular,
                        CreatedAt = DateTime.Now,
                        IsFavorite = true
                    };

                    var savedId = await _databaseService.SaveFavoriteImageAsync(favoriteImage);
                    _logger.LogDebug("Saved to database with ID: {SavedId}", savedId);

                    // Update UI on main thread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        FavoriteImages.Add(favoriteImage);
                        _logger.LogDebug("Added to FavoriteImages collection");

                        // Update the image item's favorite status
                        imageItem.IsFavorited = true;
                        _logger.LogDebug("Set imageItem.IsFavorited = true");

                        // Notify property changes
                        OnPropertyChanged(nameof(HasNoFavorites));
                        OnPropertyChanged(nameof(FavoriteImages));
                    });

                    await _toastService.ShowToastAsync($"Added to favorites: {favoriteImage.DisplayTitle}");
                    _logger.LogDebug("Successfully added image to favorites: {ImageId}", image.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling image favorite status for image: {ImageId}",
                    imageItem?.UnsplashImage?.Id ?? "unknown");
                await _toastService.ShowToastAsync("Error updating favorites");
            }
        }

        private async Task ExecuteLoadFromFavoritesAsync(FavoriteImageItem? favoriteImage)
        {
            try
            {
                if (favoriteImage == null)
                {
                    _logger.LogWarning("Cannot load: favoriteImage is null");
                    return;
                }

                _logger.LogDebug("Loading favorite image to main view: {ImageId}", favoriteImage.UnsplashId);

                // Check if image already exists in main collection to prevent duplicates
                var existingImage = Images.FirstOrDefault(img => img.UnsplashImage.Id == favoriteImage.UnsplashId);
                if (existingImage != null)
                {
                    await _toastService.ShowToastAsync("Image already loaded in main view");
                    IsFavoritesFlyoutVisible = false;
                    _logger.LogDebug("Image already exists in main collection: {ImageId}", favoriteImage.UnsplashId);
                    return;
                }

                // Create UnsplashImage from favorite
                var unsplashImage = new UnsplashImage
                {
                    Id = favoriteImage.UnsplashId,
                    Description = favoriteImage.Description,
                    user = new UnsplashImage.User
                    {
                        Name = favoriteImage.UserName,
                        PortfolioUrl = favoriteImage.UserPortfolioUrl
                    },
                    urls = new UnsplashImage.Urls
                    {
                        Thumb = favoriteImage.LocalImagePath,
                        Regular = favoriteImage.OriginalUrl,
                        Small = favoriteImage.LocalImagePath,
                        Full = favoriteImage.OriginalUrl,
                        Raw = favoriteImage.OriginalUrl
                    }
                };

                // Add to current images on main thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var imageItem = new ImageItemViewModel(unsplashImage, this);
                    imageItem.IsFavorited = true; // Set to true since it's from favorites
                    Images.Add(imageItem);
                    _logger.LogDebug("Added image to main collection with IsFavorited = true");
                });

                IsFavoritesFlyoutVisible = false;
                await _toastService.ShowToastAsync($"Added: {favoriteImage.DisplayTitle}");
                _logger.LogDebug("Successfully loaded favorite image: {ImageId}", favoriteImage.UnsplashId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading favorite image");
                await _toastService.ShowToastAsync("Error loading favorite image");
            }
        }

        private async Task ExecuteRemoveFromFavoritesAsync(FavoriteImageItem? favoriteImage)
        {
            try
            {
                if (favoriteImage == null)
                {
                    _logger.LogWarning("Cannot remove: favoriteImage is null");
                    return;
                }

                _logger.LogDebug("Removing from favorites: {ImageId}, FavoriteId: {FavoriteId}",
                    favoriteImage.UnsplashId, favoriteImage.Id);

                // FIXED: Ensure we have a valid ID before attempting delete
                if (favoriteImage.Id <= 0)
                {
                    _logger.LogWarning("Invalid favorite ID: {FavoriteId}, using UnsplashId delete instead", favoriteImage.Id);
                    await _databaseService.DeleteFavoriteImageByUnsplashIdAsync(favoriteImage.UnsplashId);
                }
                else
                {
                    // Remove from database using the proper ID
                    await _databaseService.DeleteFavoriteImageAsync(favoriteImage.Id);
                }

                _logger.LogDebug("Successfully removed from database: {ImageId}", favoriteImage.UnsplashId);

                // Update UI on main thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Remove from collection
                    var removed = FavoriteImages.Remove(favoriteImage);
                    _logger.LogDebug("Removed from FavoriteImages collection: {Success}", removed);

                    // Update corresponding main image to show unfavorited (unheart)
                    var mainImage = Images.FirstOrDefault(img => img.UnsplashImage.Id == favoriteImage.UnsplashId);
                    if (mainImage != null)
                    {
                        mainImage.IsFavorited = false;
                        _logger.LogDebug("Updated main image IsFavorited = false: {ImageId}", favoriteImage.UnsplashId);
                    }

                    // Notify property changes
                    OnPropertyChanged(nameof(HasNoFavorites));
                    OnPropertyChanged(nameof(FavoriteImages));
                });

                await _toastService.ShowToastAsync("Removed from favorites");
                _logger.LogDebug("Successfully removed from favorites: {ImageId}", favoriteImage.UnsplashId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from favorites: {ImageId}, FavoriteId: {FavoriteId}",
                    favoriteImage?.UnsplashId ?? "unknown", favoriteImage?.Id ?? -1);
                await _toastService.ShowToastAsync("Error removing from favorites");
            }
        }
        private async Task LoadFavoriteImagesAsync()
        {
            try
            {
                IsLoadingFavorites = true;
                OnPropertyChanged(nameof(HasNoFavorites));
                _logger.LogDebug("Loading favorite images from database");

                var favorites = await _databaseService.GetFavoriteImagesAsync();
                _logger.LogDebug("Retrieved {Count} favorites from database", favorites.Count);

                // Update UI on main thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    FavoriteImages.Clear();
                    foreach (var favorite in favorites)
                    {
                        FavoriteImages.Add(favorite);
                    }
                    _logger.LogDebug("Updated FavoriteImages collection with {Count} items", FavoriteImages.Count);

                    // Update existing main images to show correct favorite status
                    foreach (var imageItem in Images)
                    {
                        var isFavorited = favorites.Any(f => f.UnsplashId == imageItem.UnsplashImage.Id);
                        if (imageItem.IsFavorited != isFavorited)
                        {
                            imageItem.IsFavorited = isFavorited;
                            _logger.LogDebug("Updated favorite status for main image: {ImageId} = {IsFavorited}",
                                imageItem.UnsplashImage.Id, isFavorited);
                        }
                    }

                    OnPropertyChanged(nameof(HasNoFavorites));
                    OnPropertyChanged(nameof(FavoriteImages));
                });

                _logger.LogDebug("Successfully loaded {FavoriteCount} favorite images", favorites.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading favorite images");
                await _toastService.ShowToastAsync("Error loading favorite images");
            }
            finally
            {
                IsLoadingFavorites = false;
                OnPropertyChanged(nameof(HasNoFavorites));
            }
        }

        private async Task<UnsplashImage> CacheImageLocallyAsync(UnsplashImage image)
        {
            try
            {
                var thumbUrl = image.urls?.Thumb;
                if (string.IsNullOrEmpty(thumbUrl))
                {
                    _logger.LogWarning("No thumbnail URL for image {ImageId}", image.Id);
                    return image;
                }

                // Create local file path
                var fileName = $"fav_{image.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                var localPath = Path.Combine(FileSystem.AppDataDirectory, "FavoriteImages", fileName);

                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

                // Download and save thumbnail
                using (var httpClient = new HttpClient())
                {
                    var imageBytes = await httpClient.GetByteArrayAsync(thumbUrl);
                    await File.WriteAllBytesAsync(localPath, imageBytes);
                }

                // Return image with local path
                return new UnsplashImage
                {
                    Id = image.Id,
                    Description = image.Description,
                    user = image.user,
                    urls = new UnsplashImage.Urls
                    {
                        Raw = image.urls?.Raw,
                        Full = image.urls?.Full,
                        Regular = image.urls?.Regular,
                        Small = image.urls?.Small,
                        Thumb = localPath
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cache image locally: {ImageId}", image.Id);
                return image;
            }
        }

        #endregion

        #region Command Wrappers

        private void ExecuteLoadInitialImages()
        {
            _ = LoadInitialImagesAsync();
        }

        private void ExecuteAddImages()
        {
            _ = AddImagesAsync();
        }

        private void ExecuteRegenerateImages()
        {
            _ = RegenerateImagesAsync();
        }

        #endregion

        #region Command Logic - Fixed to prevent image doubling
        private async Task LoadInitialImagesAsync()
        {
            try
            {
                IsLoading = true;

                // Clear existing images to prevent duplication
                Images.Clear();

                var images = await _unsplashService.GetRandomImagesAsync(DefaultImageCount);

                // Get current favorites to set initial favorite status
                var currentFavorites = await _databaseService.GetFavoriteImagesAsync();
                _logger.LogDebug("Found {Count} existing favorites when loading initial images", currentFavorites.Count);

                // Ensure we're on the main thread when modifying the collection
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    foreach (var image in images)
                    {
                        var imageItem = new ImageItemViewModel(image, this);

                        // Check if this image is already favorited
                        var isFavorited = currentFavorites.Any(f => f.UnsplashId == image.Id);
                        imageItem.IsFavorited = isFavorited;

                        _logger.LogDebug("Loading image {ImageId} with favorite status: {IsFavorited}",
                            image.Id, isFavorited);

                        Images.Add(imageItem);
                    }
                });
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _logger.LogError(ex, "Invalid image count parameter.");
                await ShowErrorMessageAsync("Invalid image count. Please try again.");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "API key issue - unable to access Unsplash service.");
                await ShowErrorMessageAsync("Unable to access image service. Please check your connection and try again.");
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "Request timeout when fetching images.");
                await ShowErrorMessageAsync("Request timed out. Please check your connection and try again.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error.");
                await ShowErrorMessageAsync("Network error. Please check your connection and try again.");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Service error.");
                await ShowErrorMessageAsync("Service temporarily unavailable. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching images.");
                await ShowErrorMessageAsync("An unexpected error occurred. Please try again.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AddImagesAsync()
        {
            if (IsLoading)
            {
                _logger.LogDebug("Skipped AddImagesAsync because IsLoading is true.");
                return;
            }

            try
            {
                IsLoading = true;
                var images = await _unsplashService.GetRandomImagesAsync(AdditionalImages);

                // Get current favorites to set initial favorite status
                var currentFavorites = await _databaseService.GetFavoriteImagesAsync();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    foreach (var image in images)
                    {
                        var imageItem = new ImageItemViewModel(image, this);

                        // Check if this image is already favorited
                        var isFavorited = currentFavorites.Any(f => f.UnsplashId == image.Id);
                        imageItem.IsFavorited = isFavorited;

                        _logger.LogDebug("Adding image {ImageId} with favorite status: {IsFavorited}",
                            image.Id, isFavorited);

                        Images.Add(imageItem);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error adding images.");
                await ShowErrorMessageAsync("An unexpected error occurred. Please try again.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RegenerateImagesAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                var unlockedIndices = new List<int>();

                for (int i = 0; i < Images.Count; i++)
                {
                    if (!Images[i].IsLocked)
                        unlockedIndices.Add(i);
                }

                if (unlockedIndices.Count == 0) return;

                var newImages = await _unsplashService.GetRandomImagesAsync(unlockedIndices.Count);

                // Get current favorites to set initial favorite status
                var currentFavorites = await _databaseService.GetFavoriteImagesAsync();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    for (int i = 0; i < unlockedIndices.Count && i < newImages.Count; i++)
                    {
                        int index = unlockedIndices[i];
                        if (index < Images.Count)
                        {
                            var imageItem = new ImageItemViewModel(newImages[i], this);

                            // Check if this image is already favorited
                            var isFavorited = currentFavorites.Any(f => f.UnsplashId == newImages[i].Id);
                            imageItem.IsFavorited = isFavorited;

                            _logger.LogDebug("Regenerating image at index {Index} with {ImageId}, favorite status: {IsFavorited}",
                                index, newImages[i].Id, isFavorited);

                            Images[index] = imageItem;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error regenerating images.");
                await ShowErrorMessageAsync("An unexpected error occurred. Please try again.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ToggleLock(ImageItemViewModel? imageItem)
        {
            if (imageItem != null)
            {
                imageItem.IsLocked = !imageItem.IsLocked;
            }
        }

        private async Task DeleteImageAsync(ImageItemViewModel? imageItem)
        {
            if (imageItem != null && Images.Contains(imageItem))
            {
                int index = Images.IndexOf(imageItem);
                _undoStack.Push((imageItem, index));
                Images.RemoveAt(index);

                OnPropertyChanged(nameof(CanUndo));
                UndoDeleteCommand.NotifyCanExecuteChanged();

                var snackbar = Snackbar.Make(
                    "Image deleted",
                    () => UndoDelete(),
                    "Undo",
                    TimeSpan.FromSeconds(5));

                await snackbar.Show();
            }
        }

        private void UndoDelete()
        {
            if (_undoStack.Count > 0)
            {
                var (item, index) = _undoStack.Pop();

                if (index <= Images.Count)
                    Images.Insert(index, item);
                else
                    Images.Add(item);

                OnPropertyChanged(nameof(CanUndo));
                UndoDeleteCommand.NotifyCanExecuteChanged();
            }
        }

        private void CommandsCanExecuteChanged()
        {
            LoadInitialImagesCommand.NotifyCanExecuteChanged();
            AddImagesCommand.NotifyCanExecuteChanged();
            RegenerateImagesCommand.NotifyCanExecuteChanged();
            UndoDeleteCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CanUndo));
        }

        private async Task ShowErrorMessageAsync(string message)
        {
            try
            {
                await _toastService.ShowToastAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to display toast message");
            }
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
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
                _unsplashService?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }
}