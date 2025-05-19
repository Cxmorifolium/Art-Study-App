using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace artstudio.Models
{
    public class PromptModel
    {
        private static readonly Random random = new();

        public static string GeneratePrompt(
            Dictionary<string, Dictionary<string, List<string>>> promptData,
            int maxObjects = 5)
        {
            var promptParts = new List<string>();

            // Objects (noun)
            if (promptData.TryGetValue("noun", out var nounDict))
            {
                var allObjects = nounDict.Values.SelectMany(list => list).ToList();
                int takeCount = Math.Min(maxObjects, allObjects.Count);
                var selectedObjects = allObjects.OrderBy(x => random.Next()).Take(takeCount);
                promptParts.Add("Objects: " + string.Join(", ", selectedObjects));
            }

            // Settings
            if (promptData.TryGetValue("setting", out var settingDict) && settingDict.Count > 0)
            {
                var settingCategory = settingDict.Keys.OrderBy(x => random.Next()).First();
                var settingItem = settingDict[settingCategory].OrderBy(x => random.Next()).First();
                promptParts.Add($"Setting ({settingCategory}): {settingItem}");
            }

            // Styles (pick up to 2 categories)
            if (promptData.TryGetValue("style", out var styleDict) && styleDict.Count > 0)
            {
                var styleCategories = styleDict.Keys.OrderBy(x => random.Next()).Take(2);
                foreach (var cat in styleCategories)
                {
                    var styleItem = styleDict[cat].OrderBy(x => random.Next()).First();
                    promptParts.Add($"Style ({cat}): {styleItem}");
                }
            }

            return string.Join(" | ", promptParts);
        }
    }
}
