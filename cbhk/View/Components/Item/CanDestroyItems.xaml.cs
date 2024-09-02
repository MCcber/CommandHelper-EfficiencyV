using cbhk.CustomControls;
using cbhk.GeneralTools;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace cbhk.Generators.ItemGenerator.Components
{
    /// <summary>
    /// CanDestroyItems.xaml 的交互逻辑
    /// </summary>
    public partial class CanDestroyItems : UserControl
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

        public CanDestroyItems()
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
            ObservableCollection<CanDestroyItems> canDestroyItems = parent.ItemsSource as ObservableCollection<CanDestroyItems>;
            //删除自己
            canDestroyItems.Remove(this);
            parent.FindParent<Accordion>().FindChild<IconButtons>().Focus();
        }

        /// <summary>
        /// 加载所有子级成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanDestroyItemLoaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            itemPageDataContext = comboBox.FindParent<ItemPagesView>().DataContext as ItemPageViewModel;
            comboBox.ItemsSource = itemPageDataContext.BlockIdList;
        }
    }
}