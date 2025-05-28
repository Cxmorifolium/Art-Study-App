using artstudio.ViewModels;

namespace artstudio.Views;

public partial class StudyPage : ContentPage
{
    public StudyPage(StudyPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}