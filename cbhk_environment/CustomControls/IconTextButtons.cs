using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace cbhk_environment.CustomControls
{
    public class IconTextButtons:Button
    {
        public Geometry IconData
        {
            get { return (Geometry)GetValue(IconDataProperty); }
            set { SetValue(IconDataProperty, value); }
        }

        public static readonly DependencyProperty IconDataProperty =
            DependencyProperty.Register("IconData", typeof(Geometry), typeof(IconTextButtons), new PropertyMetadata(default(Geometry)));

        public string ContentData
        {
            get { return (string)GetValue(ContentDataProperty); }
            set { SetValue(ContentDataProperty, value); }
        }

        public static readonly DependencyProperty ContentDataProperty =
            DependencyProperty.Register("ContentData", typeof(string), typeof(IconTextButtons), new PropertyMetadata(default(Geometry)));

        public Brush IconColor
        {
            get { return (Brush)GetValue(IconColorProperty); }
            set { SetValue(IconColorProperty, value); }
        }

        public static readonly DependencyProperty IconColorProperty =
            DependencyProperty.Register("IconColor", typeof(Brush), typeof(IconTextButtons), new PropertyMetadata(default(Brush)));

        public Thickness IconMargin
        {
            get { return (Thickness)GetValue(IconMarginProperty); }
            set { SetValue(IconMarginProperty, value); }
        }

        public static readonly DependencyProperty IconMarginProperty =
            DependencyProperty.Register("IconMargin", typeof(Thickness), typeof(IconTextButtons), new PropertyMetadata(default(Thickness)));

        public Thickness ContentMargin
        {
            get { return (Thickness)GetValue(ContentMarginProperty); }
            set { SetValue(ContentMarginProperty, value); }
        }

        public static readonly DependencyProperty ContentMarginProperty =
            DependencyProperty.Register("ContentMargin", typeof(Thickness), typeof(IconTextButtons), new PropertyMetadata(default(Thickness)));

        public Brush ContentColor
        {
            get { return (Brush)GetValue(ContentColorProperty); }
            set { SetValue(ContentColorProperty, value); }
        }

        public static readonly DependencyProperty ContentColorProperty =
            DependencyProperty.Register("ContentColor", typeof(Brush), typeof(IconTextButtons), new PropertyMetadata(default(Brush)));

        public double IconWidth
        {
            get { return (double)GetValue(IconWidthProperty); }
            set { SetValue(IconWidthProperty, value); }
        }

        public static readonly DependencyProperty IconWidthProperty =
            DependencyProperty.Register("IconWidth", typeof(double), typeof(IconTextButtons), new PropertyMetadata(default(double)));

        public double IconHeight
        {
            get { return (double)GetValue(IconHeightProperty); }
            set { SetValue(IconHeightProperty, value); }
        }

        public static readonly DependencyProperty IconHeightProperty =
            DependencyProperty.Register("IconHeight", typeof(double), typeof(IconTextButtons), new PropertyMetadata(default(double)));

        public double ContentFontSize
        {
            get { return (double)GetValue(ContentFontSizeProperty); }
            set { SetValue(ContentFontSizeProperty, value); }
        }

        public static readonly DependencyProperty ContentFontSizeProperty =
            DependencyProperty.Register("ContentFontSize", typeof(double), typeof(IconTextButtons), new PropertyMetadata(default(double)));

        public double ContentWidth
        {
            get { return (double)GetValue(ContentWidthProperty); }
            set { SetValue(ContentWidthProperty, value); }
        }

        public static readonly DependencyProperty ContentWidthProperty =
            DependencyProperty.Register("ContentWidth", typeof(double), typeof(IconTextButtons), new PropertyMetadata(default(double)));

        public double ContentHeight
        {
            get { return (double)GetValue(ContentHeightProperty); }
            set { SetValue(ContentHeightProperty, value); }
        }

        public static readonly DependencyProperty ContentHeightProperty =
            DependencyProperty.Register("ContentHeight", typeof(double), typeof(IconTextButtons), new PropertyMetadata(default(double)));

        public ImageBrush PressedBackground
        {
            get { return (ImageBrush)GetValue(PressedBackgroundProperty); }
            set { SetValue(PressedBackgroundProperty, value); }
        }

        public static readonly DependencyProperty PressedBackgroundProperty =
            DependencyProperty.Register("PressedBackground", typeof(ImageBrush), typeof(IconTextButtons), new PropertyMetadata(default(ImageBrush)));

        public bool NeedMouseOverStyle
        {
            get { return (bool)GetValue(NeedMouseOverStyleProperty); }
            set { SetValue(NeedMouseOverStyleProperty, value); }
        }

        public static readonly DependencyProperty NeedMouseOverStyleProperty =
            DependencyProperty.Register("NeedMouseOverStyle", typeof(bool), typeof(IconTextButtons), new PropertyMetadata(default(bool)));
    }
}
