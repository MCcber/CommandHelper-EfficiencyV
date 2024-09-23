using cbhk.CustomControls;
using cbhk.CustomControls.Interfaces;
using cbhk.GeneralTools;
using cbhk.ViewModel.Generators;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace cbhk.Generators.ItemGenerator.Components
{
    /// <summary>
    /// EnchantmentItems.xaml 的交互逻辑
    /// </summary>
    public partial class EnchantmentItems : UserControl, IVersionUpgrader
    {
        ItemPageViewModel itemPageDataContext = null;
        ItemViewModel itemDataContext = null;

        #region 合并结果
        string id = "";
        int currentVersion = 1205;
        async Task<string> IVersionUpgrader.Result()
        {
            await Task.Delay(0);
            if (id.Length > 0)
            {
                string result = "{id:" + id + ",lvl:" + Level.Value + "s}";
                return result;
            }
            else
                return "";
        }
        #endregion

        public EnchantmentItems()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 载入附魔列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnchantmentIdLoaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBoxs = sender as ComboBox;
            itemDataContext = Window.GetWindow(comboBoxs).DataContext as ItemViewModel;
            itemPageDataContext = comboBoxs.FindParent<ItemPagesView>().DataContext as ItemPageViewModel;
            comboBoxs.ItemsSource = itemPageDataContext.EnchantmentIDList;
        }

        /// <summary>
        /// 删除该控件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            ItemsControl parent = this.FindParent<ItemsControl>();
            //删除自己
            ObservableCollection<EnchantmentItems> items = parent.ItemsSource as ObservableCollection<EnchantmentItems>;
            if(items is not null)
            items.Remove(this);
            else
            {
                StackPanel parentPanel = Parent as StackPanel;
                parentPanel.Children.Remove(this);
            }
            (sender as Button).Focus();
        }

        private void EnchantmentID_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateEnchantmentID();

        /// <summary>
        /// 用于根据版本更新ID
        /// </summary>
        /// <param name="version"></param>
        public async Task Upgrade(int version)
        {
            await Task.Delay(0);
            currentVersion = version;
            UpdateEnchantmentID();
        }

        private void UpdateEnchantmentID()
        {
            id = "";
            if (currentVersion < 1130)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    object numberResult = itemDataContext.EnchantmentTable.Select("name='" + (ID.SelectedItem as TextComboBoxItem).Text + "'").First()?["number"];
                    if (numberResult is not null)
                        id = numberResult.ToString();
                });
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (ID.SelectedItem is not null)
                    {
                        DataRow[] dataRows = itemDataContext.EnchantmentTable.Select("name='" + (ID.SelectedItem as TextComboBoxItem).Text + "'");
                        if (dataRows.Length > 0)
                            id = "\"minecraft:" + dataRows.First()?["id"].ToString() + "\"";
                    }
                });
            }
        }
    }
}