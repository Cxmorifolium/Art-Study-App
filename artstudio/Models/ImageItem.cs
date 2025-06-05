using System.ComponentModel;
using System.Runtime.CompilerServices;
using artstudio.Models;

namespace artstudio.Models
{
    public class ImageItem : INotifyPropertyChanged
    {
        private bool _isLocked;
        private bool _isDeleted;

        public UnsplashImage UnsplashImage { get; }

        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                if (_isLocked != value)
                {
                    _isLocked = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsDeleted
        {
            get => _isDeleted;
            set
            {
                if (_isDeleted != value)
                {
                    _isDeleted = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DeleteOrUndoIcon));
                }
            }
        }

        public string DeleteOrUndoIcon => IsDeleted ? "undo.png" : "delete.png";

        public string ImageSource => UnsplashImage.urls?.Small ?? "placeholder_image.png";
        public string Description
        {
            get
            {
                string desc = UnsplashImage.Description ?? "Untitled";
                return desc.Length > 40 ? desc.Substring(0, 40) + "..." : desc;
            }
        }



        public ImageItem(UnsplashImage unsplashImage)
        {
            UnsplashImage = unsplashImage;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class UnsplashImage
    {
        // just in case properties for future
        public string? Id { get; set; }
        public string? Description { get; set; }
        public Urls? urls { get; set; }
        public User? user { get ; set; }

        public class Urls
        {   
            // just in case properties for future
            public string? Raw { get; set; }
            public string? Full { get; set; }
            public string? Regular { get; set; }
            public string? Small { get; set; }
            public string? Thumb { get; set; }
        }

        public class User
        {
            // just in case properties for future
            public string? Name { get; set; }
            public string? Portfolio_Url { get; set; }
        }
    }
}