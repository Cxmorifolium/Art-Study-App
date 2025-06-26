//using System.Globalization;

//namespace artstudio.Converters
//{

//    // NOT IN USE
//    public class EnumToBoolConverter : IValueConverter
//    {
//        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
//        {
//            if (value == null || parameter == null)
//                return false;

//            return value.ToString()?.Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase);
//        }

//        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
//        {
//            if (parameter != null && value is bool boolValue && boolValue)
//            {
//                return Enum.Parse(targetType, parameter.ToString() ?? string.Empty);
//            }
//            return Binding.DoNothing;
//        }
//    }

//}