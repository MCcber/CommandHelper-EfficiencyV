using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace cbhk_environment.CustomControls
{

    public class ColorSliders : Slider
    {
        public Brush BackgroundColor
        {
            get { return (Brush)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(Brush), typeof(ColorSliders), new PropertyMetadata(default(Brush)));

        public double HanderWidth
        {
            get { return (double)GetValue(HanderWidthProperty); }
            set { SetValue(HanderWidthProperty, value); }
        }

        public static readonly DependencyProperty HanderWidthProperty =
            DependencyProperty.Register("HanderWidth", typeof(double), typeof(ColorSliders), new PropertyMetadata(default(double)));

        public double HanderHeight
        {
            get { return (double)GetValue(HanderHeightProperty); }
            set { SetValue(HanderHeightProperty, value); }
        }

        public static readonly DependencyProperty HanderHeightProperty =
            DependencyProperty.Register("HanderHeight", typeof(double), typeof(ColorSliders), new PropertyMetadata(default(double)));

        public double BackgroundWidth
        {
            get { return (double)GetValue(BackgroundWidthProperty); }
            set { SetValue(BackgroundWidthProperty, value); }
        }

        public static readonly DependencyProperty BackgroundWidthProperty =
            DependencyProperty.Register("BackgroundWidth", typeof(double), typeof(ColorSliders), new PropertyMetadata(default(double)));

        public double BackgroundHeight
        {
            get { return (double)GetValue(BackgroundHeightProperty); }
            set { SetValue(BackgroundHeightProperty, value); }
        }

        public static readonly DependencyProperty BackgroundHeightProperty =
            DependencyProperty.Register("BackgroundHeight", typeof(double), typeof(ColorSliders), new PropertyMetadata(default(double)));
    }
}
