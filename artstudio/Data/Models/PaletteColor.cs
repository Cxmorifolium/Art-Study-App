using SQLite;

namespace artstudio.Data.Models
{
    [Table("PaletteColor")]
    public class PaletteColor
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public int PaletteCollectionId { get; set; }

        [NotNull]
        public string HexColor { get; set; } = string.Empty;

        public int Position { get; set; } // Order in the palette

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Helper property
        [Ignore]
        public Color Color
        {
            get => Color.FromArgb(HexColor);
            set => HexColor = value.ToArgbHex();
        }
    }
}