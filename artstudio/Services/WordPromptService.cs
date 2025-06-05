using artstudio.Data.Models;
using artstudio.Data;
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
                var db = await _databaseService.GetDatabaseAsync();

                var words = await db.QueryAsync<Word>(
                    "SELECT * FROM Word WHERE WordCollectionId = ? ORDER BY Category, Id",
                    collection.Id);

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