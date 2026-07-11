using CBHK.CustomControl.Container;
using CBHK.CustomControl.VectorComboBox;
using CBHK.View.Generator;
using CBHK.ViewModel.Generator;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.Input
{
    public partial class GossipsItem : Control
    {
        #region Property
        public string SelectedTypeItemPath
        {
            get { return (string)GetValue(SelectedTypeItemPathProperty); }
            set { SetValue(SelectedTypeItemPathProperty, value); }
        }

        public static readonly DependencyProperty SelectedTypeItemPathProperty =
            DependencyProperty.Register(nameof(SelectedTypeItemPath), typeof(string), typeof(GossipsItem), new PropertyMetadata(default(string)));

        public double GossipValue
        {
            get { return (double)GetValue(GossipValueProperty); }
            set { SetValue(GossipValueProperty, value); }
        }

        public static readonly DependencyProperty GossipValueProperty =
            DependencyProperty.Register(nameof(GossipValue), typeof(double), typeof(GossipsItem), new PropertyMetadata(0));

        public VectorTextComboBoxItem SelectedTypeItem
        {
            get { return (VectorTextComboBoxItem)GetValue(SelectedTypeItemProperty); }
            set { SetValue(SelectedTypeItemProperty, value); }
        }

        public static readonly DependencyProperty SelectedTypeItemProperty =
            DependencyProperty.Register(nameof(SelectedTypeItem), typeof(VectorTextComboBoxItem), typeof(GossipsItem), new PropertyMetadata(default(VectorTextComboBoxItem)));

        public string TargetText
        {
            get { return (string)GetValue(TargetTextProperty); }
            set { SetValue(TargetTextProperty, value); }
        }

        public static readonly DependencyProperty TargetTextProperty =
            DependencyProperty.Register(nameof(TargetText), typeof(string), typeof(GossipsItem), new PropertyMetadata(default(string)));

        public ObservableCollection<VectorTextComboBoxItem> GossipTypeList
        {
            get { return (ObservableCollection<VectorTextComboBoxItem>)GetValue(GossipTypeListProperty); }
            set { SetValue(GossipTypeListProperty, value); }
        }

        public static readonly DependencyProperty GossipTypeListProperty =
            DependencyProperty.Register(nameof(GossipTypeList), typeof(ObservableCollection<VectorTextComboBoxItem>), typeof(GossipsItem), new PropertyMetadata(new ObservableCollection<VectorTextComboBoxItem>()));

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
                string ValueData = gossipValueString != "" ? "LeftValue:" + (gossipValueString.Contains('.') ? gossipValueString.Split('.')[0] : gossipValueString) + "," : "";
                string TargetData = TargetText.Trim() != "" ? "Target:" + TargetText + "," : "";
                result = TypeData != "" || ValueData != "" || TargetData != "" ? TypeData + ValueData + TargetData : "";
                result = "{" + result.TrimEnd(',') + "}";
                return result;
            }
        }

        #endregion

        #region Method
        public GossipsItem()
        {
            Loaded += GossipItem_Loaded;
        }
        #endregion

        #region Event
        /// <summary>
        /// 载入言论类型
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void GossipItem_Loaded(object sender, RoutedEventArgs e)
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#313233"));
            VillagerViewModel context = Window.GetWindow(sender as UserControl).DataContext as VillagerViewModel;
            GossipTypeList = context.GossipTypeList;
        }

        [RelayCommand]
        public void Delete(GossipsItem view)
        {
            GossipsItem context = view.DataContext as GossipsItem;
            VillagerViewModel villagerViewModel = Window.GetWindow(view).DataContext as VillagerViewModel;
            villagerViewModel.GossipItemList.Remove(view);
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
            foreach (var item in context.GossipTypeList)
            {
                currentGossipTypes.Add(item.Text, 0);
                handedMarkers.Add(item.Text, false);
            }
            string currentType = SelectedTypeItem.Text;
            string currentUID = TargetText;
            currentGossipTypes[currentType] = int.Parse(GossipValue.ToString());
            handedMarkers[currentType] = true;

            _ = context.GossipItemList.Where(item =>
            {
                GossipsItem gossipsItem = item.DataContext as GossipsItem;
                string currentGossipType = gossipsItem.SelectedTypeItem.Text;
                if (currentGossipTypes.ContainsKey(currentGossipType) && gossipsItem.TargetText == currentUID && !handedMarkers[currentGossipType])
                {
                    currentGossipTypes[currentGossipType] = int.Parse((string)this.GossipValue.ToString());
                    handedMarkers[currentGossipType] = true;
                }
                return true;
            });

            if (currentGossipTypes.Count == 5)
                _ = context.TransactionItemList.All(item => { (item.DataContext as TransactionItem).UpdateDiscountData(currentGossipTypes["minor_negative"], currentGossipTypes["minor_positive"], currentGossipTypes["major_negative"], currentGossipTypes["major_positive"], currentGossipTypes["trading"]); return true; });
        }
        #endregion
    }
}
