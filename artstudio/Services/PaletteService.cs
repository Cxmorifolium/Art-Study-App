using artstudio.Data.Models;
using artstudio.Data;
using System.Diagnostics;

namespace artstudio.Services
{
    public class PaletteService
    {
        private readonly DatabaseService _databaseService;

        public PaletteService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        #region Palette Collections

        public async Task<PaletteCollection> SavePaletteCollectionAsync(
            List<Color> colors,
            string? title = null,
            string? paletteType = "generated")
        {
            try
            {
                Debug.WriteLine("=== SavePaletteCollectionAsync START ===");
                Debug.WriteLine($"Title: {title}");
                Debug.WriteLine($"PaletteType: {paletteType}");
                Debug.WriteLine($"Colors count: {colors.Count}");

                // Create the palette collection
                var collection = new PaletteCollection
                {
                    Title = title ?? $"Palette - {DateTime.Now:MMM dd, HH:mm}",
                    CreatedAt = DateTime.Now,
                    PaletteType = paletteType,
                    IsFavorite = false // Will be set to true separately if needed
                };

                collection.ColorsList = colors;
                Debug.WriteLine($"Total colors in collection: {colors.Count}");

                // Save to database
                var db = await _databaseService.GetDatabaseAsync();

                // Insert the collection and get the ID
                await db.InsertAsync(collection);
                Debug.WriteLine($"Saved collection with ID: {collection.Id}");

                // Save individual colors with positions
                for (int i = 0; i < colors.Count; i++)
                {
                    var paletteColor = new PaletteColor
                    {
                        PaletteCollectionId = collection.Id,
                        HexColor = colors[i].ToArgbHex(),
                        Position = i,
                        CreatedAt = DateTime.Now
                    };

                    await db.InsertAsync(paletteColor);
                    Debug.WriteLine($"Saved color: '{colors[i].ToArgbHex()}' at position {i}");
                }

                Debug.WriteLine("=== SavePaletteCollectionAsync COMPLETE ===");
                return collection;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SavePaletteCollectionAsync: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<List<PaletteCollection>> GetFavoritePalettesAsync()
        {
            try
            {
                var db = await _databaseService.GetDatabaseAsync();

                var collections = await db.QueryAsync<PaletteCollection>(@"
                    SELECT 
                        Id,
                        Title,
                        CreatedAt,
                        PaletteType,
                        COALESCE(IsFavorite, 0) as IsFavorite
                    FROM PaletteCollection 
                    WHERE COALESCE(IsFavorite, 0) = 1 
                    ORDER BY CreatedAt DESC");

                // Load colors for each collection
                foreach (var collection in collections)
                {
                    var colors = await db.QueryAsync<PaletteColor>(
                        "SELECT * FROM PaletteColor WHERE PaletteCollectionId = ? ORDER BY Position",
                        collection.Id);

                    collection.ColorsList = colors.Select(c => Color.FromArgb(c.HexColor)).ToList();
                }

                Debug.WriteLine($"Retrieved {collections.Count} favorite palettes");
                return collections;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting favorite palettes: {ex.Message}");
                return new List<PaletteCollection>();
            }
        }

        public async Task TogglePaletteFavoriteAsync(int collectionId)
        {
            try
            {
                var db = await _databaseService.GetDatabaseAsync();

                // Get current state
                var collection = await db.FindAsync<PaletteCollection>(collectionId);
                if (collection != null)
                {
                    // Toggle favorite status
                    collection.IsFavorite = !collection.IsFavorite;

                    await db.UpdateAsync(collection);

                    Debug.WriteLine($"Toggled favorite for palette {collectionId}: IsFavorite = {collection.IsFavorite}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error toggling palette favorite for collection {collectionId}: {ex.Message}");
                throw;
            }
        }

        public async Task DeletePaletteCollectionAsync(int collectionId)
        {
            try
            {
                await _databaseService.DeletePaletteCollectionAsync(collectionId);
                Debug.WriteLine($"Deleted palette collection {collectionId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting palette collection {collectionId}: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Individual Swatch Favorites

        public async Task<FavoriteSwatch> SaveSwatchToFavoritesAsync(
            Color color,
            string? colorName = null,
            string? collection = null)
        {
            try
            {
                var swatch = new FavoriteSwatch
                {
                    HexColor = color.ToArgbHex(),
                    ColorName = colorName,
                    Collection = collection ?? "Default",
                    CreatedAt = DateTime.Now,
                    IsFavorite = true
                };

                var db = await _databaseService.GetDatabaseAsync();
                await db.InsertAsync(swatch);

                Debug.WriteLine($"Saved swatch to favorites: {swatch.HexColor} in collection '{swatch.Collection}'");
                return swatch;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving swatch to favorites: {ex.Message}");
                throw;
            }
        }

        public async Task<List<FavoriteSwatch>> GetFavoriteSwatchesAsync(string? collection = null)
        {
            try
            {
                var db = await _databaseService.GetDatabaseAsync();

                List<FavoriteSwatch> swatches;
                if (string.IsNullOrEmpty(collection))
                {
                    // Get all favorite swatches
                    swatches = await db.QueryAsync<FavoriteSwatch>(
                        "SELECT * FROM FavoriteSwatch WHERE IsFavorite = 1 ORDER BY CreatedAt DESC");
                }
                else
                {
                    // Get swatches from specific collection
                    swatches = await db.QueryAsync<FavoriteSwatch>(
                        "SELECT * FROM FavoriteSwatch WHERE IsFavorite = 1 AND Collection = ? ORDER BY CreatedAt DESC",
                        collection);
                }

                Debug.WriteLine($"Retrieved {swatches.Count} favorite swatches" +
                    (collection != null ? $" from collection '{collection}'" : ""));
                return swatches;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting favorite swatches: {ex.Message}");
                return new List<FavoriteSwatch>();
            }
        }

        public async Task<List<string>> GetSwatchCollectionNamesAsync()
        {
            try
            {
                var db = await _databaseService.GetDatabaseAsync();

                var collections = await db.QueryAsync<FavoriteSwatch>(
                        "SELECT DISTINCT Collection FROM FavoriteSwatch WHERE IsFavorite = 1 ORDER BY Collection");
                var collectionNames = collections.Select(c => c.Collection ?? string.Empty).ToList();

                Debug.WriteLine($"Found {collections.Count} swatch collections");
                return collectionNames;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting swatch collection names: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task RemoveSwatchFromFavoritesAsync(int swatchId)
        {
            try
            {
                var db = await _databaseService.GetDatabaseAsync();
                await db.DeleteAsync<FavoriteSwatch>(swatchId);

                Debug.WriteLine($"Removed swatch {swatchId} from favorites");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error removing swatch from favorites: {ex.Message}");
                throw;
            }
        }

        public async Task<List<FavoriteSwatch>> GetSwatchesSortedAsync(SwatchSortMethod sortMethod, string? collection = null)
        {
            try
            {
                var swatches = await GetFavoriteSwatchesAsync(collection);

                return sortMethod switch
                {
                    SwatchSortMethod.DateNewest => swatches.OrderByDescending(s => s.CreatedAt).ToList(),
                    SwatchSortMethod.DateOldest => swatches.OrderBy(s => s.CreatedAt).ToList(),
                    SwatchSortMethod.ColorName => swatches.OrderBy(s => s.DisplayName).ToList(),
                    SwatchSortMethod.HexValue => swatches.OrderBy(s => s.HexColor).ToList(),
                    SwatchSortMethod.Collection => swatches.OrderBy(s => s.Collection).ThenByDescending(s => s.CreatedAt).ToList(),
                    _ => swatches
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sorting swatches: {ex.Message}");
                return new List<FavoriteSwatch>();
            }
        }

        public async Task<bool> IsSwatchFavoriteAsync(Color color)
        {
            try
            {
                var db = await _databaseService.GetDatabaseAsync();
                var hexColor = color.ToHex();

                var existingSwatch = await db.FindAsync<FavoriteSwatch>(s => s.HexColor == hexColor);
                return existingSwatch != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking if swatch is favorite: {ex.Message}");
                return false;
            }
        }

        #endregion
    }

    public enum SwatchSortMethod
    {
        DateNewest,
        DateOldest,
        ColorName,
        HexValue,
        Collection
    }
}