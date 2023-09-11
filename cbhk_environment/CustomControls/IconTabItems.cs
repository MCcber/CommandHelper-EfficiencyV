using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace cbhk_environment.CustomControls
{
    public class IconTabItems:TabItem
    {
        public BitmapImage HeaderImage
        {
            get { return (BitmapImage)GetValue(HeaderImageProperty); }
            set { SetValue(HeaderImageProperty, value); }
        }

        public static readonly DependencyProperty HeaderImageProperty =
            DependencyProperty.Register("HeaderImage", typeof(BitmapImage), typeof(IconTabItems), new PropertyMetadata(default(BitmapImage)));

        public string HeaderText
        {
            get { return (string)GetValue(HeaderTextProperty); }
            set { SetValue(HeaderTextProperty, value); }
        }

        public static readonly DependencyProperty HeaderTextProperty =
            DependencyProperty.Register("HeaderText", typeof(string), typeof(IconTabItems), new PropertyMetadata(default(string)));

        public double ImageWidth
        {
            get { return (double)GetValue(ImageWidthProperty); }
            set { SetValue(ImageWidthProperty, value); }
        }

        public static readonly DependencyProperty ImageWidthProperty =
            DependencyProperty.Register("ImageWidth", typeof(double), typeof(IconTabItems), new PropertyMetadata(default(double)));

        public double ImageHeight
        {
            get { return (double)GetValue(ImageHeightProperty); }
            set { SetValue(ImageHeightProperty, value); }
        }

        public static readonly DependencyProperty ImageHeightProperty =
            DependencyProperty.Register("ImageHeight", typeof(double), typeof(IconTabItems), new PropertyMetadata(default(double)));

        public Thickness ImageMargin
        {
            get { return (Thickness)GetValue(ImageMarginProperty); }
            set { SetValue(ImageMarginProperty, value); }
        }

        public static readonly DependencyProperty ImageMarginProperty =
            DependencyProperty.Register("ImageMargin", typeof(Thickness), typeof(IconTabItems), new PropertyMetadata(default(Thickness)));

        public Thickness TextMargin
        {
            get { return (Thickness)GetValue(TextMarginProperty); }
            set { SetValue(TextMarginProperty, value); }
        }

        public static readonly DependencyProperty TextMarginProperty =
            DependencyProperty.Register("TextMargin", typeof(Thickness), typeof(IconTabItems), new PropertyMetadata(default(Thickness)));

        public Brush Selectedbackground
        {
            get { return (Brush)GetValue(SelectedbackgroundProperty); }
            set { SetValue(SelectedbackgroundProperty, value); }
        }

        public static readonly DependencyProperty SelectedbackgroundProperty =
            DependencyProperty.Register("Selectedbackground", typeof(Brush), typeof(IconTabItems), new PropertyMetadata(default(Brush)));
    }
}
