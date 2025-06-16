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

        public async Task<FavoriteSwatch> SaveSwatchToFavoritesAsync(Color color, string? colorName = null)
        {
            try
            {
                var hexColor = color.ToArgbHex();

                // Check for duplicates first
                if (await IsSwatchAlreadyFavoritedAsync(hexColor))
                {
                    // Return the existing swatch instead of creating a duplicate
                    var db = await _databaseService.GetDatabaseAsync();
                    var existingSwatch = await db.Table<FavoriteSwatch>()
                        .Where(s => s.HexColor.ToLower() == hexColor.ToLower())
                        .FirstOrDefaultAsync();

                    if (existingSwatch != null)
                    {
                        Debug.WriteLine($"Swatch {hexColor} already exists - returning existing");
                        return existingSwatch;
                    }
                }

                var swatch = new FavoriteSwatch
                {
                    HexColor = hexColor.ToUpper(),
                    ColorName = colorName,
                    Collection = null, // No collections!
                    CreatedAt = DateTime.Now,
                    IsFavorite = true
                };

                var database = await _databaseService.GetDatabaseAsync();
                await database.InsertAsync(swatch);

                Debug.WriteLine($"Saved swatch to favorites: {swatch.HexColor}");
                return swatch;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving swatch to favorites: {ex.Message}");
                throw;
            }
        }

        public async Task<List<FavoriteSwatch>> GetFavoriteSwatchesAsync()
        {
            try
            {
                var db = await _databaseService.GetDatabaseAsync();

                // Get all favorite swatches (no collection filtering)
                var swatches = await db.QueryAsync<FavoriteSwatch>(
                    "SELECT * FROM FavoriteSwatch WHERE IsFavorite = 1 ORDER BY CreatedAt DESC");

                Debug.WriteLine($"Retrieved {swatches.Count} favorite swatches");
                return swatches;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting favorite swatches: {ex.Message}");
                return new List<FavoriteSwatch>();
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

        public async Task<List<FavoriteSwatch>> GetSwatchesSortedAsync(SwatchSortMethod sortMethod)
        {
            try
            {
                var swatches = await GetFavoriteSwatchesAsync(); // No collection parameter

                return sortMethod switch
                {
                    SwatchSortMethod.DateNewest => swatches.OrderByDescending(s => s.CreatedAt).ToList(),
                    SwatchSortMethod.DateOldest => swatches.OrderBy(s => s.CreatedAt).ToList(),
                    SwatchSortMethod.ColorName => swatches.OrderBy(s => s.DisplayName).ToList(),
                    SwatchSortMethod.HexValue => swatches.OrderBy(s => s.HexColor).ToList(),
                    SwatchSortMethod.HueGradient => SortSwatchesByHue(swatches).ToList(), // 🌈 Rainbow!
                    SwatchSortMethod.Brightness => SortSwatchesByBrightness(swatches).ToList(),
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
            return await IsSwatchAlreadyFavoritedAsync(color.ToHex());
        }
        public async Task<bool> IsSwatchAlreadyFavoritedAsync(string hexColor)
        {
            try
            {
                var db = await _databaseService.GetDatabaseAsync();
                var existingSwatch = await db.Table<FavoriteSwatch>()
                    .Where(s => s.HexColor.ToLower() == hexColor.ToLower() && s.IsFavorite == true)
                    .FirstOrDefaultAsync();

                return existingSwatch != null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking if swatch is favorited: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AddSwatchToFavoritesAsync(string hexColor)
        {
            try
            {
                // Check if this color is already favorited
                if (await IsSwatchAlreadyFavoritedAsync(hexColor))
                {
                    Debug.WriteLine($"Swatch {hexColor} is already in favorites - skipping duplicate");
                    return false; // Return false to indicate it wasn't added (already exists)
                }

                var favoriteSwatch = new FavoriteSwatch
                {
                    HexColor = hexColor.ToUpper(), // Normalize to uppercase
                    Collection = null, // No collections needed!
                    CreatedAt = DateTime.Now,
                    IsFavorite = true
                };

                var db = await _databaseService.GetDatabaseAsync();
                await db.InsertAsync(favoriteSwatch);
                Debug.WriteLine($"Added new swatch {hexColor} to favorites");
                return true; // Successfully added new swatch
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding swatch to favorites: {ex.Message}");
                return false;
            }
        }

        private IEnumerable<FavoriteSwatch> SortSwatchesByHue(IEnumerable<FavoriteSwatch> swatches)
        {
            return swatches.OrderBy(s =>
            {
                try
                {
                    var color = Color.FromArgb(s.HexColor);
                    return GetHueValue(color);
                }
                catch
                {
                    return 999f; // Put invalid colors at the end
                }
            });
        }

        private IEnumerable<FavoriteSwatch> SortSwatchesByBrightness(IEnumerable<FavoriteSwatch> swatches)
        {
            return swatches.OrderByDescending(s =>
            {
                try
                {
                    var color = Color.FromArgb(s.HexColor);
                    // Calculate perceived brightness (luminance)
                    return (0.299 * color.Red + 0.587 * color.Green + 0.114 * color.Blue);
                }
                catch
                {
                    return 0f;
                }
            });
        }

        private float GetHueValue(Color color)
        {
            var r = color.Red;
            var g = color.Green;
            var b = color.Blue;

            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            var delta = max - min;

            if (delta == 0) return 0f; // Grayscale colors get hue 0

            float hue = 0f;

            if (max == r)
            {
                hue = ((g - b) / delta) % 6;
            }
            else if (max == g)
            {
                hue = (b - r) / delta + 2;
            }
            else if (max == b)
            {
                hue = (r - g) / delta + 4;
            }

            hue *= 60;
            if (hue < 0) hue += 360;

            return hue;
        }


        #endregion
    }

    public enum SwatchSortMethod
    {
        DateNewest,
        DateOldest,
        ColorName,
        HexValue,
        HueGradient,
        Brightness 
    }

}