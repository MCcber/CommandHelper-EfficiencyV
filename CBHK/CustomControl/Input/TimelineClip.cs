using System.Windows;
using System.Windows.Controls;

namespace CBHK.CustomControl.Input
{
    public class TimelineClip : Control
    {
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(TimelineClip), new PropertyMetadata(default(string)));

        public string TimeText
        {
            get { return (string)GetValue(TimeTextProperty); }
            set { SetValue(TimeTextProperty, value); }
        }

        public static readonly DependencyProperty TimeTextProperty =
            DependencyProperty.Register("TimeText", typeof(string), typeof(TimelineClip), new PropertyMetadata(default(string)));
    }
}
