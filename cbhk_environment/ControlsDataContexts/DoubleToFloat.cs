using System;
using System.Globalization;
using System.Windows.Data;

namespace cbhk_environment.ControlsDataContexts
{
    public class DoubleToFloat : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            bool success = float.TryParse(value.ToString(),out float result);
            if(success)
                return result;
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
