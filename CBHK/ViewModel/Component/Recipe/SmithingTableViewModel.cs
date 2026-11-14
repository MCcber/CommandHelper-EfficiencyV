using CBHK.Domain;
using CBHK.Interface.Component.Recipe;
using CBHK.Model.Data;
using CBHK.ViewModel.Component.Recipe.Strategy;
using Newtonsoft.Json.Linq;
using Prism.Events;
using System;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace CBHK.ViewModel.Component.Recipe
{
    /// <summary>
    /// 锻造台：模板/基础/修饰三个材料槽 + 一个结果槽
    /// </summary>
    public class SmithingTableViewModel : RecipeEditorViewModelBase
    {
        public SmithingTableViewModel(IEventAggregator EventAggregator, SlotDropStrategyRegistry DropStrategies) : base(EventAggregator, DropStrategies)
        {
            TemplateSlot = new RecipeSlotViewModel(this, "template", RecipeSlotRole.Material, "模板");
            BaseSlot = new RecipeSlotViewModel(this, "base", RecipeSlotRole.Material, "基础");
            AdditionSlot = new RecipeSlotViewModel(this, "addition", RecipeSlotRole.Material, "修饰");
            Slots.Add(TemplateSlot);
            Slots.Add(BaseSlot);
            Slots.Add(AdditionSlot);
            ResultSlot = new RecipeSlotViewModel(this, "result", RecipeSlotRole.Result, "结果");
        }

        #region Property
        public RecipeSlotViewModel TemplateSlot { get; }
        public RecipeSlotViewModel BaseSlot { get; }
        public RecipeSlotViewModel AdditionSlot { get; }

        public override RecipeType Kind => RecipeType.SmithingTable;

        public override bool SupportsCount => true;

        public override bool SupportsMultiSelect => false;

        public override bool SupportsTrimPattern => true;

        /// <summary>
        /// 锻造台没有组标识符
        /// </summary>
        public override bool SupportsGroupName => false;

        protected override string SuccessMessage => "锻造台配方生成成功！";
        #endregion

        /// <summary>
        /// 必填槽为空时的占位材料
        /// </summary>
        private const string PlaceholderItemId = "stone";

        /// <summary>
        /// 取槽位材料；必填且为空时用占位并记一条缺材料告警
        /// </summary>
        private string ResolveSmithingPart(RecipeSlotViewModel slot, bool optional, string slotName)
        {
            if (slot.Items.Count > 0)
                return RecipeSlotViewModel.GetItemId(slot.Items[0]);
            if (optional)
                return "";

            AddBuildWarning(slotName + "没放材料，已用 minecraft:" + PlaceholderItemId + " 占位");
            return PlaceholderItemId;
        }

        #region Method
        public override string BuildResult()
        {
            ClearBuildWarnings();
            StringBuilder result = new(ResultSlot.Items.Count > 0 ? "{\r\n  \"type\": \"minecraft:smithing_transform\"," : "{\r\n  \"type\": \"minecraft:smithing_trim\",");
            #region 合并模板数据
            // base 在所有版本都是必填；template/addition 在 1.21.2 之前必填，空槽用占位材料补齐
            string templateItemID = ResolveSmithingPart(TemplateSlot, Format.SmithingPartsOptional, "模板槽");
            string templateData = templateItemID.Length > 0
                ? "\"template\":" + Format.Ingredient(templateItemID, GetTag(TemplateSlot)) + ","
                : "";
            #endregion
            #region 合并基础数据
            string baseItemID = ResolveSmithingPart(BaseSlot, false, "基础槽");
            string baseData = "\"base\":" + Format.Ingredient(baseItemID, GetTag(BaseSlot)) + ",";
            #endregion
            #region 合并修饰数据
            string additionItemID = ResolveSmithingPart(AdditionSlot, Format.SmithingPartsOptional, "修饰槽");
            string additionData = additionItemID.Length > 0
                ? "\"addition\":" + Format.Ingredient(additionItemID, GetTag(AdditionSlot)) + ","
                : "";
            #endregion
            #region 合并结果数据
            string resultData = "";
            if (ResultSlot.Items.Count > 0)
            {
                string resultItemID = RecipeSlotViewModel.GetItemId(ResultSlot.Items[0]);
                resultData = "\"result\":" + Format.ResultObject(resultItemID) + ",\"count\":" + int.Parse(Count.ToString()) + "}";
            }
            #endregion
            #region 合并最终结果
            // 1.21.5 起 smithing_trim 必须带 pattern
            string patternData = ResultSlot.Items.Count == 0 && Format.TrimNeedsPattern && TrimPattern.Length > 0
                ? "\"pattern\":\"minecraft:" + TrimPattern + "\","
                : "";
            result.Append((templateData + baseData + additionData + patternData + resultData).TrimEnd(','));
            result.Append("}");
            #endregion
            return result.ToString();
        }

        public override void LoadExternalData(JObject json)
        {
            LoadSlot(json.SelectToken("template") as JObject, TemplateSlot);
            LoadSlot(json.SelectToken("base") as JObject, BaseSlot);
            LoadSlot(json.SelectToken("addition") as JObject, AdditionSlot);
            LoadSlot(json.SelectToken("result") as JObject, ResultSlot);

            if (json.SelectToken("result.count") is JToken countToken && int.TryParse(countToken.ToString(), out int count))
                Count = count;
        }

        /// <summary>
        /// 载入外部导入的一个槽位数据
        /// </summary>
        private void LoadSlot(JObject ingredient, RecipeSlotViewModel slot)
        {
            if (ingredient?.SelectToken("item") is not JToken itemToken)
                return;

            string itemID = itemToken.ToString().Replace("minecraft:", "");
            slot.Items.Add(CreateItem(itemID));
            slot.EnsureTagSlots();
            if (ingredient.SelectToken("tag") is JToken tagToken)
            {
                if (slot.Tags.Count == 0)
                    slot.Tags.Add("");
                slot.Tags[0] = tagToken.ToString();
            }
            slot.Refresh();
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

        private static string GetTag(RecipeSlotViewModel slot)
        {
            return slot.Tags.Count > 0 ? slot.Tags[0] ?? "" : "";
        }
        #endregion
    }
}
