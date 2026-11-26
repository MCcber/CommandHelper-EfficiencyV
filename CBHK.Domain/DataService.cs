using MinecraftLanguageModelLibrary.Data;
using MinecraftLanguageModelLibrary.Utility;

namespace CBHK.Domain
{
    public partial class DataService
    {
        #region Field
        private RegistryDataService registry;
        private ICatalogEnumProvider enumProvider;
        private RegexService regexService;
        private List<string> ItemSlotList = [];
        private List<string> Enchantments = [];
        private List<string> DamageTypeList = [];
        private List<string> DimensionIDList = [];
        private List<string> SelectorParameterList = [];
        private Dictionary<string, string> SelectorParameterValueList = [];
        private Dictionary<string, GameRuleItem> GameRuleList = [];
        private List<string> TeamColorList = [];
        private List<string> BossbarColorList = [];
        private List<string> BossbarStyleList = [];
        private List<string> ItemIDList = [];
        private List<string> BlockIDList = [];
        private List<string> EntityIDList = [];
        private List<string> ParticleIDList = [];
        private Dictionary<string, string> SoundDictionary = [];
        private List<string> MobAttributeList = [];
        private List<string> EffectIDList = [];
        private List<string> LootToolList = [];
        private List<string> ScoreboardTypeList = [];
        private List<string> ScoreboardCustomIDList = [];
        private List<string> AdvancementValueList = [];
        #endregion

        #region DataStructure
        private Dictionary<int, Dictionary<string, string>> ItemGroupByVersionDicionary = [];
        private Dictionary<int, Dictionary<string, string>> EnchantmentGroupByVersionDicionary = [];
        private Dictionary<int, Dictionary<string, string>> EntityGroupByVersionDictionary = [];
        #endregion

        /// <summary>
        /// 数据只来自两处：<see cref="RegistryDataService"/>（原版注册表，按版本分组）与
        /// <see cref="ICatalogEnumProvider"/>（命令语法闭合枚举，由 mcdoc 资产库解析）。
        /// 两者都取不到时列表为空——不再有数据库可读。
        /// </summary>
        public DataService(RegistryDataService Registry,ICatalogEnumProvider EnumProvider,RegexService RegexService)
        {
            registry = Registry;
            enumProvider = EnumProvider;
            regexService = RegexService;

            #region 根据版本分类所有物品ID
            foreach (string version in registry.Versions)
            {
                ItemGroupByVersionDicionary[ToVersionNumber(version)] = registry.GetIds("item", version)
                    .ToDictionary(id => id, id => id);
            }
            #endregion

            #region 根据版本分类所有附魔ID
            foreach (string version in registry.Versions)
            {
                EnchantmentGroupByVersionDicionary[ToVersionNumber(version)] = registry.GetIds("enchantment", version)
                    .ToDictionary(id => id, id => id);
            }
            #endregion

            #region 根据版本分类所有实体ID
            foreach (string version in registry.Versions)
            {
                EntityGroupByVersionDictionary[ToVersionNumber(version)] = registry.GetIds("entity_type", version)
                    .ToDictionary(id => id, id => id);
            }
            #endregion

            #region 扁平列表一律取最高可用版本
            ItemIDList.AddRange(registry.GetIds("item"));
            BlockIDList.AddRange(registry.GetIds("block"));
            EntityIDList.AddRange(registry.GetIds("entity_type"));
            ParticleIDList.AddRange(registry.GetIds("particle_type"));
            DamageTypeList.AddRange(registry.GetIds("damage_type"));
            AdvancementValueList.AddRange(registry.GetIds("advancement"));
            MobAttributeList.AddRange(registry.GetIds("attribute"));
            EffectIDList.AddRange(registry.GetIds("mob_effect"));
            foreach (string sound in registry.GetIds("sound_event"))
            {
                // 注册表只给 id，没有可读名：用 id 充当名称
                SoundDictionary[sound] = sound;
            }
            #endregion

            #region 添加游戏规则名称
            foreach (string gameRule in registry.GetIds("game_rule"))
            {
                GameRuleList[gameRule] = new GameRuleItem();
            }
            #endregion

            #region 添加维度
            DimensionIDList.AddRange(registry.GetIds("dimension"));
            if (DimensionIDList.Count == 0)
            {
                DimensionIDList.AddRange(["minecraft:overworld", "minecraft:the_nether", "minecraft:the_end"]);
            }
            #endregion

            #region 添加物品槽位编号
            ItemSlotList.AddRange(enumProvider.ItemSlots);
            #endregion

            #region 添加选择器参数和取值
            SelectorParameterList.AddRange(enumProvider.SelectorParameters);
            foreach (KeyValuePair<string, string> pair in enumProvider.SelectorParameterValues)
            {
                SelectorParameterValueList[pair.Key] = pair.Value;
            }
            #endregion

            #region 添加队伍颜色、bossbar 颜色与样式
            TeamColorList.AddRange(enumProvider.TeamColors);
            BossbarColorList.AddRange(enumProvider.BossbarColors);
            BossbarStyleList.AddRange(enumProvider.BossbarStyles);
            #endregion

            #region 添加记分板准则与 Custom 命名空间 ID
            ScoreboardTypeList.AddRange(enumProvider.ScoreboardTypes);
            // custom 命名空间下的 ID 就是原版统计项（custom_stat 注册表），比手写常量可靠
            ScoreboardCustomIDList.AddRange(registry.GetIds("custom_stat"));
            if (ScoreboardCustomIDList.Count == 0)
            {
                ScoreboardCustomIDList.AddRange(enumProvider.ScoreboardCustomIds);
            }
            #endregion

            #region 添加战利品表工具
            LootToolList.Add("mainhand");
            LootToolList.Add("offhand");
            LootToolList.AddRange(ItemIDList);
            #endregion
        }

        /// <summary>
        /// 版本键转成调用方沿用的整数形（1.21.11 → 12111）。数据目录里的键是文件名字符串。
        /// </summary>
        private static int ToVersionNumber(string version)
            => int.Parse(version.Replace(".", ""), System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>注册表数据里出现过的版本键（升序）；目录没读到数据时为空。</summary>
        public IReadOnlyList<string> GetVersionList() => registry.Versions;

        /// <summary>指定版本下某个注册表的成员 id；该版本没有这个注册表时返回空。</summary>
        public IReadOnlyList<string> GetIds(string registryName, string version) => registry.GetIds(registryName, version);

        /// <summary>指定版本下出现过的注册表名（tag 目录候选），按名字排序。</summary>
        public IReadOnlyList<string> GetRegistryNames(string version) => registry.GetRegistryNames(version);

        /// <summary>
        /// 获取所有物品的ID列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetItemIDList()
        {
            return ItemIDList;
        }

        /// <summary>
        /// 获取所有方块的ID列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetBlockIDList()
        {
            return BlockIDList;
        }

        /// <summary>
        /// 获取所有实体的ID列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetEntityIDList()
        {
            return EntityIDList;
        }

        /// <summary>
        /// 获取附魔ID与名称的字典
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, Dictionary<string, string>> GetEnchantmentIDAndNameGroupByVersionMap()
        {
            return EnchantmentGroupByVersionDicionary;
        }

        /// <summary>
        /// 获取物品槽位编号列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetItemSlotList()
        {
            return ItemSlotList;
        }

        /// <summary>
        /// 获取伤害类型列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetDamageTypeList()
        {
            return DamageTypeList;
        }

        /// <summary>
        /// 获取原版维度列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetDimensionList()
        {
            return DimensionIDList;
        }

        /// <summary>
        /// 获取选择器参数列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetSelectorParameterList()
        {
            return SelectorParameterList;
        }

        /// <summary>
        /// 获取选择器值映射表
        /// </summary>
        /// <returns></returns>
        public Dictionary<string,string> GetSelectorParameterValueList()
        {
            return SelectorParameterValueList;
        }

        /// <summary>
        /// 获取游戏规则映射表
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, GameRuleItem> GetGameRuleMap()
        {
            return GameRuleList;
        }

        /// <summary>
        /// 获取队伍颜色列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetTeamColorList()
        {
            return TeamColorList;
        }

        /// <summary>
        /// 获取Bossbar颜色列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetBossbarColorList()
        {
            return BossbarColorList;
        }

        /// <summary>
        /// 获取Bossbar样式列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetBossbarStyleList()
        {
            return BossbarStyleList;
        }

        /// <summary>
        /// 获取粒子ID列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetParticleIDList()
        {
            return ParticleIDList;
        }

        /// <summary>
        /// 获取音效ID列表
        /// </summary>
        /// <returns></returns>
        public Dictionary<string,string> SoundIDAndNameMap()
        {
            return SoundDictionary;
        }

        /// <summary>
        /// 获取生物属性ID列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetMobAttributeIDList()
        {
            return MobAttributeList;
        }

        /// <summary>
        /// 获取生物状态ID列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetMobEffectIDList()
        {
            return EffectIDList;
        }

        /// <summary>
        /// 获取进度ID列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetAdvancementList()
        {
            return AdvancementValueList;
        }

        /// <summary>
        /// 获取战利品表工具ID列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetLootToolList()
        {
            return LootToolList;
        }

        /// <summary>
        /// 获取记分板准则列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetScoreboardTypeList()
        {
            return ScoreboardTypeList;
        }

        /// <summary>
        /// 获取记分板CustomID列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetScoreboardCustomIDList()
        {
            return ScoreboardCustomIDList;
        }
    }
}
