using artstudio.ViewModels;
using artstudio.Data;
namespace artstudio.Views;

public partial class ImagePromptPage : ContentPage
{
    public ImagePromptPage(ImagePromptViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void OnFavoriteOptionsClicked(object sender, EventArgs e)
    {
        _ = HandleFavoriteOptionsAsync(sender, e);
    }

    private async Task HandleFavoriteOptionsAsync(object sender, EventArgs e)
    {
        try
        {
            if (sender is Button button && button.BindingContext is FavoriteImageItem favoriteImage)
            {
                var result = await DisplayActionSheet(
                    "Options",
                    "Cancel",
                    null,
                    "Load Image",
                    "Remove from Favorites"
                );

                if (BindingContext is ImagePromptViewModel viewModel)
                {
                    switch (result)
                    {
                        case "Load Image":
                            await viewModel.LoadFromFavoritesCommand.ExecuteAsync(favoriteImage);
                            break;

                        case "Remove from Favorites":
                            await viewModel.RemoveFromFavoritesCommand.ExecuteAsync(favoriteImage);
                            break;
                    }
                }
            }
        }
        catch (Exception)
        {
            await DisplayAlert("Error", "An error occurred while processing your request.", "OK");
        }
    }
}