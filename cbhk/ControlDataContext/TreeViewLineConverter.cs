﻿using CBHK.CustomControl;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Shapes;

namespace CBHK.ControlDataContext
{
    public class TreeViewLineConverter:IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double height = (double) values[0];

            RichTreeViewItems item = values[2] as RichTreeViewItems;
            ItemsControl ic = ItemsControl.ItemsControlFromItemContainer(item);
            if (ic is null) return null;
            bool isLastOne = ic.ItemContainerGenerator.IndexFromContainer(item) == ic.Items.Count - 1;

            Rectangle rectangle = values[3] as Rectangle;
            if (isLastOne)
            {                
                rectangle.VerticalAlignment = VerticalAlignment.Top;
                return 20.0;
            }
            else
            {
                rectangle.VerticalAlignment = VerticalAlignment.Stretch;
                return double.NaN;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
