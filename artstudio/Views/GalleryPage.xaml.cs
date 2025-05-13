using System.Collections.ObjectModel;

namespace artstudio.Views;

public partial class GalleryPage : ContentPage
{
    public ObservableCollection<string> Artworks { get; set; } = new();
    public GalleryPage()
	{
		InitializeComponent();

        Artworks = new ObservableCollection<string>
            {
                "dotnet_bot.png"
            };

        //BindingContext = this; // Debug binding
    }

    private async void OnUploadClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Please select an image",
                FileTypes = FilePickerFileType.Images
            });

            if (result != null)
            {
                Console.WriteLine($"Picked file: {result.FullPath}");
                Artworks.Add(result.FullPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"File picking canceled or failed: {ex.Message}");
        }
    }

    // Binding data source method
    // Directory cache thingy
    // Carousel view arrow and click


}