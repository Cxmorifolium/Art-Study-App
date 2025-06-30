using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace artstudio.Services;

public interface IPromptDataService
{
    Task InitializeAsync();
}

public class PromptDataService : IPromptDataService
{
    private readonly ILogger<PromptDataService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public PromptDataService(ILogger<PromptDataService> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Starting prompt data initialization");

            string baseTargetDir = Path.Combine(FileSystem.AppDataDirectory, "prompt_data");
            _logger.LogDebug("Target directory: {Directory}", baseTargetDir);

            // Create the base directory if it doesn't exist
            if (!Directory.Exists(baseTargetDir))
            {
                Directory.CreateDirectory(baseTargetDir);
                _logger.LogDebug("Created base directory: {Directory}", baseTargetDir);
            }

            // Create category folders
            string[] categories = ["nouns", "settings", "styles", "themes"];
            foreach (var category in categories)
            {
                string categoryPath = Path.Combine(baseTargetDir, category);
                if (!Directory.Exists(categoryPath))
                {
                    Directory.CreateDirectory(categoryPath);
                    _logger.LogDebug("Created directory: {Directory}", categoryPath);
                }
            }

            // Copy embedded resources and create defaults if needed
            await CopyEmbeddedResourcesAsync(baseTargetDir);
            await CreateDefaultFilesAsync(baseTargetDir);

            _logger.LogInformation("Prompt data initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize prompt data");
        }
    }

    private async Task CopyEmbeddedResourcesAsync(string baseTargetDir)
    {
        var filesToCopy = GetEmbeddedResourceFiles(baseTargetDir);

        _logger.LogDebug("Attempting to copy {FileCount} files from embedded resources", filesToCopy.Count);

        int successCount = 0;
        int skipCount = 0;
        int errorCount = 0;

        foreach (var (resourcePath, targetPath) in filesToCopy)
        {
            try
            {
                if (File.Exists(targetPath))
                {
                    _logger.LogDebug("Skipping existing file: {FileName}", Path.GetFileName(targetPath));
                    skipCount++;
                    continue;
                }

                _logger.LogDebug("Copying: {ResourcePath} -> {FileName}", resourcePath, Path.GetFileName(targetPath));

                using var input = await FileSystem.OpenAppPackageFileAsync(resourcePath);
                using var output = File.Create(targetPath);
                await input.CopyToAsync(output);

                var fileInfo = new FileInfo(targetPath);
                _logger.LogDebug("Created file: {FileName} ({FileSize} bytes)", Path.GetFileName(targetPath), fileInfo.Length);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to copy embedded resource: {ResourcePath}", resourcePath);
                errorCount++;
            }
        }

        _logger.LogDebug("Copy operation completed: {SuccessCount} copied, {SkipCount} skipped, {ErrorCount} errors",
            successCount, skipCount, errorCount);
    }

    private static Dictionary<string, string> GetEmbeddedResourceFiles(string baseTargetDir)
    {
        return new Dictionary<string, string>
        {
            { "prompt_data/nouns/cats.json", Path.Combine(baseTargetDir, "nouns", "cats.json") },
            { "prompt_data/nouns/common.json", Path.Combine(baseTargetDir, "nouns", "common.json") },
            { "prompt_data/nouns/flowers.json", Path.Combine(baseTargetDir, "nouns", "flowers.json") },
            { "prompt_data/nouns/fruits.json", Path.Combine(baseTargetDir, "nouns", "fruits.json") },
            { "prompt_data/nouns/greek_gods.json", Path.Combine(baseTargetDir, "nouns", "greek_gods.json") },
            { "prompt_data/nouns/greek_monsters.json", Path.Combine(baseTargetDir, "nouns", "greek_monsters.json") },
            { "prompt_data/nouns/herbs_n_spices.json", Path.Combine(baseTargetDir, "nouns", "herbs_n_spices.json") },
            { "prompt_data/nouns/instruments.json", Path.Combine(baseTargetDir, "nouns", "instruments.json") },
            { "prompt_data/nouns/monsters.json", Path.Combine(baseTargetDir, "nouns", "monsters.json") },
            { "prompt_data/nouns/norse_gods.json", Path.Combine(baseTargetDir, "nouns", "norse_gods.json") },
            { "prompt_data/nouns/objects.json", Path.Combine(baseTargetDir, "nouns", "objects.json") },
            { "prompt_data/nouns/premodern_weapons.json", Path.Combine(baseTargetDir, "nouns", "premodern_weapons.json") },
            { "prompt_data/settings/atmosphere.json", Path.Combine(baseTargetDir, "settings", "atmosphere.json") },
            { "prompt_data/settings/bookuniverse.json", Path.Combine(baseTargetDir, "settings", "bookuniverse.json") },
            { "prompt_data/settings/eras.json", Path.Combine(baseTargetDir, "settings", "eras.json") },
            { "prompt_data/settings/gameuniverse.json", Path.Combine(baseTargetDir, "settings", "gameuniverse.json") },
            { "prompt_data/styles/animation.json", Path.Combine(baseTargetDir, "styles", "animation.json") },
            { "prompt_data/styles/artist.json", Path.Combine(baseTargetDir, "styles", "artist.json") },
            { "prompt_data/styles/isms.json", Path.Combine(baseTargetDir, "styles", "isms.json") },
            { "prompt_data/styles/narrative.json", Path.Combine(baseTargetDir, "styles", "narrative.json") },
            { "prompt_data/themes/subjects.json", Path.Combine(baseTargetDir, "themes", "subjects.json") },
        };
    }

    private async Task CreateDefaultFilesAsync(string baseTargetDir)
    {
        _logger.LogDebug("Creating default files as fallback");

        await CreateDefaultNounsFileAsync(Path.Combine(baseTargetDir, "nouns", "default.json"));
        await CreateDefaultSettingsFileAsync(Path.Combine(baseTargetDir, "settings", "default.json"));
        await CreateDefaultStylesFileAsync(Path.Combine(baseTargetDir, "styles", "default.json"));
        await CreateDefaultThemesFileAsync(Path.Combine(baseTargetDir, "themes", "default.json"));
    }

    private async Task CreateDefaultNounsFileAsync(string filePath)
    {
        if (File.Exists(filePath)) return;

        var data = new Dictionary<string, object>
        {
            ["description"] = "Default nouns for prompt generation",
            ["general"] = new List<string> { "cat", "dog", "mountain", "ocean", "forest", "city", "robot", "dragon", "castle", "person", "flower", "car", "spaceship", "planet", "monster" },
            ["animals"] = new List<string> { "lion", "wolf", "eagle", "butterfly", "dolphin", "fox", "owl", "tiger", "bear", "horse" },
            ["places"] = new List<string> { "beach", "desert", "jungle", "cave", "underwater", "space", "village", "ruins", "skyscraper", "wasteland" }
        };

        await WriteJsonFileAsync(filePath, data);
    }

    private async Task CreateDefaultSettingsFileAsync(string filePath)
    {
        if (File.Exists(filePath)) return;

        var data = new Dictionary<string, object>
        {
            ["description"] = "Default settings for prompt generation",
            ["general"] = new List<string> { "at night", "during sunset", "in the rain", "in the snow", "in the fog", "under the stars", "in a dream", "on another planet" },
            ["locations"] = new List<string> { "in a forest", "on a mountain", "in a city", "underwater", "in a desert", "in space", "in a cave", "on a ship" }
        };

        await WriteJsonFileAsync(filePath, data);
    }

    private async Task CreateDefaultStylesFileAsync(string filePath)
    {
        if (File.Exists(filePath)) return;

        var data = new Dictionary<string, object>
        {
            ["description"] = "Default styles for prompt generation",
            ["general"] = new List<string> { "realistic", "abstract", "minimalist", "surrealistic", "vintage", "futuristic", "cyberpunk", "fantastical", "watercolor", "oil painting" },
            ["digital"] = new List<string> { "pixel art", "3D rendering", "digital painting", "low poly", "voxel art", "glitch art", "vector art" },
            ["traditional"] = new List<string> { "pencil sketch", "charcoal", "ink drawing", "pastel", "acrylic", "chalk", "stained glass" }
        };

        await WriteJsonFileAsync(filePath, data);
    }

    private async Task CreateDefaultThemesFileAsync(string filePath)
    {
        if (File.Exists(filePath)) return;

        var data = new Dictionary<string, object>
        {
            ["description"] = "Default themes for prompt generation",
            ["general"] = new List<string> { "peaceful", "chaotic", "mysterious", "magical", "dystopian", "utopian", "post-apocalyptic", "ancient", "cosmic", "spiritual", "romantic", "creepy", "hopeful", "melancholic", "adventurous" }
        };

        await WriteJsonFileAsync(filePath, data);
    }

    private async Task WriteJsonFileAsync(string filePath, Dictionary<string, object> data)
    {
        try
        {
            using FileStream fs = File.Create(filePath);
            await JsonSerializer.SerializeAsync(fs, data, JsonOptions);
            _logger.LogDebug("Created default file: {FileName}", Path.GetFileName(filePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create default file: {FilePath}", filePath);
        }
    }
}