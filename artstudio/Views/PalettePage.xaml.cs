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

    private async void OnSwatchDoubleTapped(object sender, TappedEventArgs e)
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
                case "Load Color":
                    ViewModel?.LoadSwatchColorCommand?.Execute(swatchItem.HexColor);
                    break;
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

    private async void OnPaletteDoubleTapped(object sender, TappedEventArgs e)
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

    private async void OnPaletteOptionsClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var paletteItem = button?.BindingContext as FavoritePaletteItem;

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
}