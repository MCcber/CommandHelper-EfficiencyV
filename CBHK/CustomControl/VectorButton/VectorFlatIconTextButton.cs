using CBHK.Utility.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorFlatIconTextButton : Button
    {
        #region Field
        private new bool IsLoaded = false;
        #endregion

        #region Property
        private Brush OriginBackgroundBrush { get; set; }
        private Brush OriginForegroundBrush { get; set; }

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

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(VectorFlatIconTextButton), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty OriginBackgroundBrushProperty =
            DependencyProperty.Register("OriginBackgroundBrush", typeof(Brush), typeof(VectorFlatIconTextButton), new PropertyMetadata(default(Brush)));

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

        public Brush LeftTopBorderBrush
        {
            get { return (Brush)GetValue(LeftTopBorderBrushProperty); }
            set { SetValue(LeftTopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftTopBorderBrushProperty =
            DependencyProperty.Register("LeftTopBorderBrush", typeof(Brush), typeof(VectorFlatIconTextButton), new PropertyMetadata(default(Brush)));

        public Brush RightBottomBorderBrush
        {
            get { return (Brush)GetValue(RightBottomBorderBrushProperty); }
            set { SetValue(RightBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RightBottomBorderBrushProperty =
            DependencyProperty.Register("RightBottomBorderBrush", typeof(Brush), typeof(VectorFlatIconTextButton), new PropertyMetadata(default(Brush)));

        public Brush BorderCornerBrush
        {
            get { return (Brush)GetValue(BorderCornerBrushProperty); }
            set { SetValue(BorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderCornerBrushProperty =
            DependencyProperty.Register("BorderCornerBrush", typeof(Brush), typeof(VectorFlatIconTextButton), new PropertyMetadata(default(Brush)));

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

        #region Method
        public VectorFlatIconTextButton()
        {
            ClickMode = ClickMode.Press;
            Loaded += VectorTextButton_Loaded;
            MouseEnter += VectorTextButton_MouseEnter;
            MouseLeave += VectorTextButton_MouseLeave;
            PreviewMouseLeftButtonDown += VectorTextButton_PreviewMouseLeftButtonDown;
            //PreviewMouseLeftButtonUp += VectorTextButton_PreviewMouseLeftButtonUp;
        }

        private void UpdateBorderColorByBackgroundColor()
        {
            var foregroundSource = DependencyPropertyHelper.GetValueSource(this, ForegroundProperty);
            if ((foregroundSource.BaseValueSource is BaseValueSource.DefaultStyle || foregroundSource.BaseValueSource is BaseValueSource.Style) && !IsLoaded)
            {
                Foreground = Brushes.White;
            }
            var backgroundSource = DependencyPropertyHelper.GetValueSource(this, BackgroundProperty);
            if (backgroundSource.BaseValueSource is BaseValueSource.DefaultStyle && !IsLoaded)
            {
                Background = new BrushConverter().ConvertFromString("#3c8527") as Brush;
            }
            var borderBrushSource = DependencyPropertyHelper.GetValueSource(this, BorderBrushProperty);
            if ((borderBrushSource.BaseValueSource is BaseValueSource.DefaultStyle || borderBrushSource.BaseValueSource is BaseValueSource.Style) && !IsLoaded)
            {
                BorderBrush = Brushes.Black;
            }
            var originborderCornerBrushSource = DependencyPropertyHelper.GetValueSource(this, BorderCornerBrushProperty);
            if (originborderCornerBrushSource.BaseValueSource is BaseValueSource.Default || originborderCornerBrushSource.BaseValueSource is BaseValueSource.Style || IsLoaded)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.DarkenByHSL(solidBorderBrush.Color, 0.3f);
                BorderCornerBrush = new SolidColorBrush(color);
            }
            var originTopBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, LeftTopBorderBrushProperty);
            if (originTopBorderBrushSource.BaseValueSource is BaseValueSource.Default || originTopBorderBrushSource.BaseValueSource is BaseValueSource.Style || IsLoaded)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.LightenByHSL(solidBorderBrush.Color, 0.3f);
                LeftTopBorderBrush = new SolidColorBrush(color);
            }
            var originBottomBrushSource = DependencyPropertyHelper.GetValueSource(this, RightBottomBorderBrushProperty);
            if (originBottomBrushSource.BaseValueSource is BaseValueSource.Default || originBottomBrushSource.BaseValueSource is BaseValueSource.Style || IsLoaded)
            {
                Color color = ColorTool.DarkenByHSL((Background as SolidColorBrush).Color, 0.5f);
                RightBottomBorderBrush ??= new SolidColorBrush(color);
            }
        }
        #endregion

        #region Event
        private void VectorTextButton_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateBorderColorByBackgroundColor();
            OriginForegroundBrush = Foreground;
            OriginBackgroundBrush = Background;
            IsLoaded = true;
        }

        private void VectorTextButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D77933"));
            BorderBrush = Brushes.White;

            UpdateBorderColorByBackgroundColor();
        }

        private void VectorTextButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            BorderBrush = Brushes.Black;
            Background = OriginBackgroundBrush;
            UpdateBorderColorByBackgroundColor();
        }

        private void VectorTextButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D77933"));
            BorderBrush = Brushes.White;

            UpdateBorderColorByBackgroundColor();
        }
        #endregion
    }
}
