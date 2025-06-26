using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using artstudio.ViewModels;

namespace artstudio.Data
{

    public class GalleryCreationData
    {
        public List<string> AvailableWords { get; set; } = new();
        public List<Color> AvailablePalette { get; set; } = new();
        public List<ImageItemViewModel> AvailableImages { get; set; } = new();
        public string? SessionDuration { get; set; }
    }

}
