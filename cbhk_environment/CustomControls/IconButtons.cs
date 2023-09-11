using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace cbhk_environment.CustomControls
{
    public class IconButtons:Button
    {
        public Geometry IconData
        {
            get { return (Geometry)GetValue(IconTextButtons.IconDataProperty); }
            set { SetValue(IconTextButtons.IconDataProperty, value); }
        }

        public static readonly DependencyProperty IconDataProperty =
    DependencyProperty.Register("IconData", typeof(Geometry), typeof(IconButtons), new PropertyMetadata(default(Geometry)));

        public Brush IconColor
        {
            get { return (Brush)GetValue(IconTextButtons.IconColorProperty); }
            set { SetValue(IconTextButtons.IconColorProperty, value); }
        }

        public static readonly DependencyProperty IconColorProperty =
DependencyProperty.Register("IconColor", typeof(Brush), typeof(IconButtons), new PropertyMetadata(default(Brush)));

        public Thickness IconMargin
        {
            get { return (Thickness)GetValue(IconTextButtons.IconMarginProperty); }
            set { SetValue(IconTextButtons.IconMarginProperty, value); }
        }

        public static readonly DependencyProperty IconMarginProperty =
DependencyProperty.Register("IconMargin", typeof(Thickness), typeof(IconButtons), new PropertyMetadata(default(Thickness)));

        public double IconWidth
        {
            get { return (double)GetValue(IconTextButtons.IconWidthProperty); }
            set { SetValue(IconTextButtons.IconWidthProperty, value); }
        }

        public static readonly DependencyProperty IconWidthProperty =
DependencyProperty.Register("IconWidth", typeof(double), typeof(IconButtons), new PropertyMetadata(default(double)));

        public double IconHeight
        {
            get { return (double)GetValue(IconTextButtons.IconHeightProperty); }
            set { SetValue(IconTextButtons.IconHeightProperty, value); }
        }

        public static readonly DependencyProperty IconHeightProperty =
DependencyProperty.Register("IconHeight", typeof(double), typeof(IconButtons), new PropertyMetadata(default(double)));

        public ImageBrush PressedBackground
        {
            get { return (ImageBrush)GetValue(IconTextButtons.PressedBackgroundProperty); }
            set { SetValue(IconTextButtons.PressedBackgroundProperty, value); }
        }

        public static readonly DependencyProperty PressedBackgroundProperty =
DependencyProperty.Register("PressedBackground", typeof(ImageBrush), typeof(IconButtons), new PropertyMetadata(default(ImageBrush)));
    }
}
