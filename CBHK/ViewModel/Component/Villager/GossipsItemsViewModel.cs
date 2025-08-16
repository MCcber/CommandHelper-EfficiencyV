using CBHK.CustomControl;
using CBHK.View.Component.Villager;
using CBHK.View.Generator;
using CBHK.ViewModel.Generator;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CBHK.ViewModel.Component.Villager
{
    public partial class GossipsItemsViewModel:ObservableObject
    {
        #region Property
        [ObservableProperty]
        public string _selectedTypeItemPath = "";

        [ObservableProperty]
        public double _gossipValue = 0;

        [ObservableProperty]
        public TextComboBoxItem _selectedTypeItem = null;

        [ObservableProperty]
        public string _targetText = "";

        [ObservableProperty]
        public ObservableCollection<TextComboBoxItem> _gossipTypeList = [];

        /// <summary>
        /// 返回该言论的数据
        /// </summary>
        public string GossipData
        {
            get
            {
                string result;
                string itemData = SelectedTypeItem.Text;
                string TypeData = itemData.Trim() != "" ? "Type:\"" + itemData + "\"," : "";
                string gossipValueString = GossipValue.ToString().Trim();
                string ValueData = gossipValueString != "" ? "Value:" + (gossipValueString.Contains('.') ? gossipValueString.Split('.')[0] : gossipValueString) + "," : "";
                string TargetData = TargetText.Trim() != "" ? "Target:" + TargetText + "," : "";
                result = TypeData != "" || ValueData != "" || TargetData != "" ? TypeData + ValueData + TargetData : "";
                result = "{" + result.TrimEnd(',') + "}";
                return result;
            }
        }

        #endregion

        /// <summary>
        /// 载入言论类型
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void GossipItem_Loaded(object sender, RoutedEventArgs e)
        {
            VillagerViewModel context = Window.GetWindow(sender as UserControl).DataContext as VillagerViewModel;
            GossipTypeList = context.GossipTypes;
        }

        [RelayCommand]
        public void Delete(GossipsItemsView view)
        {
            GossipsItemsViewModel context = view.DataContext as GossipsItemsViewModel;
            VillagerViewModel villagerViewModel = Window.GetWindow(view).DataContext as VillagerViewModel;
            villagerViewModel.gossipItems.Remove(view);
        }

        /// <summary>
        /// 计算言论影响的价格
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [RelayCommand]
        public void Edit(VillagerView view)
        {
            VillagerViewModel context = view.DataContext as VillagerViewModel;
            //创建字典，让字典的键来自动匹配对应的值，自动执行搜索和带入公式两个行为
            Dictionary<string, int> currentGossipTypes = [];
            //已处理的标记
            Dictionary<string, bool> handedMarkers = [];
            foreach (var item in context.GossipTypes)
            {
                currentGossipTypes.Add(item.Text, 0);
                handedMarkers.Add(item.Text, false);
            }
            string currentType = SelectedTypeItem.Text;
            string currentUID = TargetText;
            currentGossipTypes[currentType] = int.Parse(GossipValue.ToString());
            handedMarkers[currentType] = true;

            _ = context.gossipItems.Where(item =>
            {
                GossipsItemsViewModel gossipsItemsViewModel = item.DataContext as GossipsItemsViewModel;
                string currentGossipType = gossipsItemsViewModel.SelectedTypeItem.Text;
                if (currentGossipTypes.ContainsKey(currentGossipType) && gossipsItemsViewModel.TargetText == currentUID && !handedMarkers[currentGossipType])
                {
                    currentGossipTypes[currentGossipType] = int.Parse(GossipValue.ToString());
                    handedMarkers[currentGossipType] = true;
                }
                return true;
            });

            if (currentGossipTypes.Count == 5)
                _ = context.TransactionItemList.All(item => { (item.DataContext as TransactionItemViewModel).UpdateDiscountData(currentGossipTypes["minor_negative"], currentGossipTypes["minor_positive"], currentGossipTypes["major_negative"], currentGossipTypes["major_positive"], currentGossipTypes["trading"]); return true; });
        }
    }
}
