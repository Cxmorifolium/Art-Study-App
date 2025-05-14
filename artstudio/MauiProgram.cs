using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using artstudio.Services;
using artstudio.ViewModels;
using artstudio.Views;
using Microsoft.Win32;
//using artstudio.Data;


#if WINDOWS
using artstudio.Platforms.Windows;
#endif

namespace artstudio;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit(options =>
            {
#if WINDOWS
                options.SetShouldEnableSnackbarOnWindows(true);
#endif
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
        
        //Register services
        RegisterServices(builder.Services);

        // Register SQLite database
        //builder.Services.AddSingleton<AppDatabase>();

        // Register pages and view models
        builder.Services.AddTransient<PalettePage>();
        builder.Services.AddTransient<PaletteViewModel>();

        builder.Services.AddSingleton<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        // Register Export service
        services.AddSingleton<Export>();

        // Register platform-specific services
#if WINDOWS
        services.AddSingleton<IFileSaveService, FileSaveService>();
#elif ANDROID
        // Add Android implementation here
        // services.AddSingleton<IFileSaveService, AndroidFileSaveService>();
#elif IOS
        // Add iOS implementation here
        // services.AddSingleton<IFileSaveService, iOSFileSaveService>();
#elif MACCATALYST
        // Add Mac implementation here
        // services.AddSingleton<IFileSaveService, MacFileSaveService>();
#else
        // Default implementation or throw an exception
        // services.AddSingleton<IFileSaveService, DefaultFileSaveService>();
#endif
    }
}