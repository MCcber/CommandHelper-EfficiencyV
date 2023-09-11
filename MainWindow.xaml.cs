using System.Windows;
using System.Windows.Input;

namespace cbhk_signin
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow:Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //隐藏托盘
            //Hardcodet.Wpf.TaskbarNotification.TaskbarIcon taskbar_icon = FindResource("cbhk_taskbar") as Hardcodet.Wpf.TaskbarNotification.TaskbarIcon;
            //taskbar_icon.Visibility = Visibility.Collapsed;
        }

        #region 窗体行为

        /// <summary>
        /// 鼠标拖拽窗体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Point title_range = e.GetPosition(TitleStack);
            if (title_range.X >= 0 && title_range.X < TitleStack.ActualWidth && title_range.Y >= 0 && title_range.Y < TitleStack.ActualHeight && e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        public void MinimizeWindow_Exec(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        public void CloseWindow_Exec(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        ///// <summary>
        ///// 窗体尺寸更新
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void Window_StateChanged(object sender, EventArgs e)
        //{

        //    switch (WindowState)
        //    {
        //        case WindowState.Maximized:
        //            //MaxWidth = SystemParameters.WorkArea.Width + 16;
        //            //MaxHeight = SystemParameters.WorkArea.Height + 16;
        //            //BorderThickness = new Thickness(5); //最大化后需要调整
        //            Left = Top = 0;
        //            MaxHeight = SystemParameters.WorkArea.Height;
        //            MaxWidth = SystemParameters.WorkArea.Width;
        //            break;
        //        case WindowState.Normal:
        //            BorderThickness = new Thickness(0);
        //            break;
        //    }
        //}
        #endregion
    }
}
