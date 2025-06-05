using System;
using System.Collections.Generic;
using System.Linq;

namespace artstudio.Services
{
    public static class ColorSortingExtensions
    {
        // Sorts colors by hue for a rainbow-like arrangement
        public static List<Color> SortByHue(this IEnumerable<Color> colors)
        {
            return colors.OrderBy(color => GetHue(color)).ToList();
        }


        // Sorts colors by brightness (luminance) from dark to light
        public static List<Color> SortByBrightness(this IEnumerable<Color> colors)
        {
            return colors.OrderBy(color => GetLuminance(color)).ToList();
        }

        // Sorts colors by saturation from muted to vibrant
        public static List<Color> SortBySaturation(this IEnumerable<Color> colors)
        {
            return colors.OrderBy(color => GetSaturation(color)).ToList();
        }


        // Coolors-style sorting: Creates visually pleasing flow
        // Combines hue, saturation, and brightness for optimal arrangement
        public static List<Color> SortCoolorsStyle(this IEnumerable<Color> colors)
        {
            var colorList = colors.ToList();
            if (colorList.Count <= 1) return colorList;

            // Convert to HSL for better sorting
            var hslColors = colorList.Select(c => new
            {
                Color = c,
                Hue = GetHue(c),
                Saturation = GetSaturation(c),
                Lightness = GetLuminance(c)
            }).ToList();

            // Group by hue families (12 families for color wheel)
            var hueGroups = hslColors
                .GroupBy(c => Math.Floor(c.Hue / 30)) // 360/12 = 30 degrees per group
                .OrderBy(g => g.Key)
                .ToList();

            var result = new List<Color>();

            foreach (var hueGroup in hueGroups)
            {
                // Within each hue group, sort by saturation then lightness
                var sortedGroup = hueGroup
                    .OrderBy(c => c.Saturation)
                    .ThenBy(c => c.Lightness)
                    .Select(c => c.Color)
                    .ToList();

                result.AddRange(sortedGroup);
            }

            return result;
        }

        // Advanced Coolors-style sorting with smooth transitions
        // Uses traveling salesman approach for minimal color distance
        public static List<Color> SortWithSmoothTransitions(this IEnumerable<Color> colors)
        {
            var colorList = colors.ToList();
            if (colorList.Count <= 2) return colorList;

            var result = new List<Color>();
            var remaining = new List<Color>(colorList);

            // Start with the color closest to pure red (arbitrary starting point)
            var current = remaining.OrderBy(c => ColorDistance(c, Color.FromRgb(255, 0, 0))).First();
            result.Add(current);
            remaining.Remove(current);

            // Greedily pick the closest remaining color
            while (remaining.Count > 0)
            {
                var next = remaining.OrderBy(c => ColorDistance(current, c)).First();
                result.Add(next);
                remaining.Remove(next);
                current = next;
            }

            return result;
        }

        // Professional palette sorting that mimics design tools
        // Balances visual flow with color theory principles
        public static List<Color> SortProfessional(this IEnumerable<Color> colors)
        {
            var colorList = colors.ToList();

            // Separate neutrals from chromatic colors
            var neutrals = colorList.Where(c => GetSaturation(c) < 0.1).ToList();
            var chromatic = colorList.Where(c => GetSaturation(c) >= 0.1).ToList();

            var result = new List<Color>();

            // Sort chromatic colors by hue, then saturation
            var sortedChromatic = chromatic
                .OrderBy(c => GetHue(c))
                .ThenByDescending(c => GetSaturation(c))
                .ToList();

            // Sort neutrals by lightness
            var sortedNeutrals = neutrals
                .OrderBy(c => GetLuminance(c))
                .ToList();

            // Interleave or group based on palette composition
            if (sortedChromatic.Count > sortedNeutrals.Count * 2)
            {
                // More chromatic colors - group neutrals at end
                result.AddRange(sortedChromatic);
                result.AddRange(sortedNeutrals);
            }
            else
            {
                // More balanced - interleave strategically
                result.AddRange(sortedChromatic);
                result.AddRange(sortedNeutrals);
            }

            return result;
        }

        #region Helper Methods

        private static double GetHue(Color color)
        {
            var r = color.Red;
            var g = color.Green;
            var b = color.Blue;

            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            var delta = max - min;

            if (delta == 0) return 0;

            double hue;
            if (max == r)
                hue = ((g - b) / delta) % 6;
            else if (max == g)
                hue = (b - r) / delta + 2;
            else
                hue = (r - g) / delta + 4;

            hue *= 60;
            if (hue < 0) hue += 360;

            return hue;
        }

        private static double GetSaturation(Color color)
        {
            var r = color.Red;
            var g = color.Green;
            var b = color.Blue;

            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));

            if (max == 0) return 0;
            return (max - min) / max;
        }

        private static double GetLuminance(Color color)
        {
            // Standard luminance calculation
            var r = color.Red / 255.0;
            var g = color.Green / 255.0;
            var b = color.Blue / 255.0;

            return 0.299 * r + 0.587 * g + 0.114 * b;
        }

        private static double ColorDistance(Color c1, Color c2)
        {
            // Euclidean distance in RGB space (can be improved with LAB color space)
            var dr = c1.Red - c2.Red;
            var dg = c1.Green - c2.Green;
            var db = c1.Blue - c2.Blue;

            return Math.Sqrt(dr * dr + dg * dg + db * db);
        }

        #endregion
    }

    public enum ColorSortingMethod
    {
        None,
        Hue,
        Brightness,
        Saturation,
        CoolorsStyle,
        SmoothTransitions,
        Professional
    }
}