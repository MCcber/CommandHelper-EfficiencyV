using CBHK.CustomControl;
using CBHK.CustomControl.Interfaces;
using CBHK.Domain;
using CBHK.GeneralTool;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CBHK.View.Component.Item
{
    /// <summary>
    /// EnchantmentItem.xaml 的交互逻辑
    /// </summary>
    public partial class EnchantmentItem : UserControl, IVersionUpgrader
    {
        private CBHKDataContext _context = null;
        private ObservableCollection<TextComboBoxItem> EnchantmentList { get; set; } = [];

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

        public EnchantmentItem(CBHKDataContext context)
        {
            InitializeComponent();
            _context = context;
        }

        /// <summary>
        /// 载入附魔列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnchantmentIdLoaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBoxs = sender as ComboBox;
            comboBoxs.ItemsSource = EnchantmentList;
            foreach (var item in _context.EnchantmentSet)
            {
                EnchantmentList.Add(new TextComboBoxItem()
                {
                    Text = item.Name
                });
            }
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
            ObservableCollection<EnchantmentItem> items = parent.ItemsSource as ObservableCollection<EnchantmentItem>;
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
                    string name = (ID.SelectedItem as TextComboBoxItem).Text;
                    object numberResult = _context.EnchantmentSet.First(item=>item.Name == name).Number;
                    if (numberResult is not null)
                    {
                        id = numberResult.ToString();
                    }
                });
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (ID.SelectedItem is not null)
                    {
                        string name = (ID.SelectedItem as TextComboBoxItem).Text;
                        var data = _context.EnchantmentSet.First(item => item.Name == name);
                        if (data is not null)
                        {
                            id = "\"minecraft:" + data.ID + "\"";
                        }
                    }
                });
            }
        }
    }
}