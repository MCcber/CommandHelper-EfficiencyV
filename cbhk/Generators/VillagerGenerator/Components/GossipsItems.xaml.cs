﻿using cbhk.CustomControls;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace cbhk.Generators.VillagerGenerator.Components
{
    /// <summary>
    /// GossipsItems.xaml 的交互逻辑
    /// </summary>
    public partial class GossipsItems : UserControl
    {

        #region 返回该言论的数据
        public string GossipData
        {
            get
            {
                string result;
                string item_data = Type.SelectedItem.ToString();
                string TypeData = item_data.Trim() != ""?"Type:"+ item_data+",":"";
                string gossipValueString = Value.Value.ToString().Trim();
                string ValueData = gossipValueString != "" ? "Value:" + (gossipValueString.Contains('.') ? gossipValueString.Split('.')[0] : gossipValueString) +",":"";
                string TargetData = Target.Text.Trim() != "" ?"Target:\""+Target.Text+"\",":"";
                result = TypeData != "" || ValueData != "" || TargetData != "" ?TypeData + ValueData + TargetData:"";
                result = "{" + result.TrimEnd(',') + "},";
                return result;
            }
        }
        #endregion

        public GossipsItems()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 载入言论类型
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TypeLoaded(object sender, RoutedEventArgs e)
        {
            TextComboBoxs textComboBoxs = sender as TextComboBoxs;
            VillagerDataContext context = Window.GetWindow(textComboBoxs).DataContext as VillagerDataContext;
            textComboBoxs.ItemsSource = context.GossipTypes;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            VillagerDataContext context = (Window.GetWindow(sender as IconTextButtons) as Villager).DataContext as VillagerDataContext;
            context.gossipItems.Remove(this);
            context.compositionIndex--;
        }

        /// <summary>
        /// 计算言论影响的价格
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            Button currentButton = sender as Button;
            VillagerDataContext context = Window.GetWindow(currentButton).DataContext as VillagerDataContext;
            //创建字典，让字典的键来自动匹配对应的值，自动执行搜索和带入公式两个行为
            Dictionary<string, int> currentGossipTypes = [];
            //已处理的标记
            Dictionary<string, bool> handedMarkers = [];
            foreach (var item in context.GossipTypes)
            {
                currentGossipTypes.Add(item, 0);
                handedMarkers.Add(item, false);
            }
            string currentType = Type.SelectedItem.ToString();
            string currentUID = Target.Text;
            currentGossipTypes[currentType] = int.Parse(Value.Value.ToString());
            handedMarkers[currentType] = true;

            _ = context.gossipItems.Where(item =>
            {
                string currentGossipType = item.Type.SelectedItem.ToString();
                if (currentGossipTypes.ContainsKey(currentGossipType) && item.Target.Text == currentUID && !handedMarkers[currentGossipType])
                {
                    currentGossipTypes[currentGossipType] = int.Parse(item.Value.Value.ToString());
                    handedMarkers[currentGossipType] = true;
                }
                return true;
            });

            if(currentGossipTypes.Count == 5)
            _ = context.transactionItems.All(item => { item.UpdateDiscountData(currentGossipTypes["minor_negative"], currentGossipTypes["minor_positive"], currentGossipTypes["major_negative"], currentGossipTypes["major_positive"], currentGossipTypes["trading"]); return true; });
        }
    }
}
