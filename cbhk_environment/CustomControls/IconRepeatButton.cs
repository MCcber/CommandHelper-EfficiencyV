using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace cbhk_environment.CustomControls
{
    public class IconRepeatButton:RepeatButton
    {
        public Brush PressedBackground
        {
            get { return (Brush)GetValue(PressedBackgroundProperty); }
            set { SetValue(PressedBackgroundProperty, value); }
        }

        public static readonly DependencyProperty PressedBackgroundProperty =
            DependencyProperty.Register("PressedBackground", typeof(Brush), typeof(IconRepeatButton), new PropertyMetadata(default(Brush)));
    }
}
