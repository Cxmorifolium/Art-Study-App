using System.Globalization;

namespace artstudio.Converters
{
    public class HexToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return Colors.Transparent;

            string? hexString = value.ToString();

            if (string.IsNullOrWhiteSpace(hexString))
                return Colors.Transparent;

            try
            {
                // Ensure hex string starts with #
                if (!hexString.StartsWith('#'))
                {
                    hexString = "#" + hexString;
                }

                return Color.FromArgb(hexString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HexToColorConverter error: {ex.Message} for value: {hexString}");
                return Colors.Transparent;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                try
                {
                    return color.ToArgbHex();
                }
                catch
                {
                    return "#00000000";
                }
            }

            return "#00000000";
        }
    }
}