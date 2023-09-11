using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace cbhk_environment.CustomControls
{
    public class TextButtons:Button
    {
        public BitmapImage ThicknessBackground
        {
            get { return (BitmapImage)GetValue(ThicknessBackgroundProperty); }
            set { SetValue(ThicknessBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThicknessBackgroundProperty =
            DependencyProperty.Register("ThicknessBackground", typeof(BitmapImage), typeof(TextButtons), new PropertyMetadata(default(BitmapImage)));

        public Brush MouseOverBackground
        {
            get { return (Brush)GetValue(MouseOverBackgroundProperty); }
            set { SetValue(MouseOverBackgroundProperty, value); }
        }

        public static readonly DependencyProperty MouseOverBackgroundProperty =
            DependencyProperty.Register("MouseOverBackground", typeof(Brush), typeof(TextButtons), new PropertyMetadata(default(Brush)));

        public Brush MouseLeftDownBackground
        {
            get { return (Brush)GetValue(MouseLeftDownBackgroundProperty); }
            set { SetValue(MouseLeftDownBackgroundProperty, value); }
        }

        public static readonly DependencyProperty MouseLeftDownBackgroundProperty =
            DependencyProperty.Register("MouseLeftDownBackground", typeof(Brush), typeof(TextButtons), new PropertyMetadata(default(Brush)));

        public Brush MouseOverBorderBrush
        {
            get { return (Brush)GetValue(MouseOverBorderBrushProperty); }
            set { SetValue(MouseOverBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty MouseOverBorderBrushProperty =
            DependencyProperty.Register("MouseOverBorderBrush", typeof(Brush), typeof(TextButtons), new PropertyMetadata(default(Brush)));
    }
}
