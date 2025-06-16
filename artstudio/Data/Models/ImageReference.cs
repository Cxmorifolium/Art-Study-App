using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace artstudio.Data.Models
{
    [Table("ImageReference")]
    public class ImageReference
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int UserUploadedImageId { get; set; }

        [NotNull]
        public string ReferenceType { get; set; } = string.Empty; // "StudySession", "PaletteCollection", "WordCollection", "FavoriteSwatch"

        [NotNull]
        public int ReferenceId { get; set; }

        public string? ReferenceDescription { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Display helper
        [Ignore]
        public string ReferenceTypeDisplay => ReferenceType switch
        {
            "StudySession" => "Study Session",
            "PaletteCollection" => "Color Palette",
            "WordCollection" => "Word Prompt",
            "FavoriteSwatch" => "Color Swatch",
            _ => "Unknown"
        };
    }
}
