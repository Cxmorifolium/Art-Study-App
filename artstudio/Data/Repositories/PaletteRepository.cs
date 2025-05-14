using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using artstudio.Models;
using artstudio.Models.Database;
using artstudio.Data;

namespace artstudio.Data.Repositories
{
    public class PaletteRepository
    {
        private readonly SQLiteAsyncConnection _database;

        public PaletteRepository(SQLiteAsyncConnection database)
        {
            _database = database;
        }

        // Palette methods
        public Task<List<Palette>> GetAllPalettesAsync()
        {
            return _database.Table<Palette>().ToListAsync();
        }

        public Task<List<Palette>> GetFavoritePalettesAsync()
        {
            return _database.Table<Palette>().Where(p => p.IsFavorite).ToListAsync();
        }

        public Task<Palette> GetPaletteByIdAsync(int id)
        {
            return _database.Table<Palette>().Where(p => p.Id == id).FirstOrDefaultAsync();
        }

        public Task<int> SavePaletteAsync(Palette palette)
        {
            if (palette.Id != 0)
            {
                return _database.UpdateAsync(palette);
            }
            else
            {
                palette.CreatedAt = DateTime.Now;
                return _database.InsertAsync(palette);
            }
        }

        public Task<int> DeletePaletteAsync(int id)
        {
            return _database.DeleteAsync<Palette>(id);
        }

        public async Task<int> TogglePaletteFavoriteAsync(int id)
        {
            var palette = await GetPaletteByIdAsync(id);
            if (palette != null)
            {
                palette.IsFavorite = !palette.IsFavorite;
                return await SavePaletteAsync(palette);
            }
            return 0;
        }

        // ColorSwatch methods
        public Task<List<ColorSwatch>> GetAllSwatchesAsync()
        {
            return _database.Table<ColorSwatch>().ToListAsync();
        }

        public Task<List<ColorSwatch>> GetFavoriteSwatchesAsync()
        {
            return _database.Table<ColorSwatch>().Where(s => s.IsFavorite).ToListAsync();
        }

        public Task<ColorSwatch> GetSwatchByIdAsync(int id)
        {
            return _database.Table<ColorSwatch>().Where(s => s.Id == id).FirstOrDefaultAsync();
        }

        public Task<List<ColorSwatch>> GetSwatchesSortedByHueAsync()
        {
            return _database.Table<ColorSwatch>().OrderBy(s => s.Hue).ToListAsync();
        }

        public Task<List<ColorSwatch>> GetSwatchesSortedByLuminosityAsync()
        {
            return _database.Table<ColorSwatch>().OrderBy(s => s.Luminosity).ToListAsync();
        }

        public Task<List<ColorSwatch>> GetSwatchesSortedBySaturationAsync()
        {
            return _database.Table<ColorSwatch>().OrderBy(s => s.Saturation).ToListAsync();
        }

        public Task<int> SaveSwatchAsync(ColorSwatch swatch)
        {
            if (swatch.Id != 0)
            {
                return _database.UpdateAsync(swatch);
            }
            else
            {
                swatch.CreatedAt = DateTime.Now;
                return _database.InsertAsync(swatch);
            }
        }

        public Task<int> DeleteSwatchAsync(int id)
        {
            return _database.DeleteAsync<ColorSwatch>(id);
        }

        public async Task<int> ToggleSwatchFavoriteAsync(int id)
        {
            var swatch = await GetSwatchByIdAsync(id);
            if (swatch != null)
            {
                swatch.IsFavorite = !swatch.IsFavorite;
                return await SaveSwatchAsync(swatch);
            }
            return 0;
        }

        // Collection methods
        public Task<List<SwatchCollection>> GetAllCollectionsAsync()
        {
            return _database.Table<SwatchCollection>().ToListAsync();
        }

        public Task<SwatchCollection> GetCollectionByIdAsync(int id)
        {
            return _database.Table<SwatchCollection>().Where(c => c.Id == id).FirstOrDefaultAsync();
        }

        public Task<int> SaveCollectionAsync(SwatchCollection collection)
        {
            if (collection.Id != 0)
            {
                return _database.UpdateAsync(collection);
            }
            else
            {
                collection.CreatedAt = DateTime.Now;
                return _database.InsertAsync(collection);
            }
        }

        public Task<int> DeleteCollectionAsync(int id)
        {
            return _database.DeleteAsync<SwatchCollection>(id);
        }

        // Collection mapping methods
        public Task<int> AddSwatchToCollectionAsync(int swatchId, int collectionId)
        {
            var mapping = new SwatchCollectionMap
            {
                SwatchId = swatchId,
                CollectionId = collectionId
            };
            return _database.InsertAsync(mapping);
        }

        public Task<int> RemoveSwatchFromCollectionAsync(int swatchId, int collectionId)
        {
            return _database.Table<SwatchCollectionMap>()
                .Where(m => m.SwatchId == swatchId && m.CollectionId == collectionId)
                .DeleteAsync();
        }

        public async Task<List<ColorSwatch>> GetSwatchesByCollectionAsync(int collectionId)
        {
            var mappings = await _database.Table<SwatchCollectionMap>()
                .Where(m => m.CollectionId == collectionId)
                .ToListAsync();

            var swatchIds = mappings.Select(m => m.SwatchId).ToList();

            if (swatchIds.Count == 0)
                return new List<ColorSwatch>();

            var result = new List<ColorSwatch>();
            foreach (var id in swatchIds)
            {
                var swatch = await GetSwatchByIdAsync(id);
                if (swatch != null)
                    result.Add(swatch);
            }

            return result;
        }
    }
}
