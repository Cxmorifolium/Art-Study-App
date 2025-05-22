using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace artstudio.Models
{
    public class Swatch : INotifyPropertyChanged
    {
        private Color _color;
        private bool _isFavoriteColor;
        private bool _isLocked;
        private bool _isDeleted;
        private Color _previousColor;

        public Color Color
        {
            get => _color;
            set
            {
                if (!_isLocked && _color != value)
                {
                    _color = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HexCode));
                }
            }
        }
        public Color PreviousColor
        {
            get => _previousColor;
            set
            {
                _previousColor = value;
                OnPropertyChanged();
            }
        }

        public string HexCode => Color.ToHex();
        public ImageSource FavoriteColor => ImageSource.FromFile(IsFavoriteColor ? "heart.png" : "unheart.png");
        public ImageSource LockImage => ImageSource.FromFile(IsLocked ? "padlock.png" : "unlock.png");
        public ImageSource DeleteImage => ImageSource.FromFile(IsDeleted ? "undo.png" : "delete.png");


        public bool ButtonVisible => !IsDeleted && IsActive;
        public bool DeleteButtonVisible => IsActive || IsDeleted;

        public bool IsFavoriteColor
        {
            get => _isFavoriteColor;
            set
            {
                if (_isFavoriteColor != value)
                {
                    _isFavoriteColor = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FavoriteColor));
                }
            }
        }

        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                if (_isLocked != value)
                {
                    _isLocked = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LockImage));
                }
            }
        }

        public bool IsDeleted
        {
            get => _isDeleted;
            set
            {
                if (_isDeleted != value)
                {
                    _isDeleted = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DeleteImage));
                    OnPropertyChanged(nameof(ButtonVisible));


                }
            }
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ButtonVisible));
                    OnPropertyChanged(nameof(DeleteButtonVisible));
                }
            }
        }


        public ICommand ToggleFavoriteColorCommand { get; }
        public ICommand ToggleLockCommand { get; }
        public ICommand ToggleDeleteCommand { get; }
        public ICommand ToggleActivateCommand { get; }

        public Swatch(Color color)
        {
            _color = color;
            _previousColor = Colors.Transparent;
            ToggleFavoriteColorCommand = new Command(() => IsFavoriteColor = !IsFavoriteColor);
            ToggleLockCommand = new Command(() => IsLocked = !IsLocked);
            ToggleDeleteCommand = new Command(ToggleDelete);
            ToggleActivateCommand = new Command(() => IsActive = !IsActive);
        }

        private void ToggleDelete()
        {
            if (IsDeleted)
            {
                Color = _previousColor;
                IsDeleted = false;
                IsActive = false;
            }
            else
            {
                _previousColor = Color;
                Color = Colors.Transparent;
                IsDeleted = true;
                IsActive = false;
            }

            OnPropertyChanged(nameof(Color));
            OnPropertyChanged(nameof(ButtonVisible));
            OnPropertyChanged(nameof(DeleteButtonVisible));
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string name = null!) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

