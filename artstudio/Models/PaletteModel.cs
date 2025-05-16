using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;
using SQLite;

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
                float p = 2 * 1 - q;

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
            Random random = new Random();

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
                generatedPalette.Add(AddRandomVariation(generatedPalette[random!.Next(palette.Count)], randomFactor));

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
            return palette;
        }

        // Randomization of Color but controlled so there's still cohesion
        private Color AddRandomVariation(Color color, float randomFactor)
        {
            ColorToHsl(color, out float h, out float s, out float l);

            // Smaller hue shift
            float hueShift = (float)(random!.NextDouble() * randomFactor * 10 - 5);
            h = (h + hueShift) % 360;
            if (h < 0) h += 360;

            // Randomize saturation and lightness that still relates to original palette
            float satFactor = s > 0.7f ? 0.5f : 1.0f; // Reduce variance for already saturated colors
            float satShift = (float)(random.NextDouble() * 2 - 1) * randomFactor * 0.15f * satFactor;
            s = Math.Clamp(s + satShift, 0.1f, 0.9f);

            float lightFactor = (l < 0.3f || l > 0.7f) ? 0.5f : 1.0f;
            float lightShift = (float)(random.NextDouble() * 2 - 1) * randomFactor * 0.15f * lightFactor;
            l = Math.Clamp(l + lightShift, 0.1f, 0.8f);

            return HslToColor(h, s, l);

        }
        private List<Color> GenerateAnalogousPalette(float h, float s, float l, float randomFactor)
        {
            List<Color> palette = new List<Color>();

            // Angles for Analogous Colors
            float[] angles = { -60, -30, 0, 30, 60 };

            foreach (float angle in angles)
            {
                float newHue = (h + angle) % 360;
                if (newHue < 0) newHue += 360;

                // Variation in s and l 
                float newSat = Math.Clamp(s + ((float)random!.NextDouble() * 2 - 1) * 0.1f, 0.1f, 0.9f);
                float newLight = Math.Clamp(1 + ((float)random.NextDouble() * 2 - 1) * 0.1f, 0.2f, 0.8f);

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
            palette.Add(HslToColor(h, Math.Clamp(s - 0.2f, 0.1f, 0.9f), Math.Clamp(1 + 0.1f, 0.2f, 0.8f)));
            palette.Add(HslToColor(h, Math.Clamp(s + 0.1f, 0.1f, 0.9f), Math.Clamp(1 - 0.2f, 0.2f, 0.8f)));
            palette.Add(HslToColor(complementaryHue, Math.Clamp(s - 0.1f, 0.1f, 0.9f), Math.Clamp(1 + 0.2f, 0.2f, 0.8f)));

            return palette;
        }

        private List<Color> GenerateSplitComplementaryPalette(float h, float s, float l, float randomFactor)
        {
            List<Color> palette = new List<Color>();

            palette.Add(HslToColor(h, s, l));

            // Split complementary 150° and 210° from base color
            float comp1 = (h + 150) % 360;
            float comp2 = (h + 210) % 360;

            palette.Add(HslToColor(comp1, s, l));
            palette.Add(HslToColor(comp2, s, l));

            // Variation
            palette.Add(HslToColor(h, Math.Clamp(s - 0.15f, 0.1f, 0.9f), Math.Clamp(l + 0.15f, 0.2f, 0.8f)));
            palette.Add(HslToColor(h, Math.Clamp(s + 0.15f, 0.1f, 0.9f), Math.Clamp(l - 0.15f, 0.2f, 0.8f)));

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
            palette.Add(HslToColor(h, Math.Clamp(s - 0.2f, 0.1f, 0.9f), Math.Clamp(1 + 0.1f, 0.2f, 0.8f)));
            palette.Add(HslToColor((h + 120) % 360, Math.Clamp(s - 0.1f, 0.1f, 0.9f), Math.Clamp(1 + 0.2f, 0.2f, 0.8f)));

            return palette;
        }

        private List<Color> GenerateSquarePalette(float h, float s, float l, float randomFactor)
        {
            List<Color> palette = new List<Color>();

            // Four colors spaced 90° apart
            for (int i = 0; i < 4; i++)
            {
                float newHue = (h + i * 90) % 360;
                palette.Add(HslToColor(newHue, s, l));
            }

            // Add a variation
            palette.Add(HslToColor(h, Math.Clamp(s - 0.15f, 0.1f, 0.9f), Math.Clamp(l + 0.15f, 0.2f, 0.8f)));

            return palette;
        }

        // Generate monochromatic harmony (same hue, different S/L)
        private List<Color> GenerateMonochromaticPalette(float h, float s, float l, float randomFactor)
        {
            List<Color> palette = new List<Color>();

            palette.Add(HslToColor(h, s, l)); // Base color

            // Vary saturation and lightness
            palette.Add(HslToColor(h, Math.Clamp(s - 0.3f, 0.1f, 0.9f), Math.Clamp(l + 0.1f, 0.2f, 0.8f)));
            palette.Add(HslToColor(h, Math.Clamp(s - 0.15f, 0.1f, 0.9f), Math.Clamp(l + 0.2f, 0.2f, 0.8f)));
            palette.Add(HslToColor(h, Math.Clamp(s + 0.15f, 0.1f, 0.9f), Math.Clamp(l - 0.2f, 0.2f, 0.8f)));
            palette.Add(HslToColor(h, Math.Clamp(s + 0.3f, 0.1f, 0.9f), Math.Clamp(l - 0.1f, 0.2f, 0.8f)));

            return palette;
        }
    }

}
