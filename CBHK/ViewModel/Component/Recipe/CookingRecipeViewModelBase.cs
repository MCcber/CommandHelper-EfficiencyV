using CBHK.Interface.Component.Recipe;
using CBHK.Model.Data;
using CBHK.ViewModel.Component.Recipe.Strategy;
using Prism.Events;
using System.Collections.Generic;
using System.Text;

namespace CBHK.ViewModel.Component.Recipe
{
    /// <summary>
    /// 熔炉/烟熏炉/高炉/篝火共用：一个材料槽 + 一个结果槽
    /// </summary>
    public abstract partial class CookingRecipeViewModelBase : RecipeEditorViewModelBase
    {
        protected CookingRecipeViewModelBase(IEventAggregator eventAggregator, SlotDropStrategyRegistry dropStrategies)
            : base(eventAggregator, dropStrategies)
        {
            MaterialSlot = new RecipeSlotViewModel(this, "material", RecipeSlotRole.Material, "材料");
            Slots.Add(MaterialSlot);
            ResultSlot = new RecipeSlotViewModel(this, "result", RecipeSlotRole.Result, "结果");
        }

        public RecipeSlotViewModel MaterialSlot { get; }

        public override bool SupportsExperience => true;
        public override bool SupportsCookingTime => true;

        /// <summary>
        /// minecraft 配方类型串，如 minecraft:smelting
        /// </summary>
        protected abstract string JsonType { get; }

        public override string BuildResult()
        {
            List<ItemStructure> materials = [.. MaterialSlot.Items];
            List<string> tags = [.. MaterialSlot.Tags];
            StringBuilder result = new("{\r\n  \"type\": \"" + JsonType + "\",");
            string groupData = GroupName.Length > 0 ? "\"group\":\"" + GroupName + "\"," : "";
            StringBuilder keyData = new();
            if (MultiSelect)
            {
                keyData.Append("\"ingredient\":[");
                for (int i = 0; i < materials.Count; i++)
                {
                    string itemID = RecipeSlotViewModel.GetItemId(materials[i]);
                    keyData.Append(Format.Ingredient(itemID, tags.Count > 0 && i < tags.Count ? tags[i] : "") + ",");
                }
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
            string cookingTimeData = ",\"cookingtime\":" + int.Parse(Cookingtime.ToString());
            string experienceData = ",\"experience\":" + int.Parse(Experience.ToString()) + "}";
            string resultData = Format.ResultUsesItemStack
                ? "\"result\":" + Format.ResultObject(resultItemID) + "}"
                : "\"result\":\"minecraft:" + resultItemID + "\"";
            result.Append(groupData + keyData + resultData + cookingTimeData + experienceData);
            return result.ToString();
        }
    }
}
