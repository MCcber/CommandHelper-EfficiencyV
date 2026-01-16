using System.Windows;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorCheckBox
{
    public class VectorTextCheckBox:System.Windows.Controls.CheckBox
    {
        #region Property
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(VectorTextCheckBox), new PropertyMetadata(default(string)));

        public Brush SelectedBrush
        {
            get { return (Brush)GetValue(SelectedBrushProperty); }
            set { SetValue(SelectedBrushProperty, value); }
        }

        public static readonly DependencyProperty SelectedBrushProperty =
            DependencyProperty.Register("SelectedBrush", typeof(Brush), typeof(VectorTextCheckBox), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorTextCheckBox()
        {
            SelectedBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D0D4D1"));
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B8BEBA"));
            Checked += VectorTextCheckBox_Checked;
            Unchecked += VectorTextCheckBox_Unchecked;
        }

        private void VectorTextCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SelectedBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D0D4D1"));
        }

        private void VectorTextCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SelectedBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E39445"));
        }
        #endregion
    }
}
