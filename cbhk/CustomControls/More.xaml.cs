using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media.Imaging;

namespace cbhk.CustomControls
{
    /// <summary>
    /// More.xaml 的交互逻辑
    /// </summary>
    public partial class More : UserControl
    {
        private List<string> LinkTargetList = ["https://space.bilibili.com/333868639", "https://space.bilibili.com/170651403/?spm_id_from=333.999.0.0", "https://space.bilibili.com/521673619/?spm_id_from=333.999.0.0", "https://space.bilibili.com/67131398/?spm_id_from=333.999.0.0", "https://space.bilibili.com/350848639/?spm_id_from=333.999.0.0", "https://space.bilibili.com/15174905/?spm_id_from=333.999.0.0", "https://space.bilibili.com/57030021/?spm_id_from=333.999.0.0", "https://space.bilibili.com/590541175?spm_id_from=333.337.0.0", "https://space.bilibili.com/413164365/?spm_id_from=333.999.0.0"];
        public RelayCommand<IconTextButtons> LinkCommand { get; set; }
        int ColumnCount = 3;

        public More()
        {
            InitializeComponent();
            DataContext = this;
            LinkCommand = new RelayCommand<IconTextButtons>(GoToWebSite);
        }

        private void GoToWebSite(IconTextButtons btn)
        {
            DockPanel dockPanel = btn.Parent as DockPanel;
            StackPanel stackPanel = dockPanel.Parent as StackPanel;
            int parentIndex = stackPanel.Children.IndexOf(dockPanel) - 1;
            int LinkIndex = parentIndex * ColumnCount + dockPanel.Children.IndexOf(btn);
            var psi = new ProcessStartInfo
            {
                FileName = LinkTargetList[LinkIndex],
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        private void ClickToJoinGroup(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "https://jq.qq.com/?_wv=1027&k=72W2xZYe",
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        private void FeedBackBlock_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "https://github.com/MCcber/CommandHelper-EfficiencyV/issues",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
    }

    public class DonateDataItem
    {
        public BitmapImage Icon { get; set; }
        public string Description { get; set; }
        public BitmapImage Donate { get; set; } = null;
        public Visibility NoRequired { get; set; }
    }
}
