using System;
using System.Globalization;
using System.Windows.Data;

namespace CBHK.Utility.Visual.TreeView
{
    public class TreeViewControlHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double result = double.Parse(value.ToString());
            return result - 10;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
