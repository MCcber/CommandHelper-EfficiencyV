using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace cbhk_environment.More
{
    /// <summary>
    /// NoticeToUsers.xaml 的交互逻辑
    /// </summary>
    public partial class NoticeToUsers:Window
    {
        private int browseTime = 5;
        DispatcherTimer timer = new()
        {
            IsEnabled = true,
            Interval = TimeSpan.FromSeconds(1)
        };
        public NoticeToUsers()
        {
            InitializeComponent();
            donotShowNextTime.IsChecked = !MainWindowProperties.ShowNotice;
            timer.Tick += Readed_Tick;
        }

        private void Readed_Tick(object sender, EventArgs e)
        {
            browseTimeBlock.Text = browseTime + "s";
            if(browseTime == 0)
            {
                timer.IsEnabled = false;
                understandButton.IsEnabled = true;
            }
            browseTime--;
        }

        private void donotShowNextTime_Click(object sender, RoutedEventArgs e)
        {
            MainWindowProperties.ShowNotice = !(sender as CheckBox).IsChecked.Value;
        }

        private void understandButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
