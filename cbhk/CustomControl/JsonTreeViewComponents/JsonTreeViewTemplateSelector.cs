﻿using System.Windows;
using System.Windows.Controls;

namespace CBHK.CustomControl.JsonTreeViewComponents
{
    public class JsonTreeViewTemplateSelector:DataTemplateSelector
    {
        public DataTemplate JsonTreeViewItemTemplate { get; set; }
        public DataTemplate CompoundJsonTreeViewItemTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is BaseCompoundJsonTreeViewItem)
                return CompoundJsonTreeViewItemTemplate;
            return JsonTreeViewItemTemplate;
        }
    }
}
