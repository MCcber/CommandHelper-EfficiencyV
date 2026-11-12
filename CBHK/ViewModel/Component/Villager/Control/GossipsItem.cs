using CBHK.CustomControl.VectorComboBox;
using CBHK.ViewModel.Component.Villager.Event;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Events;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace CBHK.ViewModel.Component.Villager.Control
{
    public partial class GossipsItemViewModel : ObservableObject
    {
        #region Field
        private IEventAggregator eventAggregator;
        #endregion

        #region Property
        [ObservableProperty]
        private Visibility visibility = Visibility.Visible;

        [ObservableProperty]
        private string selectedTypeItemPath = string.Empty;

        [ObservableProperty]
        public decimal gossipValue;

        [ObservableProperty]
        public VectorTextComboBoxItem selectedTypeItem;

        [ObservableProperty]
        public string targetText = string.Empty;

        [ObservableProperty]
        private Brush background = Brushes.Transparent;

        [ObservableProperty]
        private ObservableCollection<VectorTextComboBoxItem> gossipTypeList = [];
        #endregion

        #region Method
        public GossipsItemViewModel(IEventAggregator EventAggregator)
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#313233"));
            eventAggregator = EventAggregator;
        }

        /// <summary>
        /// 返回该言论的数据
        /// </summary>
        /// <returns></returns>
        public string GetGossipData()
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
        #endregion

        #region Event

        [RelayCommand]
        public void Delete()
        {
            eventAggregator.GetEvent<RemoveGossipItemEvent>().Publish(this);
        }

        /// <summary>
        /// 计算言论影响的价格
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [RelayCommand]
        public void Apply()
        {
            eventAggregator.GetEvent<UpdateGossipEffectEvent>().Publish(this);
        }
        #endregion
    }
}