using System.Windows;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorFlatIconTextButton : VectorFlatTextButton
    {
        #region Property
        public double IconWidth
        {
            get { return (double)GetValue(IconWidthProperty); }
            set { SetValue(IconWidthProperty, value); }
        }

        public static readonly DependencyProperty IconWidthProperty =
            DependencyProperty.Register("IconWidth", typeof(double), typeof(VectorFlatIconTextButton), new PropertyMetadata(default(double)));

        public double IconHeight
        {
            get { return (double)GetValue(IconHeightProperty); }
            set { SetValue(IconHeightProperty, value); }
        }

        public static readonly DependencyProperty IconHeightProperty =
            DependencyProperty.Register("IconHeight", typeof(double), typeof(VectorFlatIconTextButton), new PropertyMetadata(default(double)));

        public Geometry Icon
        {
            get { return (Geometry)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(Geometry), typeof(VectorFlatIconTextButton), new PropertyMetadata(default(Geometry)));

        public Brush IconFillBrush
        {
            get { return (Brush)GetValue(IconFillBrushProperty); }
            set { SetValue(IconFillBrushProperty, value); }
        }

        public static readonly DependencyProperty IconFillBrushProperty =
            DependencyProperty.Register("IconFillBrush", typeof(Brush), typeof(VectorFlatIconTextButton), new PropertyMetadata(default(Brush)));

        public Brush IconStrokeBrush
        {
            get { return (Brush)GetValue(IconStrokeBrushProperty); }
            set { SetValue(IconStrokeBrushProperty, value); }
        }

        public static readonly DependencyProperty IconStrokeBrushProperty =
            DependencyProperty.Register("IconStrokeBrush", typeof(Brush), typeof(VectorFlatIconTextButton), new PropertyMetadata(default(Brush)));

        public double IconStrokeThickness
        {
            get { return (double)GetValue(IconStrokeThicknessProperty); }
            set { SetValue(IconStrokeThicknessProperty, value); }
        }

        public static readonly DependencyProperty IconStrokeThicknessProperty =
            DependencyProperty.Register("IconStrokeThickness", typeof(double), typeof(VectorFlatIconTextButton), new PropertyMetadata(default(double)));

        public Thickness IconMargin
        {
            get { return (Thickness)GetValue(IconMarginProperty); }
            set { SetValue(IconMarginProperty, value); }
        }

        public static readonly DependencyProperty IconMarginProperty =
            DependencyProperty.Register("IconMargin", typeof(Thickness), typeof(VectorFlatIconTextButton), new PropertyMetadata(default(Thickness)));
        #endregion
    }
}
