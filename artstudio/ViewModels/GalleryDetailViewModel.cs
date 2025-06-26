using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.Input;
using artstudio.Data;
using artstudio.Services;
using artstudio.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace artstudio.ViewModels
{
    [QueryProperty(nameof(ItemId), "itemId")]
    public partial class GalleryDetailPageViewModel : INotifyPropertyChanged
    {
        #region Fields

        private readonly DatabaseService _databaseService;
        private readonly ILogger<GalleryDetailPageViewModel> _logger;

        private bool _isLoading;
        private UserUploadedImage? _galleryItem;
        private int _itemId;

        // Editing
        private bool _isEditing;
        private string _editTitle = string.Empty;
        private string _editNotes = string.Empty;
        private string _editCustomTags = string.Empty;
        private string _newWordsText = string.Empty;
        private string _newColorsText = string.Empty;

        #endregion

        #region Constructor

        public GalleryDetailPageViewModel(DatabaseService databaseService, ILogger<GalleryDetailPageViewModel> logger)
        {
            _databaseService = databaseService;
            _logger = logger;

            InitializeCommands();
        }

        #endregion

        #region Properties

        public int ItemId
        {
            get => _itemId;
            set
            {
                if (SetProperty(ref _itemId, value))
                {
                    _ = LoadGalleryItemAsync();
                }
            }
        }

        public UserUploadedImage? GalleryItem
        {
            get => _galleryItem;
            set => SetProperty(ref _galleryItem, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region Editing Properties
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (SetProperty(ref _isEditing, value))
                {
                    OnPropertyChanged(nameof(IsNotEditing));
                    EditCommand.NotifyCanExecuteChanged();
                    SaveChangesCommand.NotifyCanExecuteChanged();
                    CancelEditCommand.NotifyCanExecuteChanged();
                }
            }
        }
        public bool IsNotEditing => !IsEditing;

        public string EditTitle
        {
            get => _editTitle;
            set => SetProperty(ref _editTitle, value);
        }

        public string EditNotes
        {
            get => _editNotes;
            set => SetProperty(ref _editNotes, value);
        }

        public string EditCustomTags
        {
            get => _editCustomTags;
            set => SetProperty(ref _editCustomTags, value);
        }
        public string NewWordsText
        {
            get => _newWordsText;
            set => SetProperty(ref _newWordsText, value);
        }

        public string NewColorsText
        {
            get => _newColorsText;
            set => SetProperty(ref _newColorsText, value);
        }
        #endregion

        #region Commands

        public AsyncRelayCommand EditCommand { get; private set; } = default!;
        public AsyncRelayCommand DeleteCommand { get; private set; } = default!;
        public AsyncRelayCommand<string> CopyColorCommand { get; private set; } = default!;
        public AsyncRelayCommand<UnsplashImage> ViewImageCommand { get; private set; } = default!;
        public AsyncRelayCommand SaveChangesCommand { get; private set; } = default!;
        public RelayCommand CancelEditCommand { get; private set; } = default!;

        // New copy commands for each section
        public AsyncRelayCommand CopyWordsCommand { get; private set; } = default!;
        public AsyncRelayCommand CopyPaletteCommand { get; private set; } = default!;

        // New delete commands for individual items
        public AsyncRelayCommand<string> DeleteWordCommand { get; private set; } = default!;
        public AsyncRelayCommand<string> DeleteColorCommand { get; private set; } = default!;
        public AsyncRelayCommand<string> DeleteImageCommand { get; private set; } = default!;

        private void InitializeCommands()
        {
            EditCommand = new AsyncRelayCommand(ExecuteEditAsync, () => GalleryItem != null);
            DeleteCommand = new AsyncRelayCommand(ExecuteDeleteAsync, () => GalleryItem != null);
            CopyColorCommand = new AsyncRelayCommand<string>(ExecuteCopyColorAsync);
            ViewImageCommand = new AsyncRelayCommand<UnsplashImage>(ExecuteViewImageAsync);
            SaveChangesCommand = new AsyncRelayCommand(ExecuteSaveChangesAsync, () => IsEditing);
            CancelEditCommand = new RelayCommand(ExecuteCancelEdit);

            // Initialize new copy commands
            CopyWordsCommand = new AsyncRelayCommand(ExecuteCopyWordsAsync, () => GalleryItem?.HasWords() == true);
            CopyPaletteCommand = new AsyncRelayCommand(ExecuteCopyPaletteAsync, () => GalleryItem?.HasPalette() == true);

            // Initialize delete commands
            DeleteWordCommand = new AsyncRelayCommand<string>(ExecuteDeleteWordAsync);
            DeleteColorCommand = new AsyncRelayCommand<string>(ExecuteDeleteColorAsync);
            DeleteImageCommand = new AsyncRelayCommand<string>(ExecuteDeleteImageAsync);
        }

        #endregion

        #region Command Implementations
        private async Task ExecuteEditAsync()
        {
            if (GalleryItem == null) return;

            try
            {
                // Enter edit mode
                IsEditing = true;

                // Populate edit fields with current values
                EditTitle = GalleryItem.Title ?? string.Empty;
                EditNotes = GalleryItem.Notes ?? string.Empty;
                EditCustomTags = string.Join(", ", GalleryItem.CustomTagsList);

                _logger.LogDebug("Entered edit mode for gallery item {ItemId}", GalleryItem.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error entering edit mode");
                await DisplayAlertAsync("Error", "Unable to enter edit mode", "OK");
            }
        }

        private async Task ExecuteSaveChangesAsync()
        {
            if (GalleryItem == null) return;

            try
            {
                // Update basic properties
                GalleryItem.Title = string.IsNullOrWhiteSpace(EditTitle) ? null : EditTitle.Trim();
                GalleryItem.Notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes.Trim();

                // Update custom tags using the new method
                var customTags = ParseCustomTags(EditCustomTags);
                GalleryItem.UpdateCustomTagsList(customTags);

                // Process new words if any
                if (!string.IsNullOrWhiteSpace(NewWordsText))
                {
                    var newWords = ParseCustomTags(NewWordsText); // Reuse the parsing logic
                    GalleryItem.AddWords(newWords);
                    NewWordsText = string.Empty; // Clear the input
                }

                // Process new colors if any
                if (!string.IsNullOrWhiteSpace(NewColorsText))
                {
                    var newColors = ParseColors(NewColorsText);
                    GalleryItem.AddColors(newColors);
                    NewColorsText = string.Empty; // Clear the input
                }

                // Save to database
                await _databaseService.UpdateUserUploadedImageAsync(GalleryItem);

                // Exit edit mode
                IsEditing = false;

                // Clear edit fields
                EditTitle = string.Empty;
                EditNotes = string.Empty;
                EditCustomTags = string.Empty;

                // Notify UI of changes
                OnPropertyChanged(nameof(GalleryItem));

                // Update command states
                CopyWordsCommand.NotifyCanExecuteChanged();
                CopyPaletteCommand.NotifyCanExecuteChanged();

                _logger.LogDebug("Successfully updated gallery item {ItemId}", GalleryItem.Id);

                // Show success message
                await DisplayAlertAsync("Success", "Changes saved successfully!", "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes to gallery item {ItemId}", GalleryItem?.Id);
                await DisplayAlertAsync("Error", "Unable to save changes", "OK");
            }
        }

        // Cancel edit
        private void ExecuteCancelEdit()
        {
            IsEditing = false;

            // Clear edit fields
            EditTitle = string.Empty;
            EditNotes = string.Empty;
            EditCustomTags = string.Empty;

            _logger.LogDebug("Cancelled edit mode");
        }

        private async Task ExecuteDeleteAsync()
        {
            if (GalleryItem == null) return;

            try
            {
                var result = await DisplayAlertAsync(
                    "Delete Gallery Item",
                    $"Are you sure you want to delete \"{GalleryItem.DisplayTitle}\"? This cannot be undone.",
                    "Delete",
                    "Cancel");

                if (!result) return;

                IsLoading = true;
                await _databaseService.DeleteUserUploadedImageAsync(GalleryItem.Id);

                _logger.LogDebug("Successfully deleted gallery item {ItemId}", GalleryItem.Id);

                // Navigate back to gallery
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting gallery item {ItemId}", GalleryItem?.Id);
                await DisplayAlertAsync("Error", "Unable to delete gallery item", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExecuteCopyColorAsync(string? hexColor)
        {
            if (string.IsNullOrEmpty(hexColor)) return;

            try
            {
                await Clipboard.SetTextAsync(hexColor);

                // Show a brief toast/feedback
                await DisplayAlertAsync("Copied", $"Color {hexColor} copied to clipboard", "OK");

                _logger.LogDebug("Copied color {HexColor} to clipboard", hexColor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying color {HexColor}", hexColor);
                await DisplayAlertAsync("Error", "Unable to copy color", "OK");
            }
        }

        private async Task ExecuteViewImageAsync(UnsplashImage? image)
        {
            if (image?.urls?.Regular == null) return;

            try
            {
                // Open image in browser or image viewer
                var uri = new Uri(image.urls.Regular);
                await Browser.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);

                _logger.LogDebug("Opened image {ImageId} in browser", image.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening image {ImageId}", image?.Id);
                await DisplayAlertAsync("Error", "Unable to open image", "OK");
            }
        }

        // New copy command implementations
        private async Task ExecuteCopyWordsAsync()
        {
            if (GalleryItem?.HasWords() != true) return;

            try
            {
                var wordsText = string.Join(", ", GalleryItem.WordsList);
                await Clipboard.SetTextAsync(wordsText);

                await DisplayAlertAsync("Copied", "Words copied to clipboard", "OK");
                _logger.LogDebug("Copied words to clipboard for gallery item {ItemId}", GalleryItem.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying words for gallery item {ItemId}", GalleryItem?.Id);
                await DisplayAlertAsync("Error", "Unable to copy words", "OK");
            }
        }

        private async Task ExecuteCopyPaletteAsync()
        {
            if (GalleryItem?.HasPalette() != true) return;

            try
            {
                var paletteText = string.Join(", ", GalleryItem.PaletteList);
                await Clipboard.SetTextAsync(paletteText);

                await DisplayAlertAsync("Copied", "Palette copied to clipboard", "OK");
                _logger.LogDebug("Copied palette to clipboard for gallery item {ItemId}", GalleryItem.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying palette for gallery item {ItemId}", GalleryItem?.Id);
                await DisplayAlertAsync("Error", "Unable to copy palette", "OK");
            }
        }

        // New delete command implementations
        private async Task ExecuteDeleteWordAsync(string? word)
        {
            if (string.IsNullOrEmpty(word) || GalleryItem == null) return;

            try
            {
                GalleryItem.RemoveWord(word);
                _logger.LogDebug("Removed word '{Word}' from gallery item {ItemId}", word, GalleryItem.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing word '{Word}' from gallery item {ItemId}", word, GalleryItem?.Id);
                await DisplayAlertAsync("Error", "Unable to remove word", "OK");
            }
        }

        private async Task ExecuteDeleteColorAsync(string? color)
        {
            if (string.IsNullOrEmpty(color) || GalleryItem == null) return;

            try
            {
                GalleryItem.RemoveColor(color);
                _logger.LogDebug("Removed color '{Color}' from gallery item {ItemId}", color, GalleryItem.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing color '{Color}' from gallery item {ItemId}", color, GalleryItem?.Id);
                await DisplayAlertAsync("Error", "Unable to remove color", "OK");
            }
        }

        private async Task ExecuteDeleteImageAsync(string? imageId)
        {
            if (string.IsNullOrEmpty(imageId) || GalleryItem == null) return;

            try
            {
                GalleryItem.RemoveImage(imageId);
                _logger.LogDebug("Removed image '{ImageId}' from gallery item {ItemId}", imageId, GalleryItem.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing image '{ImageId}' from gallery item {ItemId}", imageId, GalleryItem?.Id);
                await DisplayAlertAsync("Error", "Unable to remove image", "OK");
            }
        }

        #endregion

        #region Data Methods

        public async Task LoadGalleryItemAsync()
        {
            if (ItemId <= 0) return;

            try
            {
                IsLoading = true;
                _logger.LogDebug("Loading gallery item {ItemId}", ItemId);

                GalleryItem = await _databaseService.GetUserUploadedImageAsync(ItemId);

                if (GalleryItem == null)
                {
                    _logger.LogWarning("Gallery item {ItemId} not found", ItemId);
                    await DisplayAlertAsync("Error", "Gallery item not found", "OK");
                    await Shell.Current.GoToAsync("..");
                    return;

                }

                // Update command states
                EditCommand.NotifyCanExecuteChanged();
                DeleteCommand.NotifyCanExecuteChanged();
                CopyWordsCommand.NotifyCanExecuteChanged();
                CopyPaletteCommand.NotifyCanExecuteChanged();

                _logger.LogDebug("Successfully loaded gallery item {ItemId}: {Title}", ItemId, GalleryItem.DisplayTitle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading gallery item {ItemId}", ItemId);
                await DisplayAlertAsync("Error", "Unable to load gallery item", "OK");
                await Shell.Current.GoToAsync("..");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task RefreshGalleryItemAsync()
        {
            await LoadGalleryItemAsync();
        }

        #endregion

        #region Helper Methods

        private async Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel)
        {
            var mainPage = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (mainPage != null)
            {
                return await mainPage.DisplayAlert(title, message, accept, cancel);
            }
            return false;
        }

        private async Task DisplayAlertAsync(string title, string message, string cancel)
        {
            var mainPage = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (mainPage != null)
            {
                await mainPage.DisplayAlert(title, message, cancel);
            }
        }

        #endregion

        #region Helper Methods for Custom Tags
        private List<string> ParseCustomTags(string tagsText)
        {
            if (string.IsNullOrWhiteSpace(tagsText))
                return new List<string>();

            return tagsText
                .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(tag => tag.Trim())
                .Where(tag => !string.IsNullOrEmpty(tag))
                .Distinct()
                .ToList();
        }

        private List<string> ParseColors(string colorsText)
        {
            if (string.IsNullOrWhiteSpace(colorsText))
                return new List<string>();

            return colorsText
                .Split(new[] { ',', ';', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(color => color.Trim())
                .Where(color => !string.IsNullOrEmpty(color) && color.StartsWith("#"))
                .Distinct()
                .ToList();
        }
        #endregion

        #region Page Lifecycle

        public async Task OnAppearingAsync()
        {
            // Refresh data when page appears (in case it was edited)
            if (GalleryItem != null)
            {
                await RefreshGalleryItemAsync();
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
    }
}