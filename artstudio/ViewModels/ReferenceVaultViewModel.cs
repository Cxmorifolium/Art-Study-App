using Microsoft.Maui.Controls;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace artstudio.ViewModels
{
    public class ReferenceVaultViewModel : INotifyPropertyChanged
    {
        public enum TabType
        {
            Prompts,
            Images,
            Palettes
        }

        private TabType _currentTab = TabType.Palettes;
        private View _tabContent;
        private Color _promptsTabBorderColor = Colors.Transparent;
        private Color _imagesTabBorderColor = Colors.Transparent;
        private Color _palettesTabBorderColor = Colors.Transparent;
        private int _promptsTabBorderThickness = 0;
        private int _imagesTabBorderThickness = 0;
        private int _palettesTabBorderThickness = 0;

        // Default view
        public TabType CurrentTab
        {
            get => _currentTab;
            set
            {
                if (_currentTab != value)
                {
                    _currentTab = value;
                    OnPropertyChanged();
                    UpdateTabVisuals();
                    UpdateTabContent();
                }
            }
        }

        public View TabContent
        {
            get => _tabContent;
            set
            {
                if (_tabContent != value)
                {
                    _tabContent = value;
                    OnPropertyChanged();
                }
            }
        }

        // Tab border colors
        public Color PromptsTabBorderColor
        {
            get => _promptsTabBorderColor;
            set
            {
                if (_promptsTabBorderColor != value)
                {
                    _promptsTabBorderColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public Color ImagesTabBorderColor
        {
            get => _imagesTabBorderColor;
            set
            {
                if (_imagesTabBorderColor != value)
                {
                    _imagesTabBorderColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public Color PalettesTabBorderColor
        {
            get => _palettesTabBorderColor;
            set
            {
                if (_palettesTabBorderColor != value)
                {
                    _palettesTabBorderColor = value;
                    OnPropertyChanged();
                }
            }
        }

        // Tab border thickness
        public int PromptsTabBorderThickness
        {
            get => _promptsTabBorderThickness;
            set
            {
                if (_promptsTabBorderThickness != value)
                {
                    _promptsTabBorderThickness = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ImagesTabBorderThickness
        {
            get => _imagesTabBorderThickness;
            set
            {
                if (_imagesTabBorderThickness != value)
                {
                    _imagesTabBorderThickness = value;
                    OnPropertyChanged();
                }
            }
        }

        public int PalettesTabBorderThickness
        {
            get => _palettesTabBorderThickness;
            set
            {
                if (_palettesTabBorderThickness != value)
                {
                    _palettesTabBorderThickness = value;
                    OnPropertyChanged();
                }
            }
        }

        // Tab selection commands
        public ICommand SelectPromptsTabCommand { get; }
        public ICommand SelectImagesTabCommand { get; }
        public ICommand SelectPalettesTabCommand { get; }

        // Constructor
        public ReferenceVaultViewModel()
        {
            // Initialize commands
            SelectPromptsTabCommand = new Command(() => CurrentTab = TabType.Prompts);
            SelectImagesTabCommand = new Command(() => CurrentTab = TabType.Images);
            SelectPalettesTabCommand = new Command(() => CurrentTab = TabType.Palettes);

            // Set initial state
            UpdateTabVisuals();
            UpdateTabContent();
        }

        // Update tab visual state based on current selection
        private void UpdateTabVisuals()
        {
            // Reset all tabs
            PromptsTabBorderColor = Colors.Transparent;
            ImagesTabBorderColor = Colors.Transparent;
            PalettesTabBorderColor = Colors.Transparent;
            PromptsTabBorderThickness = 0;
            ImagesTabBorderThickness = 0;
            PalettesTabBorderThickness = 0;

            // Highlight selected tab
            switch (CurrentTab)
            {
                case TabType.Prompts:
                    PromptsTabBorderColor = Colors.White;
                    PromptsTabBorderThickness = 2;
                    break;
                case TabType.Images:
                    ImagesTabBorderColor = Colors.White;
                    ImagesTabBorderThickness = 2;
                    break;
                case TabType.Palettes:
                    PalettesTabBorderColor = Colors.White;
                    PalettesTabBorderThickness = 2;
                    break;
            }
        }

        // Update content based on selected tab
        private void UpdateTabContent()
        {
            switch (CurrentTab)
            {
                case TabType.Prompts:
                    TabContent = CreatePromptsContent();
                    break;
                case TabType.Images:
                    TabContent = CreateImagesContent();
                    break;
                case TabType.Palettes:
                    TabContent = CreatePalettesContent();
                    break;
            }
        }

        // Create content for Prompts tab
        private View CreatePromptsContent()
        {
            return new VerticalStackLayout
            {
                Children =
                {
                    new Label
                    {
                        Text = "Prompts Tab Content Goes Here!",
                        FontSize = 18,
                        HorizontalOptions = LayoutOptions.Center,
                        Margin = new Thickness(0, 20)
                    },
                    // Add your actual Prompts content here
                }
            };
        }

        // Create content for Images tab
        private View CreateImagesContent()
        {
            return new VerticalStackLayout
            {
                Children =
                {
                    new Label
                    {
                        Text = "Images Tab Content Goes Here!",
                        FontSize = 18,
                        HorizontalOptions = LayoutOptions.Center,
                        Margin = new Thickness(0, 20)
                    },
                    // Add your actual Images content here
                }
            };
        }

        // Create content for Palettes tab
        private View CreatePalettesContent()
        {
            return new VerticalStackLayout
            {
                Children =
                {
                    new Label
                    {
                        Text = "Palettes Tab Content Goes Here!",
                        FontSize = 18,
                        HorizontalOptions = LayoutOptions.Center,
                        Margin = new Thickness(0, 20)
                    },
                    // Add Saved Palettes here
                }
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}