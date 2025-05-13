using Microsoft.Maui.Controls;

namespace artstudio
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
        }

        private void OnThemeToggleClicked(object sender, EventArgs e)
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
