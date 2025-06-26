using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace artstudio.Converters
{
    public class IntToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            if (value is int intValue)
                return intValue > 0;
            return false;
        }

        public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
