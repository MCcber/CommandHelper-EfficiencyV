using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.Input
{
    public class VectorNumbericUpDown : Control
    {
        #region Field

        #endregion

        #region Property
        public decimal Value
        {
            get { return (decimal)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(decimal), typeof(VectorNumbericUpDown), new PropertyMetadata(default(decimal)));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(VectorNumbericUpDown), new PropertyMetadata(default(string)));

        public string WaterMarkerText
        {
            get { return (string)GetValue(WaterMarkerTextProperty); }
            set { SetValue(WaterMarkerTextProperty, value); }
        }

        public static readonly DependencyProperty WaterMarkerTextProperty =
            DependencyProperty.Register("WaterMarkerText", typeof(string), typeof(VectorNumbericUpDown), new PropertyMetadata(default(string)));

        public Brush WaterMarkerBrush
        {
            get { return (Brush)GetValue(WaterMarkerBrushProperty); }
            set { SetValue(WaterMarkerBrushProperty, value); }
        }

        public static readonly DependencyProperty WaterMarkerBrushProperty =
            DependencyProperty.Register("WaterMarkerBrush", typeof(Brush), typeof(VectorNumbericUpDown), new PropertyMetadata(default(Brush)));

        public Thickness WaterMarkerMargin
        {
            get { return (Thickness)GetValue(WaterMarkerMarginProperty); }
            set { SetValue(WaterMarkerMarginProperty, value); }
        }

        public static readonly DependencyProperty WaterMarkerMarginProperty =
            DependencyProperty.Register("WaterMarkerMargin", typeof(Thickness), typeof(VectorNumbericUpDown), new PropertyMetadata(default(Thickness)));

        public Brush LocateLineBrush
        {
            get { return (Brush)GetValue(LocateLineBrushProperty); }
            set { SetValue(LocateLineBrushProperty, value); }
        }

        public static readonly DependencyProperty LocateLineBrushProperty =
            DependencyProperty.Register("LocateLineBrush", typeof(Brush), typeof(VectorNumbericUpDown), new PropertyMetadata(default(Brush)));

        public Brush ThemeBackground
        {
            get { return (Brush)GetValue(ThemeBackgroundProperty); }
            set { SetValue(ThemeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeBackgroundProperty =
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(VectorNumbericUpDown), new PropertyMetadata(default(Brush)));

        public bool HasText => (bool)GetValue(HasTextPropertyKey.DependencyProperty);

        private static readonly DependencyPropertyKey HasTextPropertyKey =
            DependencyProperty.RegisterReadOnly("HasText", typeof(bool), typeof(VectorNumbericUpDown), new PropertyMetadata(false));
        #endregion

        #region Method
        public VectorNumbericUpDown()
        {
            SetResourceReference(ThemeBackgroundProperty, Theme.CommonBackground);
            Loaded += VectorNumbericUpDown_Loaded;
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

        private string GetResultNumberText(decimal currentValue)
        {
            string result = "";
            if (!string.IsNullOrWhiteSpace(WaterMarkerText))
            {
                result = WaterMarkerText + ':' + currentValue;
            }
            else
            {
                result = currentValue.ToString();
            }
            return result;
        }
        #endregion

        #region Event
        private void VectorNumbericUpDown_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateBorderColorByBackgroundColor();
        }

        public void VectorNumbericUpDownTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            VectorNumbericUpDown_LostFocus(sender, e);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if(e.Property == ThemeBackgroundProperty)
            {
                UpdateBorderColorByBackgroundColor();
            }
        }

        public void VectorNumbericUpDown_GotFocus(object sender, RoutedEventArgs e)
        {
            Text = Value.ToString();
        }

        public void VectorNumbericUpDown_LostFocus(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(Text, out decimal currentValue))
            {
                Text = (!string.IsNullOrEmpty(WaterMarkerText) ? WaterMarkerText + ':' : "") + currentValue;
            }
            else
            {
                Text = (!string.IsNullOrEmpty(WaterMarkerText) ? WaterMarkerText + ':' : "") + Value;
            }
        }

        public void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            SetValue(HasTextPropertyKey, Text is not null && Text.Length > 0);
            if (decimal.TryParse(Text, out decimal result))
            {
                Value = result;
            }
        }

        public void OnIncrease_Click(object sender,RoutedEventArgs e)
        {
            Value++;
            Text = GetResultNumberText(Value);
        }

        public void OnDecrease_Click(object sender, RoutedEventArgs e)
        {
            Value--;
            Text = GetResultNumberText(Value);
        }
        #endregion
    }
}