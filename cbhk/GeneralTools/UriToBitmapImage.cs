using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace cbhk.GeneralTools
{
    public class UriToBitmapImage : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return null;
            return new BitmapImage((Uri)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
