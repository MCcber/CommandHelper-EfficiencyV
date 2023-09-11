using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace cbhk_environment.CustomControls
{
    public class TextToggleButtons:ToggleButton
    {
        public ImageBrush SelectedBackground
        {
            get { return (ImageBrush)GetValue(SelectedBackgroundProperty); }
            set { SetValue(SelectedBackgroundProperty, value); }
        }

        public static readonly DependencyProperty SelectedBackgroundProperty =
            DependencyProperty.Register("SelectedBackground", typeof(ImageBrush), typeof(TextToggleButtons), new PropertyMetadata(default(ImageBrush)));
    }
}
