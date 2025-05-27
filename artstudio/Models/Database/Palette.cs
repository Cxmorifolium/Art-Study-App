using SQLite;

namespace artstudio.Models.Database
{
    public class Palette
    {

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        public bool IsFavorite { get; set; }

        public bool IsDefault { get; set; }

        public string? ColorsJson { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}

