using artstudio.ViewModels;

namespace artstudio.Views;

public partial class ImagePromptPage : ContentPage
{
    public ImagePromptPage(ImagePromptViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

}