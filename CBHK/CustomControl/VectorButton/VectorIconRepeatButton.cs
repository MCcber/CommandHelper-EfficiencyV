using CBHK.Utility.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorIconRepeatButton:RepeatButton
    {
        #region Property
        private Thickness OriginMargin { get; set; }
        private Brush OriginTopBorderBrush { get; set; }
        private Brush OriginBorderCornerBrush { get; set; }

        public Brush IconBrush
        {
            get { return (Brush)GetValue(IconBrushProperty); }
            set { SetValue(IconBrushProperty, value); }
        }

        public static readonly DependencyProperty IconBrushProperty =
            DependencyProperty.Register("IconBrush", typeof(Brush), typeof(VectorIconRepeatButton), new PropertyMetadata(default(Brush)));

        public double IconAngle
        {
            get { return (double)GetValue(IconAngleProperty); }
            set { SetValue(IconAngleProperty, value); }
        }

        public static readonly DependencyProperty IconAngleProperty =
            DependencyProperty.Register("IconAngle", typeof(double), typeof(VectorIconRepeatButton), new PropertyMetadata(default(double)));

        public double OriginBottomHeight
        {
            get { return (double)GetValue(OriginBottomHeightProperty); }
            set { SetValue(OriginBottomHeightProperty, value); }
        }

        public static readonly DependencyProperty OriginBottomHeightProperty =
            DependencyProperty.Register("OriginBottomHeight", typeof(double), typeof(VectorIconRepeatButton), new PropertyMetadata(default(double)));

        public Brush OriginForegroundBrush
        {
            get { return (Brush)GetValue(OriginForegroundBrushProperty); }
            set { SetValue(OriginForegroundBrushProperty, value); }
        }

        public static readonly DependencyProperty OriginForegroundBrushProperty =
            DependencyProperty.Register("OriginForegroundBrush", typeof(Brush), typeof(VectorIconRepeatButton), new PropertyMetadata(default(Brush)));

        public Brush OriginBackgroundBrush
        {
            get { return (Brush)GetValue(OriginBackgroundBrushProperty); }
            set { SetValue(OriginBackgroundBrushProperty, value); }
        }

        public static readonly DependencyProperty OriginBackgroundBrushProperty =
            DependencyProperty.Register("OriginBackgroundBrush", typeof(Brush), typeof(VectorIconRepeatButton), new PropertyMetadata(default(Brush)));

        public Brush RoundBorderBrush
        {
            get { return (Brush)GetValue(RoundBorderBrushProperty); }
            set { SetValue(RoundBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RoundBorderBrushProperty =
            DependencyProperty.Register("RoundBorderBrush", typeof(Brush), typeof(VectorIconRepeatButton), new PropertyMetadata(default(Brush)));

        public Brush TopBorderBrush
        {
            get { return (Brush)GetValue(TopBorderBrushProperty); }
            set { SetValue(TopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty TopBorderBrushProperty =
            DependencyProperty.Register("TopBorderBrush", typeof(Brush), typeof(VectorIconRepeatButton), new PropertyMetadata(default(Brush)));

        public Brush BorderCornerBrush
        {
            get { return (Brush)GetValue(BorderCornerBrushProperty); }
            set { SetValue(BorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderCornerBrushProperty =
            DependencyProperty.Register("BorderCornerBrush", typeof(Brush), typeof(VectorIconRepeatButton), new PropertyMetadata(default(Brush)));

        public Brush OriginBottomBrush
        {
            get { return (Brush)GetValue(OriginBottomBrushProperty); }
            set { SetValue(OriginBottomBrushProperty, value); }
        }

        public static readonly DependencyProperty OriginBottomBrushProperty =
            DependencyProperty.Register("OriginBottomBrush", typeof(Brush), typeof(VectorIconRepeatButton), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorIconRepeatButton()
        {
            RoundBorderBrush = Brushes.Black;
            Loaded += VectorIconRepeatButton_Loaded;
            MouseEnter += VectorIconRepeatButton_MouseEnter;
            MouseLeave += VectorIconRepeatButton_MouseLeave;
            PreviewMouseLeftButtonDown += VectorIconRepeatButton_PreviewMouseLeftButtonDown;
            PreviewMouseLeftButtonUp += VectorIconRepeatButton_PreviewMouseLeftButtonUp;
        }
        #endregion

        #region Event
        private void VectorIconRepeatButton_Loaded(object sender, RoutedEventArgs e)
        {
            OriginMargin = Margin;
            if (OriginBottomHeight == 0)
            {
                OriginBottomHeight = 6;
            }

            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);
            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(OriginBottomHeight, GridUnitType.Pixel);
            }

            var foregroundSource = DependencyPropertyHelper.GetValueSource(this, ForegroundProperty);
            if (foregroundSource.BaseValueSource is BaseValueSource.DefaultStyle || foregroundSource.BaseValueSource is BaseValueSource.ParentTemplate || foregroundSource.BaseValueSource is BaseValueSource.Style)
            {
                Foreground = Brushes.White;
            }
            var backgroundSource = DependencyPropertyHelper.GetValueSource(this, BackgroundProperty);
            if (backgroundSource.BaseValueSource is BaseValueSource.DefaultStyle || Background is null)
            {
                Background = new BrushConverter().ConvertFromString("#3c8527") as Brush;
            }
            var borderBrushSource = DependencyPropertyHelper.GetValueSource(this, BorderBrushProperty);
            if (borderBrushSource.BaseValueSource is BaseValueSource.DefaultStyle || borderBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                BorderBrush = Brushes.Black;
            }
            var originborderCornerBrushSource = DependencyPropertyHelper.GetValueSource(this, BorderCornerBrushProperty);
            if (originborderCornerBrushSource.BaseValueSource is BaseValueSource.Default || originborderCornerBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.LightenByHSL(solidBorderBrush.Color, 0.4f);
                BorderCornerBrush = OriginBorderCornerBrush = new SolidColorBrush(color);
            }
            var originTopBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, TopBorderBrushProperty);
            if (originTopBorderBrushSource.BaseValueSource is BaseValueSource.Default || originTopBorderBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.LightenByHSL(solidBorderBrush.Color, 0.2f);
                TopBorderBrush = OriginTopBorderBrush = new SolidColorBrush(color);
            }
            var originBottomBrushSource = DependencyPropertyHelper.GetValueSource(this, OriginBottomBrushProperty);
            if (originBottomBrushSource.BaseValueSource is BaseValueSource.Default || originBottomBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                Color color = ColorTool.DarkenByHSL((Background as SolidColorBrush).Color, 0.5f);
                OriginBottomBrush ??= new SolidColorBrush(color);
            }

            OriginForegroundBrush = Foreground;
            OriginBackgroundBrush = Background;
        }

        private void VectorIconRepeatButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            VectorIconRepeatButton_MouseEnter(sender, null);
        }

        private void VectorIconRepeatButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            object borderElement = Template.FindName("templateRoot", sender as FrameworkElement);
            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);
            if (borderElement is Border templateRoot)
            {
                Color color = ColorTool.DarkenByHSL((Background as SolidColorBrush).Color, 0.4f);
                templateRoot.Background = new SolidColorBrush(color);
            }
            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(0, GridUnitType.Pixel);
            }
            Margin = new(Margin.Left, Margin.Top + 2, Margin.Right, Margin.Bottom);
        }

        private void VectorIconRepeatButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            object borderElement = Template.FindName("templateRoot", sender as FrameworkElement);
            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);
            if (borderElement is Border templateRoot)
            {
                templateRoot.Background = OriginBackgroundBrush;
            }
            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(OriginBottomHeight, GridUnitType.Pixel);
            }
            TopBorderBrush = OriginTopBorderBrush;
            BorderCornerBrush = OriginBorderCornerBrush;
            Margin = OriginMargin;
        }

        private void VectorIconRepeatButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            object borderElement = Template.FindName("templateRoot", sender as FrameworkElement);
            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);

            if (borderElement is Border templateRoot)
            {
                Color darkColor = ColorTool.DarkenByHSL((Background as SolidColorBrush).Color, 0.2f);
                templateRoot.Background = new SolidColorBrush(darkColor);
            }
            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(OriginBottomHeight, GridUnitType.Pixel);
            }
            Margin = OriginMargin;
            Color lightBorderColor = ColorTool.LightenByHSL((OriginTopBorderBrush as SolidColorBrush).Color, 0.4f);
            TopBorderBrush = new SolidColorBrush(lightBorderColor);
            Color lightCornerColor = ColorTool.LightenByHSL((OriginBorderCornerBrush as SolidColorBrush).Color, 0.6f);
            BorderCornerBrush = new SolidColorBrush(lightCornerColor);
        }
        #endregion
    }
}
