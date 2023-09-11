using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace cbhk_environment.Generators.ItemGenerator.Components
{
    /// <summary>
    /// Function.xaml 的交互逻辑
    /// </summary>
    public partial class Function : UserControl
    {
        #region 可破坏可放置方块等数据源
        public ObservableCollection<CanDestroyItems> CanDestroyItemsSource { get; set; } = new();
        public ObservableCollection<CanPlaceOnItems> CanPlaceOnItemsSource { get; set; } = new();
        public ObservableCollection<EnchantmentItems> EnchantmentItemsSource { get; set; } = new();
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
        private string EnchantmentResult
        {
            get
            {
                string result = EnchantmentItemsSource.Count > 0 ? "Enchantments:[" + string.Join(",", EnchantmentItemsSource.Select(item => item.Result)) + "]," : "";
                return result;
            }
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
        public string Result
        {
            get
            {
                string result = CanDestroyBlockResult + CanPlaceOnBlockResult + EnchantmentResult + TrimResult;
                return result;
            }
        }
        #endregion

        public Function()
        {
            InitializeComponent();
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
            EnchantmentItemsSource.Clear();
        }

        /// <summary>
        /// 清空可放置方块数据
        /// </summary>
        /// <param name="obj"></param>
        private void ClearCanPlaceOnBlockClick(FrameworkElement obj)
        {
            CanPlaceOnItemsSource.Clear();
        }

        /// <summary>
        /// 清空可破坏方块数据
        /// </summary>
        /// <param name="obj"></param>
        private void ClearCanDestroyBlockClick(FrameworkElement obj)
        {
            CanDestroyItemsSource.Clear();
        }

        /// <summary>
        /// 增加附魔列表成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddEnchantmentClick(FrameworkElement obj)
        {
            EnchantmentItemsSource.Add(new EnchantmentItems());
        }

        /// <summary>
        /// 增加可放置方块列表成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddCanPlaceOnBlockClick(FrameworkElement obj)
        {
            CanPlaceOnItemsSource.Add(new CanPlaceOnItems());
        }

        /// <summary>
        /// 增加可破坏方块列表成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddCanDestroyBlockClick(FrameworkElement obj)
        {
            CanDestroyItemsSource.Add(new CanDestroyItems());
        }
        #endregion

        /// <summary>
        /// 为盔甲纹饰引用路径
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrimData_Click(object sender, RoutedEventArgs e)
        {
            //BetterFolderBrowser folderBrowserDialog = new()
            //{
            //    RootFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
            //    Title = "为盔甲纹饰引用一个命名空间",
            //    Multiselect = false
            //};
            //if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //{
            //    IconTextButtons button = sender as IconTextButtons;
            //    int currentRowIndex = Grid.GetRow(button);
            //    Grid parent = button.Parent as Grid;
            //    foreach (FrameworkElement item in parent.Children)
            //    {
            //        int row = Grid.GetRow(item);
            //        int column = Grid.GetColumn(item);
            //        if (row == currentRowIndex && column == 1)
            //        {
            //            TextBox textBox = item as TextBox;
            //            textBox.Text = folderBrowserDialog.SelectedPath;
            //        }
            //    }
            //}

            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                ShowHiddenFiles = true,
                ShowNewFolderButton = true,
                Description = "为盔甲纹饰引用一个命名空间",
                UseDescriptionForTitle = true
            };
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
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
                        textBox.Text = folderBrowserDialog.SelectedPath;
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
            JToken trim = ExternData.SelectToken("tag.Trim");

            if (materialObj != null)
            {
                material.Text = materialObj.ToString();
                ExternData.Remove("tag.material");
            }
            if (trim != null)
            {
                pattern.Text = trim.ToString();
                ExternData.Remove("tag.Trim");
            }
            if(ExternData.SelectToken("tag.CanDestroy") is JArray canDestroyBlockArray && canDestroyBlockArray.Count > 0)
            {
                foreach (JValue canDestroy in canDestroyBlockArray.Cast<JValue>())
                {
                    CanDestroyItems canDestroyItem = new();
                    canDestroyItem.ID.SelectedValuePath = "ComboBoxItemId";
                    canDestroyItem.ID.SelectedValue = canDestroy;
                }
                ExternData.Remove("tag.CanDestroy");
            }
            if (ExternData.SelectToken("tag.CanPlaceOn") is JArray canPlaceOnBlockArray && canPlaceOnBlockArray.Count > 0)
            {
                foreach (JValue canPlaceOn in canPlaceOnBlockArray.Cast<JValue>())
                {
                    CanPlaceOnItems canPlaceOnItem = new();
                    canPlaceOnItem.ID.SelectedValuePath = "ComboBoxItemId";
                    canPlaceOnItem.ID.SelectedValue = canPlaceOn;
                }
                ExternData.Remove("tag.CanPlaceOn");
            }
            if (ExternData.SelectToken("tag.Enchantments") is JArray enchantmentArray && enchantmentArray.Count > 0)
            {
                foreach (JObject enchant in enchantmentArray.Cast<JObject>())
                {
                    EnchantmentItems enchantmentItem = new();
                    enchantmentItem.ID.SelectedValuePath = "ComboBoxItemId";
                    enchantmentItem.ID.SelectedValue = enchant.SelectToken("id").ToString();
                    enchantmentItem.Level.Value = int.Parse(enchant.SelectToken("lvl").ToString());
                }
                ExternData.Remove("tag.Enchantments");
            }
        }
    }
}
