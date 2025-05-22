using SQLite;

namespace artstudio.Models.Database
{
    public class SwatchCollectionMap
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int SwatchId { get; set; }

        public int CollectionId { get; set; }
    }
}
