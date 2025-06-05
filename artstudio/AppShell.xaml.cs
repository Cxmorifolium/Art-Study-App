
namespace artstudio
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register routes for navigation and just in case
            Routing.RegisterRoute(nameof(Views.PromptGeneratorPage), typeof(Views.PromptGeneratorPage));
            Routing.RegisterRoute("study", typeof(Views.StudyPage));
            Routing.RegisterRoute("palette", typeof(Views.PalettePage));
            Routing.RegisterRoute("Images", typeof(Views.ImagePromptPage));

        }

        private void OnThemeToggleClicked(object sender, EventArgs e)
        {
            if (Application.Current != null)
            {
                // If the theme is currently light, switch to dark, and vice versa  
                if (Application.Current.UserAppTheme == AppTheme.Dark)
                {
                    Application.Current.UserAppTheme = AppTheme.Light;
                }
                else
                {
                    Application.Current.UserAppTheme = AppTheme.Dark;
                }
            }
        }
    }
}
