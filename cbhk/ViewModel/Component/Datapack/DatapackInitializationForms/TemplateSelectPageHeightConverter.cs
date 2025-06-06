﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace CBHK.ViewModel.Component.Datapack.DatapackInitializationForms
{
    public class TemplateSelectPageHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double height)
            {
                return height - 220;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
