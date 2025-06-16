using SQLite;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace artstudio.Data.Models
{
    [Table("UserUploadedImage")]
    public class UserUploadedImage : INotifyPropertyChanged
    {
        private int _id;
        private string _title = string.Empty;
        private string _localFilePath = string.Empty;
        private string _originalFileName = string.Empty;
        private string _description = string.Empty;
        private string _tags = string.Empty;
        private bool _isFavorite;
        private int _width;
        private int _height;
        private long _fileSizeBytes;
        private string _fileType = string.Empty;
        private DateTime _createdAt;
        private DateTime _updatedAt;

        [PrimaryKey, AutoIncrement]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        [NotNull]
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value ?? string.Empty);
        }

        [NotNull]
        public string LocalFilePath
        {
            get => _localFilePath;
            set => SetProperty(ref _localFilePath, value ?? string.Empty);
        }

        [NotNull]
        public string OriginalFileName
        {
            get => _originalFileName;
            set => SetProperty(ref _originalFileName, value ?? string.Empty);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value ?? string.Empty);
        }

        public string Tags
        {
            get => _tags;
            set => SetProperty(ref _tags, value ?? string.Empty);
        }

        public bool IsFavorite
        {
            get => _isFavorite;
            set => SetProperty(ref _isFavorite, value);
        }

        public int Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        public int Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        public long FileSizeBytes
        {
            get => _fileSizeBytes;
            set => SetProperty(ref _fileSizeBytes, value);
        }

        public string FileType
        {
            get => _fileType;
            set => SetProperty(ref _fileType, value ?? string.Empty);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set => SetProperty(ref _updatedAt, value);
        }

        // Display properties
        public string DisplayTitle => !string.IsNullOrEmpty(Title) ? Title : OriginalFileName;
        public string FormattedDate => CreatedAt.ToString("MMM dd, yyyy 'at' HH:mm");
        public string FileSizeDisplay => FormatFileSize(FileSizeBytes);
        public string DimensionsDisplay => Width > 0 && Height > 0 ? $"{Width}×{Height}" : "Unknown";
        public string FileInfoDisplay => $"{FileType.ToUpper()} • {FileSizeDisplay} • {DimensionsDisplay}";

        public bool HasTags => !string.IsNullOrWhiteSpace(Tags);
        public List<string> TagsList => HasTags ?
            Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(tag => tag.Trim())
                .Where(tag => !string.IsNullOrEmpty(tag))
                .ToList() :
            new List<string>();

        public string TagsDisplay => HasTags ? string.Join(", ", TagsList) : "No tags";

        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - CreatedAt;
                if (timeSpan.TotalDays >= 1)
                    return $"{(int)timeSpan.TotalDays}d ago";
                if (timeSpan.TotalHours >= 1)
                    return $"{(int)timeSpan.TotalHours}h ago";
                return $"{(int)timeSpan.TotalMinutes}m ago";
            }
        }

        // File URL for image controls
        [Ignore]
        public string FileUrl => File.Exists(LocalFilePath) ? LocalFilePath : string.Empty;

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
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
                OnPropertyChanged(nameof(TimeAgo));
            }
            else if (propertyName == nameof(FileSizeBytes))
            {
                OnPropertyChanged(nameof(FileSizeDisplay));
                OnPropertyChanged(nameof(FileInfoDisplay));
            }
            else if (propertyName == nameof(Width) || propertyName == nameof(Height))
            {
                OnPropertyChanged(nameof(DimensionsDisplay));
                OnPropertyChanged(nameof(FileInfoDisplay));
            }
            else if (propertyName == nameof(FileType))
            {
                OnPropertyChanged(nameof(FileInfoDisplay));
            }
            else if (propertyName == nameof(Tags))
            {
                OnPropertyChanged(nameof(HasTags));
                OnPropertyChanged(nameof(TagsList));
                OnPropertyChanged(nameof(TagsDisplay));
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