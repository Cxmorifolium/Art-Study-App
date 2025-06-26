using SQLite;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using artstudio.Models;

namespace artstudio.Data
{
    // For Gallery Page

    [Table("UserUploadedImage")]
    public class UserUploadedImage : INotifyPropertyChanged
    {
        private int _id;
        private string? _title;
        private string _artworkImagePath = string.Empty;
        private string? _notes;
        private string? _customTags;
        private string? _sessionDuration;
        private string? _generatedWords;
        private string? _generatedPalette;
        private string? _generatedImages;
        private DateTime _createdAt;

        [PrimaryKey, AutoIncrement]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string? Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        [NotNull]
        public string ArtworkImagePath
        {
            get => _artworkImagePath;
            set => SetProperty(ref _artworkImagePath, value ?? string.Empty);
        }

        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public string? CustomTags
        {
            get => _customTags;
            set => SetProperty(ref _customTags, value);
        }

        public string? SessionDuration
        {
            get => _sessionDuration;
            set => SetProperty(ref _sessionDuration, value);
        }

        public string? GeneratedWords
        {
            get => _generatedWords;
            set => SetProperty(ref _generatedWords, value);
        }

        public string? GeneratedPalette
        {
            get => _generatedPalette;
            set => SetProperty(ref _generatedPalette, value);
        }

        public string? GeneratedImages
        {
            get => _generatedImages;
            set => SetProperty(ref _generatedImages, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        // Display properties
        public string DisplayTitle => !string.IsNullOrEmpty(Title) ? Title : "Untitled Study";
        public string FormattedDate => CreatedAt.ToString("MMM dd, yyyy 'at' HH:mm");
        public string ShortDate => CreatedAt.ToString("MMM dd");

        // Helper methods for JSON parsing
        public List<string> WordsList
        {
            get
            {
                try
                {
                    return string.IsNullOrEmpty(GeneratedWords) ? new() :
                           JsonSerializer.Deserialize<List<string>>(GeneratedWords) ?? new();
                }
                catch
                {
                    return new();
                }
            }
        }

        public List<string> PaletteList
        {
            get
            {
                try
                {
                    return string.IsNullOrEmpty(GeneratedPalette) ? new() :
                           JsonSerializer.Deserialize<List<string>>(GeneratedPalette) ?? new();
                }
                catch
                {
                    return new();
                }
            }
        }

        public List<UnsplashImage> ImagesList
        {
            get
            {
                try
                {
                    return string.IsNullOrEmpty(GeneratedImages) ? new() :
                           JsonSerializer.Deserialize<List<UnsplashImage>>(GeneratedImages) ?? new();
                }
                catch
                {
                    return new();
                }
            }
        }

        public List<string> CustomTagsList
        {
            get
            {
                try
                {
                    return string.IsNullOrEmpty(CustomTags) ? new() :
                           JsonSerializer.Deserialize<List<string>>(CustomTags) ?? new();
                }
                catch
                {
                    return new();
                }
            }
        }

        // Helper methods to check if sections have content
        public bool HasWords() => WordsList.Any();

        public bool HasPalette() => PaletteList.Any();

        public bool HasImages() => ImagesList.Any();

        public bool HasCustomTags() => CustomTagsList.Any();

        public bool HasSessionDuration => !string.IsNullOrEmpty(SessionDuration);

        // Methods to update the JSON fields
        public void UpdateWordsList(List<string> words)
        {
            GeneratedWords = words.Any() ? JsonSerializer.Serialize(words) : null;
            OnPropertyChanged(nameof(GeneratedWords));
            OnPropertyChanged(nameof(WordsList));
            OnPropertyChanged(nameof(HasWords));
        }

        public void UpdatePaletteList(List<string> palette)
        {
            GeneratedPalette = palette.Any() ? JsonSerializer.Serialize(palette) : null;
            OnPropertyChanged(nameof(GeneratedPalette));
            OnPropertyChanged(nameof(PaletteList));
            OnPropertyChanged(nameof(HasPalette));
        }

        public void UpdateImagesList(List<UnsplashImage> images)
        {
            GeneratedImages = images.Any() ? JsonSerializer.Serialize(images) : null;
            OnPropertyChanged(nameof(GeneratedImages));
            OnPropertyChanged(nameof(ImagesList));
            OnPropertyChanged(nameof(HasImages));
        }

        public void UpdateCustomTagsList(List<string> tags)
        {
            CustomTags = tags.Any() ? JsonSerializer.Serialize(tags) : null;
            OnPropertyChanged(nameof(CustomTags));
            OnPropertyChanged(nameof(CustomTagsList));
            OnPropertyChanged(nameof(HasCustomTags));
        }

        // Methods to add individual items
        public void AddWords(List<string> newWords)
        {
            var currentWords = WordsList.ToList();
            currentWords.AddRange(newWords.Where(w => !currentWords.Contains(w, StringComparer.OrdinalIgnoreCase)));
            UpdateWordsList(currentWords);
        }

        public void AddColors(List<string> newColors)
        {
            var currentPalette = PaletteList.ToList();
            currentPalette.AddRange(newColors.Where(c => !currentPalette.Contains(c, StringComparer.OrdinalIgnoreCase)));
            UpdatePaletteList(currentPalette);
        }

        public void AddImages(List<UnsplashImage> newImages)
        {
            var currentImages = ImagesList.ToList();
            var existingIds = currentImages.Select(img => img.Id).ToHashSet();
            currentImages.AddRange(newImages.Where(img => !existingIds.Contains(img.Id)));
            UpdateImagesList(currentImages);
        }

        public void AddCustomTags(List<string> newTags)
        {
            var currentTags = CustomTagsList.ToList();
            currentTags.AddRange(newTags.Where(t => !currentTags.Contains(t, StringComparer.OrdinalIgnoreCase)));
            UpdateCustomTagsList(currentTags);
        }

        // Methods to remove individual items
        public void RemoveWord(string word)
        {
            var currentWords = WordsList.ToList();
            currentWords.RemoveAll(w => w.Equals(word, StringComparison.OrdinalIgnoreCase));
            UpdateWordsList(currentWords);
        }

        public void RemoveColor(string color)
        {
            var currentPalette = PaletteList.ToList();
            currentPalette.RemoveAll(c => c.Equals(color, StringComparison.OrdinalIgnoreCase));
            UpdatePaletteList(currentPalette);
        }

        public void RemoveImage(string imageId)
        {
            var currentImages = ImagesList.ToList();
            currentImages.RemoveAll(img => img.Id == imageId);
            UpdateImagesList(currentImages);
        }

        public void RemoveCustomTag(string tag)
        {
            var currentTags = CustomTagsList.ToList();
            currentTags.RemoveAll(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase));
            UpdateCustomTagsList(currentTags);
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (propertyName == nameof(Title))
            {
                OnPropertyChanged(nameof(DisplayTitle));
            }
            else if (propertyName == nameof(CreatedAt))
            {
                OnPropertyChanged(nameof(FormattedDate));
                OnPropertyChanged(nameof(ShortDate));
            }
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}