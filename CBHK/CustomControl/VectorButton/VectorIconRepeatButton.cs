using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
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
        #endregion

        #region Property
        public int OriginBottomHeight { get; set; }

        public Brush ThemeBackground
        {
            get { return (Brush)GetValue(ThemeBackgroundProperty); }
            set { SetValue(ThemeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeBackgroundProperty =
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(VectorIconRepeatButton), new PropertyMetadata(default(Brush)));

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
            SetResourceReference(ThemeBackgroundProperty, Theme.CommonBackground);
            Loaded += VectorIconRepeatButton_Loaded;
            MouseEnter += VectorIconRepeatButton_MouseEnter;
            MouseLeave += VectorIconRepeatButton_MouseLeave;
            PreviewMouseLeftButtonDown += VectorIconRepeatButton_PreviewMouseLeftButtonDown;
            PreviewMouseLeftButtonUp += VectorIconRepeatButton_PreviewMouseLeftButtonUp;
        }

        private void SetBottomHeight(double height)
        {
            object extraBottomLine = GetTemplateChild("extraBottomLine");
            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(height, GridUnitType.Pixel);
            }
        }

        private void UpdateBorderColorByBackgroundColor()
        {
            if (ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(themeBrush.Color);
                BorderCornerBrush = OriginBorderCornerBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.4f));
                LeftTopBorderBrush = OriginLeftTopBorderBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.2f));
                RightBottomBorderBrush = OriginRightBottomBorderBrush = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.2f));
                BottomBorderBrush = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.6f));
            }
        }
        #endregion

        #region Event
        private void VectorIconRepeatButton_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateBorderColorByBackgroundColor();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == ThemeBackgroundProperty)
            {
                UpdateBorderColorByBackgroundColor();
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            OriginMargin = Margin;
            if (OriginBottomHeight == 0)
            {
                OriginBottomHeight = 6;
            }
            SetBottomHeight(OriginBottomHeight);
        }

        private void VectorIconRepeatButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            VectorIconRepeatButton_MouseEnter(sender, null);
        }

        private void VectorIconRepeatButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetBottomHeight(0);

            if (ThemeBackground is SolidColorBrush backgroundBrush)
            {
                Background = new SolidColorBrush(ColorTool.Darken(backgroundBrush.Color, 0.2f));
                LeftTopBorderBrush = new SolidColorBrush(ColorTool.Darken(((SolidColorBrush)OriginLeftTopBorderBrush).Color, 0.2f));
                RightBottomBorderBrush = new SolidColorBrush(ColorTool.Darken(((SolidColorBrush)OriginRightBottomBorderBrush).Color, 0.2f));
                BorderCornerBrush = new SolidColorBrush(ColorTool.Darken(((SolidColorBrush)OriginBorderCornerBrush).Color, 0.2f));
            }

            Margin = new(Margin.Left, Margin.Top + 2, Margin.Right, Margin.Bottom);
        }

        private void VectorIconRepeatButton_MouseLeave(object sender, MouseEventArgs e)
        {
            SetBottomHeight(OriginBottomHeight);
            Background = ThemeBackground;
            LeftTopBorderBrush = OriginLeftTopBorderBrush;
            RightBottomBorderBrush = OriginRightBottomBorderBrush;
            BorderCornerBrush = OriginBorderCornerBrush;
            Margin = OriginMargin;
        }

        private void VectorIconRepeatButton_MouseEnter(object sender, MouseEventArgs e)
        {
            SetBottomHeight(OriginBottomHeight);

            Margin = OriginMargin;
            if(ThemeBackground is SolidColorBrush backgroundBrush)
            {
                Background = new SolidColorBrush(ColorTool.Darken(backgroundBrush.Color, 0.2f));
            }
            if(OriginLeftTopBorderBrush is SolidColorBrush originLeftTopBorderBrush)
            {
                LeftTopBorderBrush = new SolidColorBrush(ColorTool.Lighten(originLeftTopBorderBrush.Color, 0.2f));
            }
            if(OriginRightBottomBorderBrush is SolidColorBrush originRightBottomBorderBrush)
            {
                RightBottomBorderBrush = new SolidColorBrush(ColorTool.Lighten(originRightBottomBorderBrush.Color, 0.2f));
            }
            if(OriginBorderCornerBrush is SolidColorBrush originBorderCornerBrush)
            {
                BorderCornerBrush = new SolidColorBrush(ColorTool.Lighten(originBorderCornerBrush.Color, 0.4f));
            }
        }
        #endregion
    }
}
