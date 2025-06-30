using artstudio.ViewModels;
using artstudio.Services;

namespace artstudio.Views;

public partial class PalettePage : ContentPage
{
    private PaletteViewModel? ViewModel => BindingContext as PaletteViewModel;

    public PalettePage(PaletteViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    #region Swatch Event Handlers

    private async Task HandleSwatchDoubleTappedAsync(object sender, TappedEventArgs e)
    {
        var border = sender as Border;
        var swatchItem = border?.BindingContext as FavoriteSwatchItem;

        if (swatchItem != null)
        {
            var action = await DisplayActionSheet(
                $"Color {swatchItem.HexColor}",
                "Cancel",
                null,
                "Load Color",
                "Copy Hex Code",
                "Remove from Favorites"
            );

            switch (action)
            {
                case "Copy Hex Code":
                    await Clipboard.SetTextAsync(swatchItem.HexColor);
                    await DisplayAlert("Copied", $"Copied {swatchItem.HexColor} to clipboard", "OK");
                    break;
                case "Remove from Favorites":
                    ViewModel?.RemoveFavoriteSwatchCommand?.Execute(swatchItem);
                    break;
            }
        }
    }

    private void OnSwatchDoubleTapped(object sender, TappedEventArgs e)
    {
        _ = HandleSwatchDoubleTappedAsync(sender, e);
    }

    private async Task OnSwatchRemoveClickedAsync(object sender, TappedEventArgs e)
    {
        if (sender is Border border && border.BindingContext is FavoriteSwatchItem swatchItem)
        {
            var viewModel = BindingContext as PaletteViewModel;
            if (viewModel != null)
            {
                // Simulate async behavior to resolve CS1998
                await Task.Run(() => viewModel.RemoveFavoriteSwatchCommand.Execute(swatchItem));
            }
        }
    }

    private void OnSwatchRemoveClicked(object sender, TappedEventArgs e)
    {
        _ = OnSwatchRemoveClickedAsync(sender, e);
    }

    #endregion

    #region Palette Event Handlers

    private async Task HandlePaletteDoubleTappedAsync(object sender, TappedEventArgs e)
    {
        var border = sender as Border;
        var paletteItem = border?.BindingContext as FavoritePaletteItem;

        if (paletteItem != null)
        {
            var action = await DisplayActionSheet(
                $"Palette: {paletteItem.Title}",
                "Cancel",
                null,
                "Load Palette",
                "Copy All Hex Codes",
                "Remove from Favorites"
            );

            switch (action)
            {
                case "Load Palette":
                    ViewModel?.LoadFavoritePaletteCommand?.Execute(paletteItem);
                    break;
                case "Copy All Hex Codes":
                    var hexCodes = string.Join(", ", paletteItem.Colors);
                    await Clipboard.SetTextAsync(hexCodes);
                    await DisplayAlert("Copied", $"Copied {paletteItem.Colors.Count} colors to clipboard", "OK");
                    break;
                case "Remove from Favorites":
                    ViewModel?.RemoveFavoritePaletteCommand?.Execute(paletteItem);
                    break;
            }
        }
    }

    private void OnPaletteDoubleTapped(object sender, TappedEventArgs e)
    {
        _ = HandlePaletteDoubleTappedAsync(sender, e);
    }

    private void OnPaletteLoadClicked(object sender, TappedEventArgs e)
    {
        try
        {
            if (sender is Border border && border.BindingContext is FavoritePaletteItem paletteItem)
            {
                var viewModel = BindingContext as PaletteViewModel;
                viewModel?.LoadFavoritePaletteCommand.Execute(paletteItem);
            }
        }
        catch (Exception)
        {
            // If you need logging here, consider showing user feedback instead
            _ = DisplayAlert("Error", "Failed to load palette", "OK");
        }
    }

    private void OnPaletteOptionsClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var paletteItem = button?.BindingContext as FavoritePaletteItem;
        if (paletteItem != null)
        {
            _ = HandlePaletteOptionsAsync(paletteItem);
        }
    }

    private async Task HandlePaletteOptionsAsync(FavoritePaletteItem paletteItem)
    {
        try
        {
            var action = await DisplayActionSheet(
                $"Palette: {paletteItem.Title}",
                "Cancel",
                null,
                "Load Palette",
                "Copy All Hex Codes",
                "Remove from Favorites"
            );

            switch (action)
            {
                case "Load Palette":
                    ViewModel?.LoadFavoritePaletteCommand?.Execute(paletteItem);
                    break;
                case "Copy All Hex Codes":
                    var hexCodes = string.Join(", ", paletteItem.Colors);
                    await Clipboard.SetTextAsync(hexCodes);
                    await DisplayAlert("Copied", $"Copied {paletteItem.Colors.Count} colors to clipboard", "OK");
                    break;
                case "Remove from Favorites":
                    ViewModel?.RemoveFavoritePaletteCommand?.Execute(paletteItem);
                    break;
            }
        }
        catch (Exception)
        {
            await DisplayAlert("Error", "An error occurred. Please try again.", "OK");
        }
    }

    #endregion
}