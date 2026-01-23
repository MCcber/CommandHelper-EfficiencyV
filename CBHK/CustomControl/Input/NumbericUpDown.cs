using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.Input
{
    public class NumbericUpDown : Control
    {
        #region Property
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(NumbericUpDown), new PropertyMetadata(default(string)));

        public string WaterMarkerText
        {
            get { return (string)GetValue(WaterMarkerTextProperty); }
            set { SetValue(WaterMarkerTextProperty, value); }
        }

        public static readonly DependencyProperty WaterMarkerTextProperty =
            DependencyProperty.Register("WaterMarkerText", typeof(string), typeof(NumbericUpDown), new PropertyMetadata(default(string)));

        public Brush WaterMarkerBrush
        {
            get { return (Brush)GetValue(WaterMarkerBrushProperty); }
            set { SetValue(WaterMarkerBrushProperty, value); }
        }

        public static readonly DependencyProperty WaterMarkerBrushProperty =
            DependencyProperty.Register("WaterMarkerBrush", typeof(Brush), typeof(NumbericUpDown), new PropertyMetadata(default(Brush)));

        public Thickness WaterMarkerMargin
        {
            get { return (Thickness)GetValue(WaterMarkerMarginProperty); }
            set { SetValue(WaterMarkerMarginProperty, value); }
        }

        public static readonly DependencyProperty WaterMarkerMarginProperty =
            DependencyProperty.Register("WaterMarkerMargin", typeof(Thickness), typeof(NumbericUpDown), new PropertyMetadata(default(Thickness)));

        public Brush LocateLineBrush
        {
            get { return (Brush)GetValue(LocateLineBrushProperty); }
            set { SetValue(LocateLineBrushProperty, value); }
        }

        public static readonly DependencyProperty LocateLineBrushProperty =
            DependencyProperty.Register("LocateLineBrush", typeof(Brush), typeof(NumbericUpDown), new PropertyMetadata(default(Brush)));

        public bool HasText => (bool)GetValue(HasTextPropertyKey.DependencyProperty);

        private static readonly DependencyPropertyKey HasTextPropertyKey =
            DependencyProperty.RegisterReadOnly("HasText", typeof(bool), typeof(NumbericUpDown), new PropertyMetadata(false));
        #endregion

        #region Method
        public NumbericUpDown()
        {
            Foreground = Brushes.White;
            WaterMarkerBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D6D6D6"));
            LocateLineBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#242425"));
        }
        #endregion

        #region Event
        public void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            SetValue(HasTextPropertyKey, Text is not null && Text.Length > 0);
        }

        public void OnIncrease_Click(object sender,RoutedEventArgs e)
        {
            if (int.TryParse(Text, out int value))
            {
                value++;
                Text = value.ToString();
            }
        }

        public void OnDecrease_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(Text, out int value))
            {
                value--;
                Text = value.ToString();
            }
        }
        #endregion
    }
}
