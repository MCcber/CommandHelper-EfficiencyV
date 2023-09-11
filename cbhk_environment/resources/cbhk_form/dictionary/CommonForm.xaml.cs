using System.Windows;
using System.Windows.Input;

namespace cbhk_environment.resources.cbhk_form.dictionary
{
    public partial class CommonForm
    {
        // 拖动
        private void CustomWindow_MouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            Window win = (Window)((FrameworkElement)sender).TemplatedParent;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                win.DragMove();
            }
        }

        // 关闭
        private void CustomWindowBtnClose_Click(object sender, RoutedEventArgs e)
        {
            Window win = (Window)((FrameworkElement)sender).TemplatedParent;
            win.Close();
        }

        // 最小化
        private void CustomWindowBtnMinimized_Click(object sender, RoutedEventArgs e)
        {
            Window win = (Window)((FrameworkElement)sender).TemplatedParent;
            win.WindowState = WindowState.Minimized;
        }

        // 最大化、还原
        private void CustomWindowBtnMaxNormal_Click(object sender, RoutedEventArgs e)
        {
            Window win = (Window)((FrameworkElement)sender).TemplatedParent;
            if (win.WindowState == WindowState.Maximized)
            {
                win.WindowState = WindowState.Normal;
            }
            else
            {
                // 不覆盖任务栏
                win.MaxWidth = SystemParameters.WorkArea.Width;
                win.MaxHeight = SystemParameters.WorkArea.Height;
                win.WindowState = WindowState.Maximized;
            }
        }
    }
}
