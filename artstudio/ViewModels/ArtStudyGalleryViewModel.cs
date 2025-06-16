using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.Input;
using artstudio.Data.Models;
using artstudio.Service;
using Microsoft.Extensions.Logging;

namespace artstudio.ViewModels
{
    public partial class ArtStudyGalleryViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly ILogger<ArtStudyGalleryViewModel> _logger;
        private readonly string _imagesDirectory;

        private UserUploadedImage? _selectedImage;
        private bool _isImagePopupVisible;
        private bool _isDetailsExpanded;
        private bool _isEditingDetails;
        private string _editableTitle = string.Empty;
        private string _editableDescription = string.Empty;
        private string _editableTags = string.Empty;
        private bool _editableIsFavorite;
        private bool _isSelectingReferences;
        private string _searchQuery = string.Empty;
        private SortOption _sortOption = SortOption.Newest;

        public ArtStudyGalleryViewModel(DatabaseService databaseService, ILogger<ArtStudyGalleryViewModel> logger)
        {
            _databaseService = databaseService;
            _logger = logger;

            // Create images directory
            _imagesDirectory = Path.Combine(FileSystem.AppDataDirectory, "UploadedImages");
            Directory.CreateDirectory(_imagesDirectory);

            InitializeCommands();
            _ = LoadStudyImagesAsync();
        }

        #region Properties

        public ObservableCollection<UserUploadedImage> AllStudyImages { get; } = new();
        public ObservableCollection<UserUploadedImage> FilteredImages { get; } = new();

        // For content references
        public ObservableCollection<ImageReference> CurrentReferences { get; } = new();
        public ObservableCollection<PaletteCollection> AvailablePalettes { get; } = new();
        public ObservableCollection<WordCollection> AvailableWordCollections { get; } = new();
        public ObservableCollection<FavoriteSwatch> AvailableSwatches { get; } = new();

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                OnPropertyChanged();
                ApplySearchAndSort();
            }
        }

        public SortOption SortOption
        {
            get => _sortOption;
            set
            {
                _sortOption = value;
                OnPropertyChanged();
                ApplySearchAndSort();
            }
        }

        public UserUploadedImage? SelectedImage
        {
            get => _selectedImage;
            set
            {
                _selectedImage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FavoriteStatusText));
                ResetEditingState();
            }
        }

        public bool IsImagePopupVisible
        {
            get => _isImagePopupVisible;
            set
            {
                _isImagePopupVisible = value;
                OnPropertyChanged();

                if (!value)
                {
                    IsDetailsExpanded = false;
                    IsEditingDetails = false;
                    SelectedImage = null;
                }
            }
        }

        public bool IsDetailsExpanded
        {
            get => _isDetailsExpanded;
            set
            {
                _isDetailsExpanded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DetailsToggleText));
            }
        }

        public bool IsEditingDetails
        {
            get => _isEditingDetails;
            set
            {
                _isEditingDetails = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotEditingDetails));

                if (value && SelectedImage != null)
                {
                    EditableTitle = SelectedImage.Title;
                    EditableDescription = SelectedImage.Description;
                    EditableTags = SelectedImage.Tags;
                    EditableIsFavorite = SelectedImage.IsFavorite;
                }
            }
        }

        public string EditableTitle
        {
            get => _editableTitle;
            set => SetProperty(ref _editableTitle, value);
        }

        public string EditableDescription
        {
            get => _editableDescription;
            set => SetProperty(ref _editableDescription, value);
        }

        public string EditableTags
        {
            get => _editableTags;
            set => SetProperty(ref _editableTags, value);
        }

        public bool EditableIsFavorite
        {
            get => _editableIsFavorite;
            set => SetProperty(ref _editableIsFavorite, value);
        }

        public bool IsSelectingReferences
        {
            get => _isSelectingReferences;
            set => SetProperty(ref _isSelectingReferences, value);
        }

        // Display properties
        public bool HasStudyImages => FilteredImages.Count > 0;
        public bool IsEmptyState => !HasStudyImages;
        public bool IsNotEditingDetails => !IsEditingDetails;
        public string DetailsToggleText => IsDetailsExpanded ? "Hide Details" : "Show Details";
        public string FavoriteStatusText => SelectedImage?.IsFavorite == true ? "❤️ Favorite" : "🤍 Not Favorite";
        public int TotalCount => AllStudyImages.Count;
        public int FavoriteCount => AllStudyImages.Count(img => img.IsFavorite);
        public string StudyStatsDisplay => $"{TotalCount} studies • {FavoriteCount} favorites";
        public bool HasReferences => CurrentReferences.Count > 0;
        public string ReferencesDisplay => HasReferences ?
            $"Inspired by {CurrentReferences.Count} favorite{(CurrentReferences.Count != 1 ? "s" : "")}" :
            "No references yet. Click to add.";

        #endregion

        #region Commands

        public RelayCommand<UserUploadedImage> ViewImageCommand { get; private set; } = default!;
        public AsyncRelayCommand UploadImageCommand { get; private set; } = default!;
        public RelayCommand ToggleDetailsCommand { get; private set; } = default!;
        public RelayCommand EditDetailsCommand { get; private set; } = default!;
        public AsyncRelayCommand SaveDetailsCommand { get; private set; } = default!;
        public RelayCommand CancelEditCommand { get; private set; } = default!;
        public AsyncRelayCommand DeleteImageCommand { get; private set; } = default!;
        public RelayCommand<SortOption> SortCommand { get; private set; } = default!;
        public RelayCommand ClearSearchCommand { get; private set; } = default!;
        public RelayCommand ClosePopupCommand { get; private set; } = default!;
        public AsyncRelayCommand SelectReferencesCommand { get; private set; } = default!;
        public AsyncRelayCommand<object> AddReferenceCommand { get; private set; } = default!;
        public AsyncRelayCommand<ImageReference> RemoveReferenceCommand { get; private set; } = default!;

        private void InitializeCommands()
        {
            ViewImageCommand = new RelayCommand<UserUploadedImage>(ViewImage);
            UploadImageCommand = new AsyncRelayCommand(ExecuteUploadAsync);
            ToggleDetailsCommand = new RelayCommand(() => IsDetailsExpanded = !IsDetailsExpanded);
            EditDetailsCommand = new RelayCommand(() => IsEditingDetails = true);
            SaveDetailsCommand = new AsyncRelayCommand(SaveDetailsAsync);
            CancelEditCommand = new RelayCommand(() => IsEditingDetails = false);
            DeleteImageCommand = new AsyncRelayCommand(DeleteImageAsync);
            SortCommand = new RelayCommand<SortOption>((sort) => SortOption = sort);
            ClearSearchCommand = new RelayCommand(() => SearchQuery = string.Empty);
            ClosePopupCommand = new RelayCommand(() => IsImagePopupVisible = false);
            SelectReferencesCommand = new AsyncRelayCommand(LoadReferencesAsync);
            AddReferenceCommand = new AsyncRelayCommand<object>(AddReferenceAsync);
            RemoveReferenceCommand = new AsyncRelayCommand<ImageReference>(RemoveReferenceAsync);
        }

        #endregion

        #region Command Implementations

        private void ViewImage(UserUploadedImage? image)
        {
            if (image == null) return;

            SelectedImage = image;
            IsImagePopupVisible = true;
            IsDetailsExpanded = false;

            // Load references for this image
            _ = SelectReferencesCommand.ExecuteAsync(null);
        }

        private async Task ExecuteUploadAsync()
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Select an image",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    var uploadedImage = await ProcessUploadedFileAsync(result);
                    if (uploadedImage != null)
                    {
                        await LoadStudyImagesAsync(); // Refresh gallery
                        ViewImage(uploadedImage); // Show the uploaded image
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload image");
                // Consider showing user-friendly error message
            }
        }

        private async Task<UserUploadedImage?> ProcessUploadedFileAsync(FileResult fileResult)
        {
            try
            {
                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(fileResult.FileName)}";
                var localPath = Path.Combine(_imagesDirectory, fileName);

                // Copy file to app directory
                using (var sourceStream = await fileResult.OpenReadAsync())
                using (var destStream = File.Create(localPath))
                {
                    await sourceStream.CopyToAsync(destStream);
                }

                // Get file info
                var fileInfo = new FileInfo(localPath);

                // Create database record
                var uploadedImage = new UserUploadedImage
                {
                    Title = Path.GetFileNameWithoutExtension(fileResult.FileName),
                    LocalFilePath = localPath,
                    OriginalFileName = fileResult.FileName,
                    Description = string.Empty,
                    Tags = string.Empty,
                    Width = 0, // Skip dimensions for desktop simplicity
                    Height = 0,
                    FileSizeBytes = fileInfo.Length,
                    FileType = Path.GetExtension(fileResult.FileName).TrimStart('.').ToLower(),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsFavorite = false
                };

                // Save to database
                await _databaseService.SaveUserUploadedImageAsync(uploadedImage);

                _logger.LogDebug("Uploaded image {FileName}", fileResult.FileName);
                return uploadedImage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process uploaded file {FileName}", fileResult.FileName);
                return null;
            }
        }

        private async Task SaveDetailsAsync()
        {
            if (SelectedImage == null) return;

            try
            {
                // Update the image object
                SelectedImage.Title = EditableTitle;
                SelectedImage.Description = EditableDescription;
                SelectedImage.Tags = EditableTags;
                SelectedImage.IsFavorite = EditableIsFavorite;

                // Save to database
                await _databaseService.UpdateUserUploadedImageAsync(SelectedImage);

                IsEditingDetails = false;
                ApplySearchAndSort(); // Refresh if needed
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save image details");
                // Consider showing user-friendly error message
            }
        }

        private async Task DeleteImageAsync()
        {
            if (SelectedImage == null) return;

            try
            {
                // Delete physical file
                if (File.Exists(SelectedImage.LocalFilePath))
                {
                    File.Delete(SelectedImage.LocalFilePath);
                }

                // Delete from database
                await _databaseService.DeleteUserUploadedImageAsync(SelectedImage.Id);

                IsImagePopupVisible = false;
                await LoadStudyImagesAsync(); // Refresh gallery
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete image");
                // Consider showing user-friendly error message
            }
        }

        private void ResetEditingState()
        {
            IsEditingDetails = false;
            IsSelectingReferences = false;
            EditableTitle = string.Empty;
            EditableDescription = string.Empty;
            EditableTags = string.Empty;
            EditableIsFavorite = false;
        }

        #endregion

        #region Reference Management

        private async Task LoadReferencesAsync()
        {
            if (SelectedImage == null) return;

            try
            {
                IsSelectingReferences = true;

                // Load current references for this image
                var references = await _databaseService.GetImageReferencesAsync(SelectedImage.Id);
                CurrentReferences.Clear();
                foreach (var reference in references)
                {
                    CurrentReferences.Add(reference);
                }

                // Load available favorites to choose from
                var palettes = await _databaseService.GetFavoritePalettesAsync();
                var wordCollections = await _databaseService.GetFavoritesAsync(); // Your existing method
                var swatches = await _databaseService.GetFavoriteSwatchesAsync();

                AvailablePalettes.Clear();
                foreach (var palette in palettes) AvailablePalettes.Add(palette);

                AvailableWordCollections.Clear();
                foreach (var words in wordCollections) AvailableWordCollections.Add(words);

                AvailableSwatches.Clear();
                foreach (var swatch in swatches) AvailableSwatches.Add(swatch);

                OnPropertyChanged(nameof(HasReferences));
                OnPropertyChanged(nameof(ReferencesDisplay));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load references");
                // Consider showing user-friendly error message
            }
        }

        private async Task AddReferenceAsync(object? item)
        {
            if (SelectedImage == null || item == null) return;

            try
            {
                ImageReference reference;

                // Determine the reference type and create appropriate reference
                switch (item)
                {
                    case PaletteCollection palette:
                        reference = new ImageReference
                        {
                            UserUploadedImageId = SelectedImage.Id,
                            ReferenceType = "PaletteCollection",
                            ReferenceId = palette.Id,
                            ReferenceDescription = palette.DisplayTitle,
                            CreatedAt = DateTime.Now
                        };
                        break;

                    case WordCollection wordCollection:
                        reference = new ImageReference
                        {
                            UserUploadedImageId = SelectedImage.Id,
                            ReferenceType = "WordCollection",
                            ReferenceId = wordCollection.Id,
                            ReferenceDescription = wordCollection.DisplayTitle,
                            CreatedAt = DateTime.Now
                        };
                        break;

                    case FavoriteSwatch swatch:
                        reference = new ImageReference
                        {
                            UserUploadedImageId = SelectedImage.Id,
                            ReferenceType = "FavoriteSwatch",
                            ReferenceId = swatch.Id,
                            ReferenceDescription = swatch.DisplayName,
                            CreatedAt = DateTime.Now
                        };
                        break;

                    default:
                        _logger.LogWarning("Unknown reference type: {ItemType}", item.GetType().Name);
                        return;
                }

                // Check if reference already exists
                bool alreadyExists = CurrentReferences.Any(r =>
                    r.ReferenceType == reference.ReferenceType &&
                    r.ReferenceId == reference.ReferenceId);

                if (!alreadyExists)
                {
                    await _databaseService.SaveImageReferenceAsync(reference);
                    CurrentReferences.Add(reference);

                    OnPropertyChanged(nameof(HasReferences));
                    OnPropertyChanged(nameof(ReferencesDisplay));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add reference");
                // Consider showing user-friendly error message
            }
        }

        private async Task RemoveReferenceAsync(ImageReference? reference)
        {
            if (reference == null) return;

            try
            {
                await _databaseService.DeleteImageReferenceAsync(reference.Id);
                CurrentReferences.Remove(reference);

                OnPropertyChanged(nameof(HasReferences));
                OnPropertyChanged(nameof(ReferencesDisplay));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove reference");
                // Consider showing user-friendly error message
            }
        }

        #endregion

        #region Data Management

        private async Task LoadStudyImagesAsync()
        {
            try
            {
                var images = await _databaseService.GetUserUploadedImagesAsync();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    AllStudyImages.Clear();
                    foreach (var image in images)
                    {
                        AllStudyImages.Add(image);
                    }

                    ApplySearchAndSort();
                    OnPropertyChanged(nameof(HasStudyImages));
                    OnPropertyChanged(nameof(TotalCount));
                    OnPropertyChanged(nameof(FavoriteCount));
                    OnPropertyChanged(nameof(StudyStatsDisplay));
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load study images");
                // Consider showing user-friendly error message
            }
        }

        private void ApplySearchAndSort()
        {
            var results = AllStudyImages.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var query = SearchQuery.ToLower();
                results = results.Where(img =>
                    img.Title.ToLower().Contains(query) ||
                    img.Description.ToLower().Contains(query) ||
                    img.Tags.ToLower().Contains(query));
            }

            // Apply sorting
            results = SortOption switch
            {
                SortOption.Newest => results.OrderByDescending(img => img.CreatedAt),
                SortOption.Oldest => results.OrderBy(img => img.CreatedAt),
                SortOption.Favorites => results.OrderByDescending(img => img.IsFavorite).ThenByDescending(img => img.CreatedAt),
                SortOption.Title => results.OrderBy(img => img.Title),
                _ => results.OrderByDescending(img => img.CreatedAt)
            };

            FilteredImages.Clear();
            foreach (var image in results)
            {
                FilteredImages.Add(image);
            }

            OnPropertyChanged(nameof(HasStudyImages));
            OnPropertyChanged(nameof(IsEmptyState));
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

        public List<SortOption> SortOptions { get; } = new()
        {
            SortOption.Newest,
            SortOption.Oldest,
            SortOption.Favorites,
            SortOption.Title
        };
    }

    public enum SortOption
    {
        Newest,
        Oldest,
        Favorites,
        Title
    }
}