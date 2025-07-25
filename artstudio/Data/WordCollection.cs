﻿using SQLite;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace artstudio.Data
{
    [Table("WordCollection")]
    public partial class WordCollection : INotifyPropertyChanged
    {
        private int _id;
        private string? _title;
        private List<string> _wordsList = [];
        private DateTime _createdAt;
        private string? _promptType;
        private bool _isFavorite;

        [PrimaryKey, AutoIncrement]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string? Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        [Ignore] // Don't store this directly in the database
        public List<string> WordsList
        {
            get => _wordsList;
            set => SetProperty(ref _wordsList, value ?? []);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        public string? PromptType
        {
            get => _promptType;
            set => SetProperty(ref _promptType, value);
        }

        public bool IsFavorite
        {
            get => _isFavorite;
            set => SetProperty(ref _isFavorite, value);
        }

        // Essential display properties only
        public string DisplayTitle => !string.IsNullOrEmpty(Title) ? Title : "Untitled Prompt";

        public string FormattedDate => CreatedAt.ToString("MMM dd, yyyy 'at' HH:mm");

        public string WordsDisplayText
        {
            get
            {
                if (WordsList == null || WordsList.Count == 0)
                    return "No words generated";

                var joinedWords = string.Join(", ", WordsList);
                return joinedWords.Length > 100
                    ? string.Concat(joinedWords.AsSpan(0, 97), "...")
                    : joinedWords;
            }
        }

        public int WordCount => WordsList?.Count ?? 0;

        public string WordCountText => $"{WordCount} word{(WordCount != 1 ? "s" : "")}";

        public string PromptTypeDisplay => PromptType switch
        {
            "generated" => "Generated",
            "custom" => "Custom",
            "imported" => "Imported",
            _ => "Unknown"
        };

        public string CleanDisplayTitle
        {
            get
            {
                if (string.IsNullOrEmpty(Title))
                    return "Untitled Prompt";

                // Remove the collection prefix for grouped display
                // For titles like "8bit - fake flowers, Stardew Valley", show just "fake flowers, Stardew Valley"
                if (Title.Contains(" - "))
                {
                    var parts = Title.Split(" - ", 2); // Split into max 2 parts
                    if (parts.Length == 2)
                    {
                        return parts[1]; // Return everything after " - "
                    }
                }

                return Title;
            }
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            // Notify dependent properties when base properties change
            if (propertyName == nameof(WordsList))
            {
                OnPropertyChanged(nameof(WordsDisplayText));
                OnPropertyChanged(nameof(WordCount));
                OnPropertyChanged(nameof(WordCountText));
            }
            else if (propertyName == nameof(Title))
            {
                OnPropertyChanged(nameof(DisplayTitle));
            }
            else if (propertyName == nameof(CreatedAt))
            {
                OnPropertyChanged(nameof(FormattedDate));
            }
            else if (propertyName == nameof(PromptType))
            {
                OnPropertyChanged(nameof(PromptTypeDisplay));
            }
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}