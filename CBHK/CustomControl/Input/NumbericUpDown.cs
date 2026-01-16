using System.Windows;
using System.Windows.Controls;

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
        #endregion

        #region Method
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
