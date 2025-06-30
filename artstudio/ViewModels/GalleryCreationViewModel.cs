using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CommunityToolkit.Mvvm.Input;
using artstudio.Data;
using artstudio.Models;
using Microsoft.Extensions.Logging;
using artstudio.Services;
using CommunityToolkit.Mvvm.Messaging;

namespace artstudio.ViewModels
{
    [QueryProperty(nameof(GalleryData), "GalleryData")]
    public partial class GalleryCreationViewModel : INotifyPropertyChanged
    {
        #region Fields

        private readonly DatabaseService _databaseService;
        private readonly ILogger<GalleryCreationViewModel> _logger;
        private readonly IToastService _toastService;
        private readonly WordPromptService _wordPromptService;
        private readonly PaletteService _paletteService;

        private bool _isSaving;
        private string _title = string.Empty;
        private string _notes = string.Empty;
        private string _customTagsText = string.Empty;
        private string _artworkImagePath = string.Empty;
        private GalleryCreationData? _galleryData;

        // Flyout properties
        private bool _isWordsFlyoutVisible = false;
        private bool _isPaletteFlyoutVisible = false;
        private bool _isImagesFlyoutVisible = false;
        private bool _isLoadingWords = false;
        private bool _isLoadingPalettes = false;
        private bool _isLoadingImages = false;

        // Session snapshot
        private bool _isSessionsFlyoutVisible = false;
        private bool _isLoadingSessions = false;

        #endregion

        #region Constructor

        public GalleryCreationViewModel(
            DatabaseService databaseService,
            ILogger<GalleryCreationViewModel> logger,
            IToastService toastService,
            WordPromptService wordPromptService,
            PaletteService paletteService)
        {
            _databaseService = databaseService;
            _logger = logger;
            _toastService = toastService;
            _wordPromptService = wordPromptService;
            _paletteService = paletteService;

            InitializeCommands();
            InitializeCollections();
        }

        #endregion

        #region Properties

        public GalleryCreationData? GalleryData
        {
            get => _galleryData;
            set
            {
                if (SetProperty(ref _galleryData, value))
                {
                    LoadDataFromSession();
                }
            }
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public string CustomTagsText
        {
            get => _customTagsText;
            set => SetProperty(ref _customTagsText, value);
        }

        public string ArtworkImagePath
        {
            get => _artworkImagePath;
            set
            {
                if (SetProperty(ref _artworkImagePath, value))
                {
                    _logger.LogDebug("ArtworkImagePath changed to: {Value}", value);
                    OnPropertyChanged(nameof(HasArtworkImage));
                    OnPropertyChanged(nameof(ArtworkImageSource));
                    OnPropertyChanged(nameof(CanSave));
                    SaveCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public ImageSource? ArtworkImageSource
        {
            get
            {
                var pathToUse = !string.IsNullOrEmpty(_tempPreviewPath) ? _tempPreviewPath : ArtworkImagePath;

                if (string.IsNullOrEmpty(pathToUse) || !File.Exists(pathToUse))
                {
                    _logger.LogDebug("=== ArtworkImageSource: No valid path. Temp: '{TempPreviewPath}', Main: '{ArtworkImagePath}' ===",
                                    _tempPreviewPath,
                                    ArtworkImagePath);
                    return null;
                }

                try
                {
                    var imageSource = ImageSource.FromFile(pathToUse);
                    return imageSource;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,"ArtworkImageSource Error");
                    return null;
                }
            }
        }

        public bool IsSaving
        {
            get => _isSaving;
            set => SetProperty(ref _isSaving, value);
        }

        public bool HasArtworkImage
        {
            get
            {
                var hasImage = !string.IsNullOrEmpty(ArtworkImagePath) && File.Exists(ArtworkImagePath);
                return hasImage;
            }
        }

        public bool CanSave => HasArtworkImage && !string.IsNullOrEmpty(ArtworkImagePath);

        public bool HasSessionData => GalleryData != null &&
            (GalleryData.AvailableWords.Count > 0 ||
             GalleryData.AvailablePalette.Count > 0 ||
             GalleryData.AvailableImages.Count > 0);

        // Flyout properties
        public bool IsWordsFlyoutVisible
        {
            get => _isWordsFlyoutVisible;
            set => SetProperty(ref _isWordsFlyoutVisible, value);
        }

        public bool IsPaletteFlyoutVisible
        {
            get => _isPaletteFlyoutVisible;
            set => SetProperty(ref _isPaletteFlyoutVisible, value);
        }

        public bool IsImagesFlyoutVisible
        {
            get => _isImagesFlyoutVisible;
            set => SetProperty(ref _isImagesFlyoutVisible, value);
        }

        public bool IsLoadingWords
        {
            get => _isLoadingWords;
            set => SetProperty(ref _isLoadingWords, value);
        }

        public bool IsLoadingPalettes
        {
            get => _isLoadingPalettes;
            set => SetProperty(ref _isLoadingPalettes, value);
        }

        public bool IsSessionsFlyoutVisible
        {
            get => _isSessionsFlyoutVisible;
            set => SetProperty(ref _isSessionsFlyoutVisible, value);
        }

        public bool IsLoadingSessions
        {
            get => _isLoadingSessions;
            set => SetProperty(ref _isLoadingSessions, value);
        }

        // Image properties
        public ObservableCollection<FavoriteImageItem> FavoriteImages { get; } = new();
        public bool HasNoFavoriteImages => FavoriteImages.Count == 0 && !IsLoadingImages;
        public bool IsLoadingImages
        {
            get => _isLoadingImages;
            set => SetProperty(ref _isLoadingImages, value);

        }

        public bool HasNoSavedSessions => SavedSessions.Count == 0 && !IsLoadingSessions;

        public bool HasNoFavoriteWords => FavoriteWordsGroups.Count == 0 && !IsLoadingWords;
        public bool HasNoFavoritePalettes => FavoritePalettesGroups.Count == 0 && !IsLoadingPalettes;

        // Collections for selection
        public ObservableCollection<SelectableWord> SelectableWords { get; } = new();
        public ObservableCollection<SelectableColor> SelectablePalette { get; } = new();
        public ObservableCollection<SelectableImage> SelectableImages { get; } = new();

        // Flyout collections
        public ObservableCollection<WordCollectionGroup> FavoriteWordsGroups { get; } = new();
        public ObservableCollection<PaletteCollectionGroup> FavoritePalettesGroups { get; } = new();

        // Saved sessions collection
        public ObservableCollection<SessionSnapshot> SavedSessions { get; } = new();
        #endregion

        #region Commands

        public AsyncRelayCommand PickImageCommand { get; private set; } = default!;
        public AsyncRelayCommand SaveCommand { get; private set; } = default!;
        public AsyncRelayCommand ClearCommand { get; private set; } = default!;
        public RelayCommand SelectAllWordsCommand { get; private set; } = default!;
        public RelayCommand SelectNoneWordsCommand { get; private set; } = default!;
        public RelayCommand SelectAllImagesCommand { get; private set; } = default!;
        public RelayCommand SelectNoneImagesCommand { get; private set; } = default!;

        // Flyout commands
        public AsyncRelayCommand ToggleWordsFlyoutCommand { get; private set; } = default!;
        public AsyncRelayCommand TogglePaletteFlyoutCommand { get; private set; } = default!;
        public AsyncRelayCommand ToggleImagesFlyoutCommand { get; private set; } = default!;
        public RelayCommand CloseWordsFlyoutCommand { get; private set; } = default!;
        public RelayCommand ClosePaletteFlyoutCommand { get; private set; } = default!;
        public RelayCommand CloseImagesFlyoutCommand { get; private set; } = default!;
        public AsyncRelayCommand<WordCollection> LoadWordsFromFavoriteCommand { get; private set; } = default!;
        public AsyncRelayCommand<FavoritePaletteItem> LoadPaletteFromFavoriteCommand { get; private set; } = default!;
        public AsyncRelayCommand<FavoriteImageItem> DeleteImageFavoriteCommand { get; private set; } = default!;
        public AsyncRelayCommand<FavoriteImageItem> LoadImageFromFavoriteCommand { get; private set; } = default!;

        // Sessions commands
        public AsyncRelayCommand ToggleSessionsFlyoutCommand { get; private set; } = default!;
        public RelayCommand CloseSessionsFlyoutCommand { get; private set; } = default!;
        public AsyncRelayCommand<SessionSnapshot> LoadSessionCommand { get; private set; } = default!;
        public AsyncRelayCommand<SessionSnapshot> DeleteSessionCommand { get; private set; } = default!;

        private void InitializeCommands()
        {
            PickImageCommand = new AsyncRelayCommand(ExecutePickImageAsync);
            SaveCommand = new AsyncRelayCommand(ExecuteSaveAsync, () => CanSave);
            ClearCommand = new AsyncRelayCommand(ExecuteClearAsync);
            SelectAllWordsCommand = new RelayCommand(ExecuteSelectAllWords);
            SelectNoneWordsCommand = new RelayCommand(ExecuteSelectNoneWords);
            SelectAllImagesCommand = new RelayCommand(ExecuteSelectAllImages);
            SelectNoneImagesCommand = new RelayCommand(ExecuteSelectNoneImages);

            // Flyout commands
            ToggleWordsFlyoutCommand = new AsyncRelayCommand(ExecuteToggleWordsFlyoutAsync);
            TogglePaletteFlyoutCommand = new AsyncRelayCommand(ExecuteTogglePaletteFlyoutAsync);
            ToggleImagesFlyoutCommand = new AsyncRelayCommand(ExecuteToggleImagesFlyoutAsync);
            CloseWordsFlyoutCommand = new RelayCommand(ExecuteCloseWordsFlyout);
            ClosePaletteFlyoutCommand = new RelayCommand(ExecuteClosePaletteFlyout);
            CloseImagesFlyoutCommand = new RelayCommand(ExecuteCloseImagesFlyout);
            LoadWordsFromFavoriteCommand = new AsyncRelayCommand<WordCollection>(ExecuteLoadWordsFromFavoriteAsync);
            LoadPaletteFromFavoriteCommand = new AsyncRelayCommand<FavoritePaletteItem>(ExecuteLoadPaletteFromFavoriteAsync);
            LoadImageFromFavoriteCommand = new AsyncRelayCommand<FavoriteImageItem>(ExecuteLoadImageFromFavoriteAsync);
            DeleteImageFavoriteCommand = new AsyncRelayCommand<FavoriteImageItem>(ExecuteDeleteImageFavoriteAsync);
            // Session commands
            ToggleSessionsFlyoutCommand = new AsyncRelayCommand(ExecuteToggleSessionsFlyoutAsync);
            CloseSessionsFlyoutCommand = new RelayCommand(ExecuteCloseSessionsFlyout);
            LoadSessionCommand = new AsyncRelayCommand<SessionSnapshot>(ExecuteLoadSessionAsync);
            DeleteSessionCommand = new AsyncRelayCommand<SessionSnapshot>(ExecuteDeleteSessionAsync);
        }

        #endregion

        #region Flyout Command Implementations

        private async Task ExecuteToggleWordsFlyoutAsync()
        {
            try
            {
                if (!IsWordsFlyoutVisible)
                {
                    await LoadFavoriteWordsAsync();
                }
                IsWordsFlyoutVisible = !IsWordsFlyoutVisible;

                // Close other flyouts
                if (IsWordsFlyoutVisible)
                {
                    IsPaletteFlyoutVisible = false;
                    IsImagesFlyoutVisible = false;
                    IsSessionsFlyoutVisible = false; // Add this line
                }

                _logger.LogDebug("Words flyout toggled: {IsVisible}", IsWordsFlyoutVisible);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling words flyout");
                await _toastService.ShowToastAsync("Error loading favorite words");
            }
        }

        private async Task ExecuteTogglePaletteFlyoutAsync()
        {
            try
            {
                if (!IsPaletteFlyoutVisible)
                {
                    await LoadFavoritePalettesAsync();
                }
                IsPaletteFlyoutVisible = !IsPaletteFlyoutVisible;

                // Close other flyouts
                if (IsPaletteFlyoutVisible)
                {
                    IsWordsFlyoutVisible = false;
                    IsImagesFlyoutVisible = false;
                    IsSessionsFlyoutVisible = false; // Add this line
                }

                _logger.LogDebug("Palette flyout toggled: {IsVisible}", IsPaletteFlyoutVisible);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling palette flyout");
                await _toastService.ShowToastAsync("Error loading favorite palettes");
            }
        }

        private async Task ExecuteToggleImagesFlyoutAsync()
        {
            try
            {
                if (!IsImagesFlyoutVisible)
                {
                    await LoadFavoriteImagesAsync();
                }
                IsImagesFlyoutVisible = !IsImagesFlyoutVisible;

                // Close other flyouts
                if (IsImagesFlyoutVisible)
                {
                    IsWordsFlyoutVisible = false;
                    IsPaletteFlyoutVisible = false;
                    IsSessionsFlyoutVisible = false;
                }

                _logger.LogDebug("Images flyout toggled: {IsVisible}", IsImagesFlyoutVisible);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling images flyout");
                await _toastService.ShowToastAsync("Error loading favorite images");
            }
        }

        private void ExecuteCloseWordsFlyout()
        {
            IsWordsFlyoutVisible = false;
        }

        private void ExecuteClosePaletteFlyout()
        {
            IsPaletteFlyoutVisible = false;
        }

        private void ExecuteCloseImagesFlyout()
        {
            IsImagesFlyoutVisible = false;
        }

        private async Task ExecuteLoadWordsFromFavoriteAsync(WordCollection? collection)
        {
            try
            {
                if (collection == null) return;

                var categorizedWords = await _wordPromptService.GetWordsByCategoryAsync(collection);

                // Add words to current selection
                if (categorizedWords.TryGetValue("nouns", out var nouns))
                {
                    foreach (var noun in nouns)
                    {
                        if (!SelectableWords.Any(w => w.Word == noun))
                        {
                            SelectableWords.Add(new SelectableWord { Word = noun, IsSelected = true });
                        }
                    }
                }

                // Add other categories if needed
                foreach (var category in categorizedWords)
                {
                    if (category.Key != "nouns") // Already handled nouns above
                    {
                        foreach (var word in category.Value)
                        {
                            if (!SelectableWords.Any(w => w.Word == word))
                            {
                                SelectableWords.Add(new SelectableWord { Word = word, IsSelected = true });
                            }
                        }
                    }
                }

                IsWordsFlyoutVisible = false;
                await _toastService.ShowToastAsync($"Added words from: {collection.Title}");
                _logger.LogDebug("Loaded words from favorite: {CollectionTitle}", collection.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading words from favorite");
                await _toastService.ShowToastAsync("Error loading words");
            }
        }

        private async Task ExecuteLoadPaletteFromFavoriteAsync(FavoritePaletteItem? palette)
        {
            try
            {
                if (palette == null) return;

                _logger.LogInformation("Loading favorite palette: {PaletteTitle}", palette.Title);

                // Convert hex strings back to Color objects and add to current selection
                foreach (var hexColor in palette.Colors)
                {
                    try
                    {
                        var color = Color.FromArgb(hexColor);
                        if (!SelectablePalette.Any(c => c.Color.ToArgbHex() == hexColor))
                        {
                            SelectablePalette.Add(new SelectableColor { Color = color, IsSelected = true });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse color: {HexColor}", hexColor);
                    }
                }

                IsPaletteFlyoutVisible = false;
                await _toastService.ShowToastAsync($"Added palette: {palette.DisplayTitle}");
                _logger.LogDebug("Loaded palette from favorite: {PaletteTitle}", palette.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading palette from favorite");
                await _toastService.ShowToastAsync("Error loading palette");
            }
        }

        #endregion

        #region Flyout Data Loading

        private async Task LoadFavoriteWordsAsync()
        {
            try
            {
                IsLoadingWords = true;
                OnPropertyChanged(nameof(HasNoFavoriteWords));

                var favorites = await _wordPromptService.GetFavoritesWithWordsAsync();

                // Group by collection name
                var grouped = favorites
                    .GroupBy(f => GetCollectionGroupName(f.Title))
                    .Select(g => new WordCollectionGroup(g.Key, g))
                    .OrderBy(g => g.CollectionName == "Default" ? "ZZZ" : g.CollectionName)
                    .ToList();

                FavoriteWordsGroups.Clear();
                foreach (var group in grouped)
                {
                    FavoriteWordsGroups.Add(group);
                }

                _logger.LogDebug("Loaded {FavoriteCount} favorite word collections in {GroupCount} groups",
                    favorites.Count, grouped.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading favorite words");
                await _toastService.ShowToastAsync("Error loading favorite words");
            }
            finally
            {
                IsLoadingWords = false;
                OnPropertyChanged(nameof(HasNoFavoriteWords));
            }
        }

        private async Task LoadFavoritePalettesAsync()
        {
            try
            {
                IsLoadingPalettes = true;
                OnPropertyChanged(nameof(HasNoFavoritePalettes));

                var favorites = await _paletteService.GetFavoritePalettesAsync();

                // Group by collection name and convert to FavoritePaletteItem
                var grouped = favorites
                    .GroupBy(p => GetPaletteCollectionName(p.Title))
                    .Select(g => new PaletteCollectionGroup(g.Key, g.Select(p => new FavoritePaletteItem
                    {
                        Id = p.Id,
                        Title = p.Title ?? "Untitled",
                        Colors = p.ColorsList?.Select(c => c.ToArgbHex()).ToList() ?? new List<string>(),
                        CreatedAt = p.CreatedAt
                    })))
                    .OrderBy(g => g.CollectionName == "Default" ? "ZZZ" : g.CollectionName)
                    .ToList();

                FavoritePalettesGroups.Clear();
                foreach (var group in grouped)
                {
                    FavoritePalettesGroups.Add(group);
                }

                _logger.LogDebug("Loaded {FavoriteCount} favorite palettes in {GroupCount} groups",
                    favorites.Count, grouped.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading favorite palettes");
                await _toastService.ShowToastAsync("Error loading favorite palettes");
            }
            finally
            {
                IsLoadingPalettes = false;
                OnPropertyChanged(nameof(HasNoFavoritePalettes));
            }
        }

        private static string GetCollectionGroupName(string? title)
        {
            if (string.IsNullOrEmpty(title))
                return "Default";

            if (title.StartsWith("Favorite -"))
                return "Default";

            var parts = title.Split(" - ");
            if (parts.Length >= 2)
                return parts[0].Trim();

            return "Default";
        }

        private static string GetPaletteCollectionName(string? title)
        {
            if (string.IsNullOrEmpty(title))
                return "Default";

            if (title.StartsWith("Palette -"))
                return "Default";

            var parts = title.Split(" - ");
            if (parts.Length >= 2)
                return parts[0].Trim();

            return "Default";
        }

        private async Task LoadFavoriteImagesAsync()
        {
            try
            {
                IsLoadingImages = true;
                OnPropertyChanged(nameof(HasNoFavoriteImages));

                var favorites = await _databaseService.GetFavoriteImagesAsync();

                FavoriteImages.Clear();
                foreach (var favorite in favorites)
                {
                    FavoriteImages.Add(favorite);
                }

                _logger.LogDebug("Loaded {FavoriteCount} favorite images", favorites.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading favorite images");
                await _toastService.ShowToastAsync("Error loading favorite images");
            }
            finally
            {
                IsLoadingImages = false;
                OnPropertyChanged(nameof(HasNoFavoriteImages));
            }
        }

        private async Task ExecuteLoadImageFromFavoriteAsync(FavoriteImageItem? favoriteImage)
        {
            try
            {
                if (favoriteImage == null) return;

                _logger.LogInformation("Loading favorite image: {ImageTitle}", favoriteImage.DisplayTitle);

                // Convert FavoriteImageItem back to UnsplashImage
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

                // Check if image already exists in selection to prevent duplicates
                if (!SelectableImages.Any(img => img.Image.Id == favoriteImage.UnsplashId))
                {
                    SelectableImages.Add(new SelectableImage { Image = unsplashImage, IsSelected = true });
                    await _toastService.ShowToastAsync($"Added image: {favoriteImage.DisplayTitle}");
                }
                else
                {
                    await _toastService.ShowToastAsync("Image already added");
                }

                // Close flyout after loading (same as words and palette)
                IsImagesFlyoutVisible = false;

                _logger.LogDebug("Successfully loaded favorite image: {ImageId}", favoriteImage.UnsplashId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading image from favorite");
                await _toastService.ShowToastAsync("Error loading image");
            }
        }
        private async Task ExecuteDeleteImageFavoriteAsync(FavoriteImageItem? favoriteImage)
        {
            try
            {
                if (favoriteImage == null) return;

                var result = await DisplayAlertAsync(
                    "Remove Favorite",
                    $"Remove \"{favoriteImage.DisplayTitle}\" from favorites?",
                    "Remove",
                    "Cancel");

                if (!result) return;

                await _databaseService.DeleteFavoriteImageAsync(favoriteImage.Id);
                FavoriteImages.Remove(favoriteImage);

                OnPropertyChanged(nameof(HasNoFavoriteImages));
                await _toastService.ShowToastAsync("Removed from favorites");
                _logger.LogDebug("Deleted favorite image: {ImageId}", favoriteImage.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting favorite image");
                await _toastService.ShowToastAsync("Error removing favorite");
            }
        }
        #endregion

        #region Command Implementations (Existing)

        private async Task ExecutePickImageAsync()
        {
            try
            {
                _logger.LogDebug("PICK IMAGE STARTED");
                await _toastService.ShowToastAsync("Opening file picker...");

                var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Select your artwork photo"
                });

                if (result != null)
                {
                    _logger.LogDebug("FILE SELECTED: {FileNameResult} ===", result.FileName);

                    var savedPath = await SaveImageAsync(result);
                    ArtworkImagePath = savedPath;

                    OnPropertyChanged(nameof(HasArtworkImage));
                    OnPropertyChanged(nameof(CanSave));
                    SaveCommand.NotifyCanExecuteChanged();

                    _logger.LogDebug("IMAGE PATH SET: {SavedPath}", savedPath);
                    _logger.LogDebug("HasArtworkImage: {HasArtworkImage}", HasArtworkImage);
                    _logger.LogDebug("CanSave: {CanSave}", CanSave);

                    await _toastService.ShowToastAsync("Image loaded successfully!");
                }
                else
                {
                    await _toastService.ShowToastAsync("No image selected");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error picking image");
                await _toastService.ShowToastAsync($"Error loading image: {ex.Message}");
            }
        }

        private async Task ExecuteSaveAsync()
        {
            if (!CanSave)
            {
                await _toastService.ShowToastAsync("Please add an artwork photo first");
                return;
            }

            try
            {
                await _toastService.ShowToastAsync("Saving to gallery...");
                _logger.LogDebug("Saving gallery item");

                var galleryItem = BuildGalleryItem();
                await _databaseService.SaveUserUploadedImageAsync(galleryItem);

                _logger.LogDebug("Successfully saved gallery item: {Title}", galleryItem.DisplayTitle);

                await _toastService.ShowToastAsync("Artwork saved to gallery!");

                // Navigate back to gallery and trigger refresh
                await Shell.Current.GoToAsync("//Gallery");

                _logger.LogDebug("=== SENDING GALLERY REFRESH MESSAGE ===");
                WeakReferenceMessenger.Default.Send(new GalleryItemAddedMessage());
                _logger.LogDebug("=== MESSAGE SENT ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving gallery item");
                await _toastService.ShowToastAsync("Error saving to gallery");
            }
        }

        // Builds the gallery item from the current form data
        public class GalleryItemAddedMessage
        {
        }

        private async Task ExecuteClearAsync()
        {
            try
            {
                var hasChanges = !string.IsNullOrEmpty(Title) ||
                                !string.IsNullOrEmpty(Notes) ||
                                !string.IsNullOrEmpty(CustomTagsText) ||
                                !string.IsNullOrEmpty(ArtworkImagePath);

                if (hasChanges)
                {
                    var result = await DisplayAlertAsync(
                        "Clear Form",
                        "Are you sure you want to clear all content?",
                        "Clear",
                        "Keep Editing");

                    if (!result) return;
                }

                // Clear all fields
                Title = string.Empty;
                Notes = string.Empty;
                CustomTagsText = string.Empty;
                ArtworkImagePath = string.Empty;

                // Clear selections
                foreach (var word in SelectableWords)
                    word.IsSelected = false;
                foreach (var color in SelectablePalette)
                    color.IsSelected = false;
                foreach (var image in SelectableImages)
                    image.IsSelected = false;

                // Update UI
                OnPropertyChanged(nameof(HasArtworkImage));
                OnPropertyChanged(nameof(CanSave));
                SaveCommand.NotifyCanExecuteChanged();

                await _toastService.ShowToastAsync("Form cleared");
                _logger.LogDebug("Gallery creation form cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing gallery creation form");
                await _toastService.ShowToastAsync("Error clearing form");
            }
        }

        private void ExecuteSelectAllWords()
        {
            foreach (var word in SelectableWords)
            {
                word.IsSelected = true;
            }
        }

        private void ExecuteSelectNoneWords()
        {
            foreach (var word in SelectableWords)
            {
                word.IsSelected = false;
            }
        }

        private void ExecuteSelectAllImages()
        {
            foreach (var image in SelectableImages)
            {
                image.IsSelected = true;
            }
        }

        private void ExecuteSelectNoneImages()
        {
            foreach (var image in SelectableImages)
            {
                image.IsSelected = false;
            }
        }

        #endregion

        #region Session Command Implementations (add this new section)

        private async Task ExecuteToggleSessionsFlyoutAsync()
        {
            try
            {
                if (!IsSessionsFlyoutVisible)
                {
                    await LoadSavedSessionsAsync();
                }
                IsSessionsFlyoutVisible = !IsSessionsFlyoutVisible;

                // Close other flyouts
                if (IsSessionsFlyoutVisible)
                {
                    IsWordsFlyoutVisible = false;
                    IsPaletteFlyoutVisible = false;
                    IsImagesFlyoutVisible = false;
                }

                _logger.LogDebug("Sessions flyout toggled: {IsVisible}", IsSessionsFlyoutVisible);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling sessions flyout");
                await _toastService.ShowToastAsync("Error loading saved sessions");
            }
        }

        private void ExecuteCloseSessionsFlyout()
        {
            IsSessionsFlyoutVisible = false;
        }

        private async Task ExecuteLoadSessionAsync(SessionSnapshot? session)
        {
            try
            {
                if (session == null) return;

                _logger.LogInformation("Loading session: {SessionTitle}", session.DisplayTitle);

                // Clear existing selections first
                SelectableWords.Clear();
                SelectablePalette.Clear();
                SelectableImages.Clear();

                // Load words
                foreach (var word in session.WordsList)
                {
                    SelectableWords.Add(new SelectableWord { Word = word, IsSelected = true });
                }

                // Load palette
                foreach (var hexColor in session.PaletteList)
                {
                    try
                    {
                        var color = Color.FromArgb(hexColor);
                        SelectablePalette.Add(new SelectableColor { Color = color, IsSelected = true });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse color: {HexColor}", hexColor);
                    }
                }

                // Load images
                foreach (var image in session.ImagesList)
                {
                    SelectableImages.Add(new SelectableImage { Image = image, IsSelected = true });
                }

                // Set title if not already set
                if (string.IsNullOrEmpty(Title))
                {
                    Title = session.DisplayTitle;
                }

                // Close flyout
                IsSessionsFlyoutVisible = false;

                await _toastService.ShowToastAsync($"Loaded session: {session.DisplayTitle}");
                _logger.LogDebug("Successfully loaded session: {SessionId}", session.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading session");
                await _toastService.ShowToastAsync("Error loading session");
            }
        }

        private async Task ExecuteDeleteSessionAsync(SessionSnapshot? session)
        {
            try
            {
                if (session == null) return;

                var result = await DisplayAlertAsync(
                    "Delete Session",
                    $"Are you sure you want to delete \"{session.DisplayTitle}\"? This cannot be undone.",
                    "Delete",
                    "Cancel");

                if (!result) return;

                await _databaseService.DeleteSessionSnapshotAsync(session.Id);
                SavedSessions.Remove(session);

                OnPropertyChanged(nameof(HasNoSavedSessions));
                await _toastService.ShowToastAsync("Session deleted");
                _logger.LogDebug("Deleted session: {SessionId}", session.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting session");
                await _toastService.ShowToastAsync("Error deleting session");
            }
        }

        private async Task LoadSavedSessionsAsync()
        {
            try
            {
                IsLoadingSessions = true;
                OnPropertyChanged(nameof(HasNoSavedSessions));

                var sessions = await _databaseService.GetRecentSessionSnapshotsAsync(20);

                SavedSessions.Clear();
                foreach (var session in sessions)
                {
                    SavedSessions.Add(session);
                }

                _logger.LogDebug("Loaded {SessionCount} saved sessions", sessions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading saved sessions");
                await _toastService.ShowToastAsync("Error loading saved sessions");
            }
            finally
            {
                IsLoadingSessions = false;
                OnPropertyChanged(nameof(HasNoSavedSessions));
            }
        }

        #endregion

        #region Data Methods

        private void InitializeCollections()
        {
            SelectableWords.Clear();
            SelectablePalette.Clear();
            SelectableImages.Clear();
        }

        private void LoadDataFromSession()
        {
            if (GalleryData == null) return;

            try
            {
                _logger.LogDebug("Loading session data into creation form");

                // Load words
                SelectableWords.Clear();
                foreach (var word in GalleryData.AvailableWords)
                {
                    SelectableWords.Add(new SelectableWord { Word = word, IsSelected = true });
                }

                // Load palette (auto-select all)
                SelectablePalette.Clear();
                foreach (var color in GalleryData.AvailablePalette)
                {
                    SelectablePalette.Add(new SelectableColor { Color = color, IsSelected = true });
                }

                // Load images
                SelectableImages.Clear();
                foreach (var image in GalleryData.AvailableImages)
                {
                    SelectableImages.Add(new SelectableImage { Image = image.UnsplashImage, IsSelected = true });
                }

                // Set session duration if available
                if (!string.IsNullOrEmpty(GalleryData.SessionDuration))
                {
                    Title = $"Study Session - {GalleryData.SessionDuration}";
                }

                OnPropertyChanged(nameof(HasSessionData));
                _logger.LogDebug("Loaded session data: {WordCount} words, {ColorCount} colors, {ImageCount} images",
                    SelectableWords.Count, SelectablePalette.Count, SelectableImages.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading session data");
            }
        }

        private UserUploadedImage BuildGalleryItem()
        {
            var selectedWords = SelectableWords.Where(w => w.IsSelected).Select(w => w.Word).ToList();
            var selectedColors = SelectablePalette.Where(c => c.IsSelected).Select(c => c.Color.ToArgbHex()).ToList();
            var selectedImages = SelectableImages.Where(i => i.IsSelected).Select(i => i.Image).ToList();

            var customTags = ParseCustomTags();

            return new UserUploadedImage
            {
                Title = string.IsNullOrWhiteSpace(Title) ? null : Title.Trim(),
                ArtworkImagePath = ArtworkImagePath,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                CustomTags = customTags.Count > 0 ? JsonSerializer.Serialize(customTags) : null,
                SessionDuration = GalleryData?.SessionDuration,
                GeneratedWords = selectedWords.Count > 0 ? JsonSerializer.Serialize(selectedWords) : null,
                GeneratedPalette = selectedColors.Count > 0 ? JsonSerializer.Serialize(selectedColors) : null,
                GeneratedImages = selectedImages.Count > 0 ? JsonSerializer.Serialize(selectedImages) : null,
                CreatedAt = DateTime.Now
            };
        }

        private List<string> ParseCustomTags()
        {
            if (string.IsNullOrWhiteSpace(CustomTagsText))
                return new List<string>();

            return CustomTagsText
                .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(tag => tag.Trim())
                .Where(tag => !string.IsNullOrEmpty(tag))
                .Distinct()
                .ToList();
        }

        private async Task<string> SaveImageAsync(FileResult fileResult)
        {
            var fileName = $"artwork_{DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileName(fileResult.FileName)}";
            var targetPath = Path.Combine(FileSystem.AppDataDirectory, "Gallery", fileName);

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

            // Copy the file properly with disposal
            using (var sourceStream = await fileResult.OpenReadAsync())
            using (var targetStream = File.Create(targetPath))
            {
                await sourceStream.CopyToAsync(targetStream);
            } // Streams are properly disposed here

            // Now create temp copy for preview (after original streams are closed)
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), fileName);

                // Give a small delay to ensure file is released
                await Task.Delay(100);

                File.Copy(targetPath, tempPath, true);

                _logger.LogDebug("=== Main file: {TargetPath} ===", targetPath);
                _logger.LogDebug("=== Temp file: {TempPath} ===", tempPath);

                // Store temp path for preview
                _tempPreviewPath = tempPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Temp copy failed");
                // If temp copy fails, just use original path
                _tempPreviewPath = string.Empty;
            }

            return targetPath;
        }

        private string _tempPreviewPath = string.Empty;

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

    #region Supporting Models

    public class SelectableWord : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string Word { get; set; } = string.Empty;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand ToggleSelectionCommand { get; }

        public SelectableWord()
        {
            ToggleSelectionCommand = new RelayCommand(() => IsSelected = !IsSelected);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class SelectableColor : INotifyPropertyChanged
    {
        private bool _isSelected;

        public required Color Color { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public string HexValue => Color.ToArgbHex();

        public RelayCommand ToggleSelectionCommand { get; }

        public SelectableColor()
        {
            ToggleSelectionCommand = new RelayCommand(() => IsSelected = !IsSelected);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class SelectableImage : INotifyPropertyChanged
    {
        private bool _isSelected;

        public UnsplashImage Image { get; set; } = new();

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public string ThumbUrl => Image.urls?.Thumb ?? string.Empty;
        public string UserName => Image.user?.Name ?? "Unknown";

        public RelayCommand ToggleSelectionCommand { get; }

        public SelectableImage()
        {
            ToggleSelectionCommand = new RelayCommand(() => IsSelected = !IsSelected);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Helper classes for grouping favorites
    public class WordCollectionGroup : ObservableCollection<WordCollection>
    {
        public string CollectionName { get; }

        public WordCollectionGroup(string collectionName, IEnumerable<WordCollection> words) : base(words)
        {
            CollectionName = collectionName;
        }
    }

    #endregion
}