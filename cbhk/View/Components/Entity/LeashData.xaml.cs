using CBHK.CustomControls;
using CBHK.CustomControls.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CBHK.Generators.EntityGenerator.Components
{
    /// <summary>
    /// LeashData.xaml 的交互逻辑
    /// </summary>
    public partial class LeashData : UserControl,IVersionUpgrader
    {
        private int currentVersion = 1202;
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
        }

        public async Task<string> Result()
        {
            await Task.Delay(0);
            string result = "";
            Random random = new();
            if (Tied.IsChecked.Value)
            {
                string UUIDString;
                if (TiedByEntity.IsChecked.Value)
                {
                    if (currentVersion < 116)
                        UUIDString = "UUIDLeast:" + random.NextInt64() + ",UUIDMost:" + random.NextInt64();
                    else
                        UUIDString = "UUID:[I;" + random.Next() + "," + random.Next() + "," + random.Next() + "," + random.Next() + "]";
                    result = "Leash:{" + UUIDString + "}";
                }
                else
                    result = "Leash:{X:" + fence.number0.Value + ",Y:" + fence.number1.Value + ",Z:" + fence.number2.Value + "}";
            }
            return result;
        }

        public async Task Upgrade(int version)
        {
            await Task.Delay(0);
            currentVersion = version;
        }
    }
}