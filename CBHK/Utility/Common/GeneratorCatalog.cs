using System.Collections.Generic;

namespace CBHK.Utility
{
    /// <summary>主页生成器入口：Id 同时是 DistributorGenerator.commandDictionary 的键和图标文件名。</summary>
    public sealed record GeneratorEntry(string Id, string Name);

    /// <summary>
    /// 主页生成器入口清单。以前这份清单来自部署库的 Generator 表（ID + 中文名），
    /// 库删掉后只剩 Configuration.json 里 8 个 mcdoc 调度器，其它 13 个入口整片消失。
    /// 它本来就是代码事实（每个入口对应 DistributorGenerator 里一个 Start* 方法），
    /// 所以钉回代码里，不再依赖外部数据；Id 必须与 commandDictionary 的键、图标名一致。
    /// </summary>
    public static class GeneratorCatalog
    {
        public static readonly IReadOnlyList<GeneratorEntry> Entries =
        [
            new("Advancement", "进度"),
            new("Ooc", "单命令"),
            new("DamageType", "伤害类型"),
            new("Datapack", "数据包"),
            new("ChatType", "聊天类型"),
            new("CustomWorld", "自定义世界"),
            new("Armorstand", "盔甲架"),
            new("WrittenBook", "成书"),
            new("Spawner", "刷怪笼"),
            new("Recipe", "配方"),
            new("Villager", "村民"),
            new("Tag", "标签"),
            new("Item", "物品"),
            new("LootTable", "战利品表"),
            new("Predicate", "战利品表谓词"),
            new("ItemModifier", "物品修饰器"),
            new("Firework", "烟花"),
            new("Entity", "实体"),
            new("Sign", "告示牌"),
            new("Dimension", "维度"),
            new("DimensionType", "维度类型"),
        ];
    }
}
