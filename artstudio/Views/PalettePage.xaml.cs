using artstudio.ViewModels;
using artstudio.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using artstudio.Services;

namespace artstudio.Views;

public partial class PalettePage : ContentPage
{
    public PalettePage(PaletteViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

    }

}
