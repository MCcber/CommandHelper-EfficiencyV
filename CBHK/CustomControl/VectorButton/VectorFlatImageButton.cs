using CBHK.Utility.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorFlatImageButton:Button
    {
        #region Property
        private Brush OriginBackground;
        private Brush OriginLeftTopBorderBrush;
        private Brush OriginRightBottomBorderBrush;
        private Brush OriginCornerBorderBrush;

        public ImageSource Image
        {
            get { return (ImageSource)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register("Image", typeof(ImageSource), typeof(VectorFlatImageButton), new PropertyMetadata(default(ImageSource)));

        public Brush LeftTopBorderBrush
        {
            get { return (Brush)GetValue(LeftTopBorderBrushProperty); }
            set { SetValue(LeftTopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftTopBorderBrushProperty =
            DependencyProperty.Register("LeftTopBorderBrush", typeof(Brush), typeof(VectorFlatImageButton), new PropertyMetadata(default(Brush)));

        public Brush RightBottomBorderBrush
        {
            get { return (Brush)GetValue(RightBottomBorderBrushProperty); }
            set { SetValue(RightBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RightBottomBorderBrushProperty =
            DependencyProperty.Register("RightBottomBorderBrush", typeof(Brush), typeof(VectorFlatImageButton), new PropertyMetadata(default(Brush)));

        public Brush CornerBorderBrush
        {
            get { return (Brush)GetValue(CornerBorderBrushProperty); }
            set { SetValue(CornerBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty CornerBorderBrushProperty =
            DependencyProperty.Register("CornerBorderBrush", typeof(Brush), typeof(VectorFlatImageButton), new PropertyMetadata(default(Brush)));

        public Thickness ImageMargin
        {
            get { return (Thickness)GetValue(ImageMarginProperty); }
            set { SetValue(ImageMarginProperty, value); }
        }

        public static readonly DependencyProperty ImageMarginProperty =
            DependencyProperty.Register("ImageMargin", typeof(Thickness), typeof(VectorFlatImageButton), new PropertyMetadata(default(Thickness)));
        #endregion

        #region Method
        public VectorFlatImageButton()
        {
            Loaded += VectorFlatImageButton_Loaded;
            MouseEnter += VectorFlatImageButton_MouseEnter;
            MouseLeave += VectorFlatImageButton_MouseLeave;
            PreviewMouseLeftButtonDown += VectorFlatImageButton_PreviewMouseLeftButtonDown;
            PreviewMouseLeftButtonUp += VectorFlatImageButton_PreviewMouseLeftButtonUp;
        }

        private void UpdateOriginBorderBrush()
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
        private void VectorFlatImageButton_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateOriginBorderBrush();
        }

        private void VectorFlatImageButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            VectorFlatImageButton_MouseEnter(sender, null);
        }

        private void VectorFlatImageButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Color lightBackgroundColor = ColorTool.Darken((OriginBackground as SolidColorBrush).Color, 0.2f);
            Background = new SolidColorBrush(lightBackgroundColor);
            Color lightLeftTopBorderColor = ColorTool.Darken((OriginLeftTopBorderBrush as SolidColorBrush).Color, 0.1f);
            LeftTopBorderBrush = new SolidColorBrush(lightLeftTopBorderColor);
            Color lightRightBottomBorderColor = ColorTool.Darken((RightBottomBorderBrush as SolidColorBrush).Color, 0.2f);
            RightBottomBorderBrush = new SolidColorBrush(lightRightBottomBorderColor);
            Color lightCornerColor = ColorTool.Darken((OriginCornerBorderBrush as SolidColorBrush).Color, 0.2f);
            CornerBorderBrush = new SolidColorBrush(lightCornerColor);
        }

        private void VectorFlatImageButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Background = OriginBackground;
            LeftTopBorderBrush = OriginLeftTopBorderBrush;
            RightBottomBorderBrush = OriginRightBottomBorderBrush;
            CornerBorderBrush = OriginCornerBorderBrush;
        }

        private void VectorFlatImageButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
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
        #endregion
    }
}
