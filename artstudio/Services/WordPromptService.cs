using artstudio.Data.Models;
using artstudio.Service;
using System.Diagnostics;

namespace artstudio.Services
{
    public class WordPromptService
    {
        private readonly DatabaseService _databaseService;

        public WordPromptService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<WordCollection> SaveWordCollectionWithCategoriesAsync(
            Dictionary<string, List<string>> categorizedWords,
            string? title = null,
            string? promptType = "generated")
        {
            try
            {
                DebugLog("=== SaveWordCollectionWithCategoriesAsync START ===");
                DebugLog($"Title: {title}");
                DebugLog($"PromptType: {promptType}");
                DebugLog($"Categories count: {categorizedWords.Count}");

                // Create the word collection
                var collection = new WordCollection
                {
                    Title = title ?? $"Generated - {DateTime.Now:MMM dd, HH:mm}",
                    CreatedAt = DateTime.Now,
                    PromptType = promptType,
                    IsFavorite = false
                };

                // Flatten all words into a single list for WordsList property
                var allWords = new List<string>();
                foreach (var category in categorizedWords)
                {
                    DebugLog($"Category '{category.Key}': {category.Value.Count} words");
                    DebugLog($"  Words: {string.Join(", ", category.Value)}");
                    allWords.AddRange(category.Value);
                }

                collection.WordsList = allWords;
                DebugLog($"Total words in collection: {allWords.Count}");
                DebugLog($"All words: {string.Join(", ", allWords)}");

                // Save to database
                var db = await _databaseService.GetDatabaseAsync();

                // Insert the collection and get the ID
                await db.InsertAsync(collection);
                DebugLog($"Saved collection with ID: {collection.Id}");

                // Save individual words with categories
                foreach (var category in categorizedWords)
                {
                    foreach (var wordText in category.Value)
                    {
                        var word = new Word
                        {
                            WordCollectionId = collection.Id,
                            Text = wordText,
                            Category = category.Key,
                            CreatedAt = DateTime.Now
                        };

                        await db.InsertAsync(word);
                        DebugLog($"Saved word: '{wordText}' in category '{category.Key}'");
                    }
                }

                DebugLog("=== SaveWordCollectionWithCategoriesAsync COMPLETE ===");
                return collection;
            }
            catch (Exception ex)
            {
                DebugLog($"Error in SaveWordCollectionWithCategoriesAsync: {ex.Message}");
                DebugLog($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<List<WordCollection>> GetFavoritesAsync()
        {
            try
            {
                return await _databaseService.GetFavoritesAsync();
            }
            catch (Exception ex)
            {
                DebugLog($"Error in GetFavoritesAsync: {ex.Message}");
                return new List<WordCollection>();
            }
        }

        public async Task<List<WordCollection>> GetFavoritesWithWordsAsync()
        {
            try
            {
                DebugLog("=== GetFavoritesWithWordsAsync START ===");

                // Get all favorite collections
                var favorites = await GetFavoritesAsync();
                DebugLog($"Found {favorites.Count} favorite collections");

                // For each collection, populate the WordsList property
                foreach (var collection in favorites)
                {
                    try
                    {
                        // Get all words for this collection
                        var words = await GetWordsForCollectionAsync(collection.Id);

                        // Populate the WordsList property
                        collection.WordsList = words.Select(w => w.Text).ToList();

                        DebugLog($"Loaded {words.Count} words for collection '{collection.Title}' (ID: {collection.Id})");
                        DebugLog($"  Words: {string.Join(", ", collection.WordsList)}");
                    }
                    catch (Exception ex)
                    {
                        DebugLog($"Error loading words for collection {collection.Id}: {ex.Message}");
                        // Continue with empty word list if there's an error
                        collection.WordsList = new List<string>();
                    }
                }

                DebugLog("=== GetFavoritesWithWordsAsync COMPLETE ===");
                return favorites;
            }
            catch (Exception ex)
            {
                DebugLog($"Error in GetFavoritesWithWordsAsync: {ex.Message}");
                return new List<WordCollection>();
            }
        }

        // Get words for a specific collection
        public async Task<List<Word>> GetWordsForCollectionAsync(int collectionId)
        {
            try
            {
                var db = await _databaseService.GetDatabaseAsync();

                var words = await db.QueryAsync<Word>(
                    "SELECT * FROM Word WHERE WordCollectionId = ? ORDER BY Category, Id",
                    collectionId);

                DebugLog($"Retrieved {words.Count} words for collection {collectionId}");
                return words;
            }
            catch (Exception ex)
            {
                DebugLog($"Error getting words for collection {collectionId}: {ex.Message}");
                DebugLog($"This might be because the Word table doesn't exist yet");
                return new List<Word>();
            }
        }

        public async Task ToggleFavoriteAsync(int collectionId)
        {
            try
            {
                var db = await _databaseService.GetDatabaseAsync();

                // Get current state
                var collection = await db.FindAsync<WordCollection>(collectionId);
                if (collection != null)
                {
                    // Toggle favorite status
                    collection.IsFavorite = !collection.IsFavorite;

                    await db.UpdateAsync(collection);

                    DebugLog($"Toggled favorite for collection {collectionId}: IsFavorite = {collection.IsFavorite}");
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Error toggling favorite for collection {collectionId}: {ex.Message}");
                throw;
            }
        }

        public async Task<Dictionary<string, List<string>>> GetWordsByCategoryAsync(WordCollection collection)
        {
            try
            {
                var words = await GetWordsForCollectionAsync(collection.Id);

                // Also populate the collection's WordsList property for display purposes
                collection.WordsList = words.Select(w => w.Text).ToList();

                var categorizedWords = new Dictionary<string, List<string>>();

                foreach (var word in words)
                {
                    var category = word.Category ?? "general";

                    if (!categorizedWords.ContainsKey(category))
                    {
                        categorizedWords[category] = new List<string>();
                    }

                    categorizedWords[category].Add(word.Text);
                }

                DebugLog($"Retrieved words for collection {collection.Id}: {categorizedWords.Count} categories, {words.Count} total words");

                return categorizedWords;
            }
            catch (Exception ex)
            {
                DebugLog($"Error getting words by category for collection {collection.Id}: {ex.Message}");
                return new Dictionary<string, List<string>>();
            }
        }

        private static void DebugLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logMessage = $"[WPS {timestamp}] {message}";
            Debug.WriteLine(logMessage);
            System.Console.WriteLine(logMessage);
        }
    }
}