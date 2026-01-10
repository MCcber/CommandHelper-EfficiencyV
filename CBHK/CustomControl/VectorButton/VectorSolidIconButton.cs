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

        public Brush ImageFillColorBrush
        {
            get { return (Brush)GetValue(ImageFillColorBrushProperty); }
            set { SetValue(ImageFillColorBrushProperty, value); }
        }

        public static readonly DependencyProperty ImageFillColorBrushProperty =
            DependencyProperty.Register("ImageFillColorBrush", typeof(Brush), typeof(VectorSolidIconButton), new PropertyMetadata(default(Brush)));

        public Brush ImageStrokeColorBrush
        {
            get { return (Brush)GetValue(ImageStrokeColorBrushProperty); }
            set { SetValue(ImageStrokeColorBrushProperty, value); }
        }

        public static readonly DependencyProperty ImageStrokeColorBrushProperty =
            DependencyProperty.Register("ImageStrokeColorBrush", typeof(Brush), typeof(VectorSolidIconButton), new PropertyMetadata(default(Brush)));
    }
}
