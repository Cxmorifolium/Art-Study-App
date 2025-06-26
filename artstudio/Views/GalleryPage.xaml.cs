using artstudio.ViewModels;

namespace artstudio.Views;

public partial class GalleryPage : ContentPage
{
	public GalleryPage(GalleryPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        System.Diagnostics.Debug.WriteLine("=== GALLERY PAGE APPEARING ===");

        if (BindingContext is GalleryPageViewModel viewModel)
        {
            await viewModel.OnAppearingAsync();
        }
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (BindingContext is IDisposable disposableViewModel)
        {
            disposableViewModel.Dispose();
        }
    }
}