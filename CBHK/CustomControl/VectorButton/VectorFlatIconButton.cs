using CBHK.Model.Constant;
using System.Windows;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorFlatIconButton : BaseVectorFlatButton
    {
        #region Property
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

        public Thickness IconMargin
        {
            get { return (Thickness)GetValue(IconMarginProperty); }
            set { SetValue(IconMarginProperty, value); }
        }

        public static readonly DependencyProperty IconMarginProperty =
            DependencyProperty.Register("IconMargin", typeof(Thickness), typeof(VectorFlatIconButton), new PropertyMetadata(default(Thickness)));

        public double IconWidth
        {
            get { return (double)GetValue(IconWidthProperty); }
            set { SetValue(IconWidthProperty, value); }
        }

        public static readonly DependencyProperty IconWidthProperty =
            DependencyProperty.Register("IconWidth", typeof(double), typeof(VectorFlatIconButton), new PropertyMetadata(default(double)));

        public double IconHeight
        {
            get { return (double)GetValue(IconHeightProperty); }
            set { SetValue(IconHeightProperty, value); }
        }

        public static readonly DependencyProperty IconHeightProperty =
            DependencyProperty.Register("IconHeight", typeof(double), typeof(VectorFlatIconButton), new PropertyMetadata(default(double)));
        #endregion

        #region Method
        public VectorFlatIconButton()
        {
            SetResourceReference(IconBrushProperty, Theme.CommonForeground);
        }
        #endregion
    }
}
