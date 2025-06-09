namespace artstudio
{
    public partial class App : Application
    {
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            if (Application.Current != null)
            {
                Application.Current.UserAppTheme = Application.Current.RequestedTheme;
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}