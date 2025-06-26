using artstudio.ViewModels;
namespace artstudio.Views;

public partial class GalleryDetailPage : ContentPage
{
	public GalleryDetailPage(GalleryDetailPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
    }
}