using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorSolidIconButton : Button
    {
        #region Field
        private Brush OriginIconFillColor;
        private Brush OriginIconStrokeColor;
        #endregion

        #region Property
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

        public Brush ThemeBackground
        {
            get { return (Brush)GetValue(ThemeBackgroundProperty); }
            set { SetValue(ThemeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeBackgroundProperty =
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(VectorSolidIconButton), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorSolidIconButton()
        {
            SetResourceReference(ThemeBackgroundProperty, Theme.CommonBackground);
            Loaded += VectorSolidIconButton_Loaded;
        }

        public void UpdateBorderColorByBackgroundColor()
        {
            if(ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.4f));
                IconFillColor = OriginIconFillColor = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.4f));
                IconStrokeColor = OriginIconStrokeColor = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.4f));
            }
        }
        #endregion

        #region Event
        private void VectorSolidIconButton_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateBorderColorByBackgroundColor();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if(e.Property == ThemeBackgroundProperty)
            {
                UpdateBorderColorByBackgroundColor();
            }
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            //if (OriginIconFillColor is SolidColorBrush originIconFillColor)
            //{
            //    IconFillColor = new SolidColorBrush(ColorTool.Darken(originIconFillColor.Color, 0.2f));
            //}
            //if (OriginIconStrokeColor is SolidColorBrush originIconStrokeColor)
            //{
            //    IconStrokeColor = new SolidColorBrush(ColorTool.Darken(originIconStrokeColor.Color, 0.2f));
            //}
            Tag = "Hover";
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            //IconFillColor = OriginIconFillColor;
            //IconStrokeColor = OriginIconStrokeColor;
            Tag = null;
        } 
        #endregion
    }
}