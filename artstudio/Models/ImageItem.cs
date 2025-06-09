using System.ComponentModel;
using System.Runtime.CompilerServices;
using artstudio.Models;
using System.Windows.Input;
using System.Diagnostics;

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

        // Add properties for attribution and URL handling
        public string AttributionText => $"By {UnsplashImage.user?.Name ?? "Unknown"}";
        public string UserProfileUrl => UnsplashImage.user?.PortfolioUrl ?? string.Empty;
        public bool HasUserUrl => !string.IsNullOrEmpty(UserProfileUrl);

        // Add properties for image URL handling
        public string ImagePageUrl => !string.IsNullOrEmpty(UnsplashImage.Id)
            ? $"https://unsplash.com/photos/{UnsplashImage.Id}"
            : string.Empty;
        public bool HasImageUrl => !string.IsNullOrEmpty(ImagePageUrl);

        // Commands
        public ICommand OpenUserProfileCommand { get; }
        public ICommand OpenImagePageCommand { get; }

        public ImageItem(UnsplashImage unsplashImage)
        {
            UnsplashImage = unsplashImage;
            OpenUserProfileCommand = new Command(async () => await OpenUserProfileAsync());
            OpenImagePageCommand = new Command(async () => await OpenImagePageAsync());
        }

        private async Task OpenUserProfileAsync()
        {
            if (HasUserUrl)
            {
                try
                {
                    await Launcher.OpenAsync(new Uri(UserProfileUrl));
                }
                catch (Exception)
                {
                    Debug.WriteLine($"Failed to open URL: {UserProfileUrl}");
                }
            }
        }

        private async Task OpenImagePageAsync()
        {
            if (HasImageUrl)
            {
                try
                {
                    // Replace WebAuthenticator.OpenAsync with Launcher.OpenAsync
                    await Launcher.OpenAsync(new Uri(ImagePageUrl));
                }
                catch (Exception)
                {
                    Debug.WriteLine($"Failed to open image page: {ImagePageUrl}");
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class UnsplashImage
    {
        public string? Id { get; set; }
        public string? Description { get; set; }
        public Urls? urls { get; set; }
        public User? user { get; set; }

        public class Urls
        {
            public string? Raw { get; set; }
            public string? Full { get; set; }
            public string? Regular { get; set; }
            public string? Small { get; set; }
            public string? Thumb { get; set; }
        }

        public class User
        {
            public string? Name { get; set; }
            // Changed from Portfolio_Url to follow C# naming conventions
            public string? PortfolioUrl { get; set; }
        }
    }
}