using System;
using System.Globalization;
using System.Windows.Data;

namespace CBHK.Utility.Common
{
    class DivisionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double result = 0;
            if(parameter is string stringValue)
            {
                result = double.Parse(value.ToString()) / double.Parse(stringValue);
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
