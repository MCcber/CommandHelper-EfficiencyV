using CBHK.CustomControl.Container;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Shapes;

namespace CBHK.Utility.Visual.TreeView
{
    public class TreeViewLineConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[2] 是当前的 TreeViewItem
            if (values[2] is not VectorTreeViewItem item)
            {
                return null;
            }

            // 如果没有子节点，不绘制垂线（高度为0）
            if (!item.HasItems || !item.IsExpanded)
            {
                return 0.0;
            }

            // 有子节点：让垂线自动拉伸填充整个子节点区域
            var rectangle = values[3] as Rectangle;
            if (rectangle is not null)
            {
                rectangle.VerticalAlignment = VerticalAlignment.Stretch;
            }

            return double.NaN;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}