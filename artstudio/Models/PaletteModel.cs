namespace artstudio.Models
{
    public class PaletteModel
    {
        public enum ColorHarmonyType
        {
            Analogous,
            Complementary,
            SplitComplementary,
            Triadic,
            Square,
            Monochromatic
        }

        private Random? random;

        public PaletteModel()
        {
            random = new Random();
        }

        // Converting MAUI ARGB to HSL values
        public void ColorToHsl(Color color, out float h, out float s, out float l)
        {
            // Extract RGB 
            float r = color.Red;
            float g = color.Green;
            float b = color.Blue;

            // Find Min and Max of RGB values
            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));

            // Calculating light "l" as avg of max and min
            h = s = l = (max + min) / 2;

            //Achromatic
            if (max == min)
            {
                h = s = 0;
            }
            else
            {
                // Saturation
                float d = max - min;

                // If light is > 0.5 = brighter ; else darker
                s = l > 0.5f ? d / (2 - max - min) : d / (max + min);

                if (max == r)
                {
                    h = (g - b) / d + (g < b ? 6 : 0); //Red
                }
                else if (max == g)
                {
                    h = (b - r) / d + 2; // Green 
                }
                else
                {
                    h = (r - g) / d + 4; // Blue
                }

                h *= 60; // Convert to degrees
                /* 
                    * 0° = red
                    * 60° = yellow
                    * 120° = green
                    * 180° = cyan
                    * 240° = blue
                    * 300° = magenta
                    * 360° = red
                */
            }

            if (h < 0) h += 360;
        }

        // Convert back to MAUI color
        public Color HslToColor(float h, float s, float l)
        {
            float r, g, b;

            if (s == 0)
            {
                r = g = b = l; // Achromatic
            }
            else
            {
                float q = l < 0.5f ? l * (l + s) : l + s - l * s;
                float p = 2 * l - q;

                // RGB 120° each
                r = HueToRgb(p, q, h / 360f + 1 / 3f); // Red section
                g = HueToRgb(p, q, h / 360f); // Green section
                b = HueToRgb(p, q, h / 360f - 1 / 3f); // Blue section
            }
            return new Color(r, g, b);

        }

        // Helper method for HsltoColor to interpolate
        // t = hue position
        // q = upper bound "light"
        // p = lower bound "shadow"
        public static float HueToRgb(float p, float q, float t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1 / 6f) return p + (q - p) * 6 * t;
            if (t < 1 / 2F) return q;
            if (t < 2 / 3f) return p + (q - p) * (2 / 3f - t) * 6;
            return p;
        }

        private Color GenerateRandomColor()
        {
            if (random == null)
            {
                random = new Random();
            }

            float h = (float)(random.NextDouble() * 360.0);
            // Generate random saturation and lightness within a reasonable range
            float s = (float)(0.4 + random.NextDouble() * 0.5); // 0.4 to 0.9
            float l = (float)(0.3 + random.NextDouble() * 0.4); // 0.3 to 0.7

            return HslToColor(h, s, l);
        }

        public List<Color> HarmonyPaletteGenerator(
                            ColorHarmonyType colorHarmonyType,
                            float randomFactor = 0.1f,
                            List<Color>? existingPalette = null,
                            bool[]? lockedColors = null)
        {
            List<Color> palette = new List<Color>();

            // Handle locked colors case
            if (existingPalette != null && lockedColors != null && existingPalette.Count == 5 && lockedColors.Length == 5)
            {
                // Start with the existing palette
                palette = new List<Color>(existingPalette);

                // Get locked colors for reference
                List<Color> lockedColorsList = GetLockedColors(existingPalette, lockedColors);

                // If we have locked colors, use one of them as the base for harmony generation
                Color baseColor;
                if (lockedColorsList.Count > 0)
                {
                    // Use the first locked color as our harmony base
                    baseColor = lockedColorsList[0];
                }
                else
                {
                    // No locked colors, generate a new base that's different from existing
                    baseColor = GenerateBaseColorAvoidingExisting(existingPalette);
                }

                ColorToHsl(baseColor, out float h, out float s, out float l);

                // Generate harmony colors based on the base color
                List<Color> harmonyColors = GenerateHarmonyColors(colorHarmonyType, h, s, l, randomFactor);

                // Now intelligently replace non-locked colors
                int harmonyIndex = 0;
                for (int i = 0; i < 5; i++)
                {
                    if (!lockedColors[i])
                    {
                        // This position needs a new color
                        Color newColor;

                        if (harmonyIndex < harmonyColors.Count)
                        {
                            // Try to use a harmony color
                            newColor = harmonyColors[harmonyIndex];
                            harmonyIndex++;
                        }
                        else
                        {
                            // Generate additional color using theory
                            Color referenceColor = lockedColorsList.Count > 0 ? lockedColorsList[0] : baseColor;
                            newColor = GenerateColorFromTheory(referenceColor, colorHarmonyType, randomFactor);
                        }

                        // Ensure this new color doesn't duplicate any existing colors
                        List<Color> allExistingColors = new List<Color>();
                        for (int j = 0; j < palette.Count; j++)
                        {
                            if (j != i) // Don't include the position we're replacing
                            {
                                allExistingColors.Add(palette[j]);
                            }
                        }

                        newColor = GenerateNonDuplicateColor(newColor, allExistingColors, colorHarmonyType, randomFactor);
                        palette[i] = newColor;
                    }
                }

                // Final cleanup - ensure no duplicates while respecting locks
                RemoveDuplicateColorsWithLocks(palette, lockedColors, colorHarmonyType, randomFactor);
            }
            else
            {
                // No locks - use original logic
                Color baseColor = GenerateRandomColor();
                ColorToHsl(baseColor, out float h, out float s, out float l);

                List<Color> generatedPalette = GenerateHarmonyColors(colorHarmonyType, h, s, l, randomFactor);
                generatedPalette.Insert(0, baseColor);

                while (generatedPalette.Count > 5)
                    generatedPalette.RemoveAt(generatedPalette.Count - 1);

                while (generatedPalette.Count < 5)
                    generatedPalette.Add(AddRandomTheory(
                        generatedPalette[random!.Next(generatedPalette.Count)], colorHarmonyType, randomFactor));

                palette = generatedPalette;
                RemoveDuplicateColors(palette, colorHarmonyType, randomFactor);
            }

            return palette;
        }

        // Generate harmony colors based on type
        private List<Color> GenerateHarmonyColors(ColorHarmonyType colorHarmonyType, float h, float s, float l, float randomFactor)
        {
            return colorHarmonyType switch
            {
                ColorHarmonyType.Analogous => GenerateAnalogousPalette(h, s, l, randomFactor),
                ColorHarmonyType.Complementary => GenerateComplementaryPalette(h, s, l, randomFactor),
                ColorHarmonyType.SplitComplementary => GenerateSplitComplementaryPalette(h, s, l, randomFactor),
                ColorHarmonyType.Triadic => GenerateTriadicPalette(h, s, l, randomFactor),
                ColorHarmonyType.Square => GenerateSquarePalette(h, s, l, randomFactor),
                ColorHarmonyType.Monochromatic => GenerateMonochromaticPalette(h, s, l, randomFactor),
                _ => new List<Color>()
            };
        }
        private Color GenerateReplacementColor(List<Color> palette, int replaceIndex, bool[] lockedColors, ColorHarmonyType theory, float randomFactor, int attempt)
        {
            // Get all colors to avoid (all current palette colors except the one being replaced)
            List<Color> avoidColors = new List<Color>();
            for (int i = 0; i < palette.Count; i++)
            {
                if (i != replaceIndex)
                {
                    avoidColors.Add(palette[i]);
                }
            }

            // Find a locked color to use as reference for harmony
            Color referenceColor = null;
            for (int i = 0; i < lockedColors.Length; i++)
            {
                if (lockedColors[i])
                {
                    referenceColor = palette[i];
                    break;
                }
            }

            // If no locked color found, use a different color from palette as reference
            if (referenceColor == null)
            {
                referenceColor = palette[replaceIndex == 0 ? 1 : 0];
            }

            // Generate new color based on theory and reference
            Color newColor = GenerateColorFromTheory(referenceColor, theory, randomFactor * (1 + attempt * 0.15f));

            // Ensure it doesn't duplicate existing colors
            return GenerateNonDuplicateColor(newColor, avoidColors, theory, randomFactor * (1 + attempt * 0.1f), 15);
        }
        // Generate a color that doesn't duplicate any in the avoid list
        private Color GenerateNonDuplicateColor(Color startingColor, List<Color> avoidColors, ColorHarmonyType theory, float randomFactor, int maxAttempts = 15)
        {
            Color candidate = startingColor;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                bool isDuplicate = false;

                foreach (Color avoidColor in avoidColors)
                {
                    if (DuplicateColors(candidate, avoidColor))
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (!isDuplicate)
                {
                    return candidate;
                }

                // Generate progressively more different variations
                if (attempt < maxAttempts / 3)
                {
                    // First third: small theory-based changes
                    candidate = GenerateColorFromTheory(startingColor, theory, randomFactor * (1 + attempt * 0.2f));
                }
                else if (attempt < (maxAttempts * 2) / 3)
                {
                    // Second third: medium changes with different theory
                    var alternativeTheories = new[] { ColorHarmonyType.Analogous, ColorHarmonyType.Complementary, ColorHarmonyType.Monochromatic };
                    var altTheory = alternativeTheories[attempt % alternativeTheories.Length];
                    candidate = GenerateColorFromTheory(startingColor, altTheory, randomFactor * (1 + attempt * 0.3f));
                }
                else
                {
                    // Final third: dramatic changes
                    ColorToHsl(startingColor, out float h, out float s, out float l);

                    float hueShift = (float)(random!.NextDouble() * 120 + 60); // 60-180 degree shift
                    if (random.NextDouble() > 0.5) hueShift = -hueShift;

                    float newH = (h + hueShift + 360) % 360;
                    float newS = Math.Clamp(s + ((float)random.NextDouble() * 2 - 1) * 0.4f, 0.2f, 0.9f);
                    float newL = Math.Clamp(l + ((float)random.NextDouble() * 2 - 1) * 0.4f, 0.2f, 0.8f);

                    candidate = HslToColor(newH, newS, newL);
                }
            }

            // Last resort: generate a completely random color
            return GenerateRandomColor();
        }
        private Color GenerateBaseColorAvoidingExisting(List<Color> existingPalette, int maxAttempts = 15)
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Color candidate = GenerateRandomColor();
                bool isDuplicate = false;

                foreach (Color existingColor in existingPalette)
                {
                    if (DuplicateColors(candidate, existingColor))
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (!isDuplicate)
                {
                    return candidate;
                }
            }

            // If we still can't find a unique color, create one dramatically different from the first existing color
            Color baseColor = existingPalette[0];
            ColorToHsl(baseColor, out float h, out float s, out float l);

            // Create a dramatically different color
            float newH = (h + 180 + (float)(random!.NextDouble() * 60 - 30)) % 360;
            float newS = s > 0.5f ? Math.Max(0.2f, s - 0.4f) : Math.Min(0.9f, s + 0.4f);
            float newL = l > 0.5f ? Math.Max(0.2f, l - 0.3f) : Math.Min(0.8f, l + 0.3f);

            return HslToColor(newH, newS, newL);
        }
        // Helper method to check if colors are too similar
        private bool DuplicateColors(Color c1, Color c2, float threshold = 12f) // Slightly increased threshold
        {
            ColorToHsl(c1, out float h1, out float s1, out float l1);
            ColorToHsl(c2, out float h2, out float s2, out float l2);

            float hueDiff = Math.Min(Math.Abs(h1 - h2), 360 - Math.Abs(h1 - h2));
            float satDiff = Math.Abs(s1 - s2) * 100;
            float lightDiff = Math.Abs(l1 - l2) * 100;

            // Adjust these weights as needed
            float hueWeight = 1.5f;
            float satWeight = 1.0f;
            float lightWeight = 0.8f;

            float totalDiff =
                (hueDiff * hueWeight) +
                (satDiff * satWeight) +
                (lightDiff * lightWeight);

            return totalDiff < threshold;
        }

        private void RemoveDuplicateColorsWithLocks(List<Color> palette, bool[] lockedColors, ColorHarmonyType currentTheory, float randomFactor, float threshold = 15f)
        {
            bool duplicatesFound;
            int maxIterations = 25; // Increased iterations
            int iterations = 0;

            do
            {
                duplicatesFound = false;
                iterations++;

                // Check each pair of colors
                for (int i = 0; i < palette.Count && !duplicatesFound; i++)
                {
                    for (int j = i + 1; j < palette.Count && !duplicatesFound; j++)
                    {
                        if (DuplicateColors(palette[i], palette[j], threshold))
                        {
                            // Found duplicates - determine which to change
                            if (lockedColors[i] && lockedColors[j])
                            {
                                // Both locked - can't fix this, but let's try increasing threshold temporarily
                                continue;
                            }
                            else if (lockedColors[i])
                            {
                                // Change j (i is locked)
                                palette[j] = GenerateReplacementColor(palette, j, lockedColors, currentTheory, randomFactor, iterations);
                                duplicatesFound = true;
                            }
                            else if (lockedColors[j])
                            {
                                // Change i (j is locked)
                                palette[i] = GenerateReplacementColor(palette, i, lockedColors, currentTheory, randomFactor, iterations);
                                duplicatesFound = true;
                            }
                            else
                            {
                                // Neither locked - change the later one (j)
                                palette[j] = GenerateReplacementColor(palette, j, lockedColors, currentTheory, randomFactor, iterations);
                                duplicatesFound = true;
                            }
                        }
                    }
                }
            }
            while (duplicatesFound && iterations < maxIterations);
        }

        // Get list of all locked colors - updated to work with existing palette
        private List<Color> GetLockedColors(List<Color> existingPalette, bool[] lockedColors)
        {
            List<Color> lockedColorsList = new List<Color>();
            for (int i = 0; i < Math.Min(existingPalette.Count, lockedColors.Length); i++)
            {
                if (lockedColors[i])
                {
                    lockedColorsList.Add(existingPalette[i]);
                }
            }
            return lockedColorsList;
        }

        // Duplicate removal method (for when no locks are present)
        private void RemoveDuplicateColors(List<Color> palette, ColorHarmonyType currentTheory, float randomFactor, float threshold = 12f)
        {
            bool duplicatesFound;
            int maxIterations = 20;
            int iterations = 0;

            do
            {
                duplicatesFound = false;
                iterations++;

                for (int i = 0; i < palette.Count; i++)
                {
                    for (int j = i + 1; j < palette.Count; j++)
                    {
                        if (DuplicateColors(palette[i], palette[j], threshold))
                        {
                            palette[j] = AddRandomTheory(palette[j], currentTheory, randomFactor * (1 + iterations * 0.1f));
                            duplicatesFound = true;
                        }
                    }
                }
            }
            while (duplicatesFound && iterations < maxIterations);
        }


        // Compliment theories less than 3 colors by merging other color harmonies
        private Color AddRandomTheory(Color baseColor, ColorHarmonyType currentTheory, float randomFactor)
        {
            // Define compatible theory combinations
            var theoryCompatibility = new Dictionary<ColorHarmonyType, List<ColorHarmonyType>>
            {
                [ColorHarmonyType.Triadic] = new List<ColorHarmonyType>
            { ColorHarmonyType.Analogous, ColorHarmonyType.Monochromatic, ColorHarmonyType.Complementary },
                [ColorHarmonyType.Complementary] = new List<ColorHarmonyType>
            { ColorHarmonyType.Monochromatic, ColorHarmonyType.Analogous },
                [ColorHarmonyType.Analogous] = new List<ColorHarmonyType>
            { ColorHarmonyType.Monochromatic, ColorHarmonyType.Complementary },
                [ColorHarmonyType.SplitComplementary] = new List<ColorHarmonyType>
            { ColorHarmonyType.Analogous, ColorHarmonyType.Monochromatic },
                [ColorHarmonyType.Square] = new List<ColorHarmonyType>
            { ColorHarmonyType.Monochromatic, ColorHarmonyType.Analogous },
                [ColorHarmonyType.Monochromatic] = new List<ColorHarmonyType>
            { ColorHarmonyType.Analogous, ColorHarmonyType.Complementary }
            };

            // Get compatible theories for the current theory
            var compatibleTheories = theoryCompatibility[currentTheory];
            var secondaryTheory = compatibleTheories[random!.Next(compatibleTheories.Count)];

            // Generate variation based on the secondary theory
            return GenerateColorFromTheory(baseColor, secondaryTheory, randomFactor);
        }

        private Color GenerateColorFromTheory(Color baseColor, ColorHarmonyType theory, float randomFactor)
        {
            ColorToHsl(baseColor, out float h, out float s, out float l);

            switch (theory)
            {
                case ColorHarmonyType.Analogous:
                    // Small hue shift (±30°)
                    float analogousShift = (float)(random!.NextDouble() * 60 - 30) * randomFactor;
                    h = (h + analogousShift + 360) % 360;
                    // Slight saturation/lightness variation
                    s = Math.Clamp(s + ((float)random.NextDouble() * 2 - 1) * 0.1f * randomFactor, 0.2f, 0.9f);
                    l = Math.Clamp(l + ((float)random.NextDouble() * 2 - 1) * 0.1f * randomFactor, 0.2f, 0.85f);
                    break;

                case ColorHarmonyType.Complementary:
                    // Move toward complementary (180°) with some variation
                    float complementaryHue = (h + 180) % 360;
                    float complementaryShift = (float)(random!.NextDouble() * 40 - 20) * randomFactor; // ±20° variation
                    h = (complementaryHue + complementaryShift + 360) % 360;
                    // Adjust saturation/lightness for contrast
                    s = Math.Clamp(s + ((float)random.NextDouble() * 2 - 1) * 0.15f * randomFactor, 0.3f, 0.9f);
                    l = Math.Clamp(l + ((float)random.NextDouble() * 2 - 1) * 0.2f * randomFactor, 0.2f, 0.8f);
                    break;

                case ColorHarmonyType.Monochromatic:
                    // Keep same hue, vary saturation and lightness significantly
                    float satVariation = (float)(random!.NextDouble() * 2 - 1) * 0.3f * randomFactor;
                    float lightVariation = (float)(random.NextDouble() * 2 - 1) * 0.4f * randomFactor;
                    s = Math.Clamp(s + satVariation, 0.2f, 0.95f);
                    l = Math.Clamp(l + lightVariation, 0.15f, 0.9f);
                    break;

                default:
                    // Fallback to original AddRandomVariation logic
                    float hueShift = (float)(random!.NextDouble() * 2 - 1) * randomFactor * 8;
                    h = (h + hueShift + 360) % 360;
                    s = Math.Clamp(s + ((float)random.NextDouble() * 2 - 1) * randomFactor * 0.08f, 0.2f, 0.9f);
                    l = Math.Clamp(l + ((float)random.NextDouble() * 2 - 1) * randomFactor * 0.08f, 0.2f, 0.85f);
                    break;
            }

            return HslToColor(h, s, l);
        }

        private List<Color> GenerateAnalogousPalette(float h, float s, float l, float randomFactor)
        {
            List<Color> palette = new List<Color>();

            // Angles for Analogous Colors
            float[] angles = { -45, -22.5f, 0, 22.5f, 45 };

            foreach (float angle in angles)
            {
                float newHue = (h + angle) % 360;
                if (newHue < 0) newHue += 360;

                // Variation in s and l 
                float newSat = Math.Clamp(s + ((float)random!.NextDouble() * 2 - 1) * 0.1f, 0.4f, 0.9f);
                float newLight = Math.Clamp(l + ((float)random.NextDouble() * 2 - 1) * 0.15f, 0.3f, 0.7f);

                palette.Add(HslToColor(newHue, newSat, newLight));
            }

            return palette;
        }

        private List<Color> GenerateComplementaryPalette(float h, float s, float l, float randomFactor)
        {
            List<Color> palette = new List<Color>();

            // Start with base color
            palette.Add(HslToColor(h, s, l));

            // Complementary hue
            float complementaryHue = (h + 180) % 360;
            palette.Add(HslToColor(complementaryHue, s, l));

            // Variation of those colors
            palette.Add(HslToColor(h, Math.Clamp(s - 0.2f, 0.1f, 0.9f), Math.Clamp(l + 0.1f, 0.2f, 0.8f)));
            palette.Add(HslToColor(h, Math.Clamp(s + 0.1f, 0.1f, 0.9f), Math.Clamp(l - 0.2f, 0.2f, 0.8f)));
            palette.Add(HslToColor(complementaryHue, Math.Clamp(s - 0.1f, 0.1f, 0.9f), Math.Clamp(l + 0.2f, 0.2f, 0.8f)));

            return palette;
        }

        private List<Color> GenerateSplitComplementaryPalette(float h, float s, float l, float randomFactor)
        {
            List<Color> palette = new List<Color>();

            // Base color
            Color baseColor = HslToColor(h, s, l);
            palette.Add(baseColor);

            // Split complementary colors (150° and 210° from base)
            float comp1 = (h + 150) % 360;
            float comp2 = (h + 210) % 360;

            palette.Add(HslToColor(comp1, s, l));
            palette.Add(HslToColor(comp2, s, l));

            // Add theory-based variations for remaining slots
            palette.Add(AddRandomTheory(baseColor, ColorHarmonyType.SplitComplementary, randomFactor));
            palette.Add(AddRandomTheory(palette[1], ColorHarmonyType.SplitComplementary, randomFactor));

            return palette;
        }

        private List<Color> GenerateTriadicPalette(float h, float s, float l, float randomFactor)
        {
            List<Color> palette = new List<Color>();

            // Three colors spaced apart 120°
            for (int i = 0; i < 3; i++)
            {
                float newHue = (h + i * 120) % 360;
                palette.Add(HslToColor(newHue, s, l));
            }

            // Add theory-based variations for remaining slots
            palette.Add(AddRandomTheory(palette[0], ColorHarmonyType.Triadic, randomFactor));
            palette.Add(AddRandomTheory(palette[1], ColorHarmonyType.Triadic, randomFactor));

            return palette;
        }

        private List<Color> GenerateSquarePalette(float h, float s, float l, float randomFactor)
        {
            List<Color> palette = new List<Color>();
            if (random == null)
            {
                random = new Random();
            }

            // Four colors spaced 90° apart
            for (int i = 0; i < 4; i++)
            {
                float newHue = (h + i * 90) % 360;
                float newS = Math.Clamp(s + ((float)random!.NextDouble() * 2 - 1) * 0.1f, 0.4f, 0.9f);
                float newL = Math.Clamp(l + ((float)random.NextDouble() * 2 - 1) * 0.1f, 0.3f, 0.7f);

                palette.Add(HslToColor(newHue, newS, newL));
            }

            // Add a variation based on the previous colors using theory-based approach
            int anchorIndex = random.Next(4);
            Color anchorColor = palette[anchorIndex];
            Color variedColor = AddRandomTheory(anchorColor, ColorHarmonyType.Square, randomFactor * 1.5f);
            palette.Add(variedColor);

            return palette;
        }

        // Generate monochromatic harmony (same hue, different S/L)
        private List<Color> GenerateMonochromaticPalette(float h, float s, float l, float randomFactor)
        {
            List<Color> palette = new List<Color>();

            palette.Add(HslToColor(h, s, l)); // Base

            // Deep shade (darker and more saturated)
            palette.Add(HslToColor(h, Math.Clamp(s + 0.2f, 0.1f, 0.9f), Math.Clamp(l - 0.25f, 0.2f, 0.8f)));

            // Tint (lighter and less saturated)
            palette.Add(HslToColor(h, Math.Clamp(s - 0.2f, 0.1f, 0.9f), Math.Clamp(l + 0.25f, 0.2f, 0.8f)));

            // Mid-range shade (slightly darker)
            palette.Add(HslToColor(h, Math.Clamp(s + 0.1f, 0.1f, 0.9f), Math.Clamp(l - 0.1f, 0.2f, 0.8f)));

            // Mid-range tint (slightly lighter)
            palette.Add(HslToColor(h, Math.Clamp(s - 0.1f, 0.1f, 0.9f), Math.Clamp(l + 0.1f, 0.2f, 0.8f)));

            return palette;
        }
    }
}