using CBHK.Utility.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorFlatTextButton : Button
    {
        #region Field
        private new bool IsLoaded = false;
        #endregion

        #region Property
        private Brush OriginBackgroundBrush { get; set; }
        private Brush OriginForegroundBrush { get; set; }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(VectorFlatTextButton), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty OriginBackgroundBrushProperty =
            DependencyProperty.Register("OriginBackgroundBrush", typeof(Brush), typeof(VectorFlatTextButton), new PropertyMetadata(default(Brush)));

        public Brush LeftTopBorderBrush
        {
            get { return (Brush)GetValue(LeftTopBorderBrushProperty); }
            set { SetValue(LeftTopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftTopBorderBrushProperty =
            DependencyProperty.Register("LeftTopBorderBrush", typeof(Brush), typeof(VectorFlatTextButton), new PropertyMetadata(default(Brush)));

        public Brush RightBottomBorderBrush
        {
            get { return (Brush)GetValue(RightBottomBorderBrushProperty); }
            set { SetValue(RightBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RightBottomBorderBrushProperty =
            DependencyProperty.Register("RightBottomBorderBrush", typeof(Brush), typeof(VectorFlatTextButton), new PropertyMetadata(default(Brush)));

        public Brush BorderCornerBrush
        {
            get { return (Brush)GetValue(BorderCornerBrushProperty); }
            set { SetValue(BorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderCornerBrushProperty =
            DependencyProperty.Register("BorderCornerBrush", typeof(Brush), typeof(VectorFlatTextButton), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorFlatTextButton()
        {
            ClickMode = ClickMode.Press;
            Loaded += VectorFlatTextButton_Loaded;
            MouseEnter += VectorFlatTextButton_MouseEnter;
            MouseLeave += VectorFlatTextButton_MouseLeave;
            PreviewMouseLeftButtonDown += VectorFlatTextButton_PreviewMouseLeftButtonDown;
            //PreviewMouseLeftButtonUp += VectorFlatTextButton_PreviewMouseLeftButtonUp;
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
                Color color = ColorTool.Darken(solidBorderBrush.Color, 0.3f);
                BorderCornerBrush = new SolidColorBrush(color);
            }
            var originTopBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, LeftTopBorderBrushProperty);
            if (originTopBorderBrushSource.BaseValueSource is BaseValueSource.Default || originTopBorderBrushSource.BaseValueSource is BaseValueSource.Style || IsLoaded)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.3f);
                LeftTopBorderBrush = new SolidColorBrush(color);
            }
            var originBottomBrushSource = DependencyPropertyHelper.GetValueSource(this, RightBottomBorderBrushProperty);
            if (originBottomBrushSource.BaseValueSource is BaseValueSource.Default || originBottomBrushSource.BaseValueSource is BaseValueSource.Style || IsLoaded)
            {
                Color color = ColorTool.Darken((Background as SolidColorBrush).Color, 0.5f);
                RightBottomBorderBrush ??= new SolidColorBrush(color);
            }
        }
        #endregion

        #region Event
        private void VectorFlatTextButton_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateBorderColorByBackgroundColor();
            OriginForegroundBrush = Foreground;
            OriginBackgroundBrush = Background;
            IsLoaded = true;
        }

        private void VectorFlatTextButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D77933"));
            BorderBrush = Brushes.White;

            UpdateBorderColorByBackgroundColor();
        }

        private void VectorFlatTextButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            BorderBrush = Brushes.Black;
            Background = OriginBackgroundBrush;
            UpdateBorderColorByBackgroundColor();
        }

        private void VectorFlatTextButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D77933"));
            BorderBrush = Brushes.White;

            UpdateBorderColorByBackgroundColor();
        }
        #endregion
    }
}
