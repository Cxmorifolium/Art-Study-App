using SQLite;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace artstudio.Data
{
    [Table("Word")]
    public class Word : INotifyPropertyChanged
    {
        private int _id;
        private int _wordCollectionId;
        private string _text = string.Empty;
        private string? _category;
        private DateTime _createdAt;

        [PrimaryKey, AutoIncrement]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        [Indexed] // Index for faster queries by collection
        public int WordCollectionId
        {
            get => _wordCollectionId;
            set => SetProperty(ref _wordCollectionId, value);
        }

        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value ?? string.Empty);
        }

        public string? Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        // Display properties
        public string DisplayCategory => !string.IsNullOrEmpty(Category) ? Category : "general";

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            // Notify dependent properties when base properties change
            if (propertyName == nameof(Category))
            {
                OnPropertyChanged(nameof(DisplayCategory));
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

        public override string ToString()
        {
            return $"{Text} ({DisplayCategory})";
        }
    }
}