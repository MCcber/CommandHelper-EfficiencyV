using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace cbhk_environment.Generators.EntityGenerator.Components
{
    /// <summary>
    /// UUIDOrPosGroup.xaml 的交互逻辑
    /// </summary>
    public partial class UUIDOrPosGroup : UserControl
    {
        private bool isUUID = false;
        public bool IsUUID
        {
            get
            {
                return isUUID;
            }
            set
            {
                isUUID = value;
                number3.Visibility = isUUID?Visibility.Visible:Visibility.Collapsed;
            }
        }
        public UUIDOrPosGroup()
        {
            InitializeComponent();
            number0.Minimum = number1.Minimum = number2.Minimum = number3.Minimum = int.MinValue;
            number0.Maximum = number1.Maximum = number2.Maximum = number3.Maximum = int.MaxValue;
        }

        /// <summary>
        /// 生成坐标或UUID
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void generator_Click(object sender, RoutedEventArgs e)
        {
            if (EnableButton.IsChecked.Value)
            {
                List<int> ints = new();
                Random random = new();
                ints.Add(random.Next(int.MinValue, int.MaxValue));
                ints.Add(random.Next(int.MinValue, int.MaxValue));
                ints.Add(random.Next(int.MinValue, int.MaxValue));
                ints.Add(random.Next(int.MinValue, int.MaxValue));
                ints.Sort((x, y) => -x.CompareTo(y));
                number0.Value = ints[0];
                number1.Value = ints[1];
                number2.Value = ints[2];
                number3.Value = ints[3];
            }
        }
    }
}
