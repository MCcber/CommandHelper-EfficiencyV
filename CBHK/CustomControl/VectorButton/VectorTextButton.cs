using CBHK.Utility.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorTextButton : Button
    {
        #region Property
        private Thickness OriginMargin { get; set; }
        private Brush OriginTopBorderBrush { get; set; }
        private Brush OriginBorderCornerBrush { get; set; }
        private Brush OriginRoundBorderBrush { get; set; }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(VectorTextButton), new PropertyMetadata(default(string)));

        public double OriginBottomHeight
        {
            get { return (double)GetValue(OriginBottomHeightProperty); }
            set { SetValue(OriginBottomHeightProperty, value); }
        }

        public static readonly DependencyProperty OriginBottomHeightProperty =
            DependencyProperty.Register("OriginBottomHeight", typeof(double), typeof(VectorTextButton), new PropertyMetadata(default(double)));

        public Brush OriginForegroundBrush
        {
            get { return (Brush)GetValue(OriginForegroundBrushProperty); }
            set { SetValue(OriginForegroundBrushProperty, value); }
        }

        public static readonly DependencyProperty OriginForegroundBrushProperty =
            DependencyProperty.Register("OriginForegroundBrush", typeof(Brush), typeof(VectorTextButton), new PropertyMetadata(default(Brush)));

        public Brush OriginBackgroundBrush
        {
            get { return (Brush)GetValue(OriginBackgroundBrushProperty); }
            set { SetValue(OriginBackgroundBrushProperty, value); }
        }

        public static readonly DependencyProperty OriginBackgroundBrushProperty =
            DependencyProperty.Register("OriginBackgroundBrush", typeof(Brush), typeof(VectorTextButton), new PropertyMetadata(default(Brush)));

        public Brush RoundBorderBrush
        {
            get { return (Brush)GetValue(RoundBorderBrushProperty); }
            set { SetValue(RoundBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RoundBorderBrushProperty =
            DependencyProperty.Register("RoundBorderBrush", typeof(Brush), typeof(VectorTextButton), new PropertyMetadata(default(Brush)));

        public Brush TopBorderBrush
        {
            get { return (Brush)GetValue(TopBorderBrushProperty); }
            set { SetValue(TopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty TopBorderBrushProperty =
            DependencyProperty.Register("TopBorderBrush", typeof(Brush), typeof(VectorTextButton), new PropertyMetadata(default(Brush)));

        public Brush BorderCornerBrush
        {
            get { return (Brush)GetValue(BorderCornerBrushProperty); }
            set { SetValue(BorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderCornerBrushProperty =
            DependencyProperty.Register("BorderCornerBrush", typeof(Brush), typeof(VectorTextButton), new PropertyMetadata(default(Brush)));

        public Brush OriginBottomBrush
        {
            get { return (Brush)GetValue(OriginBottomBrushProperty); }
            set { SetValue(OriginBottomBrushProperty, value); }
        }

        public static readonly DependencyProperty OriginBottomBrushProperty =
            DependencyProperty.Register("OriginBottomBrush", typeof(Brush), typeof(VectorTextButton), new PropertyMetadata(default(Brush)));

        #endregion

        #region Method
        public VectorTextButton()
        {
            RoundBorderBrush = Brushes.Black;
            Loaded += VectorTextButton_Loaded;
            MouseEnter += VectorButton_MouseEnter;
            MouseLeave += VectorButton_MouseLeave;
            PreviewMouseLeftButtonDown += VectorTextButton_PreviewMouseLeftButtonDown;
            PreviewMouseLeftButtonUp += VectorTextButton_PreviewMouseLeftButtonUp;
        }
        #endregion

        #region Event
        private void VectorTextButton_Loaded(object sender, RoutedEventArgs e)
        {
            OriginMargin = Margin;
            if (Text == "")
            {
                Text = "Button";
            }
            if (OriginBottomHeight == 0)
            {
                OriginBottomHeight = 6;
            }

            var foregroundSource = DependencyPropertyHelper.GetValueSource(this, ForegroundProperty);
            if (foregroundSource.BaseValueSource is BaseValueSource.DefaultStyle || foregroundSource.BaseValueSource is BaseValueSource.Style)
            {
                Foreground = Brushes.White;
            }
            var backgroundSource = DependencyPropertyHelper.GetValueSource(this, BackgroundProperty);
            if (backgroundSource.BaseValueSource is BaseValueSource.DefaultStyle || foregroundSource.BaseValueSource is BaseValueSource.Style)
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

        private void VectorTextButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            VectorButton_MouseEnter(sender, null);
        }

        private void VectorTextButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            object borderElement = Template.FindName("border", sender as FrameworkElement);
            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);
            if (borderElement is Border border)
            {
                Color color = ColorTool.DarkenByHSL((Background as SolidColorBrush).Color, 0.4f);
                border.Background = new SolidColorBrush(color);
            }
            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(0, GridUnitType.Pixel);
            }
            Margin = new(Margin.Left, Margin.Top + 10, Margin.Right, Margin.Bottom);
        }

        private void VectorButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            object borderElement = Template.FindName("border", sender as FrameworkElement);
            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);
            if (borderElement is Border border)
            {
                border.Background = OriginBackgroundBrush;
            }
            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(6, GridUnitType.Pixel);
            }
            TopBorderBrush = OriginTopBorderBrush;
            BorderCornerBrush = OriginBorderCornerBrush;
            Margin = OriginMargin;
            OriginBottomHeight = 6;
        }

        private void VectorButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            object borderElement = Template.FindName("border", sender as FrameworkElement);
            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);

            if (borderElement is Border border)
            {
                Color darkColor = ColorTool.DarkenByHSL((Background as SolidColorBrush).Color, 0.2f);
                border.Background = new SolidColorBrush(darkColor);
            }
            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(6, GridUnitType.Pixel);
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
