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
                System.Diagnostics.Debug.WriteLine($"Toast/Snackbar failed: {ex.Message}");
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
                    System.Diagnostics.Debug.WriteLine($"Fallback alert also failed: {fallbackEx.Message}");
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
                System.Diagnostics.Debug.WriteLine($"WindowsToastService: Attempting to show snackbar: {message}");

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

                System.Diagnostics.Debug.WriteLine($"WindowsToastService: Snackbar.Show() completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WindowsToastService: Snackbar failed: {ex.Message}");
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
                    System.Diagnostics.Debug.WriteLine($"WindowsToastService: Fallback also failed: {fallbackEx.Message}");
                }
            }
        }
    }
}