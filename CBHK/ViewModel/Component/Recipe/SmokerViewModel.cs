using CBHK.Domain;
using CBHK.Model.Data;
using CBHK.ViewModel.Component.Recipe.Strategy;
using Newtonsoft.Json.Linq;
using Prism.Events;
using System;
using System.Windows.Media.Imaging;

namespace CBHK.ViewModel.Component.Recipe
{
    /// <summary>
    /// 烟熏炉配方：一个材料槽 + 一个结果槽
    /// </summary>
    public class SmokerViewModel : CookingRecipeViewModelBase
    {
        public SmokerViewModel(IEventAggregator EventAggregator, SlotDropStrategyRegistry DropStrategies)
            : base(EventAggregator, DropStrategies)
        {
        }

        public override RecipeType Kind => RecipeType.Smoker;

        protected override string JsonType => "minecraft:smoking";

        protected override string SuccessMessage => "烟熏炉配方生成成功！";

        /// <summary>
        /// 载入外部导入的配方数据，只写槽位
        /// </summary>
        public override void LoadExternalData(JObject json)
        {
            if (json.SelectToken("ingredient") is JArray ingredients)
            {
                MultiSelect = true;
                foreach (JToken item in ingredients)
                    AddMaterial(item);
            }
            else
                if (json.SelectToken("ingredient") is JObject ingredient)
            {
                MultiSelect = false;
                AddMaterial(ingredient);
            }

            if (json.SelectToken("result") is JToken recipeResult)
                ResultSlot.Items.Add(CreateItem(recipeResult.ToString().Replace("minecraft:", "")));

            MaterialSlot.EnsureTagSlots();
            MaterialSlot.Refresh();
            ResultSlot.Refresh();
        }

        /// <summary>
        /// 一条材料入槽，Tag 与物品下标对齐
        /// </summary>
        private void AddMaterial(JToken item)
        {
            if (item.SelectToken("item") is not JToken itemIDObj)
                return;
            MaterialSlot.Items.Add(CreateItem(itemIDObj.ToString().Replace("minecraft:", "")));
            MaterialSlot.EnsureTagSlots();
            if (item.SelectToken("tag") is JToken itemTagObj)
                MaterialSlot.Tags[MaterialSlot.Items.Count - 1] = itemTagObj.ToString();
        }

        /// <summary>
        /// 物品只存裸 id，与物品库一致
        /// </summary>
        private ItemStructure CreateItem(string itemID)
        {
            return new()
            {
                ImagePath = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + itemID + ".png")),
                IDAndName = itemID
            };
        }
    }
}
