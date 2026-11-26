using System.Collections.Generic;
using CBHK.Model.Constant;

namespace CBHK.Model.Data
{
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
            new("Advancement", "进度") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.JsonFile, McdocEntryId = "advancement",OutputCategory = GeneratorCategory.JsonFile },
            new("Armorstand", "盔甲架") { FunctionCategory = GeneratorCategory.Entity | GeneratorCategory.Command, OutputCategory = GeneratorCategory.Command },
            new("BlockSoundSet", "方块音效集") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.Resource | GeneratorCategory.JsonFile, McdocEntryId = "block_transformer", OutputCategory = GeneratorCategory.Resource },
            new("BlockTransformer", "方块转换器") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.Resource | GeneratorCategory.JsonFile, McdocEntryId = "block_transformer", OutputCategory = GeneratorCategory.World },
            new("BannerPattern", "旗帜图案") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.Resource | GeneratorCategory.JsonFile, McdocEntryId = "banner_pattern", SinceVersion = "1.20.5",OutputCategory = GeneratorCategory.Resource },
            new("ChatType", "聊天类型") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.JsonFile, McdocEntryId = "chat_type", OutputCategory = GeneratorCategory.JsonFile },
            new("CustomWorld", "自定义世界") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.World | GeneratorCategory.JsonFile, McdocEntryId = "biome", SinceVersion = "1.16", OutputCategory = GeneratorCategory.JsonFile },
            new("DamageType", "伤害类型") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.JsonFile, McdocEntryId = "damage_type", SinceVersion = "1.19.4", OutputCategory = GeneratorCategory.JsonFile },
            new("Datapack", "数据包") { FunctionCategory = GeneratorCategory.Datapack, OutputCategory = GeneratorCategory.Datapack },
            new("Dimension", "维度") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.World | GeneratorCategory.JsonFile, OutputCategory = GeneratorCategory.JsonFile },
            new("DimensionType", "维度类型") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.World | GeneratorCategory.JsonFile, McdocEntryId = "dimension_type", OutputCategory = GeneratorCategory.JsonFile },
            new("Entity", "实体") { FunctionCategory = GeneratorCategory.Entity | GeneratorCategory.Command, OutputCategory = GeneratorCategory.Entity },
            new("Firework", "烟花") { FunctionCategory = GeneratorCategory.Item | GeneratorCategory.Command, OutputCategory = GeneratorCategory.Item },
            new("Item", "物品") { FunctionCategory = GeneratorCategory.Item | GeneratorCategory.Command, OutputCategory = GeneratorCategory.Item },
            new("ItemModifier", "物品修饰器") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.JsonFile, McdocEntryId = "item_modifier", SinceVersion = "1.17", OutputCategory = GeneratorCategory.JsonFile },
            new("LootTable", "战利品表") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.JsonFile, McdocEntryId = "loot_table", OutputCategory = GeneratorCategory.JsonFile },
            new("Ooc", "单命令") { FunctionCategory = GeneratorCategory.Command | GeneratorCategory.Entity, OutputCategory = GeneratorCategory.Command },
            new("Predicate", "战利品表谓词") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.JsonFile, McdocEntryId = "predicate", OutputCategory = GeneratorCategory.JsonFile },
            new("Recipe", "配方") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.JsonFile, McdocEntryId = "recipe", OutputCategory = GeneratorCategory.JsonFile },
            new("Sign", "告示牌"){ FunctionCategory = GeneratorCategory.Item | GeneratorCategory.Command, McdocEntryId = "sign", OutputCategory = GeneratorCategory.Item },
            new("Spawner", "刷怪笼") { FunctionCategory = GeneratorCategory.Entity | GeneratorCategory.Command, OutputCategory = GeneratorCategory.Command },
            new("Tag", "标签") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.JsonFile, OutputCategory = GeneratorCategory.JsonFile },
            new("Villager", "村民") { FunctionCategory = GeneratorCategory.Entity | GeneratorCategory.Command, OutputCategory = GeneratorCategory.Entity },
            new("WrittenBook", "成书") { FunctionCategory = GeneratorCategory.Item | GeneratorCategory.Command, OutputCategory = GeneratorCategory.Item },
            new("BlockStateProvider", "方块状态提供器") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.Resource | GeneratorCategory.JsonFile, McdocEntryId = "worldgen/block_state_provider" },
            new("ContextFloatProvider", "浮点数数值提供器") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.Resource | GeneratorCategory.JsonFile, McdocEntryId = "context_float_provider" },
            new("ContextIntProvider", "整数数值提供器") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.Resource | GeneratorCategory.JsonFile, McdocEntryId = "context_int_provider" },
            new("DecoratedPotPattern", "饰纹陶罐图案") { FunctionCategory = GeneratorCategory.Resource | GeneratorCategory.Datapack | GeneratorCategory.JsonFile, McdocEntryId = "decorated_pot_pattern" },
            new("Dialog", "对话框") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.JsonFile, McdocEntryId = "dialog", SinceVersion = "1.21.6" },
            new("Enchantment", "魔咒") {FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.Resource | GeneratorCategory.JsonFile, McdocEntryId = "enchantment", SinceVersion = "1.21" },
            new("EnchantmentProvider", "魔咒提供器") {FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.Resource | GeneratorCategory.JsonFile, McdocEntryId = "enchantment_provider", SinceVersion = "1.21" },
            new("Instrument", "山羊角乐器") {FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.Resource | GeneratorCategory.JsonFile, McdocEntryId = "instrument", SinceVersion = "1.21.2" },
            new("JukeboxSong", "唱片机曲目") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.Resource | GeneratorCategory.JsonFile, McdocEntryId = "jukebox_song", SinceVersion = "1.21" },
            new("SlotSource", "槽位源") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.JsonFile, McdocEntryId = "slot_source" },
            new("SulfurCubeArchetype", "硫方怪原型") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.Entity | GeneratorCategory.JsonFile, McdocEntryId = "sulfur_cube_archetype" },
            new("TestEnvironment", "测试环境") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.World | GeneratorCategory.Resource | GeneratorCategory.JsonFile, McdocEntryId = "test_environment" },
            new("TestInstance", "测试实例") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.World | GeneratorCategory.Resource | GeneratorCategory.JsonFile, McdocEntryId = "test_instance" },
            new("Timeline", "时间线") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.World | GeneratorCategory.Resource | GeneratorCategory.JsonFile, McdocEntryId = "timeline", SinceVersion = "1.21.11" },
            new("TradeSet", "交易集") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.JsonFile, McdocEntryId = "trade_set" },
            new("TrialSpawner", "试炼刷怪笼配置") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.Entity | GeneratorCategory.JsonFile, McdocEntryId = "trial_spawner", SinceVersion = "1.21.2" },
            new("TrimMaterial", "纹饰材料") {FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.Resource | GeneratorCategory.JsonFile, McdocEntryId = "trim_material", SinceVersion = "1.19.4" },
            new("TrimPattern", "纹饰图案") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.Resource | GeneratorCategory.JsonFile, McdocEntryId = "trim_pattern", SinceVersion = "1.19.4" },
            new("VillagerTrade", "村民交易") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.JsonFile, McdocEntryId = "villager_trade" },
            new("WorldClock", "世界时钟") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.World | GeneratorCategory.JsonFile, McdocEntryId = "world_clock", SinceVersion = "26.1" },
            new("VariantResource", "数据包变种资源") { FunctionCategory = GeneratorCategory.Datapack | GeneratorCategory.Resource | GeneratorCategory.JsonFile },
        ];
    }
}
