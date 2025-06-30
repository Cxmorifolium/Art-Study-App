using artstudio.Data;
using artstudio.Services;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using static artstudio.ViewModels.GalleryCreationViewModel;

namespace artstudio.ViewModels
{
    public partial class GalleryPageViewModel : INotifyPropertyChanged, IDisposable
    {
        #region Fields

        private readonly DatabaseService _databaseService;
        private readonly ILogger<GalleryPageViewModel> _logger;

        private bool _isLoading;
        private bool _isRefreshing;
        private string _searchText = string.Empty;
        private List<UserUploadedImage> _allGalleryItems = new();

        #endregion

        #region Constructor

        public GalleryPageViewModel(DatabaseService databaseService, ILogger<GalleryPageViewModel> logger)
        {
            _databaseService = databaseService;
            _logger = logger;

            InitializeCommands();
            _ = LoadGalleryAsync();

            WeakReferenceMessenger.Default.Register<GalleryItemAddedMessage>(this, OnGalleryItemAdded);
        }

        #endregion

        #region Properties

        public ObservableCollection<UserUploadedImage> GalleryItems { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    OnPropertyChanged(nameof(HasSearchText));
                    _ = FilterGalleryAsync();
                }
            }
        }

        public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);

        #endregion

        #region Commands

        public AsyncRelayCommand RefreshCommand { get; private set; } = default!;
        public AsyncRelayCommand ShowFilterCommand { get; private set; } = default!;
        public AsyncRelayCommand AddToGalleryCommand { get; private set; } = default!;
        public AsyncRelayCommand SearchCommand { get; private set; } = default!;
        public RelayCommand ClearSearchCommand { get; private set; } = default!;
        public AsyncRelayCommand GoToStudyCommand { get; private set; } = default!;
        public AsyncRelayCommand<UserUploadedImage> ViewDetailsCommand { get; private set; } = default!;

        private void InitializeCommands()
        {
            RefreshCommand = new AsyncRelayCommand(ExecuteRefreshAsync);
            AddToGalleryCommand = new AsyncRelayCommand(ExecuteAddToGalleryAsync);
            SearchCommand = new AsyncRelayCommand(ExecuteSearchAsync);
            ClearSearchCommand = new RelayCommand(ExecuteClearSearch);
            GoToStudyCommand = new AsyncRelayCommand(ExecuteGoToStudyAsync);
            ViewDetailsCommand = new AsyncRelayCommand<UserUploadedImage>(ExecuteViewDetailsAsync);
        }

        #endregion

        #region Command Implementations

        private async Task ExecuteRefreshAsync()
        {
            IsRefreshing = true;
            await LoadGalleryAsync();
            IsRefreshing = false;
        }
        private async Task ExecuteAddToGalleryAsync()
        {
            try
            { 
                _logger.LogDebug("Add to Gallery command executed");

                // Test with alert first
                //await DisplayAlertAsync("Debug", "Add command called!", "OK");

                // Then try navigation
                await Shell.Current.GoToAsync("GalleryCreation");

                //Debug.WriteLine("Navigation Completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to gallery creation");
                await DisplayAlertAsync("Error", $"Navigation failed: {ex.Message}", "OK");
            }
        }

        private async Task ExecuteSearchAsync()
        {
            await FilterGalleryAsync();
        }

        private void ExecuteClearSearch()
        {
            SearchText = string.Empty;
        }

        private async Task ExecuteGoToStudyAsync()
        {
            try
            {
                _logger.LogDebug("Navigating to Study Mode");

                // Use the exact FlyoutItem Title from your AppShell.xaml
                await Shell.Current.GoToAsync("StudyMode");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to study page");
                await DisplayAlertAsync("Error", $"Unable to open study page: {ex.Message}", "OK");
            }
        }

        private async Task ExecuteViewDetailsAsync(UserUploadedImage? galleryItem)
        {
            if (galleryItem == null) return;

            try
            {
                await Shell.Current.GoToAsync($"GalleryDetail?itemId={galleryItem.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to gallery detail for item {ItemId}", galleryItem.Id);
                await DisplayAlertAsync("Error", "Unable to open gallery detail", "OK");
            }
        }

        #endregion

        #region Data Methods

        public async Task LoadGalleryAsync()
        {
            try
            {
                IsLoading = true;
                _logger.LogDebug("Loading gallery items from database");

                _allGalleryItems = await _databaseService.GetUserUploadedImagesAsync();
                await FilterGalleryAsync();

                _logger.LogDebug("Loaded {Count} gallery items", _allGalleryItems.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading gallery items");
                await DisplayAlertAsync("Error", "Unable to load gallery items", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task FilterGalleryAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    var filteredItems = _allGalleryItems.AsEnumerable();

                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        var searchLower = SearchText.Trim().ToLower();

                        filteredItems = filteredItems.Where(item =>
                            // Search in title
                            (item.Title?.ToLower().Contains(searchLower) == true) ||

                            // Search in notes
                            (item.Notes?.ToLower().Contains(searchLower) == true) ||

                            // Search in custom tags
                            item.CustomTagsList.Any(tag => tag.ToLower().Contains(searchLower)) ||

                            // Search in generated words
                            item.WordsList.Any(word => word.ToLower().Contains(searchLower))
                        );
                    }

                    var sortedItems = filteredItems
                        .OrderByDescending(item => item.CreatedAt)
                        .ToList();

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        GalleryItems.Clear();
                        foreach (var item in sortedItems)
                        {
                            GalleryItems.Add(item);
                        }
                    });
                });

                _logger.LogDebug("Filtered gallery: {FilteredCount} of {TotalCount} items",
                    GalleryItems.Count, _allGalleryItems.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering gallery items");
            }
        }

        public async Task DeleteGalleryItemAsync(UserUploadedImage item)
        {
            try
            {
                _logger.LogDebug("Deleting gallery item {ItemId}", item.Id);

                await _databaseService.DeleteUserUploadedImageAsync(item.Id);

                // Remove from local collections
                _allGalleryItems.Remove(item);
                GalleryItems.Remove(item);

                _logger.LogDebug("Successfully deleted gallery item {ItemId}", item.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting gallery item {ItemId}", item.Id);
                await DisplayAlertAsync("Error", "Unable to delete gallery item", "OK");
            }
        }

        #endregion

        #region Message Handlers
        private void OnGalleryItemAdded(object recipient, GalleryItemAddedMessage message)
        {
            //Debug.WriteLine("=== GALLERY REFRESH MESSAGE RECEIVED ===");
            // Use fire-and-forget for async operation to avoid blocking
            _ = Task.Run(async () =>
            {
                try
                {
                    await LoadGalleryAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing gallery after item added");
                }
            });
        }
        #endregion

        #region Page Lifecycle
        public async Task OnAppearingAsync()
        {
            // Refresh gallery when page appears (backup refresh mechanism)
            await LoadGalleryAsync();
        }
        #endregion

        #region Helper Methods

        private async Task DisplayAlertAsync(string title, string message, string cancel)
        {
            var mainPage = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (mainPage != null)
            {
                await mainPage.DisplayAlert(title, message, cancel);
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region IDisposable
        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Unregister from messaging
                    WeakReferenceMessenger.Default.Unregister<GalleryItemAddedMessage>(this);
                }
                _disposed = true;
            }
        }
        #endregion
    }

    #region Extensions for UserUploadedImage

    public static class UserUploadedImageExtensions
    {
        public static bool HasWords(this UserUploadedImage item) =>
            item.WordsList.Count > 0;

        public static bool HasPalette(this UserUploadedImage item) =>
            item.PaletteList.Count > 0;

        public static bool HasImages(this UserUploadedImage item) =>
            item.ImagesList.Count > 0;

        public static bool HasCustomTags(this UserUploadedImage item) =>
            item.CustomTagsList.Count > 0;

        public static bool HasNotes(this UserUploadedImage item) =>
            !string.IsNullOrWhiteSpace(item.Notes);

        public static bool HasSessionDuration(this UserUploadedImage item) =>
            !string.IsNullOrWhiteSpace(item.SessionDuration);

        public static int WordCount(this UserUploadedImage item) =>
            item.WordsList.Count;

        public static int ImageCount(this UserUploadedImage item) =>
            item.ImagesList.Count;

        public static List<string> PreviewTags(this UserUploadedImage item) =>
            item.CustomTagsList.Take(3).ToList(); // Show max 3 tags in preview
    }

    #endregion
}