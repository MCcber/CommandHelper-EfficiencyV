using CBHK.Utility.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorIconRepeatButton:RepeatButton
    {
        #region  Field
        private Thickness OriginMargin;
        private Brush OriginLeftTopBorderBrush;
        private Brush OriginRightBottomBorderBrush;
        private Brush OriginBorderCornerBrush;
        private Brush OriginBottomBrush;
        private Brush OriginForegroundBrush;
        private Brush OriginBackgroundBrush;
        #endregion

        #region Property
        public int OriginBottomHeight { get; set; }
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

        public Brush BottomBorderBrush
        {
            get { return (Brush)GetValue(BottomBorderBrushProperty); }
            set { SetValue(BottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty BottomBorderBrushProperty =
            DependencyProperty.Register("BottomBorderBrush", typeof(Brush), typeof(VectorIconRepeatButton), new PropertyMetadata(default(Brush)));

        public Brush RightBottomBorderBrush
        {
            get { return (Brush)GetValue(RightBottomBorderBrushProperty); }
            set { SetValue(RightBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RightBottomBorderBrushProperty =
            DependencyProperty.Register("RightBottomBorderBrush", typeof(Brush), typeof(VectorIconRepeatButton), new PropertyMetadata(default(Brush)));

        public Brush LeftTopBorderBrush
        {
            get { return (Brush)GetValue(LeftTopBorderBrushProperty); }
            set { SetValue(LeftTopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftTopBorderBrushProperty =
            DependencyProperty.Register("LeftTopBorderBrush", typeof(Brush), typeof(VectorIconRepeatButton), new PropertyMetadata(default(Brush)));

        public Brush BorderCornerBrush
        {
            get { return (Brush)GetValue(BorderCornerBrushProperty); }
            set { SetValue(BorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderCornerBrushProperty =
            DependencyProperty.Register("BorderCornerBrush", typeof(Brush), typeof(VectorIconRepeatButton), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorIconRepeatButton()
        {
            Loaded += VectorIconRepeatButton_Loaded;
            MouseEnter += VectorIconRepeatButton_MouseEnter;
            MouseLeave += VectorIconRepeatButton_MouseLeave;
            PreviewMouseLeftButtonDown += VectorIconRepeatButton_PreviewMouseLeftButtonDown;
            PreviewMouseLeftButtonUp += VectorIconRepeatButton_PreviewMouseLeftButtonUp;
        }

        private void UpdateBorderColorByBackgroundColor(object sender)
        {
            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);
            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(OriginBottomHeight, GridUnitType.Pixel);
            }

            var foregroundSource = DependencyPropertyHelper.GetValueSource(this, ForegroundProperty);
            if (foregroundSource.BaseValueSource is BaseValueSource.DefaultStyle || foregroundSource.BaseValueSource is BaseValueSource.ParentTemplate || foregroundSource.BaseValueSource is BaseValueSource.Style || Foreground is null)
            {
                Foreground = Brushes.White;
            }
            var backgroundSource = DependencyPropertyHelper.GetValueSource(this, BackgroundProperty);
            if (backgroundSource.BaseValueSource is BaseValueSource.DefaultStyle || Background is null)
            {
                Background = new BrushConverter().ConvertFromString("#3c8527") as Brush;
            }
            var borderBrushSource = DependencyPropertyHelper.GetValueSource(this, BorderBrushProperty);
            if (borderBrushSource.BaseValueSource is BaseValueSource.DefaultStyle || borderBrushSource.BaseValueSource is BaseValueSource.Style || BorderBrush is null)
            {
                BorderBrush = Brushes.Black;
            }
            var originborderCornerBrushSource = DependencyPropertyHelper.GetValueSource(this, BorderCornerBrushProperty);
            if (originborderCornerBrushSource.BaseValueSource is BaseValueSource.Default || originborderCornerBrushSource.BaseValueSource is BaseValueSource.Style || BorderCornerBrush is null)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.4f);
                BorderCornerBrush = new SolidColorBrush(color);
            }
            var originLeftTopBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, LeftTopBorderBrushProperty);
            if (originLeftTopBorderBrushSource.BaseValueSource is BaseValueSource.Default || originLeftTopBorderBrushSource.BaseValueSource is BaseValueSource.Style || LeftTopBorderBrush is null)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.4f);
                LeftTopBorderBrush = new SolidColorBrush(color);
            }
            var originRightBottomBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, RightBottomBorderBrushProperty);
            if (originRightBottomBorderBrushSource.BaseValueSource is BaseValueSource.Default || originRightBottomBorderBrushSource.BaseValueSource is BaseValueSource.Style || RightBottomBorderBrush is null)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.3f);
                RightBottomBorderBrush = new SolidColorBrush(color);
            }
            var originBottomBrushSource = DependencyPropertyHelper.GetValueSource(this, BottomBorderBrushProperty);
            if (originBottomBrushSource.BaseValueSource is BaseValueSource.Default || originBottomBrushSource.BaseValueSource is BaseValueSource.Style || BottomBorderBrush is null)
            {
                Color color = ColorTool.Darken((Background as SolidColorBrush).Color, 0.4f);
                BottomBorderBrush ??= new SolidColorBrush(color);
            }
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

            UpdateBorderColorByBackgroundColor(sender);

            OriginBorderCornerBrush = BorderCornerBrush;
            OriginLeftTopBorderBrush = LeftTopBorderBrush;
            OriginRightBottomBorderBrush = RightBottomBorderBrush;
            OriginForegroundBrush = Foreground;
            OriginBackgroundBrush = Background;
        }

        private void VectorIconRepeatButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            VectorIconRepeatButton_MouseEnter(sender, null);
        }

        private void VectorIconRepeatButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);
            Color color = ColorTool.Darken((Background as SolidColorBrush).Color, 0.4f);
            Background = new SolidColorBrush(color);
            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(0, GridUnitType.Pixel);
            }
            Margin = new(Margin.Left, Margin.Top + 2, Margin.Right, Margin.Bottom);
        }

        private void VectorIconRepeatButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);
            Background = OriginBackgroundBrush;
            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(OriginBottomHeight, GridUnitType.Pixel);
            }
            LeftTopBorderBrush = OriginLeftTopBorderBrush;
            BorderCornerBrush = OriginBorderCornerBrush;
            Margin = OriginMargin;
        }

        private void VectorIconRepeatButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);

            Color darkColor = ColorTool.Darken((Background as SolidColorBrush).Color, 0.2f);
            Background = new SolidColorBrush(darkColor);
            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(OriginBottomHeight, GridUnitType.Pixel);
            }
            Margin = OriginMargin;
            Color lightBorderColor = ColorTool.Lighten((OriginLeftTopBorderBrush as SolidColorBrush).Color, 0.4f);
            LeftTopBorderBrush = new SolidColorBrush(lightBorderColor);
            Color lightCornerColor = ColorTool.Lighten((OriginBorderCornerBrush as SolidColorBrush).Color, 0.6f);
            BorderCornerBrush = new SolidColorBrush(lightCornerColor);
        }
        #endregion
    }
}
