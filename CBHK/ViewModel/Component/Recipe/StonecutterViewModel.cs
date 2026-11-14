using CBHK.Domain;
using CBHK.Interface.Component.Recipe;
using CBHK.Model.Data;
using CBHK.ViewModel.Component.Recipe.Strategy;
using Newtonsoft.Json.Linq;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace CBHK.ViewModel.Component.Recipe
{
    /// <summary>
    /// 切石机：一个材料槽（可多选）+ 一个结果槽
    /// </summary>
    public class StonecutterViewModel : RecipeEditorViewModelBase
    {
        public StonecutterViewModel(IEventAggregator EventAggregator, SlotDropStrategyRegistry DropStrategies) : base(EventAggregator, DropStrategies)
        {
            MaterialSlot = new RecipeSlotViewModel(this, "material", RecipeSlotRole.Material, "材料");
            Slots.Add(MaterialSlot);
            ResultSlot = new RecipeSlotViewModel(this, "result", RecipeSlotRole.Result, "结果");
        }

        #region Property
        public RecipeSlotViewModel MaterialSlot { get; }

        public override RecipeType Kind => RecipeType.Stonecutter;

        public override bool SupportsCount => true;

        protected override string SuccessMessage => "切石机配方生成成功！";
        #endregion

        #region Method
        public override string BuildResult()
        {
            List<ItemStructure> materials = [.. MaterialSlot.Items];
            List<string> tags = [.. MaterialSlot.Tags];
            StringBuilder result = new("{\r\n  \"type\": \"minecraft:stonecutting\",");
            #region 组标识符
            string groupData = GroupName.Length > 0 ? "\"group\":\"" + GroupName + "\"," : "";
            #endregion
            #region 根据配方类型合并材料数据
            StringBuilder keyData = new();
            #region 合并无序数据
            if (MultiSelect)
            {
                keyData.Append("\"ingredient\":[");
                for (int i = 0; i < materials.Count; i++)
                {
                    string itemID = RecipeSlotViewModel.GetItemId(materials[i]);
                    keyData.Append(Format.Ingredient(itemID, tags.Count > 0 && i < tags.Count ? tags[i] : "") + ",");
                }
                //去除末尾逗号并追加闭中括号
                if (keyData.ToString().EndsWith(","))
                    keyData.Remove(keyData.ToString().Length - 1, 1);
                keyData.Append("],");
            }
            else
            {
                keyData.Append("\"ingredient\":");
                string itemID = materials.Count > 0 ? RecipeSlotViewModel.GetItemId(materials[0]) : "";
                keyData.Append(Format.Ingredient(itemID, tags.Count > 0 ? tags[0] : "") + ",");
            }
            string resultItemID = ResultSlot.Items.Count > 0 ? RecipeSlotViewModel.GetItemId(ResultSlot.Items[0]) : "";
            string countValue = int.Parse(Count.ToString()).ToString();
            string resultData;
            string countData;
            if (Format.ResultUsesItemStack)
            {
                // 1.20.5 起 count 收进结果的 ItemStack，外层不再有 count 字段；双括号：结果对象 + 根对象
                resultData = "\"result\":" + Format.ResultObject(resultItemID) + ",\"count\":" + countValue + "}}";
                countData = "";
            }
            else
            {
                resultData = "\"result\":\"minecraft:" + resultItemID + "\"";
                countData = ",\"count\":" + countValue + "}";
            }
            #endregion
            #endregion
            #region 合并最终结果
            result.Append(groupData + keyData + resultData + countData);
            #endregion
            return result.ToString();
        }

        public override void LoadExternalData(JObject json)
        {
            if (json.SelectToken("ingredient") is JObject ingredient)
            {
                LoadMaterial(ingredient);
            }
            else
                if (json.SelectToken("ingredient") is JArray ingredients)
            {
                foreach (JToken item in ingredients)
                    LoadMaterial(item as JObject);
            }

            if (json.SelectToken("result") is JToken resultToken)
            {
                string itemID = resultToken.ToString().Replace("minecraft:", "");
                ResultSlot.Items.Add(CreateItem(itemID));
                ResultSlot.Refresh();
                if (json.SelectToken("result.count") is JToken countToken && int.TryParse(countToken.ToString(), out int count))
                    Count = count;
            }
        }

        /// <summary>
        /// 载入外部导入的一个材料
        /// </summary>
        private void LoadMaterial(JObject ingredient)
        {
            if (ingredient?.SelectToken("item") is not JToken itemToken)
                return;

            string itemID = itemToken.ToString().Replace("minecraft:", "");
            MaterialSlot.Items.Add(CreateItem(itemID));
            MaterialSlot.EnsureTagSlots();
            if (ingredient.SelectToken("tag") is JToken tagToken)
                MaterialSlot.Tags[MaterialSlot.Items.Count - 1] = tagToken.ToString();
            MaterialSlot.Refresh();
        }

        private ItemStructure CreateItem(string itemID)
        {
            string iconPath = AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\" + itemID + ".png";
            return new ItemStructure()
            {
                ImagePath = File.Exists(iconPath) ? new BitmapImage(new Uri(iconPath)) : null,
                IDAndName = itemID
            };
        }
        #endregion
    }
}
