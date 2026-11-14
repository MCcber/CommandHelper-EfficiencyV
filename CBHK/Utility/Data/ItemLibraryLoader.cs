using CBHK.Model.Constant;
using CBHK.Model.Data;
using CBHK.Utility.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CBHK.Utility.Data
{
    /// <summary>
    /// 物品库后台：村民页与配方页共用同一套扫描、并行解码与排序
    /// </summary>
    public static class ItemLibraryLoader
    {
        public const string DefaultVersionKey = "1.20.5";

        /// <summary>
        /// 自定义物品存档目录
        /// </summary>
        public static string ItemSaveFolderPath { get; } = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Saves\Item";

        /// <summary>
        /// 读取指定版本存在的物品 id 集合，每次现读，不做缓存
        /// </summary>
        public static List<string> LoadItemIds(Resource resource, string versionKey)
        {
            List<string> itemIds = [];

            if (resource?.RunningDataObject is null || string.IsNullOrEmpty(versionKey))
                return itemIds;
            if (!resource.RunningDataObject.TryGetValue(versionKey, out JToken versionData))
                return itemIds;
            if (versionData.SelectToken("item") is not JArray itemArray)
                return itemIds;

            foreach (JToken token in itemArray)
            {
                string itemId = token.Value<string>()?.Replace("minecraft:", "");
                if (!string.IsNullOrEmpty(itemId))
                    itemIds.Add(itemId);
            }
            return itemIds;
        }

        /// <summary>
        /// 载入原版物品：按注册表 item 数组匹配图集，按文件名排序
        /// </summary>
        public static Task<List<ItemStructure>> LoadOriginalItemsAsync(Resource resource, string versionKey = DefaultVersionKey)
        {
            return Task.Run(() =>
            {
                List<ItemStructure> items = [];

                if (resource?.RunningDataObject is null
                    || !resource.RunningDataObject.TryGetValue(versionKey, out JToken token)
                    || token.SelectToken("item") is not JArray itemArray)
                    return items;

                string imageDir = AppDomain.CurrentDomain.BaseDirectory + resource.ImageSetDirectoryPath;
                if (!Directory.Exists(imageDir))
                    return items;

                HashSet<string> fileSet = new(
                    Directory.EnumerateFiles(imageDir, "*.png").Select(Path.GetFileNameWithoutExtension),
                    StringComparer.OrdinalIgnoreCase);

                (string key, string file)[] plans = [.. itemArray
                    .Select(token => token.Value<string>())
                    .Where(key => !string.IsNullOrEmpty(key))
                    .Select(key => (key, file: fileSet.Contains(key) ? key + ".png"
                                                    : fileSet.Contains(key + "_spawn_egg") ? key + "_spawn_egg.png"
                                                    : null))
                    .Where(plan => plan.file is not null)
                    .OrderBy(plan => plan.file, StringComparer.OrdinalIgnoreCase)];

                ItemStructure[] result = new ItemStructure[plans.Length];
                Parallel.For(0, plans.Length, i =>
                {
                    try
                    {
                        BitmapImage bitmap = new();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(Path.Combine(imageDir, plans[i].file), UriKind.Absolute);
                        bitmap.EndInit();
                        bitmap.Freeze();

                        result[i] = new ItemStructure { IDAndName = plans[i].key, ImagePath = bitmap };
                    }
                    catch
                    {
                        // 单张坏图丢掉，不影响整体
                    }
                });

                return [.. result.Where(item => item is not null)];
            });
        }

        /// <summary>
        /// 载入自定义物品：读存档 NBT 的 oldID 匹配图集，按存档文件名排序
        /// </summary>
        public static Task<List<ItemStructure>> LoadCustomItemsAsync(Resource resource)
        {
            return Task.Run(() =>
            {
                List<ItemStructure> items = [];

                if (resource?.RunningDataObject is null)
                    return items;

                string imageDir = AppDomain.CurrentDomain.BaseDirectory + resource.ImageSetDirectoryPath;
                if (!Directory.Exists(imageDir) || !Directory.Exists(ItemSaveFolderPath))
                    return items;

                HashSet<string> imageFileSet = new(
                    Directory.EnumerateFiles(imageDir, "*.png").Select(Path.GetFileNameWithoutExtension),
                    StringComparer.OrdinalIgnoreCase);

                (string saveName, string nbt, string key, string file)[] plans = [.. Directory.EnumerateFiles(ItemSaveFolderPath)
                    .Select(path =>
                    {
                        string saveName = Path.GetFileNameWithoutExtension(path);

                        string nbt;
                        try
                        {
                            nbt = ExternalCommandParser.TryGetItemNbt(ExternalCommandParser.Read(path, true), out string itemNbt, out _, out _)
                                ? itemNbt
                                : "";
                        }
                        catch
                        {
                            return (saveName, nbt: (string)null, key: (string)null, file: (string)null);
                        }

                        string key = null;
                        try
                        {
                            if (JObject.Parse(nbt)["oldID"] is JToken idToken)
                                key = idToken.Value<string>()?.Replace("minecraft:", "");
                        }
                        catch
                        {
                            return (saveName, nbt, key: (string)null, file: (string)null);
                        }

                        string file = null;
                        if (key is not null)
                        {
                            if (imageFileSet.Contains(key))
                                file = key + ".png";
                            else if (imageFileSet.Contains(key + "_spawn_egg"))
                                file = key + "_spawn_egg.png";
                        }

                        return (saveName, nbt, key, file);
                    })
                    .Where(plan => plan.nbt is not null && plan.key is not null && plan.file is not null)
                    .OrderBy(plan => plan.saveName, StringComparer.OrdinalIgnoreCase)];

                ItemStructure[] result = new ItemStructure[plans.Length];
                Parallel.For(0, plans.Length, i =>
                {
                    try
                    {
                        BitmapImage bitmap = new();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.DecodePixelWidth = 64;
                        bitmap.UriSource = new Uri(Path.Combine(imageDir, plans[i].file), UriKind.Absolute);
                        bitmap.EndInit();
                        bitmap.Freeze();

                        result[i] = new ItemStructure
                        {
                            IDAndName = plans[i].key,
                            NBT = plans[i].nbt,
                            ImagePath = bitmap
                        };
                    }
                    catch
                    {
                        // 单张坏图丢掉，不影响整体
                    }
                });

                return [.. result.Where(item => item is not null)];
            });
        }
    }
}
