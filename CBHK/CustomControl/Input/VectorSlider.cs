using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace CBHK.CustomControl.Input
{
    public class VectorSlider : Slider
    {
        #region Field
        private double ThumbBottomBorderHeight = 4;
        private Brush OriginThumbBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F4F6F9"));
        #endregion

        #region Property
        public double SlidingAreaHeight
        {
            get { return (double)GetValue(SlidingAreaHeightProperty); }
            set { SetValue(SlidingAreaHeightProperty, value); }
        }

        public static readonly DependencyProperty SlidingAreaHeightProperty =
    DependencyProperty.Register("SlidingAreaHeight", typeof(double), typeof(VectorSlider), new PropertyMetadata(default(double)));

        public Brush ThemeBackground
        {
            get { return (Brush)GetValue(ThemeBackgroundProperty); }
            set { SetValue(ThemeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeBackgroundProperty =
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(VectorSlider), new PropertyMetadata(default(Brush)));

        public Brush ThumbBackground
        {
            get { return (Brush)GetValue(ThumbBackgroundProperty); }
            set { SetValue(ThumbBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThumbBackgroundProperty =
            DependencyProperty.Register("ThumbBackground", typeof(Brush), typeof(VectorSlider), new PropertyMetadata(default(Brush)));

        public Brush ThumbRoundBorderBrush
        {
            get { return (Brush)GetValue(ThumbRoundBorderBrushProperty); }
            set { SetValue(ThumbRoundBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty ThumbRoundBorderBrushProperty =
            DependencyProperty.Register("ThumbRoundBorderBrush", typeof(Brush), typeof(VectorSlider), new PropertyMetadata(default(Brush)));

        public Brush ThumbBorderCornerBrush
        {
            get { return (Brush)GetValue(ThumbBorderCornerBrushProperty); }
            set { SetValue(ThumbBorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty ThumbBorderCornerBrushProperty =
            DependencyProperty.Register("ThumbBorderCornerBrush", typeof(Brush), typeof(VectorSlider), new PropertyMetadata(default(Brush)));

        public Brush ThumbBottomBorderBrush
        {
            get { return (Brush)GetValue(ThumbBottomBorderBrushProperty); }
            set { SetValue(ThumbBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty ThumbBottomBorderBrushProperty =
            DependencyProperty.Register("ThumbBottomBorderBrush", typeof(Brush), typeof(VectorSlider), new PropertyMetadata(default(Brush)));

        public Brush BackgroundRoundBorderBrush
        {
            get { return (Brush)GetValue(BackgroundRoundBorderBrushProperty); }
            set { SetValue(BackgroundRoundBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty BackgroundRoundBorderBrushProperty =
            DependencyProperty.Register("BackgroundRoundBorderBrush", typeof(Brush), typeof(VectorSlider), new PropertyMetadata(default(Brush)));

        public Brush BackgroundBorderCornerBrush
        {
            get { return (Brush)GetValue(BackgroundBorderCornerBrushProperty); }
            set { SetValue(BackgroundBorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BackgroundBorderCornerBrushProperty =
            DependencyProperty.Register("BackgroundBorderCornerBrush", typeof(Brush), typeof(VectorSlider), new PropertyMetadata(default(Brush)));

        public Brush BackgroundBottomBorderBrush
        {
            get { return (Brush)GetValue(BackgroundBottomBorderBrushProperty); }
            set { SetValue(BackgroundBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty BackgroundBottomBorderBrushProperty =
            DependencyProperty.Register("BackgroundBottomBorderBrush", typeof(Brush), typeof(VectorSlider), new PropertyMetadata(default(Brush)));

        public double ThumbWidth
        {
            get { return (double)GetValue(ThumbWidthProperty); }
            set { SetValue(ThumbWidthProperty, value); }
        }

        public static readonly DependencyProperty ThumbWidthProperty =
            DependencyProperty.Register("ThumbWidth", typeof(double), typeof(VectorSlider), new PropertyMetadata(default(double)));

        public double ThumbHeight
        {
            get { return (double)GetValue(ThumbHeightProperty); }
            set { SetValue(ThumbHeightProperty, value); }
        }

        public static readonly DependencyProperty ThumbHeightProperty =
            DependencyProperty.Register("ThumbHeight", typeof(double), typeof(VectorSlider), new PropertyMetadata(default(double)));
        #endregion

        #region Method
        public VectorSlider()
        {
            SetResourceReference(ThemeBackgroundProperty, Theme.CommonBackground);
            ThumbBackground = OriginThumbBackground;
            BorderBrush = Brushes.Black;
            Loaded += VectorSlider_Loaded;
            MouseEnter += VectorSlider_MouseEnter;
            MouseLeave += VectorSlider_MouseLeave;
            PreviewMouseLeftButtonDown += VectorSlider_PreviewMouseLeftButtonDown;
            PreviewMouseLeftButtonUp += VectorSlider_PreviewMouseLeftButtonUp;
        }

        private void UpdateBorderColorByBackgroundColor()
        {
            ThumbBackground = OriginThumbBackground;
            if (ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.2f));
                BackgroundBorderCornerBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.4f));
                BackgroundRoundBorderBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.2f));
                BackgroundBottomBorderBrush = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.1f));
            }

            if (OriginThumbBackground is SolidColorBrush thumbBackgroundBrush)
            {
                ThumbBorderCornerBrush = new SolidColorBrush(ColorTool.Lighten(thumbBackgroundBrush.Color, 0.4f));
                ThumbRoundBorderBrush = new SolidColorBrush(ColorTool.Lighten(thumbBackgroundBrush.Color, 0.2f));
                ThumbBottomBorderBrush = new SolidColorBrush(ColorTool.Darken(thumbBackgroundBrush.Color, 0.6f));
            }
        }
        #endregion

        #region Event
        private void VectorSlider_Loaded(object sender, RoutedEventArgs e)
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

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("PART_Track") is Track track && track.Thumb is not null)
            {
                track.Thumb.ApplyTemplate();
                object extraBottomLine = track.Thumb.Template.FindName("extraBottomLine", track.Thumb);
                if (extraBottomLine is RowDefinition row)
                {
                    row.Height = new(ThumbBottomBorderHeight, GridUnitType.Pixel);
                }
            }
        }

        private void VectorSlider_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            VectorSlider_MouseEnter(sender, null);
        }

        private void VectorSlider_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (OriginThumbBackground is SolidColorBrush solidColorBrush)
            {
                ThumbBackground = new SolidColorBrush(ColorTool.Darken(solidColorBrush.Color, 0.3f));
            }
        }

        private void VectorSlider_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ThumbBackground = OriginThumbBackground;
        }

        private void VectorSlider_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (OriginThumbBackground is SolidColorBrush solidColorBrush)
            {
                ThumbBackground = new SolidColorBrush(ColorTool.Darken(solidColorBrush.Color, 0.2f));
            }
        }
        #endregion
    }
}
