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
            if(parameter is string parameterStringValue && double.TryParse(parameterStringValue,out double parameterDoubleValue))
            {
                result -= parameterDoubleValue;
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
