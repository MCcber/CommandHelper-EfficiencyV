using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace CBHK.Utility.Common
{
    /// <summary>
    /// 外部数据（give/summon/setblock 命令文本或文件）→ 可物化的 NBT 文本。
    /// 只做解析，不碰任何界面：各生成器自己决定拿到 NBT 之后是灌 JsonText 还是填控件。
    /// 规则与原导入工具一致（正则、1.12/1.13+ 分支都不动），
    /// 唯一区别是解析失败返回 false，由调用方自己弹提示。
    /// </summary>
    public static class ExternalCommandParser
    {
        /// <summary>文本或文件内容（isPath 时读文件；读不到当空串）。</summary>
        public static string Read(string textOrPath, bool isPath)
        {
            if (string.IsNullOrEmpty(textOrPath)) return "";
            if (!isPath) return textOrPath;
            try
            {
                return File.ReadAllText(textOrPath);
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>把花括号里那段 NBT 抠出来，并补齐缺引号的 key、清掉数值单位（NBT 写法差异）。</summary>
        public static string ExtractBracedNbt(string data)
        {
            if (string.IsNullOrEmpty(data) || !data.Contains('{') || !data.Contains('}')) return "";
            var nbt = data[data.IndexOf('{')..(data.LastIndexOf('}') + 1)];
            nbt = Regex.Replace(nbt, @"([\{\[,])([\s+]?\w+[\s+]?):", "$1\"$2\":");
            nbt = Regex.Replace(nbt, @"(\d+[,\]\}]?)([a-zA-Z])", "$1").Replace("I;", "");
            return nbt;
        }

        /// <summary>
        /// 去掉旧页面时代的 oldID（只对"拿 NBT 当文档内容"的调用方有意义）：
        /// 留着它会在 mcdoc 树上多出一行"未声明键"。
        /// </summary>
        public static string RemoveOldId(string nbt)
        {
            if (string.IsNullOrWhiteSpace(nbt)) return nbt;
            try
            {
                var obj = JObject.Parse(nbt);
                obj.Remove("oldID");
                if (obj.SelectToken("EntityTag") is JObject entityTag) entityTag.Remove("oldID");
                return obj.ToString();
            }
            catch (Exception)
            {
                return nbt;
            }
        }

        /// <summary>去掉资源 id 的命名空间（命令里是 minecraft:x，下拉候选是裸 id）。</summary>
        public static string StripNamespace(string id)
        {
            if (string.IsNullOrEmpty(id)) return id;
            var colon = id.LastIndexOf(':');
            return colon >= 0 && colon + 1 < id.Length ? id[(colon + 1)..] : id;
        }

        /// <summary>命令是不是召唤写法（give 侧没有就是给予；summon 侧就是召唤）。</summary>
        public static bool IsSummonCommand(string data) => !string.IsNullOrEmpty(data) && Regex.IsMatch(data, @"^/?summon");

        /// <summary>give 命令 → (物品 NBT, 物品 id 含命名空间, 是否召唤写法)。解析不出 NBT 时返回 false（不抛）。</summary>
        public static bool TryGetItemNbt(string data, out string nbt, out string itemId, out bool summon)
        {
            nbt = "";
            itemId = "";
            summon = IsSummonCommand(data);
            if (string.IsNullOrEmpty(data)) return false;
            if (Regex.IsMatch(data, @"^/?give (@[apesr])|(\w+) ([\w]+_)?"))
            {
                itemId = Regex.Match(data, @"(?<=/?give\s@[apesr]\s)([\w:]+)").ToString();
            }
            nbt = ExtractBracedNbt(data);
            if (nbt.Length == 0)
            {
                // 命令里没带 NBT（give @p minecraft:stone）也要能用：id 还在，内容交空对象
                if (itemId.Length == 0) return false;
                nbt = "{}";
                return true;
            }
            try
            {
                var resultObj = JObject.Parse(nbt);
                // 命令里没给 id（裸 NBT）：从值本身兜底，别让页面停在"上一次选的物品"上
                if (itemId.Length == 0)
                {
                    itemId = resultObj.SelectToken("oldID")?.ToString()
                        ?? resultObj.SelectToken("itemView.oldID")?.ToString() ?? "";
                }
                if (resultObj.SelectToken("itemView") is not JObject)
                {
                    if (!data.StartsWith('{') && !data.EndsWith('}'))
                    {
                        int itemCount = int.Parse(Regex.Match(data, @"\d+$").ToString());
                        resultObj = JObject.Parse("{Count:" + itemCount + ",tag:" + nbt.Replace("\n", "").Replace("\r", "") + "}");
                        resultObj.Add("oldID", "minecraft:" + itemId);
                    }
                }
                else if (resultObj.SelectToken("itemView") is JObject itemObj)
                {
                    resultObj = JObject.Parse(itemObj.ToString());
                }
                nbt = Regex.Replace(resultObj.ToString(), @"\s+", "");
                return nbt.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>summon 命令 → (实体 NBT, 实体 id 含命名空间)；NBT 里缺 oldID 时按命令补齐。</summary>
        public static bool TryGetEntityNbt(string data, out string nbt, out string entityId, out bool summon)
        {
            nbt = "";
            entityId = "";
            summon = IsSummonCommand(data);
            if (string.IsNullOrEmpty(data)) return false;
            if (Regex.IsMatch(data, @"^/?summon"))
            {
                entityId = Regex.Match(data, @"(?<=/?summon\s)([\w:]+)").ToString();
            }
            else if (Regex.IsMatch(data, @"^/?give\s"))
            {
                // 给予刷怪蛋：give @p pig_spawn_egg —— 实体 id 就是物品 id 去掉 _spawn_egg
                entityId = Regex.Match(data, @"(?<=/?give\s@[apesr]\s)([\w:]+)").ToString();
                if (entityId.EndsWith("_spawn_egg", StringComparison.OrdinalIgnoreCase))
                {
                    entityId = entityId[..^"_spawn_egg".Length];
                }
            }
            nbt = ExtractBracedNbt(data);
            if (nbt.Length == 0)
            {
                // 命令里没带 NBT（summon minecraft:pig ~ ~ ~）也要能用：id 还在，合成一份最小 NBT
                if (entityId.Length == 0) return false;
                // 必须是严格 JSON：这条路的下游（JsonText / mcdoc 文档）只吃 JSON，不吃 NBT 的裸键写法
                nbt = new JObject { ["id"] = "minecraft:" + StripNamespace(entityId) }.ToString();
                return true;
            }
            try
            {
                var resultObj = JObject.Parse(nbt);
                if (resultObj.SelectToken("oldID") is null && resultObj.SelectToken("EntityTag.oldID") is null)
                {
                    // 归一后再拼命名空间：命令里本来就带 minecraft:，直接拼会出现 minecraft:minecraft:pig
                    resultObj.Add("oldID", "\"minecraft:" + StripNamespace(entityId) + "\"");
                }
                nbt = resultObj.ToString();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>setblock/fill 刷怪笼 → (NBT 文本, 是不是 1.12 及以前的写法)。</summary>
        public static bool TryGetSpawnerNbt(string data, out string nbt, out bool legacy)
        {
            nbt = ExtractBracedNbt(data);
            legacy = !string.IsNullOrEmpty(data) && Regex.IsMatch(data, @"^/setblock (minecraft:)?mob_spawner");
            return nbt.Length > 0;
        }
    }
}