using System;
using System.Globalization;
using System.Windows.Data;

namespace cbhk_environment.ControlsDataContexts
{
    public class ConvertIconComboBoxItemText : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is not null and IconComboBoxItem)
            {
                IconComboBoxItem iconComboBoxItem = (IconComboBoxItem)value;
                return iconComboBoxItem.ComboBoxItemText;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
