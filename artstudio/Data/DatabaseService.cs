using SQLite;
using artstudio.Data.Models;
using System.Diagnostics;

namespace artstudio.Data
{
    public partial class DatabaseService : IDisposable
    {
        private SQLiteAsyncConnection? _database;
        private readonly string _dbPath;
        private const int CurrentDatabaseVersion = 4; // Increment this when schema changes

        public DatabaseService()
        {
            _dbPath = Path.Combine(FileSystem.AppDataDirectory, "artstudio.db3");
            DebugLog($"Database path: {_dbPath}");
        }

        public async Task<SQLiteAsyncConnection> GetDatabaseAsync()
        {
            if (_database != null)
                return _database;

            _database = new SQLiteAsyncConnection(_dbPath);

            // Create tables and handle migrations
            await InitializeDatabaseAsync();

            return _database;
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                DebugLog("Initializing database...");

                // Get current database version
                int currentVersion = await GetDatabaseVersionAsync();
                DebugLog($"Current database version: {currentVersion}");

                if (currentVersion == 0)
                {
                    // Fresh database - create all tables
                    await CreateTablesAsync();
                    await SetDatabaseVersionAsync(CurrentDatabaseVersion);
                    DebugLog("Created fresh database with all tables");
                }
                else if (currentVersion < CurrentDatabaseVersion)
                {
                    // Migration needed
                    await MigrateDatabaseAsync(currentVersion);
                    await SetDatabaseVersionAsync(CurrentDatabaseVersion);
                    DebugLog($"Migrated database from version {currentVersion} to {CurrentDatabaseVersion}");
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Error initializing database: {ex.Message}");
                throw;
            }
        }

        private async Task CreateTablesAsync()
        {
            if (_database == null) return;

            // Word/Prompt related tables
            await _database.CreateTableAsync<WordCollection>();
            await _database.CreateTableAsync<Word>();

            // Palette related tables
            await _database.CreateTableAsync<PaletteCollection>();
            await _database.CreateTableAsync<PaletteColor>();
            await _database.CreateTableAsync<FavoriteSwatch>();

            DebugLog("Created all tables: WordCollection, Word, PaletteCollection, PaletteColor, FavoriteSwatch");
        }

        private async Task MigrateDatabaseAsync(int fromVersion)
        {
            if (_database == null) return;

            DebugLog($"Starting database migration from version {fromVersion}");

            if (fromVersion < 2)
            {
                // Migration to version 2: Add columns to WordCollection
                try
                {
                    await _database.ExecuteAsync("ALTER TABLE WordCollection ADD COLUMN IsFavorite INTEGER DEFAULT 0");
                    DebugLog("Added IsFavorite column to WordCollection table");
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("duplicate column"))
                    {
                        DebugLog($"Error adding IsFavorite column: {ex.Message}");
                        throw;
                    }
                    else
                    {
                        DebugLog("IsFavorite column already exists");
                    }
                }

                try
                {
                    await _database.ExecuteAsync("ALTER TABLE WordCollection ADD COLUMN ExpiresAt TEXT");
                    DebugLog("Added ExpiresAt column to WordCollection table");
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("duplicate column"))
                    {
                        DebugLog($"Error adding ExpiresAt column: {ex.Message}");
                    }
                    else
                    {
                        DebugLog("ExpiresAt column already exists");
                    }
                }

                // Create Word table if it doesn't exist
                try
                {
                    await _database.CreateTableAsync<Word>();
                    DebugLog("Created Word table during migration");
                }
                catch (Exception ex)
                {
                    DebugLog($"Word table might already exist: {ex.Message}");
                }
            }

            if (fromVersion < 3)
            {
                // Migration to version 3: Add palette tables
                try
                {
                    await _database.CreateTableAsync<PaletteCollection>();
                    await _database.CreateTableAsync<PaletteColor>();
                    await _database.CreateTableAsync<FavoriteSwatch>();
                    DebugLog("Added palette tables: PaletteCollection, PaletteColor, FavoriteSwatch");
                }
                catch (Exception ex)
                {
                    DebugLog($"Error creating palette tables: {ex.Message}");
                    // Tables might already exist, continue
                }

                // Ensure Word table exists for version 3 as well
                try
                {
                    await _database.CreateTableAsync<Word>();
                    DebugLog("Ensured Word table exists in version 3 migration");
                }
                catch (Exception ex)
                {
                    DebugLog($"Word table creation in v3 migration: {ex.Message}");
                }
            }

            if (fromVersion < 4)
            {
                // Migration to version 4: Ensure Word table exists
                try
                {
                    await _database.CreateTableAsync<Word>();
                    DebugLog("Created Word table in version 4 migration");
                }
                catch (Exception ex)
                {
                    DebugLog($"Word table creation in v4 migration: {ex.Message}");
                    // Table might already exist, continue
                }
            }

            // Add future migrations here
        }

        #region Word Collections

        public async Task<int> CleanupExpiredItemsAsync()
        {
            try
            {
                if (_database == null) return 0;

                // Get expired collections (not favorited and past expiration date)
                var expiredCollections = await _database.QueryAsync<WordCollection>
                        (@" SELECT Id FROM WordCollection 
                            WHERE ExpiresAt IS NOT NULL 
                            AND ExpiresAt < datetime('now') 
                            AND COALESCE(IsFavorite, 0) = 0");

                if (expiredCollections.Count > 0) // Replace Any() with Count > 0
                {
                    DebugLog($"Manually cleaning up {expiredCollections.Count} expired collections");

                    foreach (var collection in expiredCollections)
                    {
                        // Delete associated words
                        await _database.ExecuteAsync("DELETE FROM Word WHERE WordCollectionId = ?", collection.Id);

                        // Delete the collection
                        await _database.ExecuteAsync("DELETE FROM WordCollection WHERE Id = ?", collection.Id);

                        DebugLog($"Deleted expired collection {collection.Id}");
                    }

                    return expiredCollections.Count;
                }

                return 0;
            }
            catch (Exception ex)
            {
                DebugLog($"Error cleaning up expired items: {ex.Message}");
                return 0;
            }
        }


        public async Task<List<WordCollection>> GetHistoryAsync()
        {
            try
            {
                var db = await GetDatabaseAsync();

                var collections = await db.QueryAsync<WordCollection>(@"
                    SELECT 
                        Id,
                        Title,
                        CreatedAt,
                        PromptType,
                        COALESCE(IsHistory, 1) as IsHistory,
                        COALESCE(IsFavorite, 0) as IsFavorite,
                        ExpiresAt
                    FROM WordCollection 
                    WHERE COALESCE(IsHistory, 1) = 1 
                    ORDER BY CreatedAt DESC");

                DebugLog($"Retrieved {collections.Count} history collections from database");
                return collections;
            }
            catch (Exception ex)
            {
                DebugLog($"Error getting history: {ex.Message}");
                DebugLog($"Stack trace: {ex.StackTrace}");
                return [];
            }
        }

        public async Task<List<WordCollection>> GetFavoritesAsync()
        {
            try
            {
                var db = await GetDatabaseAsync();

                var collections = await db.QueryAsync<WordCollection>(@"
                    SELECT 
                        Id,
                        Title,
                        CreatedAt,
                        PromptType,
                        COALESCE(IsHistory, 0) as IsHistory,
                        COALESCE(IsFavorite, 0) as IsFavorite
                    FROM WordCollection 
                    WHERE COALESCE(IsFavorite, 0) = 1 
                    ORDER BY CreatedAt DESC");

                DebugLog($"Retrieved {collections.Count} favorite word collections");
                return collections;
            }
            catch (Exception ex)
            {
                DebugLog($"Error getting favorites: {ex.Message}");
                return [];
            }
        }

        public async Task ClearHistoryAsync(bool includeExpiredOnly = false)
        {
            try
            {
                var db = await GetDatabaseAsync();

                if (includeExpiredOnly)
                {
                    var expiredCount = await CleanupExpiredItemsAsync();
                    DebugLog($"Cleared {expiredCount} expired history items");
                }
                else
                {
                    var historyIds = await db.QueryAsync<WordCollection>(
                        "SELECT Id FROM WordCollection WHERE COALESCE(IsHistory, 1) = 1 AND COALESCE(IsFavorite, 0) = 0");

                    foreach (var collection in historyIds)
                    {
                        await db.ExecuteAsync("DELETE FROM Word WHERE WordCollectionId = ?", collection.Id);
                    }

                    await db.ExecuteAsync("DELETE FROM WordCollection WHERE COALESCE(IsHistory, 1) = 1 AND COALESCE(IsFavorite, 0) = 0");

                    DebugLog($"Cleared {historyIds.Count} history items (preserved favorites)");
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Error clearing history: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteWordCollectionAsync(int collectionId)
        {
            try
            {
                var db = await GetDatabaseAsync();

                await db.ExecuteAsync("DELETE FROM Word WHERE WordCollectionId = ?", collectionId);
                await db.ExecuteAsync("DELETE FROM WordCollection WHERE Id = ?", collectionId);

                DebugLog($"Deleted word collection {collectionId}");
            }
            catch (Exception ex)
            {
                DebugLog($"Error deleting word collection {collectionId}: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Palette Collections (New Functionality)

        public async Task<List<PaletteCollection>> GetFavoritePalettesAsync()
        {
            try
            {
                var db = await GetDatabaseAsync();

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

                DebugLog($"Retrieved {collections.Count} favorite palette collections");
                return collections;
            }
            catch (Exception ex)
            {
                DebugLog($"Error getting favorite palettes: {ex.Message}");
                return [];
            }
        }

        public async Task DeletePaletteCollectionAsync(int collectionId)
        {
            try
            {
                var db = await GetDatabaseAsync();

                // Delete associated colors first
                await db.ExecuteAsync("DELETE FROM PaletteColor WHERE PaletteCollectionId = ?", collectionId);

                // Delete the collection
                await db.ExecuteAsync("DELETE FROM PaletteCollection WHERE Id = ?", collectionId);

                DebugLog($"Deleted palette collection {collectionId}");
            }
            catch (Exception ex)
            {
                DebugLog($"Error deleting palette collection {collectionId}: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Favorite Swatches (New Functionality)

        public async Task<List<FavoriteSwatch>> GetFavoriteSwatchesAsync(string? collection = null)
        {
            try
            {
                var db = await GetDatabaseAsync();

                List<FavoriteSwatch> swatches;
                if (string.IsNullOrEmpty(collection))
                {
                    swatches = await db.QueryAsync<FavoriteSwatch>(
                        "SELECT * FROM FavoriteSwatch WHERE IsFavorite = 1 ORDER BY CreatedAt DESC");
                }
                else
                {
                    swatches = await db.QueryAsync<FavoriteSwatch>(
                        "SELECT * FROM FavoriteSwatch WHERE IsFavorite = 1 AND Collection = ? ORDER BY CreatedAt DESC",
                        collection);
                }

                DebugLog($"Retrieved {swatches.Count} favorite swatches" +
                    (collection != null ? $" from collection '{collection}'" : ""));
                return swatches;
            }
            catch (Exception ex)
            {
                DebugLog($"Error getting favorite swatches: {ex.Message}");
                return [];
            }
        }

        public async Task<List<string>> GetSwatchCollectionNamesAsync()
        {
            try
            {
                var db = await GetDatabaseAsync();

                var collections = await db.QueryAsync<FavoriteSwatch>(
                        "SELECT DISTINCT Collection FROM FavoriteSwatch WHERE IsFavorite = 1 ORDER BY Collection");
                var collectionNames = collections.Select(c => c.Collection ?? string.Empty).ToList();

                DebugLog($"Found {collections.Count} swatch collections");
                return collectionNames;
            }
            catch (Exception ex)
            {
                DebugLog($"Error getting swatch collection names: {ex.Message}");
                return [];
            }
        }

        public async Task DeleteFavoriteSwatchAsync(int swatchId)
        {
            try
            {
                var db = await GetDatabaseAsync();
                await db.DeleteAsync<FavoriteSwatch>(swatchId);

                DebugLog($"Deleted favorite swatch {swatchId}");
            }
            catch (Exception ex)
            {
                DebugLog($"Error deleting favorite swatch {swatchId}: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Common Database Methods

        private async Task<int> GetDatabaseVersionAsync()
        {
            if (_database == null) return 0;

            try
            {
                var result = await _database.ExecuteScalarAsync<int>("PRAGMA user_version");
                return result;
            }
            catch
            {
                return 0; // Assume version 0 if we can't get it
            }
        }

        private async Task SetDatabaseVersionAsync(int version)
        {
            if (_database == null) return;

            await _database.ExecuteAsync($"PRAGMA user_version = {version}");
            DebugLog($"Set database version to {version}");
        }

        private static void DebugLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logMessage = $"[DB {timestamp}] {message}";
            Debug.WriteLine(logMessage);
            System.Console.WriteLine(logMessage);
        }

        public void Dispose()
        {
            if (_database != null)
            {
                _ = _database.CloseAsync();
            }

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}