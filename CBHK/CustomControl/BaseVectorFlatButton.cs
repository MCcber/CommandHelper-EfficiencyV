using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CBHK.CustomControl
{
    public class BaseVectorFlatButton : Button
    {
        #region Field
        private SolidColorBrush OriginLeftTopBorderBrush;
        private SolidColorBrush OriginRightBottomBorderBrush;
        private SolidColorBrush OriginBorderCornerBrush;
        #endregion

        #region Property
        public Brush LeftTopBorderBrush
        {
            get { return (Brush)GetValue(LeftTopBorderBrushProperty); }
            set { SetValue(LeftTopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftTopBorderBrushProperty =
            DependencyProperty.Register("LeftTopBorderBrush", typeof(Brush), typeof(BaseVectorFlatButton), new PropertyMetadata(default(Brush)));

        public Brush RightBottomBorderBrush
        {
            get { return (Brush)GetValue(RightBottomBorderBrushProperty); }
            set { SetValue(RightBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RightBottomBorderBrushProperty =
            DependencyProperty.Register("RightBottomBorderBrush", typeof(Brush), typeof(BaseVectorFlatButton), new PropertyMetadata(default(Brush)));

        public Brush BorderCornerBrush
        {
            get { return (Brush)GetValue(BorderCornerBrushProperty); }
            set { SetValue(BorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderCornerBrushProperty =
            DependencyProperty.Register("BorderCornerBrush", typeof(Brush), typeof(BaseVectorFlatButton), new PropertyMetadata(default(Brush)));

        public Brush ThemeBackground
        {
            get { return (Brush)GetValue(ThemeBackgroundProperty); }
            set { SetValue(ThemeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeBackgroundProperty =
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(BaseVectorFlatButton), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public BaseVectorFlatButton()
        {
            SetResourceReference(ThemeBackgroundProperty,Theme.CommonBackground);
            SetResourceReference(ForegroundProperty,Theme.CommonForeground);
            Loaded += BaseVectorFlatButton_Loaded;
        }

        public virtual void UpdateBorderColorByBackgroundColor()
        {
            if (ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(themeBrush.Color);
                LeftTopBorderBrush = OriginLeftTopBorderBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color,0.2f));
                RightBottomBorderBrush = OriginRightBottomBorderBrush = new SolidColorBrush(ColorTool.Darken(themeBrush.Color,0.2f));
                BorderCornerBrush = OriginBorderCornerBrush = new SolidColorBrush(ColorTool.Desaturate(themeBrush.Color,0.4f));
            }
        }
        #endregion

        #region Event
        private void BaseVectorFlatButton_Loaded(object sender, RoutedEventArgs e)
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

            if(ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.2f));
            }
            if(OriginLeftTopBorderBrush is SolidColorBrush originLeftTopBorderBrush)
            {
                LeftTopBorderBrush = new SolidColorBrush(ColorTool.Darken(originLeftTopBorderBrush.Color, 0.2f));
            }
            if(OriginRightBottomBorderBrush is SolidColorBrush originRightBottomBorderBrush)
            {
                RightBottomBorderBrush = new SolidColorBrush(ColorTool.Darken(originRightBottomBorderBrush.Color,0.2f));
            }
            if(OriginBorderCornerBrush is SolidColorBrush originBorderCornerBrush)
            {
                BorderCornerBrush = new SolidColorBrush(ColorTool.Darken(originBorderCornerBrush.Color, 0.2f));
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

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if(ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(themeBrush.Color);
            }
            if(OriginLeftTopBorderBrush is SolidColorBrush originLeftTopBorderBrush)
            {
                LeftTopBorderBrush = new SolidColorBrush(originLeftTopBorderBrush.Color);
            }
            if(OriginRightBottomBorderBrush is SolidColorBrush originRightBottomBorderBrush)
            {
                RightBottomBorderBrush = new SolidColorBrush(originRightBottomBorderBrush.Color);
            }
            if(OriginBorderCornerBrush is SolidColorBrush originBorderCornerBrush)
            {
                BorderCornerBrush = new SolidColorBrush(originBorderCornerBrush.Color);
            }
        }
        #endregion
    }
}
