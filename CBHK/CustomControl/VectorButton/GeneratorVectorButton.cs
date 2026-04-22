using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class GeneratorVectorButton : Button
    {
        #region Field
        private Brush OriginBackgroundBrush;
        private Brush OriginTopBottomBorderBrush;
        private Brush OriginSideBorderBrush;
        #endregion

        #region Property
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

        public Brush ThemeBackground
        {
            get { return (Brush)GetValue(ThemeBackgroundProperty); }
            set { SetValue(ThemeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeBackgroundProperty =
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(GeneratorVectorButton), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public GeneratorVectorButton()
        {
            SetResourceReference(ThemeBackgroundProperty, Theme.CommonBackground);
            Loaded += VectorTextButton_Loaded;
            MouseEnter += VectorTextButton_MouseEnter;
            MouseLeave += VectorTextButton_MouseLeave;
            PreviewMouseLeftButtonDown += GeneratorVectorButton_PreviewMouseLeftButtonDown;
            PreviewMouseLeftButtonUp += VectorTextButton_PreviewMouseLeftButtonUp;
        }

        private void UpdateBorderColorByBackgroundColor()
        {
            BorderBrush = Brushes.Black;
            if(ThemeBackground is SolidColorBrush themeBrush)
            {
                OriginBackgroundBrush = new SolidColorBrush(themeBrush.Color);
                Color startColor = ColorTool.Lighten(themeBrush.Color, 0.1f);
                Color endColor = ColorTool.Darken(themeBrush.Color, 0.1f);
                LinearGradientBrush linearGradientBrush = new(startColor, endColor, 90);
                Background = linearGradientBrush;

                TopBottomBorderBrush = OriginTopBottomBorderBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.1f));
                SideBorderBrush = OriginSideBorderBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.1f));
            }
        }
        #endregion

        #region Event
        private void VectorTextButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Title))
            {
                Title = "Button";
            }
            if(string.IsNullOrEmpty(SubTitle))
            {
                SubTitle = "Description";
            }

            UpdateBorderColorByBackgroundColor();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if(e.Property == ThemeBackgroundProperty)
            {
                UpdateBorderColorByBackgroundColor();
            }
        }

        private void GeneratorVectorButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            BorderBrush = Brushes.White;
        }

        private void VectorTextButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            VectorTextButton_MouseEnter(sender, null);
        }

        private void VectorTextButton_MouseLeave(object sender, MouseEventArgs e)
        {
            BorderBrush = Brushes.Black;

            if(OriginBackgroundBrush is SolidColorBrush solidColorBrush)
            {
                Color startColor = ColorTool.Lighten(solidColorBrush.Color, 0.1f);
                Color endColor = ColorTool.Darken(solidColorBrush.Color, 0.1f);
                LinearGradientBrush linearGradientBrush = new(startColor, endColor, 90);
                Background = linearGradientBrush;
            }
        }

        private void VectorTextButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if(OriginBackgroundBrush is SolidColorBrush solidColorBrush)
            {
                Color startColor = ColorTool.Lighten(solidColorBrush.Color, 0.2f);
                Color endColor = ColorTool.Darken(solidColorBrush.Color, 0.2f);
                LinearGradientBrush result = new(startColor, endColor, 90);
                Background = result;
            }
        }
        #endregion
    }
}
