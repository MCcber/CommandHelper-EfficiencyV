using CBHK.Model.Common;
using System;
using System.Globalization;
using System.Windows.Data;

namespace CBHK.Utility.Common
{
    /// <summary>
    /// Tag转字符串
    /// </summary>
    public class TagToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => (NBTDataStructure)value;
    }
}
