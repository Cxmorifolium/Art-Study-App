using SQLite;

namespace artstudio.Models.Database
{
    public class SwatchCollection
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
