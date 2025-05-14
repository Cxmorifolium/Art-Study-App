using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using artstudio.Models;
using artstudio.Services;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Alerts;

namespace artstudio.ViewModels
{
    public class ImagePromptViewModel : INotifyPropertyChanged
    {
        private readonly Unsplash _unsplashService;
        private const int DefaultImageCount = 3;
        private const int AdditionalImages = 1;
        private bool _isLoading;

        public ObservableCollection<ImageItem> Images { get; } = new ObservableCollection<ImageItem>();
        
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

        private readonly Stack<(ImageItem item, int index)> _undoStack = new();

        public ICommand RegenerateImagesCommand { get; }
        public ICommand AddImagesCommand { get; }
        public ICommand LoadInitialImagesCommand { get; }
        public ICommand ToggleLockCommand { get; }
        public ICommand DeleteImageCommand { get; }
        public ICommand UndoDeleteCommand { get; }
        public bool CanUndo => _undoStack.Count > 0;

        public ImagePromptViewModel()
        {
            _unsplashService = new Unsplash();
            
            IsLoading = false;

            LoadInitialImagesCommand = new Command(async () => await LoadInitialImagesAsync(), () => !IsLoading);
            AddImagesCommand = new Command(async () => await AddImagesAsync(), () => !IsLoading);
            RegenerateImagesCommand = new Command(async () => await RegenerateImagesAsync(), () => !IsLoading);
            ToggleLockCommand = new Command<ImageItem>(ToggleLock);
            DeleteImageCommand = new Command<ImageItem>(DeleteImage);
            UndoDeleteCommand = new Command(UndoDelete, () => CanUndo);


            // Load initial images when the ViewModel is created
            MainThread.BeginInvokeOnMainThread(async () => await LoadInitialImagesAsync());

        }

        private void CommandsCanExecuteChanged()
        {
            ((Command)AddImagesCommand).ChangeCanExecute();
            ((Command)RegenerateImagesCommand).ChangeCanExecute();
            OnPropertyChanged(nameof(CanUndo));
            ((Command)UndoDeleteCommand).ChangeCanExecute();

        }
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

        private async Task RegenerateImagesAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;

                // Get only the unlocked images (these will be replaced)
                var unlockedIndices = new List<int>();
                for (int i = 0; i < Images.Count; i++)
                {
                    if (!Images[i].IsLocked)
                    {
                        unlockedIndices.Add(i);
                    }
                }

                if (unlockedIndices.Count == 0) return; 

                // Get new images to replace the unlocked ones
                var newImages = await _unsplashService.GetRandomImagesAsync(unlockedIndices.Count);

                // Replace unlocked images with new ones
                for (int i = 0; i < unlockedIndices.Count && i < newImages.Count; i++)
                {
                    var index = unlockedIndices[i];
                    
                    if (index < Images.Count)
                    {
                        Images[index] = new ImageItem(newImages[i]);
                    }
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

        private async Task AddImagesAsync()
        {
            //if (IsLoading) return;
            //Debug
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

        private void ToggleLock(ImageItem? imageItem)
        {
            if (imageItem != null)
            {
                imageItem.IsLocked = !imageItem.IsLocked;
            }
        }

        private async void DeleteImage(ImageItem? imageItem)
        {
            if (imageItem != null && Images.Contains(imageItem))
            {
                int index = Images.IndexOf(imageItem);
                _undoStack.Push((imageItem, index));
                Images.RemoveAt(index);

                OnPropertyChanged(nameof(CanUndo));
                ((Command)UndoDeleteCommand).ChangeCanExecute();

                // Add Notification aka Snackbar
                var snackbar = Snackbar.Make(
                "Image deleted",
                async () => UndoDelete(),
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

                // Insert at original index, or fallback to end
                if (index <= Images.Count)
                    Images.Insert(index, item);
                else
                    Images.Add(item);

                OnPropertyChanged(nameof(CanUndo));
                ((Command)UndoDeleteCommand).ChangeCanExecute();
            }
        }


        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

    }

    // Helper class to wrap UnsplashImage with additional properties
    public class ImageItem : INotifyPropertyChanged
    {
        private bool _isLocked;

        public UnsplashImage UnsplashImage { get; }

        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                if (_isLocked != value)
                {
                    _isLocked = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isDeleted;
        public bool IsDeleted
        {
            get => _isDeleted;
            set
            {
                if (_isDeleted != value)
                {
                    _isDeleted = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DeleteOrUndoIcon));
                }
            }
        }

        public string DeleteOrUndoIcon => IsDeleted ? "undo.png" : "delete.png";


        public ImageItem(UnsplashImage unsplashImage)
        {
            UnsplashImage = unsplashImage;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


}
