using SQLite;
using System.Diagnostics;
using artstudio.Data;
using artstudio.Models;
using Microsoft.Extensions.Logging;

namespace artstudio.Services
{
    public partial class DatabaseService : IDisposable
    {
        private SQLiteAsyncConnection? _database;
        private readonly string _dbPath;
        private readonly ILogger<DatabaseService> _logger;
        private const int CurrentDatabaseVersion = 10; // Increment this when schema changes: (added new column for session user enter name)

        public DatabaseService(ILogger<DatabaseService> logger)
        {
            _logger = logger;
            _dbPath = Path.Combine(FileSystem.AppDataDirectory, "artstudio.db3");
            _logger.LogDebug("Database path: {DbPath}", _dbPath);
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
                _logger.LogInformation("Initializing database...");

                // Get current database version
                int currentVersion = await GetDatabaseVersionAsync();
                _logger.LogInformation("Current database version: {Version}", currentVersion);

                if (currentVersion == 0)
                {
                    // Fresh database - create all tables
                    await CreateTablesAsync();
                    await SetDatabaseVersionAsync(CurrentDatabaseVersion);
                    _logger.LogInformation("Created fresh database with all tables");
                }
                else if (currentVersion < CurrentDatabaseVersion)
                {
                    // Migration needed
                    await MigrateDatabaseAsync(currentVersion);
                    await SetDatabaseVersionAsync(CurrentDatabaseVersion);
                    _logger.LogInformation("Migrated database from version {FromVersion} to {ToVersion}",
                        currentVersion, CurrentDatabaseVersion);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing database");
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

            // Gallery
            await _database.CreateTableAsync<UserUploadedImage>();

            // Session snapshots
            await _database.CreateTableAsync<SessionSnapshot>();

            // Image favoriting
            await _database.CreateTableAsync<FavoriteImageItem>();

            _logger.LogDebug("Created all tables: WordCollection, Word, PaletteCollection, PaletteColor, FavoriteSwatch, UserUploadedImage, SessionSnapshot, FavoriteImageItem");
        }

        private async Task MigrateDatabaseAsync(int fromVersion)
        {
            if (_database == null) return;

            _logger.LogInformation("Starting database migration from version {FromVersion}", fromVersion);

            if (fromVersion < 2)
            {
                // Migration to version 2: Add columns to WordCollection
                try
                {
                    await _database.ExecuteAsync("ALTER TABLE WordCollection ADD COLUMN IsFavorite INTEGER DEFAULT 0");
                    _logger.LogDebug("Added IsFavorite column to WordCollection table");
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("duplicate column"))
                    {
                        _logger.LogError(ex, "Error adding IsFavorite column");
                        throw;
                    }
                    else
                    {
                        _logger.LogDebug("IsFavorite column already exists");
                    }
                }

                try
                {
                    await _database.ExecuteAsync("ALTER TABLE WordCollection ADD COLUMN ExpiresAt TEXT");
                    _logger.LogDebug("Added ExpiresAt column to WordCollection table");
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("duplicate column"))
                    {
                        _logger.LogWarning(ex, "Error adding ExpiresAt column");
                    }
                    else
                    {
                        _logger.LogDebug("ExpiresAt column already exists");
                    }
                }

                // Create Word table if it doesn't exist
                try
                {
                    await _database.CreateTableAsync<Word>();
                    _logger.LogDebug("Created Word table during migration");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error recreating FavoriteImageItem table");
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
                    _logger.LogDebug("Added palette tables: PaletteCollection, PaletteColor, FavoriteSwatch");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error recreating FavoriteImageItem table");
                    // Tables might already exist, continue
                }

                // Ensure Word table exists for version 3 as well
                try
                {
                    await _database.CreateTableAsync<Word>();
                    _logger.LogDebug("Ensured Word table exists in version 3 migration");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Word table creation in v3 migration");
                }
            }

            if (fromVersion < 4)
            {
                // Migration to version 4: Ensure Word table exists
                try
                {
                    await _database.CreateTableAsync<Word>();
                    _logger.LogDebug("Created Word table in version 4 migration");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Word table creation in v4 migration");
                    // Table might already exist, continue
                }
            }

            if (fromVersion < 5)
            {
                // Migration to version 5: Add gallery tables
                try
                {
                    await _database.CreateTableAsync<UserUploadedImage>();
                    _logger.LogDebug("Added gallery tables: UserUploadedImage, ImageReference");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error creating gallery tables}");
                    // Tables might already exist, continue
                }
            }

            if (fromVersion < 6)
            {
                // Migration to version 6: Clean recreation of UserUploadedImage table
                try
                {

                    // Drop existing table (this will lose any test data)
                    await _database.ExecuteAsync("DROP TABLE IF EXISTS UserUploadedImage");

                    // Create table with complete schema
                    await _database.CreateTableAsync<UserUploadedImage>();

                    _logger.LogDebug("Successfully created UserUploadedImage table with all required columns");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Word table creation in v3 migration");
                    throw;
                }
            }

            if (fromVersion < 7)
            {
                // Migration to version 7: Add SessionSnapshot table
                try
                {
                    await _database.CreateTableAsync<SessionSnapshot>();
                    _logger.LogDebug("Added SessionSnapshot table for session saving functionality");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error creating SessionSnapshot table");
                    // Table might already exist, continue
                }
            }

            if (fromVersion < 8)
            {
                // Migration to version 8: Add FavoriteImageItem table
                try
                {
                    await _database.CreateTableAsync<FavoriteImageItem>();
                    _logger.LogDebug("Added FavoriteImageItem table for image favoriting functionality");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error creating FavoriteImageItem table");
                    // Table might already exist, continue
                }
            }
            if (fromVersion < 9) 
            {
                // Migration to version 9: Fix FavoriteImageItem table with proper primary key
                try
                {
                    await _database.ExecuteAsync("DROP TABLE IF EXISTS FavoriteImageItem");

                    // Create table with proper schema
                    await _database.CreateTableAsync<FavoriteImageItem>();

                    _logger.LogDebug("Recreated FavoriteImageItem table with proper primary key");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error recreating FavoriteImageItem table");
                    // Continue - table might not have existed
                }
            }

            if (fromVersion < 10) 
            {
                // Migration to version 10: Add Title column to SessionSnapshot table
                try
                {
                    var tableInfo = await _database.GetTableInfoAsync("SessionSnapshot");
                    var titleColumnExists = tableInfo.Any(column => column.Name.Equals("Title", StringComparison.OrdinalIgnoreCase));

                    if (!titleColumnExists)
                    {
                        await _database.ExecuteAsync("ALTER TABLE SessionSnapshot ADD COLUMN Title TEXT");
                        _logger.LogInformation("Added Title column to SessionSnapshot table for custom session naming");
                    }
                    else
                    {
                        _logger.LogDebug("Title column already exists in SessionSnapshot table");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Error adding Title column to SessionSnapshot table: {ex.Message}");
                    // Continue - column might already exist or table structure is different
                }
            }
            // Add future migrations here
        }

        //public async Task ResetDatabaseForTestingAsync()
        //{
        //    try
        //    {
        //        if (_database != null)
        //        {
        //            await _database.CloseAsync();
        //            _database = null;
        //        }

        //        if (File.Exists(_dbPath))
        //        {
        //            File.Delete(_dbPath);
        //            DebugLog("Deleted existing database for fresh start");
        //        }

        //        // Next call to GetDatabaseAsync will create fresh database
        //    }
        //    catch (Exception ex)
        //    {
        //        DebugLog($"Error resetting database: {ex.Message}");
        //    }
        //}

        #region Word Collections

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
                COALESCE(IsFavorite, 0) as IsFavorite
                FROM WordCollection 
                WHERE COALESCE(IsFavorite, 0) = 1 
                ORDER BY CreatedAt DESC");

                _logger.LogDebug("Retrieved {Count} favorite word collections", collections.Count);
                return collections;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting favorites");
                return [];
            }
        }

        public async Task DeleteWordCollectionAsync(int collectionId)
        {
            try
            {
                var db = await GetDatabaseAsync();

                await db.ExecuteAsync("DELETE FROM Word WHERE WordCollectionId = ?", collectionId);
                await db.ExecuteAsync("DELETE FROM WordCollection WHERE Id = ?", collectionId);

                _logger.LogInformation("Deleted word collection {CollectionId}", collectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting word collection {CollectionId}", collectionId);
                throw;
            }
        }

        #endregion

        #region Palette Collections

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

                _logger.LogDebug("Retrieved {Count} favorite word collections", collections.Count);
                return collections;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting favorites");
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

                _logger.LogInformation($"Deleted palette collection {collectionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting palette {CollectionId}", collectionId);
                throw;
            }
        }

        #endregion

        #region Favorite Swatches

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

                _logger.LogDebug("Retrieved {Count} favorite swatches{FromCollection}",
                        swatches.Count,
                        collection != null ? $" from collection '{collection}'" : string.Empty);
                return swatches;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting favorite swatches");
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

                _logger.LogDebug("Found {Count} swatch collections", collections.Count);
                return collectionNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting swatch collection names");
                return [];
            }
        }

        public async Task DeleteFavoriteSwatchAsync(int swatchId)
        {
            try
            {
                var db = await GetDatabaseAsync();
                await db.DeleteAsync<FavoriteSwatch>(swatchId);

                _logger.LogInformation("Deleted favorite swatch {SwatchId}", swatchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting favorite swatch {SwatchId}", swatchId);
                throw;
            }
        }

        #endregion

        #region Image References
        public async Task<int> SaveFavoriteImageAsync(FavoriteImageItem favoriteImage)
        {
            if (_database == null)
                throw new InvalidOperationException("Database not initialized");

            return await _database.InsertAsync(favoriteImage);
        }

        public async Task<List<FavoriteImageItem>> GetFavoriteImagesAsync()
        {
            if (_database == null)
                throw new InvalidOperationException("Database not initialized");

            return await _database.Table<FavoriteImageItem>()
                                  .Where(f => f.IsFavorite)
                                  .OrderByDescending(f => f.CreatedAt)
                                  .ToListAsync();

        }

        public async Task<int> DeleteFavoriteImageAsync(int favoriteImageId)
        {
            try
            {
                if (_database == null)
                    throw new InvalidOperationException("Database not initialized");

                _logger.LogDebug("Attempting to delete favorite image with ID: {FavoriteImageId}", favoriteImageId);

                // First verify the record exists
                var existingFavorite = await _database.FindAsync<FavoriteImageItem>(favoriteImageId);
                if (existingFavorite == null)
                {
                    _logger.LogDebug("Favorite image with ID {FavoriteImageId} not found", favoriteImageId);
                    return 0;
                }

                // Delete the record
                var result = await _database.DeleteAsync<FavoriteImageItem>(favoriteImageId);
                _logger.LogInformation("Successfully deleted favorite image {FavoriteImageId}, rows affected: {Result}", favoriteImageId, result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting favorite image {FavoriteImageId}", favoriteImageId);
                throw;
            }
        }
        public async Task<int> DeleteFavoriteImageByUnsplashIdAsync(string unsplashId)
        {
            try
            {
                if (_database == null)
                    throw new InvalidOperationException("Database not initialized");

                _logger.LogDebug("Attempting to delete favorite image with UnsplashId: {UnsplashId}", unsplashId);

                // Find and delete by UnsplashId
                var result = await _database.ExecuteAsync(
                    "DELETE FROM FavoriteImageItem WHERE UnsplashId = ?",
                    unsplashId);

                _logger.LogInformation("Successfully deleted favorite image {UnsplashId}, rows affected: {Result}", unsplashId, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting favorite image by UnsplashId {UnsplashId}", unsplashId);
                throw;
            }
        }
        public async Task<FavoriteImageItem?> GetFavoriteByUnsplashIdAsync(string unsplashId)
        {
            try
            {
                var db = await GetDatabaseAsync();
                return await db.Table<FavoriteImageItem>()
                    .Where(f => f.UnsplashId == unsplashId)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Error getting favorite by UnsplashId {UnsplashId}", unsplashId);
                return null;
            }
        }
        #endregion

        #region Gallery Methods

        public async Task<List<UserUploadedImage>> GetUserUploadedImagesAsync()
        {
            try
            {
                var db = await GetDatabaseAsync();
                var images = await db.QueryAsync<UserUploadedImage>(@"
            SELECT * FROM UserUploadedImage 
            ORDER BY CreatedAt DESC");

                _logger.LogDebug("Retrieved {ImagesCount} user uploaded images", images.Count);
                return images;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user images");
                return [];
            }
        }

        public async Task<UserUploadedImage?> GetUserUploadedImageAsync(int imageId)
        {
            try
            {
                var db = await GetDatabaseAsync();
                var image = await db.FindAsync<UserUploadedImage>(imageId);

                if (image != null)
                {
                    _logger.LogDebug("Retrieved user uploaded image: {ImageId}", image.Id);
                }
                else
                {
                    _logger.LogWarning("User uploaded image {ImageId} not found", imageId);
                }

                return image;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user image {ImageId}", imageId);
                return null;
            }
        }

        public async Task<int> SaveUserUploadedImageAsync(UserUploadedImage image)
        {
            try
            {
                var db = await GetDatabaseAsync();

                if (image.Id == 0)
                {
                    // New image
                    image.CreatedAt = DateTime.Now;
                    await db.InsertAsync(image);
                    _logger.LogInformation("Saved new user uploaded image: {ImageDisplayTitle} (ID: {ImageId})", image.DisplayTitle, image.Id);
                }
                else
                {
                    // Update existing image
                    await db.UpdateAsync(image);
                    _logger.LogInformation("Updated user uploaded image: {ImageId}", image.Id);
                }

                return image.Id;
            }
            catch (Exception ex)
            {
               _logger.LogError(ex, "Error saving user image");
                throw;
            }
        }

        public async Task UpdateUserUploadedImageAsync(UserUploadedImage image)
        {
            try
            {
                var db = await GetDatabaseAsync();
                await db.UpdateAsync(image);

                _logger.LogInformation("Updated user uploaded image: {ImageId}", image.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching gallery");
                throw;
            }
        }

        public async Task DeleteUserUploadedImageAsync(int imageId)
        {
            try
            {
                var db = await GetDatabaseAsync();

                // Get the image first to clean up the file
                var image = await db.FindAsync<UserUploadedImage>(imageId);
                if (image != null)
                {
                    // Delete the physical image file if it exists
                    await DeleteImageFileAsync(image.ArtworkImagePath);
                }

                // Delete from database
                await db.DeleteAsync<UserUploadedImage>(imageId);

                _logger.LogInformation("Deleted user uploaded image {ImageId}", imageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user image {ImageId}", imageId);
                throw;
            }
        }

        public async Task<List<UserUploadedImage>> SearchGalleryAsync(string searchTerm)
        {
            try
            {
                var db = await GetDatabaseAsync();

                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return await GetUserUploadedImagesAsync();
                }

                var searchLower = searchTerm.Trim().ToLower();

                // Search in title, notes, and JSON fields
                var images = await db.QueryAsync<UserUploadedImage>(@"
            SELECT * FROM UserUploadedImage 
            WHERE LOWER(COALESCE(Title, '')) LIKE ? 
               OR LOWER(COALESCE(Notes, '')) LIKE ?
               OR LOWER(COALESCE(CustomTags, '')) LIKE ?
               OR LOWER(COALESCE(GeneratedWords, '')) LIKE ?
            ORDER BY CreatedAt DESC",
                    $"%{searchLower}%", $"%{searchLower}%", $"%{searchLower}%", $"%{searchLower}%");

                _logger.LogDebug("Found {ImagesCount} images with tag '{Tag}'", searchTerm, images.Count);
                return images;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching gallery");
                return [];
            }
        }

        public async Task<List<UserUploadedImage>> GetGalleryByTagAsync(string tag)
        {
            try
            {
                var db = await GetDatabaseAsync();
                var tagLower = tag.Trim().ToLower();

                var images = await db.QueryAsync<UserUploadedImage>(@"
            SELECT * FROM UserUploadedImage 
            WHERE LOWER(COALESCE(CustomTags, '')) LIKE ?
            ORDER BY CreatedAt DESC",
                    $"%{tagLower}%");

                _logger.LogDebug("Found {ImagesCount} images with tag '{Tag}'", images.Count, tag);
                return images;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting images by tag '{Tag}'", tag);
                return [];
            }
        }

        public async Task<List<string>> GetAllCustomTagsAsync()
        {
            try
            {
                var db = await GetDatabaseAsync();
                var images = await db.QueryAsync<UserUploadedImage>(@"
            SELECT CustomTags FROM UserUploadedImage 
            WHERE CustomTags IS NOT NULL AND CustomTags != ''");

                var allTags = new HashSet<string>();

                foreach (var image in images)
                {
                    if (!string.IsNullOrEmpty(image.CustomTags))
                    {
                        try
                        {
                            var tags = System.Text.Json.JsonSerializer.Deserialize<List<string>>(image.CustomTags);
                            if (tags != null)
                            {
                                foreach (var tag in tags)
                                {
                                    if (!string.IsNullOrWhiteSpace(tag))
                                    {
                                        allTags.Add(tag.Trim());
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error searching gallery");
                        }
                    }
                }

                var result = allTags.OrderBy(tag => tag).ToList();
                _logger.LogDebug("Found {ResultCount} unique custom tags", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all custom tags");
                return [];
            }
        }

        private async Task DeleteImageFileAsync(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                    return;

                await Task.Run(() => File.Delete(imagePath));
                _logger.LogInformation("Deleted image file: {ImagePath}", imagePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image file {ImagePath}", imagePath);
                // Don't throw - file deletion failure shouldn't prevent database deletion
            }
        }

        #endregion

        #region Session Snapshots
        // Session Snapshot Methods
        public async Task<int> SaveSessionSnapshotAsync(SessionSnapshot sessionSnapshot)
        {
            if (_database == null)
                throw new InvalidOperationException("Database not initialized");

            return await _database.InsertAsync(sessionSnapshot);
        }

        public async Task<SessionSnapshot?> GetSessionSnapshotAsync(int id)
        {
            if (_database == null)
                throw new InvalidOperationException("Database not initialized");

            return await _database.Table<SessionSnapshot>()
                                  .Where(s => s.Id == id)
                                  .FirstOrDefaultAsync();
        }

        public async Task<List<SessionSnapshot>> GetAllSessionSnapshotsAsync()
        {
            if (_database == null)
                throw new InvalidOperationException("Database not initialized");

            return await _database.Table<SessionSnapshot>()
                                  .OrderByDescending(s => s.CreatedAt)
                                  .ToListAsync();
        }

        public async Task<List<SessionSnapshot>> GetRecentSessionSnapshotsAsync(int count = 10)
        {
            if (_database == null)
                throw new InvalidOperationException("Database not initialized");

            return await _database.Table<SessionSnapshot>()
                                  .OrderByDescending(s => s.CreatedAt)
                                  .Take(count)
                                  .ToListAsync();
        }

        public async Task<int> UpdateSessionSnapshotAsync(SessionSnapshot sessionSnapshot)
        {
            if (_database == null)
                throw new InvalidOperationException("Database not initialized");

            return await _database.UpdateAsync(sessionSnapshot);
        }

        public async Task<int> DeleteSessionSnapshotAsync(int id)
        {
            if (_database == null)
                throw new InvalidOperationException("Database not initialized");

            return await _database.DeleteAsync<SessionSnapshot>(id);
        }

        public async Task CleanupOldSessionImagesAsync()
        {
            try
            {
                var sessionImagesDir = Path.Combine(FileSystem.AppDataDirectory, "SessionImages");

                if (!Directory.Exists(sessionImagesDir))
                    return;

                var files = Directory.GetFiles(sessionImagesDir);
                var cutoffDate = DateTime.Now.AddDays(-30); // Keep images for 30 days

                int deletedCount = 0;
                await Task.Run(() =>
                {
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTime < cutoffDate)
                        {
                            try
                            {
                                File.Delete(file);
                                deletedCount++;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to delete old image file {File}", file);
                            }
                        }
                    }
                });

                if (deletedCount > 0)
                {
                    _logger.LogDebug("Cleaned up {DeletedCount} old session image files", deletedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session images cleanup");
            }
        }
        public async Task<int> DeleteOldSessionSnapshotsAsync(int keepCount = 20)
        {
            if (_database == null)
                throw new InvalidOperationException("Database not initialized");

            var allSnapshots = await _database.Table<SessionSnapshot>()
                                             .OrderByDescending(s => s.CreatedAt)
                                             .ToListAsync();

            if (allSnapshots.Count <= keepCount)
                return 0;

            var toDelete = allSnapshots.Skip(keepCount).ToList();
            int deletedCount = 0;

            foreach (var snapshot in toDelete)
            {
                await _database.DeleteAsync(snapshot);
                deletedCount++;
            }

            // Also cleanup old cached images
            await CleanupOldSessionImagesAsync();

            return deletedCount;
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
            _logger.LogDebug("Set database version to {Version}", version);
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