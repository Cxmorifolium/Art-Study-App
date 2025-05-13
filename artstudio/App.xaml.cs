using Microsoft.Maui.Controls;
using artstudio.Views;
using Microsoft.Extensions.DependencyInjection; 

namespace artstudio
{
    public partial class App : Application
    {
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            Application.Current.UserAppTheme = Application.Current.RequestedTheme;

            
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }


    }
}