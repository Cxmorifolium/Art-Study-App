#if WINDOWS
using artstudio.Services;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace artstudio.Platforms.Windows
{
    public class FileSaveService : IFileSaveService
    {
        public async Task SaveFileAsync(byte[] imageBytes, string fileName)
        {
            // Get the current window handle
            var window = Application.Current?.Windows[0].Handler.PlatformView as Microsoft.UI.Xaml.Window;
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

            // Initialize file picker with window handle
            var picker = new FileSavePicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            // Configure picker
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.SuggestedFileName = fileName;
            picker.FileTypeChoices.Add("PNG Image", new List<string> { ".png" });

            // Pick file location
            StorageFile file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                await FileIO.WriteBytesAsync(file, imageBytes);
            }
        }
    }
}
#endif