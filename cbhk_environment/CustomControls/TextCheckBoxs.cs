using System.Windows;
using System.Windows.Controls;

namespace cbhk_environment.CustomControls
{
    public class TextCheckBoxs:CheckBox
    {
        public string HeaderText
        {
            get { return (string)GetValue(HeaderTextProperty); }
            set { SetValue(HeaderTextProperty, value); }
        }

        public static readonly DependencyProperty HeaderTextProperty =
            DependencyProperty.Register("HeaderText", typeof(string), typeof(TextCheckBoxs), new PropertyMetadata(default(string)));

        public double HeaderWidth
        {
            get { return (double)GetValue(HeaderWidthProperty); }
            set { SetValue(HeaderWidthProperty, value); }
        }

        public static readonly DependencyProperty HeaderWidthProperty =
            DependencyProperty.Register("HeaderWidth", typeof(double), typeof(TextCheckBoxs), new PropertyMetadata(default(double)));

        public double HeaderHeight
        {
            get { return (double)GetValue(HeaderHeightProperty); }
            set { SetValue(HeaderHeightProperty, value); }
        }

        public static readonly DependencyProperty HeaderHeightProperty =
            DependencyProperty.Register("HeaderHeight", typeof(double), typeof(TextCheckBoxs), new PropertyMetadata(default(double)));

        public Thickness TextCheckMargin
        {
            get { return (Thickness)GetValue(TextCheckMarginProperty); }
            set { SetValue(TextCheckMarginProperty, value); }
        }

        public static readonly DependencyProperty TextCheckMarginProperty =
            DependencyProperty.Register("TextCheckMargin", typeof(Thickness), typeof(TextCheckBoxs), new PropertyMetadata(default(Thickness)));
    }
}
