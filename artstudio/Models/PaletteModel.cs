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
            Tetradic,
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
            float s = (float)(random.NextDouble());
            float l = (float)(random.NextDouble());

            return HslToColor(h, s, l);
        }


        public List<Color> HarmonyPaletteGenerator(
            ColorHarmonyType colorHarmonyType,
            float randomFactor = 0.1f,
            List<Color>? existingPalette = null,
            bool[]? lockedColors = null)
        {
            List<Color> palette = new List<Color>();

            Color baseColor = GenerateRandomColor();

            ColorToHsl(baseColor, out float h, out float s, out float l);

            List<Color> generatedPalette = new List<Color>();

            switch (colorHarmonyType)
            {
                case ColorHarmonyType.Analogous:
                    generatedPalette = GenerateAnalogousPalette(h, s, l, randomFactor);
                    break;
                case ColorHarmonyType.Complementary:
                    generatedPalette = GenerateComplementaryPalette(h, s, l, randomFactor);
                    break;
                case ColorHarmonyType.SplitComplementary:
                    generatedPalette = GenerateSplitComplementaryPalette(h, s, l, randomFactor);
                    break;
                case ColorHarmonyType.Triadic:
                    generatedPalette = GenerateTriadicPalette(h, s, l, randomFactor);
                    break;
                case ColorHarmonyType.Square:
                    generatedPalette = GenerateSquarePalette(h, s, l, randomFactor);
                    break;
                case ColorHarmonyType.Monochromatic:
                    generatedPalette = GenerateMonochromaticPalette(h, s, l, randomFactor);
                    break;
            }

            generatedPalette.Insert(0, baseColor);

            while (generatedPalette.Count > 5)
                generatedPalette.RemoveAt(generatedPalette.Count - 1);

            while (generatedPalette.Count < 5)
                generatedPalette.Add(AddRandomVariation(
                    generatedPalette[random!.Next(generatedPalette.Count)], randomFactor));

            // Duplicate handling
            for (int i = 0; i < generatedPalette.Count; i++)
            {
                for (int j = i + 1; j < generatedPalette.Count; j++)
                {
                    if (DuplicateColors(generatedPalette[i], generatedPalette[j]))
                    {
                        generatedPalette[j] = AddRandomVariation(generatedPalette[j], randomFactor);
                        i = -1; // Restart checking from the beginning
                        break; // Exit inner loop to recheck from the start
                    }
                }
            }

            if (existingPalette != null && lockedColors != null && existingPalette.Count == 5 && lockedColors.Length == 5)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (lockedColors[i])
                    {
                        // Keep the locked color
                        palette.Add(existingPalette[i]);
                    }
                    else
                    {
                        // Use the new generated color
                        palette.Add(generatedPalette[i]);
                    }
                }
            }
            else
            {
                // Just use the generated palette if no locks are provided
                palette = generatedPalette;
            }
            RemoveDuplicateColors(palette, randomFactor);
            return palette;
        }

        // Helper method to check if colors are too similar
        private bool DuplicateColors(Color c1, Color c2, float threshold = 10f)
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

        // Helper method to check against lock conditions
        private void RemoveDuplicateColors(List<Color> palette, float randomFactor, float threshold = 10f)
        {
            bool duplicatesFound;

            do
            {
                duplicatesFound = false;

                for (int i = 0; i < palette.Count; i++)
                {
                    for (int j = i + 1; j < palette.Count; j++)
                    {
                        if (DuplicateColors(palette[i], palette[j], threshold))
                        {
                            palette[j] = AddRandomVariation(palette[j], randomFactor);
                            duplicatesFound = true;
                        }
                    }
                }
            }
            while (duplicatesFound);
        }

        // Randomization of Color but controlled so there's still cohesion
        private Color AddRandomVariation(Color color, float randomFactor)
        {
            ColorToHsl(color, out float h, out float s, out float l);

            float hueShift = (float)(random!.NextDouble() * 2 - 1) * randomFactor * 8; // smaller range
            h = (h + hueShift + 360) % 360;

            float satShift = (float)(random.NextDouble() * 2 - 1) * randomFactor * 0.08f;
            s = Math.Clamp(s + satShift, 0.2f, 0.9f);

            float lightShift = (float)(random.NextDouble() * 2 - 1) * randomFactor * 0.08f;
            l = Math.Clamp(l + lightShift, 0.2f, 0.85f);

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

            palette.Add(HslToColor(h, s, l));

            // Split complementary 150° and 210° from base color
            float compHue = (h + 180) % 360;
            //float comp1 = (h + 150) % 360;
            //float comp2 = (h + 210) % 360;
            // Base color and its complement
            palette.Add(HslToColor(h, s, l));
            palette.Add(HslToColor(compHue, s, l));

            //palette.Add(HslToColor(comp1, s, l));
            //palette.Add(HslToColor(comp2, s, l));

            // Variation
            //palette.Add(HslToColor(h, Math.Clamp(s - 0.15f, 0.1f, 0.9f), Math.Clamp(l + 0.15f, 0.2f, 0.8f)));
            //palette.Add(HslToColor(h, Math.Clamp(s + 0.15f, 0.1f, 0.9f), Math.Clamp(l - 0.15f, 0.2f, 0.8f)));
            palette.Add(HslToColor(h, Math.Clamp(s + 0.1f, 0.1f, 0.9f), Math.Clamp(l - 0.1f, 0.2f, 0.8f)));
            palette.Add(HslToColor(compHue, Math.Clamp(s - 0.1f, 0.1f, 0.9f), Math.Clamp(l + 0.1f, 0.2f, 0.8f)));
            float midHue = (h + 90) % 360;
            palette.Add(HslToColor(midHue, Math.Clamp(s * 0.9f, 0.1f, 0.9f), Math.Clamp(l * 1.1f, 0.2f, 0.8f)));

            return palette;
        }

        private List<Color> GenerateTriadicPalette(float h, float s, float l, float randomFactor)
        {
            List<Color> palette = new List<Color>();

            // Three Colors spaced apart 120°
            for (int i = 0; i < 3; i++)
            {
                float newHue = (h + i * 120) % 360;
                palette.Add(HslToColor(newHue, s, l));
            }

            // Variations
            palette.Add(HslToColor(h, Math.Clamp(s - 0.2f, 0.1f, 0.9f), Math.Clamp(l + 0.1f, 0.2f, 0.8f)));
            palette.Add(HslToColor((h + 120) % 360, Math.Clamp(s - 0.1f, 0.1f, 0.9f), Math.Clamp(l + 0.2f, 0.2f, 0.8f)));

            return palette;
        }

        private List<Color> GenerateSquarePalette(float h, float s, float l, float randomFactor)
        {
            List<Color> palette = new List<Color>()
                ;
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

            // Add a variation based on the previous colors
            int anchorIndex = random.Next(4);
            Color anchorColor = palette[anchorIndex];
            Color variedColor = AddRandomVariation(anchorColor, randomFactor * 1.5f);
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
