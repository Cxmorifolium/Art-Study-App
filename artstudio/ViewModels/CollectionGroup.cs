using System.Collections.ObjectModel;
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

    public class PaletteCollectionGroup
    {
        public string CollectionName { get; }
        public ObservableCollection<PaletteCollection> Palettes { get; }
        public int Count => Palettes.Count;
        public string DisplayText => $"{CollectionName} ({Count})";

        public PaletteCollectionGroup(string collectionName, IEnumerable<PaletteCollection> palettes)
        {
            CollectionName = collectionName;
            Palettes = new ObservableCollection<PaletteCollection>(palettes);
        }
    }
}