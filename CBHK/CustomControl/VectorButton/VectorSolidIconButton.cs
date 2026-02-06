using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    #region Field

    #endregion

    #region Property
    public class VectorSolidIconButton : Button
    {
        public Geometry Icon
        {
            get { return (Geometry)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(Geometry), typeof(VectorSolidIconButton), new PropertyMetadata(default(Geometry)));

        public Brush IconFillColor
        {
            get { return (Brush)GetValue(IconFillColorProperty); }
            set { SetValue(IconFillColorProperty, value); }
        }

        public static readonly DependencyProperty IconFillColorProperty =
            DependencyProperty.Register("IconFillColor", typeof(Brush), typeof(VectorSolidIconButton), new PropertyMetadata(default(Brush)));

        public Brush IconStrokeColor
        {
            get { return (Brush)GetValue(IconStrokeColorProperty); }
            set { SetValue(IconStrokeColorProperty, value); }
        }

        public static readonly DependencyProperty IconStrokeColorProperty =
            DependencyProperty.Register("IconStrokeColor", typeof(Brush), typeof(VectorSolidIconButton), new PropertyMetadata(default(Brush)));

        public Thickness IconMargin
        {
            get { return (Thickness)GetValue(IconMarginProperty); }
            set { SetValue(IconMarginProperty, value); }
        }

        public static readonly DependencyProperty IconMarginProperty =
            DependencyProperty.Register("IconMargin", typeof(Thickness), typeof(VectorSolidIconButton), new PropertyMetadata(default(Thickness)));

        public double IconWidth
        {
            get { return (double)GetValue(IconWidthProperty); }
            set { SetValue(IconWidthProperty, value); }
        }

        public static readonly DependencyProperty IconWidthProperty =
            DependencyProperty.Register("IconWidth", typeof(double), typeof(VectorSolidIconButton), new PropertyMetadata(default(double)));

        public double IconHeight
        {
            get { return (double)GetValue(IconHeightProperty); }
            set { SetValue(IconHeightProperty, value); }
        }

        public static readonly DependencyProperty IconHeightProperty =
            DependencyProperty.Register("IconHeight", typeof(double), typeof(VectorSolidIconButton), new PropertyMetadata(default(double)));
    }
    #endregion

    #region Method

    #endregion

    #region Event

    #endregion
}
