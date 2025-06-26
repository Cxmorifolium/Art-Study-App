using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace artstudio.Data
{
    [Table("FavoriteImageItem")]
    public class FavoriteImageItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [NotNull]
        public string UnsplashId { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? UserName { get; set; }
        public string? UserPortfolioUrl { get; set; }
        public string? LocalImagePath { get; set; }
        public string? OriginalUrl { get; set; }
        [NotNull]
        public DateTime CreatedAt { get; set; }
        [NotNull]
        public bool IsFavorite { get; set; }
        // Display property for UI
        public string DisplayTitle => !string.IsNullOrEmpty(Title) ? Title : "Untitled Image";
        public string FormattedDate => CreatedAt.ToString("MMM dd, HH:mm");
    }
}
