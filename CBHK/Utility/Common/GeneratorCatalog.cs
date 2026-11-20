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
        /// <summary>
        /// 「数据包变种资源」页的下拉候选：Id 转下划线就是 mcdoc 的 dispatch 键
        /// （CatSoundVariant → cat_sound_variant）。按动物分组，基础变种在前、音效变体在后。
        /// 这 13 个入口各自的 #[since] 不同，版本下拉由页面按当前入口重算。
        /// </summary>
        public static readonly IReadOnlyList<GeneratorEntry> VariantEntries =
        [
            new("CatVariant", "猫变种"),
            new("CatSoundVariant", "猫音效变种"),
            new("ChickenVariant", "鸡变种"),
            new("ChickenSoundVariant", "鸡音效变种"),
            new("CowVariant", "牛变种"),
            new("CowSoundVariant", "牛音效变种"),
            new("FrogVariant", "青蛙变种"),
            new("PaintingVariant", "画变种"),
            new("PigVariant", "猪变种"),
            new("PigSoundVariant", "猪音效变种"),
            new("WolfVariant", "狼变种"),
            new("WolfSoundVariant", "狼音效变种"),
            new("ZombieNautilusVariant", "僵尸鹦鹉螺变种"),
        ];

        public static readonly IReadOnlyList<GeneratorEntry> Entries =
        [
            new("Advancement", "进度"),
            new("Armorstand", "盔甲架"),
            new("BlockSoundSet","方块音效集"),
            new("BlockTransformer","方块转换器"),
            new("BannerPattern","旗帜图案"),
            new("ChatType", "聊天类型"),
            new("CustomWorld", "自定义世界"),
            new("DamageType", "伤害类型"),
            new("Datapack", "数据包"),
            new("Dimension", "维度"),
            new("DimensionType", "维度类型"),
            new("Entity", "实体"),
            new("Firework", "烟花"),
            new("Item", "物品"),
            new("ItemModifier", "物品修饰器"),
            new("LootTable", "战利品表"),
            new("Ooc", "单命令"),
            new("Predicate", "战利品表谓词"),
            new("Recipe", "配方"),
            new("Sign", "告示牌"),
            new("Spawner", "刷怪笼"),
            new("Tag", "标签"),
            new("Villager", "村民"),
            new("WrittenBook", "成书"),
            new("BlockStateProvider", "方块状态提供器"),
            new("ContextFloatProvider", "浮点数数值提供器"),
            new("ContextIntProvider", "整数数值提供器"),
            new("DecoratedPotPattern", "饰纹陶罐图案"),
            new("Dialog", "对话框"),
            new("Enchantment", "魔咒"),
            new("EnchantmentProvider", "魔咒提供器"),
            new("Instrument", "山羊角乐器"),
            new("JukeboxSong", "唱片机曲目"),
            new("SlotSource", "槽位源"),
            new("SulfurCubeArchetype", "硫方怪原型"),
            new("TestEnvironment", "测试环境"),
            new("TestInstance", "测试实例"),
            new("Timeline", "时间线"),
            new("TradeSet", "交易集"),
            new("TrialSpawner", "试炼刷怪笼配置"),
            new("TrimMaterial", "纹饰材料"),
            new("TrimPattern", "纹饰图案"),
            new("VillagerTrade", "村民交易"),
            new("WorldClock", "世界时钟"),
            new("VariantResource", "数据包变种资源"),
        ];
    }
}
