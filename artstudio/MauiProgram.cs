using artstudio.Service;
using artstudio.Models;
using artstudio.Services;
using artstudio.ViewModels;
using artstudio.Views;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Serilog;

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

        // Configure Serilog Logger
        ConfigureSerilog();
        builder.Services.AddSerilog();

        // Register services
        RegisterServices(builder.Services);
        RegisterViewModelsAndPages(builder.Services);

        // Register the prompt data service
        builder.Services.AddSingleton<IPromptDataService, PromptDataService>();

#if DEBUG
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
#else
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
#endif

        // Build the app FIRST
        var app = builder.Build();

        // THEN initialize the service
        using var scope = app.Services.CreateScope();
        var promptDataService = scope.ServiceProvider.GetRequiredService<IPromptDataService>();
        _ = promptDataService.InitializeAsync();

        return app;
    }

    private static void ConfigureSerilog()
    {
        var logDirectory = Path.Combine(FileSystem.AppDataDirectory, "logs");
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(
                path: Path.Combine(logDirectory, "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7) // Keep 7 days of logs
#if DEBUG
            .WriteTo.Debug()
            .WriteTo.Console()
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Warning()
#endif
            .CreateLogger();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        // Core services
        services.AddSingleton<DatabaseService>();
        services.AddSingleton<WordPromptService>();
        services.AddSingleton<PaletteService>();
        services.AddScoped<Unsplash>();
        services.AddTransient<PaletteModel>();

        // Debug service
        services.AddSingleton<IDebugService, DebugService>();

        // Export service
        services.AddSingleton<Export>();

        // Prompt generator with proper DI
        services.AddSingleton<PromptGenerator>(provider =>
        {
            string baseDir = Path.Combine(FileSystem.AppDataDirectory, "prompt_data");
            return new PromptGenerator(baseDir);
        });

        // Platform-specific services
        RegisterPlatformServices(services);
    }

    private static void RegisterPlatformServices(IServiceCollection services)
    {
#if WINDOWS
        services.AddSingleton<IToastService, WindowsToastService>();
        services.AddSingleton<IFileSaveService, FileSaveService>();
#elif ANDROID
        services.AddSingleton<IToastService, ToastService>();
        // Add Android-specific services here
#elif IOS
        services.AddSingleton<IToastService, ToastService>();
        // Add iOS-specific services here
#elif MACCATALYST
        services.AddSingleton<IToastService, ToastService>();
        // Add Mac-specific services here
#else
        services.AddSingleton<IToastService, ToastService>();
#endif
    }

    private static void RegisterViewModelsAndPages(IServiceCollection services)
    {
        // ViewModels
        services.AddTransient<PaletteViewModel>();
        services.AddTransient<StudyPageViewModel>();
        services.AddTransient<ImagePromptViewModel>();
        services.AddTransient<PromptGeneratorViewModel>();

        // Pages
        services.AddTransient<PalettePage>();
        services.AddTransient<StudyPage>();
        services.AddTransient<ImagePromptPage>();
        services.AddTransient<PromptGeneratorPage>();

        // App
        services.AddSingleton<App>();
    }
}