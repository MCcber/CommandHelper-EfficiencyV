using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CBHK.CustomControl.Input
{
    public class VectorTextBox:TextBox
    {
        #region Property
        public string WaterMarkerText
        {
            get { return (string)GetValue(WaterMarkerTextProperty); }
            set { SetValue(WaterMarkerTextProperty, value); }
        }

        public static readonly DependencyProperty WaterMarkerTextProperty =
            DependencyProperty.Register("WaterMarkerText", typeof(string), typeof(VectorTextBox), new PropertyMetadata(default(string)));

        public Brush WaterMarkerBrush
        {
            get { return (Brush)GetValue(WaterMarkerBrushProperty); }
            set { SetValue(WaterMarkerBrushProperty, value); }
        }

        public static readonly DependencyProperty WaterMarkerBrushProperty =
            DependencyProperty.Register("WaterMarkerBrush", typeof(Brush), typeof(VectorTextBox), new PropertyMetadata(default(Brush)));

        public Brush LocateLineBrush
        {
            get { return (Brush)GetValue(LocateLineBrushProperty); }
            set { SetValue(LocateLineBrushProperty, value); }
        }

        public static readonly DependencyProperty LocateLineBrushProperty =
            DependencyProperty.Register("LocateLineBrush", typeof(Brush), typeof(VectorTextBox), new PropertyMetadata(default(Brush)));

        public Brush ThemeBackground
        {
            get { return (Brush)GetValue(ThemeBackgroundProperty); }
            set { SetValue(ThemeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeBackgroundProperty =
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(VectorTextBox), new PropertyMetadata(default(Brush)));

        public Thickness WaterMarkerMargin
        {
            get { return (Thickness)GetValue(WaterMarkerMarginProperty); }
            set { SetValue(WaterMarkerMarginProperty, value); }
        }

        public static readonly DependencyProperty WaterMarkerMarginProperty =
            DependencyProperty.Register("WaterMarkerMargin", typeof(Thickness), typeof(VectorTextBox), new PropertyMetadata(default(Thickness)));

        public bool HasText => (bool)GetValue(HasTextPropertyKey.DependencyProperty);

        private static readonly DependencyPropertyKey HasTextPropertyKey =
            DependencyProperty.RegisterReadOnly("HasText", typeof(bool), typeof(VectorTextBox), new PropertyMetadata(false));
        #endregion

        #region Method
        public VectorTextBox()
        {
            SetResourceReference(ThemeBackgroundProperty, Theme.CommonBackground);
            Loaded += VectorTextBox_Loaded;
        }

        public void UpdateBorderColorByBackgroundColor()
        {
            if (ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(themeBrush.Color);
                WaterMarkerBrush = ColorTool.GetWatermarkBrush(themeBrush);
                BorderBrush = LocateLineBrush = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.2f));
            }
        }
        #endregion

        #region Event
        private void VectorTextBox_Loaded(object sender, RoutedEventArgs e)
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

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
            SetValue(HasTextPropertyKey, Text is not null && Text.Length > 0);
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            if(!IsFocused && ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.1f));
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(themeBrush.Color);
            }
        }
        #endregion
    }
}