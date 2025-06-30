using artstudio.Data;
using Microsoft.Extensions.Logging;

namespace artstudio.Services
{
    public class WordPromptService
    {
        private readonly DatabaseService _databaseService;
        private readonly ILogger<WordPromptService> _logger;

        public WordPromptService(DatabaseService databaseService, ILogger<WordPromptService> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        public async Task<WordCollection> SaveWordCollectionWithCategoriesAsync(
            Dictionary<string, List<string>> categorizedWords,
            string? title = null,
            string? promptType = "generated")
        {
            try
            {
                _logger.LogDebug("SaveWordCollectionWithCategoriesAsync START");
                _logger.LogDebug("Title: {Title}", title);
                _logger.LogDebug("PromptType: {PromptType}", promptType);
                _logger.LogDebug("Categories count: {CategoriesCount}", categorizedWords.Count);

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
                    _logger.LogDebug("Category '{CategoryKey}': {WordCount} words", category.Key, category.Value.Count);
                    _logger.LogDebug("Words: {Words}", string.Join(", ", category.Value));
                    allWords.AddRange(category.Value);
                }

                collection.WordsList = allWords;
                _logger.LogDebug("Total words in collection: {TotalWords}", allWords.Count);
                _logger.LogDebug("All words: {AllWords}", string.Join(", ", allWords));

                // Save to database
                var db = await _databaseService.GetDatabaseAsync();
                // Insert the collection and get the ID
                await db.InsertAsync(collection);
                _logger.LogDebug("Saved collection with ID: {CollectionId}", collection.Id);

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
                        _logger.LogDebug("Saved word: '{WordText}' in category '{CategoryKey}'", wordText, category.Key);
                    }
                }

                _logger.LogDebug("SaveWordCollectionWithCategoriesAsync COMPLETE");
                return collection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveWordCollectionWithCategoriesAsync");
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
                _logger.LogError(ex, "Error in GetFavoritesAsync");
                return new List<WordCollection>();
            }
        }

        public async Task<List<WordCollection>> GetFavoritesWithWordsAsync()
        {
            try
            {
                _logger.LogDebug("GetFavoritesWithWordsAsync START");

                // Get all favorite collections
                var favorites = await GetFavoritesAsync();
                _logger.LogDebug("Found {FavoritesCount} favorite collections", favorites.Count);

                // For each collection, populate the WordsList property
                foreach (var collection in favorites)
                {
                    try
                    {
                        // Get all words for this collection
                        var words = await GetWordsForCollectionAsync(collection.Id);
                        // Populate the WordsList property
                        collection.WordsList = words.Select(w => w.Text).ToList();
                        _logger.LogDebug("Loaded {WordCount} words for collection '{CollectionTitle}' (ID: {CollectionId})",
                            words.Count, collection.Title, collection.Id);
                        _logger.LogDebug("Words: {Words}", string.Join(", ", collection.WordsList));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading words for collection {CollectionId}", collection.Id);
                        // Continue with empty word list if there's an error
                        collection.WordsList = new List<string>();
                    }
                }

                _logger.LogDebug("GetFavoritesWithWordsAsync COMPLETE");
                return favorites;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFavoritesWithWordsAsync");
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
                _logger.LogDebug("Retrieved {WordCount} words for collection {CollectionId}", words.Count, collectionId);
                return words;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting words for collection {CollectionId} - this might be because the Word table doesn't exist yet", collectionId);
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
                    _logger.LogDebug("Toggled favorite for collection {CollectionId}: IsFavorite = {IsFavorite}",
                        collectionId, collection.IsFavorite);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling favorite for collection {CollectionId}", collectionId);
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

                _logger.LogDebug("Retrieved words for collection {CollectionId}: {CategoriesCount} categories, {TotalWords} total words",
                    collection.Id, categorizedWords.Count, words.Count);
                return categorizedWords;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting words by category for collection {CollectionId}", collection.Id);
                return new Dictionary<string, List<string>>();
            }
        }
    }
}