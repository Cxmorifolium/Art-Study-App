using System.Collections.ObjectModel;
using System.ComponentModel;
using artstudio.Data.Models;

namespace artstudio.ViewModels
{
    public class SwatchCollectionGroup
    {
        public string CollectionName { get; }
        public ObservableCollection<FavoriteSwatch> Swatches { get; }
        public int Count => Swatches.Count;
        public string DisplayText => $"{CollectionName} ({Count})";

        public SwatchCollectionGroup(string collectionName, IEnumerable<FavoriteSwatch> swatches)
        {
            CollectionName = collectionName;
            Swatches = new ObservableCollection<FavoriteSwatch>(swatches);
        }
    }

    // Fix: PaletteCollectionGroup should NOT inherit from ObservableCollection<FavoritePaletteItem>
    // because it creates confusion in the DataTemplate binding
    public class PaletteCollectionGroup : INotifyPropertyChanged
    {
        public string CollectionName { get; }

        private ObservableCollection<FavoritePaletteItem> _palettes;
        public ObservableCollection<FavoritePaletteItem> Palettes
        {
            get => _palettes;
            set
            {
                _palettes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Count));
                OnPropertyChanged(nameof(CountText));
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        public int Count => Palettes?.Count ?? 0;

        // Display properties
        public string DisplayName => CollectionName == "Default" ? "My Palettes" : CollectionName;
        public string CountText => $"{Count} palette{(Count == 1 ? "" : "s")}";

        public PaletteCollectionGroup(string collectionName, IEnumerable<FavoritePaletteItem> palettes)
        {
            CollectionName = collectionName;
            _palettes = new ObservableCollection<FavoritePaletteItem>(palettes);

            // Subscribe to collection changes to update counts
            _palettes.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(Count));
                OnPropertyChanged(nameof(CountText));
            };
        }

        // Method to remove palette from this group
        public void Remove(FavoritePaletteItem palette)
        {
            Palettes.Remove(palette);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    #region Helper Classes
    // Helper classes for favorites data
    public class FavoriteSwatchItem
    {
        public string HexColor { get; set; } = string.Empty;
        public string? CollectionName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class FavoritePaletteItem
    {
        public int Id { get; set; } // Add ID for database operations
        public string Title { get; set; } = string.Empty;
        public List<string> Colors { get; set; } = new();
        public DateTime CreatedAt { get; set; }

        // Helper property for display
        public string DisplayTitle =>
            Title.Length > 40 ? Title.Substring(0, 37) + "..." : Title;
    }
    #endregion
}