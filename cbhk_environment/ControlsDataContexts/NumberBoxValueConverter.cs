using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace cbhk_environment.ControlsDataContexts
{
    public class NumberBoxValueConverter : MarkupExtension, IValueConverter
    {
        private bool hasDot;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var dbl = (double)value;
            if (hasDot && Math.Truncate(dbl) == dbl)
            {
                return $"{dbl}.";
            }
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                hasDot = str.EndsWith(".");

                if (double.TryParse(str, out var val))
                    return val;
            }
            return DependencyProperty.UnsetValue;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
