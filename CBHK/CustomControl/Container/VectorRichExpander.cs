using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.Container
{
    public class VectorRichExpander : Expander
    {
        #region Field
        private Brush OriginLeftTopBorderBrush;
        private Brush OriginRightBottomBorderBrush;
        private Brush OriginBorderCornerBrush;
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

        public Brush BorderCornerBrush
        {
            get { return (Brush)GetValue(BorderCornerBrushProperty); }
            set { SetValue(BorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderCornerBrushProperty =
            DependencyProperty.Register("BorderCornerBrush", typeof(Brush), typeof(VectorRichExpander), new PropertyMetadata(default(Brush)));

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

        public Brush ThemeBackground
        {
            get { return (Brush)GetValue(ThemeBackgroundProperty); }
            set { SetValue(ThemeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeBackgroundProperty =
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(VectorRichExpander), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorRichExpander()
        {
            SetResourceReference(ThemeBackgroundProperty, Theme.CommonBackground);
            SetResourceReference(ForegroundProperty,Theme.CommonForeground);
            SetResourceReference(ArrowBrushProperty, Theme.CommonForeground);
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
            if(ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(themeBrush.Color);
                LeftTopBorderBrush = OriginLeftTopBorderBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.2f));
                RightBottomBorderBrush = OriginRightBottomBorderBrush = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.4f));
                BorderCornerBrush = OriginBorderCornerBrush = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.2f));
            }
        }
        #endregion

        #region Event
        private void VectorRichExpander_Loaded(object sender, RoutedEventArgs e)
        {
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

        private void VectorRichExpander_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            VectorRichExpander_MouseEnter(sender, null);
        }

        private void VectorRichExpander_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if(ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.2f));
            }
            if(OriginLeftTopBorderBrush is SolidColorBrush originLeftTopBorderBrush)
            {
                LeftTopBorderBrush = new SolidColorBrush(ColorTool.Darken(originLeftTopBorderBrush.Color, 0.4f));
            }
            if(OriginRightBottomBorderBrush is SolidColorBrush originRightBottomBorderBrush)
            {
                RightBottomBorderBrush = new SolidColorBrush(ColorTool.Darken(originRightBottomBorderBrush.Color, 0.2f));
            }
            if(OriginBorderCornerBrush is SolidColorBrush originBorderCornerBrush)
            {
                BorderCornerBrush = new SolidColorBrush(ColorTool.Darken(originBorderCornerBrush.Color, 0.2f));
            }
        }

        private void VectorRichExpander_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            LeftTopBorderBrush = OriginLeftTopBorderBrush;
            RightBottomBorderBrush = OriginRightBottomBorderBrush;
            BorderCornerBrush = OriginBorderCornerBrush;
        }

        private void VectorRichExpander_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.1f));
            }
            if (OriginLeftTopBorderBrush is SolidColorBrush originLeftTopBorderBrush)
            {
                LeftTopBorderBrush = new SolidColorBrush(ColorTool.Lighten(originLeftTopBorderBrush.Color, 0.1f));
            }
            if (OriginRightBottomBorderBrush is SolidColorBrush originRightBottomBorderBrush)
            {
                RightBottomBorderBrush = new SolidColorBrush(ColorTool.Lighten(originRightBottomBorderBrush.Color, 0.1f));
            }
            if (OriginBorderCornerBrush is SolidColorBrush originBorderCornerBrush)
            {
                BorderCornerBrush = new SolidColorBrush(ColorTool.Lighten(originBorderCornerBrush.Color, 0.1f));
            }
        }

        private void VectorRichExpander_Expanded(object sender, RoutedEventArgs e) => RotationAngle = 180;

        private void VectorRichExpander_Collapsed(object sender, RoutedEventArgs e) => RotationAngle = 0;
        #endregion
    }
}