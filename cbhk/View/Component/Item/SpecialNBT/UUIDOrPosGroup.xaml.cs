﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace CBHK.View.Component.Item.SpecialNBT
{
    /// <summary>
    /// UUIDOrPosGroup.xaml 的交互逻辑
    /// </summary>
    public partial class UUIDOrPosGroup : UserControl
    {
        public UUIDOrPosGroup()
        {
            InitializeComponent();
            number0.Minimum = number1.Minimum = number2.Minimum = number3.Minimum = int.MinValue;
            number0.Maximum = number1.Maximum = number2.Maximum = number3.Maximum = int.MaxValue;
        }
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
                number3.Visibility = isUUID ? Visibility.Visible : Visibility.Collapsed;
            }
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
                List<int> ints = [];
                Random random = new();
                ints.Add(random.Next(int.MinValue, int.MaxValue));
                ints.Add(random.Next(int.MinValue, int.MaxValue));
                ints.Add(random.Next(int.MinValue, int.MaxValue));
                if(IsUUID)
                ints.Add(random.Next(int.MinValue, int.MaxValue));
                ints.Sort((x, y) => -x.CompareTo(y));
                number0.Value = ints[0];
                number1.Value = ints[1];
                number2.Value = ints[2];
                if(IsUUID)
                number3.Value = ints[3];
            }
        }
    }
}
