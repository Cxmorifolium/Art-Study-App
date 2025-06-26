using Microsoft.Maui.Controls;
using System.Globalization;

namespace artstudio.Converters
{
    public class BoolToHeartIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isFavorited)
            {
                return isFavorited ? "heart.png" : "unheart.png";
            }
            return "unheart.png";
        }

        public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}