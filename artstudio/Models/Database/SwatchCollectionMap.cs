using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
