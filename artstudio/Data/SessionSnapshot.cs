using SQLite;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using artstudio.Models;
using artstudio.ViewModels;

namespace artstudio.Data
{
    [Table("SessionSnapshot")]
    public class SessionSnapshot : INotifyPropertyChanged
    {
        private int _id;
        private string? _title;
        private string? _savedWords;
        private string? _savedPalette;
        private string? _savedImages;
        private DateTime _createdAt;
        private string? _sessionMode;
        private string? _sessionDuration;

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

        public string? SavedWords
        {
            get => _savedWords;
            set => SetProperty(ref _savedWords, value);
        }

        public string? SavedPalette
        {
            get => _savedPalette;
            set => SetProperty(ref _savedPalette, value);
        }

        public string? SavedImages
        {
            get => _savedImages;
            set => SetProperty(ref _savedImages, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        public string? SessionMode
        {
            get => _sessionMode;
            set => SetProperty(ref _sessionMode, value);
        }

        public string? SessionDuration
        {
            get => _sessionDuration;
            set => SetProperty(ref _sessionDuration, value);
        }

        // Display properties
        public string DisplayTitle => !string.IsNullOrEmpty(Title) ? Title : $"Session {CreatedAt:MMM dd, HH:mm}";
        public string FormattedDate => CreatedAt.ToString("MMM dd, yyyy 'at' HH:mm");

        // Helper methods for JSON parsing
        [Ignore]
        public List<string> WordsList
        {
            get
            {
                try
                {
                    return string.IsNullOrEmpty(SavedWords) ? new() :
                           JsonSerializer.Deserialize<List<string>>(SavedWords) ?? new();
                }
                catch
                {
                    return new();
                }
            }
        }

        [Ignore]
        public List<string> PaletteList
        {
            get
            {
                try
                {
                    return string.IsNullOrEmpty(SavedPalette) ? new() :
                           JsonSerializer.Deserialize<List<string>>(SavedPalette) ?? new();
                }
                catch
                {
                    return new();
                }
            }
        }

        [Ignore]
        public List<UnsplashImage> ImagesList
        {
            get
            {
                try
                {
                    return string.IsNullOrEmpty(SavedImages) ? new() :
                           JsonSerializer.Deserialize<List<UnsplashImage>>(SavedImages) ?? new();
                }
                catch
                {
                    return new();
                }
            }
        }

        // Helper methods to check if sections have content
        [Ignore]
        public bool HasWords => WordsList.Any();

        [Ignore]
        public bool HasPalette => PaletteList.Any();

        [Ignore]
        public bool HasImages => ImagesList.Any();

        [Ignore]
        public bool HasContent => HasWords || HasPalette || HasImages;

        // Methods to create from current session
        public static SessionSnapshot FromCurrentSession(
            List<string> words,
            List<Color> palette,
            List<UnsplashImage> images, // Changed from List<ImageItemViewModel>
            string sessionMode,
            string sessionDuration,
            string? customTitle = null)
        {
            // Convert colors to hex strings
            var paletteHex = palette.Select(c => c.ToArgbHex()).ToList();

            // Images are already UnsplashImage objects now
            var unsplashImages = images;

            return new SessionSnapshot
            {
                Title = customTitle,
                SavedWords = words.Any() ? JsonSerializer.Serialize(words) : null,
                SavedPalette = paletteHex.Any() ? JsonSerializer.Serialize(paletteHex) : null,
                SavedImages = unsplashImages.Any() ? JsonSerializer.Serialize(unsplashImages) : null,
                SessionMode = sessionMode,
                SessionDuration = sessionDuration,
                CreatedAt = DateTime.Now
            };
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
                OnPropertyChanged(nameof(DisplayTitle));
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