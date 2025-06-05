using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace artstudio.Services
{
    public interface IToastService
    {
        Task ShowToastAsync(string message);
        Task ShowToastAsync(string message, int durationMs = 3000);
    }
    public class ToastService : IToastService
    {
        public async Task ShowToastAsync(string message)
        {
            await ShowToastAsync(message, 3000);
        }

        public async Task ShowToastAsync(string message, int durationMs = 3000)
        {
            try
            {
#if WINDOWS
                // Use Snackbar for Windows as Toast might not work properly
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
#else
                // Use Toast for other platforms
                var toast = Toast.Make(message, ToastDuration.Short, 14);
                await toast.Show();
#endif
            }
            catch (Exception ex)
            {
                // Fallback to DisplayAlert if toast/snackbar fails
                try
                {
                    var mainPage = Application.Current?.MainPage;
                    if (mainPage != null)
                    {
                        await mainPage.DisplayAlert("Notification", message, "OK");
                    }
                }
                catch
                {
                    // If all else fails, just log it
                    System.Diagnostics.Debug.WriteLine($"Toast failed: {message}");
                }
            }
        }
    }

    public class WindowsToastService : IToastService
    {
        public async Task ShowToastAsync(string message)
        {
            await ShowToastAsync(message, 3000);
        }

        public async Task ShowToastAsync(string message, int durationMs = 3000)
        {
            try
            {
                // Try CommunityToolkit Snackbar first
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
            }
            catch (Exception ex)
            {
                // Fallback to DisplayAlert
                var mainPage = Application.Current?.MainPage;
                if (mainPage != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await mainPage.DisplayAlert("Success", message, "OK");
                    });
                }
            }
        }
    }
}
