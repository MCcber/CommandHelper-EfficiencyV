using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace cbhk_environment.CustomControls
{
    public class RichCheckBoxs:CheckBox
    {
        public Thickness TextMargin
        {
            get { return (Thickness)GetValue(TextMarginProperty); }
            set { SetValue(TextMarginProperty, value); }
        }

        public static readonly DependencyProperty TextMarginProperty =
            DependencyProperty.Register("TextMargin", typeof(Thickness), typeof(RichCheckBoxs), new PropertyMetadata(default(Thickness)));

        public Thickness ColorMargin
        {
            get { return (Thickness)GetValue(ColorMarginProperty); }
            set { SetValue(ColorMarginProperty, value); }
        }

        public static readonly DependencyProperty ColorMarginProperty =
            DependencyProperty.Register("ColorMargin", typeof(Thickness), typeof(RichCheckBoxs), new PropertyMetadata(default(Thickness)));

        public Thickness ImageMargin
        {
            get { return (Thickness)GetValue(ImageMarginProperty); }
            set { SetValue(ImageMarginProperty, value); }
        }

        public static readonly DependencyProperty ImageMarginProperty =
            DependencyProperty.Register("ImageMargin", typeof(Thickness), typeof(RichCheckBoxs), new PropertyMetadata(default(Thickness)));

        public Visibility ColorVisibility
        {
            get { return (Visibility)GetValue(ColorVisibilityProperty); }
            set { SetValue(ColorVisibilityProperty, value); }
        }

        public static readonly DependencyProperty ColorVisibilityProperty =
            DependencyProperty.Register("ColorVisibility", typeof(Visibility), typeof(RichCheckBoxs), new PropertyMetadata(default(Visibility)));

        public Visibility TextVisibility
        {
            get { return (Visibility)GetValue(TextVisibilityProperty); }
            set { SetValue(TextVisibilityProperty, Visibility); }
        }

        public static readonly DependencyProperty TextVisibilityProperty =
            DependencyProperty.Register("TextVisibility", typeof(Visibility), typeof(RichCheckBoxs), new PropertyMetadata(default(Visibility)));

        public Visibility ImageVisibility
        {
            get { return (Visibility)GetValue(ImageVisibilityProperty); }
            set { SetValue(ImageVisibilityProperty, value); }
        }

        public static readonly DependencyProperty ImageVisibilityProperty =
            DependencyProperty.Register("ImageVisibility", typeof(Visibility), typeof(RichCheckBoxs), new PropertyMetadata(default(Visibility)));

        public Brush ContentColor
        {
            get { return (Brush)GetValue(ContentColorProperty); }
            set { SetValue(ContentColorProperty, value); }
        }

        public static readonly DependencyProperty ContentColorProperty =
            DependencyProperty.Register("ContentColor", typeof(Brush), typeof(RichCheckBoxs), new PropertyMetadata(default(Brush)));

        public BitmapImage ContentImage
        {
            get { return (BitmapImage)GetValue(ContentImageProperty); }
            set { SetValue(ContentImageProperty, value); }
        }

        public static readonly DependencyProperty ContentImageProperty =
            DependencyProperty.Register("ContentImage", typeof(BitmapImage), typeof(RichCheckBoxs), new PropertyMetadata(default(BitmapImage)));

        public string HeaderText
        {
            get { return (string)GetValue(HeaderTextProperty); }
            set { SetValue(HeaderTextProperty, value); }
        }

        public static readonly DependencyProperty HeaderTextProperty =
            DependencyProperty.Register("HeaderText", typeof(string), typeof(RichCheckBoxs), new PropertyMetadata(default(string)));

        public double HeaderWidth
        {
            get { return (double)GetValue(HeaderWidthProperty); }
            set { SetValue(HeaderWidthProperty, value); }
        }

        public static readonly DependencyProperty HeaderWidthProperty =
            DependencyProperty.Register("HeaderWidth", typeof(double), typeof(RichCheckBoxs), new PropertyMetadata(default(double)));

        public double HeaderHeight
        {
            get { return (double)GetValue(HeaderHeightProperty); }
            set { SetValue(HeaderHeightProperty, value); }
        }

        public static readonly DependencyProperty HeaderHeightProperty =
            DependencyProperty.Register("HeaderHeight", typeof(double), typeof(RichCheckBoxs), new PropertyMetadata(default(double)));

        public double ImageWidth
        {
            get { return (double)GetValue(ImageWidthProperty); }
            set { SetValue(ImageWidthProperty, value); }
        }

        public static readonly DependencyProperty ImageWidthProperty =
            DependencyProperty.Register("ImageWidth", typeof(double), typeof(RichCheckBoxs), new PropertyMetadata(default(double)));

        public double ImageHeight
        {
            get { return (double)GetValue(ImageHeightProperty); }
            set { SetValue(ImageHeightProperty, value); }
        }

        public static readonly DependencyProperty ImageHeightProperty =
            DependencyProperty.Register("ImageHeight", typeof(double), typeof(RichCheckBoxs), new PropertyMetadata(default(double)));
    }
}
