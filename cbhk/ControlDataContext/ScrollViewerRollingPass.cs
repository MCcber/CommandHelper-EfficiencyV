using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using CBHK.GeneralTool;

namespace CBHK.ControlDataContext
{
    public class ScrollViewerRollingPass
    {
        public void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = (sender as FrameworkElement).TemplatedParent as ScrollViewer;
            if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight || scrollViewer.VerticalOffset == 0)
            {
                ScrollViewer parent = scrollViewer.FindParent<ScrollViewer>();
                if (parent is null) return;
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
