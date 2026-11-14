using CBHK.Domain;
using CBHK.Interface.Component.Recipe;
using CBHK.Model.Data;
using CBHK.ViewModel.Component.Recipe.Strategy;
using Newtonsoft.Json.Linq;
using Prism.Events;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace CBHK.ViewModel.Component.Recipe
{
    /// <summary>
    /// 工作台配方页：3×3 材料槽 + 结果槽
    /// </summary>
    public class CraftingTableViewModel : RecipeEditorViewModelBase
    {
        #region Field
        /// <summary>
        /// 顺位枚举key
        /// </summary>
        private readonly string[] EnumKeys = ["m", "o", "j", "a", "n", "g", "d", "s", "b"];
        /// <summary>
        /// 枚举Key索引
        /// </summary>
        private int EnumKeyIndex = 0;
        #endregion

        /// <summary>
        /// 初始化数据
        /// </summary>
        /// <param name="EventAggregator"></param>
        /// <param name="DropStrategies"></param>
        public CraftingTableViewModel(IEventAggregator EventAggregator, SlotDropStrategyRegistry DropStrategies) : base(EventAggregator, DropStrategies)
        {
            for (int i = 1; i <= 9; i++)
            {
                var slot = new RecipeSlotViewModel(this, i.ToString(), RecipeSlotRole.Material, "材料" + i);
                Slots.Add(slot);
            }
            ResultSlot = new RecipeSlotViewModel(this, "result", RecipeSlotRole.Result, "结果");
            RefreshAll();
        }

        #region Property
        public override RecipeType Kind => RecipeType.CraftingTable;

        public override bool SupportsShaped => true;

        public override bool SupportsCount => true;
        #endregion

        #region Method
        /// <summary>
        /// 刷新全部槽位显示
        /// </summary>
        private void RefreshAll()
        {
            foreach (RecipeSlotViewModel slot in Slots)
                slot.Refresh();
            ResultSlot?.Refresh();
        }

        public override string BuildResult()
        {
            #region 根据配方类型合并材料数据
            //有序还是无序
            string shapedData = Shaped ? "{\"type\": \"minecraft:crafting_shaped\"," : "{\"type\": \"minecraft:crafting_shapeless\",";
            //组标识符
            string groupData = GroupName.Length > 0 ? "\"group\":\"" + GroupName + "\"," : "";
            StringBuilder keyData = new();
            string patternData = "";
            if (Shaped)
            {
                #region 收集Key数据
                patternData = "\"pattern\":[\"";
                for (int i = 0; i < Slots.Count; i++)
                {
                    RecipeSlotViewModel slot = Slots[i];
                    if (slot.Key.Trim() == "")
                        slot.Key = EnumKeys[i];
                    patternData += (slot.Items.Count > 0 && !string.IsNullOrEmpty(slot.Items[0].IDAndName) ? slot.Key : "") + " ";
                    if ((i + 1) % 3 == 0 && i < (Slots.Count - 1))
                    {
                        if (patternData.EndsWith(' '))
                            patternData = patternData[..(patternData.Length - 1)];
                        patternData += "\",\r\n\"";
                    }
                    if (i == (Slots.Count - 1))
                    {
                        patternData = patternData[..(patternData.Length - 1)];
                        patternData += "\"";
                    }
                }

                patternData += "],";
                #endregion

                #region 合并Key代表的物品数据
                keyData.Append("\"Key\":{");
                for (int index = 0; index < Slots.Count; index++)
                {
                    ObservableCollection<ItemStructure> itemStructureList = Slots[index].Items;
                    ObservableCollection<string> tags = Slots[index].Tags;
                    int tagIndex = -1;
                    string itemData = "";
                    string itemID = "";
                    if (MultiSelect)
                    {
                        itemData = string.Join(",", itemStructureList.Select(item =>
                        {
                            tagIndex++;
                            itemID = RecipeSlotViewModel.GetItemId(item);
                            return Format.Ingredient(itemID, tags.Count > 0 && tagIndex < tags.Count ? tags[tagIndex] : "");
                        }));
                        if (itemData.Length > 0)
                            keyData.Append("\"" + Slots[index].Key + "\":[" + itemData + "],");
                    }
                    else
                    if (itemStructureList.Count > 0)
                    {
                        itemID = RecipeSlotViewModel.GetItemId(itemStructureList[0]);
                        itemData = Format.Ingredient(itemID, tags.Count > 0 ? tags[0] : "");
                        if (itemData.Length > 0)
                            keyData.Append("\"" + Slots[index].Key + "\":" + itemData + ",");
                    }
                }
                //去除末尾逗号并追加闭中括号
                if (keyData.ToString().EndsWith(","))
                    keyData.Remove(keyData.ToString().Length - 1, 1);
                keyData.Append("},");
                #endregion
            }
            else
            {
                #region 合并无序数据
                keyData.Append("\"ingredients\":[");
                for (int i = 0; i < Slots.Count; i++)
                {
                    ObservableCollection<ItemStructure> itemStructures = Slots[i].Items;
                    ObservableCollection<string> tags = Slots[i].Tags;
                    string itemID = "";
                    if (itemStructures.Count > 0 && !MultiSelect)
                    {
                        itemID = RecipeSlotViewModel.GetItemId(itemStructures[0]);
                        keyData.Append(Format.Ingredient(itemID, tags.Count > 0 ? tags[0] : "") + ",");
                    }
                    else
                        if (itemStructures.Count > 0 && MultiSelect)
                    {
                        keyData.Append("[");
                        for (int j = 0; j < itemStructures.Count; j++)
                        {
                            itemID = RecipeSlotViewModel.GetItemId(itemStructures[j]);
                            keyData.Append(Format.Ingredient(itemID, tags.Count > 0 && j <= (tags.Count - 1) ? tags[j] : "") + ",");
                        }
                        //去除末尾逗号并追加闭中括号
                        if (keyData.ToString().EndsWith(","))
                            keyData.Remove(keyData.ToString().Length - 1, 1);
                        keyData.Append("],");
                    }
                }
                //去除末尾逗号并追加闭中括号
                if (keyData.ToString().EndsWith(","))
                    keyData.Remove(keyData.ToString().Length - 1, 1);
                keyData.Append("],");
                #endregion
            }

            #region 合并合成结果数据
            ItemStructure resultStructure = ResultSlot.Items.Count > 0 ? ResultSlot.Items[0] : null;
            string resultItemID = resultStructure is not null ? RecipeSlotViewModel.GetItemId(resultStructure) : "";
            string countValue = int.Parse(Count.ToString()).ToString();
            string resultData = Format.ResultUsesItemStack
                // 1.20.5 起结果是 ItemStack（id 字段）
                ? "\"result\":" + Format.ResultObject(resultItemID) + (Count > 1 ? ",\"count\":" + countValue + "}" : "}") + "}"
                : "\"result\":{\"item\":" + (resultStructure is not null ? "\"minecraft:" + resultItemID + "\"" : "\"\"") + (Count > 1 ? ",\"count\":" + countValue + "}}" : "}}");
            return shapedData + groupData + patternData + keyData + resultData;
            #endregion
            #endregion
        }

        /// <summary>
        /// 打开搜索页时补全该槽的Key
        /// </summary>
        public override string ResolveSlotKey(RecipeSlotViewModel slot)
        {
            if (!string.IsNullOrEmpty(slot.Key))
                return slot.Key;

            slot.Key = EnumKeys[EnumKeyIndex];
            EnumKeyIndex++;
            return slot.Key;
        }

        public override void LoadExternalData(JObject json)
        {
            #region 载入外部材料数据
            string craftingType = json.SelectToken("type").ToString().Replace("minecraft:", "");
            if (craftingType == "crafting_shaped")
            {
                Shaped = true;
                if (json.SelectToken("pattern") is JArray pattern)
                {
                    string[] OneToThree = pattern[0].ToString().Split(' ');
                    string[] FourToSix = pattern[1].ToString().Split(' ');
                    string[] SevenToNine = pattern[2].ToString().Split(' ');
                    for (int i = 0; i < Slots.Count; i++)
                    {
                        if (i < 3)
                            Slots[i].Key = OneToThree[i];
                        if (i >= 3 && i < 6)
                            Slots[i].Key = FourToSix[i - 3];
                        if (i >= 6 && i < 9)
                            Slots[i].Key = SevenToNine[i - 6];
                    }
                }
                if (json.SelectToken("Key") is JObject keyObj)
                {
                    for (int i = 0; i < Slots.Count; i++)
                    {
                        RecipeSlotViewModel slot = Slots[i];
                        if (slot.Key.Length > 0)
                        {
                            if (keyObj[slot.Key] is JObject keyMean)
                            {
                                if (keyMean["item"] is JToken item)
                                    AddItem(slot, item);
                                if (keyMean["tag"] is JToken tag)
                                    slot.Tags.Add(tag.ToString());
                            }
                            else
                                if (keyObj[slot.Key] is JArray keyMeans)
                            {
                                MultiSelect = true;
                                foreach (JToken itemObj in keyMeans)
                                {
                                    if (itemObj["item"] is JToken item)
                                        AddItem(slot, item);
                                    if (itemObj["tag"] is JToken tag)
                                        slot.Tags.Add(tag.ToString());
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (json.SelectToken("ingredients") is JArray ingredients)
                {
                    for (int i = 0; i < ingredients.Count && i < Slots.Count; i++)
                    {
                        RecipeSlotViewModel slot = Slots[i];
                        if (ingredients[i] is JArray items)
                        {
                            MultiSelect = true;
                            foreach (JToken itemObj in items)
                            {
                                if (itemObj["item"] is JToken item)
                                    AddItem(slot, item);
                                if (itemObj["tag"] is JToken tag)
                                    slot.Tags.Add(tag.ToString());
                            }
                        }
                        else
                        if (ingredients[i] is JObject item)
                        {
                            JToken itemIDObj = item["item"];
                            JToken itemTagObj = item["tag"];
                            if (itemIDObj is not null)
                            {
                                AddItem(slot, itemIDObj);
                                if (itemTagObj is not null)
                                    slot.Tags.Add(itemTagObj.ToString());
                            }
                        }
                    }
                }
            }
            #endregion

            #region 载入外部结果数据
            if (json.SelectToken("result") is JToken recipeResult)
            {
                string itemID = recipeResult.SelectToken("item").ToString().Replace("minecraft:", "");
                JToken itemCountObj = json.SelectToken("result.count");
                ResultSlot.Items.Add(new ItemStructure()
                {
                    ImagePath = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + itemID + ".png")),
                    IDAndName = itemID
                });
                if (itemCountObj is not null)
                    Count = int.Parse(itemCountObj.ToString());
            }
            #endregion

            foreach (RecipeSlotViewModel slot in Slots)
                slot.EnsureTagSlots();
            ResultSlot.EnsureTagSlots();
            RefreshAll();
        }

        /// <summary>
        /// 往槽位追加一个物品
        /// </summary>
        private void AddItem(RecipeSlotViewModel slot, JToken itemToken)
        {
            string itemID = itemToken.ToString().Replace("minecraft:", "");
            slot.Items.Add(new ItemStructure()
            {
                ImagePath = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + itemID + ".png")),
                IDAndName = itemID
            });
        }
        #endregion
    }
}
