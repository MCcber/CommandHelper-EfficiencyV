using CBHK.Utility.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.Container
{
    public class VectorRichExpander : Expander
    {
        #region Field
        private Brush OriginBackground;
        private Brush OriginLeftTopBorderBrush;
        private Brush OriginRightBottomBorderBrush;
        private Brush OriginCornerBorderBrush;
        #endregion

        #region Property
        public double RotationAngle
        {
            get { return (double)GetValue(RotationAngleProperty); }
            set { SetValue(RotationAngleProperty, value); }
        }

        public static readonly DependencyProperty RotationAngleProperty =
            DependencyProperty.Register("RotationAngle", typeof(double), typeof(VectorRichExpander), new PropertyMetadata(default(double)));

        public object HeaderContent
        {
            get { return GetValue(HeaderContentProperty); }
            set { SetValue(HeaderContentProperty, value); }
        }

        public static readonly DependencyProperty HeaderContentProperty =
            DependencyProperty.Register("HeaderContent", typeof(object), typeof(VectorRichExpander), new PropertyMetadata(default(object)));

        public DataTemplate HeaderContentTemplate
        {
            get { return (DataTemplate)GetValue(HeaderContentTemplateProperty); }
            set { SetValue(HeaderContentTemplateProperty, value); }
        }

        public static readonly DependencyProperty HeaderContentTemplateProperty =
            DependencyProperty.Register("HeaderContentTemplate", typeof(DataTemplate), typeof(VectorRichExpander), new PropertyMetadata(default(DataTemplate)));

        public Brush LeftTopBorderBrush
        {
            get { return (Brush)GetValue(LeftTopBorderBrushProperty); }
            set { SetValue(LeftTopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftTopBorderBrushProperty =
            DependencyProperty.Register("LeftTopBorderBrush", typeof(Brush), typeof(VectorRichExpander), new PropertyMetadata(default(Brush)));

        public Brush RightBottomBorderBrush
        {
            get { return (Brush)GetValue(RightBottomBorderBrushProperty); }
            set { SetValue(RightBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RightBottomBorderBrushProperty =
            DependencyProperty.Register("RightBottomBorderBrush", typeof(Brush), typeof(VectorRichExpander), new PropertyMetadata(default(Brush)));

        public Brush CornerBorderBrush
        {
            get { return (Brush)GetValue(CornerBorderBrushProperty); }
            set { SetValue(CornerBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty CornerBorderBrushProperty =
            DependencyProperty.Register("CornerBorderBrush", typeof(Brush), typeof(VectorRichExpander), new PropertyMetadata(default(Brush)));

        public Brush ArrowBrush
        {
            get { return (Brush)GetValue(ArrowBrushProperty); }
            set { SetValue(ArrowBrushProperty, value); }
        }

        public static readonly DependencyProperty ArrowBrushProperty =
            DependencyProperty.Register("ArrowBrush", typeof(Brush), typeof(VectorRichExpander), new PropertyMetadata(default(Brush)));

        public Thickness ArrowMargin
        {
            get { return (Thickness)GetValue(ArrowMarginProperty); }
            set { SetValue(ArrowMarginProperty, value); }
        }

        public static readonly DependencyProperty ArrowMarginProperty =
            DependencyProperty.Register("ArrowMargin", typeof(Thickness), typeof(VectorRichExpander), new PropertyMetadata(default(Thickness)));
        #endregion

        #region Method
        public VectorRichExpander()
        {
            Loaded += VectorRichExpander_Loaded;
            MouseEnter += VectorRichExpander_MouseEnter;
            MouseLeave += VectorRichExpander_MouseLeave;
            PreviewMouseLeftButtonDown += VectorRichExpander_PreviewMouseLeftButtonDown;
            PreviewMouseLeftButtonUp += VectorRichExpander_PreviewMouseLeftButtonUp;
            Expanded += VectorRichExpander_Expanded;
            Collapsed += VectorRichExpander_Collapsed;
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
            var originCornerBrushSource = DependencyPropertyHelper.GetValueSource(this, CornerBorderBrushProperty);
            if (originCornerBrushSource.BaseValueSource is BaseValueSource.Default || originCornerBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                Color color = ColorTool.Darken((Background as SolidColorBrush).Color, 0.2f);
                CornerBorderBrush = new SolidColorBrush(color);
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
            if (OriginCornerBorderBrush is null && CornerBorderBrush is not null)
            {
                OriginCornerBorderBrush = CornerBorderBrush;
            }
        }
        #endregion

        #region Event
        private void VectorRichExpander_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateBorderColorByBackgroundColor();
        }

        private void VectorRichExpander_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            VectorRichExpander_MouseEnter(sender, null);
        }

        private void VectorRichExpander_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Color lightBackgroundColor = ColorTool.Darken((OriginBackground as SolidColorBrush).Color, 0.2f);
            Background = new SolidColorBrush(lightBackgroundColor);
            Color lightLeftTopBorderColor = ColorTool.Darken((OriginLeftTopBorderBrush as SolidColorBrush).Color, 0.4f);
            LeftTopBorderBrush = new SolidColorBrush(lightLeftTopBorderColor);
            Color lightRightBottomBorderColor = ColorTool.Darken((RightBottomBorderBrush as SolidColorBrush).Color, 0.2f);
            RightBottomBorderBrush = new SolidColorBrush(lightRightBottomBorderColor);
            Color lightCornerColor = ColorTool.Darken((OriginCornerBorderBrush as SolidColorBrush).Color, 0.2f);
            CornerBorderBrush = new SolidColorBrush(lightCornerColor);
        }

        private void VectorRichExpander_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Background = OriginBackground;
            LeftTopBorderBrush = OriginLeftTopBorderBrush;
            RightBottomBorderBrush = OriginRightBottomBorderBrush;
            CornerBorderBrush = OriginCornerBorderBrush;
        }

        private void VectorRichExpander_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Color lightBackgroundColor = ColorTool.Lighten((OriginBackground as SolidColorBrush).Color, 0.1f);
            Background = new SolidColorBrush(lightBackgroundColor);
            Color lightLeftTopBorderColor = ColorTool.Lighten((OriginLeftTopBorderBrush as SolidColorBrush).Color, 0.1f);
            LeftTopBorderBrush = new SolidColorBrush(lightLeftTopBorderColor);
            Color lightRightBottomBorderColor = ColorTool.Lighten((OriginRightBottomBorderBrush as SolidColorBrush).Color, 0.1f);
            RightBottomBorderBrush = new SolidColorBrush(lightRightBottomBorderColor);
            Color lightCornerColor = ColorTool.Lighten((OriginCornerBorderBrush as SolidColorBrush).Color, 0.1f);
            CornerBorderBrush = new SolidColorBrush(lightCornerColor);
        }

        private void VectorRichExpander_Expanded(object sender, System.Windows.RoutedEventArgs e) => RotationAngle = 180;

        private void VectorRichExpander_Collapsed(object sender, RoutedEventArgs e) => RotationAngle = 0;
        #endregion
    }
}