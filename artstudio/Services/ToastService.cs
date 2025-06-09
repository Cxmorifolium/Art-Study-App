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
                var toast = Toast.Make(message, ToastDuration.Short, 14);
                await toast.Show();
#endif
            }
            catch (Exception)
            {
                try
                {
                    var mainPage = Application.Current?.Windows.FirstOrDefault()?.Page;
                    if (mainPage != null)
                    {
                        await mainPage.DisplayAlert("Notification", message, "OK");
                    }
                }
                catch
                {
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
            catch (Exception)
            {
                var mainPage = Application.Current?.Windows.FirstOrDefault()?.Page; // Updated to use Windows[0].Page
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
