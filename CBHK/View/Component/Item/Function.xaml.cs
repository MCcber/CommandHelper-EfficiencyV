using CBHK.Domain;
using CBHK.Interface;
using CBHK.Utility.Common;
using CBHK.ViewModel.Component.Item;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CBHK.View.Component.Item
{
    /// <summary>
    /// Function.xaml 的交互逻辑
    /// </summary>
    public partial class Function : UserControl,IVersionUpgrader
    {
        private CBHKDataContext _context = null;
        private ItemPageViewModel _viewModel;

        #region 可破坏可放置方块等数据源
        public ObservableCollection<CanDestroyItems> CanDestroyItemsSource { get; set; } = [];
        public ObservableCollection<CanPlaceOnItems> CanPlaceOnItemsSource { get; set; } = [];
        public ObservableCollection<EnchantmentItem> EnchantmentItemsSource { get; set; } = [];
        #endregion

        #region 可破坏方块列表
        private string CanDestroyBlockResult
        {
            get
            {
                string result = CanDestroyItemsSource.Count > 0 ? "CanDestroy:[" + string.Join(',', CanDestroyItemsSource.Select(item => item.Result)) + "]," : "";
                return result;
            }
        }
        #endregion

        #region 可放置方块列表
        private string CanPlaceOnBlockResult
        {
            get
            {
                string result = CanPlaceOnItemsSource.Count > 0 ? "CanPlaceOn:[" + string.Join(",", CanPlaceOnItemsSource.Select(item => item.Result)) + "]," : "";
                return result;
            }
        }
        #endregion

        #region 附魔列表
        string enchantmentKey = "EnchantmentIDAndNameGroupByVersionMap";
        private async Task<string> GetEnchantmentResult()
        {
            string result = "";
            if (EnchantmentItemsSource.Count > 0)
            {
                StringBuilder enchantString = new();
                string enchantmentElement;
                for (int i = 0; i < EnchantmentItemsSource.Count; i++)
                {
                    enchantmentElement = await (EnchantmentItemsSource[i] as IVersionUpgrader).Result();
                    if (enchantmentElement is not null)
                        enchantString.Append(enchantmentElement + ",");
                }
                result = enchantmentKey + ":[" + enchantString.ToString().TrimEnd(',') + "],";
            }
            return result;
        }
        #endregion

        #region 材质引用
        private string Material
        {
            get
            {
                string result = material.Text.Trim().Length > 0 ? "material:\"" + material.Text + "\"," : "";
                return result;
            }
        }
        #endregion

        #region 纹饰引用
        private string Pattern
        {
            get
            {
                string result = pattern.Text.Trim().Length > 0 ? "pattern:\"" + pattern.Text + "\"" : "";
                return result;
            }
        }
        #endregion

        #region 盔甲纹饰
        private string TrimResult
        {
            get
            {
                string result = "Trim:{" + ((Material.Length > 0 ? Material : "") + (Pattern.Length > 0 ? "," + Pattern : "")).Trim(',') + "},";
                result = result != "Trim:{}," ?result:"";
                return result;
            }
        }
        #endregion

        #region 合并结果
        int currentVersion = 1205;
        async Task<string> IVersionUpgrader.Result()
        {
            await Upgrade(currentVersion);
            string EnchantmentResult = await GetEnchantmentResult();
            string result = CanDestroyBlockResult + CanPlaceOnBlockResult + EnchantmentResult + TrimResult;
            return result;
        }
        #endregion

        public Function(CBHKDataContext context,ItemPageViewModel viewModel)
        {
            InitializeComponent();
            _context = context;
            _viewModel = viewModel;

            #region 连接指令
            //添加
            CanDestroyBlock.Modify = new RelayCommand<FrameworkElement>(AddCanDestroyBlockClick);
            CanPlaceOnBlock.Modify = new RelayCommand<FrameworkElement>(AddCanPlaceOnBlockClick);
            Enchantment.Modify = new RelayCommand<FrameworkElement>(AddEnchantmentClick);
            //清空
            CanDestroyBlock.Fresh = new RelayCommand<FrameworkElement>(ClearCanDestroyBlockClick);
            CanPlaceOnBlock.Fresh = new RelayCommand<FrameworkElement>(ClearCanPlaceOnBlockClick);
            Enchantment.Fresh = new RelayCommand<FrameworkElement>(ClearEnchantmentClick);
            #endregion
            #region 初始化数据
            CanDestroyBlockPanel.ItemsSource = CanDestroyItemsSource;
            CanPlaceOnBlockPanel.ItemsSource = CanPlaceOnItemsSource;
            EnchantmentPanel.ItemsSource = EnchantmentItemsSource;
            #endregion
        }

        #region 附魔、可破坏可放置、属性等数据的编辑和清空
        /// <summary>
        /// 清空附魔数据
        /// </summary>
        /// <param name="obj"></param>
        private void ClearEnchantmentClick(FrameworkElement obj)
        {
            ItemPageViewModel itemPageDataContext = obj.FindParent<ItemPageView>().DataContext as ItemPageViewModel;
            for (int i = 0; i < EnchantmentItemsSource.Count; i++)
            {
                itemPageDataContext.VersionComponents.Remove(EnchantmentItemsSource[i]);
                EnchantmentItemsSource.RemoveAt(i);
                i--;
            }
        }

        /// <summary>
        /// 清空可放置方块数据
        /// </summary>
        /// <param name="obj"></param>
        private void ClearCanPlaceOnBlockClick(FrameworkElement obj) => CanPlaceOnItemsSource.Clear();

        /// <summary>
        /// 清空可破坏方块数据
        /// </summary>
        /// <param name="obj"></param>
        private void ClearCanDestroyBlockClick(FrameworkElement obj) => CanDestroyItemsSource.Clear();

        /// <summary>
        /// 增加附魔列表成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddEnchantmentClick(FrameworkElement obj)
        {
            EnchantmentItem enchantmentItems = new(_context,_viewModel);
            EnchantmentItemsSource.Add(enchantmentItems);
            ItemPageViewModel itemPageDataContext = obj.FindParent<ItemPageView>().DataContext as ItemPageViewModel;
            itemPageDataContext.VersionComponents.Add(enchantmentItems);
        }

        /// <summary>
        /// 增加可放置方块列表成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddCanPlaceOnBlockClick(FrameworkElement obj) => CanPlaceOnItemsSource.Add(new CanPlaceOnItems());

        /// <summary>
        /// 增加可破坏方块列表成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddCanDestroyBlockClick(FrameworkElement obj) => CanDestroyItemsSource.Add(new CanDestroyItems());
        #endregion

        /// <summary>
        /// 为盔甲纹饰引用路径
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrimData_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                ShowHiddenItems = true,
                Title = "为盔甲纹饰引用一个命名空间",
                Multiselect = false,
            };
            if (openFolderDialog.ShowDialog().Value)
            {
                Button button = sender as Button;
                int currentRowIndex = Grid.GetRow(button);
                Grid parent = button.Parent as Grid;
                foreach (FrameworkElement item in parent.Children)
                {
                    int row = Grid.GetRow(item);
                    int column = Grid.GetColumn(item);
                    if (row == currentRowIndex && column == 1)
                    {
                        TextBox textBox = item as TextBox;
                        textBox.Text = openFolderDialog.FolderName;
                    }
                }
            }
        }

        /// <summary>
        /// 获取外部数据
        /// </summary>
        /// <param name="ExternData"></param>
        public void GetExternData(ref JObject ExternData)
        {
            JToken materialObj = ExternData.SelectToken("tag.material");
            materialObj ??= ExternData.SelectToken("material");
            JToken trim = ExternData.SelectToken("tag.Trim");
            trim ??= ExternData.SelectToken("Trim");

            if (materialObj != null)
            {
                material.Text = materialObj.ToString();
                ExternData.Remove("tag.material");
                ExternData.Remove("material");
            }
            if (trim != null)
            {
                pattern.Text = trim.ToString();
                ExternData.Remove("tag.Trim");
                ExternData.Remove("Trim");
            }

            JToken CanDestory = ExternData.SelectToken("tag.CanDestroy");
            CanDestory ??= ExternData.SelectToken("CanDestroy");
            if (CanDestory is JArray canDestroyBlockArray && canDestroyBlockArray.Count > 0)
            {
                foreach (JValue canDestroy in canDestroyBlockArray.Cast<JValue>())
                {
                    CanDestroyItems canDestroyItem = new();
                    canDestroyItem.ID.SelectedValuePath = "ComboBoxItemId";
                    canDestroyItem.ID.SelectedValue = canDestroy;
                }
                ExternData.Remove("tag.CanDestroy");
            }

            JToken CanPlaceOn = ExternData.SelectToken("tag.CanPlaceOn");
            CanPlaceOn ??= ExternData.SelectToken("CanPlaceOn");
            if (CanPlaceOn is JArray canPlaceOnBlockArray && canPlaceOnBlockArray.Count > 0)
            {
                foreach (JValue canPlaceOn in canPlaceOnBlockArray.Cast<JValue>())
                {
                    CanPlaceOnItems canPlaceOnItem = new();
                    canPlaceOnItem.ID.SelectedValuePath = "ComboBoxItemId";
                    canPlaceOnItem.ID.SelectedValue = canPlaceOn;
                }
                ExternData.Remove("tag.CanPlaceOn");
            }

            JToken Enchantments = ExternData.SelectToken("tag.EnchantmentIDAndNameGroupByVersionMap");
            Enchantments ??= ExternData.SelectToken("EnchantmentIDAndNameGroupByVersionMap");
            if (Enchantments is JArray enchantmentArray && enchantmentArray.Count > 0)
            {
                foreach (JObject enchant in enchantmentArray.Cast<JObject>())
                {
                    EnchantmentItem enchantmentItem = new(_context,_viewModel);
                    enchantmentItem.ID.SelectedValuePath = "ComboBoxItemId";
                    enchantmentItem.ID.SelectedValue = enchant.SelectToken("id").ToString();
                    enchantmentItem.Level.Value = int.Parse(enchant.SelectToken("lvl").ToString());
                }
                ExternData.Remove("tag.EnchantmentIDAndNameGroupByVersionMap");
            }
        }

        public async Task Upgrade(int version)
        {
            currentVersion = version;
            await Task.Delay(0);
            if (version < 1130)
                enchantmentKey = "ench";
            else
                enchantmentKey = "EnchantmentIDAndNameGroupByVersionMap";
        }
    }
}