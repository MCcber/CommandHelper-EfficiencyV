using cbhk.CustomControls.Interfaces;
using cbhk.GeneralTools;
using System;
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
        DataTable EnchantmentTable = null;
        ItemPageDataContext dataContext = null;

        #region 合并结果
        string id = "";
        int currentVersion = 1202;
        async Task<string> IVersionUpgrader.Result()
        {
            await Upgrade(currentVersion);
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
        private async void EnchantmentIdLoaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBoxs = sender as ComboBox;
            if (comboBoxs.ItemsSource != null) return;
            dataContext = this.FindParent<ItemPages>().DataContext as ItemPageDataContext;
            ItemDataContext context = Window.GetWindow(this).DataContext as ItemDataContext;
            EnchantmentTable = context.EnchantmentTable;
            ObservableCollection<string> source = [];
            string currentPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";

            await Task.Run(() =>
            {
                foreach (DataRow row in EnchantmentTable.Rows)
                {
                    string id = row["id"].ToString();
                    string name = row["name"].ToString();
                    string imagePath = id + ".png";
                    source.Add(name);
                }
            });
            comboBoxs.ItemsSource = source;
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

        private async void EnchantmentID_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            id = "";
            ItemPageDataContext itemPageDataContext = this.FindParent<ItemPages>().DataContext as ItemPageDataContext;
            await Upgrade(itemPageDataContext.CurrentMinVersion);
        }

        /// <summary>
        /// 用于根据版本更新ID
        /// </summary>
        /// <param name="version"></param>
        public async Task Upgrade(int version)
        {
            currentVersion = version;
            await Task.Run(() =>
            {
                id = "";
                if (version < 113)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        object numberResult = EnchantmentTable.Select("name='" + ID.SelectedValue.ToString() + "'").First()?["number"];
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
                            id = "\"minecraft:" + EnchantmentTable.Select("name='" + ID.SelectedValue.ToString() + "'").First()?["id"].ToString() + "\"";
                        }
                    });
                }
            });
        }
    }
}