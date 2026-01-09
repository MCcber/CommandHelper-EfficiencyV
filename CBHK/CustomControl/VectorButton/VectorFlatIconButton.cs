using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorFlatIconButton : Button
    {
        public Geometry Icon
        {
            get { return (Geometry)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(Geometry), typeof(VectorFlatIconButton), new PropertyMetadata(default(Geometry)));

        public Brush IconBrush
        {
            get { return (Brush)GetValue(IconBrushProperty); }
            set { SetValue(IconBrushProperty, value); }
        }

        public static readonly DependencyProperty IconBrushProperty =
            DependencyProperty.Register("IconBrush", typeof(Brush), typeof(VectorFlatIconButton), new PropertyMetadata(default(Geometry)));
    }
}
