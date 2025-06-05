using SQLite;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace artstudio.Data.Models
{
    [Table("PaletteCollection")]
    public class PaletteCollection : INotifyPropertyChanged
    {
        private int _id;
        private string? _title;
        private List<Color> _colorsList = new();
        private DateTime _createdAt;
        private string? _paletteType;
        private bool _isHistory;
        private bool _isFavorite;
        private DateTime? _expiresAt;

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

        [Ignore] // Don't store this directly in the database
        public List<Color> ColorsList
        {
            get => _colorsList;
            set => SetProperty(ref _colorsList, value ?? new List<Color>());
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        public string? PaletteType
        {
            get => _paletteType;
            set => SetProperty(ref _paletteType, value);
        }

        public bool IsHistory
        {
            get => _isHistory;
            set => SetProperty(ref _isHistory, value);
        }

        public bool IsFavorite
        {
            get => _isFavorite;
            set => SetProperty(ref _isFavorite, value);
        }

        public DateTime? ExpiresAt
        {
            get => _expiresAt;
            set => SetProperty(ref _expiresAt, value);
        }

        // Display properties for XAML binding
        public string DisplayTitle => !string.IsNullOrEmpty(Title) ? Title : "Untitled Palette";

        public string FormattedDate => CreatedAt.ToString("MMM dd, yyyy 'at' HH:mm");

        public string ColorsDisplayText
        {
            get
            {
                if (ColorsList == null || !ColorsList.Any())
                    return "No colors";

                return $"{ColorsList.Count} colors";
            }
        }

        public int ColorCount => ColorsList?.Count ?? 0;

        public string ColorCountText => $"{ColorCount} color{(ColorCount != 1 ? "s" : "")}";

        public string PaletteTypeDisplay => PaletteType switch
        {
            "generated" => "Generated",
            "custom" => "Custom",
            "imported" => "Imported",
            _ => "Unknown"
        };


        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            // Notify dependent properties when base properties change
            if (propertyName == nameof(ColorsList))
            {
                OnPropertyChanged(nameof(ColorsDisplayText));
                OnPropertyChanged(nameof(ColorCount));
                OnPropertyChanged(nameof(ColorCountText));
            }
            else if (propertyName == nameof(Title))
            {
                OnPropertyChanged(nameof(DisplayTitle));
            }
            else if (propertyName == nameof(CreatedAt))
            {
                OnPropertyChanged(nameof(FormattedDate));
            }
            else if (propertyName == nameof(PaletteType))
            {
                OnPropertyChanged(nameof(PaletteTypeDisplay));
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

