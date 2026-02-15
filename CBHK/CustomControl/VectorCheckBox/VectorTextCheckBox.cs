using CBHK.Utility.Common;
using System.Windows;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorCheckBox
{
    public class VectorTextCheckBox:System.Windows.Controls.CheckBox
    {
        #region Field
        private bool isUserClicking = false;
        private bool isReFreshingBrush = false;
        private Brush OriginInnerBorderBrush;
        private Brush OriginBackground;
        #endregion

        #region Property
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(VectorTextCheckBox), new PropertyMetadata(default(string)));

        public Brush InnerBorderBrush
        {
            get { return (Brush)GetValue(InnerBorderBrushProperty); }
            set { SetValue(InnerBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty InnerBorderBrushProperty =
            DependencyProperty.Register("InnerBorderBrush", typeof(Brush), typeof(VectorTextCheckBox), new PropertyMetadata(default(Brush)));

        public Brush InnerLeftTopBackground
        {
            get { return (Brush)GetValue(InnerLeftTopBackgroundProperty); }
            set { SetValue(InnerLeftTopBackgroundProperty, value); }
        }

        public static readonly DependencyProperty InnerLeftTopBackgroundProperty =
            DependencyProperty.Register("InnerLeftTopBackground", typeof(Brush), typeof(VectorTextCheckBox), new PropertyMetadata(default(Brush)));

        public Brush InnerRightTopBackground
        {
            get { return (Brush)GetValue(InnerRightTopBackgroundProperty); }
            set { SetValue(InnerRightTopBackgroundProperty, value); }
        }

        public static readonly DependencyProperty InnerRightTopBackgroundProperty =
            DependencyProperty.Register("InnerRightTopBackground", typeof(Brush), typeof(VectorTextCheckBox), new PropertyMetadata(default(Brush)));

        public Brush InnerLeftBottomBackground
        {
            get { return (Brush)GetValue(InnerLeftBottomBackgroundProperty); }
            set { SetValue(InnerLeftBottomBackgroundProperty, value); }
        }

        public static readonly DependencyProperty InnerLeftBottomBackgroundProperty =
            DependencyProperty.Register("InnerLeftBottomBackground", typeof(Brush), typeof(VectorTextCheckBox), new PropertyMetadata(default(Brush)));

        public Brush InnerRightBottomBackground
        {
            get { return (Brush)GetValue(InnerRightBottomBackgroundProperty); }
            set { SetValue(InnerRightBottomBackgroundProperty, value); }
        }

        public static readonly DependencyProperty InnerRightBottomBackgroundProperty =
            DependencyProperty.Register("InnerRightBottomBackground", typeof(Brush), typeof(VectorTextCheckBox), new PropertyMetadata(default(Brush)));

        public Brush LeftTopBorderBrush
        {
            get { return (Brush)GetValue(LeftTopBorderBrushProperty); }
            set { SetValue(LeftTopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftTopBorderBrushProperty =
            DependencyProperty.Register("LeftTopBorderBrush", typeof(Brush), typeof(VectorTextCheckBox), new PropertyMetadata(default(Brush)));

        public Brush RightBottomBorderBrush
        {
            get { return (Brush)GetValue(RightBottomBorderBrushProperty); }
            set { SetValue(RightBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RightBottomBorderBrushProperty =
            DependencyProperty.Register("RightBottomBorderBrush", typeof(Brush), typeof(VectorTextCheckBox), new PropertyMetadata(default(Brush)));

        public Brush BorderCornerBrush
        {
            get { return (Brush)GetValue(BorderCornerBrushProperty); }
            set { SetValue(BorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderCornerBrushProperty =
            DependencyProperty.Register("BorderCornerBrush", typeof(Brush), typeof(VectorTextCheckBox), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorTextCheckBox()
        {
            Loaded += VectorTextCheckBox_Loaded;
        }

        private void UpdateBorderColorByBackgroundColor()
        {
            var foregroundSource = DependencyPropertyHelper.GetValueSource(this, ForegroundProperty);
            if (foregroundSource.BaseValueSource is BaseValueSource.DefaultStyle || foregroundSource.BaseValueSource is BaseValueSource.Style)
            {
                Foreground = Brushes.White;
            }

            var borderBrushSource = DependencyPropertyHelper.GetValueSource(this, BorderBrushProperty);
            if (borderBrushSource.BaseValueSource is BaseValueSource.DefaultStyle || borderBrushSource.BaseValueSource is BaseValueSource.Style || BorderBrush is null)
            {
                BorderBrush = Brushes.Black;
            }

            var originborderCornerBrushSource = DependencyPropertyHelper.GetValueSource(this, BorderCornerBrushProperty);
            if (originborderCornerBrushSource.BaseValueSource is BaseValueSource.Default || originborderCornerBrushSource.BaseValueSource is BaseValueSource.Style || BorderCornerBrush is null || isReFreshingBrush)
            {
                SolidColorBrush solidBorderBrush = OriginInnerBorderBrush as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.4f);
                BorderCornerBrush = new SolidColorBrush(color);
            }

            var originLeftTopBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, LeftTopBorderBrushProperty);
            if (originLeftTopBorderBrushSource.BaseValueSource is BaseValueSource.Default || originLeftTopBorderBrushSource.BaseValueSource is BaseValueSource.Style || LeftTopBorderBrush is null || isReFreshingBrush)
            {
                SolidColorBrush solidBorderBrush = OriginInnerBorderBrush as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.3f);
                LeftTopBorderBrush = new SolidColorBrush(color);
            }

            var originRightBottomBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, RightBottomBorderBrushProperty);
            if (originRightBottomBorderBrushSource.BaseValueSource is BaseValueSource.Default || originRightBottomBorderBrushSource.BaseValueSource is BaseValueSource.Style || RightBottomBorderBrush is null || isReFreshingBrush)
            {
                SolidColorBrush solidBorderBrush = OriginInnerBorderBrush as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.2f);
                RightBottomBorderBrush = new SolidColorBrush(color);
            }

            var originInnerLeftTopBackgroundSource = DependencyPropertyHelper.GetValueSource(this, InnerLeftTopBackgroundProperty);
            if (originInnerLeftTopBackgroundSource.BaseValueSource is BaseValueSource.Default || originInnerLeftTopBackgroundSource.BaseValueSource is BaseValueSource.Style || InnerLeftTopBackground is null || isReFreshingBrush)
            {
                SolidColorBrush solidBorderBrush = OriginBackground as SolidColorBrush;
                Color color = ColorTool.Darken(solidBorderBrush.Color, 0.02f);
                InnerLeftTopBackground = new SolidColorBrush(color);
            }

            var originInnerRightTopBackgroundSource = DependencyPropertyHelper.GetValueSource(this, InnerRightTopBackgroundProperty);
            if (originInnerRightTopBackgroundSource.BaseValueSource is BaseValueSource.Default || originInnerRightTopBackgroundSource.BaseValueSource is BaseValueSource.Style || InnerRightTopBackground is null || isReFreshingBrush)
            {
                SolidColorBrush solidBorderBrush = OriginBackground as SolidColorBrush;
                Color color = ColorTool.Darken(solidBorderBrush.Color, 0.05f);
                InnerRightTopBackground = new SolidColorBrush(color);
            }

            var originInnerLeftBottomBackgroundSource = DependencyPropertyHelper.GetValueSource(this, InnerLeftBottomBackgroundProperty);
            if (originInnerLeftBottomBackgroundSource.BaseValueSource is BaseValueSource.Default || originInnerLeftBottomBackgroundSource.BaseValueSource is BaseValueSource.Style || InnerLeftBottomBackground is null || isReFreshingBrush)
            {
                SolidColorBrush solidBorderBrush = OriginBackground as SolidColorBrush;
                Color color = ColorTool.Darken(solidBorderBrush.Color, 0.05f);
                InnerLeftBottomBackground = new SolidColorBrush(color);
            }

            var originInnerRightBottomBackgroundSource = DependencyPropertyHelper.GetValueSource(this, InnerRightBottomBackgroundProperty);
            if (originInnerRightBottomBackgroundSource.BaseValueSource is BaseValueSource.Default || originInnerRightBottomBackgroundSource.BaseValueSource is BaseValueSource.Style || InnerRightBottomBackground is null || isReFreshingBrush)
            {
                SolidColorBrush solidBorderBrush = OriginBackground as SolidColorBrush;
                Color color = ColorTool.Darken(solidBorderBrush.Color, 0.08f);
                InnerRightBottomBackground = new SolidColorBrush(color);
            }
        }
        #endregion

        #region Event
        private void VectorTextCheckBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (Text == "")
            {
                Text = "CheckButton";
            }

            if (IsChecked is bool value && value)
            {
                OriginBackground = new BrushConverter().ConvertFromString("#FFFFFF") as Brush;
                OriginInnerBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DD7929"));
            }
            else
            {
                OriginBackground = new BrushConverter().ConvertFromString("#B8BEBA") as Brush;
                OriginInnerBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B8BEBA"));
            }

            var originBackgroundBrushSource = DependencyPropertyHelper.GetValueSource(this, BackgroundProperty);
            if (originBackgroundBrushSource.BaseValueSource is BaseValueSource.Default || originBackgroundBrushSource.BaseValueSource is BaseValueSource.Style || Background is null)
            {
                Background = OriginBackground;
            }
            var originInnerBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, InnerBorderBrushProperty);
            if (originInnerBorderBrushSource.BaseValueSource is BaseValueSource.Default || originInnerBorderBrushSource.BaseValueSource is BaseValueSource.Style || InnerBorderBrush is null)
            {
                InnerBorderBrush = OriginInnerBorderBrush;
            }

            UpdateBorderColorByBackgroundColor();
        }

        protected override void OnClick()
        {
            isUserClicking = true;
            base.OnClick();
            isUserClicking = false;
        }

        protected override void OnChecked(RoutedEventArgs e)
        {
            base.OnChecked(e);
            if (IsChecked is bool)
            {
                isReFreshingBrush = true;
                Background = OriginBackground = new BrushConverter().ConvertFromString("#FFFFFF") as Brush;
                InnerBorderBrush = OriginInnerBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DD7929"));
                UpdateBorderColorByBackgroundColor();
                isReFreshingBrush = false;
            }
        }

        protected override void OnUnchecked(RoutedEventArgs e)
        {
            base.OnUnchecked(e);
            if(IsChecked is bool)
            {
                isReFreshingBrush = true;
                Background = OriginBackground = new BrushConverter().ConvertFromString("#B8BEBA") as Brush;
                InnerBorderBrush = OriginInnerBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B8BEBA"));
                UpdateBorderColorByBackgroundColor();
                isReFreshingBrush = false;
            }
        }
        #endregion
    }
}
