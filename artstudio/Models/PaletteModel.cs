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

        private Random _random;

        public PaletteModel()
        {
            _random = new Random();
        }

        // Convert MAUI Color to HSL
        public void ColorToHsl(Color color, out float h, out float s, out float l)
        {
            float r = color.Red;
            float g = color.Green;
            float b = color.Blue;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));

            h = s = l = (max + min) / 2;

            if (max == min)
            {
                h = s = 0; // Achromatic
            }
            else
            {
                float d = max - min;
                s = l > 0.5f ? d / (2 - max - min) : d / (max + min);

                if (max == r)
                    h = (g - b) / d + (g < b ? 6 : 0);
                else if (max == g)
                    h = (b - r) / d + 2;
                else
                    h = (r - g) / d + 4;

                h *= 60;
            }

            if (h < 0) h += 360;
        }

        // Convert HSL back to MAUI Color
        public Color HslToColor(float h, float s, float l)
        {
            float r, g, b;

            if (s == 0)
            {
                r = g = b = l; // Achromatic
            }
            else
            {
                float q = l < 0.5f ? l * (1 + s) : l + s - l * s;
                float p = 2 * l - q;

                r = HueToRgb(p, q, (h / 360f) + 1f / 3f);
                g = HueToRgb(p, q, h / 360f);
                b = HueToRgb(p, q, (h / 360f) - 1f / 3f);
            }

            return new Color(Math.Clamp(r, 0, 1), Math.Clamp(g, 0, 1), Math.Clamp(b, 0, 1));
        }

        private static float HueToRgb(float p, float q, float t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1f / 6f) return p + (q - p) * 6 * t;
            if (t < 1f / 2f) return q;
            if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6;
            return p;
        }

        // Enhanced perceptual color difference calculation
        private float GetPerceptualColorDistance(Color c1, Color c2)
        {
            ColorToHsl(c1, out float h1, out float s1, out float l1);
            ColorToHsl(c2, out float h2, out float s2, out float l2);

            // Calculate hue difference (circular)
            float hueDiff = Math.Abs(h1 - h2);
            if (hueDiff > 180) hueDiff = 360 - hueDiff;

            // Weight the differences based on human perception
            float hueWeight = 2.0f;
            float satWeight = 1.0f;
            float lightWeight = 1.0f;

            // Normalize to 0-100 scale for easier thresholds
            float normalizedHueDiff = (hueDiff / 180f) * 100f;
            float normalizedSatDiff = Math.Abs(s1 - s2) * 100f;
            float normalizedLightDiff = Math.Abs(l1 - l2) * 100f;

            return (normalizedHueDiff * hueWeight) +
                   (normalizedSatDiff * satWeight) +
                   (normalizedLightDiff * lightWeight);
        }

        // Check if two colors are too similar (Coolors uses around 15-20 threshold)
        private bool AreColorsTooSimilar(Color c1, Color c2, float threshold = 18f)
        {
            return GetPerceptualColorDistance(c1, c2) < threshold;
        }

        // Generate a high-quality random color
        private Color GenerateRandomColor()
        {
            // Use golden ratio for better hue distribution
            float goldenAngle = 137.508f;
            float h = (_random.NextSingle() * goldenAngle) % 360f;

            // Constrain saturation and lightness to pleasing ranges
            float s = 0.35f + (_random.NextSingle() * 0.55f); // 35% to 90%
            float l = 0.25f + (_random.NextSingle() * 0.5f);  // 25% to 75%

            return HslToColor(h, s, l);
        }

        // Main palette generation method
        public List<Color> HarmonyPaletteGenerator(
            ColorHarmonyType colorHarmonyType,
            float randomFactor = 0.1f,
            List<Color>? existingPalette = null,
            bool[]? lockedColors = null)
        {
            const int paletteSize = 5;

            // If we have an existing palette with locks, regenerate unlocked colors
            if (existingPalette != null && lockedColors != null &&
                existingPalette.Count == paletteSize && lockedColors.Length == paletteSize)
            {
                return RegenerateWithLocks(existingPalette, lockedColors, colorHarmonyType, randomFactor);
            }

            // Generate completely new palette
            return GenerateNewPalette(colorHarmonyType, randomFactor, paletteSize);
        }

        private List<Color> RegenerateWithLocks(List<Color> existingPalette, bool[] lockedColors,
            ColorHarmonyType harmonyType, float randomFactor)
        {
            var newPalette = new List<Color>(existingPalette);
            var lockedIndices = new List<int>();

            // Identify locked positions
            for (int i = 0; i < lockedColors.Length; i++)
            {
                if (lockedColors[i])
                {
                    lockedIndices.Add(i);
                }
            }

            // Generate new colors for unlocked positions
            for (int i = 0; i < newPalette.Count; i++)
            {
                if (!lockedColors[i])
                {
                    Color newColor;
                    int attempts = 0;
                    const int maxAttempts = 50;

                    do
                    {
                        // Generate color based on harmony type and existing palette context
                        if (lockedIndices.Count > 0)
                        {
                            // Use a locked color as reference for harmony
                            var referenceColor = newPalette[lockedIndices[_random.Next(lockedIndices.Count)]];
                            newColor = GenerateHarmoniousColor(referenceColor, harmonyType, randomFactor, attempts);
                        }
                        else
                        {
                            // No locked colors - generate based on harmony type
                            newColor = GenerateHarmoniousColor(GenerateRandomColor(), harmonyType, randomFactor, attempts);
                        }

                        attempts++;
                    }
                    while (IsColorTooSimilarToExisting(newColor, newPalette, i) && attempts < maxAttempts);

                    newPalette[i] = newColor;
                }
            }

            return newPalette;
        }

        private List<Color> GenerateNewPalette(ColorHarmonyType harmonyType, float randomFactor, int paletteSize)
        {
            var palette = new List<Color>();

            // Start with a base color
            var baseColor = GenerateRandomColor();
            palette.Add(baseColor);

            // Generate harmony colors based on type
            var harmonyColors = GenerateHarmonyColorsFromBase(baseColor, harmonyType, randomFactor);

            // Add harmony colors, ensuring no duplicates
            foreach (var color in harmonyColors)
            {
                if (palette.Count >= paletteSize) break;

                if (!IsColorTooSimilarToExisting(color, palette, -1))
                {
                    palette.Add(color);
                }
            }

            // Fill remaining slots with diverse colors
            while (palette.Count < paletteSize)
            {
                var newColor = GenerateComplementaryColor(palette, harmonyType, randomFactor);
                if (!IsColorTooSimilarToExisting(newColor, palette, -1))
                {
                    palette.Add(newColor);
                }
            }

            return palette;
        }

        private bool IsColorTooSimilarToExisting(Color candidateColor, List<Color> palette, int skipIndex)
        {
            for (int i = 0; i < palette.Count; i++)
            {
                if (i == skipIndex) continue; // Skip the position we're replacing

                if (AreColorsTooSimilar(candidateColor, palette[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private Color GenerateHarmoniousColor(Color referenceColor, ColorHarmonyType harmonyType,
            float randomFactor, int attemptNumber = 0)
        {
            ColorToHsl(referenceColor, out float h, out float s, out float l);

            // Add some variation based on attempt number to avoid infinite loops
            float variation = 1f + (attemptNumber * 0.1f);

            return harmonyType switch
            {
                ColorHarmonyType.Analogous => GenerateAnalogousColor(h, s, l, randomFactor * variation),
                ColorHarmonyType.Complementary => GenerateComplementaryColor(h, s, l, randomFactor * variation),
                ColorHarmonyType.SplitComplementary => GenerateSplitComplementaryColor(h, s, l, randomFactor * variation),
                ColorHarmonyType.Triadic => GenerateTriadicColor(h, s, l, randomFactor * variation),
                ColorHarmonyType.Square => GenerateSquareColor(h, s, l, randomFactor * variation),
                ColorHarmonyType.Monochromatic => GenerateMonochromaticColor(h, s, l, randomFactor * variation),
                _ => GenerateRandomColor()
            };
        }

        private List<Color> GenerateHarmonyColorsFromBase(Color baseColor, ColorHarmonyType harmonyType, float randomFactor)
        {
            ColorToHsl(baseColor, out float h, out float s, out float l);

            return harmonyType switch
            {
                ColorHarmonyType.Analogous => GenerateAnalogousColors(h, s, l, randomFactor),
                ColorHarmonyType.Complementary => GenerateComplementaryColors(h, s, l, randomFactor),
                ColorHarmonyType.SplitComplementary => GenerateSplitComplementaryColors(h, s, l, randomFactor),
                ColorHarmonyType.Triadic => GenerateTriadicColors(h, s, l, randomFactor),
                ColorHarmonyType.Square => GenerateSquareColors(h, s, l, randomFactor),
                ColorHarmonyType.Monochromatic => GenerateMonochromaticColors(h, s, l, randomFactor),
                _ => new List<Color>()
            };
        }

        // Individual harmony color generators
        private Color GenerateAnalogousColor(float h, float s, float l, float randomFactor)
        {
            float hueShift = (-30f + _random.NextSingle() * 60f) * randomFactor;
            float newH = (h + hueShift + 360f) % 360f;
            float newS = Math.Clamp(s + (-0.1f + _random.NextSingle() * 0.2f) * randomFactor, 0.2f, 0.9f);
            float newL = Math.Clamp(l + (-0.1f + _random.NextSingle() * 0.2f) * randomFactor, 0.2f, 0.8f);
            return HslToColor(newH, newS, newL);
        }

        private Color GenerateComplementaryColor(float h, float s, float l, float randomFactor)
        {
            float complementaryH = (h + 180f + (-15f + _random.NextSingle() * 30f) * randomFactor) % 360f;
            float newS = Math.Clamp(s + (-0.2f + _random.NextSingle() * 0.4f) * randomFactor, 0.3f, 0.9f);
            float newL = Math.Clamp(l + (-0.2f + _random.NextSingle() * 0.4f) * randomFactor, 0.2f, 0.8f);
            return HslToColor(complementaryH, newS, newL);
        }

        private Color GenerateSplitComplementaryColor(float h, float s, float l, float randomFactor)
        {
            float[] angles = { 150f, 210f };
            float angle = angles[_random.Next(angles.Length)];
            float newH = (h + angle + (-10f + _random.NextSingle() * 20f) * randomFactor) % 360f;
            float newS = Math.Clamp(s + (-0.15f + _random.NextSingle() * 0.3f) * randomFactor, 0.3f, 0.9f);
            float newL = Math.Clamp(l + (-0.15f + _random.NextSingle() * 0.3f) * randomFactor, 0.2f, 0.8f);
            return HslToColor(newH, newS, newL);
        }

        private Color GenerateTriadicColor(float h, float s, float l, float randomFactor)
        {
            float[] angles = { 120f, 240f };
            float angle = angles[_random.Next(angles.Length)];
            float newH = (h + angle + (-10f + _random.NextSingle() * 20f) * randomFactor) % 360f;
            float newS = Math.Clamp(s + (-0.15f + _random.NextSingle() * 0.3f) * randomFactor, 0.3f, 0.9f);
            float newL = Math.Clamp(l + (-0.15f + _random.NextSingle() * 0.3f) * randomFactor, 0.2f, 0.8f);
            return HslToColor(newH, newS, newL);
        }

        private Color GenerateSquareColor(float h, float s, float l, float randomFactor)
        {
            float[] angles = { 90f, 180f, 270f };
            float angle = angles[_random.Next(angles.Length)];
            float newH = (h + angle + (-10f + _random.NextSingle() * 20f) * randomFactor) % 360f;
            float newS = Math.Clamp(s + (-0.15f + _random.NextSingle() * 0.3f) * randomFactor, 0.3f, 0.9f);
            float newL = Math.Clamp(l + (-0.15f + _random.NextSingle() * 0.3f) * randomFactor, 0.2f, 0.8f);
            return HslToColor(newH, newS, newL);
        }

        private Color GenerateMonochromaticColor(float h, float s, float l, float randomFactor)
        {
            // Keep hue the same, vary saturation and lightness significantly
            float newS = Math.Clamp(s + (-0.3f + _random.NextSingle() * 0.6f) * randomFactor, 0.2f, 0.9f);
            float newL = Math.Clamp(l + (-0.4f + _random.NextSingle() * 0.8f) * randomFactor, 0.15f, 0.85f);
            return HslToColor(h, newS, newL);
        }

        // Generate multiple colors for initial palette creation
        private List<Color> GenerateAnalogousColors(float h, float s, float l, float randomFactor)
        {
            var colors = new List<Color>();
            float[] offsets = { -30f, -15f, 15f, 30f };

            foreach (float offset in offsets)
            {
                float newH = (h + offset) % 360f;
                if (newH < 0) newH += 360f;

                float newS = Math.Clamp(s + (-0.1f + _random.NextSingle() * 0.2f), 0.3f, 0.9f);
                float newL = Math.Clamp(l + (-0.15f + _random.NextSingle() * 0.3f), 0.25f, 0.75f);

                colors.Add(HslToColor(newH, newS, newL));
            }

            return colors;
        }

        private List<Color> GenerateComplementaryColors(float h, float s, float l, float randomFactor)
        {
            var colors = new List<Color>();

            // Main complementary color
            float compH = (h + 180f) % 360f;
            colors.Add(HslToColor(compH, s, l));

            // Variations
            colors.Add(HslToColor(h, Math.Clamp(s - 0.2f, 0.2f, 0.9f), Math.Clamp(l + 0.15f, 0.2f, 0.8f)));
            colors.Add(HslToColor(compH, Math.Clamp(s + 0.1f, 0.2f, 0.9f), Math.Clamp(l - 0.15f, 0.2f, 0.8f)));

            return colors;
        }

        private List<Color> GenerateSplitComplementaryColors(float h, float s, float l, float randomFactor)
        {
            var colors = new List<Color>();

            float comp1 = (h + 150f) % 360f;
            float comp2 = (h + 210f) % 360f;

            colors.Add(HslToColor(comp1, s, l));
            colors.Add(HslToColor(comp2, s, l));
            colors.Add(HslToColor(h, Math.Clamp(s - 0.15f, 0.2f, 0.9f), Math.Clamp(l + 0.1f, 0.2f, 0.8f)));

            return colors;
        }

        private List<Color> GenerateTriadicColors(float h, float s, float l, float randomFactor)
        {
            var colors = new List<Color>();

            float h1 = (h + 120f) % 360f;
            float h2 = (h + 240f) % 360f;

            colors.Add(HslToColor(h1, s, l));
            colors.Add(HslToColor(h2, s, l));
            colors.Add(HslToColor(h, Math.Clamp(s - 0.1f, 0.2f, 0.9f), Math.Clamp(l + 0.1f, 0.2f, 0.8f)));

            return colors;
        }

        private List<Color> GenerateSquareColors(float h, float s, float l, float randomFactor)
        {
            var colors = new List<Color>();

            float[] angles = { 90f, 180f, 270f };
            foreach (float angle in angles)
            {
                float newH = (h + angle) % 360f;
                colors.Add(HslToColor(newH, s, l));
            }

            return colors;
        }

        private List<Color> GenerateMonochromaticColors(float h, float s, float l, float randomFactor)
        {
            var colors = new List<Color>();

            // Dark shade
            colors.Add(HslToColor(h, Math.Clamp(s + 0.2f, 0.3f, 0.9f), Math.Clamp(l - 0.25f, 0.15f, 0.8f)));

            // Light tint  
            colors.Add(HslToColor(h, Math.Clamp(s - 0.2f, 0.2f, 0.9f), Math.Clamp(l + 0.25f, 0.2f, 0.85f)));

            // Medium variations
            colors.Add(HslToColor(h, Math.Clamp(s + 0.1f, 0.2f, 0.9f), Math.Clamp(l - 0.1f, 0.2f, 0.8f)));
            colors.Add(HslToColor(h, Math.Clamp(s - 0.1f, 0.2f, 0.9f), Math.Clamp(l + 0.1f, 0.2f, 0.8f)));

            return colors;
        }

        private Color GenerateComplementaryColor(List<Color> existingPalette, ColorHarmonyType harmonyType, float randomFactor)
        {
            // Generate a color that's different from existing palette
            var referenceColor = existingPalette[_random.Next(existingPalette.Count)];
            return GenerateHarmoniousColor(referenceColor, harmonyType, randomFactor + 0.2f);
        }
    }
}