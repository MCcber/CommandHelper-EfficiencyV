using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CBHK.Utility.Data
{
    /// <summary>#RRGGBB 文本 → 画刷（颜色字段的色块预览）。非法值返回透明，不抛。</summary>
    public sealed class HexToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = System.Convert.ToString(value)?.Trim();
            if (string.IsNullOrEmpty(text) || !text.StartsWith("#")) return Brushes.Transparent;
            try
            {
                return new BrushConverter().ConvertFromString(text) as Brush ?? Brushes.Transparent;
            }
            catch
            {
                return Brushes.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
