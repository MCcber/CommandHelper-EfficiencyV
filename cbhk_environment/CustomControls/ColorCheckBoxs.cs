using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace cbhk_environment.CustomControls
{
    public class ColorCheckBoxs:CheckBox
    {
        public Brush ContentColor
        {
            get { return (Brush)GetValue(ContentColorProperty); }
            set { SetValue(ContentColorProperty, value); }
        }

        public static readonly DependencyProperty ContentColorProperty =
            DependencyProperty.Register("ContentColor", typeof(Brush), typeof(ColorCheckBoxs), new PropertyMetadata(default(Brush)));

        public double HeaderWidth
        {
            get { return (double)GetValue(HeaderWidthProperty); }
            set { SetValue(HeaderWidthProperty, value); }
        }

        public static readonly DependencyProperty HeaderWidthProperty =
            DependencyProperty.Register("HeaderWidth", typeof(double), typeof(ColorCheckBoxs), new PropertyMetadata(default(double)));

        public double HeaderHeight
        {
            get { return (double)GetValue(HeaderHeightProperty); }
            set { SetValue(HeaderHeightProperty, value); }
        }

        public static readonly DependencyProperty HeaderHeightProperty =
            DependencyProperty.Register("HeaderHeight", typeof(double), typeof(ColorCheckBoxs), new PropertyMetadata(default(double)));
    }
}
