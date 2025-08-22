using System;
using System.Globalization;
using System.Windows.Data;

namespace CBHK.ControlDataContext
{
    public class ConverterIntToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not null)
                return value.ToString();
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
