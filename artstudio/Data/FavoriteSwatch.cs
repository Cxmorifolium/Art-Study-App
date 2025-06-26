using SQLite;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace artstudio.Data
{
    [Table("FavoriteSwatch")]
    public class FavoriteSwatch : INotifyPropertyChanged
    {
        private int _id;
        private string _hexColor = string.Empty;
        private string? _colorName;
        private DateTime _createdAt;
        private string? _collection;
        private bool _isFavorite;

        [PrimaryKey, AutoIncrement]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        [NotNull]
        public string HexColor
        {
            get => _hexColor;
            set => SetProperty(ref _hexColor, value ?? string.Empty);
        }

        public string? ColorName
        {
            get => _colorName;
            set => SetProperty(ref _colorName, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        public string? Collection
        {
            get => _collection;
            set => SetProperty(ref _collection, value);
        }

        public bool IsFavorite
        {
            get => _isFavorite;
            set => SetProperty(ref _isFavorite, value);
        }

        // Helper properties
        [Ignore]
        public Color Color
        {
            get => Color.FromArgb(HexColor);
            set => HexColor = value.ToArgbHex();
        }

        public string DisplayName => !string.IsNullOrEmpty(ColorName) ? ColorName : HexColor;

        public string FormattedDate => CreatedAt.ToString("MMM dd, yyyy 'at' HH:mm");

        public string CollectionDisplay => !string.IsNullOrEmpty(Collection) ? Collection : "Default";

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (propertyName == nameof(HexColor))
            {
                OnPropertyChanged(nameof(Color));
                OnPropertyChanged(nameof(DisplayName));
            }
            else if (propertyName == nameof(ColorName))
            {
                OnPropertyChanged(nameof(DisplayName));
            }
            else if (propertyName == nameof(Collection))
            {
                OnPropertyChanged(nameof(CollectionDisplay));
            }
            else if (propertyName == nameof(CreatedAt))
            {
                OnPropertyChanged(nameof(FormattedDate));
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
