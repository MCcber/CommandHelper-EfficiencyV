using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CBHK.Utility.Data
{
    /// <summary>#RRGGBB 字符串 ↔ Color（VectorColorPicker 的 SelectedColor 是 Color，节点值是字符串）。</summary>
    public sealed class HexStringToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = System.Convert.ToString(value)?.Trim();
            if (string.IsNullOrEmpty(text) || !text.StartsWith('#')) return Colors.Transparent;
            try
            {
                return (Color)ColorConverter.ConvertFromString(text);
            }
            catch
            {
                return Colors.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Color color ? $"#{color.R:X2}{color.G:X2}{color.B:X2}" : "";
    }
}
