using CBHK.Utility.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorFlatIconButton : Button
    {
        #region Field
        private Brush OriginBackground;
        private Brush OriginLeftTopBorderBrush;
        private Brush OriginRightBottomBorderBrush;
        private Brush OriginBorderCornerBrush;
        #endregion

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

        public double IconWidh
        {
            get { return (double)GetValue(IconWidhProperty); }
            set { SetValue(IconWidhProperty, value); }
        }

        public static readonly DependencyProperty IconWidhProperty =
            DependencyProperty.Register("IconWidh", typeof(double), typeof(VectorFlatIconButton), new PropertyMetadata(default(double)));

        public double IconHeight
        {
            get { return (double)GetValue(IconHeightProperty); }
            set { SetValue(IconHeightProperty, value); }
        }

        public static readonly DependencyProperty IconHeightProperty =
            DependencyProperty.Register("IconHeight", typeof(double), typeof(VectorFlatIconButton), new PropertyMetadata(default(double)));

        public Brush LeftTopBorderBrush
        {
            get { return (Brush)GetValue(LeftTopBorderBrushProperty); }
            set { SetValue(LeftTopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftTopBorderBrushProperty =
            DependencyProperty.Register("LeftTopBorderBrush", typeof(Brush), typeof(VectorFlatIconButton), new PropertyMetadata(default(Brush)));

        public Brush RightBottomBorderBrush
        {
            get { return (Brush)GetValue(RightBottomBorderBrushProperty); }
            set { SetValue(RightBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RightBottomBorderBrushProperty =
            DependencyProperty.Register("RightBottomBorderBrush", typeof(Brush), typeof(VectorFlatIconButton), new PropertyMetadata(default(Brush)));

        public Brush BorderCornerBrush
        {
            get { return (Brush)GetValue(BorderCornerBrushProperty); }
            set { SetValue(BorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderCornerBrushProperty =
            DependencyProperty.Register("BorderCornerBrush", typeof(Brush), typeof(VectorFlatIconButton), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorFlatIconButton()
        {
            Loaded += VectorFlatIconButton_Loaded;
            MouseEnter += VectorFlatIconButton_MouseEnter;
            MouseLeave += VectorFlatIconButton_MouseLeave;
            PreviewMouseLeftButtonDown += VectorFlatIconButton_PreviewMouseLeftButtonDown;
            PreviewMouseLeftButtonUp += VectorFlatIconButton_PreviewMouseLeftButtonUp;
        }

        private void UpdateBorderColorByBackgroundColor()
        {
            var foregroundSource = DependencyPropertyHelper.GetValueSource(this, ForegroundProperty);
            if (foregroundSource.BaseValueSource is BaseValueSource.DefaultStyle || foregroundSource.BaseValueSource is BaseValueSource.Style)
            {
                Foreground = Brushes.White;
            }
            var backgroundSource = DependencyPropertyHelper.GetValueSource(this, BackgroundProperty);
            if (backgroundSource.BaseValueSource is BaseValueSource.DefaultStyle || backgroundSource.BaseValueSource is BaseValueSource.Style)
            {
                Background = new BrushConverter().ConvertFromString("#48494A") as Brush;
            }
            var borderBrushSource = DependencyPropertyHelper.GetValueSource(this, BorderBrushProperty);
            if (borderBrushSource.BaseValueSource is BaseValueSource.DefaultStyle || borderBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                BorderBrush = Brushes.Black;
            }
            var originLeftTopBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, LeftTopBorderBrushProperty);
            if (originLeftTopBorderBrushSource.BaseValueSource is BaseValueSource.Default || originLeftTopBorderBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.2f);
                LeftTopBorderBrush = new SolidColorBrush(color);
            }
            var originRightBottomBrushSource = DependencyPropertyHelper.GetValueSource(this, RightBottomBorderBrushProperty);
            if (originRightBottomBrushSource.BaseValueSource is BaseValueSource.Default || originRightBottomBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                Color color = ColorTool.Darken((Background as SolidColorBrush).Color, 0.4f);
                RightBottomBorderBrush = new SolidColorBrush(color);
            }
            var originCornerBrushSource = DependencyPropertyHelper.GetValueSource(this, BorderCornerBrushProperty);
            if (originCornerBrushSource.BaseValueSource is BaseValueSource.Default || originCornerBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                Color color = ColorTool.Darken((Background as SolidColorBrush).Color, 0.2f);
                BorderCornerBrush = new SolidColorBrush(color);
            }

            if (OriginBackground is null && Background is not null)
            {
                OriginBackground = Background;
            }
            if (OriginLeftTopBorderBrush is null && LeftTopBorderBrush is not null)
            {
                OriginLeftTopBorderBrush = LeftTopBorderBrush;
            }
            if (OriginRightBottomBorderBrush is null && RightBottomBorderBrush is not null)
            {
                OriginRightBottomBorderBrush = RightBottomBorderBrush;
            }
            if (OriginBorderCornerBrush is null && BorderCornerBrush is not null)
            {
                OriginBorderCornerBrush = BorderCornerBrush;
            }
        }
        #endregion

        #region Event
        private void VectorFlatIconButton_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateBorderColorByBackgroundColor();
        }

        private void VectorFlatIconButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            VectorFlatIconButton_MouseEnter(sender, null);
        }

        private void VectorFlatIconButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Color lightBackgroundColor = ColorTool.Darken((OriginBackground as SolidColorBrush).Color, 0.2f);
            Background = new SolidColorBrush(lightBackgroundColor);
            Color lightLeftTopBorderColor = ColorTool.Darken((OriginLeftTopBorderBrush as SolidColorBrush).Color, 0.4f);
            LeftTopBorderBrush = new SolidColorBrush(lightLeftTopBorderColor);
            Color lightRightBottomBorderColor = ColorTool.Darken((RightBottomBorderBrush as SolidColorBrush).Color, 0.2f);
            RightBottomBorderBrush = new SolidColorBrush(lightRightBottomBorderColor);
            Color lightCornerColor = ColorTool.Darken((OriginBorderCornerBrush as SolidColorBrush).Color, 0.2f);
            BorderCornerBrush = new SolidColorBrush(lightCornerColor);
        }

        private void VectorFlatIconButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Background = OriginBackground;
            LeftTopBorderBrush = OriginLeftTopBorderBrush;
            RightBottomBorderBrush = OriginRightBottomBorderBrush;
            BorderCornerBrush = OriginBorderCornerBrush;
        }

        private void VectorFlatIconButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Color lightBackgroundColor = ColorTool.Lighten((OriginBackground as SolidColorBrush).Color, 0.1f);
            Background = new SolidColorBrush(lightBackgroundColor);
            Color lightLeftTopBorderColor = ColorTool.Lighten((OriginLeftTopBorderBrush as SolidColorBrush).Color, 0.1f);
            LeftTopBorderBrush = new SolidColorBrush(lightLeftTopBorderColor);
            Color lightRightBottomBorderColor = ColorTool.Lighten((OriginRightBottomBorderBrush as SolidColorBrush).Color, 0.1f);
            RightBottomBorderBrush = new SolidColorBrush(lightRightBottomBorderColor);
            Color lightCornerColor = ColorTool.Lighten((OriginBorderCornerBrush as SolidColorBrush).Color, 0.1f);
            BorderCornerBrush = new SolidColorBrush(lightCornerColor);
        }
        #endregion
    }
}
