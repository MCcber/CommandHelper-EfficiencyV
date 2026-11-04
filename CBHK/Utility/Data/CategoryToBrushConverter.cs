using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CBHK.Utility.Data
{
    /// <summary>misode getCategory 的分类 → 行左侧色条（无类别返回透明）。</summary>
    public sealed class CategoryToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var category = System.Convert.ToString(value);
            return category switch
            {
                // misode nodes.css：--category-*（照抄，不要另调色）
                "pool" => new SolidColorBrush(Color.FromRgb(0x76, 0xB8, 0x65)),
                "predicate" => new SolidColorBrush(Color.FromRgb(0x65, 0xB5, 0xB8)),
                "function" => new SolidColorBrush(Color.FromRgb(0x97, 0x9F, 0xA7)),
                _ => Brushes.Transparent,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
