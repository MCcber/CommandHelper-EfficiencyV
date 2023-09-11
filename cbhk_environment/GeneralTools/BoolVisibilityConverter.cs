using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace cbhk_environment.GeneralTools
{
    public class BoolVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility currentValue = (Visibility)value;
            return currentValue == Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool? currentValue = (bool?)value;
            return currentValue.Value ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
