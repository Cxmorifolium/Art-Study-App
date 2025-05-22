using artstudio.Services;
using artstudio.ViewModels;
using artstudio.Views;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.IO;
using System.Diagnostics;

#if WINDOWS
using artstudio.Platforms.Windows;
#endif

namespace artstudio;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit(options =>
            {
#if WINDOWS
                options.SetShouldEnableSnackbarOnWindows(true);
#endif
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register services  
        RegisterServices(builder.Services);

        // Register pages and view models  
        builder.Services.AddTransient<PalettePage>();
        builder.Services.AddTransient<PaletteViewModel>();

        builder.Services.AddSingleton<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Initialize prompt data in background
        _ = InitializePromptDataAsync();


        return app;
    }

    private static void RegisterServices(IServiceCollection services)
    {
        // Register debug service first
        services.AddSingleton<IDebugService, DebugService>();

        // Register other services
        services.AddSingleton<Export>();

        // Register ViewModels
        services.AddTransient<PromptGeneratorViewModel>();

#if WINDOWS
        services.AddSingleton<IFileSaveService, FileSaveService>();
#elif ANDROID
        // Add Android implementation here  
#elif IOS
        // Add iOS implementation here  
#elif MACCATALYST
        // Add Mac implementation here  
#endif
    }
    private static async Task InitializePromptDataAsync()
    {

        try
        {
            DebugLog("=== INITIALIZING PROMPT DATA ===");

            string baseTargetDir = Path.Combine(FileSystem.AppDataDirectory, "prompt_data");
            DebugLog($"Target directory: {baseTargetDir}");

            // Create the base directory if it doesn't exist
            if (!Directory.Exists(baseTargetDir))
            {
                Directory.CreateDirectory(baseTargetDir);
                DebugLog($"Created base directory: {baseTargetDir}");
            }

            // Create category folders
            string[] categories = { "nouns", "settings", "styles", "themes" };
            foreach (var category in categories)
            {
                string categoryPath = Path.Combine(baseTargetDir, category);
                if (!Directory.Exists(categoryPath))
                {
                    Directory.CreateDirectory(categoryPath);
                    DebugLog($"Created directory: {categoryPath}");
                }
            }

            // First, try to copy from embedded resources
            await CopyEmbeddedResourcesAsync(baseTargetDir);

            // If that fails or no files copied, create default files
            await CreateDefaultFilesAsync(baseTargetDir);

            DebugLog("=== PROMPT DATA INITIALIZATION COMPLETE ===");
        }
        catch (Exception ex)
        {
            DebugLog($"Error initializing prompt data: {ex.Message}");
            DebugLog($"Stack trace: {ex.StackTrace}");
        }
    }

    private static async Task CopyEmbeddedResourcesAsync(string baseTargetDir)
    {
        // List of resource files to copy
        var filesToCopy = new Dictionary<string, string>
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

        DebugLog($"Attempting to copy {filesToCopy.Count} files from embedded resources");

        int successCount = 0;
        int skipCount = 0;
        int errorCount = 0;

        foreach (var kvp in filesToCopy)
        {
            try
            {
                if (File.Exists(kvp.Value))
                {
                    DebugLog($"Skipping existing file: {Path.GetFileName(kvp.Value)}");
                    skipCount++;
                    continue;
                }

                DebugLog($"Copying: {kvp.Key} -> {Path.GetFileName(kvp.Value)}");

                using var input = await FileSystem.OpenAppPackageFileAsync(kvp.Key);
                using var output = File.Create(kvp.Value);
                await input.CopyToAsync(output);

                var fileInfo = new FileInfo(kvp.Value);
                DebugLog($"Created file: {Path.GetFileName(kvp.Value)} ({fileInfo.Length} bytes)");
                successCount++;
            }
            catch (Exception ex)
            {
                DebugLog($"Error copying {kvp.Key}: {ex.Message}");
                errorCount++;
            }
        }

        DebugLog($"Copy operation completed: {successCount} copied, {skipCount} skipped, {errorCount} errors");
    }

    private static async Task CreateDefaultFilesAsync(string baseTargetDir)
    {
        DebugLog("Creating default files as fallback");

        // Create default files for each category
        await CreateDefaultNounsFileAsync(Path.Combine(baseTargetDir, "nouns", "default.json"));
        await CreateDefaultSettingsFileAsync(Path.Combine(baseTargetDir, "settings", "default.json"));
        await CreateDefaultStylesFileAsync(Path.Combine(baseTargetDir, "styles", "default.json"));
        await CreateDefaultThemesFileAsync(Path.Combine(baseTargetDir, "themes", "default.json"));
    }

    private static async Task CreateDefaultNounsFileAsync(string filePath)
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

    private static async Task CreateDefaultSettingsFileAsync(string filePath)
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

    private static async Task CreateDefaultStylesFileAsync(string filePath)
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

    private static async Task CreateDefaultThemesFileAsync(string filePath)
    {
        if (File.Exists(filePath)) return;

        var data = new Dictionary<string, object>
        {
            ["description"] = "Default themes for prompt generation",
            ["general"] = new List<string> { "peaceful", "chaotic", "mysterious", "magical", "dystopian", "utopian", "post-apocalyptic", "ancient", "cosmic", "spiritual", "romantic", "creepy", "hopeful", "melancholic", "adventurous" }
        };

        await WriteJsonFileAsync(filePath, data);
    }

    private static async Task WriteJsonFileAsync(string filePath, Dictionary<string, object> data)
    {
        try
        {
            using FileStream fs = File.Create(filePath);
            await JsonSerializer.SerializeAsync(fs, data, new JsonSerializerOptions { WriteIndented = true });
            DebugLog($"Created default file: {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            DebugLog($"Error creating default file {filePath}: {ex.Message}");
        }
    }

    // Simple debug logging that WORKS in MAUI
    private static void DebugLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logMessage = $"[{timestamp}] {message}";

        // Output to multiple channels to ensure visibility
        Debug.WriteLine(logMessage);
        System.Console.WriteLine(logMessage);

        // Try to write to a log file
        try
        {
            var logPath = Path.Combine(FileSystem.AppDataDirectory, "init_log.txt");
            File.AppendAllText(logPath, logMessage + Environment.NewLine);
        }
        catch
        {
            // Ignore file write errors during initialization
        }
    }
}