using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace cbhk_environment.CustomControls
{
    public class IconCheckBoxs:CheckBox
    {
        public BitmapImage ContentImage
        {
            get { return (BitmapImage)GetValue(ContentImageProperty); }
            set { SetValue(ContentImageProperty, value); }
        }

        public static readonly DependencyProperty ContentImageProperty =
            DependencyProperty.Register("ContentImage", typeof(BitmapImage), typeof(IconCheckBoxs), new PropertyMetadata(default(BitmapImage)));

        public double HeaderWidth
        {
            get { return (double)GetValue(HeaderWidthProperty); }
            set { SetValue(HeaderWidthProperty, value); }
        }

        public static readonly DependencyProperty HeaderWidthProperty =
            DependencyProperty.Register("HeaderWidth", typeof(double), typeof(IconCheckBoxs), new PropertyMetadata(default(double)));

        public double HeaderHeight
        {
            get { return (double)GetValue(HeaderHeightProperty); }
            set { SetValue(HeaderHeightProperty, value); }
        }

        public static readonly DependencyProperty HeaderHeightProperty =
            DependencyProperty.Register("HeaderHeight", typeof(double), typeof(IconCheckBoxs), new PropertyMetadata(default(double)));


        public Thickness IconCheckMargin
        {
            get { return (Thickness)GetValue(IconCheckMarginProperty); }
            set { SetValue(IconCheckMarginProperty, value); }
        }

        public static readonly DependencyProperty IconCheckMarginProperty =
            DependencyProperty.Register("IconCheckMargin", typeof(Thickness), typeof(IconCheckBoxs), new PropertyMetadata(default(Thickness)));

        public BitmapImage IconCheckBackgroundImage
        {
            get { return (BitmapImage)GetValue(IconCheckBackgroundImageProperty); }
            set { SetValue(IconCheckBackgroundImageProperty, value); }
        }

        public static readonly DependencyProperty IconCheckBackgroundImageProperty =
            DependencyProperty.Register("IconCheckBackgroundImage", typeof(BitmapImage), typeof(IconCheckBoxs), new PropertyMetadata(default(BitmapImage)));
    }
}
