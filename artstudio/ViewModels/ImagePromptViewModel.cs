using artstudio.Models;
using artstudio.Services;
using CommunityToolkit.Maui.Alerts;
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
        private const int DefaultImageCount = 3;
        private const int AdditionalImages = 1;
        private bool _isLoading;
        private bool _disposed = false;
        private readonly ILogger<ImagePromptViewModel> _logger;

        private readonly Stack<(ImageItem item, int index)> _undoStack = new();

        public ObservableCollection<ImageItem> Images { get; } = new();

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

        public bool CanUndo => _undoStack.Count > 0;

        #region Commands

        public ICommand LoadInitialImagesCommand { get; }
        public ICommand AddImagesCommand { get; }
        public ICommand RegenerateImagesCommand { get; }
        public ICommand ToggleLockCommand { get; }
        public ICommand DeleteImageCommand { get; }
        public ICommand UndoDeleteCommand { get; }

        #endregion

        public ImagePromptViewModel(ILogger<ImagePromptViewModel> logger)
        {
            _logger = logger;
            _unsplashService = new Unsplash();

            LoadInitialImagesCommand = new Command(ExecuteLoadInitialImages, () => !IsLoading);
            AddImagesCommand = new Command(ExecuteAddImages, () => !IsLoading);
            RegenerateImagesCommand = new Command(ExecuteRegenerateImages, () => !IsLoading);
            ToggleLockCommand = new Command<ImageItem>(ToggleLock);
            DeleteImageCommand = new Command<ImageItem>(ExecuteDeleteImage);
            UndoDeleteCommand = new Command(UndoDelete, () => CanUndo);

            // Load initial images on main thread
            MainThread.BeginInvokeOnMainThread(ExecuteLoadInitialImages);
        }

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

        private void ExecuteDeleteImage(ImageItem imageItem)
        {
            _ = DeleteImageAsync(imageItem);
        }

        #endregion

        #region Command Logic

        private async Task LoadInitialImagesAsync()
        {
            try
            {
                IsLoading = true;
                Images.Clear();

                var images = await _unsplashService.GetRandomImagesAsync(DefaultImageCount);
                foreach (var image in images)
                {
                    Images.Add(new ImageItem(image));
                }
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
                foreach (var image in images)
                {
                    Images.Add(new ImageItem(image));
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _logger.LogError(ex, "Invalid image count parameter");
                await ShowErrorMessageAsync("Invalid image count. Please try again.");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "API key issue.");
                await ShowErrorMessageAsync("Unable to access image service. Please check your connection and try again.");
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Request timeout.");
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

                for (int i = 0; i < unlockedIndices.Count && i < newImages.Count; i++)
                {
                    int index = unlockedIndices[i];
                    if (index < Images.Count)
                        Images[index] = new ImageItem(newImages[i]);
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _logger.LogWarning(ex, "Invalid image count parameter.");
                await ShowErrorMessageAsync("Invalid image count. Please try again.");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "API key issue.");
                await ShowErrorMessageAsync("Unable to access image service. Please check your connection and try again.");
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "Request timeout.");
                await ShowErrorMessageAsync("Request timed out. Please check your connection and try again.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Network error.");
                await ShowErrorMessageAsync("Network error. Please check your connection and try again.");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Service error.");
                await ShowErrorMessageAsync("Service temporarily unavailable. Please try again later.");
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

        private void ToggleLock(ImageItem? imageItem)
        {
            if (imageItem != null)
            {
                imageItem.IsLocked = !imageItem.IsLocked;
            }
        }

        private async Task DeleteImageAsync(ImageItem? imageItem)
        {
            if (imageItem != null && Images.Contains(imageItem))
            {
                int index = Images.IndexOf(imageItem);
                _undoStack.Push((imageItem, index));
                Images.RemoveAt(index);

                OnPropertyChanged(nameof(CanUndo));
                ((Command)UndoDeleteCommand).ChangeCanExecute();

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
                ((Command)UndoDeleteCommand).ChangeCanExecute();
            }
        }

        private void CommandsCanExecuteChanged()
        {
            ((Command)AddImagesCommand).ChangeCanExecute();
            ((Command)RegenerateImagesCommand).ChangeCanExecute();
            ((Command)UndoDeleteCommand).ChangeCanExecute();
            OnPropertyChanged(nameof(CanUndo));
        }

        private async Task ShowErrorMessageAsync(string message)
        {
            try
            {
                var toast = Toast.Make(message, CommunityToolkit.Maui.Core.ToastDuration.Long);
                await toast.Show();
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