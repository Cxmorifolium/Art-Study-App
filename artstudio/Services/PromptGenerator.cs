using System.Text.Json;
using System.IO;
using System.Diagnostics;

namespace artstudio.Services
{
    public class PromptGenerator
    {
        private Dictionary<string, Dictionary<string, List<string>>> _promptData;
        private Random _random = new Random();

        public PromptGenerator(string baseDir)
        {
            DebugLog($"Initializing PromptGenerator with baseDir: {baseDir}");
            _promptData = LoadPromptData(baseDir);
            EnsureDefaultCategories();
            LogLoadedData();
        }

        private Dictionary<string, Dictionary<string, List<string>>> LoadPromptData(string baseDir)
        {
            var promptData = new Dictionary<string, Dictionary<string, List<string>>>();

            try
            {
                DebugLog($"Checking if base directory exists: {baseDir}");

                if (!Directory.Exists(baseDir))
                {
                    DebugLog($"Base directory does not exist: {baseDir}");
                    return promptData;
                }

                var categoryDirs = Directory.GetDirectories(baseDir);
                DebugLog($"Found {categoryDirs.Length} category directories");

                foreach (var categoryDir in categoryDirs)
                {
                    string category = Path.GetFileName(categoryDir);
                    DebugLog($"Processing category: {category}");
                    promptData[category] = new Dictionary<string, List<string>>();

                    var jsonFiles = Directory.GetFiles(categoryDir, "*.json");
                    DebugLog($"Found {jsonFiles.Length} JSON files in {category}");

                    foreach (var file in jsonFiles)
                    {
                        try
                        {
                            LoadJsonFileSync(file, promptData[category]);
                        }
                        catch (Exception ex)
                        {
                            DebugLog($"Error processing file {file}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Error accessing directory {baseDir}: {ex.Message}");
            }

            return promptData;
        }

        private void LoadJsonFileSync(string filePath, Dictionary<string, List<string>> categoryData)
        {
            try
            {
                DebugLog($"Reading file: {Path.GetFileName(filePath)}");

                string jsonContent = File.ReadAllText(filePath);

                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    DebugLog($"File {filePath} is empty or whitespace");
                    return;
                }

                DebugLog($"File content length: {jsonContent.Length} characters");

                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    DebugLog($"JSON root is not an object in file {filePath}");
                    return;
                }

                DebugLog($"Processing {root.EnumerateObject().Count()} top-level properties from {Path.GetFileName(filePath)}");

                foreach (var property in root.EnumerateObject())
                {
                    if (property.Name == "description" || property.Name == "sources")
                    {
                        DebugLog($"Skipping metadata property: {property.Name}");
                        continue;
                    }

                    ProcessJsonProperty(property.Name, property.Value, categoryData);
                }
            }
            catch (JsonException jsonEx)
            {
                DebugLog($"JSON parsing error in {filePath}: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                DebugLog($"Error reading {filePath}: {ex.Message}");
            }
        }

        private void ProcessJsonProperty(string propertyName, JsonElement value, Dictionary<string, List<string>> categoryData)
        {
            DebugLog($"Processing property: {propertyName}, ValueKind: {value.ValueKind}");

            switch (value.ValueKind)
            {
                case JsonValueKind.Object:
                    DebugLog($"Processing nested object with {value.EnumerateObject().Count()} properties");

                    foreach (var nestedProperty in value.EnumerateObject())
                    {
                        string fullKey = $"{propertyName}.{nestedProperty.Name}";
                        ProcessJsonProperty(fullKey, nestedProperty.Value, categoryData);
                    }
                    break;

                case JsonValueKind.Array:
                    ProcessJsonArray(propertyName, value, categoryData);
                    break;

                case JsonValueKind.String:
                    var stringValue = value.GetString();
                    if (!string.IsNullOrEmpty(stringValue))
                    {
                        if (!categoryData.ContainsKey(propertyName))
                            categoryData[propertyName] = new List<string>();

                        categoryData[propertyName].Add(stringValue);
                        DebugLog($"Added single string to {propertyName}: {stringValue}");
                    }
                    break;

                default:
                    DebugLog($"Unexpected JsonValueKind: {value.ValueKind} for property: {propertyName}");
                    break;
            }
        }

        private void ProcessJsonArray(string propertyName, JsonElement arrayElement, Dictionary<string, List<string>> categoryData)
        {
            var arrayItems = arrayElement.EnumerateArray().ToList();
            DebugLog($"Processing array with {arrayItems.Count} items for property {propertyName}");

            if (!categoryData.ContainsKey(propertyName))
            {
                categoryData[propertyName] = new List<string>();
            }

            int stringItemCount = 0;
            foreach (var item in arrayItems)
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    string? itemValue = item.GetString();
                    if (!string.IsNullOrEmpty(itemValue))
                    {
                        categoryData[propertyName].Add(itemValue);
                        stringItemCount++;
                    }
                }
                else if (item.ValueKind == JsonValueKind.Object)
                {
                    // Handle objects within arrays (flatten them)
                    foreach (var objProperty in item.EnumerateObject())
                    {
                        string nestedKey = $"{propertyName}.{objProperty.Name}";
                        ProcessJsonProperty(nestedKey, objProperty.Value, categoryData);
                    }
                }
                else
                {
                    DebugLog($"Non-string array item found: {item.ValueKind} in {propertyName}");
                }
            }

            DebugLog($"Added {stringItemCount} string items to {propertyName}");
        }

        private void EnsureDefaultCategories()
        {
            var defaultCategories = new Dictionary<string, Dictionary<string, List<string>>>
            {
                ["nouns"] = new Dictionary<string, List<string>>
                {
                    ["general"] = new List<string> { "cat", "dog", "mountain", "ocean", "forest", "city", "robot", "dragon", "castle", "person", "flower", "car", "spaceship", "planet", "monster" }
                },
                ["settings"] = new Dictionary<string, List<string>>
                {
                    ["general"] = new List<string> { "at night", "during sunset", "in the rain", "in the snow", "in the fog", "under the stars", "in a dream", "on another planet" }
                },
                ["styles"] = new Dictionary<string, List<string>>
                {
                    ["general"] = new List<string> { "realistic", "abstract", "minimalist", "surrealistic", "vintage", "futuristic", "cyberpunk", "fantastical", "watercolor", "oil painting" }
                },
                ["themes"] = new Dictionary<string, List<string>>
                {
                    ["general"] = new List<string> { "peaceful", "chaotic", "mysterious", "magical", "dystopian", "utopian", "post-apocalyptic", "ancient", "cosmic", "spiritual" }
                }
            };

            foreach (var category in defaultCategories.Keys)
            {
                if (!_promptData.ContainsKey(category))
                {
                    DebugLog($"Adding default category: {category}");
                    _promptData[category] = defaultCategories[category];
                }
                else if (_promptData[category].Count == 0)
                {
                    DebugLog($"Category {category} exists but is empty, adding defaults");
                    _promptData[category] = defaultCategories[category];
                }
                else
                {
                    DebugLog($"Category {category} already exists with {_promptData[category].Count} subcategories");
                }
            }
        }

        private void LogLoadedData()
        {
            DebugLog("=== LOADED DATA SUMMARY ===");

            foreach (var category in _promptData.Keys)
            {
                DebugLog($"Category: {category}");

                foreach (var subcategory in _promptData[category].Keys)
                {
                    int itemCount = _promptData[category][subcategory].Count;
                    DebugLog($"  Subcategory: {subcategory} ({itemCount} items)");

                    if (itemCount > 0)
                    {
                        var sampleItems = _promptData[category][subcategory].Take(3);
                        DebugLog($"    Sample items: {string.Join(", ", sampleItems)}");
                    }
                }
            }

            DebugLog("=== END SUMMARY ===");
        }

        public List<string> GetAvailableCategories()
        {
            return _promptData.Keys.ToList();
        }

        public int GetCategoryItemsCount(string category)
        {
            if (!_promptData.ContainsKey(category))
                return 0;

            int count = 0;
            foreach (var items in _promptData[category].Values)
            {
                count += items.Count;
            }
            return count;
        }

        public List<string> GetRandomItems(string category, string? subcategory = null, int count = 1)
        {
            DebugLog($"GetRandomItems called: category={category}, subcategory={subcategory}, count={count}");

            if (!_promptData.ContainsKey(category))
            {
                DebugLog($"Category {category} not found");
                return new List<string>();
            }

            List<string> items;
            if (!string.IsNullOrEmpty(subcategory) && _promptData[category].ContainsKey(subcategory))
            {
                items = _promptData[category][subcategory];
                DebugLog($"Using specific subcategory {subcategory} with {items.Count} items");
            }
            else
            {
                items = new List<string>();
                foreach (var subItems in _promptData[category].Values)
                {
                    items.AddRange(subItems);
                }
                DebugLog($"Using all subcategories, total items: {items.Count}");
            }

            count = Math.Min(count, items.Count);
            if (count == 0)
            {
                DebugLog("No items available");
                return new List<string>();
            }

            var result = items.OrderBy(x => _random.Next()).Take(count).ToList();
            DebugLog($"Returning {result.Count} items: {string.Join(", ", result)}");
            return result;
        }

        public (string, Dictionary<string, List<string>>) GenerateDefaultPrompt()
        {
            return GeneratePrompt(
                includeNouns: true,
                includeSettings: true,
                includeStyles: true,
                includeThemes: true,
                nounCount: 1,
                settingMin: 1,
                settingMax: 2,
                styleMin: 1,
                styleMax: 2,
                themeCount: 1,
                settingProbability: 0.9,
                themeProbability: 0.7
            );
        }

        public (string, Dictionary<string, List<string>>) GenerateCustomPrompt(
            int nounCount = 0,
            int settingCount = 0,
            int styleCount = 0,
            int themeCount = 0)
        {
            // Ensure counts are within valid ranges
            nounCount = Math.Max(0, Math.Min(5, nounCount));
            settingCount = Math.Max(0, Math.Min(3, settingCount));
            styleCount = Math.Max(0, Math.Min(3, styleCount));
            themeCount = Math.Max(0, Math.Min(2, themeCount));

            return GeneratePrompt(
                includeNouns: nounCount > 0,
                includeSettings: settingCount > 0,
                includeStyles: styleCount > 0,
                includeThemes: themeCount > 0,
                nounCount: nounCount,
                settingMin: settingCount,
                settingMax: settingCount,
                styleMin: styleCount,
                styleMax: styleCount,
                themeCount: themeCount,
                settingProbability: 1.0,
                themeProbability: 1.0
            );
        }

        public (string, Dictionary<string, List<string>>) GeneratePrompt(
            bool includeNouns = true,
            bool includeSettings = true,
            bool includeStyles = true,
            bool includeThemes = true,
            int nounCount = 1,
            int settingMin = 1,
            int settingMax = 2,
            int styleMin = 1,
            int styleMax = 2,
            int themeCount = 1,
            double settingProbability = 0.9,
            double themeProbability = 0.5)
        {
            DebugLog("=== GENERATING PROMPT ===");
            var components = new Dictionary<string, List<string>>();
            var promptParts = new List<string>();

            // Add nouns
            if (includeNouns && nounCount > 0)
            {
                DebugLog($"Getting {nounCount} nouns");
                var nouns = GetRandomItems("nouns", count: nounCount);
                if (nouns.Count > 0)
                {
                    components["nouns"] = nouns;
                    promptParts.AddRange(nouns);
                }
            }

            // Add settings
            if (includeSettings && _random.NextDouble() < settingProbability)
            {
                int actualSettingCount = settingMin == settingMax ? settingMin : _random.Next(settingMin, settingMax + 1);
                DebugLog($"Getting {actualSettingCount} settings");

                var settings = GetRandomItems("settings", count: actualSettingCount);
                if (settings.Count > 0)
                {
                    components["settings"] = settings;
                    promptParts.AddRange(settings);
                }
            }

            // Add styles
            if (includeStyles)
            {
                int actualStyleCount = styleMin == styleMax ? styleMin : _random.Next(styleMin, styleMax + 1);
                DebugLog($"Getting {actualStyleCount} styles");

                var styles = GetRandomItems("styles", count: actualStyleCount);
                if (styles.Count > 0)
                {
                    components["styles"] = styles;
                    promptParts.AddRange(styles);
                }
            }

            // Add themes
            if (includeThemes && _random.NextDouble() < themeProbability && themeCount > 0)
            {
                DebugLog($"Getting {themeCount} themes");
                var themes = GetRandomItems("themes", count: themeCount);
                if (themes.Count > 0)
                {
                    components["themes"] = themes;
                    promptParts.AddRange(themes);
                }
            }

            // Shuffle the prompt parts for variety
            promptParts = promptParts.OrderBy(x => _random.Next()).ToList();
            string prompt = string.Join(", ", promptParts);

            DebugLog($"Generated prompt: {prompt}");
            DebugLog("=== END PROMPT GENERATION ===");

            return (prompt, components);
        }

        // Simple debug logging method
        private static void DebugLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logMessage = $"[PG {timestamp}] {message}";

            // Output to multiple channels to ensure visibility
            Debug.WriteLine(logMessage);
            System.Console.WriteLine(logMessage);

            // Also try to write to a log file
            try
            {
                var logPath = Path.Combine(FileSystem.AppDataDirectory, "generator_log.txt");
                File.AppendAllText(logPath, logMessage + Environment.NewLine);
            }
            catch
            {
                // Ignore file write errors
            }
        }
    }
}