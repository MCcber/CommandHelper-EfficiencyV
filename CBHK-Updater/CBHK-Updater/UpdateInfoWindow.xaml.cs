using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CBHK_Updater
{
    /// <summary>
    /// UpdateInfoWindow.xaml 的交互逻辑
    /// </summary>
    public partial class UpdateInfoWindow : Window
    {
        public UpdateInfoWindow()
        {
            InitializeComponent();
            InfoTextBox.Text = "※ 最新版本：" + MainWindow.updateInfo.tag_name + "\r\n※ 更新时间：" + MainWindow.updateInfo.created_at + "\r\n※ 内容：\r\n" + MainWindow.updateInfo.body;

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
