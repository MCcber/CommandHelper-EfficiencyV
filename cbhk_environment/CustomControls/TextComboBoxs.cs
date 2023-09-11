using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace cbhk_environment.CustomControls
{
    public class TextComboBoxs:ComboBox
    {
        public Brush InputFieldBackground
        {
            get { return (Brush)GetValue(InputFieldBackgroundProperty); }
            set { SetValue(InputFieldBackgroundProperty, value); }
        }

        public static readonly DependencyProperty InputFieldBackgroundProperty =
            DependencyProperty.Register("InputFieldBackground", typeof(Brush), typeof(TextComboBoxs), new PropertyMetadata(default(Brush)));

        public Brush TextBoxBorderBrush
        {
            get { return (Brush)GetValue(TextBoxBorderBrushProperty); }
            set { SetValue(TextBoxBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty TextBoxBorderBrushProperty =
            DependencyProperty.Register("TextBoxBorderBrush", typeof(Brush), typeof(TextComboBoxs), new PropertyMetadata(default(Brush)));

        public Thickness TextBoxThickness
        {
            get { return (Thickness)GetValue(TextBoxThicknessProperty); }
            set { SetValue(TextBoxThicknessProperty, value); }
        }

        public static readonly DependencyProperty TextBoxThicknessProperty =
            DependencyProperty.Register("TextBoxThickness", typeof(Thickness), typeof(TextComboBoxs), new PropertyMetadata(default(Thickness)));

        public Brush PopupBackground
        {
            get { return (Brush)GetValue(PopupBackgroundProperty); }
            set { SetValue(PopupBackgroundProperty, value); }
        }

        public static readonly DependencyProperty PopupBackgroundProperty =
            DependencyProperty.Register("PopupBackground", typeof(Brush), typeof(TextComboBoxs), new PropertyMetadata(default(Brush)));
        public Brush ArrowBackground
        {
            get { return (Brush)GetValue(ArrowBackgroundProperty); }
            set { SetValue(ArrowBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ArrowBackgroundProperty =
            DependencyProperty.Register("ArrowBackground", typeof(Brush), typeof(TextComboBoxs), new PropertyMetadata(default(Brush)));
    }
}
