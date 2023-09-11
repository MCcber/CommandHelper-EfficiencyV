using cbhk_environment.CustomControls;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;

namespace cbhk_environment.More
{
    /// <summary>
    /// AboutUs.xaml 的交互逻辑
    /// </summary>
    public partial class AboutUs
    {
        private List<string> LinkTargetList = new() { "https://space.bilibili.com/333868639", "https://space.bilibili.com/170651403/?spm_id_from=333.999.0.0", "https://space.bilibili.com/521673619/?spm_id_from=333.999.0.0", "https://space.bilibili.com/67131398/?spm_id_from=333.999.0.0", "https://space.bilibili.com/350848639/?spm_id_from=333.999.0.0", "https://space.bilibili.com/15174905/?spm_id_from=333.999.0.0", "https://space.bilibili.com/57030021/?spm_id_from=333.999.0.0", "https://space.bilibili.com/590541175?spm_id_from=333.337.0.0", "https://space.bilibili.com/413164365/?spm_id_from=333.999.0.0" };
        public RelayCommand<IconTextButtons> LinkCommand { get; set; }
        int ColumnCount = 3;
        public AboutUs()
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
    }
}
