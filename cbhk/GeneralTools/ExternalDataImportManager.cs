using cbhk.CustomControls;
using cbhk.GeneralTools.MessageTip;
using cbhk.Generators.FireworkRocketGenerator.Components;
using cbhk.Generators.ItemGenerator.Components;
using cbhk.Generators.RecipeGenerator.Components;
using cbhk.Generators.SpawnerGenerator.Components;
using cbhk.Model.Common;
using cbhk.Model.Generator.Tag;
using cbhk.View.Compoments.Spawner;
using cbhk.ViewModel.Components.Recipe;
using cbhk.ViewModel.Components.Villager;
using cbhk.ViewModel.Generators;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace cbhk.GeneralTools
{
    public static class ExternalDataImportManager
    {
        private static DataCommunicator dataCommunicator = DataCommunicator.GetDataCommunicator();
        private static DataTable ItemTable = null;
        private static DataTable EntityTable = null;
        public static void Init()
        {
            Task.Run(async () =>
            {
                ItemTable = await dataCommunicator.GetData("SELECT * FROM Items");
                EntityTable = await dataCommunicator.GetData("SELECT * FROM Entities");
            });
        }

        #region 处理导入外部标签
        public static void ImportTagDataHandler(string filePathOrData, ref ObservableCollection<TagItemTemplate> itemList,ref TagViewModel context, bool IsPath = true)
        {
            string data = IsPath ? File.ReadAllText(filePathOrData) : filePathOrData;

            try
            {
                JObject content = JObject.Parse(data);
                if (content.SelectToken("replace") is JToken replace)
                    context.Replace = replace.ToString().ToString().ToLower() == "true";
                if(content.SelectToken("values") is JArray valuesArray)
                {
                    List<string> values = valuesArray.ToList().ConvertAll(item=>item.ToString().Replace("minecraft:",""));
                    StringBuilder id = new();
                    foreach (var item in itemList)
                    {
                        id.Clear();
                        id.Append(item.DisplayId);
                        item.BeChecked = values.Where(value => value == id.ToString()).Any();
                        if(item.BeChecked.Value)
                        {
                            switch (item.DataType)
                            {
                                case "ItemView":
                                    context.Items.Add(id.ToString());
                                    break;
                                case "Block&ItemView":
                                    context.Blocks.Add(id.ToString());
                                    break;
                                case "EntityView":
                                    context.Entities.Add(id.ToString());
                                    break;
                                case "GameEvent":
                                    context.GameEvent.Add(id.ToString());
                                    break;
                                case "Biome":
                                    context.Biomes.Add(id.ToString());
                                    break;
                            }
                        }
                    }
                }
            }
            catch
            {
                Message.PushMessage("导入失败！文件内容格式不合法");
            }
        }
        #endregion

        #region 处理导入外部配方
        public static void ImportRecipeDataHandler(string filePathOrData, ref RecipeViewModel recipeContext, bool IsPath = true)
        {
            string data = IsPath ? File.ReadAllText(filePathOrData) : filePathOrData;
            try
            {
                JObject json = JObject.Parse(data);
                if (json["type"] is JToken recipeType)
                {
                    switch (recipeType.ToString().Replace("minecraft:",""))
                    {
                        case "crafting_shaped":
                        case "crafting_shapeness":
                            {
                                RecipeViewModel.RecipeType type = RecipeViewModel.RecipeType.CraftingTable;
                                CraftingTableView craftingTable = recipeContext.AddExternRecipe(type) as CraftingTableView;
                                CraftingTableViewModel context = craftingTable.DataContext as CraftingTableViewModel;
                                context.ImportMode = true;
                                context.ExternalData = json;
                            }
                            break;
                        case "smithing_transform":
                        case "smithing_trim":
                            {
                                RecipeViewModel.RecipeType type = RecipeViewModel.RecipeType.SmithingTable;
                                SmithingTableView smithingTable = recipeContext.AddExternRecipe(type) as SmithingTableView;
                                SmithingTableViewModel context = smithingTable.DataContext as SmithingTableViewModel;
                                context.ImportMode = true;
                                context.ExternalData = json;
                            }
                            break;
                        case "blasting":
                            {
                                RecipeViewModel.RecipeType type = RecipeViewModel.RecipeType.BlastFurnace;
                                BlastFurnaceView blastFurnace = recipeContext.AddExternRecipe(type) as BlastFurnaceView;
                                BlastFurnaceViewModel context = blastFurnace.DataContext as BlastFurnaceViewModel;
                                context.ImportMode = true;
                                context.ExternalData = json;
                            }
                            break;
                        case "campfire_cooking":
                            {
                                RecipeViewModel.RecipeType type = RecipeViewModel.RecipeType.Campfire;
                                CampfireView campfire = recipeContext.AddExternRecipe(type) as CampfireView;
                                CampfireViewModel context = campfire.DataContext as CampfireViewModel;
                                context.ImportMode = true;
                                context.ExternalData = json;
                            }
                            break;
                        case "smelting":
                            {
                                RecipeViewModel.RecipeType type = RecipeViewModel.RecipeType.Furnace;
                                FurnaceView furnace = recipeContext.AddExternRecipe(type) as FurnaceView;
                                FurnaceViewModel context = furnace.DataContext as FurnaceViewModel;
                                context.ImportMode = true;
                                context.ExternalData = json;
                            }
                            break;
                        case "smoker":
                            {
                                RecipeViewModel.RecipeType type = RecipeViewModel.RecipeType.Smoker;
                                SmokerView smoker = recipeContext.AddExternRecipe(type) as SmokerView;
                                SmokerViewModel context = smoker.DataContext as SmokerViewModel;
                                context.ImportMode = true;
                                context.ExternalData = json;
                            }
                            break;
                        case "stonecutting":
                            {
                                RecipeViewModel.RecipeType type = RecipeViewModel.RecipeType.Stonecutter;
                                StonecutterView stonecutter = recipeContext.AddExternRecipe(type) as StonecutterView;
                                StonecutterViewModel context = stonecutter.DataContext as StonecutterViewModel;
                                context.ImportMode = true;
                                context.ExternalData = json;
                            }
                            break;
                    }
                }
            }
            catch
            {
                Message.PushMessage("导入失败！文件内容格式不合法");
            }
        }
        #endregion

        #region 处理导入外部刷怪笼
        public static void ImportSpawnerDataHandler(string filePathOrData, ref ObservableCollection<RichTabItems> itemPageList, bool IsPath = true)
        {
            string data = IsPath ? File.ReadAllText(filePathOrData) : filePathOrData;

            #region 提取可用NBT数据
            string nbtData = "";
            if (data.Contains('{') && data.Contains('}'))
                nbtData = data[data.IndexOf('{')..(data.LastIndexOf('}') + 1)];
            //补齐缺失双引号对的key
            nbtData = Regex.Replace(nbtData, @"([\{\[,])([\s+]?\w+[\s+]?):", "$1\"$2\":");
            //清除数值型数据的单位
            nbtData = Regex.Replace(nbtData, @"(\d+[\,\]\}]?)([a-zA-Z])", "$1").Replace("I;", "");
            #endregion

            try
            {
                JObject nbtObj = JObject.Parse(nbtData);
                //确定版本
                bool version1_12 = Regex.IsMatch(data, @"^/setblock (minecraft:)?mob_spawner");
                AddSpawnerData(nbtObj,version1_12?"1.12.0":"1.13.0",itemPageList);
            }
            catch
            {
                Message.PushMessage("导入失败！文件内容格式不合法");
            }
        }
        public static string GetSpawnerDataHandler(string filePathOrData, bool IsPath = true)
        {
            string result = "";
            string data = IsPath ? File.ReadAllText(filePathOrData) : filePathOrData;

            if (data.Length == 0) return result;

            #region 提取可用NBT数据和实体ID
            if (data.Contains('{') && data.Contains('}'))
                result = data[data.IndexOf('{')..(data.LastIndexOf('}') + 1)];

            if (result.Length > 0)
            {
                //补齐缺失双引号对的key
                result = Regex.Replace(result, @"([\{\[,])([\s+]?\w+[\s+]?):", "$1\"$2\":");
                //清除数值型数据的单位
                result = Regex.Replace(result, @"(\d+[\,\]\}]?)([a-zA-Z])", "$1").Replace("I;", "");
            }

            return result;
            #endregion
        }

        /// <summary>
        /// 添加刷怪笼
        /// </summary>
        /// <param name="nbtObj"></param>
        /// <param name="itemPageList"></param>
        private static void AddSpawnerData(JObject nbtObj,string version, ObservableCollection<RichTabItems> itemPageList)
        {
            SpawnerPageView spawnerPage = new() { FontWeight = FontWeights.Normal };
            RichTabItems richTabItems = new()
            {
                Header = "刷怪笼",
                IsContentSaved = true,
                Content = spawnerPage,
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                BorderThickness = new(4, 4, 4, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                SelectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC6B23")),
                Foreground = new SolidColorBrush(Colors.White),
                LeftBorderTexture = Application.Current.Resources["TabItemLeft"] as Brush,
                RightBorderTexture = Application.Current.Resources["TabItemRight"] as Brush,
                TopBorderTexture = Application.Current.Resources["TabItemTop"] as Brush,
                SelectedLeftBorderTexture = Application.Current.Resources["SelectedTabItemLeft"] as Brush,
                SelectedRightBorderTexture = Application.Current.Resources["SelectedTabItemRight"] as Brush,
                SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as Brush,
            };
            SpawnerPageViewModel context = spawnerPage.DataContext as SpawnerPageViewModel;
            itemPageList.Add(richTabItems);
            context.SelectedVersion = version == "1.12.0" ? context.VersionSource[1] : context.VersionSource[0];

            #region 开启导入模式，为外部数据赋值
            context.ExternalSpawnerData = nbtObj;
            context.ImportMode = true;
            #endregion

            #region 处理潜在实体数据
            if (nbtObj.SelectToken("SpawnPotentials") is JArray spawnPotentials)
            {
                foreach (JObject spawnPotential in spawnPotentials.Cast<JObject>())
                {
                    context.AddSpawnPotential(null);
                    SpawnPotential spawnPotentialInstance = context.SpawnPotentials[^1];
                    if (spawnPotential.SelectToken("weight") is JToken weight)
                        spawnPotentialInstance.weight.Value = short.Parse(weight.ToString());

                    #region 方块光限制
                    if (spawnPotential.SelectToken("data.custom_spawn_rules.block_light_limit") is JArray BlockLightArray)
                    {
                        spawnPotentialInstance.BlockLightValueType.IsChecked = false;
                        spawnPotentialInstance.UseDefaultBlockLightValue.IsChecked = false;
                        spawnPotentialInstance.BlockLightRange.Visibility = Visibility.Visible;
                        spawnPotentialInstance.BlockLightValue.Visibility = Visibility.Collapsed;
                        spawnPotentialInstance.BlockLightMinValue.Value = int.Parse(BlockLightArray[0].ToString());
                        spawnPotentialInstance.BlockLightMaxValue.Value = int.Parse(BlockLightArray[1].ToString());
                    }
                    else
                        if (spawnPotential.SelectToken("data.custom_spawn_rules.block_light_limit") is JToken BlockLight)
                    {
                        spawnPotentialInstance.BlockLightValueType.IsChecked = true;
                        spawnPotentialInstance.UseDefaultBlockLightValue.IsChecked = false;
                        spawnPotentialInstance.BlockLightRange.Visibility = Visibility.Collapsed;
                        spawnPotentialInstance.BlockLightValue.Visibility = Visibility.Visible;
                        spawnPotentialInstance.BlockLightValue.Value = int.Parse(BlockLight.ToString());
                    }
                    #endregion

                    #region 天空光限制
                    if (spawnPotential.SelectToken("data.custom_spawn_rules.sky_light_limit") is JArray SkyLightArray)
                    {
                        spawnPotentialInstance.SkyLightValueType.IsChecked = false;
                        spawnPotentialInstance.UseDefaultSkyLightValue.IsChecked = false;
                        spawnPotentialInstance.SkyLightRange.Visibility = Visibility.Visible;
                        spawnPotentialInstance.SkyLightValue.Visibility = Visibility.Collapsed;
                        spawnPotentialInstance.SkyLightMinValue.Value = int.Parse(SkyLightArray[0].ToString());
                        spawnPotentialInstance.SkyLightMaxValue.Value = int.Parse(SkyLightArray[1].ToString());
                    }
                    else
                        if (spawnPotential.SelectToken("data.custom_spawn_rules.sky_light_limit") is JToken SkyLight)
                    {
                        spawnPotentialInstance.SkyLightValueType.IsChecked = true;
                        spawnPotentialInstance.UseDefaultSkyLightValue.IsChecked = false;
                        spawnPotentialInstance.SkyLightRange.Visibility = Visibility.Collapsed;
                        spawnPotentialInstance.SkyLightValue.Visibility = Visibility.Visible;
                        spawnPotentialInstance.SkyLightValue.Value = int.Parse(SkyLight.ToString());
                    }
                    #endregion

                    #region 实体数据
                    if (spawnPotential.SelectToken("data.entity") is JObject entity)
                    {
                        ImageBrush imageBrush = null;
                        string data = entity.ToString();
                        spawnPotentialInstance.entity.Tag = data;
                        string entityID = JObject.Parse(data)["id"].ToString().Replace("minecraft:","");
                        string rootPath = AppDomain.CurrentDomain.BaseDirectory + "resources\\data_sources\\entityImages\\";
                        string iconPath = rootPath + entityID + ".png";
                        if (!File.Exists(iconPath))
                        {
                            iconPath = rootPath + entityID + "_spawn_egg.png";
                            imageBrush = new(new BitmapImage(new Uri(iconPath)));
                        }
                        spawnPotentialInstance.entity.EntityIcon.Background = imageBrush;
                    }
                    #endregion
                }
            }
            #endregion

            TabControl tabControl = richTabItems.FindParent<TabControl>();
            tabControl.SelectedIndex = itemPageList.Count - 1;
        }
        #endregion

        #region 处理导入外部实体数据
        public static string GetEntityDataHandler(string filePathOrData, bool IsPath = true)
        {
            string result = "";
            string data = IsPath ? File.ReadAllText(filePathOrData) : filePathOrData;
            string entityID = "";

            

            //召唤实体物品
            if (Regex.IsMatch(data, @"^/?summon"))
                entityID = Regex.Match(data, @"(?<=/?summon\s)([\w:]+)").ToString();

            if (data.Length == 0) return result;

            #region 提取可用NBT数据和实体ID
            if (data.Contains('{') && data.Contains('}'))
                result = data[data.IndexOf('{')..(data.LastIndexOf('}') + 1)];
            if(result.Length > 0)
            {
                //补齐缺失双引号对的key
                result = Regex.Replace(result, @"([\{\[,])([\s+]?\w+[\s+]?):", "$1\"$2\":");
                //清除数值型数据的单位
                result = Regex.Replace(result, @"(\d+[\,\]\}]?)([a-zA-Z])", "$1").Replace("I;", "");

                #region 插入实体ID
                try
                {
                    JObject resultObj = JObject.Parse(result);
                    JToken idObj = resultObj.SelectToken("id");
                    idObj ??= resultObj.SelectToken("EntityTag.id");
                    if (idObj is null)
                        resultObj.Add("id", "\"minecraft:" + entityID + "\"");
                    result = resultObj.ToString();
                }
                catch
                {
                    Message.PushMessage("导入失败！文件内容格式不合法");
                }
                #endregion
            }
            return result;
            #endregion
        }
        
        /// <summary>
        /// 导入村民数据
        /// </summary>
        public static void ImportVillagerDataHandler(string filePathOrData,VillagerViewModel context, bool IsPath = true)
        {
            string data = IsPath ? File.ReadAllText(filePathOrData) : filePathOrData;

            

            #region 提取可用NBT数据和村民ID
            string nbtData = "";
            if (data.Contains('{') && data.Contains('}'))
                nbtData = data[data.IndexOf('{')..(data.LastIndexOf('}') + 1)];
            //补齐缺失双引号对的key
            nbtData = Regex.Replace(nbtData, @"([\{\[,])([\s+]?\w+[\s+]?):", "$1\"$2\":");
            //清除数值型数据的单位
            nbtData = Regex.Replace(nbtData, @"(\d+[\,\]\}]?)([a-zA-Z])", "$1").Replace("I;", "");
            #endregion

            try
            {
                JObject nbtObj = JObject.Parse(nbtData);
                if (nbtObj.SelectToken("EntitTag") is JObject entityTag)
                    nbtObj = JObject.Parse(entityTag.ToString());
                AddVillagerData(nbtObj,context);
            }
            catch
            {
                Message.PushMessage("导入失败！文件内容格式不合法");
            }
        }

        /// <summary>
        /// 设置村民数据
        /// </summary>
        /// <param name="nbtObj"></param>
        /// <param name="context"></param>
        private static void AddVillagerData(JObject nbtObj,VillagerViewModel context)
        {
            #region 处理交易数据
            if(nbtObj.SelectToken("Offers.Recipes") is JArray Recipes)
            {
                string rootPath = AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\";
                foreach (JObject recipe in Recipes.Cast<JObject>())
                {
                    #region 读取数据
                    #region buy,buyB,sell
                    string buyID = "";
                    JToken buyCountObj = recipe.SelectToken("buy.Count");

                    string buyBID = "";
                    JToken buyBCountObj = recipe.SelectToken("buyB.Count");

                    string sellID = "";
                    JToken sellCountObj = recipe.SelectToken("sell.Count");
                    #endregion
                    #region other
                    JToken demand = recipe.SelectToken("demand");
                    JToken maxUses = recipe.SelectToken("maxUses");
                    JToken priceMultiplier = recipe.SelectToken("priceMultiplier");
                    JToken rewardExp = recipe.SelectToken("rewardExp");
                    JToken specialPrice = recipe.SelectToken("specialPrice");
                    JToken uses = recipe.SelectToken("uses");
                    JToken xp = recipe.SelectToken("xp");
                    #endregion
                    #endregion

                    #region 应用数据
                    #region buy
                    if (recipe.SelectToken("buy.id") is JToken buyIDObj)
                        buyID = buyIDObj.ToString().Replace("minecraft:", "");
                    bool ExistItem = false;
                    if (buyID.Length > 0)
                    {
                        ExistItem = true;
                        context.AddTransactionItem();
                        TransactionItemsViewModel transactionItemsViewModel = context.transactionItems[^1].DataContext as TransactionItemsViewModel;
                        string iconPath = rootPath + buyID + ".png";
                        if (File.Exists(iconPath))
                            transactionItemsViewModel.Buy.Source = new BitmapImage(new Uri(iconPath, UriKind.Absolute));

                        if (buyCountObj != null)
                        {
                            transactionItemsViewModel.BuyCountDisplayText = "x" + int.Parse(buyCountObj.ToString());
                        }
                        Uri iconUri = new(iconPath, UriKind.Absolute);
                        ItemStructure imageTag = new(new BitmapImage(iconUri), buyID, recipe.SelectToken("buy.tag") is JObject buyTagObj ? buyTagObj.ToString() : "");
                        transactionItemsViewModel.Buy.Source = new BitmapImage(iconUri);
                        transactionItemsViewModel.Buy.Tag = imageTag;
                    }
                    #endregion
                    #region buyB
                    if(ExistItem)
                    {
                        if (recipe.SelectToken("buyB.id") is JToken buyBIDObj)
                            buyBID = buyBIDObj.ToString().Replace("minecraft:", "");
                        if (buyBID.Length > 0)
                        {
                            string iconPath = rootPath + buyBID + ".png";
                            TransactionItemsViewModel transactionItemsViewModel = context.transactionItems[^1].DataContext as TransactionItemsViewModel;
                            if (File.Exists(iconPath))
                                transactionItemsViewModel.BuyB.Source = new BitmapImage(new Uri(iconPath, UriKind.Absolute));

                            if (buyBCountObj != null)
                            {
                                transactionItemsViewModel.BuyBCountDisplayText = "x" + int.Parse(buyCountObj.ToString());
                            }
                            Uri iconUri = new(iconPath, UriKind.Absolute);
                            ItemStructure imageTag = new(new BitmapImage(iconUri), buyBID, recipe.SelectToken("buyB.tag") is JObject buyTagObj ? buyTagObj.ToString() : "");
                            transactionItemsViewModel.BuyB.Source = new BitmapImage(iconUri);
                            transactionItemsViewModel.BuyB.Tag = imageTag;
                        }
                    }
                    #endregion
                    #region sell
                    if (ExistItem)
                    {
                        if (recipe.SelectToken("sell.id") is JToken sellIDObj)
                            sellID = sellIDObj.ToString().Replace("minecraft:", "");
                        if (sellID.Length > 0)
                        {
                            string iconPath = rootPath + sellID + ".png";
                            TransactionItemsViewModel transactionItemsViewModel = context.transactionItems[^1].DataContext as TransactionItemsViewModel;
                            if (File.Exists(iconPath))
                                transactionItemsViewModel.Sell.Source = new BitmapImage(new Uri(iconPath, UriKind.Absolute));

                            if (sellCountObj != null)
                            {
                                transactionItemsViewModel.SellCountDisplayText = "x" + int.Parse(sellCountObj.ToString());
                            }
                            Uri iconUri = new(iconPath, UriKind.Absolute);
                            ItemStructure imageTag = new(new BitmapImage(iconUri), sellID, recipe.SelectToken("sell.tag") is JObject sellTagObj ? sellTagObj.ToString() : "");
                            transactionItemsViewModel.Sell.Source = new BitmapImage(iconUri);
                            transactionItemsViewModel.Sell.Tag = imageTag;
                        }
                    }
                    #endregion
                    #region other
                    if (ExistItem)
                    {
                        TransactionItemsViewModel transactionItemsViewModel = context.transactionItems[^1].DataContext as TransactionItemsViewModel;
                        transactionItemsViewModel.Demand = int.Parse(demand.ToString());
                        transactionItemsViewModel.MaxUses = int.Parse(maxUses.ToString());
                        transactionItemsViewModel.PriceMultiplier = int.Parse(priceMultiplier.ToString());
                        transactionItemsViewModel.RewardExp = rewardExp.ToString() == "1" || rewardExp.ToString() == "true";
                        transactionItemsViewModel.SpecialPrice = int.Parse(specialPrice.ToString());
                        transactionItemsViewModel.Uses = int.Parse(uses.ToString());
                        transactionItemsViewModel.Xp = int.Parse(xp.ToString());
                    }
                    #endregion
                    #endregion
                }
            }
            #endregion

            #region 处理言论数据
            if (nbtObj.SelectToken("Gossips") is JArray Gossips)
            {
                foreach (JObject gossip in Gossips.Cast<JObject>())
                {
                    context.AddGossipItem();
                    JToken value = gossip.SelectToken("Value");
                    GossipsItemsViewModel gossipsItemsViewModel = context.gossipItems[^1].DataContext as GossipsItemsViewModel;
                    if (gossip.SelectToken("Target") is JArray targetUID)
                        gossipsItemsViewModel.TargetText = targetUID[0].ToString() + "," + targetUID[1].ToString() + "," + targetUID[2].ToString() + "," + targetUID[3].ToString();
                    if (gossip.SelectToken("Type") is JObject type)
                        gossipsItemsViewModel.SelectedTypeItemPath = type.ToString();
                    if (value != null)
                        gossipsItemsViewModel.GossipValue = int.Parse(value.ToString());
                }
            }
            #endregion

            #region 处理种类、职业、等级等数据
            if (nbtObj.SelectToken("VillagerData") is JObject VillagerData)
            {
                JToken level = VillagerData.SelectToken("level");
                JToken profession = VillagerData.SelectToken("profession");
                JToken type = VillagerData.SelectToken("type");
                if(level != null)
                context.VillagerLevel = context.VillagerLevelSource.Where(item=>item.Text == level.ToString()).First();
                if(profession != null)
                {
                    string professionType = context.VillagerProfessionTypeDataBase.Where(item => item.Key == profession.ToString().Replace("minecraft:", "")).First().Value;
                    context.VillagerProfessionType = context.VillagerProfessionTypeSource.Where(item => item.Text == professionType).First();
                }
                if(type != null)
                {
                    string villagerType = context.VillagerTypeDataBase.Where(item => item.Key == type.ToString().Replace("minecraft:", "")).First().Value;
                    context.VillagerType = context.VillagerTypeSource.Where(item => item.Text == villagerType).First();
                }
            }
            #endregion

            #region 交配意愿
            if (nbtObj.SelectToken("Willing") is JToken Willing)
                context.Willing = Willing.ToString() == "1" || Willing.ToString() == "true";
            #endregion

            #region 补货间隔
            if (nbtObj.SelectToken("LastRestock") is JToken LastRestock)
                context.LastRestock = double.Parse(LastRestock.ToString());
            #endregion

            #region 当前经验
            if (nbtObj.SelectToken("Xp") is JToken Xp)
                context.Xp = int.Parse(Xp.ToString());
            #endregion
        }

        /// <summary>
        /// 导入实体数据
        /// </summary>
        /// <param name="filePathOrData"></param>
        /// <param name="itemPageList"></param>
        /// <param name="IsPath"></param>
        public static void ImportEntityDataHandler(string filePathOrData, ref ObservableCollection<RichTabItems> itemPageList, bool IsPath = true)
        {
            string GeneratorMode = "Summon";
            bool version1_12 = false;
            string data = IsPath ? File.ReadAllText(filePathOrData) : filePathOrData;

            #region 提取可用NBT数据和实体ID
            string nbtData = "", entityID = "";
            try
            {
                nbtData = data[data.IndexOf('{')..(data.LastIndexOf('}') + 1)];
                //补齐缺失双引号对的key
                nbtData = Regex.Replace(nbtData, @"([\{\[,])([\s+]?\w+[\s+]?):", "$1\"$2\":");
                //清除数值型数据的单位
                nbtData = Regex.Replace(nbtData, @"(\d+[\,\]\}]?)([a-zA-Z])", "$1").Replace("I;", "");

                //召唤实体
                if (Regex.IsMatch(data, @"^/?summon"))
                {
                    GeneratorMode = "Summon";
                    entityID = Regex.Match(data, @"(?<=/?summon\s)([\w:]+)").ToString();
                }
                else//给予怪物蛋
                    if (Regex.IsMatch(data, @"^/?give (@[apesr])|(\w+) ([\w]+_)?spawn_egg"))
                {
                    GeneratorMode = "Give";
                    bool v1_13 = Regex.IsMatch(data, @"(?<=/?give\s@[apesr]\s)(\w+)(?=_spawn_egg)");
                    entityID = Regex.Match(data, @"(?<=/?give\s@[apesr]\s)(\w+)(?=_spawn_egg)").ToString();
                    if (!v1_13)
                        version1_12 = true;
                }
                else
                {
                    Message.PushMessage("导入失败！文件内容格式不合法");
                    return;
                }
            }
            catch(Exception e)
            {
                Message.PushMessage(e.Message,MessageBoxImage.Error);
            }

            try
            {
                JToken entityTagID = JObject.Parse(nbtData).SelectToken("EntityTag.id");
                if (entityTagID != null && entityID.Length == 0)
                    entityID = entityTagID.ToString();
                //过滤掉命名空间
                entityID = Regex.Replace(entityID, @"[\w\\/\.]+:", "").Trim();
            }
            catch
            {
                Message.PushMessage("导入失败！文件内容格式不合法");
                return;
            }
            #endregion

            try
            {
                JObject nbtObj = JObject.Parse(nbtData);
                //启用外部导入模式
                DataRow result = EntityTable.Select("id='minecraft:"+ entityID + "'").First();
                if (result != null)
                {
                    entityID = result["id"].ToString();
                    //添加实体命令
                    AddEntityCommand(nbtObj, entityID, version1_12, GeneratorMode, IsPath ? filePathOrData : "",ref itemPageList);
                }
            }
            catch
            {
                Message.PushMessage("导入失败！文件内容格式不合法",MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 添加实体
        /// </summary>
        private static void AddEntityCommand(JObject externData, string selectedEntityID, bool version1_12, string mode, string filePath,ref ObservableCollection<RichTabItems> itemPageList)
        {
            RichTabItems richTabItems = new()
            {
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                Header = "实体",
                IsContentSaved = true,
                BorderThickness = new(4, 4, 4, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                SelectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC6B23")),
                Foreground = new SolidColorBrush(Colors.White),
                LeftBorderTexture = Application.Current.Resources["TabItemLeft"] as Brush,
                RightBorderTexture = Application.Current.Resources["TabItemRight"] as Brush,
                TopBorderTexture = Application.Current.Resources["TabItemTop"] as Brush,
                SelectedLeftBorderTexture = Application.Current.Resources["SelectedTabItemLeft"] as Brush,
                SelectedRightBorderTexture = Application.Current.Resources["SelectedTabItemRight"] as Brush,
                SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as Brush,
            };
            Generators.EntityGenerator.Components.EntityPagesView entityPages = new() { FontWeight = FontWeights.Normal };
            if (externData != null)
            {
                Generators.EntityGenerator.Components.EntityPagesViewModel context = entityPages.DataContext as Generators.EntityGenerator.Components.EntityPagesViewModel;
                context.Give = mode != "Summon";
                context.ImportMode = true;
                if (filePath.Length > 0 && File.Exists(filePath))
                    context.ExternFilePath = filePath;
                context.ExternallyReadEntityData = externData;
                if (version1_12)
                    context.SelectedVersion = context.VersionSource[^1];
                context.SelectedEntityId = context.EntityIDList.Where(item => item.ComboBoxItemText == selectedEntityID).First();
            }
            richTabItems.Content = entityPages;
            itemPageList.Add(richTabItems);
            if (itemPageList.Count == 1)
            {
                TabControl tabControl = richTabItems.FindParent<TabControl>();
                tabControl.SelectedIndex = 0;
            }
        }
        #endregion

        #region 处理导入外部物品数据
        public static string GetItemDataHandler(string filePathOrData, bool IsPath = true)
        {
            string result = "";
            string data = IsPath ? File.ReadAllText(filePathOrData) : filePathOrData;
            string itemID = "";

            //给予物品
            if (Regex.IsMatch(data, @"^/?give (@[apesr])|(\w+) ([\w]+_)?"))
                itemID = Regex.Match(data, @"(?<=/?give\s@[apesr]\s)(\w+)").ToString();

            if (data is null) return result;

            #region 提取可用NBT数据和物品ID
            if (data.Contains('{') && data.Contains('}'))
                result = data[data.IndexOf('{')..(data.LastIndexOf('}') + 1)];

            //补齐缺失双引号对的key
            result = Regex.Replace(result, @"([\{\[,])([\s+]?\w+[\s+]?):", "$1\"$2\":");
            //清除数值型数据的单位
            result = Regex.Replace(result, @"(\d+[\,\]\}]?)([a-zA-Z])", "$1").Replace("I;", "");

            if (result.Length > 0)
            {
                #region 插入物品ID
                JObject resultObj = JObject.Parse(result);
                if (resultObj.SelectToken("ItemView") is not JObject)
                {
                    if(!data.StartsWith('{') && !data.EndsWith('}'))
                    {
                        int itemCount = int.Parse(Regex.Match(data, @"\d+$").ToString());
                        resultObj = JObject.Parse("{Count:" + itemCount + ",tag:" + result.Replace("\n", "").Replace("\r", "") + "}");
                        resultObj.Add("id", "minecraft:" + itemID);
                    }
                }
                else
                    if(resultObj.SelectToken("ItemView") is JObject itemObj)
                    resultObj = JObject.Parse(itemObj.ToString());
                    result = Regex.Replace(resultObj.ToString(),@"\s+","");
                #endregion
            }

            return result;
            #endregion
        }
        public static void ImportItemDataHandler(string filePathOrData, ref ObservableCollection<RichTabItems> itemPageList, bool IsPath = true, bool ReferenceMode = false)
        {
            string GeneratorMode = "";
            bool version1_12 = false;
            
            string data = IsPath ? File.ReadAllText(filePathOrData) : filePathOrData;

            #region 提取可用NBT数据和实体ID
            string nbtData = "", itemID = "";
            if(data.Contains('{') && data.Contains('}'))
            nbtData = data[data.IndexOf('{')..(data.LastIndexOf('}') + 1)];
            //补齐缺失双引号对的key
            nbtData = Regex.Replace(nbtData, @"([\{\[,])([\s+]?\w+[\s+]?):", "$1\"$2\":");
            //清除数值型数据的单位
            nbtData = Regex.Replace(nbtData, @"(\d+[\,\]\}]?)([a-zA-Z])", "$1").Replace("I;", "");

            //召唤实体
            if (Regex.IsMatch(data, @"^/?summon"))
            {
                GeneratorMode = "Summon";
                itemID = Regex.Match(data, @"(?<=/?summon\s)([\w:]+)").ToString();
            }
            else//给予怪物蛋
                if (Regex.IsMatch(data, @"^/?give (@[apesr])|(\w+) ([\w]+_)?"))
            {
                GeneratorMode = "Give";
                bool v1_13 = Regex.IsMatch(data, @"(?<=/?give\s@[apesr]\s?)(\w+)");
                itemID = Regex.Match(data, @"(?<=/?give\s@[apesr]\s?)(\w+)").ToString();
                if (!v1_13)
                    version1_12 = true;
            }
            else
            if (nbtData.Length == 0)
            {
                Message.PushMessage("导入失败！文件内容格式不合法");
                return;
            }

            try
            {
                JToken itemTagID = JObject.Parse(nbtData).SelectToken("ItemView.id");
                itemTagID ??= JObject.Parse(nbtData).SelectToken("id");
                if (itemTagID != null && itemID.Length == 0)
                    itemID = itemTagID.ToString();
                //过滤掉命名空间
                itemID = Regex.Replace(itemID, @"[\w\\/\.]+:", "").Trim();
            }
            catch
            {
                Message.PushMessage("导入失败！文件内容格式不合法");
                return;
            }
            #endregion

            try
            {
                JObject nbtObj = JObject.Parse(nbtData);
                //启用外部导入模式
                DataRow[] results = ItemTable.Select("id='"+itemID+"'");
                if (results.Length > 0)
                {
                    itemID = results[0]["id"].ToString();
                    //添加实体命令
                    if(!ReferenceMode)
                    AddItemCommand(nbtObj, itemID, version1_12, GeneratorMode, IsPath ? filePathOrData : "", ref itemPageList);
                }
            }
            catch
            {
                Message.PushMessage("导入失败！文件内容格式不合法");
            }
        }

        /// <summary>
        /// 添加物品
        /// </summary>
        private static void AddItemCommand(JObject externData, string selectedItemID, bool version1_12, string mode, string filePath, ref ObservableCollection<RichTabItems> itemPageList)
        {
            RichTabItems richTabItems = new()
            {
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                Header = "物品",
                IsContentSaved = true,
                BorderThickness = new(4, 4, 4, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                SelectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC6B23")),
                Foreground = new SolidColorBrush(Colors.White),
                LeftBorderTexture = Application.Current.Resources["TabItemLeft"] as Brush,
                RightBorderTexture = Application.Current.Resources["TabItemRight"] as Brush,
                TopBorderTexture = Application.Current.Resources["TabItemTop"] as Brush,
                SelectedLeftBorderTexture = Application.Current.Resources["SelectedTabItemLeft"] as Brush,
                SelectedRightBorderTexture = Application.Current.Resources["SelectedTabItemRight"] as Brush,
                SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as Brush,
            };
            ItemPagesView itemPages = new() { FontWeight = FontWeights.Normal };
            ItemPageViewModel context = itemPages.DataContext as ItemPageViewModel;
            context.Summon = mode == "Summon";
            if (filePath.Length > 0 && File.Exists(filePath))
                context.ExternFilePath = filePath;
            if (externData != null)
            {
                context.ImportMode = true;
                context.ExternallyReadEntityData = externData;
            }
            if (version1_12)
                context.SelectedVersion.Text = "1.12-";
            context.SelectedItemId = new IconComboBoxItem() { ComboBoxItemId = selectedItemID };

            richTabItems.Content = itemPages;
            itemPageList.Add(richTabItems);
            TabControl tabControl = richTabItems.FindParent<TabControl>();
            if (itemPageList.Count == 1)
            {
                tabControl.SelectedIndex = 0;
            }
            else
            {
                tabControl.SelectedIndex = tabControl.Items.Count - 1;
            }
        }
        #endregion

        #region 处理导入外部烟花数据
        public static void ImportFireworkDataHandler(string filePathOrData, ref ObservableCollection<RichTabItems> itemPageList, bool IsPath = true)
        {
            string GeneratorMode;
            bool version1_12 = false;

            
            string data = IsPath ? File.ReadAllText(filePathOrData) : filePathOrData;

            #region 提取可用NBT数据和烟花ID
            string nbtData = "";
            if (data.Contains('{') && data.Contains('}'))
                nbtData = data[data.IndexOf('{')..(data.LastIndexOf('}') + 1)];
            //补齐缺失双引号对的key
            nbtData = Regex.Replace(nbtData, @"([\{\[,])([\s+]?\w+[\s+]?):", "$1\"$2\":");
            //清除数值型数据的单位
            nbtData = Regex.Replace(nbtData, @"(\d+[\,\]\}]?)([a-zA-Z])", "$1").Replace("I;", "");

            //召唤实体
            if (Regex.IsMatch(data, @"^/?summon"))
                GeneratorMode = "Summon";
            else//给予怪物蛋
                if (Regex.IsMatch(data, @"^/?give (@[apesr])|(\w+) ((minecraft:)?(firework_rocket))"))
            {
                GeneratorMode = "Give";
                bool v1_13 = Regex.IsMatch(data, @"(?<=/?give\s@[apesr]\s)(\w+)");
                if (!v1_13)
                    version1_12 = true;
            }
            else
            {
                Message.PushMessage("导入失败！文件内容格式不合法");
                if (itemPageList.Count > 0)
                itemPageList.RemoveAt(itemPageList.Count - 1);
                return;
            }

            try
            {
                JToken itemTagID = JObject.Parse(nbtData).SelectToken("ItemView.id");
            }
            catch
            {
                Message.PushMessage("导入失败！文件内容格式不合法");
                return;
            }
            #endregion

            try
            {
                JObject nbtObj = JObject.Parse(nbtData);
                //添加实体命令
                AddFireworkCommand(nbtObj, version1_12, GeneratorMode, IsPath ? filePathOrData : "", ref itemPageList);
            }
            catch
            {
                Message.PushMessage("导入失败！文件内容格式不合法");
            }
        }

        /// <summary>
        /// 添加烟花
        /// </summary>
        private static void AddFireworkCommand(JObject externData, bool version1_12, string mode, string filePath, ref ObservableCollection<RichTabItems> itemPageList)
        {
            RichTabItems richTabItems = new()
            {
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                Header = "烟花",
                IsContentSaved = true,
                BorderThickness = new(4, 4, 4, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                SelectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC6B23")),
                Foreground = new SolidColorBrush(Colors.White),
                LeftBorderTexture = Application.Current.Resources["TabItemLeft"] as Brush,
                RightBorderTexture = Application.Current.Resources["TabItemRight"] as Brush,
                TopBorderTexture = Application.Current.Resources["TabItemTop"] as Brush,
                SelectedLeftBorderTexture = Application.Current.Resources["SelectedTabItemLeft"] as Brush,
                SelectedRightBorderTexture = Application.Current.Resources["SelectedTabItemRight"] as Brush,
                SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as Brush,
            };
            FireworkRocketPagesView itemPages = new() { FontWeight = FontWeights.Normal };
            FireworkRocketPagesViewModel context = itemPages.DataContext as FireworkRocketPagesViewModel;
            if (externData != null)
            {
                context.Give = mode != "Summon";
                context.ImportMode = true;
                if (filePath.Length > 0 && File.Exists(filePath))
                    context.ExternFilePath = filePath;
                context.ExternallyReadEntityData = externData;
                if (version1_12)
                    context.SelectedVersion.Text = "1.12-";
            }
            itemPageList.Add(richTabItems);
            if (itemPageList.Count == 1)
            {
                TabControl tabControl = richTabItems.FindParent<TabControl>();
                tabControl.SelectedIndex = 0;
            }
            richTabItems.Content = itemPages;
        }
        #endregion
    }
}