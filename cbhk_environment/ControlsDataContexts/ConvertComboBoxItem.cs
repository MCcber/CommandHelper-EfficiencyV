using System;
using System.Globalization;
using System.Windows.Data;

namespace cbhk_environment.ControlsDataContexts
{
    public class ConvertComboBoxItem : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
                return value.ToString();
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
