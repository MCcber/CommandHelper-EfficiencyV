using System;
using System.Windows;
using System.Windows.Controls;

namespace CBHK.Utility.Visual
{
    public static class TextSizeHelper
    {
        // 定义一个开关属性：AutoFontSize
        public static readonly DependencyProperty AutoFontSizeProperty =
            DependencyProperty.RegisterAttached("AutoFontSize", typeof(bool), typeof(TextSizeHelper),
                new PropertyMetadata(false, OnAutoFontSizeChanged));

        public static void SetAutoFontSize(DependencyObject obj, bool value) => obj.SetValue(AutoFontSizeProperty, value);
        public static bool GetAutoFontSize(DependencyObject obj) => (bool)obj.GetValue(AutoFontSizeProperty);

        private static void OnAutoFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element && (bool)e.NewValue)
            {
                element.SizeChanged += (s, args) => UpdateFontSize(element);
            }
        }

        private static void UpdateFontSize(FrameworkElement element)
        {
            // 获取实际可用高度
            double actualHeight = element.ActualHeight;
            double paddingTop = 0;
            double paddingBottom = 0;

            // 尝试获取 Padding (TextBlock 有 Padding，但它不是 Control)
            if (element is Control ctrl)
            {
                paddingTop = ctrl.Padding.Top;
                paddingBottom = ctrl.Padding.Bottom;
            }
            else if (element is TextBlock tb)
            {
                paddingTop = tb.Padding.Top;
                paddingBottom = tb.Padding.Bottom;
            }

            double availableHeight = actualHeight - paddingTop - paddingBottom;

            if (availableHeight > 0)
            {
                double newSize = Math.Max(1, availableHeight * 0.5);

                // 统一设置字号
                if (element is Control c)
                {
                    if (Math.Abs(c.FontSize - newSize) > 0.5)
                    {
                        c.FontSize = newSize;
                    }
                }
                else if (element is TextBlock t)
                {
                    if (Math.Abs(t.FontSize - newSize) > 0.5)
                    {
                        t.FontSize = newSize;
                    }
                }
            }
        }
    }
}
