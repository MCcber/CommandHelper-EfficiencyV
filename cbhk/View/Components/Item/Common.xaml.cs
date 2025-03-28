using CBHK.CustomControls;
using CBHK.CustomControls.Interfaces;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;

namespace CBHK.Generators.ItemGenerator.Components
{
    /// <summary>
    /// Common.xaml 的交互逻辑
    /// </summary>
    public partial class Common : UserControl,IVersionUpgrader
    {
        public DataTable HideInfomationTable { get; set; }
        public ObservableCollection<TextComboBoxItem> HideFlagsSource { get; set; } = [];
        ItemPageViewModel itemPageDataContext = null;
        private int CurrentMinVersion = 1202;

        #region 保存物品信息隐藏选项
        private string ItemHideFlags
        {
            get
            {
                string result = "";
                if (HideFlagsBox.SelectedItem is null) return "";
                Dispatcher.Invoke(() =>
                {
                    string selectedItem = (HideFlagsBox.SelectedItem as TextComboBoxItem).Text;
                    string key = HideInfomationTable.Select("name='" + selectedItem + "'").First()["id"].ToString();
                    result = key != "0" ? "HideFlags:" + key + "," : "";
                });
                return result;
            }
        }
        #endregion
        #region 保存名称与描述
        public string ItemNameValue = "";
        public string ItemLoreValue = "";
        private string ItemDisplay
        {
            get
            {
                string DisplayNameString = "";
                string ItemLoreString = "";
                if (ItemNameValue.Trim() != "")
                {
                    if (CurrentMinVersion >= 1130)
                        DisplayNameString = @"Name:'[" + ItemNameValue.Replace(@"\\n", "") + "]',";
                    else
                        DisplayNameString = @"Name:\\\\\\\""" + ItemNameValue.Replace(@"\\n", "").Replace(@"""", @"\\\""") + @"\\\\\\\"",";
                }
                if (ItemLoreValue.Trim() != "")
                {
                    if (CurrentMinVersion >= 1130)
                        ItemLoreString = "Lore:[" + ItemLoreValue + "]";
                    else
                        ItemLoreString = @"Lore:[\\\\\\\""" + ItemLoreValue.Replace(@"""", @"\\\""") + @"\\\\\\\""]";
                }
                string result = DisplayNameString != "" || ItemLoreString != "" ? "display:{" + (DisplayNameString + ItemLoreString).TrimEnd(',') + "}," : "";
                return result;
            }
        }
        #endregion
        #region 无法破坏
        private string UnbreakableString
        {
            get => Dispatcher.Invoke(() =>
            {
                return Unbreakable.IsChecked.Value ? "Unbreakable:1b," : "";
            }); 
        }
        #endregion
        #region 交互锁定
        private string CustomCreativeLockResult
        {
            get => Dispatcher.Invoke(() =>
            {
                return CustomCreativeLock.IsChecked.Value ? "CustomCreativeLock:{}," : "";
            }); 
        }
        #endregion
        #region 物品模型
        private string CustomModelDataResult
        {
            get => Dispatcher.Invoke(() =>
            {
                return CustomModelData.Value >= 0 ? "CustomModelData:" + CustomModelData.Value + "," : "";
            }); 
        }
        #endregion
        #region 修理损耗
        private string RepairCostResult
        {
            get => Dispatcher.Invoke(() =>
            {
                return RepairCost.Value > 0 ? "RepairCost:" + RepairCost.Value + "," : "";
            }); 
        }
        #endregion

        public Common()
        {
            InitializeComponent();
            ItemLore.Name = "ItemLore";
            ItemLore.IsMultiLine = true;
        }

        /// <summary>
        /// 获取外来数据
        /// </summary>
        /// <param name="ExternData"></param>
        public void GetExternData(ref JObject ExternData)
        {
            JToken unbreakble = ExternData.SelectToken("tag.Unbreakable");
            unbreakble ??= ExternData.SelectToken("Unbreakable");
            JToken customCreativeLock = ExternData.SelectToken("tag.CustomCreativeLock");
            customCreativeLock ??= ExternData.SelectToken("CustomCreativeLock");
            JToken name = ExternData.SelectToken("tag.display.Name");
            name ??= ExternData.SelectToken("display.Name");
            JToken lore = ExternData.SelectToken("tag.display.Lore");
            lore ??= ExternData.SelectToken("display.Lore");
            JToken HideFlags = ExternData.SelectToken("tag.HideFlags");
            HideFlags ??= ExternData.SelectToken("HideFlags");
            JToken CustomModelDataObj = ExternData.SelectToken("tag.CustomModelData");
            CustomModelDataObj ??= ExternData.SelectToken("CustomModelData");
            JToken RepairCostObj = ExternData.SelectToken("tag.RepairCost");
            RepairCostObj ??= ExternData.SelectToken("RepairCost");

            if (unbreakble != null)
            {
                Unbreakable.IsChecked = unbreakble.ToString().ToLower() == "true" || unbreakble.ToString().ToLower() == "1";
                ExternData.Remove("tag.Unbreakable");
                ExternData.Remove("Unbreakable");
            }
            if (customCreativeLock != null)
            {
                CustomCreativeLock.IsChecked = customCreativeLock.ToString().ToLower() == "true" || customCreativeLock.ToString().ToLower() == "1";
                ExternData.Remove("tag.CustomCreativeLock");
                ExternData.Remove("CustomCreativeLock");
            }
            if (name != null)
            {
                ((ItemName.richTextBox.Document.Blocks.FirstBlock as Paragraph).Inlines.FirstInline as RichRun).Text = JObject.Parse(name.ToString())["text"].ToString();
                ExternData.Remove("tag.display.Name");
                ExternData.Remove("display.Name");
            }
            if (lore != null)
            {
                JArray loreArray = JArray.Parse(lore.ToString());
                ((ItemLore.richTextBox.Document.Blocks.FirstBlock as Paragraph).Inlines.FirstInline as RichRun).Text = string.Join("",loreArray).Replace("\"", "").Trim('[').Trim(']');
                ExternData.Remove("tag.display.Lore");
                ExternData.Remove("display.Lore");
            }
            if (HideFlags != null)
            {
                string selectedItem = HideFlagsBox.SelectedValue.ToString();
                string value = HideInfomationTable.Select("id='" + selectedItem + "'").First()["name"].ToString();
                HideFlagsBox.SelectedValue = value;
                ExternData.Remove("tag.HideFlags");
                ExternData.Remove("HideFlags");
            }
            if (CustomModelDataObj != null)
            {
                CustomModelData.Value = int.Parse(CustomModelDataObj.ToString());
                ExternData.Remove("tag.CustomModelData");
                ExternData.Remove("CustomModelData");
            }
            if (RepairCostObj != null)
            {
                RepairCost.Value = int.Parse(RepairCostObj.ToString());
                ExternData.Remove("tag.RepairCost");
                ExternData.Remove("RepairCost");
            }
        }

        public async Task Upgrade(int version)
        {
            await Task.Delay(0);
            CurrentMinVersion = version;
            await ItemName.Upgrade(version);
            await ItemLore.Upgrade(version);
        }

        public async Task<string> Result()
        {
            string result = "";
            string tag = "";
            if (itemPageDataContext is not null)
                CurrentMinVersion = itemPageDataContext.CurrentMinVersion;
            await ItemName.Upgrade(CurrentMinVersion);
            await ItemLore.Upgrade(CurrentMinVersion);
            await CustomTag.GetResult();
            tag = CustomTag.Result;
            ItemNameValue = await ItemName.Result();
            ItemLoreValue = await ItemLore.Result();
            result = tag + ItemDisplay + ItemHideFlags + UnbreakableString + CustomCreativeLockResult + CustomModelDataResult + RepairCostResult;
            return result;
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            foreach (DataRow row in HideInfomationTable.Rows)
            {
                string name = row["name"].ToString();
                HideFlagsSource.Add(new TextComboBoxItem() { Text = name });
            }
            HideFlagsBox.ItemsSource = HideFlagsSource;
        }
    }
}