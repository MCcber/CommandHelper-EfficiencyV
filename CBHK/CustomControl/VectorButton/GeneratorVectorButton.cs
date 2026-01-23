using CBHK.Utility.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CBHK.CustomControl.VectorButton
{
    public class GeneratorVectorButton : Button
    {
        #region Property
        private Brush OriginBackgroundBrush;
        private Brush OriginTopBottomBorderBrush;
        private Brush OriginSideBorderBrush;

        public ImageSource Icon

        {
            get { return (ImageSource)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(ImageSource), typeof(GeneratorVectorButton), new PropertyMetadata(default(ImageSource)));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(GeneratorVectorButton), new PropertyMetadata(default(string)));

        public string SubTitle
        {
            get { return (string)GetValue(SubTitleProperty); }
            set { SetValue(SubTitleProperty, value); }
        }

        public static readonly DependencyProperty SubTitleProperty =
            DependencyProperty.Register("SubTitle", typeof(string), typeof(GeneratorVectorButton), new PropertyMetadata(default(string)));

        public Brush TopBottomBorderBrush
        {
            get { return (Brush)GetValue(TopBottomBorderBrushProperty); }
            set { SetValue(TopBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty TopBottomBorderBrushProperty =
            DependencyProperty.Register("TopBottomBorderBrush", typeof(Brush), typeof(GeneratorVectorButton), new PropertyMetadata(default(Brush)));

        public Brush SideBorderBrush
        {
            get { return (Brush)GetValue(SideBorderBrushProperty); }
            set { SetValue(SideBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty SideBorderBrushProperty =
            DependencyProperty.Register("SideBorderBrush", typeof(Brush), typeof(GeneratorVectorButton), new PropertyMetadata(default(Brush)));

        #endregion

        #region Method
        public GeneratorVectorButton()
        {
            Loaded += VectorTextButton_Loaded;
            MouseEnter += VectorTextButton_MouseEnter;
            MouseLeave += VectorTextButton_MouseLeave;
            PreviewMouseLeftButtonDown += GeneratorVectorButton_PreviewMouseLeftButtonDown;
            PreviewMouseLeftButtonUp += VectorTextButton_PreviewMouseLeftButtonUp;
        }

        #endregion

        #region Event
        private void VectorTextButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (Title == "")
            {
                Title = "Button";
            }
            if(SubTitle == "")
            {
                SubTitle = "Description";
            }

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
            var originTopBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, TopBottomBorderBrushProperty);
            if (originTopBorderBrushSource.BaseValueSource is BaseValueSource.Default || originTopBorderBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.2f);
                TopBottomBorderBrush = new SolidColorBrush(color);
            }
            var originSideBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, SideBorderBrushProperty);
            if (originSideBorderBrushSource.BaseValueSource is BaseValueSource.Default || originSideBorderBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.Darken(solidBorderBrush.Color, 0.1f);
                SideBorderBrush = new SolidColorBrush(color);
            }

            OriginBackgroundBrush = Background;
            OriginTopBottomBorderBrush = TopBottomBorderBrush;
            OriginSideBorderBrush = SideBorderBrush;
        }

        private void GeneratorVectorButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            BorderBrush = Brushes.White;
            SolidColorBrush solidColorBrush = OriginBackgroundBrush as SolidColorBrush;
            Color startColor = ColorTool.Lighten(solidColorBrush.Color, 0.1f);
            Color endColor = ColorTool.Darken(solidColorBrush.Color, 0.1f);
            LinearGradientBrush linearGradientBrush = new(startColor, endColor, 90);
            Background = linearGradientBrush;
        }

        private void VectorTextButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            VectorTextButton_MouseEnter(sender, null);
        }

        private void VectorTextButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            BorderBrush = Brushes.Black;
            Background = OriginBackgroundBrush;
        }

        private void VectorTextButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Color darkColor = ColorTool.Lighten((OriginBackgroundBrush as SolidColorBrush).Color, 0.1f);
            Background = new SolidColorBrush(darkColor);
        }
        #endregion
    }
}
