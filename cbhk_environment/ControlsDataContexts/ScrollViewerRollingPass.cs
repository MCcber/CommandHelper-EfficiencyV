using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using cbhk_environment.GeneralTools;

namespace cbhk_environment.ControlsDataContexts
{
    public class ScrollViewerRollingPass
    {
        public void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = (sender as FrameworkElement).TemplatedParent as ScrollViewer;
            if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight || scrollViewer.VerticalOffset == 0)
            {
                ScrollViewer parent = scrollViewer.FindParent<ScrollViewer>();
                if (parent == null) return;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = UIElement.MouseWheelEvent,
                    Source = sender
                };
                parent.RaiseEvent(eventArg);
            }
        }
    }
}
