using CBHK.CustomControls;
using CBHK.CustomControls.Interfaces;
using CBHK.GeneralTools;
using CBHK.ViewModel.Generators;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CBHK.Generators.ItemGenerator.Components
{
    /// <summary>
    /// AttributeItems.xaml 的交互逻辑
    /// </summary>
    public partial class AttributeItems : UserControl,IVersionUpgrader
    {
        #region 合并数据
        int currentVersion = 1205;
        async Task<string> IVersionUpgrader.Result()
        {
            await Upgrade(currentVersion);
            string attributeIDString = itemDataContext.AttributeTable.Select("name='" + (AttributeNameBox.SelectedItem as TextComboBoxItem).Text + "'").First()["id"].ToString();
            string attributeSlotString = itemDataContext.AttributeSlotTable.Select("value='" + (Slot.SelectedItem as TextComboBoxItem).Text + "'").First()["id"].ToString();
            string slotData = attributeSlotString == "all" ? "" : ",Slot:\"" + attributeSlotString + "\"";
            string result = "{AttributeName:\"" + attributeIDString + "\",Name:\"" + NameBox.Text + "\",Amount:" + Amount.Value.ToString() + "d,Operation:" + Operations.SelectedIndex + UUIDString + slotData + "}";
            return result;
        }
        #endregion

        private string UUIDString = "";
        ItemPageViewModel itemPageDataContext = null;
        ItemViewModel itemDataContext = null;

        public AttributeItems()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 载入属性控件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AttributeItems_Loaded(object sender, RoutedEventArgs e)
        {
            itemPageDataContext = (sender as UserControl).FindParent<ItemPagesView>().DataContext as ItemPageViewModel;
            itemDataContext = Window.GetWindow(sender as UserControl).DataContext as ItemViewModel;
        }

        /// <summary>
        /// 载入属性ID列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AttributeIdsLoaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            comboBox.ItemsSource = itemPageDataContext.AttributeIDList;
        }

        /// <summary>
        /// 载入属性生效槽位ID列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AttributeSlotsLoaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            comboBox.ItemsSource = itemPageDataContext.AttributeSlotList;
        }

        /// <summary>
        /// 载入属性值类型ID列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AttributeValueTypesLoaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            comboBox.ItemsSource = itemPageDataContext.AttributeValueTypeList;
        }

        /// <summary>
        /// 删除当前属性成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<AttributeItems> attributeItems = this.FindParent<Data>().AttributeSource;
            attributeItems.Remove(this);
        }

        public async Task Upgrade(int version)
        {
            currentVersion = version;
            await Task.Delay(0);
            Random random = new();
            if (version < 116)
                UUIDString = ",UUIDLeast:" + random.NextInt64() + ",UUIDMost:" + random.NextInt64();
            else
            {
                UUIDString = ",UUID:[I;" + random.Next(1000, 10000).ToString() + "," + random.Next(1000, 10000).ToString() + "," + random.Next(1000, 10000).ToString() + "," + random.Next(1000, 10000).ToString() + "]";
            }
        }
    }
}