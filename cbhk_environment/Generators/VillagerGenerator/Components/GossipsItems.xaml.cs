using cbhk_environment.CustomControls;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace cbhk_environment.Generators.VillagerGenerator.Components
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
                string ValueData = Value.ToString().Trim() != "" ? "Value:" + (Value.ToString().Contains(".") ? Value.ToString().Split('.')[0] :Value.ToString()) +",":"";
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
            villager_datacontext context = Window.GetWindow(textComboBoxs).DataContext as villager_datacontext;
            textComboBoxs.ItemsSource = context.GossipTypes;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            villager_datacontext context = (Window.GetWindow(sender as IconTextButtons) as Villager).DataContext as villager_datacontext;
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
            villager_datacontext context = Window.GetWindow(currentButton).DataContext as villager_datacontext;
            //创建字典，让字典的键来自动匹配对应的值，自动执行搜索和带入公式两个行为
            Dictionary<string, int> currentGossipTypes = new();
            //已处理的标记
            Dictionary<string, bool> handedMarkers = new();
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
