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
        public string Description => UnsplashImage.Description ?? "Untitled";

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
}
