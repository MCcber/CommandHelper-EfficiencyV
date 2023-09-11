using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace cbhk_environment.resources.MainFormDataContext
{
    public class NotifyIconViewModel: ObservableObject
    {
        /// <summary>
        /// 打开管家主页
        /// </summary>
        public RelayCommand ShowWindowCommand { get; set; }

        /// <summary>
        /// 退出管家
        /// </summary>
        public RelayCommand ExitApplicationCommand { get; set; }

        /// <summary>
        /// 管家引用
        /// </summary>
        public MainWindow win;
        public NotifyIconViewModel(MainWindow window)
        {
            win = window;
            ShowWindowCommand = new RelayCommand(show_window_command);
            ExitApplicationCommand = new RelayCommand(exit_application_command);
        }

        private void show_window_command()
        {
            win.ShowInTaskbar = true;
            win.Opacity = 1.0;
            win.WindowState = WindowState.Normal;
            win.Topmost = true;
            win.Show();
            win.Focus();
            win.Topmost = false;
        }

        private void exit_application_command()
        {
            win.taskbarButton.Visibility = Visibility.Collapsed;
            System.Environment.Exit(0);
        }
    }
}
