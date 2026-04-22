using System.Windows;
using System.Windows.Controls.Primitives;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorTextRepeatButton : RepeatButton
    {
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(VectorTextRepeatButton), new PropertyMetadata(default(string)));
    }
}
