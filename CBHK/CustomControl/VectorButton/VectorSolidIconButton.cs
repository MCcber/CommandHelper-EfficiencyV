using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorSolidIconButton : Button
    {
        public Geometry ImageData
        {
            get { return (Geometry)GetValue(ImageDataProperty); }
            set { SetValue(ImageDataProperty, value); }
        }

        public static readonly DependencyProperty ImageDataProperty =
            DependencyProperty.Register("ImageData", typeof(Geometry), typeof(VectorSolidIconButton), new PropertyMetadata(default(Geometry)));

        public Brush ImageColorBrush
        {
            get { return (Brush)GetValue(ImageColorBrushProperty); }
            set { SetValue(ImageColorBrushProperty, value); }
        }

        public static readonly DependencyProperty ImageColorBrushProperty =
            DependencyProperty.Register("ImageColorBrush", typeof(Brush), typeof(VectorSolidIconButton), new PropertyMetadata(default(Brush)));
    }
}
