using cbhk_environment.CustomControls;
using System.Windows;
using System.Windows.Controls;

namespace cbhk_environment.Generators.EntityGenerator.Components
{
    /// <summary>
    /// LeashData.xaml 的交互逻辑
    /// </summary>
    public partial class LeashData : UserControl
    {
        public LeashData()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 是否启用牵引者
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void BeingLed_Click(object sender, RoutedEventArgs e)
        {
            TextCheckBoxs textCheckBoxs = sender as TextCheckBoxs;
            TiedByFence.IsChecked = !textCheckBoxs.IsChecked.Value;
            tractorDisplayText.Visibility = tractor.Visibility = textCheckBoxs.IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;
            fenceDisplayText.Visibility = fence.Visibility = textCheckBoxs.IsChecked.Value ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// 是否启用栅栏
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TiedToAFence_Click(object sender, RoutedEventArgs e)
        {
            TextCheckBoxs textCheckBoxs = sender as TextCheckBoxs;
            TiedByEntity.IsChecked = !textCheckBoxs.IsChecked.Value;
            fenceDisplayText.Visibility = fence.Visibility = textCheckBoxs.IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;
            tractorDisplayText.Visibility = tractor.Visibility = textCheckBoxs.IsChecked.Value ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
