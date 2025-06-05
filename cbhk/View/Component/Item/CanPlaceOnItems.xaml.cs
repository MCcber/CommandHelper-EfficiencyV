using CBHK.CustomControl;
using CBHK.GeneralTool;
using CBHK.ViewModel.Component.Item;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace CBHK.View.Component.Item
{
    /// <summary>
    /// CanPlaceOnItems.xaml 的交互逻辑
    /// </summary>
    public partial class CanPlaceOnItems : UserControl
    {
        ItemPageViewModel itemPageDataContext = null;
        public string Result
        {
            get
            {
                string result;
                string currentBlockID = (ID.SelectedItem as IconComboBoxItem).ComboBoxItemId.Replace("minecraft:", "");
                string id = currentBlockID;
                result = "\"" + id + "\"";
                for (int i = 0; i < itemPageDataContext.VersionIDList.Count; i++)
                {
                    if (itemPageDataContext.VersionIDList[i].HighVersionID == currentBlockID && itemPageDataContext.CurrentMinVersion < 1130)
                    {
                        id = itemPageDataContext.VersionIDList[i].LowVersionID;
                        result = "\"" + id + "\"";
                        break;
                    }
                }
                return result;
            }
        }

        public CanPlaceOnItems()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 删除该控件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            ItemsControl parent = this.FindParent<ItemsControl>();
            ObservableCollection<CanPlaceOnItems> canDestroyItems = parent.ItemsSource as ObservableCollection<CanPlaceOnItems>;
            //删除自己
            canDestroyItems.Remove(this);
            parent.FindParent<Accordion>().FindChild<IconButtons>().Focus();
        }

        /// <summary>
        /// 加载所有子级成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanPlaceOnItemLoaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            itemPageDataContext = comboBox.FindParent<ItemPageView>().DataContext as ItemPageViewModel;
            comboBox.ItemsSource = itemPageDataContext.BlockIdList;
        }
    }
}