using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CBHK.CustomControl.Container
{
    public class VectorTextMenuItem:MenuItem
    {
        #region Field
        private Brush OriginLeftTopBorderBrush;
        private Brush OriginRightBottomBorderBrush;
        private Brush OriginBorderCornerBrush;
        #endregion

        #region Property
        public Brush LeftTopBorderBrush
        {
            get { return (Brush)GetValue(LeftTopBorderBrushProperty); }
            set { SetValue(LeftTopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftTopBorderBrushProperty =
            DependencyProperty.Register("LeftTopBorderBrush", typeof(Brush), typeof(VectorTextMenuItem), new PropertyMetadata(default(Brush)));

        public Brush RightBottomBorderBrush
        {
            get { return (Brush)GetValue(RightBottomBorderBrushProperty); }
            set { SetValue(RightBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RightBottomBorderBrushProperty =
            DependencyProperty.Register("RightBottomBorderBrush", typeof(Brush), typeof(VectorTextMenuItem), new PropertyMetadata(default(Brush)));

        public Brush BorderCornerBrush
        {
            get { return (Brush)GetValue(BorderCornerBrushProperty); }
            set { SetValue(BorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderCornerBrushProperty =
            DependencyProperty.Register("BorderCornerBrush", typeof(Brush), typeof(VectorTextMenuItem), new PropertyMetadata(default(Brush)));

        public Brush ThemeBackground
        {
            get { return (Brush)GetValue(ThemeBackgroundProperty); }
            set { SetValue(ThemeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeBackgroundProperty =
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(VectorTextMenuItem), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorTextMenuItem()
        {
            SetResourceReference(ThemeBackgroundProperty, Theme.CommonBackground);
            SetResourceReference(ForegroundProperty, Theme.CommonForeground);
            Loaded += VectorTextMenuItem_Loaded;
        }

        public void UpdateBorderColorByBackgroundColor()
        {
            if(ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(themeBrush.Color);
                LeftTopBorderBrush = OriginLeftTopBorderBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.2f));
                RightBottomBorderBrush = OriginRightBottomBorderBrush = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.2f));
                BorderCornerBrush = OriginBorderCornerBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.3f));
            }
        }
        #endregion

        #region Property
        private void VectorTextMenuItem_Loaded(object sender, RoutedEventArgs e)
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

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);

            if (ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.2f));
                LeftTopBorderBrush = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.2f));
                RightBottomBorderBrush = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.2f));
                BorderCornerBrush = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.2f));
            }
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);
            OnMouseEnter(e);
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            if(ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.2f));
                LeftTopBorderBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.2f));
                RightBottomBorderBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.2f));
                BorderCornerBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.2f));
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            Background = ThemeBackground;
            LeftTopBorderBrush = OriginLeftTopBorderBrush;
            RightBottomBorderBrush = OriginRightBottomBorderBrush;
            BorderCornerBrush = OriginBorderCornerBrush;
        }
        #endregion
    }
}