
using System.Diagnostics;

namespace cbhk_environment.More
{
    /// <summary>
    /// FeedBackBugs.xaml 的交互逻辑
    /// </summary>
    public partial class FeedBackBugs
    {
        public FeedBackBugs()
        {
            InitializeComponent();
        }

        private void FeedBackBlock_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "https://github.com/MCcber/Minecraft/issues",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
    }
}
