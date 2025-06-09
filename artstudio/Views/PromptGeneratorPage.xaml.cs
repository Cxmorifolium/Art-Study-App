using artstudio.ViewModels;
using artstudio.Data.Models;
using System.Diagnostics;

namespace artstudio.Views;

public partial class PromptGeneratorPage : ContentPage
{
    private PromptGeneratorViewModel? ViewModel => BindingContext as PromptGeneratorViewModel;

    public PromptGeneratorPage(PromptGeneratorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnFavoriteOptionsClickedAsync(object sender, EventArgs e)
    {
        try
        {
            if (sender is Button button && button.BindingContext is WordCollection favoriteItem)
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
        catch (Exception ex)
        {
            // Handle the exception gracefully to prevent app crashes
            await DisplayAlert("Error", "An error occurred while processing your request. Please try again.", "OK");
            // Log the exception for debugging
            Debug.WriteLine($"Error in OnFavoriteOptionsClickedAsync: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}