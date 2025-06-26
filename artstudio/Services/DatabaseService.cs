using SQLite;
using System.Diagnostics;
using artstudio.Data;
using artstudio.Models;

namespace artstudio.Services
{
    public partial class DatabaseService : IDisposable
    {
        private SQLiteAsyncConnection? _database;
        private readonly string _dbPath;
        private const int CurrentDatabaseVersion = 10; // Increment this when schema changes: (added new column for session user enter name)

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
            
            // Gallery
            await _database.CreateTableAsync<UserUploadedImage>();

            // Session snapshots
            await _database.CreateTableAsync<SessionSnapshot>();

            // Image favoriting
            await _database.CreateTableAsync<FavoriteImageItem>();

            DebugLog("Created all tables: WordCollection, Word, PaletteCollection, PaletteColor, FavoriteSwatch, and Gallery");
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

            if (fromVersion < 5)
            {
                // Migration to version 5: Add gallery tables
                try
                {
                    await _database.CreateTableAsync<UserUploadedImage>();
                    DebugLog("Added gallery tables: UserUploadedImage, ImageReference");
                }
                catch (Exception ex)
                {
                    DebugLog($"Error creating gallery tables: {ex.Message}");
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

                    DebugLog("Successfully created UserUploadedImage table with all required columns");
                }
                catch (Exception ex)
                {
                    DebugLog($"Error recreating UserUploadedImage table: {ex.Message}");
                    throw;
                }
            }

            if (fromVersion < 7)
            {
                // Migration to version 7: Add SessionSnapshot table
                try
                {
                    await _database.CreateTableAsync<SessionSnapshot>();
                    DebugLog("Added SessionSnapshot table for session saving functionality");
                }
                catch (Exception ex)
                {
                    DebugLog($"Error creating SessionSnapshot table: {ex.Message}");
                    // Table might already exist, continue
                }
            }

            if (fromVersion < 8)
            {
                // Migration to version 8: Add FavoriteImageItem table
                try
                {
                    await _database.CreateTableAsync<FavoriteImageItem>();
                    DebugLog("Added FavoriteImageItem table for image favoriting functionality");
                }
                catch (Exception ex)
                {
                    DebugLog($"Error creating FavoriteImageItem table: {ex.Message}");
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

                    DebugLog("Recreated FavoriteImageItem table with proper primary key");
                }
                catch (Exception ex)
                {
                    DebugLog($"Error recreating FavoriteImageItem table: {ex.Message}");
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
                        DebugLog("Added Title column to SessionSnapshot table for custom session naming");
                    }
                    else
                    {
                        DebugLog("Title column already exists in SessionSnapshot table");
                    }
                }
                catch (Exception ex)
                {
                    DebugLog($"Error adding Title column to SessionSnapshot table: {ex.Message}");
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

                DebugLog($"Retrieved {collections.Count} favorite word collections");
                return collections;
            }
            catch (Exception ex)
            {
                DebugLog($"Error getting favorites: {ex.Message}");
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

                DebugLog($"Deleted word collection {collectionId}");
            }
            catch (Exception ex)
            {
                DebugLog($"Error deleting word collection {collectionId}: {ex.Message}");
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

                DebugLog($"Attempting to delete favorite image with ID: {favoriteImageId}");

                // First verify the record exists
                var existingFavorite = await _database.FindAsync<FavoriteImageItem>(favoriteImageId);
                if (existingFavorite == null)
                {
                    DebugLog($"Favorite image with ID {favoriteImageId} not found");
                    return 0;
                }

                // Delete the record
                var result = await _database.DeleteAsync<FavoriteImageItem>(favoriteImageId);
                DebugLog($"Successfully deleted favorite image {favoriteImageId}, rows affected: {result}");

                return result;
            }
            catch (Exception ex)
            {
                DebugLog($"Error deleting favorite image {favoriteImageId}: {ex.Message}");
                throw;
            }
        }
        public async Task<int> DeleteFavoriteImageByUnsplashIdAsync(string unsplashId)
        {
            try
            {
                if (_database == null)
                    throw new InvalidOperationException("Database not initialized");

                DebugLog($"Attempting to delete favorite image with UnsplashId: {unsplashId}");

                // Find and delete by UnsplashId
                var result = await _database.ExecuteAsync(
                    "DELETE FROM FavoriteImageItem WHERE UnsplashId = ?",
                    unsplashId);

                DebugLog($"Successfully deleted favorite image {unsplashId}, rows affected: {result}");
                return result;
            }
            catch (Exception ex)
            {
                DebugLog($"Error deleting favorite image by UnsplashId {unsplashId}: {ex.Message}");
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
                DebugLog($"Error getting favorite by UnsplashId {unsplashId}: {ex.Message}");
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

                DebugLog($"Retrieved {images.Count} user uploaded images");
                return images;
            }
            catch (Exception ex)
            {
                DebugLog($"Error getting user images: {ex.Message}");
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
                    DebugLog($"Retrieved user uploaded image: {image.Id}");
                }
                else
                {
                    DebugLog($"User uploaded image {imageId} not found");
                }

                return image;
            }
            catch (Exception ex)
            {
                DebugLog($"Error getting user image {imageId}: {ex.Message}");
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
                    DebugLog($"Saved new user uploaded image: {image.DisplayTitle} (ID: {image.Id})");
                }
                else
                {
                    // Update existing image
                    await db.UpdateAsync(image);
                    DebugLog($"Updated user uploaded image: {image.Id}");
                }

                return image.Id;
            }
            catch (Exception ex)
            {
                DebugLog($"Error saving user image: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateUserUploadedImageAsync(UserUploadedImage image)
        {
            try
            {
                var db = await GetDatabaseAsync();
                await db.UpdateAsync(image);

                DebugLog($"Updated user uploaded image: {image.Id}");
            }
            catch (Exception ex)
            {
                DebugLog($"Error updating user image: {ex.Message}");
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

                DebugLog($"Deleted user uploaded image {imageId}");
            }
            catch (Exception ex)
            {
                DebugLog($"Error deleting user image {imageId}: {ex.Message}");
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

                DebugLog($"Search for '{searchTerm}' returned {images.Count} results");
                return images;
            }
            catch (Exception ex)
            {
                DebugLog($"Error searching gallery: {ex.Message}");
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

                DebugLog($"Found {images.Count} images with tag '{tag}'");
                return images;
            }
            catch (Exception ex)
            {
                DebugLog($"Error getting images by tag '{tag}': {ex.Message}");
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
                            DebugLog($"Error parsing tags from image: {ex.Message}");
                        }
                    }
                }

                var result = allTags.OrderBy(tag => tag).ToList();
                DebugLog($"Found {result.Count} unique custom tags");
                return result;
            }
            catch (Exception ex)
            {
                DebugLog($"Error getting all custom tags: {ex.Message}");
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
                DebugLog($"Deleted image file: {imagePath}");
            }
            catch (Exception ex)
            {
                DebugLog($"Error deleting image file {imagePath}: {ex.Message}");
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
                                DebugLog($"Failed to delete old image file {file}: {ex.Message}");
                            }
                        }
                    }
                });

                if (deletedCount > 0)
                {
                    DebugLog($"Cleaned up {deletedCount} old session image files");
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Error during session images cleanup: {ex.Message}");
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