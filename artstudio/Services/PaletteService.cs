using artstudio.Data;
using Microsoft.Extensions.Logging;

namespace artstudio.Services
{
    public class PaletteService
    {
        private readonly DatabaseService _databaseService;
        private readonly ILogger<PaletteService> _logger;

        public PaletteService(DatabaseService databaseService, ILogger<PaletteService> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        #region Palette Collections

        public async Task<PaletteCollection> SavePaletteCollectionAsync(
            List<Color> colors,
            string? title = null,
            string? paletteType = "generated")
        {
            try
            {
                _logger.LogDebug("=== SavePaletteCollectionAsync START ===");
                _logger.LogDebug("Title: {title}", title);
                _logger.LogDebug("PaletteType: {PaletteType}", paletteType);
                _logger.LogDebug("Colors count: {ColorsCount}", colors.Count);

                // Create the palette collection
                var collection = new PaletteCollection
                {
                    Title = title ?? $"Palette - {DateTime.Now:MMM dd, HH:mm}",
                    CreatedAt = DateTime.Now,
                    PaletteType = paletteType,
                    IsFavorite = false // Will be set to true separately if needed
                };

                collection.ColorsList = colors;
                _logger.LogDebug("Total colors in collection: {ColorCount}", colors.Count);

                // Save to database
                var db = await _databaseService.GetDatabaseAsync();

                // Insert the collection and get the ID
                await db.InsertAsync(collection);
                _logger.LogDebug("Saved collection with ID: {CollectionID}", collection.Id);

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
                    _logger.LogDebug("Saved color: '{ColorValue}' at position {Position}", colors[i].ToArgbHex(), i);
                }

                _logger.LogDebug("=== SavePaletteCollectionAsync COMPLETE ===");
                return collection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SavePaletteCollectionAsync");
                _logger.LogError(ex, "Stack trace");
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

                _logger.LogDebug("Retrieved {PaletteCount} favorite palettes", collections.Count);
                return collections;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting favorite palettes");
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

                    _logger.LogDebug("Toggled favorite for palette {CollectionId}: IsFavorite = {FavoriteCollection}", collectionId, collection.IsFavorite);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling palette favorite for collection {CollectionID}", collectionId);
                throw;
            }
        }

        public async Task DeletePaletteCollectionAsync(int collectionId)
        {
            try
            {
                await _databaseService.DeletePaletteCollectionAsync(collectionId);
                _logger.LogDebug("Deleted palette collection {PaletteCollectionID}", collectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting palette collection {PaletteCollectionID}", collectionId);
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
                        _logger.LogDebug("Swatch {HexColor} already exists - returning existing", hexColor);
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

                _logger.LogDebug("Saved swatch to favorites: {swatch.HexColor}", swatch.HexColor);
                return swatch;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving swatch to favorites");
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

                _logger.LogDebug("Retrieved {CountSwatches} favorite swatches", swatches.Count);
                return swatches;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting favorite swatches");
                return new List<FavoriteSwatch>();
            }
        }

        public async Task RemoveSwatchFromFavoritesAsync(int swatchId)
        {
            try
            {
                var db = await _databaseService.GetDatabaseAsync();
                await db.DeleteAsync<FavoriteSwatch>(swatchId);

                _logger.LogDebug("Removed swatch {SwatchId} from favorites", swatchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing swatch from favorites");
                throw;
            }
        }

        // 🎯 NEW: Direct removal by hex color (more efficient for UI operations)
        public async Task RemoveSwatchFromFavoritesAsync(string hexColor)
        {
            try
            {
                var db = await _databaseService.GetDatabaseAsync();

                var swatchToDelete = await db.Table<FavoriteSwatch>()
                    .Where(s => s.HexColor.ToLower() == hexColor.ToLower())
                    .FirstOrDefaultAsync();

                if (swatchToDelete != null)
                {
                    await db.DeleteAsync<FavoriteSwatch>(swatchToDelete.Id);
                    _logger.LogDebug("Removed swatch {HexColor} from favorites", hexColor);
                }
                else
                {
                    _logger.LogDebug("Swatch {HexColor} not found in favorites", hexColor);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing swatch from favorites by hex");
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
                    SwatchSortMethod.HueGradient => SortSwatchesByHue(swatches).ToList(), // Rainbow!
                    SwatchSortMethod.Brightness => SortSwatchesByBrightness(swatches).ToList(),
                    _ => swatches
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sorting swatches");
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
                _logger.LogError(ex, "Error checking if swatch is favorited:");
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
                    _logger.LogDebug("Swatch {HexColor} is already in favorites - skipping duplicate", hexColor);
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
                _logger.LogDebug("Added new swatch {HexColor} to favorites", hexColor);

                return true; // Successfully added new swatch
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding swatch to favorites:");
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