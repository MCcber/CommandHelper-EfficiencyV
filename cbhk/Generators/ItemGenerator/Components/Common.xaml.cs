﻿using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows.Controls;
using System.Windows;

namespace cbhk.Generators.ItemGenerator.Components
{
    /// <summary>
    /// Common.xaml 的交互逻辑
    /// </summary>
    public partial class Common : UserControl
    {
        public DataTable HideInfomationTable = null;
        public ObservableCollection<string> HideFlagsSource { get; set; } = new();
        #region 保存物品信息隐藏选项
        private string ItemHideFlags
        {
            get
            {
                if (HideFlagsBox.SelectedValue == null) return "";
                string selectedItem = HideFlagsBox.SelectedValue.ToString();
                string key = HideInfomationTable.Select("name='" + selectedItem + "'").First()["id"].ToString();
                string result = key != "0" ? "HideFlags:" + key + "," : "";
                return result;
            }
        }
        #endregion
        #region 保存名称与描述
        private string ItemDisplay
        {
            get
            {
                string DisplayNameString = ItemName.Text.Trim() != "" ? "Name:'{\"text\":\"" + ItemName.Text + "\"}'," : "";
                string ItemLoreString = ItemLore.Text.Trim() != "" ? "Lore:[\"[\\\"" + ItemLore.Text + "\\\"]\"]" : "";
                string result = DisplayNameString != "" || ItemLoreString != "" ? "display:{" + (DisplayNameString + ItemLoreString).TrimEnd(',') + "}," : "";
                return result;
            }
        }
        #endregion
        #region 无法破坏
        private string UnbreakableString
        {
            get => Unbreakable.IsChecked.Value ? "Unbreakable:1b," : "";
        }
        #endregion
        #region 交互锁定
        private string CustomCreativeLockResult
        {
            get => CustomCreativeLock.IsChecked.Value ? "CustomCreativeLock:{}," : "";
        }
        #endregion
        #region 物品模型
        private string CustomModelDataResult
        {
            get => CustomModelData.Value >= 0 ? "CustomModelData:" + CustomModelData.Value + "," : "";
        }
        #endregion
        #region 修理损耗
        private string RepairCostResult
        {
            get => RepairCost.Value > 0 ? "RepairCost:" + RepairCost.Value + "," : "";
        }
        #endregion
        #region 合并结果
        public string Result
        {
            get => ItemDisplay + ItemHideFlags + UnbreakableString + CustomCreativeLockResult + CustomModelDataResult + RepairCostResult;
        }
        #endregion

        public Common()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 获取外来数据
        /// </summary>
        /// <param name="ExternData"></param>
        public void GetExternData(ref JObject ExternData)
        {
            JToken unbreakble = ExternData.SelectToken("tag.Unbreakable");
            JToken customCreativeLock = ExternData.SelectToken("tag.CustomCreativeLock");
            JToken name = ExternData.SelectToken("tag.display.Name");
            JToken lore = ExternData.SelectToken("tag.display.Lore");
            JToken HideFlags = ExternData.SelectToken("tag.HideFlags");
            JToken CustomModelDataObj = ExternData.SelectToken("tag.CustomModelData");
            JToken RepairCostObj = ExternData.SelectToken("tag.RepairCost");

            if (unbreakble != null)
            {
                Unbreakable.IsChecked = unbreakble.ToString().ToLower() == "true" || unbreakble.ToString().ToLower() == "1";
                ExternData.Remove("tag.Unbreakable");
            }
            if (customCreativeLock != null)
            {
                CustomCreativeLock.IsChecked = customCreativeLock.ToString().ToLower() == "true" || customCreativeLock.ToString().ToLower() == "1";
                ExternData.Remove("tag.CustomCreativeLock");
            }
            if (name != null)
            {
                ItemName.Text = JObject.Parse(name.ToString())["text"].ToString();
                ExternData.Remove("tag.display.Name");
            }
            if (lore != null)
            {
                JArray loreArray = JArray.Parse(lore.ToString());
                ItemLore.Text = string.Join("",loreArray).Replace("\"", "").Trim('[').Trim(']');
                ExternData.Remove("tag.display.Lore");
            }
            if (HideFlags != null)
            {
                string selectedItem = HideFlagsBox.SelectedValue.ToString();
                string value = HideInfomationTable.Select("id='" + selectedItem + "'").First()["name"].ToString();
                HideFlagsBox.SelectedValue = value;
                ExternData.Remove("tag.HideFlags");
            }
            if (CustomModelDataObj != null)
            {
                CustomModelData.Value = int.Parse(CustomModelDataObj.ToString());
                ExternData.Remove("tag.CustomModelData");
            }
            if (RepairCostObj != null)
            {
                RepairCost.Value = int.Parse(RepairCostObj.ToString());
                ExternData.Remove("tag.RepairCost");
            }
        }
    }
}