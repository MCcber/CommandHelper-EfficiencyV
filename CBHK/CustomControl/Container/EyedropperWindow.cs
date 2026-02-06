using CBHK.Utility.Visual;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CBHK.CustomControl.Container
{
    public class EyedropperWindow:Window
    {
        #region Field
        // 定义一个事件把选中的颜色传回去
        public event Action<Color> ColorPicked;
        private System.Windows.Threading.DispatcherTimer timer;
        private Border PreviewColorBox;
        private Grid MagnifierGrid;
        #endregion

        public EyedropperWindow()
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            // 覆盖所有屏幕
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;

            // 使用 Timer 高频更新预览颜色，比 MouseMove 更流畅且性能更好
            timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(20) // 50 FPS
            };
            timer.Tick += UpdatePreview;
            timer.Start();
        }

        private void ConfirmPick()
        {
            timer.Stop();
            if (Mouse.Captured == this)
            {
                Mouse.Capture(null);
            }
            var mousePos = ScreenGrabber.GetMousePosition();
            var color = ScreenGrabber.GetColorAt(mousePos);

            ColorPicked?.Invoke(color);
            Close();
        }

        #region Event
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            MagnifierGrid = GetTemplateChild("MagnifierGrid") as Grid;
            PreviewColorBox = GetTemplateChild("PreviewColorBox") as Border;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            // 窗体激活时，强制捕获鼠标，确保点击不会漏掉
            Mouse.Capture(this, CaptureMode.SubTree);
        }

        private void UpdatePreview(object sender, EventArgs e)
        {
            var mousePos = ScreenGrabber.GetMousePosition();
            var color = ScreenGrabber.GetColorAt(mousePos);

            // 更新放大镜颜色
            PreviewColorBox.Background = new SolidColorBrush(color);

            // 让放大镜跟随鼠标 (由于我们在全屏窗口内，WPF坐标约等于屏幕坐标)
            // *注意：这里未处理 DPI 换算，如果预览偏离鼠标，需要除以 VisualTreeHelper.GetDpi(this).PixelsPerDip

            // 简单的跟随逻辑
            Point relativePoint = Mouse.GetPosition(this);

            double magLeft = relativePoint.X + 20;
            double magTop = relativePoint.Y + 20;

            // 防止放大镜跑出屏幕右下角
            if (magLeft + MagnifierGrid.Width > ActualWidth) magLeft = relativePoint.X - MagnifierGrid.Width - 20;
            if (magTop + MagnifierGrid.Height > ActualHeight) magTop = relativePoint.Y - MagnifierGrid.Height - 20;

            Canvas.SetLeft(MagnifierGrid, magLeft);
            Canvas.SetTop(MagnifierGrid, magTop);
        }

        /// <summary>
        /// 点击确认选色
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
            ConfirmPick();
            e.Handled = true;
        }

        /// <summary>
        /// 右键或ESC取消
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            Close();
        } 
        #endregion
    }
}
