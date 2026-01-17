using System.Windows;
using System.Windows.Controls;

namespace CBHK.CustomControl.Container
{
    public class VectorTextTabItem:TabItem
    {
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(VectorTextTabItem), new PropertyMetadata(default(string)));
    }
}
