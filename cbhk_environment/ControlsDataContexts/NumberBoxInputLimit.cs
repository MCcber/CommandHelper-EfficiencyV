using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace cbhk_environment.ControlsDataContexts
{
    public static class NumberBoxInputLimit
    {
        public static string GetDecimalValueProxy(TextBox obj) => (string)obj.GetValue(DecimalValueProxyProperty);

        public static void SetDecimalValueProxy(TextBox obj, string value) => obj.SetValue(DecimalValueProxyProperty, value);

        public static readonly DependencyProperty DecimalValueProxyProperty =
            DependencyProperty.RegisterAttached("DecimalValueProxy", typeof(string), typeof(NumberBoxInputLimit),
                new FrameworkPropertyMetadata("0", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, null, CoerceDecimalValueProxy));

        private static object CoerceDecimalValueProxy(DependencyObject d, object baseValue)
        {
            if (decimal.TryParse(baseValue as string, out _)) return baseValue;
            return DependencyProperty.UnsetValue;
        }
    }
}
