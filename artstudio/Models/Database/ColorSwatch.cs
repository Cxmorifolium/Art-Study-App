using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace artstudio.Models.Database
{
    public class ColorSwatch
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string ColorHex { get; set; } // #RRGGBB format

        public string Name { get; set; }

        public bool IsFavorite { get; set; }

        public int Hue { get; set; } // For sorting

        public int Saturation { get; set; } // For sorting

        public int Luminosity { get; set; } // For sorting

        public DateTime CreatedAt { get; set; }
    }
}
