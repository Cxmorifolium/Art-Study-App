using artstudio.ViewModels;
namespace artstudio.Views;

public partial class GalleryCreationPage : ContentPage
{
	public GalleryCreationPage(GalleryCreationViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
    }
}