using System;
using System.Globalization;
using System.Windows.Data;

namespace CBHK.ViewModel.Components.Datapack.HomePage
{
    public class HomePageHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double height)
            {
                return height - 130 > 0 ? height - 130 : 0;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
