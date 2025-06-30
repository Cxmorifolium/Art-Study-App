using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;

namespace artstudio.Services
{
    public interface IToastService
    {
        Task ShowToastAsync(string message);
        Task ShowToastAsync(string message, int durationMs = 3000);
    }

    public class ToastService : IToastService
    {
        private readonly ILogger<ToastService> _logger;

        public ToastService(ILogger<ToastService> logger)
        {
            _logger = logger;
        }

        public async Task ShowToastAsync(string message)
        {
            await ShowToastAsync(message, 3000);
        }

        public async Task ShowToastAsync(string message, int durationMs = 3000)
        {
            try
            {
                var snackbarOptions = new SnackbarOptions
                {
                    BackgroundColor = Colors.DarkSlateGray,
                    TextColor = Colors.White,
                    ActionButtonTextColor = Colors.Yellow,
                    CornerRadius = new CornerRadius(10),
                    Font = Microsoft.Maui.Font.SystemFontOfSize(14),
                    ActionButtonFont = Microsoft.Maui.Font.SystemFontOfSize(14),
                    CharacterSpacing = 0.5
                };
                var snackbar = Snackbar.Make(message, duration: TimeSpan.FromMilliseconds(durationMs), visualOptions: snackbarOptions);
                await snackbar.Show();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Toast/Snackbar failed: {Message}", message);
                try
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        var mainPage = Application.Current?.Windows.FirstOrDefault()?.Page;
                        if (mainPage != null)
                        {
                            await mainPage.DisplayAlert("Notification", message, "OK");
                        }
                    });
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fallback alert also failed for message: {Message}", message);
                }
            }
        }
    }

    public class WindowsToastService : IToastService
    {
        private readonly ILogger<WindowsToastService> _logger;

        public WindowsToastService(ILogger<WindowsToastService> logger)
        {
            _logger = logger;
        }

        public async Task ShowToastAsync(string message)
        {
            await ShowToastAsync(message, 3000);
        }

        public async Task ShowToastAsync(string message, int durationMs = 3000)
        {
            try
            {
                _logger.LogDebug("Attempting to show snackbar: {Message}", message);

                var snackbarOptions = new SnackbarOptions
                {
                    BackgroundColor = Colors.DarkSlateGray,
                    TextColor = Colors.White,
                    CornerRadius = new CornerRadius(8),
                    Font = Microsoft.Maui.Font.SystemFontOfSize(14),
                    CharacterSpacing = 0.5
                };

                var snackbar = Snackbar.Make(message, duration: TimeSpan.FromMilliseconds(durationMs), visualOptions: snackbarOptions);
                await snackbar.Show();

                _logger.LogDebug("Snackbar.Show() completed for message: {Message}", message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Snackbar failed for message: {Message}", message);
                try
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        var mainPage = Application.Current?.Windows.FirstOrDefault()?.Page;
                        if (mainPage != null)
                        {
                            await mainPage.DisplayAlert("Success", message, "OK");
                        }
                    });
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fallback also failed for message: {Message}", message);
                }
            }
        }
    }
}