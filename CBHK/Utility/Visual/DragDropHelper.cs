using System.Windows;
using System.Windows.Threading;
using static CBHK.Utility.Visual.Screen.ScreenGrabber;

namespace CBHK.Utility.Visual
{
    public static class DragDropHelper
    {
        #region Field
        private static DragAdorner dragAdorner;
        private static Window targetWindow;
        #endregion

        #region Method
        /// <summary>
        /// 核心启动方法：传入谁在拖拽，以及拖拽什么数据
        /// </summary>
        /// <param name="source"></param>
        /// <param name="data"></param>
        public static void StartDrag(UIElement source, object data,double height = 35)
        {
            targetWindow = Window.GetWindow(source);
            if (targetWindow == null)
            {
                return;
            }

            var layer = AdornerLayerHelper.GetAdornerLayer(targetWindow);
            dragAdorner = new DragAdorner(source,height)
            {
                Visibility = Visibility.Visible
            };

            layer.Add(dragAdorner);

            DragDrop.AddGiveFeedbackHandler(targetWindow, OnGiveFeedback);

            try
            {
                DragDrop.DoDragDrop(source, data, DragDropEffects.Move);
            }
            finally
            {
                dragAdorner.Visibility = Visibility.Collapsed;
                DragDrop.RemoveGiveFeedbackHandler(targetWindow, OnGiveFeedback);
                layer.Remove(dragAdorner);
                dragAdorner = null;
            }
        }
        #endregion

        #region Event
        private static void OnGiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            // 强行插队渲染，使用最准的 Win32 坐标
            targetWindow.Dispatcher.BeginInvoke(() =>
            {
                if (dragAdorner == null) return;

                Win32Point w32Point = new();
                GetCursorPos(ref w32Point);

                // 屏幕坐标 -> 窗口坐标 -> 控件内部坐标
                Point screenPoint = new(w32Point.X, w32Point.Y);
                Point windowPoint = targetWindow.PointFromScreen(screenPoint);
                // 这里假设装饰器是以窗口为参考系的，直接用 windowPoint
                dragAdorner.UpdatePosition(windowPoint);

            }, DispatcherPriority.Render);

            e.UseDefaultCursors = true;
            e.Handled = true;
        }
        #endregion
    }
}