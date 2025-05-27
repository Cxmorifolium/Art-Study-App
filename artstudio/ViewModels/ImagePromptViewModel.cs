using artstudio.Models;
using artstudio.Services;
using CommunityToolkit.Maui.Alerts;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace artstudio.ViewModels
{
    public class ImagePromptViewModel : INotifyPropertyChanged
    {
        private readonly Unsplash _unsplashService;
        private const int DefaultImageCount = 3;
        private const int AdditionalImages = 1;
        private bool _isLoading;

        private readonly Stack<(ImageItem item, int index)> _undoStack = new();

        public ObservableCollection<ImageItem> Images { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                    CommandsCanExecuteChanged();
                }
            }
        }

        public bool CanUndo => _undoStack.Count > 0;

        #region Commands

        public ICommand LoadInitialImagesCommand { get; }
        public ICommand AddImagesCommand { get; }
        public ICommand RegenerateImagesCommand { get; }
        public ICommand ToggleLockCommand { get; }
        public ICommand DeleteImageCommand { get; }
        public ICommand UndoDeleteCommand { get; }

        #endregion

        public ImagePromptViewModel()
        {
            _unsplashService = new Unsplash();

            LoadInitialImagesCommand = new Command(async () => await LoadInitialImagesAsync(), () => !IsLoading);
            AddImagesCommand = new Command(async () => await AddImagesAsync(), () => !IsLoading);
            RegenerateImagesCommand = new Command(async () => await RegenerateImagesAsync(), () => !IsLoading);
            ToggleLockCommand = new Command<ImageItem>(ToggleLock);
            DeleteImageCommand = new Command<ImageItem>(async (imageItem) => await DeleteImageAsync(imageItem));
            UndoDeleteCommand = new Command(UndoDelete, () => CanUndo);

            // Load initial images on main thread
            MainThread.BeginInvokeOnMainThread(async () => await LoadInitialImagesAsync());
        }

        #region Command Logic

        private async Task LoadInitialImagesAsync()
        {
            try
            {
                IsLoading = true;
                Images.Clear();

                var images = await _unsplashService.GetRandomImagesAsync(DefaultImageCount);
                foreach (var image in images)
                {
                    Images.Add(new ImageItem(image));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching images: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AddImagesAsync()
        {
            if (IsLoading)
            {
                Debug.WriteLine("Skipped AddImagesAsync because IsLoading is true.");
                return;
            }

            try
            {
                IsLoading = true;

                var images = await _unsplashService.GetRandomImagesAsync(AdditionalImages);
                foreach (var image in images)
                {
                    Images.Add(new ImageItem(image));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding images: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RegenerateImagesAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;

                var unlockedIndices = new List<int>();
                for (int i = 0; i < Images.Count; i++)
                {
                    if (!Images[i].IsLocked)
                        unlockedIndices.Add(i);
                }

                if (unlockedIndices.Count == 0) return;

                var newImages = await _unsplashService.GetRandomImagesAsync(unlockedIndices.Count);

                for (int i = 0; i < unlockedIndices.Count && i < newImages.Count; i++)
                {
                    int index = unlockedIndices[i];
                    if (index < Images.Count)
                        Images[index] = new ImageItem(newImages[i]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error regenerating images: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ToggleLock(ImageItem? imageItem)
        {
            if (imageItem != null)
            {
                imageItem.IsLocked = !imageItem.IsLocked;
            }
        }

        private async Task DeleteImageAsync(ImageItem? imageItem)
        {
            if (imageItem != null && Images.Contains(imageItem))
            {
                int index = Images.IndexOf(imageItem);
                _undoStack.Push((imageItem, index));
                Images.RemoveAt(index);

                OnPropertyChanged(nameof(CanUndo));
                ((Command)UndoDeleteCommand).ChangeCanExecute();

                var snackbar = Snackbar.Make(
                    "Image deleted",
                    () => UndoDelete(),
                    "Undo",
                    TimeSpan.FromSeconds(5));

                await snackbar.Show();
            }
        }            


        private void UndoDelete()
        {
            if (_undoStack.Count > 0)
            {
                var (item, index) = _undoStack.Pop();

                if (index <= Images.Count)
                    Images.Insert(index, item);
                else
                    Images.Add(item);

                OnPropertyChanged(nameof(CanUndo));
                ((Command)UndoDeleteCommand).ChangeCanExecute();
            }
        }

        private void CommandsCanExecuteChanged()
        {
            ((Command)AddImagesCommand).ChangeCanExecute();
            ((Command)RegenerateImagesCommand).ChangeCanExecute();
            ((Command)UndoDeleteCommand).ChangeCanExecute();
            OnPropertyChanged(nameof(CanUndo));
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
