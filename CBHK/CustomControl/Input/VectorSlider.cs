using CBHK.Utility.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.Input
{
    public class VectorSlider : Slider
    {
        #region Property
        private double ThumbBottomBorderHeight { get; set; }
        private Brush OriginThumbBackground { get; set; }
        private Brush OriginBackground { get; set; }

        public double SlidingAreaHeight
        {
            get { return (double)GetValue(SlidingAreaHeightProperty); }
            set { SetValue(SlidingAreaHeightProperty, value); }
        }

        public static readonly DependencyProperty SlidingAreaHeightProperty =
    DependencyProperty.Register("SlidingAreaHeight", typeof(double), typeof(VectorSlider), new PropertyMetadata(default(double)));

        public Brush ThumbBackground
        {
            get { return (Brush)GetValue(ThumbBackgroundProperty); }
            set { SetValue(ThumbBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThumbBackgroundProperty =
            DependencyProperty.Register("ThumbBackground", typeof(Brush), typeof(VectorSlider), new PropertyMetadata(default(Brush)));

        public Brush ThumbRoundBorderBrush
        {
            get { return (Brush)GetValue(ThumbRoundBorderBrushProperty); }
            set { SetValue(ThumbRoundBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty ThumbRoundBorderBrushProperty =
            DependencyProperty.Register("ThumbRoundBorderBrush", typeof(Brush), typeof(VectorSlider), new PropertyMetadata(default(Brush)));

        public Brush ThumbBorderCornerBrush
        {
            get { return (Brush)GetValue(ThumbBorderCornerBrushProperty); }
            set { SetValue(ThumbBorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty ThumbBorderCornerBrushProperty =
            DependencyProperty.Register("ThumbBorderCornerBrush", typeof(Brush), typeof(VectorSlider), new PropertyMetadata(default(Brush)));

        public Brush ThumbBottomBorderBrush
        {
            get { return (Brush)GetValue(ThumbBottomBorderBrushProperty); }
            set { SetValue(ThumbBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty ThumbBottomBorderBrushProperty =
            DependencyProperty.Register("ThumbBottomBorderBrush", typeof(Brush), typeof(VectorSlider), new PropertyMetadata(default(Brush)));

        public Brush BackgroundRoundBorderBrush
        {
            get { return (Brush)GetValue(BackgroundRoundBorderBrushProperty); }
            set { SetValue(BackgroundRoundBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty BackgroundRoundBorderBrushProperty =
            DependencyProperty.Register("BackgroundRoundBorderBrush", typeof(Brush), typeof(VectorSlider), new PropertyMetadata(default(Brush)));

        public Brush BackgroundBorderCornerBrush
        {
            get { return (Brush)GetValue(BackgroundBorderCornerBrushProperty); }
            set { SetValue(BackgroundBorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BackgroundBorderCornerBrushProperty =
            DependencyProperty.Register("BackgroundBorderCornerBrush", typeof(Brush), typeof(VectorSlider), new PropertyMetadata(default(Brush)));

        public Brush BackgroundBottomBorderBrush
        {
            get { return (Brush)GetValue(BackgroundBottomBorderBrushProperty); }
            set { SetValue(BackgroundBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty BackgroundBottomBorderBrushProperty =
            DependencyProperty.Register("BackgroundBottomBorderBrush", typeof(Brush), typeof(VectorSlider), new PropertyMetadata(default(Brush)));

        public double ThumbWidth
        {
            get { return (double)GetValue(ThumbWidthProperty); }
            set { SetValue(ThumbWidthProperty, value); }
        }

        public static readonly DependencyProperty ThumbWidthProperty =
            DependencyProperty.Register("ThumbWidth", typeof(double), typeof(VectorSlider), new PropertyMetadata(default(double)));

        public double ThumbHeight
        {
            get { return (double)GetValue(ThumbHeightProperty); }
            set { SetValue(ThumbHeightProperty, value); }
        }

        public static readonly DependencyProperty ThumbHeightProperty =
            DependencyProperty.Register("ThumbHeight", typeof(double), typeof(VectorSlider), new PropertyMetadata(default(double)));
        #endregion

        #region Method
        public VectorSlider()
        {
            Loaded += VectorSlider_Loaded;
            MouseEnter += VectorSlider_MouseEnter;
            MouseLeave += VectorSlider_MouseLeave;
            PreviewMouseLeftButtonDown += VectorSlider_PreviewMouseLeftButtonDown;
            PreviewMouseLeftButtonUp += VectorSlider_PreviewMouseLeftButtonUp;
        }
        #endregion

        #region Event
        private void VectorSlider_Loaded(object sender, RoutedEventArgs e)
        {
            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);
            if(extraBottomLine is RowDefinition row)
            {
                row.Height = new(ThumbBottomBorderHeight, GridUnitType.Pixel);
            }
            var foregroundSource = DependencyPropertyHelper.GetValueSource(this, ForegroundProperty);
            if (foregroundSource.BaseValueSource is BaseValueSource.DefaultStyle)
            {
                Foreground = Brushes.White;
            }
            var backgroundSource = DependencyPropertyHelper.GetValueSource(this, BackgroundProperty);
            if (backgroundSource.BaseValueSource is BaseValueSource.DefaultStyle || backgroundSource.BaseValueSource is BaseValueSource.Style || backgroundSource.BaseValueSource is BaseValueSource.Default)
            {
                Background = new BrushConverter().ConvertFromString("#3c8527") as Brush;
            }
            var thumbBackgroundSource = DependencyPropertyHelper.GetValueSource(this, ThumbBackgroundProperty);
            if (thumbBackgroundSource.BaseValueSource is BaseValueSource.DefaultStyle || thumbBackgroundSource.BaseValueSource is BaseValueSource.Default)
            {
                ThumbBackground = new BrushConverter().ConvertFromString("#D0D1D4") as Brush;
            }
            var borderBrushSource = DependencyPropertyHelper.GetValueSource(this, BorderBrushProperty);
            if (borderBrushSource.BaseValueSource is BaseValueSource.DefaultStyle || borderBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                BorderBrush = Brushes.Black;
            }
            var originBackgroundBorderCornerBrushSource = DependencyPropertyHelper.GetValueSource(this, BackgroundBorderCornerBrushProperty);
            if (originBackgroundBorderCornerBrushSource.BaseValueSource is BaseValueSource.Default || originBackgroundBorderCornerBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.6f);
                BackgroundBorderCornerBrush = new SolidColorBrush(color);
            }
            var originBackgroundRoundBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, BackgroundRoundBorderBrushProperty);
            if (originBackgroundRoundBorderBrushSource.BaseValueSource is BaseValueSource.Default || originBackgroundRoundBorderBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.4f);
                BackgroundRoundBorderBrush = new SolidColorBrush(color);
            }
            var originBackgroundBottomBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, BackgroundBottomBorderBrushProperty);
            if (originBackgroundBottomBorderBrushSource.BaseValueSource is BaseValueSource.Default || originBackgroundBottomBorderBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.2f);
                BackgroundBottomBorderBrush = new SolidColorBrush(color);
            }
            var originThumbBorderCornerBrushSource = DependencyPropertyHelper.GetValueSource(this, ThumbBorderCornerBrushProperty);
            if (originThumbBorderCornerBrushSource.BaseValueSource is BaseValueSource.Default || originThumbBorderCornerBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                SolidColorBrush solidBorderBrush = ThumbBackground as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.4f);
                ThumbBorderCornerBrush = new SolidColorBrush(color);
            }
            var originThumbRoundBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, ThumbRoundBorderBrushProperty);
            if (originThumbRoundBorderBrushSource.BaseValueSource is BaseValueSource.Default || originThumbRoundBorderBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                SolidColorBrush solidBorderBrush = ThumbBackground as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.2f);
                ThumbRoundBorderBrush = new SolidColorBrush(color);
            }
            var originThumbBottomBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, ThumbBottomBorderBrushProperty);
            if (originThumbBottomBorderBrushSource.BaseValueSource is BaseValueSource.Default || originThumbBottomBorderBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                SolidColorBrush solidBorderBrush = ThumbBackground as SolidColorBrush;
                Color color = ColorTool.Darken(solidBorderBrush.Color, 0.6f);
                ThumbBottomBorderBrush = new SolidColorBrush(color);
            }

            OriginThumbBackground = ThumbBackground;
        }

        private void VectorSlider_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            VectorSlider_MouseEnter(sender, null);
        }

        private void VectorSlider_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            VectorSlider_MouseEnter(sender, null);
        }

        private void VectorSlider_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ThumbBackground = OriginThumbBackground;
        }

        private void VectorSlider_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Color color = ColorTool.Darken((OriginThumbBackground as SolidColorBrush).Color, 0.2f);
            ThumbBackground = new SolidColorBrush(color);
        }
        #endregion
    }
}
