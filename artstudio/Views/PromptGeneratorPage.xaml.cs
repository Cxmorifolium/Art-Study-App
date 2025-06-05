using artstudio.ViewModels;
using artstudio.Data.Models;

namespace artstudio.Views;

public partial class PromptGeneratorPage : ContentPage
{
    private PromptGeneratorViewModel? ViewModel => BindingContext as PromptGeneratorViewModel;

    public PromptGeneratorPage(PromptGeneratorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnFavoriteOptionsClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var favoriteItem = button?.BindingContext as WordCollection;

        if (favoriteItem != null)
        {
            var action = await DisplayActionSheet(
                $"Prompt: {favoriteItem.DisplayTitle}",
                "Cancel",
                null,
                "Load Prompt",
                "Remove from Favorites"
            );

            switch (action)
            {
                case "Load Prompt":
                    ViewModel?.LoadFavoriteCommand?.Execute(favoriteItem);
                    break;
                case "Remove from Favorites":
                    // Show confirmation  
                    bool confirm = await DisplayAlert(
                        "Remove Favorite",
                        $"Remove '{favoriteItem.DisplayTitle}' from favorites?",
                        "Remove",
                        "Cancel"
                    );

                    if (confirm)
                    {
                        ViewModel?.RemoveFavoriteFromFlyoutCommand?.Execute(favoriteItem);
                    }
                    break;
            }
        }
    }
}
