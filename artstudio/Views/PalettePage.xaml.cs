using artstudio.ViewModels;

namespace artstudio.Views;

public partial class PalettePage : ContentPage
{
    public PalettePage(PaletteViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

    }

}
