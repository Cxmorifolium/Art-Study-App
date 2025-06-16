using System.ComponentModel;

namespace artstudio.Models
{
    public class SwatchModel
    {
        public Color Color { get; set; }
        public Color PreviousColor { get; set; }
        public bool IsLocked { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; } 

        // Pure data properties only
        public string HexCode => Color.ToHex();

        public SwatchModel(Color color)
        {
            Color = color;
            PreviousColor = Colors.Transparent;
            IsActive = false;
        }

        // Business logic methods (no UI dependencies)
        public void Delete()
        {
            if (IsDeleted)
            {
                // Restore
                Color = PreviousColor;
                IsDeleted = false;
                IsActive = false;
            }
            else
            {
                // Delete
                PreviousColor = Color;
                Color = Colors.Transparent;
                IsDeleted = true;
                IsActive = false;
            }
        }

        public void ToggleLock()
        {
            IsLocked = !IsLocked;
        }

        public bool CanUpdateColor()
        {
            return !IsLocked;
        }
    }
}