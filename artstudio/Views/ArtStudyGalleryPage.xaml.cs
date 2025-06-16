using System.Collections.ObjectModel;

namespace artstudio.Views;

public partial class GalleryPage : ContentPage
{
    public ObservableCollection<string> Artworks { get; set; } = new();
    public GalleryPage()
    {
        InitializeComponent();
    }



}