using artstudio.ViewModels;
using artstudio.Services;
using System.Diagnostics;

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

    // Updated to avoid async void and call the renamed method
    private void OnSwatchDoubleTapped(object sender, TappedEventArgs e)
    {
        _ = HandleSwatchDoubleTappedAsync(sender, e);
    }

    private async Task OnSwatchRemoveClickedAsync(object sender, TappedEventArgs e)
    {
        Debug.WriteLine("=== OnSwatchRemoveClickedAsync fired ===");

        if (sender is Border border && border.BindingContext is FavoriteSwatchItem swatchItem)
        {
            Debug.WriteLine($"Found swatch item: {swatchItem.HexColor}");

            var viewModel = BindingContext as PaletteViewModel;
            if (viewModel != null)
            {
                Debug.WriteLine("ViewModel found, executing command");

                // Simulate async behavior to resolve CS1998
                await Task.Run(() => viewModel.RemoveFavoriteSwatchCommand.Execute(swatchItem));
            }
            else
            {
                Debug.WriteLine("ViewModel is null!");
            }
        }
        else
        {
            Debug.WriteLine("Border or BindingContext is null!");
        }
    }

    // Updated to avoid async void and call the renamed method
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
            System.Diagnostics.Debug.WriteLine("=== OnPaletteLoadClicked fired ===");
            if (sender is Border border && border.BindingContext is FavoritePaletteItem paletteItem)
            {
                System.Diagnostics.Debug.WriteLine($"Found palette item: {paletteItem.Title}");
                var viewModel = BindingContext as PaletteViewModel;
                if (viewModel != null)
                {
                    System.Diagnostics.Debug.WriteLine("ViewModel found, executing command");
                    // Remove unnecessary Task.Run - the command handles async internally
                    viewModel.LoadFavoritePaletteCommand.Execute(paletteItem);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ViewModel is null!");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Border or BindingContext is null!");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnPaletteLoadClicked: {ex.Message}");
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
        catch (Exception ex)
        {
            // Log the exception or handle it appropriately
            System.Diagnostics.Debug.WriteLine($"Error in palette options: {ex.Message}");
            // Optionally show user-friendly error message
        }
    }

    #endregion

    #region Debug Methods (Optional - for troubleshooting)

    // Add this temporarily if you need to debug palette data
    private void DebugPaletteData(object sender, EventArgs e)
    {
        if (ViewModel?.GroupedFavoritePalettes != null)
        {
            Debug.WriteLine($"=== MANUAL DEBUG: {ViewModel.GroupedFavoritePalettes.Count} groups ===");

            foreach (var group in ViewModel.GroupedFavoritePalettes)
            {
                Debug.WriteLine($"Group: {group.DisplayName} ({group.Count} palettes)");

                foreach (var palette in group.Palettes.Take(2))
                {
                    Debug.WriteLine($"  Palette: {palette.Title}");
                    Debug.WriteLine($"  Colors count: {palette.Colors?.Count ?? 0}");
                    Debug.WriteLine($"  Colors: [{string.Join(", ", palette.Colors ?? new List<string>())}]");
                }
            }
        }
    }

    #endregion
}