using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace artstudio.Models
{
    public class PromptDataLoader
    {   // Initialize the data
        public static async Task InitializePromptsAsync()
        {
            string baseAppDir = FileSystem.AppDataDirectory;
            string sourceDir = Path.Combine(AppContext.BaseDirectory, "Resources", "Prompts");
            string targetDir = Path.Combine(baseAppDir, "prompts");

            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            await CopyDirectoryAsync(sourceDir, targetDir);
        }
        private static async Task CopyDirectoryAsync(string source, string target)
        {
            foreach (var dirPath in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                string newDirPath = dirPath.Replace(source, target);
                if (!Directory.Exists(newDirPath))
                    Directory.CreateDirectory(newDirPath);
            }

            foreach (var filePath in Directory.GetFiles(source, "*.json", SearchOption.AllDirectories))
            {
                string newFilePath = filePath.Replace(source, target);
                using var sourceStream = File.OpenRead(filePath);
                using var destStream = File.Create(newFilePath);
                await sourceStream.CopyToAsync(destStream);
            }
        }

        // Dictionary to hold the prompt data
        public Dictionary<string, Dictionary<string, List<string>>> PromptData { get; private set; } = new();

        public async Task LoadPromptDataAsync(string baseDir)
        {
            if (!Directory.Exists(baseDir)) throw new DirectoryNotFoundException(baseDir);

            foreach (var categoryDir in Directory.GetDirectories(baseDir))
            {
                string category = Path.GetFileName(categoryDir);
                if (!PromptData.ContainsKey(category))
                    PromptData[category] = new Dictionary<string, List<string>>();

                foreach (var file in Directory.GetFiles(categoryDir, "*.json"))
                {
                    try
                    {
                        string jsonString = await File.ReadAllTextAsync(file);
                        using JsonDocument doc = JsonDocument.Parse(jsonString);
                        JsonElement root = doc.RootElement;

                        foreach (var prop in root.EnumerateObject())
                        {
                            // Skip description and sources keys  
                            if (prop.NameEquals("description") || prop.NameEquals("sources"))
                                continue;

                            // Handle list of strings  
                            if (prop.Value.ValueKind == JsonValueKind.Array)
                            {
                                var list = new List<string>();
                                foreach (var item in prop.Value.EnumerateArray())
                                {
                                    if (item.ValueKind == JsonValueKind.String)
                                    {
                                        string? value = item.GetString();
                                        if (value != null)
                                        {
                                            list.Add(value);
                                        }
                                    }
                                }
                                AddToPromptData(category, prop.Name, list);
                            }
                            // Handle nested objects recursively  
                            else if (prop.Value.ValueKind == JsonValueKind.Object)
                            {
                                FlattenNested(prop.Name, prop.Value, category);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading {file}: {ex.Message}");
                    }
                }
            }
        }

        private void AddToPromptData(string category, string key, List<string> values)
        {
            if (!PromptData[category].ContainsKey(key))
                PromptData[category][key] = new List<string>();
            PromptData[category][key].AddRange(values);
        }

        private void FlattenNested(string parentKey, JsonElement element, string category)
        {
            foreach (var prop in element.EnumerateObject())
            {
                string key = $"{parentKey}.{prop.Name}";

                if (prop.Value.ValueKind == JsonValueKind.Array)
                {
                    var list = new List<string>();
                    foreach (var item in prop.Value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            string? value = item.GetString();
                            if (value != null)
                            {
                                list.Add(value);
                            }
                        }
                    }
                    AddToPromptData(category, key, list);
                }
                else if (prop.Value.ValueKind == JsonValueKind.Object)
                {
                    FlattenNested(key, prop.Value, category);
                }
            }
        }
    }

}
