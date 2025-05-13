using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Skia;

namespace artstudio.Services
{
    public class Export
    {
        private readonly IFileSaveService _fileSaveService;

        public Export(IFileSaveService fileSaveService)
        {
            _fileSaveService = fileSaveService ?? throw new ArgumentNullException(nameof(fileSaveService));
        }

        public async Task ExportPaletteAsync(IList<Color> colors)
        {
            if (colors == null || colors.Count == 0)
                throw new ArgumentException("No colors to export.");

            var imageBytes = await GeneratePaletteImageAsync(colors);
            string fileName = $"palette_{DateTime.Now:yyyyMMdd_HHmmss}.png";

            // Use the injected platform-specific service
            await _fileSaveService.SaveFileAsync(imageBytes, fileName);
        }

        public static Task<byte[]> GeneratePaletteImageAsync(IList<Color> colors)
        {
            const int swatchWidth = 200;
            const int swatchHeight = 200;
            int totalWidth = swatchWidth * colors.Count;
            const int totalHeight = swatchHeight;

            var image = new SkiaBitmapExportContext(totalWidth, totalHeight, 1.0f);
            var canvas = image.Canvas;

            for (int i = 0; i < colors.Count; i++)
            {
                var rect = new Rect(i * swatchWidth, 0, swatchWidth, swatchHeight);
                canvas.FillColor = colors[i];
                canvas.FillRectangle(rect);
            }

            using var stream = new MemoryStream();
            image.WriteToStream(stream);
            return Task.FromResult(stream.ToArray());
        }
    }
}
